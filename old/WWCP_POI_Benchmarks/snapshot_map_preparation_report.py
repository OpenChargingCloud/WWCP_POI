"""Matched normalized snapshot map/binding preparation reports; strict existing archive validation."""
import argparse
import statistics
from pathlib import Path
from domain_recovery_report import median
from validation_projection_report import compare, change


def build(before_path, before_binding, after_path, after_binding):
    _, left, _, right = compare(before_path, before_binding, after_path, after_binding)
    lines = ['# Normalized immutable snapshot map preparation: matched recovery comparison', '',
             f'Before `{Path(before_path).name}` / `{Path(before_binding).name}`; '
             f'after `{Path(after_path).name}` / `{Path(after_binding).name}`.', '',
             'Eight rich 16/64-EVSE shapes, all four archive profiles and four recovery stages; '
             'two fresh sequential workers, three warmups and three measured calls per worker: '
             '64 workers / 192 measured calls per side. The preceding formal series is preserved. '
             'C# harness, shared dependencies, runtime configuration and manifest have identical bytes. '
             'Production sources and DLLs differ under explicit bindings.', '',
             'All prepared archive bytes/ETags, complete graph/value/reference inventories, original '
             'signature peers, both branch states and complete recovered model/head results match. '
             'The fixed 32 reference artifacts remain unchanged.', '',
             'Fresh models retain the complete domain parser, metadata completion and reference checks. '
             'Normalized exact property equality permits reuse of immutable property values, dictionaries, '
             'child sets, entity records and map branches. Removed identities and reference consumers are '
             'removed; all current references are checked again. Unchanged reverse indexes are reused. '
             'Representation binding reads stable typed identities directly instead of serializing '
             'each child subtree again. No mutable JSON/model/runtime/trust cache is added.', '',
             'Stages cover different work and are not additive shares. Model-only controls eagerly decode '
             'prepared suffix models; production v3/v4 suffix recovery remains lazy. Setup, warmup, '
             'full byte/branch/runtime controls and disposal are excluded from measured calls. '
             'Current policy and every crypto/authority check remain inside recovery.', '',
             'Allocations are exact operation-thread cumulative counters, not resident RAM. Time/CPU '
             'and sampled/collected process memory are descriptive on a shared Windows desktop without '
             'affinity control. Historical before and new after runs occur at different times; no task '
             'builds/tests/profiles overlap the new formal series. No production capacity, constant '
             'total-memory or cancellation-latency bound follows.', '',
             '## Operation-thread allocations and elapsed time', '',
             '| Shape | Stage | Before MiB | After MiB | Allocation change | Before ms | After ms | Time change |',
             '| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |']
    for key in sorted(left):
        a = median(left[key], 'AllocatedBytes') / 1048576; b = median(right[key], 'AllocatedBytes') / 1048576
        t = median(left[key], 'ElapsedMilliseconds'); u = median(right[key], 'ElapsedMilliseconds')
        lines.append(f'| {key[0]} | {key[1]} | {a:.4f} | {b:.4f} | {change(a,b):+.2f}% | {t:.3f} | {u:.3f} | {change(t,u):+.2f}% |')
    lines += ['', '## Collected additional recovered-history memory', '',
              'One additional recovered history is collected while prepared inputs/source history stay live. '
              'These approximate process-heap deltas include models, shared maps, runtime and retained states. '
              'Sharing normalized immutable entities can reduce this retained graph; temporary allocations '
              'and retained memory remain distinct measurements.', '',
              '| Shape | Before MiB | After MiB | Change |', '| --- | ---: | ---: | ---: |']
    for name in sorted({key[0] for key in left}):
        a = statistics.median(run['ReplayRetention']['ManagedDeltaBytes'] for run in left[(name, 'read-cbor')]) / 1048576
        b = statistics.median(run['ReplayRetention']['ManagedDeltaBytes'] for run in right[(name, 'read-cbor')]) / 1048576
        lines.append(f'| {name} | {a:.4f} | {b:.4f} | {change(a,b):+.2f}% |')
    lines += ['', 'Ordinary domain parsing, detached JSON preparation, property comparison, metrological '
              'normalization, crypto and fresh local runtime still allocate. Group-heavy and larger '
              'catalogs, longer histories and production concurrency need separate workloads.', '']
    return '\n'.join(lines)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--before', required=True); parser.add_argument('--before-binding', required=True)
    parser.add_argument('--after', required=True); parser.add_argument('--after-binding', required=True)
    parser.add_argument('--output', required=True); args = parser.parse_args()
    Path(args.output).write_text(build(args.before, args.before_binding, args.after, args.after_binding), encoding='utf-8', newline='\n')
