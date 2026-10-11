"""Validate same-build span/private-memory/mapped-spool CBOR recovery measurements (stdlib only)."""
import argparse
import hashlib
import json
from pathlib import Path
from archive_recovery_report import SHAPES, INVENTORY, median

OPERATIONS = ['read-cbor', 'read-cbor-input-memory', 'read-cbor-input-spool']


def build(path, previous_path):
    report = json.loads(Path(path).read_text(encoding='utf-8-sig'))
    previous = json.loads(Path(previous_path).read_text(encoding='utf-8-sig'))
    env = report['Environment']
    if not report['Complete'] or not previous['Complete'] or env['Suite'] != 'recovery' or env['BuildConfiguration'] != 'Release':
        raise ValueError('Complete Release reports are required')
    if (env['ProcessesPerOperation'], env['WarmupsPerProcess'], env['SamplesPerProcess']) != (2, 5, 5):
        raise ValueError('Unexpected process/warmup/sample method')
    groups = {}
    for run in report['Runs']: groups.setdefault((run['Shape']['Name'], run['Operation']), []).append(run)
    if groups.keys() != {(shape, operation) for shape in SHAPES for operation in OPERATIONS}:
        raise ValueError('Missing/unexpected groups')
    controls = {run['Shape']['Name']: run for run in previous['Runs'] if run['Operation'] == 'read-cbor'}
    if controls.keys() != SHAPES: raise ValueError('Missing preceding inputs')
    profiles = set()
    for shape in SHAPES:
        first = groups[(shape, 'read-cbor')][0]
        profiles.add(first['RecoveryInventory']['Profile'])
        if any(first[field] != controls[shape][field] for field in INVENTORY):
            raise ValueError('Preceding archive input/inventory changed')
        if first['Samples'][0]['Result'] != controls[shape]['Samples'][0]['Result']:
            raise ValueError('Preceding recovered output changed')
        for operation in OPERATIONS:
            runs = groups[(shape, operation)]
            if len(runs) != 2 or any(len(run['Samples']) != 5 for run in runs):
                raise ValueError('Worker/sample counts differ')
            for run in runs:
                if any(run[field] != first[field] for field in INVENTORY): raise ValueError('Input/inventory differs')
                if any(sample['Result'] != first['Samples'][0]['Result'] for sample in run['Samples']):
                    raise ValueError('Recovered heads or consumed bytes differ')
                if (operation == 'read-cbor') != (run['ReplayRetention'] is not None):
                    raise ValueError('Unexpected retention probe')
    if profiles != {'wwcp-poi-history-v' + str(value) for value in range(1, 5)}: raise ValueError('Missing archive profiles')
    text = ['# Borrowed CBOR input measurements', '',
        f'Raw report: `{Path(path).name}`. Exact preceding input control: `{Path(previous_path).name}` '
        f'(SHA-256 `{hashlib.sha256(Path(previous_path).read_bytes()).hexdigest()}`).', '',
        '42 fresh workers / 210 samples, seven shapes/all four profiles, two processes/five warmups/five calls. '
        'All three operations use one new production/harness build and identical prepared input bytes, '
        'state/commit/receipt/peer inventories and recovered heads/results. Span control is freshly measured; '
        'the preceding report only binds exact inputs/results, not a before/after speedup.', '',
        'Memory capture allows the entire archive under its byte budget. File capture forces a temporary '
        'spool at zero threshold, shared maintenance lease, async-independent synchronous reads, flush and '
        'read-only mapping. Both sources are borrowed non-seekable wrappers returning at most 16 KiB per read. '
        'Each source wrapper is created inside measurement; prepared bytes/options are setup. '
        'File capture includes its actual I/O/map/handle cleanup costs. Exact archive/branch/peer/runtime '
        'verification is outside measurement.', '',
        'No task build/test overlaps the formal run. Host is shared and not affinity controlled. '
        'Operation-thread managed allocation differs from 10 ms sampled managed/working-set peaks and '
        'file-backed resident pages. The fixture retains its original input bytes in all operations. '
        'These are local source/disk recovery costs, not WAN, cancellation latency or total-memory bounds.', '',
        '| Shape | Operation | Median ms | Process CPU ms | Allocated MiB | Allocation vs span | Min–max ms | Largest sampled managed MiB | Largest sampled working set MiB |',
        '| --- | --- | ---: | ---: | ---: | ---: | --- | ---: | ---: |']
    for shape in sorted(SHAPES):
        baseline = median(groups[(shape, 'read-cbor')], 'AllocatedBytes')
        for operation in OPERATIONS:
            runs = groups[(shape, operation)]; allocated = median(runs, 'AllocatedBytes')
            samples = [sample for run in runs for sample in run['Samples']]
            text.append(f'| {shape} | {operation} | {median(runs,"ElapsedMilliseconds"):.2f} | '
                f'{median(runs,"CpuMilliseconds"):.2f} | {allocated/1048576:.4f} | {100*(allocated/baseline-1):+.2f}% | '
                f'{min(sample["ElapsedMilliseconds"] for sample in samples):.2f}–{max(sample["ElapsedMilliseconds"] for sample in samples):.2f} | '
                f'{max(sample["SampledManagedPeakBytes"] for sample in samples)/1048576:.2f} | '
                f'{max(sample["SampledWorkingSetPeakBytes"] for sample in samples)/1048576:.2f} |')
    return '\n'.join(text) + '\n'


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ['input', 'previous', 'output']: parser.add_argument('--' + name, required=True)
    args = parser.parse_args(); destination = Path(args.output)
    if destination.exists(): raise FileExistsError(destination)
    text = build(args.input, args.previous)
    destination.write_text(text, encoding='utf-8')
    print('Validated 42 workers/210 samples, same-build operation controls and exact preceding inputs/results; saved', destination)
