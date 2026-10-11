"""Validate the fixed shared-reference/tariff/parking recovery baseline (stdlib only)."""
import argparse
import hashlib
import json
import math
import re
import statistics
from pathlib import Path
from domain_recovery_measure import SHAPES, OPERATIONS

INVENTORY = ('Shape', 'Entities', 'RetainedCommits', 'Receipts', 'CatalogIds', 'StaticIdentity',
             'ArchiveIdentity', 'RecoveryInventory', 'DomainInventory')


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def digest_identity(value, kind):
    return re.fullmatch(re.escape(kind) + r':sha256:hex:[0-9a-f]{64}', value) is not None


def load(path, binding_path):
    value = json.loads(Path(path).read_text(encoding='utf-8-sig')); env = value['Environment']
    binding = json.loads(Path(binding_path).read_text(encoding='utf-8-sig'))
    if not value['Complete'] or env['Suite'] != 'domainrecovery' or env['BuildConfiguration'] != 'Release':
        raise ValueError('A complete Release domainrecovery report is required')
    if (env['ProcessesPerOperation'], env['WarmupsPerProcess'], env['SamplesPerProcess']) != (2, 3, 3):
        raise ValueError('Unexpected worker/warmup/sample method')
    if env['SourceBindingSHA256'] != sha(binding_path):
        raise ValueError('Source binding bytes changed')
    for field in ('productionSourceSHA256', 'productionAssemblySHA256', 'benchmarkSourceSHA256', 'benchmarkAssemblySHA256'):
        if env[field[0].upper()+field[1:]] != binding[field]:
            raise ValueError('Source or assembly changed: ' + field)
    if env['SharedDependencies'] != binding['sharedDependencies']:
        raise ValueError('Shared dependency changed')
    if env['RuntimeConfigSHA256'] != binding['runtimeConfigSHA256'] or env['DependencyManifestSHA256'] != binding['dependencyManifestSHA256']:
        raise ValueError('Runtime configuration or dependency manifest changed')
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
            for sample in run['Samples']:
                for field in ('ElapsedMilliseconds', 'CpuMilliseconds', 'AllocatedBytes', 'ManagedBeforeBytes',
                              'SampledManagedPeakBytes', 'WorkingSetBeforeBytes', 'SampledWorkingSetPeakBytes',
                              'ProcessLifetimePeakWorkingSetBytes', 'Gen0', 'Gen1', 'Gen2'):
                    if not math.isfinite(sample[field]) or sample[field] < 0:
                        raise ValueError('Invalid measurement: ' + field)
            static = run['StaticIdentity'].split('|')
            if len(static) != 2 or not digest_identity(static[0], 'json') or not digest_identity(static[1], 'cbor'):
                raise ValueError('Malformed static identity')
            if not digest_identity(run['ArchiveIdentity'], 'cbor'):
                raise ValueError('Malformed archive identity')
            if run['ChildETagRetention'] is not None or run['ChildETagState'] is not None:
                raise ValueError('Unexpected child-cache probe')
            if key[1] == 'read-cbor':
                retention = run['ReplayRetention']
                if retention['RetainedCommits'] != run['RetainedCommits'] or retention['RetainedSnapshots'] != run['RecoveryInventory']['Snapshots']:
                    raise ValueError('Collected retention inventory changed')
            elif run['ReplayRetention'] is not None:
                raise ValueError('Unexpected retention probe')
    for name, shape in shapes.items():
        full = groups[(name, 'read-cbor')][0]; domain = full['DomainInventory']; profile = int(name[8]); evses = shape['EVSEs']
        if full['RecoveryInventory']['Profile'] != f'wwcp-poi-history-v{profile}':
            raise ValueError('Archive profile changed')
        expected = dict(Pools=evses//8, Stations=evses//4, EVSEs=evses, GridOperators=2, SoftwareReleases=4, Certificates=2,
                        MeterSlots=evses//8*3+evses//4*2+evses, Tariffs=4, TariffElements=8, PriceComponents=24, Restrictions=16,
                        ParkingOperators=1, ParkingGarages=evses//8, ParkingSpaces=evses, ParkingGroups=2, ParkingProducts=2, MergeCommits=1)
        expected['SoftwareAssignments'] = expected['MeterSlots']*2
        if any(domain[field] != count for field, count in expected.items()) or domain['ReferenceEdges'] <= evses*8:
            raise ValueError('Missing shared-reference/tariff/parking inventory')
        if domain['ElementOperations'] < 4 or not digest_identity(domain['BranchStateIdentity'], 'branches'):
            raise ValueError('Missing element operations or exact branch-state identity')
        if full['Receipts'] != (2 if profile == 4 else 0):
            raise ValueError('Retention receipt inventory changed')
        if full['RecoveryInventory']['CommitPeers'] != full['RetainedCommits']*2:
            raise ValueError('Original commit peers changed')
        checkpoint = 1 if profile in (1, 2) else 0
        if full['RecoveryInventory']['BatchPeers'] != (full['RetainedCommits'] - full['RecoveryInventory']['Snapshots'] - checkpoint)*2:
            raise ValueError('Original batch peers changed')
        for operation in OPERATIONS:
            run = groups[(name, operation)][0]; result = run['Samples'][0]['Result']
            if any(run[field] != full[field] for field in INVENTORY):
                raise ValueError('Stage inputs or domain inventories differ')
            output = run['RecoveryInventory']['InputCBORBytes'] if operation == 'read-cbor' else run['RecoveryInventory']['InputJSONBytes'] if operation == 'read-json' else 0
            if result['OutputBytes'] != output or result['WrittenBytes']:
                raise ValueError('Unexpected input/output bytes')
            if operation == 'read-cbor-model':
                if not digest_identity(result['Identity'], 'model'):
                    raise ValueError('Missing complete model/peer identity')
            elif not digest_identity(result['Identity'], 'json') or result['Identity'] != full['Samples'][0]['Result']['Identity']:
                raise ValueError('Recovered heads differ')
    return value, groups


def median(runs, field):
    return statistics.median(sample[field] for run in runs for sample in run['Samples'])


def build(path, binding_path):
    value, groups = load(path, binding_path)
    lines = ['# Shared-reference, tariff and parking recovery baseline', '',
             f'Input `{Path(path).name}`; binding `{Path(binding_path).name}`. Eight shapes/all four profiles, '
             '16/64 EVSEs, four stages, two sequential fresh processes/three warmups/three samples: '
             '64 workers / 192 measured calls. This package makes no production change and records a new baseline.', '',
             'Prepared archives contain shared grid/software/document registries, meters at pool/connection/station/EVSE '
             'slots, nested tariff components/restrictions, EVSE/connector tariff references, parking garage/space/group/'
             'product relations, targeted membership edits and one signed two-parent merge. Two original Ed25519 peers '
             'authenticate each retained commit and batch. Setup, crypto/input controls, full byte/branch/reference '
             'checks and disposal are outside measured calls; restore/full recovery still perform fresh trust.', '',
             'All stages/repeated workers use exact common inputs, complete model/peer fingerprints, static ETags, heads '
             'and every branch-state identity. Recovered head models resolve one shared catalog instance per ID and '
             'fresh local runtime. Original source EVSE/grid/parking/meter statuses stay local.', '',
             'Model-only eagerly decodes all suffix models from an already parsed archive tree, using production parsers. '
             'Prepared-model restore uses the existing private production replay entry points. Actual v3/v4 recovery '
             'keeps lazy suffix model timing. Complete CBOR/JSON recovery includes normal syntax/model/replay work. '
             'Stages cannot be added/subtracted as processing shares. Historical simple EVSE-power workloads remain '
             'separate controls; different data, edits and peer counts prevent a before/after performance comparison.', '',
             'Formal workers run sequentially outside task builds/tests on a shared Windows desktop without affinity '
             'control. Time, CPU, sampled peaks and collected heap deltas are descriptive; no production capacity '
             'or cancellation-latency bound follows.', '',
             '## Actual domain and archive inventory', '',
             '| Shape | Entities | Meters / assignments | References | Commits / snapshots / merges | Receipts / catalog IDs | CBOR / JSON bytes |',
             '| --- | ---: | ---: | ---: | ---: | ---: | ---: |']
    for name in sorted(row[0] for row in SHAPES):
        run = groups[(name, 'read-cbor')][0]; d = run['DomainInventory']; i = run['RecoveryInventory']
        lines.append(f'| {name} | {run["Entities"]} | {d["MeterSlots"]} / {d["SoftwareAssignments"]} | {d["ReferenceEdges"]} | '
                     f'{run["RetainedCommits"]} / {i["Snapshots"]} / {d["MergeCommits"]} | {run["Receipts"]} / {run["CatalogIds"]} | '
                     f'{i["InputCBORBytes"]} / {i["InputJSONBytes"]} |')
    lines += ['', 'Every shape contains two grid operators, four software releases, two approval documents, '
              'four tariffs/eight elements/24 price components/16 restrictions, one parking operator, two overlapping '
              'groups and two parking products. Pools/garages are EVSEs/8, stations EVSEs/4 and spaces EVSEs. '
              'All reference counts describe actual retained head edges; repeated references by the same owner '
              'collapse in the production reference index. Meter applicability counts describe physical slots.', '',
              '## Operation-thread allocation and elapsed time', '',
              '| Shape | Stage | Median MiB | Median ms |', '| --- | --- | ---: | ---: |']
    for key in sorted(groups):
        lines.append(f'| {key[0]} | {key[1]} | {median(groups[key], "AllocatedBytes")/1048576:.4f} | {median(groups[key], "ElapsedMilliseconds"):.3f} |')
    lines += ['', '## Collected additional recovered-history memory', '',
              'One additional CBOR-recovered history is collected with source/input still live. Approximate process-heap '
              'deltas include model/maps/runtime/retained states; they provide no constant total-memory bound.', '',
              '| Shape | Median MiB |', '| --- | ---: |']
    for name in sorted(row[0] for row in SHAPES):
        lines.append(f'| {name} | {statistics.median(run["ReplayRetention"]["ManagedDeltaBytes"] for run in groups[(name,"read-cbor")])/1048576:.4f} |')
    lines += ['', 'Use this baseline to select and compare later model/reference/replay changes against exactly these '
              'inputs and current verification contracts. Individual trees, quantity parsing, reference resolution, '
              'fresh cryptography and retained immutable states remain costs. Production concurrency, larger catalogs '
              'and independent implementations require separate measurements.', '']
    return '\n'.join(lines)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--input', required=True); parser.add_argument('--binding', required=True); parser.add_argument('--output', required=True)
    args = parser.parse_args(); output = Path(args.output)
    if output.exists():
        raise FileExistsError('Preserve existing evidence; use a fresh output path')
    output.write_text(build(args.input, args.binding), encoding='utf-8', newline='\n')
    print('Complete groups, exact stage results/inputs, shared domain inventories, original peers and binary bindings verified.')
