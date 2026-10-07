# Static-v1 reference files

[Profile and byte contracts](../INTEROPERABILITY.md) · [Test coverage](../../WWCP_POI_Tests/README.md)

These fixed files are inputs and expected outputs for **`wwcp-poi-static-v1`**, the v2
ChangeSet signing contract and the v1 commit/history/merge contracts. HEX fields in
[vectors.json](vectors.json) describe exact bytes; JSON document fields contain their full
wire content. Stored JSON artifacts have no BOM or trailing newline. Manifest formatting
does not participate in any hash.

| Files | Purpose |
| --- | --- |
| [snapshot.input.json](snapshot.input.json) | Fixed hierarchy, timestamps, optional null, custom decimals/text and W/VA/V/Hz readings |
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
