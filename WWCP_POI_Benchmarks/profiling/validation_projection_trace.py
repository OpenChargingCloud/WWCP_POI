"""Profile one bound rich restore worker outside formal measurements; build an isolated TraceEvent analyzer."""
import argparse
import datetime
import json
import shutil
import subprocess
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parent.parent))
from domain_recovery_measure import run as verify, sha


def run(directory, binding, work, trace_tool):
    directory = Path(directory).resolve(); binding = Path(binding).resolve()
    work = Path(work).resolve(); trace_tool = Path(trace_tool).resolve()
    verify(directory, binding, None, 'trace-binding', verify_only=True)
    if work.exists():
        raise FileExistsError('Preserve existing traces; use a fresh work directory')
    work.mkdir(parents=True); sources = Path(__file__).resolve().parent
    for name in ('Program.cs', 'TraceAnalysis.csproj'):
        shutil.copy2(sources / (name + '.in'), work / name)
    started = datetime.datetime.now(datetime.timezone.utc).isoformat()
    version = subprocess.check_output([str(trace_tool), '--version'], text=True).strip()
    with (work / 'analyzer-build.log').open('w', encoding='utf-8') as log:
        subprocess.run(['dotnet', 'build', str(work / 'TraceAnalysis.csproj'), '-c', 'Release'],
                       stdout=log, stderr=subprocess.STDOUT, check=True)
    shape = dict(Name='domain-v1-evses-64', EVSEs=64, Changes=7, Branches=2, SnapshotEvery=0, Retentions=0, Locations=0)
    command = [str(trace_tool), 'collect', '--profile', 'gc-verbose', '--output', str(work / 'restore.nettrace'),
               '--show-child-io', '--', 'dotnet', 'exec', str(directory / 'WWCP_POI_Benchmarks.dll'),
               '--worker', '--shape', json.dumps(shape), '--operation', 'read-restore', '--samples', '3', '--warmups', '3']
    with (work / 'trace.log').open('w', encoding='utf-8') as log:
        subprocess.run(command, stdout=log, stderr=subprocess.STDOUT, check=True)
    subprocess.run(['dotnet', str(work / 'bin/Release/net10.0/TraceAnalysis.dll'),
                    str(work / 'restore.nettrace'), str(work / 'profile.json')], check=True)
    record = dict(startedUTC=started, finishedUTC=datetime.datetime.now(datetime.timezone.utc).isoformat(),
                  command=command, binding=dict(file=str(binding), sha256=sha(binding)),
                  traceTool=dict(file=str(trace_tool), version=version, sha256=sha(trace_tool)),
                  templates=[dict(file=str(sources / (name + '.in')), sha256=sha(sources / (name + '.in')))
                             for name in ('Program.cs', 'TraceAnalysis.csproj')],
                  analyzerBinaries=[dict(file=p.name, sha256=sha(p)) for p in sorted((work / 'bin/Release/net10.0').glob('*.dll'))],
                  trace=dict(file=str(work / 'restore.nettrace'), sha256=sha(work / 'restore.nettrace')),
                  summary=dict(file=str(work / 'profile.json'), sha256=sha(work / 'profile.json')),
                  formalTimingMeasurement=False, sampledAllocationStacks=True)
    (work / 'trace-binding.json').write_text(json.dumps(record, indent=2) + '\n', encoding='utf-8', newline='\n')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--binary-directory', required=True); parser.add_argument('--binding', required=True)
    parser.add_argument('--work-directory', required=True); parser.add_argument('--dotnet-trace', required=True)
    args = parser.parse_args(); run(args.binary_directory, args.binding, args.work_directory, args.dotnet_trace)
