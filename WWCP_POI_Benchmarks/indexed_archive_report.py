"""Check matched indexed-reader/control recovery reports and summarize their costs (stdlib only)."""
import argparse
import hashlib
import json
import statistics
from pathlib import Path
from archive_recovery_report import SHAPES, INVENTORY, median

OPERATIONS = {'read-cbor', 'read-json', 'read-restore'}


def load(path):
    report = json.loads(Path(path).read_text(encoding='utf-8-sig'))
    env = report['Environment']
    if not report['Complete'] or env['Suite'] != 'recovery' or env['BuildConfiguration'] != 'Release':
        raise ValueError('A complete Release recovery subset is required')
    if (env['ProcessesPerOperation'], env['WarmupsPerProcess'], env['SamplesPerProcess']) != (2, 5, 5):
        raise ValueError('Unexpected measurement method')
    groups = {}
    for run in report['Runs']:
        groups.setdefault((run['Shape']['Name'], run['Operation']), []).append(run)
    if groups.keys() != {(shape, operation) for shape in SHAPES for operation in OPERATIONS}:
        raise ValueError('Missing/unexpected groups')
    for key, runs in groups.items():
        if len(runs) != 2 or any(len(run['Samples']) != 5 for run in runs):
            raise ValueError('Worker/sample count mismatch')
        first = groups[(key[0], 'read-cbor')][0]
        expected = runs[0]['Samples'][0]['Result']
        for run in runs:
            if any(run[field] != first[field] for field in INVENTORY):
                raise ValueError('Repeated inventory mismatch')
            if any(sample['Result'] != expected for sample in run['Samples']):
                raise ValueError('Changed output')
            if run['Samples'][0]['Result']['Identity'] != first['Samples'][0]['Result']['Identity']:
                raise ValueError('Recovered heads differ')
            retention = run['ReplayRetention']
            if (key[1] == 'read-cbor') != (retention is not None):
                raise ValueError('Unexpected retention probe')
            if retention and (retention['RetainedCommits'] != run['RetainedCommits'] or retention['RetainedSnapshots'] != run['RecoveryInventory']['Snapshots']):
                raise ValueError('Retained inventory mismatch')
    return report, groups


def build(before_path, after_path, original_path):
    before, left = load(before_path); after, right = load(after_path)
    original = json.loads(Path(original_path).read_text(encoding='utf-8-sig'))
    if not original['Complete'] or len(original['Runs']) != 98:
        raise ValueError('The original complete stage baseline is required')
    selected = [run for run in original['Runs'] if run['Operation'] in OPERATIONS]
    origin = before['Environment']['DerivedFrom']
    if before['Runs'] != selected or origin['sha256'] != hashlib.sha256(Path(original_path).read_bytes()).hexdigest():
        raise ValueError('Baseline subset is not the exact original records')
    for field in ['BenchmarkSourceSHA256', 'BuildConfiguration', 'Runtime', 'RuntimeTuning', 'Dependencies',
                  'ProcessesPerOperation', 'SamplesPerProcess', 'WarmupsPerProcess']:
        if before['Environment'][field] != after['Environment'][field]:
            raise ValueError('Harness/runtime/method changed: ' + field)
    for key in left:
        for field in INVENTORY:
            if left[key][0][field] != right[key][0][field]:
                raise ValueError('Input/state/peer inventory changed: ' + str(key) + ':' + field)
        if left[key][0]['Samples'][0]['Result'] != right[key][0]['Samples'][0]['Result']:
            raise ValueError('Recovered output changed')
    text = ['# Indexed archive recovery comparison', '',
            f'Before: `{Path(before_path).name}`; after: `{Path(after_path).name}`.', '',
            'Before is an exact 42-worker / 210-sample subset of the preceding 98-worker decoder baseline; '
            'no new before executions are implied. After contains 42 fresh workers / 210 samples. '
            'All seven shapes/four profiles and complete CBOR/JSON recovery plus prepared-model restore match '
            'in bytes, identities, static/commit/receipt/peer inventories and repeated outputs. '
            'The C# harness, runtime tuning, dependencies and 2/5/5 process/warmup/sample method agree.', '',
            'Full-CBOR recovery now indexes/validates borrowed input before decoding individual parts. '
            'Boundary suffix bytes are privately frozen before callbacks can mutate original input; no complete '
            'archive tree remains. JSON and prepared-model restore are controls; their boundary path now uses '
            'the shared synchronous cursor. Verification and retention probes remain outside measurement.', '',
            'No build/test from this task overlaps the new formal series. The host is not dedicated or affinity '
            'controlled. Times and 10 ms sampled peaks are descriptive. Allocation covers the operation thread; '
            'CPU includes the process sampling thread. Collected retention deltas and sampled peaks are not capacity bounds.', '',
            '| Shape | Operation | Before/after ms | Before/after CPU ms | Before/after allocated MiB | Allocation change | After min–max ms | After largest sampled managed MiB |',
            '| --- | --- | ---: | ---: | ---: | ---: | --- | ---: |']
    for key in sorted(left):
        old = left[key]; new = right[key]
        old_alloc = median(old, 'AllocatedBytes'); new_alloc = median(new, 'AllocatedBytes')
        samples = [sample for run in new for sample in run['Samples']]
        text.append(f'| {key[0]} | {key[1]} | {median(old,"ElapsedMilliseconds"):.2f}/{median(new,"ElapsedMilliseconds"):.2f} | '
                    f'{median(old,"CpuMilliseconds"):.2f}/{median(new,"CpuMilliseconds"):.2f} | '
                    f'{old_alloc/1048576:.4f}/{new_alloc/1048576:.4f} | {100*(new_alloc/old_alloc-1):+.2f}% | '
                    f'{min(sample["ElapsedMilliseconds"] for sample in samples):.2f}–{max(sample["ElapsedMilliseconds"] for sample in samples):.2f} | '
                    f'{max(sample["SampledManagedPeakBytes"] for sample in samples)/1048576:.2f} |')
    text += ['', '## Collected additional recovered-history memory', '',
             'One additional actual recovered history is held through collection in each full-CBOR worker, '
             'source/input kept alive on both sides. Approximate deltas include retained states/models/runtime, '
             'and other objects can become collectible. Cumulative allocation and retained memory are distinct.', '',
             '| Shape | Before median MiB | After median MiB | After min–max MiB |', '| --- | ---: | ---: | --- |']
    for shape in sorted(SHAPES):
        old = [run['ReplayRetention']['ManagedDeltaBytes']/1048576 for run in left[(shape, 'read-cbor')]]
        new = [run['ReplayRetention']['ManagedDeltaBytes']/1048576 for run in right[(shape, 'read-cbor')]]
        text.append(f'| {shape} | {statistics.median(old):.4f} | {statistics.median(new):.4f} | {min(new):.4f}–{max(new):.4f} |')
    return '\n'.join(text) + '\n'


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ['before', 'after', 'original', 'output']: parser.add_argument('--' + name, required=True)
    args = parser.parse_args(); destination = Path(args.output)
    if destination.exists(): raise FileExistsError(destination)
    result = build(args.before, args.after, args.original)
    destination.parent.mkdir(parents=True, exist_ok=True); destination.write_text(result, encoding='utf-8')
    print('Verified exact baseline subset, unchanged harness and all matched results; saved', destination)
