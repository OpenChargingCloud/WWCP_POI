# Static-v2 reference files

[Profile and byte contracts](../INTEROPERABILITY.md) · [Test coverage](../../WWCP_POI_Tests/README.md)

These fixed files are inputs and expected outputs for **`wwcp-poi-static-v2`**, the v2
ChangeSet signing contract and the v1 commit/history/merge contracts. HEX fields in
[vectors.json](vectors.json) describe exact bytes; JSON document fields contain their full
wire content. Stored JSON artifacts have no BOM or trailing newline. Manifest formatting
does not participate in any hash.

| Files | Purpose |
| --- | --- |
| [snapshot.input.json](snapshot.input.json) | Fixed hierarchy, shared software/document catalogs and meter assignments, parking garage/overlapping groups/products, timestamps, optional null, custom decimals/text and W/VA/V/Hz readings |
| [snapshot.canonical.json](snapshot.canonical.json), snapshot.canonical.cbor | Exact SHA-256 state digest inputs |
| [snapshot.json](snapshot.json), snapshot.cbor | Versioned snapshot with profile and root ETags |
| [changeset.json](changeset.json), changeset.cbor | Two signed peers, multilingual text, application metadata and lossless numeric/SI spelling |
| [commit.json](commit.json), commit.cbor | Signed first-parent transition and deterministic JSON commit identity |
| [merge.json](merge.json), merge.cbor | Disjoint-branch integration with two parents and signed audit metadata |
| [resolved-merge.json](resolved-merge.json), resolved-merge.cbor | Conflicting power edits resolved explicitly to `175000 W`, normalized result `175 kW` |
| [history.json](history.json), history.cbor | Signed checkpoint plus both alternatives/integrations; published disjoint-merge head |

The manifest also publishes canonical identity/signing bytes, state ETags, commit IDs,
Ed25519 public keys and expected signatures. Fixture Alice's seed is bytes `00` through `1f`;
Bob's is `20` through `3f`. They are public reproducibility inputs. Both key IDs and all
fixture timestamps are fixed. There are six archived commits including the checkpoint.

The generator and ordinary vector assertions are in
[InteropFixture.cs](../../WWCP_POI_Tests/Interoperability/InteropFixture.cs) and
[InteroperabilityTests.cs](../../WWCP_POI_Tests/Interoperability/InteroperabilityTests.cs).
The manifest JSON examples are canonicalized for reproducibility; ordinary `ToJSON` transport
writers are not required to use that property order. Parsers accept valid ordering and compare
canonical identities.

These vectors cover representative transport/signing/merge cases. They do not claim every
domain relation, cryptographic algorithm or merge topology is covered.

## Snapshot and retention regression references

[snapshot-retention.vectors.json](snapshot-retention.vectors.json) adds 31 named fields for the
snapshot identity/signing profiles, history-v2/v3/v4, page-v3, announcement-v2, retention plans/receipts
and manifest-v4. Fifteen companion artifacts contain exact bytes:

| Files | Purpose |
| --- | --- |
| snapshot-commit.json/cbor | Original full snapshot and both equal Ed25519 peers |
| snapshot-complete-history.json/cbor | Complete signed checkpoint/snapshot/suffix history |
| snapshot-boundary-history.json/cbor | Original chain claim, signed root and retained suffix |
| snapshot-suffix-page.json/cbor | Local snapshot root ID/peers and later transition |
| retention-plan.json | Reviewed exact source archive and retained/pruned IDs |
| retention-receipt.json/cbor | Unsigned pruning bookkeeping and prior archive ETag |
| pruned-history.json/cbor | Boundary archive plus receipt catalog |
| pruned-bootstrap-manifest.json/cbor | Exact pruned archive digest and bounded fragment layout |

The same public fixture keys, fixed input hierarchy and timestamps are used. Cold paths do not
enter identities. SnapshotRetentionReferenceTests compares all fields under `en-US`, `de-DE` and
`ar-EG`, recovers original signed archives, hashes published preimages directly and verifies both
snapshot signatures directly using BouncyCastle. The ordinary run never rewrites these files.
`GenerateSnapshotRetentionVectors` is an explicit deliberate generator in that fixture.

These are fixed local regression references; independent peer interoperability remains open.
Both manifests and all 29 companion artifacts were regenerated for static-v2 on 2026-10-08,
including state digests, commit IDs and signatures. The ordinary static-v2 run is recorded in
[current model verification](../VERIFICATION-DOMAIN-MODEL.md). Earlier
[executed evidence and limits](../VERIFICATION-SNAPSHOTS-RETENTION.md) records the preceding
static-v1 baseline.
