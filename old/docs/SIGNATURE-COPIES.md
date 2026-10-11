# Immutable signature-envelope copies

## Identity and validation

`RoamingNetworkCommit` is immutable. Its identity binds the network, revision, ordered parents,
state identifiers and unsigned ChangeSet or snapshot content. Commit peer signatures and embedded
ChangeSet peer signatures are excluded from that identity. All signature peers remain equal.

`WithSignatures`, `WithSignature` and the copy produced by `Sign` now carry the source's already
validated commit ID and immutable payload. The copy still normalizes a default signature array to
empty and rejects null entries with the preceding exception. It does not verify authenticity:
verification and every current application policy remain separate operations, as before.

`WithChangeSet` uses the copy path only when its unsigned content is proven unchanged:

- The source and replacement are the same instance, or every following condition holds.
- IDs, base revision and UTC timestamp match exactly.
- Immutable operation and before/after ETag arrays share their original backing arrays, preserving
  the previously validated values and operation order.
- Description keys and text match ordinally; metadata keys and raw JSON spelling match exactly.

Signature copies of a ChangeSet detach their metadata using its existing constructor. Exact value
comparison accommodates those copies without changing the record's equality/hash behavior. No
fields or copy semantics of `RoamingNetworkChangeSet` change in this package. Metadata comparisons
still scan and allocate raw text proportional to their size.

Independent operation arrays, changed metadata spelling or changed unsigned headers use the full
preceding commit constructor. That constructor validates the header, serializes and canonicalizes
the unsigned payload, computes its SHA-256 ID and checks the original declared ID. Canonically
equivalent replacements are still accepted by that fallback. Changed content, invalid operation
values, depth errors and declared-ID mismatches retain their preceding validation order/errors.
Checkpoints and snapshot commits still reject `WithChangeSet`; snapshot metadata edits and parsed
JSON/CBOR commits retain full construction and validation.

Any future unsigned ChangeSet property must also be covered by the exact copy proof before this
optimization can admit replacements carrying that property.

## Scoped canonical bytes and ownership

When the source already owns an entry in the active synchronous
[canonical preparation scope](CANONICAL-PREPARATION.md), its successful copy can share that private
immutable content entry. Sharing never prepares an absent source and retains no payload outside
the scope. It caches neither a key nor a verification/authorization decision.

Each alias conservatively consumes another entry and its full byte length against the unchanged
64-entry, 1-MiB-per-value and 4-MiB-total admission limits. Saturation uses fresh preparation for
later byte requests. The outer scope releases every alias on success or failure; independent
threads/readers have independent scopes. Public identity/signing arrays remain detached.

Runtime values remain outside commit content. Envelope unions in duplicate delivery and incremental
pack import still check current incoming and merged peers, preserve their order/deduplication and
publish atomically. They retain existing static snapshots and local runtime objects. Recovery
creates fresh local runtime and checks every current peer and boundary authority.

## Regression evidence

`SignatureCopyTests` adds 103 cases. The unchanged full constructor supplies an independent copy
route; frozen preceding canonical writers/factories check exact IDs and signing preimages. Tests
cover all three commit kinds, default/empty/reordered/repeated peers, deterministic signatures,
detached public arrays, record equality, independent parsed batches, every unsigned field,
canonical-equivalent metadata, invalid operations, depth errors, declared IDs, admission bounds,
thread separation and cleanup. JSON/CBOR recovery covers all four history profiles. Duplicate
store/publication and incremental-pack unions cover fresh commit/batch policy, atomic rejection,
successful retry, original peer order, retained static state and independent recovered runtime.

The [execution/source/binary/reference evidence](verification/signature-copies-results.json) records
the final fixture, interoperability and complete Release runs. Fixed reference files remain
unchanged. The initial fixture's twenty failures came from requiring JSON insertion order after
CBOR transport; both preceding/current routes already normalize it. That assertion now compares
the two original/current transport routes. Both explicit generators remain excluded.

## Measurements and limits

The [matched comparison](performance/signature-copies-summary.md) uses four shapes with 1, 64, 512
or 2,048 operations, one/four peers, and five operations: checkpoint, ChangeSet and snapshot
signature copies, embedded batch peer replacement and real Ed25519 commit signing. Each measured
call creates sixteen independent copies from one source. Two fresh processes, three warmups and
three samples per group/side give 80 workers and 240 measured calls in total.

The same newly added C# harness binary, runtime configuration and dependency bytes run against
preserved preceding/current POI libraries. Full-constructor oracle comparison, complete JSON/CBOR
bytes and cryptographic verification are outside measurement. Inputs, commit IDs and complete
signed output digests match across processes and builds. No canonical scope is active, so the
signing probe retains the preceding individual signing-byte factory as a control. A prepared
replacement batch is supplied: cloning that batch's descriptions/metadata is excluded.

Raw [before](performance/signature-copies-before.json) and [after](performance/signature-copies-after.json)
reports bind [before sources/binaries](verification/signature-copies-before-binding.json) and
[after sources/binaries](verification/signature-copies-after-binding.json). The
[scoped production patch](performance/signature-copies.diff) reverses to the exact preceding raw
source bytes in a private checkout. Portable launch/report scripts verify the fixed method,
complete matrix, every repeated inventory/result and matching harness/dependencies.

For sixteen snapshot signature copies at 512 EVSEs, median allocation changes
84.7071 -> 0.0015 MiB (about 1.5 KiB after). For sixteen embedded peer replacements in the
2,048-operation shape it changes 137.6590 -> 0.0052 MiB; prepared batch metadata cloning is
excluded. Real commit signing allocation falls 44.71–48.93% across the four shapes; at
2,048 operations it changes 277.0092 -> 143.3433 MiB. It retains real unsigned signing-byte
preparation and Ed25519 costs. All time medians are lower in this shared-desktop series;
they establish no production latency or throughput guarantee. Table percentages use four
decimal places to avoid displaying a nonzero allocation as a complete 100% removal.

These probes measure the eliminated copy work. They do not establish end-to-end recovery,
production throughput, retained-memory or concurrency improvements. Cryptography, individual
signing-byte preparation, independent parsing, batch metadata copies and changed unsigned content
remain costs. Shared-desktop times and sampled peaks are descriptive.

## Next work

The subsequent [parser-cancellation package](PARSER-CANCELLATION.md) adds owned-loop checkpoints
while preserving full input validation, trust/error timing, private recovery, local budgets and
cleanup. The subsequent [domain recovery package](DOMAIN-RECOVERY-WORKLOADS.md) measures shared
references, nested tariffs, parking and signed merges as a new baseline with unchanged production.
Normalization-aware map reuse, finer checks within individual parsers, production concurrency
and persistent indexes remain separate candidates.
