"""Run the fixed shared-reference/tariff/parking recovery baseline against explicitly bound binaries (stdlib only)."""
import argparse
import datetime
import hashlib
import json
import os
import platform
import statistics
import subprocess
from pathlib import Path

SHAPES = [(f'domain-v{profile}-evses-{evses}', evses, 7, 2, 0 if profile == 1 else 7, 2 if profile == 4 else 0, 0)
          for evses in (16, 64) for profile in range(1, 5)]
OPERATIONS = ['read-cbor-model', 'read-restore', 'read-cbor', 'read-json']


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def run(binary_directory, binding_path, output, label, verify_only=False):
    directory = Path(binary_directory).resolve(); binding_file = Path(binding_path).resolve()
    binding = json.loads(binding_file.read_text(encoding='utf-8-sig'))
    library = directory / 'WWCP_POI.dll'; harness = directory / 'WWCP_POI_Benchmarks.dll'
    harness_hash = binding.get('measurementBenchmarkAssemblySHA256', binding.get('benchmarkAssemblySHA256'))
    if sha(library) != binding['productionAssemblySHA256'] or sha(harness) != harness_hash:
        raise ValueError('Binary bytes differ from the explicit source/binary binding')
    for dependency in binding['sharedDependencies']:
        if sha(directory / dependency['name']) != dependency['sha256']:
            raise ValueError('Shared dependency changed: ' + dependency['name'])
    if sha(directory / 'WWCP_POI_Benchmarks.runtimeconfig.json') != binding['runtimeConfigSHA256']:
        raise ValueError('Runtime configuration changed')
    if sha(directory / 'WWCP_POI_Benchmarks.deps.json') != binding['dependencyManifestSHA256']:
        raise ValueError('Dependency manifest changed')
    if verify_only:
        print('Production, preserved harness and shared dependencies match their explicit binding.')
        return
    destination = Path(output).resolve()
    if destination.exists():
        raise FileExistsError('Preserve existing evidence; use a fresh output path')
    env = {'TimestampUTC': datetime.datetime.now(datetime.timezone.utc).isoformat(), 'Label': label,
           'Suite': 'domainrecovery', 'BuildConfiguration': 'Release', 'ProcessesPerOperation': 2,
           'WarmupsPerProcess': 3, 'SamplesPerProcess': 3,
           'ProductionSourceSHA256': binding['productionSourceSHA256'],
           'BenchmarkSourceSHA256': binding['benchmarkSourceSHA256'],
           'ProductionAssemblySHA256': sha(library), 'BenchmarkAssemblySHA256': sha(harness),
           'SourceBindingSHA256': sha(binding_file),
           'DotnetSDK': subprocess.check_output(['dotnet', '--version'], text=True).strip(),
           'InstalledRuntimes': subprocess.check_output(['dotnet', '--list-runtimes'], text=True).strip(),
           'Method': 'Sequential fresh dotnet workers; no overlapping task builds/tests; shared Windows desktop; setup/warmup excluded.',
           'OS': platform.platform(), 'Cpu': os.environ.get('PROCESSOR_IDENTIFIER'), 'LogicalProcessors': os.cpu_count(),
           'RuntimeTuning': {name: os.environ.get(name) for name in ['DOTNET_TieredCompilation', 'DOTNET_TieredPGO', 'DOTNET_ReadyToRun']},
           'RuntimeConfigSHA256': sha(directory / 'WWCP_POI_Benchmarks.runtimeconfig.json'),
           'DependencyManifestSHA256': sha(directory / 'WWCP_POI_Benchmarks.deps.json'),
           'SharedDependencies': binding['sharedDependencies']}
    report = {'Environment': env, 'Complete': False, 'Runs': []}
    destination.parent.mkdir(parents=True, exist_ok=True)
    for row in SHAPES:
        shape = dict(zip(['Name', 'EVSEs', 'Changes', 'Branches', 'SnapshotEvery', 'Retentions', 'Locations'], row))
        for operation in OPERATIONS:
            for repeat in range(2):
                command = ['dotnet', str(harness), '--worker', '--shape', json.dumps(shape), '--operation', operation,
                           '--samples', '3', '--warmups', '3']
                result = subprocess.run(command, capture_output=True, text=True, timeout=600)
                if result.returncode:
                    raise RuntimeError(result.stderr)
                record = json.loads(result.stdout)
                if record['Shape'] != shape or record['Operation'] != operation or len(record['Samples']) != 3:
                    raise ValueError('Worker returned a different shape, operation or sample count')
                report['Runs'].append(record)
                destination.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8', newline='\n')
                allocation = statistics.median(sample['AllocatedBytes'] for sample in record['Samples']) / 1048576
                print(shape['Name'], operation, 'process', repeat + 1, f'{allocation:.3f} MiB', flush=True)
    report['Complete'] = True; env['FinishedUTC'] = datetime.datetime.now(datetime.timezone.utc).isoformat()
    destination.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8', newline='\n')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--binary-directory', required=True); parser.add_argument('--binding', required=True)
    parser.add_argument('--output'); parser.add_argument('--label', default='domain-recovery-rerun')
    parser.add_argument('--verify-only', action='store_true')
    args = parser.parse_args()
    if not args.verify_only and args.output is None:
        parser.error('--output is required for a measurement run')
    run(args.binary_directory, args.binding, args.output, args.label, args.verify_only)
