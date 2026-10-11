"""Validate matched root/head snapshot reconstruction measurements (stdlib only)."""
import argparse
import json
import statistics
from pathlib import Path

SHAPES = {'recover-graph-128', 'recover-graph-512', 'recover-history-64', 'recover-pruned-4',
          'recover-boundary-16', 'recover-batch-1-peers-1', 'recover-batch-2048-peers-4'}
OPERATIONS = {'read-cbor-model', 'read-signatures', 'read-restore', 'read-cbor'}
INVENTORY = ('Shape', 'Entities', 'RetainedCommits', 'Receipts', 'CatalogIds', 'StaticIdentity',
             'ArchiveIdentity', 'RecoveryInventory')


def load(path):
    value = json.loads(Path(path).read_text(encoding='utf-8-sig'))
    env = value['Environment']
    if not value['Complete'] or env['Suite'] != 'recovery' or env['BuildConfiguration'] != 'Release':
        raise ValueError('A complete Release recovery report is required')
    if (env['ProcessesPerOperation'], env['WarmupsPerProcess'], env['SamplesPerProcess']) != (2, 3, 3):
        raise ValueError('Unexpected worker/warmup/sample method')
    groups = {}
    for run in value['Runs']:
        groups.setdefault((run['Shape']['Name'], run['Operation']), []).append(run)
    if set(groups) != {(shape, operation) for shape in SHAPES for operation in OPERATIONS}:
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
            if key[1] == 'read-cbor':
                retention = run['ReplayRetention']
                if retention['RetainedCommits'] != run['RetainedCommits'] or retention['RetainedSnapshots'] != run['RecoveryInventory']['Snapshots']:
                    raise ValueError('Collected retention inventory changed')
            elif run['ReplayRetention'] is not None:
                raise ValueError('Unexpected retention probe')
    if {groups[(shape, 'read-cbor')][0]['RecoveryInventory']['Profile'] for shape in SHAPES} != {f'wwcp-poi-history-v{v}' for v in (1, 2, 3, 4)}:
        raise ValueError('All four archive profiles must be represented')
    for shape in SHAPES:
        full = groups[(shape, 'read-cbor')][0]
        for operation in OPERATIONS:
            run = groups[(shape, operation)][0]
            if any(run[field] != full[field] for field in INVENTORY):
                raise ValueError('Stage inventories differ')
            result = run['Samples'][0]['Result']
            if result['OutputBytes'] != (run['RecoveryInventory']['InputCBORBytes'] if operation == 'read-cbor' else 0) or result['WrittenBytes']:
                raise ValueError('Unexpected input/output byte count')
            if operation == 'read-restore' and result['Identity'] != full['Samples'][0]['Result']['Identity']:
                raise ValueError('Restored heads differ')
            if operation == 'read-signatures' and result['Identity'] != f"peers:{run['RecoveryInventory']['CommitPeers'] + run['RecoveryInventory']['BatchPeers']}:{full['Samples'][0]['Result']['Identity']}":
                raise ValueError('Original signature peer count or head changed')
            if operation == 'read-cbor-model' and not result['Identity'].startswith('model:sha256:hex:'):
                raise ValueError('Missing exact model fingerprint')
    return value, groups


def median(runs, field):
    return statistics.median(sample[field] for run in runs for sample in run['Samples'])


def build(before_path, after_path, input_path):
    before, old = load(before_path); after, new = load(after_path)
    for field in ('BenchmarkSourceSHA256', 'BenchmarkAssemblySHA256', 'DotnetSDK', 'InstalledRuntimes',
                  'OS', 'Cpu', 'LogicalProcessors', 'RuntimeTuning', 'RuntimeConfigSHA256', 'SharedDependencies', 'Method'):
        if before['Environment'][field] != after['Environment'][field]:
            raise ValueError('Harness, method or dependency changed: ' + field)
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
            raise ValueError('Exact model/recovered result changed')
    lines = ['# Root/head snapshot reconstruction measurements', '',
             f'Before `{Path(before_path).name}`; after `{Path(after_path).name}`. Seven shapes/all four profiles, '
             'four operations, two fresh processes/three warmups/three samples per side: '
             '112 workers / 336 measured calls in total.', '',
             'The unchanged C# harness and assembly, runtime configuration, shared dependency bytes and method match. '
             'Every repeated output agrees; exact prepared archive identities, byte counts and inventories also bind '
             f'`{Path(input_path).name}`. Model fingerprints include all original peer envelopes; complete recovery '
             'checks every retained branch state, exact archive bytes/peers and fresh local runtime outside measurement.', '',
             'Before uses preserved current binaries from the bounded canonical preparation package. Scoped source reconstruction binds '
             'their original bytes; after uses the newly built production binary with the same harness. '
             'Sources and binaries are recorded separately. Workers run sequentially; no task build/test overlaps '
             'the formal series. The shared Windows desktop is not dedicated or affinity controlled.', '',
             'Model-only composes the envelope using production parsers and eagerly decodes suffix models. Actual '
             'v3/v4 recovery still decodes suffix commits lazily during replay. Prepared-model restore includes '
             'fresh trust, ancestry and state checks. Individual model/JSON trees, canonicalization, cryptography '
             'and retained snapshots remain. The signature-only probe calls individual peer callbacks without a preparation '
             'scope, and retains the preceding complete envelope factory as a control. Actual recovery/restore also uses fresh model/runtime objects and conditionally retains exact immutable snapshots; it joins '
             'a bounded scope across model preparation and replay. Stage medians cannot be added or treated as full-recovery shares.', '',
             '| Shape | Operation | Before MiB | After MiB | Allocation change | Before ms | After ms |',
             '| --- | --- | ---: | ---: | ---: | ---: | ---: |']
    for key in sorted(old):
        a = median(old[key], 'AllocatedBytes') / 1048576; b = median(new[key], 'AllocatedBytes') / 1048576
        lines.append(f'| {key[0]} | {key[1]} | {a:.4f} | {b:.4f} | {(b/a-1)*100:+.2f}% | '
                     f'{median(old[key], "ElapsedMilliseconds"):.3f} | {median(new[key], "ElapsedMilliseconds"):.3f} |')
    lines += ['', '## Collected additional recovered-history memory', '',
              'The existing post-measurement collection holds one additional recovered history with source/input '
              'live on both sides. These process-heap deltas are approximate; they include model/state/runtime '
              'overhead, can be negative, and establish no total-memory bound. This package adds no persistent '
              'cache or retained mutable JSON tree.', '',
              '| Shape | Before median MiB | After median MiB |', '| --- | ---: | ---: |']
    for shape in sorted(SHAPES):
        values = [statistics.median(run['ReplayRetention']['ManagedDeltaBytes'] for run in groups[(shape, 'read-cbor')]) / 1048576 for groups in (old, new)]
        lines.append(f'| {shape} | {values[0]:.4f} | {values[1]:.4f} |')
    lines += ['', 'Elapsed times, process CPU and 10 ms sampled peaks in the raw reports remain descriptive. '
              'Managed allocation measures the operation thread; setup, verification and disposal stay outside it. '
              'These synthetic workloads establish neither production throughput nor cancellation latency. '
              'No universal timing or allocation improvement is inferred.', '']
    return '\n'.join(lines)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--before', required=True); parser.add_argument('--after', required=True)
    parser.add_argument('--inputs', required=True); parser.add_argument('--output', required=True)
    args = parser.parse_args()
    output = Path(args.output)
    if output.exists():
        raise FileExistsError('Preserve existing evidence; use a new output path')
    result = build(args.before, args.after, args.inputs)
    output.write_text(result, encoding='utf-8', newline='\n')
    print('Matched inputs, exact outputs, harness/dependencies, all profiles and retention inventories verified.')
