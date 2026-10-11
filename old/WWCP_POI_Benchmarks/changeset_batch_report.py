"""Verify complete before/after signed-batch reports and summarize allocation/time/peaks (stdlib only)."""
import argparse
import json
import statistics
from pathlib import Path


def load(path):
    report = json.loads(Path(path).read_text(encoding='utf-8-sig'))
    if not report['Complete']:
        raise ValueError('Incomplete report')
    groups = {}
    for run in report['Runs']:
        groups.setdefault((run['Shape']['Name'], run['Operation']), []).append(run)
    expected = {(f'batch-{count}-peers-{peers}', operation) for count in (1, 64, 512, 2048)
                for peers in (1, 4) for operation in ('changeset-batch-json', 'changeset-batch-cbor')}
    if groups.keys() != expected:
        raise ValueError('Unexpected batch workloads')
    for key, runs in groups.items():
        if len(runs) != report['Environment']['ProcessesPerOperation']:
            raise ValueError('Missing worker')
        result = runs[0]['Samples'][0]['Result']
        for run in runs:
            if len(run['Samples']) != report['Environment']['SamplesPerProcess'] or any(sample['Result'] != result for sample in run['Samples']):
                raise ValueError('Missing sample or nondeterministic output')
            for field in ('Shape', 'Entities', 'RetainedCommits', 'Receipts', 'CatalogIds', 'StaticIdentity', 'ArchiveIdentity'):
                if run[field] != runs[0][field]:
                    raise ValueError('Inventory mismatch')
        shape = runs[0]['Shape']
        if shape['EVSEs'] != min(shape['Changes'], 512) or shape['Branches'] not in (1, 4):
            raise ValueError('Unexpected fixture size')
        if any(runs[0][name] for name in ('RetainedCommits', 'Receipts', 'CatalogIds')):
            raise ValueError('Unexpected history in batch fixture')
    return report, groups


def median(runs, field):
    return statistics.median(sample[field] for run in runs for sample in run['Samples'])


def build(before_path, after_path):
    before, first = load(before_path)
    after, second = load(after_path)
    for field in ('BenchmarkSourceSHA256', 'Runtime', 'OS', 'Architecture', 'LogicalProcessors', 'ServerGC',
                  'GCLatency', 'RuntimeTuning', 'Cpu', 'BuildConfiguration', 'Dependencies',
                  'SamplesPerProcess', 'ProcessesPerOperation', 'WarmupsPerProcess'):
        if before['Environment'][field] != after['Environment'][field]:
            raise ValueError('Environment/source method changed: ' + field)
    for key in first:
        a, b = first[key][0], second[key][0]
        for field in ('Shape', 'Entities', 'RetainedCommits', 'Receipts', 'CatalogIds', 'StaticIdentity', 'ArchiveIdentity'):
            if a[field] != b[field]:
                raise ValueError('Before/after inventory mismatch')
        if a['Samples'][0]['Result'] != b['Samples'][0]['Result']:
            raise ValueError('Before/after exact bytes/digest mismatch')
    text = ['# Direct ChangeSet CBOR measurements', '',
            f'Inputs: `{Path(before_path).name}`, `{Path(after_path).name}`. Complete matched groups, '
            'same harness/runtime/dependencies, exact bytes/digests and static inventories checked.', '',
            'Four ordered batch sizes, one/four equal Ed25519 peers, two fresh processes, five warmups '
            'and five measured samples: 32 workers / 160 samples per report. Construction, result '
            'preparation, signing, independent previous-tree byte checks, signature verification and '
            'result application are outside measurement. Complete outputs include SHA-256.', '',
            'JSON is complete canonical batch JSON; ordinary serializer dictionary order varies across '
            'processes. ArchiveIdentity binds complete signed batch CBOR here; there is no retained history. '
            'The 2,048-operation fixture repeats the 512-target round four times. These are synthetic '
            'property/custom-data batches, not every domain operation or production capacity evidence.', '',
            'No task build/test overlaps these reports. No host affinity/dedicated host; timings/CPU '
            'and sampled memory are descriptive. Peaks include prepared inputs, reference trees/results '
            'and the preceding result; brief peaks can be missed.', '',
            '| Batch size | Peers | Operation | Before MiB | After MiB | Allocation change | Before ms | After ms | Before managed peak MiB | After managed peak MiB |',
            '| ---: | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |']
    for count in (1, 64, 512, 2048):
        for peers in (1, 4):
            name = f'batch-{count}-peers-{peers}'
            for operation in ('changeset-batch-json', 'changeset-batch-cbor'):
                a, b = first[(name, operation)], second[(name, operation)]
                old, new = median(a, 'AllocatedBytes'), median(b, 'AllocatedBytes')
                text.append(f'| {count} | {peers} | {operation} | {old/1048576:.4f} | {new/1048576:.4f} '
                            f'| {(new/old-1)*100:+.1f}% | {median(a,"ElapsedMilliseconds"):.3f} '
                            f'| {median(b,"ElapsedMilliseconds"):.3f} | {median(a,"SampledManagedPeakBytes")/1048576:.2f} '
                            f'| {median(b,"SampledManagedPeakBytes")/1048576:.2f} |')
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
    print('Verified signed batch reports and saved', output)
