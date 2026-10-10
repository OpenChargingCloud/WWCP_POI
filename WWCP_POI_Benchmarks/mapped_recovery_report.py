"""Validate mapped-file controls and paired cold/bootstrap recovery measurements (stdlib only)."""
import argparse
import json
from pathlib import Path
from archive_recovery_report import SHAPES, INVENTORY, median


def groups(report):
    if not report['Complete']:
        raise ValueError('Incomplete measurements')
    env = report['Environment']
    if (env['ProcessesPerOperation'], env['WarmupsPerProcess'], env['SamplesPerProcess']) != (2, 3, 3):
        raise ValueError('Unexpected process/warmup/sample method')
    result = {}
    for run in report['Runs']:
        result.setdefault((run['Shape']['Name'], run['Operation']), []).append(run)
    for runs in result.values():
        if len(runs) != 2 or any(len(run['Samples']) != 3 for run in runs):
            raise ValueError('Missing workers or samples')
        first = runs[0]
        for run in runs:
            for key in ['Shape', 'Entities', 'RetainedCommits', 'Receipts', 'CatalogIds', 'StaticIdentity', 'ArchiveIdentity']:
                if run[key] != first[key]:
                    raise ValueError('Input inventory changed')
            if any(sample['Result'] != first['Samples'][0]['Result'] for sample in run['Samples']):
                raise ValueError('Recovered result changed')
    return result


def build(files, previous, before, after):
    controls = groups(files)
    operations = ['read-cbor-file-array', 'read-cbor-file-mapped']
    if controls.keys() != {(shape, operation) for shape in SHAPES for operation in operations}:
        raise ValueError('Unexpected file-control groups')
    original = {run['Shape']['Name']: run for run in previous['Runs'] if run['Operation'] == 'read-cbor'}
    profiles = set()
    for shape in SHAPES:
        first = controls[(shape, operations[0])][0]
        profiles.add(first['RecoveryInventory']['Profile'])
        for run in controls[(shape, operations[0])] + controls[(shape, operations[1])]:
            if any(run[field] != first[field] or run[field] != original[shape][field] for field in INVENTORY):
                raise ValueError('Exact prepared archive/inventory differs')
            if any(sample['Result'] != original[shape]['Samples'][0]['Result'] for sample in run['Samples']):
                raise ValueError('Exact preceding result differs')
    if profiles != {'wwcp-poi-history-v' + str(value) for value in range(1, 5)}:
        raise ValueError('Missing archive profile')
    old, new = groups(before), groups(after)
    expected = {(shape, operation) for shape in ['integrated-128', 'integrated-512']
                for operation in ['cold-read', 'bootstrap-transfer']}
    if old.keys() != new.keys() or new.keys() != expected:
        raise ValueError('Unexpected paired integration groups')
    for key in old:
        left, right = old[key][0], new[key][0]
        for field in ['Shape', 'Entities', 'RetainedCommits', 'Receipts', 'CatalogIds', 'StaticIdentity', 'ArchiveIdentity']:
            if left[field] != right[field]:
                raise ValueError('Before/after input binding differs')
        if left['Samples'][0]['Result'] != right['Samples'][0]['Result']:
            raise ValueError('Before/after result differs')
    lines = ['# Mapped recovery measurements', '',
        '44 fresh workers / 132 samples. Each group uses two processes, three warmups and three measured calls.', '',
        'File controls use the same new build and seven prepared archives/all four profiles. The array control '
        'reproduces the preceding Open input algorithm (writer lease, known-length allocation, exact read/EOF probe, '
        'span replay). The mapped operation calls production Open. Both retain the writer lease and recovered '
        'history until exact branch/archive/peer/runtime verification and disposal outside measurement. '
        'Prepared disk input and source bytes are setup; file opening, capture/mapping and replay are measured. '
        'Original bytes/inventories/results also bind to stream-input-recovery.json; its old timings are not a speedup control.', '',
        'The paired cold/bootstrap runs use the preceding preserved production/harness binaries and newly built '
        'binaries. Workload.cs and its measured operations are unchanged. The new harness only adds independent '
        'file operations. Prepared static/archive identities, inventories and every output result match. '
        'Bootstrap includes durable chunk transfer and default one-MiB capture, with staging cleanup outside '
        'measurement; cold retrieval includes digest/current trust/receipt verification. Source/staging payload '
        'counts do not account for total physical input-spool I/O.', '',
        'No task build/test overlaps measurement. Workers run sequentially on a shared desktop without affinity '
        'control. Timings and ten-ms sampled peaks are descriptive; operation-thread managed allocations differ '
        'from mapped resident pages, OS cache, total disk use and retained branch/model memory. '
        'Neither these samples nor byte limits establish constant total memory or cancellation latency.', '',
        '| Shape | Input | Median ms | CPU ms | Allocated MiB | Allocation vs array | Largest sampled managed MiB | Largest sampled working set MiB |',
        '| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |']
    for shape in sorted(SHAPES):
        base = median(controls[(shape, operations[0])], 'AllocatedBytes')
        for operation in operations:
            runs = controls[(shape, operation)]; samples = [s for run in runs for s in run['Samples']]
            allocation = median(runs, 'AllocatedBytes')
            lines.append(f'| {shape} | {operation} | {median(runs,"ElapsedMilliseconds"):.2f} | '
                f'{median(runs,"CpuMilliseconds"):.2f} | {allocation/1048576:.4f} | {100*(allocation/base-1):+.3f}% | '
                f'{max(s["SampledManagedPeakBytes"] for s in samples)/1048576:.2f} | '
                f'{max(s["SampledWorkingSetPeakBytes"] for s in samples)/1048576:.2f} |')
    lines += ['', '| Shape | Operation | Before MiB | After MiB | Allocation change | Before ms | After ms |',
        '| --- | --- | ---: | ---: | ---: | ---: | ---: |']
    for key in sorted(old):
        a, b = median(old[key], 'AllocatedBytes'), median(new[key], 'AllocatedBytes')
        lines.append(f'| {key[0]} | {key[1]} | {a/1048576:.4f} | {b/1048576:.4f} | {100*(b/a-1):+.3f}% | '
            f'{median(old[key],"ElapsedMilliseconds"):.2f} | {median(new[key],"ElapsedMilliseconds"):.2f} |')
    return '\n'.join(lines) + '\n'


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ['files', 'previous', 'before', 'after', 'output']:
        parser.add_argument('--' + name, required=True)
    args = parser.parse_args(); destination = Path(args.output)
    if destination.exists():
        raise FileExistsError(destination)
    inputs = [json.loads(Path(getattr(args, name)).read_text(encoding='utf-8-sig'))
              for name in ['files', 'previous', 'before', 'after']]
    destination.write_text(build(*inputs), encoding='utf-8')
    print('Validated 44 workers/132 samples, exact file controls and paired integration results:', destination)
