"""Check paired direct archive ChangeSet reports and summarize complete output costs (stdlib only)."""
import argparse
import json
import statistics
from pathlib import Path

SCALING = {'archive-json', 'archive-json-stream', 'archive-cbor', 'archive-cbor-stream', 'snapshot-cbor', 'bootstrap-export'}
BATCHES = {'batch-archive-cbor', 'batch-archive-cbor-stream', 'batch-archive-control-json'}
FIELDS = ('Shape', 'Entities', 'RetainedCommits', 'Receipts', 'CatalogIds', 'StaticIdentity', 'ArchiveIdentity')


def load(path):
    report = json.loads(Path(path).read_text(encoding='utf-8-sig'))
    if not report['Complete']:
        raise ValueError('Incomplete report')
    suite = report['Environment']['Suite']
    if suite == 'scaling':
        shapes = {'graph-128', 'graph-512', 'history-64', 'pruned-4'}
        operations = SCALING
        pairs = [('archive-cbor', 'archive-cbor-stream'), ('archive-json', 'archive-json-stream')]
    elif suite == 'batcharchives':
        shapes = {f'archive-batch-{count}-peers-{peers}' for count in (1, 64, 512, 2048) for peers in (1, 4)}
        operations = BATCHES
        pairs = [('batch-archive-cbor', 'batch-archive-cbor-stream')]
    else:
        raise ValueError('Unexpected suite')
    env = report['Environment']
    if (env['ProcessesPerOperation'], env['WarmupsPerProcess'], env['SamplesPerProcess']) != (2, 5, 5):
        raise ValueError('Unexpected process/warmup/sample method')
    groups = {}
    for run in report['Runs']:
        groups.setdefault((run['Shape']['Name'], run['Operation']), []).append(run)
    if groups.keys() != {(shape, op) for shape in shapes for op in operations}:
        raise ValueError('Missing/unexpected workloads')
    for shape in shapes:
        inventory = None
        for operation in operations:
            runs = groups[(shape, operation)]
            if len(runs) != 2:
                raise ValueError('Missing worker')
            result = runs[0]['Samples'][0]['Result']
            for run in runs:
                current = tuple(json.dumps(run[field], sort_keys=True) for field in FIELDS)
                if inventory is None:
                    inventory = current
                if current != inventory or len(run['Samples']) != 5 or any(s['Result'] != result for s in run['Samples']):
                    raise ValueError('Inventory/sample/identity mismatch')
                if suite == 'batcharchives':
                    s = run['Shape']
                    if s['EVSEs'] != min(s['Changes'], 512) or s['Changes'] not in (1, 64, 512, 2048) or s['Branches'] not in (1, 4):
                        raise ValueError('Unexpected signed-batch shape')
                    if run['RetainedCommits'] != 2 or run['Receipts'] or run['CatalogIds']:
                        raise ValueError('Unexpected signed-batch history inventory')
        for first, second in pairs:
            if groups[(shape, first)][0]['Samples'][0]['Result'] != groups[(shape, second)][0]['Samples'][0]['Result']:
                raise ValueError('Buffered/streamed output mismatch')
    return report, groups


def median(runs, field):
    return statistics.median(s[field] for run in runs for s in run['Samples'])


def build(before_path, after_path):
    before, first = load(before_path)
    after, second = load(after_path)
    for field in ('Suite', 'BenchmarkSourceSHA256', 'Runtime', 'OS', 'Architecture', 'LogicalProcessors', 'ServerGC',
                  'GCLatency', 'RuntimeTuning', 'Cpu', 'BuildConfiguration', 'Dependencies',
                  'SamplesPerProcess', 'ProcessesPerOperation', 'WarmupsPerProcess'):
        if before['Environment'][field] != after['Environment'][field]:
            raise ValueError('Environment/source method changed: ' + field)
    if first.keys() != second.keys():
        raise ValueError('Before/after workload set changed')
    for key in first:
        a, b = first[key][0], second[key][0]
        if any(a[field] != b[field] for field in FIELDS) or a['Samples'][0]['Result'] != b['Samples'][0]['Result']:
            raise ValueError('Before/after inventory/output changed')
    text = ['# Direct archive ChangeSet measurements', '',
            f'Inputs: `{Path(before_path).name}`, `{Path(after_path).name}`. Exact workload groups, '
            'same harness/runtime/dependencies, all outputs/counts/digests and static/history inventories checked.', '',
            'Each report has 48 fresh workers / 240 samples, with two processes/five warmups/five samples. '
            'Complete output/SHA-256 is measured. Construction, signing, recovery and validation are setup. '
            'The borrowed digest sink retains no complete output.', '',
            'Scaling uses prepared graph/history/pruned fixtures and six operations, including buffered/stream '
            'JSON controls, snapshot CBOR and frozen bootstrap. This series adds no cold/warm snapshot probe.', '',
            'Batch archives retain a signed checkpoint and one genuinely applied 1/64/512/2,048-operation '
            'batch with one/four equal Ed25519 peers on the batch and both commits. The original standalone '
            'batch probe checks previous-tree bytes, signing preimages and result application in setup. '
            'Archive recovery checks head/state and peer counts. The canonical JSON control measures the '
            'complete signed batch, not JSON archive output. The largest fixture repeats 512 targets four times.', '',
            'No build/test from this benchmark task overlaps either series. The host is not dedicated; '
            'timings and sampled peaks remain descriptive, and setup/preceding results can affect memory. '
            'These synthetic shapes do not establish every operation, cold replay or production capacity.', '',
            '| Shape | Operation | Before MiB | After MiB | Allocation change | Before ms | After ms | Before managed peak MiB | After managed peak MiB |',
            '| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |']
    for key in sorted(first):
        a, b = first[key], second[key]
        old, new = median(a, 'AllocatedBytes'), median(b, 'AllocatedBytes')
        text.append(f'| {key[0]} | {key[1]} | {old/1048576:.4f} | {new/1048576:.4f} | {(new/old-1)*100:+.1f}% '
                    f'| {median(a,"ElapsedMilliseconds"):.3f} | {median(b,"ElapsedMilliseconds"):.3f} '
                    f'| {median(a,"SampledManagedPeakBytes")/1048576:.2f} | {median(b,"SampledManagedPeakBytes")/1048576:.2f} |')
    return '\n'.join(text) + '\n'


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('before', 'after', 'output'):
        parser.add_argument('--' + name, required=True)
    args = parser.parse_args()
    output = Path(args.output)
    if output.exists():
        raise FileExistsError(output)
    value = build(args.before, args.after)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(value, encoding='utf-8')
    print('Verified complete direct archive ChangeSet reports and saved', output)
