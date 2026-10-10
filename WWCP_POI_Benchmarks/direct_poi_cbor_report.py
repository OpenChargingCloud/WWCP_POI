"""Verify paired direct-POI-CBOR measurements with child caches in both builds (stdlib only)."""

import argparse
import statistics
from pathlib import Path
from child_etag_report import load, median, OPERATIONS


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
        if not key[1].startswith('child-etags-'):
            continue
        for runs in (first[key], second[key]):
            for run in runs:
                state = run['ChildETagState']
                retained = run['ChildETagRetention']['State']
                if not 0 < state['Entries'] <= 4096 or not 0 < state['PayloadBytes'] <= 1048576 or state['Rejected']:
                    raise ValueError('Unexpected cache bound or saturated fixture')
                if state['Entries'] != retained['Entries'] or state['PayloadBytes'] != retained['PayloadBytes']:
                    raise ValueError('Retention fixture differs from export fixture')
                if key[1].endswith('-cold'):
                    if state['Hits'] or state['Misses'] != state['Entries']:
                        raise ValueError('Cold sample inherited a cache')
                elif state['Misses'] != state['Entries'] or state['Hits'] <= 0:
                    raise ValueError('Warm sample did not reuse cache')
                if retained['Hits'] or retained['Misses'] != retained['Entries']:
                    raise ValueError('Retention fixture was not a first export')
        if a['ChildETagState'] != b['ChildETagState']:
            raise ValueError('Before/after cache statistics changed')
    text = ['# Direct POI CBOR measurements', '',
            f'Inputs: `{Path(before_path).name}`, `{Path(after_path).name}`. Same benchmark source, '
            'runtime/dependencies, graph/history inventories, exact outputs and bounded cache statistics checked.', '',
            'Cold maps/root hashes are prepared outside each call; warm calls reuse one prefilled context. '
            'Import/root hashing are excluded. Document consumption is after measurement; complete CBOR/archive '
            'calls include output SHA-256. No build or test ran alongside either benchmark report.', '',
            'No dedicated host or affinity; timings and process peaks are descriptive. Retention is an approximate '
            'post-collection heap delta while the same map/root remains alive, not an isolated cache size or '
            'a global memory bound. Negative deltas reflect collectible setup objects.', '']
    for shape in sorted({key[0] for key in first}):
        text.extend([f'## {shape}', '',
                     '| Operation | Before MiB allocated | After MiB allocated | Change | Before median ms | After median ms |',
                     '| --- | ---: | ---: | ---: | ---: | ---: |'])
        for operation in sorted(OPERATIONS):
            old, new = first[(shape, operation)], second[(shape, operation)]
            a, b = median(old, 'AllocatedBytes'), median(new, 'AllocatedBytes')
            text.append(f'| {operation} | {a / 1048576:.4f} | {b / 1048576:.4f} | {(b/a-1)*100:+.1f}% '
                        f'| {median(old, "ElapsedMilliseconds"):.3f} | {median(new, "ElapsedMilliseconds"):.3f} |')
        text.extend(['', '| Operation | Before managed peak MiB | After managed peak MiB | Before working-set peak MiB | After working-set peak MiB |',
                     '| --- | ---: | ---: | ---: | ---: |'])
        for operation in ('child-etags-cbor-cold', 'snapshot-cbor', 'archive-cbor-stream'):
            old, new = first[(shape, operation)], second[(shape, operation)]
            text.append(f'| {operation} | {median(old, "SampledManagedPeakBytes")/1048576:.2f} '
                        f'| {median(new, "SampledManagedPeakBytes")/1048576:.2f} '
                        f'| {median(old, "SampledWorkingSetPeakBytes")/1048576:.2f} '
                        f'| {median(new, "SampledWorkingSetPeakBytes")/1048576:.2f} |')
        for label, groups in [('Before', first), ('After', second)]:
            retained = [run['ChildETagRetention'] for (name, operation), runs in groups.items()
                        if name == shape and operation.startswith('child-etags-') for run in runs]
            state = retained[0]['State']
            if any(value['State'] != state for value in retained):
                raise ValueError('Retained counts differ across formats/workers')
            deltas = [value['ManagedDeltaBytes'] for value in retained]
            text.extend(['', f'{label}: **{state["Entries"]} pairs / {state["PayloadBytes"]:,} logical bytes**; '
                         f'collected managed delta median **{statistics.median(deltas)/1024:.2f} KiB**, '
                         f'range **{min(deltas)/1024:.2f}–{max(deltas)/1024:.2f} KiB** ({len(deltas)} workers).'])
        text.append('')
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
    print('Verified direct POI CBOR reports and saved', output)
