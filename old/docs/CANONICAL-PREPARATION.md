# Bounded canonical identity and signature preparation

Commit identity and signature profiles continue to use Styx canonical JSON and their original
domain separation. This package reuses successful unsigned canonical bytes while a synchronous
recovery or peer verification call is active. It changes neither a wire profile nor a digest,
signature, numeric token, SI string, ancestry rule or application trust policy.

## Ownership and lifetime

`POICanonicalPreparation` stores unsigned content by object reference identity. Immutable
ChangeSets and commits supply the content; equal record values and shared IDs do not establish
cache identity. No field is added to a ChangeSet record, so its equality and hash remain intact.
Commit construction admits bytes only after checking the computed identity against a declared
identity. At this package's baseline, signature-only copies still pass the complete constructor and identity checks;
replacing a batch never bypasses unsigned-content validation.

An outer synchronous scope owns a context. Nested recovery and verification scopes join it;
their disposal does not release the outer context. The outer scope clears every entry and
byte count on success, rejection, callback failure or cancellation. Contexts are thread local,
do not flow through `ExecutionContext` into another thread, and are not retained by a recovered
history or its trust delegates. There is no `await` inside a preparation scope.

Scopes cover JSON/CBOR history model preparation and replay, prepared-model restore, boundary
replay, canonical bootstrap restore, history commit/batch peer loops, public `VerifySignatures`
loops and standalone snapshot ChangeSet verification. Full archive syntax and indexing still
precede model preparation and trust; bootstrap canonical/manifest checks retain their order.
Direct individual `GetSigningBytes`/`VerifySignature` calls outside a scope use the preceding
complete-envelope factory; they cannot reuse content across peers. Public identity and signing arrays always own detached bytes.

## Admission bounds

The context retains at most **64 entries**, **1 MiB per canonical value** and **4 MiB of canonical
bytes in total**. Admission keeps the successful prefix without eviction. An oversized value or
full context falls back to fresh preparation; it does not reject a valid commit or signature.
Failed unsigned canonicalization is never admitted. Valid unsigned content can remain admitted
when a later envelope or trust check fails; no such result is cached. The limits bound cached
canonical bytes and entry count,
not total process memory. Dictionary/object overhead, temporarily prepared values, signing
result arrays, headers and referenced immutable object graphs are additional memory.

No public key, private key, signature envelope or trust decision is cached. Each original peer
still invokes the current resolver/verifier and cryptographic operation. Every authorization
and boundary policy remains fresh. Runtime states are neither hashed nor restored from these
static bytes; recovery still creates local runtime state and publication keeps its existing
atomic before/after checks.

## Exact signing envelopes

The unsigned object is written and canonicalized through the preceding Styx pipeline. Its
container depth is recorded. A small algorithm/key/profile/encoding header is independently
canonicalized with a null payload placeholder. A JSON token reader locates that root property,
and a new array combines the canonical header with the exact canonical unsigned bytes.
Property order, escaping and the commit-ID array remain Styx-defined. Header strings containing
`"Commit":null` or `"ChangeSet":null` cannot select a placeholder inside a string.

Adding the signature envelope increases container depth by one. Unsigned depth 64 and invalid
preparation use the preceding complete-envelope factory. This preserves the existing default
`JsonDocument` depth limit, validation order, exception type and diagnostic byte positions.
Algorithm/key guards still run first. Arbitrary valid number tokens stay raw, including decimal,
exponent and negative-zero spellings; this optimization introduces no numeric conversion.

## Verification and measurements

The new fixture compares frozen preceding canonical factories and unsigned commit/ChangeSet
writers, public-array detachment, different algorithms and Unicode/escaped key IDs, metadata
changes, unchanged record equality, original deterministic peers and rejected unsigned copies.
It also checks depth edges, invalid-input retries, all admission bounds, nested/copied scopes,
released owner references, independent parallel readers, fresh peer resolvers, all four archive
profiles, bootstrap, eager/lazy error timing, cancellation and atomic rejection/retry.
Snapshot payload serialization and Styx remain shared unchanged helpers in the oracle.
All **106 new cases**, **2,048 interoperability cases** and **2,380 full Release cases** pass
on 2026-10-09 with zero failures and one ordinary worker skipped. Both explicit generators
are excluded; all fixed artifacts remain unchanged. Library/test/benchmark builds succeed.

[Raw before](performance/canonical-preparation-before.json) and
[raw after](performance/canonical-preparation-after.json) cover seven prepared shapes and all
four profiles, with model-only decoding, individual signature verification, prepared-model
restore and complete CBOR recovery. Each operation uses two fresh processes, three warmups
and three measured calls per process: **112 workers / 336 samples** across both sides.

Complete CBOR recovery allocation decreases **0.40–7.36%** across the seven shapes. The pruned
history changes **117.3930 -> 108.7507 MiB (-7.36%)**; the 2,048-operation/four-peer batch changes
**1581.0677 -> 1524.6701 MiB (-3.57%)**; 512 EVSEs change **1571.1970 -> 1561.2854 MiB (-0.63%)**.
Prepared restore includes both decreases and increases: pruned history falls 11.85%, while
history-64 rises 1.29% and the single-operation shape rises 0.72%. Model/signature controls
include small allocation changes. Most elapsed medians are higher, including complete CBOR
recovery at 512 EVSEs (2659.444 -> 3308.712 ms) and the large batch (2119.726 -> 2495.105 ms).
These data establish no general speedup or total-memory reduction.

The C# harness source and preserved binary, dependency bytes, runtime configuration, prepared
inputs, original peers, recovered branch states, output identities and local runtime checks
match. Before uses preserved current model-preparation binaries. After replaces only the
production library/PDB in a separate private folder. Workers are sequential and do not overlap
task builds/tests. The shared Windows desktop has no dedicated CPU or affinity control.

The signature-only probe invokes individual callbacks without a preparation scope, so it
is an unchanged per-peer preparation control rather than a measurement of scoped peer reuse. Model-only boundary decoding is
eager; actual v3/v4 recovery remains lazy. Stage medians cannot be added or interpreted as shares
of complete recovery. Managed allocations are cumulative operation-thread allocations, not
peak live memory. Post-measurement collected-history deltas are approximate and do not bound
total memory or production throughput. There is no persistent preparation cache.

[The validated summary](performance/canonical-preparation-summary.md),
[source/binary/test/reference bindings](verification/canonical-preparation-results.json) and
[reproduction commands](../WWCP_POI_Benchmarks/README.md#canonical-identitysignature-preparation-measurements)
retain the exact evidence and worker arguments. The scoped reverse patch reconstructs the
before source bytes in a private checkout; fixed interoperability artifacts stay unchanged.

## Remaining work

Root/head reconstruction, individual JSON/CBOR trees, projections, immutable maps and retained
versions still allocate. The subsequent [signature-copy package](SIGNATURE-COPIES.md) reuses
validated IDs and bounded scoped content when only peer envelopes change, with full constructor
fallback for independent arrays or changed unsigned data. Its evidence is separate from this baseline.
Finer cancellation inside model/scalar parsing, richer tariff/parking/reference workloads,
production concurrency and persistent indexes remain separate candidates.

The subsequent [parser-cancellation package](PARSER-CANCELLATION.md) adds checks within owned
syntax/limit loops and around individual model/replay calls. Those individual calls remain
synchronous; finer cancellation within their internals remains separate work.

The subsequent [root/head reconstruction package](SNAPSHOT-RECONSTRUCTION.md) conditionally
retains exact immutable snapshots after full model validation and completion. It preserves this
package's contracts and conservative capture fallback; the measurements above remain historical.
