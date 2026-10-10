"""Validate fixed signature-copy measurements and reproduce their summary (stdlib only)."""
import argparse
import json
import statistics
from pathlib import Path
from signature_copies_measure import SHAPES, OPERATIONS

INVENTORY = ('Shape', 'Entities', 'RetainedCommits', 'Receipts', 'CatalogIds', 'StaticIdentity', 'ArchiveIdentity')


def load(path):
    value = json.loads(Path(path).read_text(encoding='utf-8-sig'))
    env = value['Environment']
    if not value['Complete'] or env['Suite'] != 'signature-copies' or env['BuildConfiguration'] != 'Release':
        raise ValueError('A complete Release signature-copy report is required')
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
            if run['Samples'][0]['Result']['WrittenBytes'] != 0 or run['Samples'][0]['Result']['OutputBytes'] <= 0:
                raise ValueError('Unexpected transport result')
            if run['RecoveryInventory'] is not None or run['ReplayRetention'] is not None:
                raise ValueError('Unexpected recovery probe')
    return value, groups


def median(runs, field):
    return statistics.median(sample[field] for run in runs for sample in run['Samples'])


def build(before_path, after_path):
    before, old = load(before_path); after, new = load(after_path)
    for field in ('BenchmarkSourceSHA256', 'BenchmarkAssemblySHA256', 'DotnetSDK', 'InstalledRuntimes',
                  'OS', 'Cpu', 'LogicalProcessors', 'RuntimeTuning', 'RuntimeConfigSHA256', 'SharedDependencies', 'Method'):
        if before['Environment'][field] != after['Environment'][field]:
            raise ValueError('Harness, method or dependency changed: ' + field)
    for key in old:
        for field in INVENTORY:
            if old[key][0][field] != new[key][0][field]:
                raise ValueError('Prepared input or output inventory changed: ' + str(key) + ':' + field)
        if old[key][0]['Samples'][0]['Result'] != new[key][0]['Samples'][0]['Result']:
            raise ValueError('Exact copy output or identity changed')
    lines = ['# Immutable signature-copy measurements', '',
             f'Before `{Path(before_path).name}`; after `{Path(after_path).name}`. Four shapes/five operations, '
             'two fresh processes/three warmups/three samples per side: 80 workers / 240 measured calls.', '',
             'Each measured call creates sixteen independent copies of one immutable source. Construction, '
             'transport, full-constructor oracle comparison and current cryptographic verification are outside measurement. '
             'The commit-sign operation includes real Ed25519 signing and its unsigned signing preimage. '
             'No canonical preparation scope is active. WithChangeSet receives a prepared batch peer copy; '
             'creation of that batch and cloning its metadata are excluded.', '',
             'The exact same new C# harness binary, runtime configuration and shared dependency bytes are used '
             'on both sides. Every input, complete signed output digest, commit ID and repeated result agrees. '
             'Workers run sequentially; no task build/test overlaps these formal series. The Windows desktop '
             'is shared and is not affinity controlled.', '',
             '| Shape | Operation | Before MiB | After MiB | Allocation change | Before ms | After ms |',
             '| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |']
    for key in sorted(old):
        a = median(old[key], 'AllocatedBytes') / 1048576; b = median(new[key], 'AllocatedBytes') / 1048576
        lines.append(f'| {key[0]} | {key[1]} | {a:.4f} | {b:.4f} | {(b/a-1)*100:+.4f}% | '
                     f'{median(old[key], "ElapsedMilliseconds"):.3f} | {median(new[key], "ElapsedMilliseconds"):.3f} |')
    lines += ['', 'Elapsed time, process CPU and 10 ms sampled peaks are descriptive. Managed allocation '
              'measures the operation thread. These copy probes establish no end-to-end recovery, throughput, '
              'retained-memory or production-concurrency improvement. Signature generation, independent parsing, '
              'changed unsigned content and exact metadata comparison retain their costs.', '']
    return '\n'.join(lines)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--before', required=True); parser.add_argument('--after', required=True); parser.add_argument('--output', required=True)
    args = parser.parse_args(); destination = Path(args.output)
    if destination.exists():
        raise FileExistsError('Preserve existing evidence; use a fresh output path')
    destination.write_text(build(args.before, args.after), encoding='utf-8', newline='\n')
    print('Matched inputs, exact copy outputs, harness/dependencies and method verified.')
