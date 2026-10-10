"""Validate the complete archive-recovery baseline and report stage/retention costs (stdlib only)."""
import argparse
import json
import statistics
from pathlib import Path

OPERATIONS = {'read-cbor-limits', 'read-cbor-tree', 'read-cbor-model', 'read-signatures', 'read-restore', 'read-cbor', 'read-json'}
SHAPES = {'recover-graph-128', 'recover-graph-512', 'recover-history-64', 'recover-pruned-4',
          'recover-boundary-16', 'recover-batch-1-peers-1', 'recover-batch-2048-peers-4'}
INVENTORY = ('Shape', 'Entities', 'RetainedCommits', 'Receipts', 'CatalogIds', 'StaticIdentity', 'ArchiveIdentity', 'RecoveryInventory')


def load(path):
    report = json.loads(Path(path).read_text(encoding='utf-8-sig'))
    env = report['Environment']
    if not report['Complete'] or env['Suite'] != 'recovery' or env['BuildConfiguration'] != 'Release':
        raise ValueError('A complete Release recovery report is required')
    if (env['ProcessesPerOperation'], env['WarmupsPerProcess'], env['SamplesPerProcess']) != (2, 5, 5):
        raise ValueError('Unexpected process/warmup/sample method')
    groups = {}
    for run in report['Runs']:
        groups.setdefault((run['Shape']['Name'], run['Operation']), []).append(run)
    if groups.keys() != {(shape, operation) for shape in SHAPES for operation in OPERATIONS}:
        raise ValueError('Missing/unexpected workload groups')
    profiles = set()
    for shape in SHAPES:
        first = groups[(shape, 'read-cbor')][0]
        inventory = first['RecoveryInventory']
        profiles.add(inventory['Profile'])
        head = first['Samples'][0]['Result']['Identity']
        for operation in OPERATIONS:
            runs = groups[(shape, operation)]
            if len(runs) != 2:
                raise ValueError('Missing worker')
            expected = runs[0]['Samples'][0]['Result']
            for run in runs:
                if any(run[field] != first[field] for field in INVENTORY) or len(run['Samples']) != 5:
                    raise ValueError('Inventory/sample count mismatch')
                if any(sample['Result'] != expected for sample in run['Samples']):
                    raise ValueError('Operation output changed')
                if operation in {'read-cbor', 'read-json', 'read-restore'} and expected['Identity'] != head:
                    raise ValueError('Recovered heads disagree')
                if operation in {'read-cbor-limits', 'read-cbor-tree'} and expected['Identity'] != run['ArchiveIdentity']:
                    raise ValueError('Scan/tree input identity changed')
                if operation == 'read-signatures' and expected['Identity'] != f'peers:{inventory["CommitPeers"] + inventory["BatchPeers"]}:{head}':
                    raise ValueError('Signature inventory changed')
                if operation == 'read-cbor-model' and not expected['Identity'].startswith('model:sha256:hex:'):
                    raise ValueError('Missing exact model fingerprint')
                size = inventory['InputCBORBytes'] if operation in {'read-cbor', 'read-cbor-limits'} else inventory['InputJSONBytes'] if operation == 'read-json' else 0
                if expected['OutputBytes'] != size or expected['WrittenBytes']:
                    raise ValueError('Unexpected input/output byte count')
                retention = run['ReplayRetention']
                if operation == 'read-cbor':
                    if retention is None or retention['RetainedCommits'] != run['RetainedCommits'] or retention['RetainedSnapshots'] != inventory['Snapshots']:
                        raise ValueError('Missing collected replay retention probe')
                elif retention is not None:
                    raise ValueError('Retention probe on an unexpected operation')
        if shape.startswith('recover-batch-'):
            count, peers = (1, 1) if shape.endswith('1-peers-1') else (2048, 4)
            if inventory['Profile'] != 'wwcp-poi-history-v1' or first['RetainedCommits'] != 2 or first['Receipts'] or first['CatalogIds']:
                raise ValueError('Unexpected signed-batch history')
            if (inventory['CommitPeers'], inventory['BatchPeers'], inventory['MaxBatchOperations'], inventory['Snapshots']) != (2 * peers, peers, count, 0):
                raise ValueError('Unexpected signed-batch peer/operation inventory')
        elif shape == 'recover-boundary-16' and (inventory['Profile'] != 'wwcp-poi-history-v3' or first['Receipts'] or first['CatalogIds']):
            raise ValueError('Unexpected snapshot boundary')
        elif shape == 'recover-pruned-4' and (inventory['Profile'] != 'wwcp-poi-history-v4' or first['Receipts'] != 4 or not first['CatalogIds']):
            raise ValueError('Unexpected retention history')
    if profiles != {f'wwcp-poi-history-v{index}' for index in (1, 2, 3, 4)}:
        raise ValueError('All four archive profiles must be represented')
    return report, groups


def bind_inputs(groups, scaling_path, batch_path):
    previous = {}
    for path in [scaling_path, batch_path]:
        report = json.loads(Path(path).read_text(encoding='utf-8-sig'))
        if not report['Complete']:
            raise ValueError('Incomplete previous input control')
        for run in report['Runs']:
            if run['Operation'] in {'archive-cbor', 'batch-archive-cbor'}:
                previous[run['Shape']['Name']] = run
    bindings = []
    for shape in sorted(SHAPES - {'recover-boundary-16'}):
        name = shape.replace('recover-batch-', 'archive-batch-') if shape.startswith('recover-batch-') else shape.removeprefix('recover-')
        prior = previous[name]; current = groups[(shape, 'read-cbor')][0]
        for field in ['Entities', 'RetainedCommits', 'Receipts', 'CatalogIds', 'StaticIdentity', 'ArchiveIdentity']:
            if prior[field] != current[field]:
                raise ValueError('Prepared archive input changed: ' + shape + ':' + field)
        if prior['Samples'][0]['Result']['OutputBytes'] != current['RecoveryInventory']['InputCBORBytes']:
            raise ValueError('Prepared archive byte count changed')
        bindings.append((shape, name))
    return bindings


def median(runs, field):
    return statistics.median(sample[field] for run in runs for sample in run['Samples'])


def build(path, scaling, batches):
    report, groups = load(path)
    bindings = bind_inputs(groups, scaling, batches)
    text = ['# Archive decoder and replay baseline', '',
            f'Input: `{Path(path).name}`. Seven shapes/all four profiles, seven operations, '
            'two fresh processes/five warmups/five samples: 98 workers / 490 measured calls.', '',
            'Every output and repeated process inventory agrees. Six existing graph/history/pruned/signed-batch '
            'CBOR inputs match the preceding export reports exactly in digest, byte count and static/history inventory. '
            'The boundary-only fixture is new. This is a fresh baseline, not a before/after performance comparison.', '',
            'Input construction, signing, production-stage delegate binding and prepared stage inputs are setup. '
            'Complete recovery includes the actual production scanner, document/model decoding, trusted signature callbacks '
            'and replay. Actual restored histories remain live until verification/disposal outside measurement. '
            'Tree re-encoding, exact model fingerprints, archive-byte/peer comparison, every branch state and fresh/local '
            'runtime checks occur after each call. Prior results are released before the next measurement.', '',
            'Model-only envelope composition calls the actual private field/profile/model parsers; it is benchmark code. '
            'Complete profiles decode their commits eagerly; boundary production paths decode suffix commits lazily during '
            'restore. The model-only stage makes that suffix eager. Restore-from-model calls the actual production restore '
            'method and includes its repeated trust/state/ancestry checks. Signature-only verifies every equal peer once '
            'without applying state. Stage medians cannot be added, subtracted or interpreted as shares of complete recovery.', '',
            'OutputBytes is the consumed input size for complete JSON/CBOR recovery and CBOR scan, zero for object-only '
            'stages. No reader operation emits wire bytes. The result binds input CBOR, exact decoded model, peer count or '
            'recovered head as appropriate.', '',
            'No build/test from this task overlaps the formal series. The host is not dedicated or affinity controlled. '
            'Elapsed times and 10 ms sampled peaks are descriptive; setup inputs/verification can affect lifetime memory. '
            'Allocation counts cover the operation thread; CPU counts cover the process including its sampling thread. '
            'Largest sampled managed/working-set peaks are not exact operation peaks or production capacity.', '',
            '| Shape | Profile | CBOR bytes | JSON UTF-8 bytes | Commits | Snapshots | Commit/batch peers | Largest batch |',
            '| --- | --- | ---: | ---: | ---: | ---: | --- | ---: |']
    for shape in sorted(SHAPES):
        run = groups[(shape, 'read-cbor')][0]; value = run['RecoveryInventory']
        text.append(f'| {shape} | {value["Profile"]} | {value["InputCBORBytes"]} | {value["InputJSONBytes"]} | {run["RetainedCommits"]} | '
                    f'{value["Snapshots"]} | {value["CommitPeers"]}/{value["BatchPeers"]} | {value["MaxBatchOperations"]} |')
    text += ['', '| Shape | Operation | Median ms | Min–max ms | Median process CPU ms | Allocated MiB | Largest sampled managed MiB | Largest sampled working set MiB |',
             '| --- | --- | ---: | --- | ---: | ---: | ---: | ---: |']
    for key in sorted(groups):
        runs = groups[key]; samples = [sample for run in runs for sample in run['Samples']]
        text.append(f'| {key[0]} | {key[1]} | {median(runs,"ElapsedMilliseconds"):.3f} | '
                    f'{min(s["ElapsedMilliseconds"] for s in samples):.3f}–{max(s["ElapsedMilliseconds"] for s in samples):.3f} | '
                    f'{median(runs,"CpuMilliseconds"):.3f} | {median(runs,"AllocatedBytes")/1048576:.4f} | {max(s["SampledManagedPeakBytes"] for s in samples)/1048576:.2f} | '
                    f'{max(s["SampledWorkingSetPeakBytes"] for s in samples)/1048576:.2f} |')
    text += ['', '## Collected recovery retention', '',
             'One post-measurement probe per read-cbor worker collects before/after holding one additional recovered '
             'history, input/source held on both sides. It checks every branch state, inventory and fresh head runtime '
             'without re-exporting an archive. GC.KeepAlive preserves the recovered/source histories through collection. '
             'These approximate process-heap deltas include retained model/state/runtime objects and overhead; they are '
             'not logical cache payload sizes or total-memory bounds. Other objects can become collectible, including '
             'negative deltas on small inputs.', '', '| Shape | Median additional managed MiB | Min–max MiB |', '| --- | ---: | --- |']
    for shape in sorted(SHAPES):
        values = [run['ReplayRetention']['ManagedDeltaBytes'] / 1048576 for run in groups[(shape, 'read-cbor')]]
        text.append(f'| {shape} | {statistics.median(values):.4f} | {min(values):.4f}–{max(values):.4f} |')
    text += ['', '## Exact preceding input bindings', '', '| Current shape | Preceding export shape |', '| --- | --- |']
    text += [f'| {current} | {prior} |' for current, prior in bindings]
    return '\n'.join(text) + '\n'


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ['input', 'scaling-control', 'batch-control', 'output']:
        parser.add_argument('--' + name, required=True)
    args = parser.parse_args(); output = Path(args.output)
    if output.exists(): raise FileExistsError(output)
    result = build(args.input, args.scaling_control, args.batch_control)
    output.parent.mkdir(parents=True, exist_ok=True); output.write_text(result, encoding='utf-8')
    print('Verified all recovery stages/inventories and exact preceding archive inputs; saved', output)
