# Static content profiles and interoperability

[Repository overview](../README.md) · [ETags and CBOR](ETAGS-CBOR.md) · [History](HISTORY.md) · [Reference files](interoperability/README.md)

## Profile declarations

The static content contract is **`wwcp-poi-static-v2`**, exposed as `POIContentProfile.Id`.
It fixes the stored-property projection, normalization, canonical JSON and metrological CBOR
used by both state identifiers. Changes that alter these bytes require a new profile identifier
and reference vectors; dependency updates must pass the existing vectors first.

Static-v2 adds network software/certificate registries and operator-owned parking products,
replaces point operator documents with ID references and introduces parking membership/garage/
product references. Tagged static-v1 documents and archives are rejected, including their batches
and signatures. Current vector files were regenerated successfully. Dedicated shared-model tests
pass **73 cases**. The full static-v2 run passes **1,025 tests**, with zero failures and one
ordinary worker skipped; see [model verification](VERIFICATION-DOMAIN-MODEL.md). That execution
predates the streaming archive changes. Their [measured byte/identity preservation](STREAMING-ARCHIVES.md)
uses matching static-v2 workloads. The later [streaming verification](VERIFICATION-STREAMING.md)
checks all four profiles and fixed references, then reruns the full suite with 1,174 passed,
zero failures and one skipped worker.
See [domain model](DOMAIN-MODEL.md) and
[the historical snapshot execution record](VERIFICATION-SNAPSHOTS-RETENTION.md).

Complete tagged POI documents declare the profile in a schema-owned `contentProfile` property:

```json
{
  "@id": "interop-network",
  "contentProfile": "wwcp-poi-static-v2",
  "ETags": [
    ["json", "sha256", "hex", "<64 lowercase digits>"],
    ["cbor", "sha256", "hex", "<64 lowercase digits>"]
  ]
}
```

This excerpt omits static properties and is not a complete reference snapshot. CBOR carries
the same text profile and native digest bytes. Each schema-owned document with declared ETags
must declare this supported profile when imported through the complete JSON/CBOR parsers.
Fresh domain input without ETags may omit it and is interpreted under the current profile.
Unknown profiles and non-string declarations are rejected; no alternate profile is guessed.
Ordinary domain view parsers do not verify ETags; use `RoamingNetworkDataSnapshot.Parse`,
typed `ParseCBOR` or `POIRepresentation.ParseJSON` for complete transport validation.

The declaration is transport metadata and is removed from static storage and digest inputs,
like revision bookkeeping. A customer field named `contentProfile` inside `customData` is
ordinary immutable content and remains in both hashes.

Commits and history archives require a `ContentProfile` header. For commits this field is
inside `GetIdentityBytes()` and therefore bound by their deterministic IDs and ancestry
signatures. The archive header must match the supported checkpoint/commit contract.
The profile declaration preserves static ETag inputs; adding it to commit identity changes
commit IDs from development builds that lacked this field. Those commit/archive envelopes
must be prepared again from their static snapshots and signed; no missing-header fallback exists.

| Contract | Profile | Fixed relationship |
| --- | --- | --- |
| Static POI projection and encodings | `wwcp-poi-static-v2` | State JSON/CBOR ETags |
| ChangeSet signing | `wwcp-poi-changeset-json-v2` | Applies to the current static-v2 transition contract |
| Commit identity | `wwcp-poi-commit-json-v1` | Includes `ContentProfile`, ordered parents and unsigned v2 batch |
| Commit signing | `wwcp-poi-commit-signature-json-v1` | Binds identity and complete unsigned commit |
| History archive | `wwcp-poi-history-v1` | Requires static-v2 checkpoint and commit content |
| Three-way merge audit | `wwcp-poi-three-way-merge-v1` | Explicit preparation decisions in signed batch metadata |

Full snapshot links additionally use `wwcp-poi-snapshot-commit-json-v1` and
`wwcp-poi-snapshot-commit-signature-json-v1`. Archives/pages containing them select
`wwcp-poi-history-v2`/`wwcp-poi-commit-pack-v2`; bootstrap manifests bind history-v2 through
`wwcp-poi-bootstrap-manifest-v2`. The container/profile names remain unchanged; their `ContentProfile` header now binds static-v2.
The current [snapshot regression references](interoperability/snapshot-retention.vectors.json)
have been regenerated along with the ordinary references. See [snapshots](SNAPSHOTS.md).

[Snapshot boundaries](SNAPSHOT-BOUNDARIES.md) add history-v3, manifest-v3, replication-state-v2
and commit-pack-v3 without changing original commit/snapshot identity/signature preimages. Manifest
identity binds the original checkpoint claim and anchor; boundary authorization is mandatory because
omitted ancestry cannot independently prove chain association. Pages resolve root ID/peers against
local retained content. Fixed boundary/page references and executed workflow cases are described in
[snapshot/retention verification](VERIFICATION-SNAPSHOTS-RETENTION.md).

[Explicit retention](RETENTION.md) additionally introduces `wwcp-poi-retention-plan-v1`,
`wwcp-poi-retention-receipt-v1`, history-v4 and manifest-v4. Plans bind the complete source archive;
native JSON/CBOR receipts retain removed IDs and the digest of that archived source. The receipt
identity uses canonical JSON excluding `Id`; it is unsigned bookkeeping. Root signatures do not
authenticate the catalog. Existing identity/signature/page/chunk contracts are unchanged without
these catalogs. Fixed retention/archive/manifest references and failure/recovery cases now pass.
The references are local regression evidence; independent peer interoperability remains open.

## Static projection

Both hashes read the same authoritative immutable property document. They do not rebuild
optional values through a domain serializer. Their input includes the complete owned graph,
static timestamps, wire identifiers, source, descriptions, quantities, custom data and legal
transparency-software/certificate data.

1. Owned graph children are expanded, grouped by their defined relationship and ordered by
   ordinal wire ID. Empty owned graph relationships are materialized as `[]`. Validated inherited
   EVSE station views (`address`, `authenticationModes`, `openingTimes`) are excluded from owned
   EVSE properties; the station retains its own content.
2. Schema-defined creation/change timestamps are initialized once when absent/null, then
   normalized to UTC round-trip text with seven fractional digits and explicit offset. Initial
   imports without timestamps can generate different static states; distribute a complete
   checkpoint when replicas must start identically. Added-node defaults use batch `CreatedAt`.
3. Valid optional absence, JSON null, zero, false and empty value collections remain distinct.
   Owned graph-array and managed timestamp defaults are the explicit exceptions above.
4. Schema quantities are parsed by dimension and rendered using the typed Styx SI formatter.
   For example, `100000 W` and `0.1 MW` become `100 kW`. Numeric/unitless readings are rejected.
   Unknown customer strings that look like readings remain text.
5. Schema timestamps are normalized; customer timestamps/text are preserved. Identifier wire
   spelling remains content even where addressing uses a normalized domain identity. Unicode
   text is not normalized to NFC/NFD.
6. Group/reference arrays and value arrays retain their defined ordering. Addressed operations
   sort the identified collections they touch; import does not arbitrarily sort every array.
   Tariff element/restriction/component ordering remains content.
7. Schema-derived `ETags`, `contentProfile`, runtime statuses/histories, measurements, forecasts,
   private keys, internal data and root revision/applied-batch bookkeeping are excluded.
   Exclusion follows schema paths; similarly named customer fields remain content.

Arrays and objects use their ordinary JSON presence semantics. The applier's old-value matching
and addressed identity rules are documented separately in [ChangeSets](CHANGESETS.md) and
[element operations](ELEMENT-OPERATIONS.md).

## Canonical bytes

### JSON state identifier

`ToCanonicalJSON()` returns exactly the UTF-8 bytes hashed by the JSON ETag:

- Styx `CanonicalJSON.ToUTF8Bytes` over the projected document.
- Ordinal object-property ordering, original array ordering and no insignificant whitespace.
- No BOM or trailing newline; invariant numeric formatting and deterministic string escaping.
- Exact supported integer/Decimal values, including Decimal scale. The domain string importer
  reads decimals without a binary-float intermediary. Source exponent spelling can normalize
  at this domain boundary; static hashing is not a promise to retain every source number lexeme.
- SHA-256 over those bytes; lowercase HEX is the default textual digest encoding.

This is the Styx canonical JSON contract; it is not an RFC 8785/JCS claim.
Passing a `JObject` previously parsed into Double cannot recover precision already lost.
Use the string snapshot parser for the exact decimal import boundary.

### CBOR state identifier

`ToCanonicalCBOR()` returns the corresponding deterministic CBOR digest input:

- The same static hierarchy and text property names, encoded by Styx with
  `CBORWriterOptions.Canonical`.
- Schema-selected SI strings become metrological tag **44252**, retaining the unit reference
  and scale. Customer strings stay text. The reference fixture covers W, VA, V and Hz.
- JSON integers and exact decimals use the corresponding integer/bignum or decimal-fraction
  encoding. Known schema timestamps remain their normalized text values.
- Digest inputs contain no derived ETag tuples, profile declarations or runtime/bookkeeping.

The binary reference files fix exact unit references, number encodings and map ordering for
the supplied cases. Received valid alternative encodings are reconstructed and checked against
the canonical content, rather than hashing their incidental transport ordering.

### Signed batch and commit bytes

Unsigned batches preserve exact JSON number lexemes, SI spelling, optional old/new presence,
operation order and customer metadata. The ChangeSet CBOR codec must reproduce these signed
bytes exactly. Its tag 262 Number wrapper preserves numeric spellings that native encodings
would change. Header ETags use binary tuples; customer/audit ID-like arrays remain ordinary JSON.
See the complete [batch CBOR contract](CHANGESET-CBOR.md).

Commit IDs always hash their canonical **JSON** identity preimage, including `ContentProfile`.
Their CBOR tuple therefore remains `["json", "sha256", h'<32 bytes>']`.
Batch and commit peer arrays stay outside commit identity. Adding a peer changes the transport
envelope while preserving the commit ID and earlier signatures. The published vectors use
Ed25519, two equal peers, multilingual descriptions and application metadata.

## Executable reference workflow

The new [interoperability tests](../WWCP_POI_Tests/Interoperability) execute:

- Independent imports of one fixed checkpoint with different runtime statuses and equal static IDs.
- Signed transition exchange via JSON and CBOR, including both before/after digest checks.
- Published divergent branches, retained foreign commits and explicit three-way preparation.
- Structured conflict values, custom SI resolution and removal versus explicit JSON null.
- Fresh batch/commit signatures over integration and audit metadata.
- Full signed archive bootstrap, JSON/CBOR recovery, continued exchange and duplicate delivery.
- Expected-head races, required signer policy, invalid signatures/ancestry and conflicting batch IDs.
- Persistence failure before replacement, exclusive writer leases and abrupt child-process exit
  immediately before/after atomic replacement. Recovery accepts exactly the previous/new archive.

The crash tests terminate a child process without unwinding its archive/disposal blocks. They
provide process-interruption evidence; they do not simulate power loss or filesystem damage.
[Signed snapshot and retention exits](CRASH-RECOVERY.md) add 26 passing cases across complete and
previously pruned histories, including cold publication/flush, exact active root/catalog recovery,
original peers and digest-checked older archives. Missing acknowledgements are handled by exact
candidate/plan identity: snapshot re-delivery is idempotent, and a completed pruning receipt prevents
blind replay of a stale plan. Both recovery paths continue signed exchange with fresh runtime.
Exhaustive graph/merge combinations and independent implementation interoperability remain additional
work. Separate [scaling measurements](SCALING.md) preserve state/archive/operation identities and
byte counts while measuring digest reuse and local archive/runtime/cold paths.

## Receiving a merge at a different head

`TryPublish` requires its first parent as the current head. The separate `TryAdoptHead` previews
and explicitly selects a retained descendant through every parent edge. It accepts an incoming
integration over its right parent, preserving the original signed commit and carrying only local
runtime with proven lifetime continuity. Divergence returns a notice requiring explicit merge.

[Incremental replication](REPLICATION.md) announces retained DAG tips, exports missing ancestry
in bounded JSON/CBOR pages and atomically imports each page without selecting a remote head.
Different checkpoints require explicit separate [bootstrap](BOOTSTRAP.md). Frozen archive manifests
and bounded fragments now support verified disk receipts, restart and validated activation.
Signature verification
alone does not select authorized keys, distinct signers or quorum; configure application trust.

The existing reference workflow still exercises separate full archive bootstrap and receiver
runtime delivery. New dedicated [replication/adoption fixtures](REPLICATION.md#implementation-and-evidence)
add 61 passing cases for bounded JSON/CBOR pages, atomic failure, original signed merge adoption,
local runtime lifetimes, changed trust, head races and file persistence/recovery. The current
[static-v2 full-suite run](VERIFICATION-DOMAIN-MODEL.md) also reruns operation-history lifetimes,
explicit temporary reference plans and all fixed vector assertions. Bootstrap adds
32 passing cases for both wire formats, receipt failures, corruption, replay/trust rejection,
explicit activation, fresh runtime and continued incremental exchange.
[Bootstrap crash recovery](BOOTSTRAP-CRASH-RECOVERY.md) adds 89 cases, including 36 real exits across
all archive profiles and explicit exact-destination recovery with fresh trust under its writer lease.
Those packages retained existing wire profiles and identities at the then-current static-v1 baseline.
The current static-v2 model change regenerates the fixed state/commit/signature artifacts.
[Cold archive retrieval](COLD-ARCHIVES.md) adds 98 cases for moved local files, immutable location
catalogs, exact source digests, current authority/replay, receipt membership and older receipt chains.
It adds no wire profile and preserves source bytes, active history/runtime and receipt identities.
The [structural merge fixture](MERGING.md#structural-merge-evidence) adds 34 passing cases for
deletion/recreation/ownership, complete typed reference diagnostics, group/parking/connector scopes,
signed subtree choices, criss-cross ancestor selection, resolver reentry/exception rollback and
the formerly rejected referenced replacement, whose revised expectation now prepares explicit
detachment/restoration and now passes. Reference decisions optionally bind `RelatedEntity` in merge
audit metadata; unchanged scenarios retain their existing identity/signature/reference bytes.
Streaming archive replay, independent implementations, transport authentication and key
negotiation remain additional work. Current fixed references use static-v2.

Temporary reference plans add an optional `wwcp-poi-reference-transition-v1` audit section through
the existing lossless batch metadata codecs. New transition vectors and broader coverage remain
planned; ordinary merges without temporary steps retain their existing metadata shape.

## Running and updating references

The [archive-limit package](ARCHIVE-LIMITS.md) adds 60 passing cases for complete/boundary/pruned
recovery through JSON/CBOR, direct/cold files and bootstrap. Local budgets cover encoded bytes,
retained roots/commits, receipts and aggregate catalog-ID entries before materialization/replay;
structured rejection keeps active state unchanged. Archive limits do not introduce a content profile.

Ordinary tests compare committed expectations and never rewrite them:

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj --no-restore
```

The explicit generator publishes reviewed artifacts from fixed inputs:

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj --no-restore --filter 'FullyQualifiedName~GenerateReferenceVectors'
```

Review the resulting byte/digest/signature diff. A changed existing reference is a contract change,
not a reason to silently refresh passing expectations. The next ordinary build copies the files
into the test output and the full run verifies them under multiple cultures and across processes.
