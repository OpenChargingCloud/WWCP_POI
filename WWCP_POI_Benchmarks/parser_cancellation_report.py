"""Validate matched cooperative archive-parser measurements (stdlib only)."""
import argparse
import datetime
import json
import statistics
from pathlib import Path
from parser_cancellation_measure import SHAPES, OPERATIONS

INVENTORY = ('Shape', 'Entities', 'RetainedCommits', 'Receipts', 'CatalogIds', 'StaticIdentity',
             'ArchiveIdentity', 'RecoveryInventory')


def load(path):
    value = json.loads(Path(path).read_text(encoding='utf-8-sig')); env = value['Environment']
    if not value['Complete'] or env['Suite'] != 'parser-cancellation' or env['BuildConfiguration'] != 'Release':
        raise ValueError('A complete Release parser-cancellation report is required')
    if (env['ProcessesPerOperation'], env['WarmupsPerProcess'], env['SamplesPerProcess']) != (2, 3, 3):
        raise ValueError('Unexpected worker/warmup/sample method')
    shapes = {row[0]: dict(zip(['Name', 'EVSEs', 'Changes', 'Branches', 'SnapshotEvery', 'Retentions', 'Locations'], row)) for row in SHAPES}
    groups = {}
    for run in value['Runs']:
        key = (run['Shape']['Name'], run['Operation'])
        if run['Shape'] != shapes.get(key[0]):
            raise ValueError('Unexpected shape parameters')
        groups.setdefault(key, []).append(run)
    if set(groups) != {(shape, operation) for shape in shapes for operation in OPERATIONS}:
        raise ValueError('Missing or unexpected workload groups')
    for key, runs in groups.items():
        if len(runs) != 2:
            raise ValueError('Missing fresh process')
        first = runs[0]
        for run in runs:
            if len(run['Samples']) != 3 or any(run[field] != first[field] for field in INVENTORY):
                raise ValueError('Repeated inventory or sample count changed')
            if any(sample['Result'] != first['Samples'][0]['Result'] for sample in run['Samples']):
                raise ValueError('Output changed within a group')
            if any(sample['AllocatedBytes'] < 0 or sample['ElapsedMilliseconds'] < 0 for sample in run['Samples']):
                raise ValueError('Negative operation allocation or elapsed time')
            if run['ChildETagRetention'] is not None or run['ChildETagState'] is not None:
                raise ValueError('Unexpected child-cache probe')
            if key[1] == 'read-cbor':
                retention = run['ReplayRetention']
                if retention['RetainedCommits'] != run['RetainedCommits'] or retention['RetainedSnapshots'] != run['RecoveryInventory']['Snapshots']:
                    raise ValueError('Collected retention inventory changed')
            elif run['ReplayRetention'] is not None:
                raise ValueError('Unexpected retention probe')
    if {groups[(shape, 'read-cbor')][0]['RecoveryInventory']['Profile'] for shape in shapes} != {f'wwcp-poi-history-v{v}' for v in (1, 2, 3, 4)}:
        raise ValueError('All four archive profiles must be represented')
    for shape in shapes:
        full = groups[(shape, 'read-cbor')][0]
        for operation in OPERATIONS:
            run = groups[(shape, operation)][0]
            if any(run[field] != full[field] for field in INVENTORY):
                raise ValueError('Stage inventories differ')
            result = run['Samples'][0]['Result']
            if result['OutputBytes'] != run['RecoveryInventory']['InputCBORBytes'] or result['WrittenBytes']:
                raise ValueError('Unexpected input/output byte count')
            expected = run['ArchiveIdentity'] if operation == 'read-cbor-limits' else full['Samples'][0]['Result']['Identity']
            if result['Identity'] != expected:
                raise ValueError('Checked archive digest or restored head changed')
    return value, groups


def median(runs, field):
    return statistics.median(sample[field] for run in runs for sample in run['Samples'])


def build(before_path, after_path, input_path):
    before, old = load(before_path); after, new = load(after_path)
    for field in ('BenchmarkSourceSHA256', 'BenchmarkAssemblySHA256', 'DotnetSDK', 'InstalledRuntimes',
                  'OS', 'Cpu', 'LogicalProcessors', 'RuntimeTuning', 'RuntimeConfigSHA256', 'SharedDependencies', 'Method'):
        if before['Environment'][field] != after['Environment'][field]:
            raise ValueError('Harness, method or dependency changed: ' + field)
    if datetime.datetime.fromisoformat(before['Environment']['FinishedUTC']) > datetime.datetime.fromisoformat(after['Environment']['TimestampUTC']):
        raise ValueError('Formal before/after series overlap')
    previous = json.loads(Path(input_path).read_text(encoding='utf-8-sig'))
    if not previous['Complete']:
        raise ValueError('Incomplete preceding prepared-input report')
    prior = {run['Shape']['Name']: run for run in previous['Runs']}
    for key in old:
        first = old[key][0]; second = new[key][0]
        for field in INVENTORY:
            if first[field] != second[field] or first[field] != prior[key[0]][field]:
                raise ValueError('Prepared input or recovery inventory changed: ' + str(key) + ':' + field)
        if first['Samples'][0]['Result'] != second['Samples'][0]['Result']:
            raise ValueError('Exact checked/recovered result changed')
    lines = ['# Cooperative archive-parser cancellation measurements', '',
             f'Before `{Path(before_path).name}`; after `{Path(after_path).name}`. Six shapes/all four profiles, '
             'three operations, two fresh processes/three warmups/three samples per side: '
             '72 workers / 216 measured calls in total.', '',
             'The same new C# harness binary adds an uncancelled, cancellable token to the existing private-memory '
             'borrowed-stream recovery probe. Both sides use identical dependencies/runtime configuration and '
             'prepared archives, independently bound to '
             f'`{Path(input_path).name}`. Exact output bytes, static identities, heads, every retained branch state, '
             'original peers and fresh local runtime are checked outside measured calls.', '',
             'read-cbor-limits measures the existing local-budget scan with a default token. read-cbor measures '
             'ordinary span recovery with a default token. read-cbor-input-token measures complete synchronous '
             'borrowed-stream capture and recovery with a cancellable token that is never cancelled. It includes '
             'existing stream buffers/copies and token-scoped trust wrappers as well as new parser checks. '
             'The before build already contains input/trust cancellation; only the production library changes '
             'between sides. Comparisons between different operations cannot isolate parser-check costs.', '',
             'Workers run sequentially; no task build/test overlaps either formal series. The shared Windows '
             'desktop is not dedicated or affinity controlled. Stage medians cannot be added or subtracted '
             'as processing shares. These successful-read measurements establish no cancellation-latency bound.', '',
             '| Shape | Operation | Before MiB | After MiB | Allocation change | Before ms | After ms |',
             '| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |']
    for key in sorted(old):
        a = median(old[key], 'AllocatedBytes') / 1048576; b = median(new[key], 'AllocatedBytes') / 1048576
        lines.append(f'| {key[0]} | {key[1]} | {a:.4f} | {b:.4f} | {(b/a-1)*100:+.4f}% | '
                     f'{median(old[key], "ElapsedMilliseconds"):.3f} | {median(new[key], "ElapsedMilliseconds"):.3f} |')
    large = ('recover-batch-2048-peers-4', 'read-cbor')
    active = ('recover-batch-2048-peers-4', 'read-cbor-input-token')
    lines += ['', 'Timings are mixed. In the largest batch, ordinary span recovery changes '
              f'{median(old[large], "ElapsedMilliseconds"):.3f} -> {median(new[large], "ElapsedMilliseconds"):.3f} ms; '
              'active-token input recovery changes '
              f'{median(old[active], "ElapsedMilliseconds"):.3f} -> {median(new[active], "ElapsedMilliseconds"):.3f} ms. '
              'The measured increases remain part of the evidence. This shared-desktop comparison does '
              'not isolate their cause or establish a universal check overhead, speedup or latency bound.']
    lines += ['', '## Collected additional recovered-history memory', '',
              'The existing post-measurement collection holds one additional ordinary span-recovered history '
              'with source/input live on both sides. Process-heap deltas are approximate, include model/state/runtime '
              'overhead and establish no total-memory bound. No parsing token/observer or progress context is '
              'retained in returned histories.', '',
              '| Shape | Before median MiB | After median MiB |', '| --- | ---: | ---: |']
    for shape in sorted(row[0] for row in SHAPES):
        values = [statistics.median(run['ReplayRetention']['ManagedDeltaBytes'] for run in groups[(shape, 'read-cbor')]) / 1048576 for groups in (old, new)]
        lines.append(f'| {shape} | {values[0]:.4f} | {values[1]:.4f} |')
    lines += ['', 'Elapsed time, process CPU and 10 ms sampled peaks remain descriptive. Managed allocation '
              'measures the operation thread; setup, verification and disposal stay outside it. Individual '
              'Styx scalar/structured-key/SkipValue calls, immutable model construction, canonicalization, '
              'cryptography and bulk copies remain synchronous and cannot be interrupted internally by this '
              'package. Cancellation is observed around those calls and inside owned value/commit/receipt/replay '
              'loops. No hard byte, time, throughput or production-concurrency guarantee follows.', '']
    return '\n'.join(lines)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--before', required=True); parser.add_argument('--after', required=True)
    parser.add_argument('--inputs', required=True); parser.add_argument('--output', required=True)
    args = parser.parse_args(); output = Path(args.output)
    if output.exists():
        raise FileExistsError('Preserve existing evidence; use a fresh output path')
    output.write_text(build(args.before, args.after, args.inputs), encoding='utf-8', newline='\n')
    print('Matched inputs, exact outputs, harness/dependencies, all profiles and retention inventories verified.')
