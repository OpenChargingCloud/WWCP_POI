# ChangeSet CBOR transport

[Repository overview](../README.md) · [ChangeSets](CHANGESETS.md) · [POI ETags and CBOR](ETAGS-CBOR.md) · [Signatures](SIGNATURES.md)

## API and exchange

`RoamingNetworkChangeSet.ToCBOR()` writes a deterministic CBOR map using
[direct emission with the existing signed scalar codec](DIRECT-CHANGESET-CBOR.md), avoiding
complete CBOR/native-ETag conversion trees. `ParseCBOR()` and
`TryParseCBOR()` reconstruct the immutable batch, including all equal peer signatures.
The map uses the same case-sensitive property names and required fields as System.Text.Json.
It retains operation and signature order, complete element paths, descriptions, arbitrary JSON
metadata, parent scope and optional old/new payload presence.

`RemoveProperty` and `RemoveElementProperty` preserve their absent `NewValue` and any expected
old value, including schema-defined SI readings. The history merge's reserved audit metadata
remains lossless ordinary JSON content, including typed ID tuples and custom resolution values;
it does not alter the v2 signing profile. See [branch integration](MERGING.md).

```csharp
var outbound = source.CreateChangeSet("change-43", DateTimeOffset.UtcNow, operations)
    .WithDescription("de", "Netzanschluss aktualisiert")
    .WithMetadata("acme:ticketId", "INC-4711")
    .Sign(privateKey, "acme:alice", COSEAlgorithm.Ed25519);

byte[] bytes = outbound.ToCBOR();
var inbound = RoamingNetworkChangeSet.ParseCBOR(bytes);
var next = replica.ApplyChangeSet(inbound, VerifySignature: VerifyPeer);

if (!RoamingNetworkChangeSet.TryParseCBOR(bytes, out var batch, out var error))
    Console.WriteLine(error);
```

`VerifyPeer` resolves trusted keys and checks each envelope, as shown in the
[signature guide](SIGNATURES.md). Parsing does not verify signer authority, applicability or
the declared result state. Application checks the source network, exact revision, both source
ETags, all signatures, ordered operations and both resulting ETags.

## Wire values

| Value | CBOR representation |
| --- | --- |
| `BeforeETags` / `AfterETags` | JSON then CBOR identifiers, each `[format, "sha256", h'<32 digest bytes>']` |
| Ordered changes, paths and peer signatures | Arrays; their order is preserved |
| Signature envelope fields | Text, including the explicit encoding and signature value |
| Omitted `OldValue` / `NewValue` | Absent map key |
| Defined JSON null payload | Present key with CBOR null |
| Schema-defined SI readings | Styx metrological tag 44252 when the original text can be reconstructed exactly |
| Other SI spellings | Original unit-bearing text, preserving the signed spelling |
| Canonical integer text | Native integer/bignum |
| Exactly recoverable ordinary decimals | Scale-preserving decimal fraction, tag 4 |
| Other JSON number spellings | Embedded JSON Number wrapper, tag 262 |
| Customer text and other JSON values | Their corresponding JSON-compatible CBOR types |

The JSON/CBOR label in an ETag names its **digest input**, independently of the batch's transport.
Only the two typed header ETag arrays change to native digest bytes. JSON arrays inside operation
payloads or metadata, including customer fields named `ETags`, keep their original values.

Metrological detection follows the addressed entity/element schema and property name. It never
interprets descriptions, metadata or `customData` strings such as `"250 kW"` as physical readings.
An original SI spelling that cannot be recovered exactly stays text; numeric or unitless readings
remain invalid domain data. Transport decoding does not relax the domain validators.

## Preserving the v2 signing input

The existing `wwcp-poi-changeset-json-v2` profile binds JSON number spelling as well as string
values. `1.0`, `1e0` and `-0` must therefore survive exchange without normalization. Native
integers and decimals are used only where reconstruction retains that spelling exactly.
For other spellings this transport defines the scalar wrapper
`262(h'<UTF-8 bytes of {"Number": originalNumber}>')`. Its embedded JSON must be an object
containing exactly one `Number` member whose value is a JSON number. This uses the registered
[embedded JSON object tag](https://www.iana.org/assignments/cbor-tags/), with a Number wrapper
defined by this ChangeSet profile.

Transport selection leaves `GetSigningBytes(...)` unchanged for the same signature header;
all existing peer envelopes are retained. It does not clear signatures or sign the CBOR bytes.
The complete tagged batch bytes are distinct from static POI canonical bytes and POI ETags.

## Decoder limits

The reader rejects duplicate map keys, non-text JSON property names, trailing data, unknown
model fields and malformed ETag tuples. Native digest byte strings are restricted to the typed
header ETags. Elsewhere bare byte strings, undefined/simple values, binary floats and unknown
tags are outside this profile. Integer/bignum and decimal-fraction numeric inputs are exact;
native decimal fractions outside Decimal capacity are reconstructed using integer mantissa and
decimal exponent, with an exponent range of -65536 to 65536.

Tag 44252 is accepted only at schema-defined reading paths. Tag 262 accepts only the Number
wrapper above. Required fields and operation shapes use the ordinary immutable model's
validators. Arbitrary JSON metadata retains numeric precision and optional/null distinctions.
The [interoperability package](INTEROPERABILITY.md) publishes exact JSON/CBOR/signing-byte references
and executes two-peer cryptographic verification after transport, SI/number-spelling retention,
optional payload presence, header ETag byte tuples, duplicate-key/trailing-item rejection and
continued signed transitions after recovery. Further malformed-tag/size-limit and exhaustive
schema-path cases remain additional coverage work.

The subsequent [direct-encoding verification](DIRECT-CHANGESET-CBOR.md) adds 55 cases and passes
283 focused / 1,308 full Release cases against the unchanged signing/byte references. It includes
exact numeric/SI spellings, scoped elements, all operation shapes and persistent tag-depth failures.
Matched long-batch/multiple-peer measurements are separate from the earlier one-operation fixture.
The decoder and static domain ranges remain unchanged.

## Direct archive payload output

The [direct archive ChangeSet package](DIRECT-ARCHIVE-CHANGESET.md) emits the same bytes inside
all archive profiles after complete signing/schema/scalar/depth preflight, without a complete
per-batch CBOR output buffer. Native root ETags, exact signed scalar tokens and every peer remain.
Public standalone `ToCBOR()` is unchanged; prepared JSON/index/schema paths and decoder trees remain.
