"""Check preceding simple-workload inputs with the new harness; these calls are correctness controls."""
import argparse
import datetime
import json
import subprocess
from pathlib import Path
from domain_recovery_measure import run as verify_binding, sha
from parser_cancellation_measure import SHAPES

INVENTORY = ('Shape', 'Entities', 'RetainedCommits', 'Receipts', 'CatalogIds', 'StaticIdentity',
             'ArchiveIdentity', 'RecoveryInventory')


def run(binary_directory, binding, preceding, output):
    verify_binding(binary_directory, binding, None, 'controls', verify_only=True)
    destination = Path(output)
    if destination.exists():
        raise FileExistsError('Preserve existing evidence; use a fresh output path')
    previous = json.loads(Path(preceding).read_text(encoding='utf-8-sig'))
    if not previous['Complete']:
        raise ValueError('Complete preceding evidence is required')
    prepared = {item['Shape']['Name']: item for item in previous['Runs'] if item['Operation'] == 'read-cbor-limits'}
    result = dict(Complete=False, Method='Six one-warmup/one-sample fresh-process correctness controls; no performance comparison.',
                  StartedUTC=datetime.datetime.now(datetime.timezone.utc).isoformat(),
                  SourceBindingSHA256=sha(binding), PrecedingReportSHA256=sha(preceding), Runs=[])
    for row in SHAPES:
        shape = dict(zip(['Name', 'EVSEs', 'Changes', 'Branches', 'SnapshotEvery', 'Retentions', 'Locations'], row))
        command = ['dotnet', str(Path(binary_directory).resolve()/'WWCP_POI_Benchmarks.dll'), '--worker',
                   '--shape', json.dumps(shape), '--operation', 'read-cbor-limits', '--warmups', '1', '--samples', '1']
        process = subprocess.run(command, capture_output=True, text=True, timeout=600)
        if process.returncode:
            raise RuntimeError(process.stderr)
        current = json.loads(process.stdout); before = prepared[shape['Name']]
        if any(current[field] != before[field] for field in INVENTORY) or current['DomainInventory'] is not None:
            raise ValueError('Preceding prepared simple-workload data changed: '+shape['Name'])
        if len(current['Samples']) != 1 or current['Samples'][0]['Result'] != before['Samples'][0]['Result']:
            raise ValueError('Exact input/result changed: '+shape['Name'])
        result['Runs'].append(current)
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_text(json.dumps(result, indent=2)+'\n', encoding='utf-8', newline='\n')
        print(shape['Name'], 'preceding input/identity/peer/result inventory unchanged', flush=True)
    result['Complete'] = True; result['FinishedUTC'] = datetime.datetime.now(datetime.timezone.utc).isoformat()
    destination.write_text(json.dumps(result, indent=2)+'\n', encoding='utf-8', newline='\n')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--binary-directory', required=True); parser.add_argument('--binding', required=True)
    parser.add_argument('--preceding', required=True); parser.add_argument('--output', required=True)
    args = parser.parse_args(); run(args.binary_directory, args.binding, args.preceding, args.output)
