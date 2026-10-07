# Signatures and trust

[Repository overview](../README.md) · [Architecture](ARCHITECTURE.md) · [ChangeSets](CHANGESETS.md)

## Implemented today

`RoamingNetworkChangeSet.Sign` and `TrySign` generate real asymmetric signatures using Styx's
`COSEAlgorithm` primitives. They accept Bouncy Castle asymmetric keys or Styx `COSEKey` objects.
The built-in input profile is `wwcp-poi-changeset-json-v2`, with Styx canonical JSON UTF-8 bytes.
Styx supplies ECDSA, EdDSA and ML-DSA signing/verification for its supported algorithms and keys.
ECDSA signing defaults to deterministic RFC 6979; `deterministic: false` selects randomized signing.

`Signatures` is an `ImmutableArray<RoamingNetworkChangeSetSignature>` of **equal peers**. Empty
means unsigned. Every `Sign(...)` appends a signature and preserves existing signatures; no entry
is designated primary. Each envelope contains five strings:

| Field | Intended use |
| --- | --- |
| `Algorithm` | Canonical Styx COSE algorithm name, e.g. `Ed25519` or `ESP256` |
| `KeyId` | Identifier used by an application to resolve a verification key |
| `Value` | Signature bytes in canonical standard Base64 |
| `Profile` | `wwcp-poi-changeset-json-v2` for the built-in profile |
| `Encoding` | `base64` for the built-in profile |

Envelope construction rejects empty fields. Built-in verification additionally checks the profile,
canonical algorithm label, encoding, Base64 and cryptographic signature. ECDSA bytes use the
Styx COSE primitive's fixed-width `r || s` representation. This profile uses those primitives
directly; the envelope is not a serialized COSE_Sign/COSE_Sign1 message.

Private keys are never retained in a ChangeSet or exported. Applications resolve trusted public
keys and decide which signers may modify their data. `WithSignature` appends an externally generated
envelope; `WithSignatures` explicitly replaces the array and `WithoutSignatures` removes it.

The applier implements this control flow:

| Batch | Behavior |
| --- | --- |
| No signature | Accepted without calling the signature verifier, subject to ordinary validation |
| Signatures present, no verifier | Rejected |
| Any signature's verifier returns false | Rejected |
| Any signature's verifier throws | Rejected; failure is wrapped with batch/signature context |
| Every signature's verifier returns true | Continue with operation/domain validation |

Target-network, revision and both `BeforeETags` checks happen before the verifier. Signature verification happens
before any ChangeSet operation. The callback receives `(batch, signature)` for each entry;
failures identify `Signatures[index]` and its KeyId. Valid signatures do not bypass field, ownership, old-value or
tariff-reference validation. Both `AfterETags` are checked on the candidate after timestamp
updates; any mismatch rejects the batch without returning a successor.

## Descriptions, metadata and signing

Commit descriptions use `ImmutableDictionary<String, String>` language labels and arbitrary
Unicode text. `WithDescription(language, text)` adds/replaces one translation; overloads replace
all translations or copy an Illias `I18NString`. Metadata uses
`ImmutableDictionary<String, JsonElement>` with arbitrary nonempty, case-sensitive keys and
defined JSON values. `WithMetadata(key, value)` accepts a JsonElement or serializes a typed value;
the dictionary overload replaces all entries. Values are cloned, including nested arrays/objects.

Both properties belong to the ChangeSet and are signed. They do not alter the infrastructure
snapshot or its POI ETags. Each description/metadata edit returns an **unsigned** copy, clearing
all previous signatures; sign the edited content again. The original batch remains unchanged.

```csharp
using Org.BouncyCastle.Crypto;
using org.GraphDefined.Vanaheimr.Illias;
using cloud.charging.open.protocols.WWCP.POI;

// Private/public keys are supplied by the application's key store.
var signed = source.CreateChangeSet("change-42", DateTimeOffset.UtcNow, [/* operations */])
    .WithDescription("de", "Maximalleistung angepasst")
    .WithDescription("en", "Updated maximum power")
    .WithMetadata("acme:ticketId", "INC-4711")
    .WithMetadata("acme:approval", new { department = "operations", approved = true })
    .Sign(alicePrivateKey, "acme:alice", COSEAlgorithm.Ed25519)
    .Sign(bobPrivateKey,   "acme:bob",   COSEAlgorithm.Ed25519);

var trustedKeys = new Dictionary<string, AsymmetricKeyParameter> {
    ["acme:alice"] = alicePublicKey,
    ["acme:bob"]   = bobPublicKey
};

bool VerifyPeer(RoamingNetworkChangeSet batch, RoamingNetworkChangeSetSignature signature)
    => trustedKeys.TryGetValue(signature.KeyId, out var key) &&
       batch.VerifySignature(signature, key, signature.KeyId, out _);

var next = source.ApplyChangeSet(signed, VerifySignature: VerifyPeer);

// Standalone verification of all peers, without applying operations:
var valid = signed.VerifySignatures(
    signature => trustedKeys.GetValueOrDefault(signature.KeyId), out var error);
```

`VerifySignature(signature, publicKey, expectedKeyId, out error)` verifies one member of this
batch's array against a trusted key/ID. `VerifySignatures(resolver, out error)` verifies all
entries and returns false for unsigned batches. `TrySign` returns an error and no partial batch
if key/profile/canonicalization/signing fails. None of these methods apply operations; domain
validation and expected-state checks still occur during preparation/application.

## Integrating an application policy

This wrapper enforces a signed-only policy at an application boundary:

```csharp
using cloud.charging.open.protocols.WWCP.POI;

static RoamingNetwork ApplySigned(
    RoamingNetwork source,
    RoamingNetworkChangeSet changeSet,
    Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, bool> verify)
{
    if (changeSet.Signatures.IsEmpty)
        throw new InvalidOperationException("This endpoint requires a signed ChangeSet.");

    return source.ApplyChangeSet(changeSet, VerifySignature: verify);
}
```

Here `verify` must be supplied by the application's actual verification implementation.
A callback returning true unconditionally only satisfies the hook; it establishes no authenticity.

The core applier accepts unsigned batches. Passing a verifier alone does not enforce that all
batches are signed, because the callback is not invoked for an unsigned batch. If an application
requires particular signers or a minimum number of distinct trusted keys, it enforces that policy
as well. Each peer signs the batch content and its own header, while the signature array itself
is excluded to allow independent additional signatures and reordering.

## Canonical POI content and ChangeSet signing

The repository has two JSON layers: Newtonsoft.Json POI documents and System.Text.Json ChangeSet
documents. Serializer options can change naming, escaping, whitespace and other representations.

POI content identity and ChangeSet signature input have separate profiles. Equal domain IDs can
have different wire spellings, and equivalent SI unit representations can differ from normalized
output. The ChangeSet profile retains the operation payload's exact logical strings and numeric
tokens instead of rebuilding operations from the resulting domain objects.

Static domain immutability does not freeze runtime statuses. `ToJSONSnapshot()` includes current
runtime statuses and can therefore produce different bytes at the same POI revision. This profile
signs the frozen operation payloads and before/after static state identifiers. Signing a ChangeSet
does not authenticate runtime updates, including `RoamingNetworkRuntimeUpdate` instructions.
The [runtime API](RUNTIME.md) has no built-in signature envelope; its authentication belongs to
the application's runtime channel.

### `wwcp-poi-changeset-json-v2` input

`GetSigningBytes(algorithm, keyId)` builds the following fixed schema, then serializes it using
Styx `CanonicalJSON.ToUTF8Bytes(JsonDocument)`:

```json
{
  "Profile": "wwcp-poi-changeset-json-v2",
  "Algorithm": "Ed25519",
  "KeyId": "acme:alice",
  "Encoding": "base64",
  "ChangeSet": {
    "Id": "change-42",
    "RoamingNetworkId": "network-a",
    "BaseRevision": 0,
    "CreatedAt": "2026-10-07T10:30:00.0000000+00:00",
    "BeforeETags": ["<JSON and CBOR four-element HEX tuples>"],
    "AfterETags": ["<JSON and CBOR four-element HEX tuples>"],
    "Description": { "de": "Maximalleistung angepasst", "en": "Updated maximum power" },
    "Metadata": { "acme:ticketId": "INC-4711" },
    "Changes": ["<complete ordered operation objects>"]
  }
}
```

The placeholders above describe array contents, not literal signing values. The profile specifies:

1. Fixed PascalCase field names, independent of transport serializer options; no `Signatures` field
   or signature `Value` participates. Profile/algorithm/key ID/encoding of the current peer are bound.
2. Ordinal object-key ordering, compact UTF-8 JSON and Styx string escaping; arrays retain order.
   JSON numeric tokens, ID spelling and unit strings are preserved. `1`, `1.0` and equivalent SI
   spellings need not produce identical signature input. Unicode normalization is not performed.
3. UTC `CreatedAt` in invariant .NET `O` format, with seven fractional second digits.
4. Each ETag is `[format, algorithm, "hex", lowercaseDigest]`. Transport Base64 ETags normalize
   to the same HEX signing representation after parsing.
5. `Description` and `Metadata` are always objects, including `{}` when empty. Metadata JSON null
   remains a value. Old/new operation values are omitted when absent and emitted when explicitly
   JSON null. Optional property/parent fields are omitted when absent.
6. Every operation includes kind/type/ID, `ElementPath` (always an array, empty for graph operations)
   and its applicable property, parent scope and old/new values. Each path segment binds its exact
   `PropertyName` and supplied `ElementId`; path array order is retained.
   Duplicate object member names anywhere in payloads/metadata and undefined values are rejected.

The v2 profile binds the newly introduced element addressing contract. Built-in verification does
not accept the previous profile; batches prepared under it need new signatures. Signature envelopes
and equal-peer behavior are otherwise unchanged. See [element operations](ELEMENT-OPERATIONS.md).

The repository also provides canonical POI JSON/CBOR bytes and SHA-256 content identifiers as
specified in [ETags and CBOR](ETAGS-CBOR.md). Those exclude runtime state and revision bookkeeping;
the ChangeSet signing profile binds the batch header, descriptions/metadata and all operation values.

## Authenticity, authorization and domain validation

These are separate checks:

- Signature verification establishes that the agreed payload verifies with a particular key.
- Authorization decides whether that key may change this network, operator, entity or property.
- Domain validation decides whether the resulting infrastructure data is structurally and
  semantically acceptable to the model.

The library supplies cryptographic verification helpers, the per-signature hook and domain validation. It does not implement a
signer-to-operator permission model. Applications can enforce trust and authorization before
application or within their verification boundary.

Transparency-software certificate fields are domain metadata. Parsing those strings and validity
intervals does not validate a certificate chain or authenticate a ChangeSet signature.

## Revisions, replay and branches

`BaseRevision` rejects a batch targeting a different numeric version of the supplied snapshot.
Required `BeforeETags` identify the expected static POI predecessor cryptographically and
`AfterETags` identify the expected result. Both canonical JSON and deterministic CBOR hashes
must match. Preparation uses `CreateChangeSet`; add descriptions/metadata, then sign with `Sign`
after the ETags are fixed. The signing contract binds both arrays and every operation so that a sender
cannot replace either expected state independently of the signed request.

ETags are readonly values containing format, algorithm and immutable digest bytes. Sign the
specified structured wire representation: JSON tuples contain an explicit encoding (`hex` or
`base64`) and encoded digest, CBOR ETag tuples
contain digest byte strings. `ETag.ToString()` is for display and explicit text parsing; it does
not define the ChangeSet signing bytes. Both JSON serializers use the same ETag array contract.
HEX and Base64 decode to the same ETag value. The built-in signing profile always writes HEX
for these typed values, regardless of their transport encoding.

These identities establish static data agreement. They do not authenticate the sender or bind
commit history, excluded runtime values, a replay ledger or a global ordering across replicas.

`Id` is not deduplicated by the applier. `CreatedAt` has no automatic freshness policy.
`AppliedChangeSetId` records the most recently applied batch only. A validly signed batch can
still be reapplied to the same original snapshot to derive another successor.

## Explicit merge trust

`TryMerge` validates each input against the common source, including both ETag pairs and every
signature via the caller's `verifySignature(batch, signature)` callback. Default calls return a compatibility notice
only. Explicit preparation creates a new unsigned ChangeSet after validating both combined
execution orders on strictly static storage. Runtime updates are outside the signed static merge.

The combined header, operation sequence and result ETags constitute a new batch. Source signatures
are not copied and do not authenticate it. Add the merge's own descriptions/metadata and sign it
separately with `Sign`. Persist the original batches and their merge relationship separately
if an audit trail is required. No cryptographic ancestry or automatic head publication is provided.
See [merge semantics](CHANGESETS.md#merging-concurrent-batches).

For a future distributed commit model, the remaining design work includes:

- A commit identity binding ancestry, ordered operations and signatures beyond the existing before/after POI content identities.
- Additional signing profiles/transports beyond the implemented canonical JSON v2 profile.
- Key resolution, trust policy, revocation and signer authorization.
- Replay/history persistence and publication of the current head.
- Automatic conflict resolution/rebasing beyond the explicit common-source merge, auditability and transport behavior.

These are architectural next steps. They are not implemented guarantees of the current library.
