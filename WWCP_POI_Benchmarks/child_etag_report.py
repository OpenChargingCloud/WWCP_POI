"""Check paired child-cache reports and write cold/warm/retention tables (stdlib only)."""

import argparse
import json
import statistics
from pathlib import Path

OPERATIONS = {
    'child-etags-json-cold', 'child-etags-json-warm',
    'child-etags-cbor-cold', 'child-etags-cbor-warm',
    'snapshot-json-document', 'snapshot-cbor',
    'archive-json', 'archive-json-stream', 'archive-cbor', 'archive-cbor-stream'
}


def load(path):
    report = json.loads(Path(path).read_text(encoding='utf-8-sig'))
    if not report['Complete']:
        raise ValueError('Incomplete report: ' + str(path))
    groups = {}
    env = report['Environment']
    for run in report['Runs']:
        key = (run['Shape']['Name'], run['Operation'])
        groups.setdefault(key, []).append(run)
    shapes = {key[0] for key in groups}
    if not shapes or {key[1] for key in groups} != OPERATIONS:
        raise ValueError('Missing or unexpected operations')
    for shape in shapes:
        inventory = None
        for operation in OPERATIONS:
            runs = groups[(shape, operation)]
            if len(runs) != env['ProcessesPerOperation']:
                raise ValueError('Incomplete worker group')
            identity = runs[0]['Samples'][0]['Result']
            for run in runs:
                current = tuple(json.dumps(run[field], sort_keys=True) for field in (
                    'Shape', 'Entities', 'RetainedCommits', 'Receipts', 'CatalogIds', 'StaticIdentity', 'ArchiveIdentity'))
                if inventory is None:
                    inventory = current
                if inventory != current or len(run['Samples']) != env['SamplesPerProcess']:
                    raise ValueError('Inventory or sample count changed')
                if any(sample['Result'] != identity for sample in run['Samples']):
                    raise ValueError('Nondeterministic output')
        for first, second in [('child-etags-json-cold', 'child-etags-json-warm'),
                              ('child-etags-json-warm', 'snapshot-json-document'),
                              ('child-etags-cbor-cold', 'child-etags-cbor-warm'),
                              ('child-etags-cbor-warm', 'snapshot-cbor'),
                              ('archive-json', 'archive-json-stream'), ('archive-cbor', 'archive-cbor-stream')]:
            if groups[(shape, first)][0]['Samples'][0]['Result'] != groups[(shape, second)][0]['Samples'][0]['Result']:
                raise ValueError('Cold/warm/complete/stream outputs disagree')
    return report, groups


def median(runs, field):
    return statistics.median(sample[field] for run in runs for sample in run['Samples'])


def build(before_path, after_path):
    before, first = load(before_path)
    after, second = load(after_path)
    if first.keys() != second.keys():
        raise ValueError('Workload sets changed')
    for field in ('BenchmarkSourceSHA256', 'Runtime', 'OS', 'Architecture', 'LogicalProcessors', 'ServerGC',
                  'GCLatency', 'RuntimeTuning', 'Cpu', 'BuildConfiguration', 'Dependencies',
                  'SamplesPerProcess', 'ProcessesPerOperation', 'WarmupsPerProcess'):
        if before['Environment'][field] != after['Environment'][field]:
            raise ValueError('Environment/source method changed: ' + field)
    for key in first:
        a, b = first[key][0], second[key][0]
        for field in ('Shape', 'Entities', 'RetainedCommits', 'Receipts', 'CatalogIds', 'StaticIdentity', 'ArchiveIdentity'):
            if a[field] != b[field]:
                raise ValueError('Before/after inventory changed')
        if a['Samples'][0]['Result'] != b['Samples'][0]['Result']:
            raise ValueError('Before/after bytes/digest changed')
        if key[1].startswith('child-etags-'):
            for run in first[key]:
                if any(run['ChildETagState'].values()) or any(run['ChildETagRetention']['State'].values()):
                    raise ValueError('Unexpected cache in before build')
            for run in second[key]:
                state = run['ChildETagState']
                retained = run['ChildETagRetention']['State']
                if not 0 < state['Entries'] <= 4096 or not 0 < state['PayloadBytes'] <= 1048576 or state['Rejected']:
                    raise ValueError('Unexpected cache bound or saturated performance fixture')
                if state['Entries'] != retained['Entries'] or state['PayloadBytes'] != retained['PayloadBytes']:
                    raise ValueError('Retention fixture differs from export fixture')
                if key[1].endswith('-cold'):
                    if state['Hits'] or state['Misses'] != state['Entries']:
                        raise ValueError('Cold sample inherited a child cache')
                elif state['Misses'] != state['Entries'] or state['Hits'] <= 0:
                    raise ValueError('Warm sample did not reuse child cache')
                if retained['Hits'] or retained['Misses'] != retained['Entries']:
                    raise ValueError('Retention fixture was not a first export')
    text = [
        '# Child ETag cache measurements', '',
        f'Inputs: `{Path(before_path).name}`, `{Path(after_path).name}`. Matching benchmark source, '
        'runtime/dependencies, graph/history inventories, and exact output byte counts/digests checked.', '',
        'Cold maps and root digests are prepared outside every sample. Warm calls share one prefilled '
        'context. Import and root hashing are excluded. Document consumption/byte verification occurs '
        'after measurement; complete CBOR and archive calls include output SHA-256.', '',
        'Before timings partly overlap local build/test work; no exclusive host or affinity. Time '
        'medians are descriptive. Retained memory is a separate approximate process-managed heap '
        'delta after full collection, not allocation or a total-memory cap. Logical payload excludes '
        'object headers, digest-pair arrays, dictionary capacity and locks.', ''
    ]
    for shape in sorted({key[0] for key in first}):
        text.extend([f'## {shape}', '',
                     '| Operation | Before MiB allocated | After MiB allocated | Change | Before median ms | After median ms |',
                     '| --- | ---: | ---: | ---: | ---: | ---: |'])
        for operation in sorted(OPERATIONS):
            old, new = first[(shape, operation)], second[(shape, operation)]
            a, b = median(old, 'AllocatedBytes'), median(new, 'AllocatedBytes')
            text.append(f'| {operation} | {a / 1048576:.4f} | {b / 1048576:.4f} | {(b/a-1)*100:+.1f}% '
                        f'| {median(old, "ElapsedMilliseconds"):.3f} | {median(new, "ElapsedMilliseconds"):.3f} |')
        retained = [run['ChildETagRetention'] for (name, operation), runs in second.items()
                    if name == shape and operation.startswith('child-etags-') for run in runs]
        state = retained[0]['State']
        if any(value['State'] != state for value in retained):
            raise ValueError('Retained counts differ across workers/formats')
        deltas = [value['ManagedDeltaBytes'] for value in retained]
        text.extend(['', f'Retained pairs: **{state["Entries"]}**; logical payload: **{state["PayloadBytes"]:,} bytes**. '
                     f'Post-collection managed delta over {len(deltas)} workers: median **{statistics.median(deltas)/1024:.2f} KiB**, '
                     f'range **{min(deltas)/1024:.2f}–{max(deltas)/1024:.2f} KiB**. No default-bound saturation.', ''])
    return '\n'.join(text)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--before', required=True)
    parser.add_argument('--after', required=True)
    parser.add_argument('--output', required=True)
    args = parser.parse_args()
    output = Path(args.output)
    if output.exists():
        raise FileExistsError(output)
    text = build(args.before, args.after)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(text + '\n', encoding='utf-8')
    print('Verified cache reports and saved', output)
