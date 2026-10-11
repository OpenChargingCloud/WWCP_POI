"""Compare bound recovery reports with exact shared inputs, harness and verification inventories (stdlib only)."""
import argparse
import datetime
import math
import statistics
from pathlib import Path
from domain_recovery_report import INVENTORY, load, median

METHOD = ('Suite', 'BuildConfiguration', 'ProcessesPerOperation', 'WarmupsPerProcess', 'SamplesPerProcess',
          'BenchmarkSourceSHA256', 'BenchmarkAssemblySHA256', 'DotnetSDK', 'InstalledRuntimes', 'Method', 'OS',
          'Cpu', 'LogicalProcessors', 'RuntimeTuning', 'RuntimeConfigSHA256', 'DependencyManifestSHA256', 'SharedDependencies')


def compare(before_path, before_binding, after_path, after_binding):
    before, left = load(before_path, before_binding); after, right = load(after_path, after_binding)
    for field in METHOD:
        if before['Environment'][field] != after['Environment'][field]:
            raise ValueError('Comparison method/harness/dependency changed: ' + field)
    for report in (before, after):
        start = datetime.datetime.fromisoformat(report['Environment']['TimestampUTC'])
        finish = datetime.datetime.fromisoformat(report['Environment']['FinishedUTC'])
        if finish <= start or start.utcoffset() is None or finish.utcoffset() is None:
            raise ValueError('Invalid formal series times')
    if set(left) != set(right):
        raise ValueError('Workload groups changed')
    for key in left:
        a = left[key][0]; b = right[key][0]
        for field in INVENTORY:
            if a[field] != b[field]:
                raise ValueError('Prepared input/domain/branch identity changed: ' + field)
        if a['Samples'][0]['Result'] != b['Samples'][0]['Result']:
            raise ValueError('Complete model/peer/head result changed')
        if key[1] == 'read-cbor':
            for run in left[key] + right[key]:
                if not math.isfinite(run['ReplayRetention']['ManagedDeltaBytes']):
                    raise ValueError('Nonfinite collected heap delta')
    return before, left, after, right


def change(before, after):
    return (after / before - 1) * 100 if before else 0 if not after else float('inf')


def build(before_path, before_binding, after_path, after_binding):
    before, left, after, right = compare(before_path, before_binding, after_path, after_binding)
    lines = ['# Temporary validation projection reuse: matched recovery comparison', '',
             f'Before `{Path(before_path).name}` / `{Path(before_binding).name}`; '
             f'after `{Path(after_path).name}` / `{Path(after_binding).name}`.', '',
             'Each side contains eight rich 16/64-EVSE shapes across four profiles, four production recovery '
             'stages, two fresh sequential workers/three warmups/three samples: 64 workers / 192 measured calls '
             'per side. The preceding baseline remains unchanged. The complete C# harness, dependency DLLs, '
             'runtime configuration and manifest have identical bytes. Production source/DLL bytes differ '
             'under explicit bindings.', '',
             'All prepared archive bytes/ETags, graph/value/reference inventories, two original peers per '
             'retained commit/batch, static heads and every branch-state fingerprint match. Complete model/peer '
             'results and recovered heads match in every group. These workloads contain no tariff groups; the '
             'separate *TG ID correction is covered by focused JSON/CBOR tests.', '',
             'A pass reuses successful temporary ancestor projections and one station-reference view for one '
             'fixed immutable map. Ordinary parsers and all reference checks still run; failed projections are '
             'not cached. The pass ends before a later map/change and retains no runtime, trust or history cache.', '',
             'Model-only eagerly decodes already parsed suffix models; actual v3/v4 suffix recovery remains lazy. '
             'Prepared-model restore and full CBOR/JSON include different work. Stages are not additive shares. '
             'Setup/warmup, full byte/branch/graph/runtime controls and disposal are outside calls; fresh current '
             'policy and crypto verification remain within recovery.', '',
             'Operation-thread cumulative allocations are exact counters for measured synchronous calls, '
             'not resident RAM. Time/CPU/sampled peaks and collected heap deltas are descriptive measurements '
             'on a shared Windows desktop without affinity control. Historical before and new after series '
             'run at different times, outside task builds/tests. No production throughput/capacity, constant '
             'total-memory or cancellation-latency bound follows.', '',
             '## Cumulative operation-thread allocation and elapsed time', '',
             '| Shape | Stage | Before MiB | After MiB | Allocation change | Before ms | After ms | Time change |',
             '| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |']
    for key in sorted(left):
        a = median(left[key], 'AllocatedBytes') / 1048576; b = median(right[key], 'AllocatedBytes') / 1048576
        t = median(left[key], 'ElapsedMilliseconds'); u = median(right[key], 'ElapsedMilliseconds')
        lines.append(f'| {key[0]} | {key[1]} | {a:.4f} | {b:.4f} | {change(a,b):+.2f}% | {t:.3f} | {u:.3f} | {change(t,u):+.2f}% |')
    lines += ['', '## Collected additional recovered-history memory', '',
              'One additional history is collected after full CBOR recovery while prepared source/input stay live. '
              'These approximate process-heap deltas include model/maps/runtime/retained states; private validation '
              'pass projections are not retained. Heap decreases and increases are reported without a capacity claim.', '',
              '| Shape | Before MiB | After MiB | Change |', '| --- | ---: | ---: | ---: |']
    for name in sorted({key[0] for key in left}):
        a = statistics.median(run['ReplayRetention']['ManagedDeltaBytes'] for run in left[(name, 'read-cbor')]) / 1048576
        b = statistics.median(run['ReplayRetention']['ManagedDeltaBytes'] for run in right[(name, 'read-cbor')]) / 1048576
        lines.append(f'| {name} | {a:.4f} | {b:.4f} | {change(a,b):+.2f}% |')
    lines += ['', 'The model-only controls quantify unchanged decoding work. Remaining parsing, own-document '
              'preparation, immutable map rebuilding, crypto and final root/head model reconstruction still allocate. '
              'Larger registries, more parking/group consumers and production concurrency need separate workloads.', '']
    return '\n'.join(lines)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--before', required=True); parser.add_argument('--before-binding', required=True)
    parser.add_argument('--after', required=True); parser.add_argument('--after-binding', required=True)
    parser.add_argument('--output', required=True); args = parser.parse_args()
    Path(args.output).write_text(build(args.before, args.before_binding, args.after, args.after_binding), encoding='utf-8', newline='\n')
