# Example A1: The roaming network `QA` begins with one organization, two more join it, and how to check it

A roaming network does not begin as a consortium. In this example one organization, `AlphaOrg`, begins alone: it makes
its keys, the roaming network operator (RNO) *"Open Charging Cloud (OCC)"* and the roaming network `QA`, and serves
`QA` at a URL of its own. Then `BetaOrg` joins: it reads `QA` at `AlphaOrg`, checks it, makes its own keys and its own
copy of `QA`, and from then on both decide, a 2/2 quorum. Then `GammaOrg` joins the same way, and two of the three
decide: a 2/3 quorum. Who holds the keys of the operator, and how many of the holders decide, is content of `QA` like
everything else, changed by a change set the holders before it sign. So it can grow, change and shrink again, step by
step, as a market does.

| Revision | Who holds the keys of `OCC` | Quorum | `QA` is served at | Signed by |
|---|---|---|---|---|
| 0, the genesis | `AlphaOrg` | 1 of 1 | `alpha.example` | `AlphaOrg` |
| 1 | `AlphaOrg`, `BetaOrg` | 2 of 2 | `alpha.example`, `beta.example` | the change set: `AlphaOrg`, the holder before, and the new identity key of `BetaOrg`; the commit: both |
| 2 | `AlphaOrg`, `BetaOrg`, `GammaOrg` | 2 of 3 | `alpha.example`, `beta.example`, `gamma.example` | the change set: `AlphaOrg` and `BetaOrg`, the holders before, and the new identity key of `GammaOrg`; the commit: all three |

Every revision is published with its ETags, its commit and its trust anchor. The trust anchor fits into a QR code, so
that it is easily handed to everybody who uses `QA`. A charging station operator (CSO), or the app of an EV driver, who
knows nothing of `QA` but this QR code, then checks what it reads from any of the three servers: that enough of the
holders of the operator's keys signed it, and that nothing in it changed since. And whoever scanned the QR code when
`AlphaOrg` was alone follows the key statements of `OCC`, one after the other, to the keys of today.

The checked revision 2 of `QA` is where [example A2](exampleA2.md) starts: the charging station operator `DE*GEF` asks
to be added to it.


## The program

The examples are one console project, [csharp/](csharp/), referencing `WWCP_POI3`: one file per example,
[exampleA1.cs](csharp/exampleA1.cs), [exampleA2.cs](csharp/exampleA2.cs), [exampleA3.cs](csharp/exampleA3.cs),
[exampleA4.cs](csharp/exampleA4.cs), [exampleA5.cs](csharp/exampleA5.cs), [exampleA6.cs](csharp/exampleA6.cs),
[exampleA7.cs](csharp/exampleA7.cs), [exampleA8.cs](csharp/exampleA8.cs),
[exampleA9.cs](csharp/exampleA9.cs),
[exampleA10.cs](csharp/exampleA10.cs), and [Program.cs](csharp/Program.cs) calling them in their order. What one example makes, the next is given:
`ExampleA1.Run()` returns the three organizations as the shareholders of `OCC`, each with its private keys, the trust
anchor and the history of `QA`, whose head is the current version of `QA`. Each check, `ExampleA1.Validate()` and `ExampleA1.ValidateSuccession()` here,
reads only what is published, as a charging station operator or an app would. `ExampleA1.WithoutGammaOrg(a1)` tries
what it took if `GammaOrg` left again, and commits nothing.

```csharp
using var a1 = ExampleA1.Run();

ExampleA1.Validate();

ExampleA1.ValidateSuccession();

ExampleA1.WithoutGammaOrg(a1);
```

```
dotnet run
```

Run in [csharp/](csharp/), it writes the private keys of each organization into a directory of its own
(`AlphaOrg/`, `BetaOrg/`, `GammaOrg/`), what everybody may read into `published/`, and the history of `QA` into
`poi/QA/`; all of them are ignored by git. The output and the JSON below are what one run printed and published, `…`
for elided keys, signatures and digests: the keys are new in every run, and so are all digests.

In reality each organization runs its part on its own machine, and what one hands to another goes over HTTP: a
change set waiting for signatures is served at `GET ~/RNs/QA/history/changeSets/pending/{id}`, and a signature is sent
to it with `POST …/signatures` ([SPECIFICATION.md](../SPECIFICATION.md), section 4.2). Here, one program plays all
three.


## 1. Two kinds of keys, and who decides

Every party of a roaming network has two kinds of keys ([SPECIFICATION.md](../SPECIFICATION.md), section 4.1):

- **identity keys**, the long-term keys: they state the keys and the quorum of the operator, and certify its signing
  keys. They are what a verifier pins, and they are kept offline as far as possible.
- **signing keys**, the everyday keys: they sign commits, change sets and attestations.

Each organization makes its identity key and its signing key when it begins or joins, and keeps the private keys to
itself. Each chooses its own kind of identity key: `AlphaOrg` ECDSA P-521 (`ES512`), `BetaOrg` EdDSA `Ed448`,
`GammaOrg` `ML-DSA-65`, post-quantum; the signing keys are P-256 (`ES256`).

An organization with its two key pairs and the URL of its copy of `QA` is a **`RoamingShareholder`**: it holds, or is
about to hold, one voice among those who decide as the roaming network operator. Everything an organization does in
this example, it does as one: `alpha`, `beta` and `gamma`.

```csharp
RoamingShareholder NewShareholder(String Name, COSEKeyPair IdentityKey, String Host)
{

    var shareholder = RoamingShareholder.Create(
                          Name,
                          IdentityKey,
                          URL: URL.Parse($"https://{Host}/RNs/{qaId}")
                      );

    Directory.CreateDirectory(Name);
    Helpers.SaveKey(shareholder.IdentityKey.Private, $"{Name}/identity-privateKey.pem");
    Helpers.SaveKey(shareholder.SigningKey. Private, $"{Name}/signing-privateKey.pem");

    return shareholder;

}
```

| A shareholder | does what |
|---|---|
| `IdentityPublicKey` | the public half of its identity key, naming it as its `holder` |
| `StateKeys(…)`, `CoSign(keyStatement)` | states who holds the keys of the operator, or signs such a statement as well |
| `CertifySigningKey(…)` | certifies its own signing key with its identity key |
| `SignWithIdentityKey(changeSet)`, `SignWithSigningKey(changeSet)` | signs a change set with one of its keys |
| `CommitSignerFor(operatorId)` | signs commits with its signing key, as the roaming network operator |

A `COSEKeyPair` is a key pair with its signature algorithm: whatever it signs, it signs with the algorithm of its
kind; `RoamingShareholder.Create` makes the ES256 signing key unless it is given one. A public key is written as its
`@id`, the SHA-256 thumbprint of the COSE key (RFC 9679), its `key`, the Base64 of its deterministic CBOR, and its
`holder`, the name of the shareholder, which is no part of the thumbprint.

A shareholder is one on its own machine only, as nobody else has its private keys. Of the others an organization
knows what they publish: their public identity key, the certificate of their signing key and their URL.

**The quorum.** `minConsensus` says how many holders, the shareholders, decide: wherever the RNO must sign - the
roaming network itself, adding and removing parties, its own keys - signatures of as many different holders are
needed. Holders are counted, not keys, and not shares either: a holder renewing its key, or naming two identity keys,
still has one voice. `minConsensus` must be more than half of the holders, so that any two quorums share a holder: two
contradicting decisions need one holder signing both, and that holder can be named. So one holder decides alone, two
holders decide together, and of three, two decide; an odd number of holders is not needed, 3 of 4 is fine as well. A
`minConsensus` of 1 is not written.

**Who decides on the holders.** A change of the identity keys or of `minConsensus` is signed by identity keys of as
many holders as the version *before* it requires, and by every identity key it enters anew, as the proof that its
holder has it and asked for it. So nobody is named a holder without knowing, and nobody joins without the holders
before agreeing. The quorum must fit the keys in every version, not between the changes of one change set: a second
holder and the quorum of two arrive together.


## 2. `AlphaOrg` alone: revision 0

### The key statement

The roaming network operator states its identity keys and its quorum in a **key statement** (section 8.6), signed with
identity keys of `minConsensus` of its holders. One holder, one deciding, one signature:

```csharp
var alpha                 = NewShareholder("AlphaOrg", COSEKeyPair.NewES512(), "alpha.example");

var keyStatementAlpha     = alpha.StateKeys(
                                qaId,
                                occId,
                                IdentityKeys:  [ alpha.IdentityPublicKey ],
                                MinConsensus:  1,
                                SignedAt:      now,
                                ValidUntil:    now.AddYears(1)
                            );
```

```json
{
  "roamingNetworkId": "QA",
  "roamingNetworkOperatorId": "OCC",
  "identityKeys": [ { "@id": "1da76b63…", "key": "pAECIAMh…", "holder": "AlphaOrg" } ],
  "minConsensus": 1,
  "signedAt": "2026-11-13T22:21:33.300Z",
  "validUntil": "2027-11-13T22:21:33.300Z",
  "signatures": [ { "algorithm": "ES512", "keyId": "1da76b63…", "value": "Ab7ra0AK…" } ]
}
```

Each signature is over the canonical JSON of the statement without `signatures`, with its own `algorithm` and `keyId`
and the `@context` `…/roamingNetwork/keyStatement/v3`. The statement names no signing keys: each holder certifies its
own.

### The signing key, certified by its holder

A signing key is for the `party` only: it signs what the operator owns, see `POIKeyUsages`. Each organization
certifies its own signing key with its identity key, alone: renewing it later needs nobody else, as a signing key
gives its holder one voice only, however it comes to be trusted.

```csharp
alpha.CertifySigningKey(occId, now, NotAfter: now.AddYears(1))
```

```json
{
  "roamingNetworkOperatorId": "OCC",
  "signingKey": { "@id": "6a49d53d…", "key": "pAECIAEh…", "notAfter": "2027-11-13T22:21:33.300Z", "holder": "AlphaOrg", "usages": [ "party" ] },
  "signedAt": "2026-11-13T22:21:33.300Z",
  "algorithm": "ES512",
  "keyId": "1da76b63…",
  "value": "ABx+jh8Y…"
}
```

A certificate (`…/roamingNetwork/signingKeyCertificate/v2`) names the operator, not a network: the same signing keys
serve in every roaming network `OCC` operates.

### `OCC`, `QA` and where it is served

The operator takes its identity keys and its quorum from the key statement, and its signing keys from their
certificates; the roaming network embeds its operator and names no keys of its own. It names its `urls`
([SPECIFICATION.md](../SPECIFICATION.md), section 2): where it is served, `~/RNs/QA` at every server holding a copy of
it, the preferred one first. They are content, so they are signed with everything else: whoever checked one copy knows
where the others are.

```csharp
var occ                   = new RoamingNetworkOperator(
                                keyStatementAlpha,
                                Name:                    I18NString.Create("Open Charging Cloud"),
                                SigningKeyCertificates:  [ alpha.CertifySigningKey(occId, now, NotAfter: now.AddYears(1)) ]
                            );

// QA names where it is served: so far at AlphaOrg only.
var qa                    = occ.CreateRoamingNetwork(
                                qaId,
                                Name:          ImmutableI18NString.Create("Quality Assurance"),
                                Description:   ImmutableI18NString.Create("The roaming network of the examples"),
                                URLs:          [ alpha.URL!.Value ]
                            );
```

### The history and its genesis commit

`RoamingNetworkHistory.Start` makes the genesis commit, revision 0, signed by the signing key of `AlphaOrg` in the
role of the roaming network operator, `alpha.CommitSignerFor(occId)`, and keeps the history in `poi/QA`. The store
stays open for the examples building on this one; disposing the result of `Run()` closes it.

```csharp
var store                 = new RoamingNetworkHistoryFileStore($"poi/{qa.Id}");
var history               = RoamingNetworkHistory.Start(
                                qa,
                                Signers:  [ alpha.CommitSignerFor(occId) ],
                                Store:    store
                            );

Publish(history, "AlphaOrg alone");

// Where a server or a witness following QA from its start begins: the genesis version with its commit.
File.WriteAllText($"published/{qa.Id}-genesis.json", history.ToSnapshotJSON(IncludeRuntimeStatus: false).ToString());
```

```
Revision 0: AlphaOrg alone
  holders AlphaOrg; minConsensus 1; commit 3bb78607…, signed by 1 signing key(s)
  urls https://alpha.example/RNs/QA
  trust anchor: QR code of 379 characters, 246 bytes of CBOR
```

`Publish` writes what everybody may read of a revision into `published/`: the roaming network with its ETags,
`QA-revision{N}.json`, the commit leading to it, `QA-revision{N}-commit.json`, and its trust anchor as the text of its
QR code, `QA-trustAnchor-revision{N}.txt`. POI_API serves the same of the head as `GET ~/RNs/QA`,
`GET ~/RNs/QA/history/commits/{commitId}` and `GET ~/RNs/QA/trustAnchor`. `QA-genesis.json` stays the snapshot of
revision 0, `{ timestamp, commit, roamingNetwork }`, where a witness begins ([example A6](exampleA6.md)).

`published/QA-revision0.json`, the roaming network as `AlphaOrg` begins it:

```json
{
  "@id": "QA",
  "@context": "https://open.charging.cloud/contexts/wwcp/roamingNetwork/v1",
  "name": { "en": "Quality Assurance" },
  "description": { "en": "The roaming network of the examples" },
  "operator": {
    "@id": "OCC",
    "name": { "en": "Open Charging Cloud" },
    "keyStatement": {
      "roamingNetworkId": "QA",
      "roamingNetworkOperatorId": "OCC",
      "identityKeys": [ { "@id": "1da76b63…", "key": "pAECIAMh…", "holder": "AlphaOrg" } ],
      "minConsensus": 1,
      "signedAt": "2026-11-13T22:21:33.300Z",
      "validUntil": "2027-11-13T22:21:33.300Z",
      "signatures": [ { "algorithm": "ES512", "keyId": "1da76b63…", "value": "Ab7ra0AK…" } ]
    },
    "signingKeyCertificates": [
      {
        "roamingNetworkOperatorId": "OCC",
        "signingKey": { "@id": "6a49d53d…", "key": "pAECIAEh…", "notAfter": "2027-11-13T22:21:33.300Z", "holder": "AlphaOrg", "usages": [ "party" ] },
        "signedAt": "2026-11-13T22:21:33.300Z",
        "algorithm": "ES512",
        "keyId": "1da76b63…",
        "value": "ABx+jh8Y…"
      }
    ],
    "identityKeys": [ { "@id": "1da76b63…", "key": "pAECIAMh…", "holder": "AlphaOrg" } ],
    "created": "2026-11-13T22:21:33.573Z",
    "lastChange": "2026-11-13T22:21:33.573Z"
  },
  "urls": [ "https://alpha.example/RNs/QA" ],
  "created": "2026-11-13T22:21:33.613Z",
  "lastChange": "2026-11-13T22:21:33.613Z",
  "eTags": [
    [ "json", "sha256", "base64", "FBoxKCEn…" ],
    [ "cbor", "sha256", "base64", "zvZV3KOd…" ],
    [ "effectiveTree", "sha256", "base64", "ySVZ2+G8…" ]
  ]
}
```

The operator writes no `signingKeys`: they are those of its `signingKeyCertificates`, and a representation naming
`signingKeys` of the operator is refused. It writes no `minConsensus` either, as it is 1.
`published/QA-revision0-commit.json`, the genesis commit:

```json
{
  "@id": "3bb78607…",
  "@context": "https://open.charging.cloud/contexts/wwcp/roamingNetwork/commit/v3",
  "roamingNetworkId": "QA",
  "revision": 0,
  "eTags": [ [ "json", "sha256", "hex", "141a3128…" ], [ "cbor", "sha256", "hex", "cef655dc…" ], [ "effectiveTree", "sha256", "hex", "c92559db…" ] ],
  "signatures": [
    {
      "role": "roamingNetworkOperator",
      "signer": "OCC",
      "signedAt": "2026-11-13T22:21:33.763Z",
      "algorithm": "ES256",
      "keyId": "6a49d53d…",
      "value": "SXFLDi9N…"
    }
  ]
}
```

A genesis commit has neither `parentIds` nor a `changeSet`: it is the version itself, not a change of another. Its one
signature is by the signing key of `AlphaOrg` (`6a49d53d…`), which the certificate above certifies. The
trust anchor of revision 0, what somebody scanning the QR code of `AlphaOrg` pins (section 5 explains it):

```json
{
  "@id": "QA",
  "@context": "https://open.charging.cloud/contexts/wwcp/roamingNetwork/trustAnchor/v2",
  "urls": [ "https://alpha.example/RNs/QA" ],
  "keys": [ "1da76b63…" ],
  "name": { "en": "Quality Assurance" },
  "description": { "en": "The roaming network of the examples" }
}
```


## 3. `BetaOrg` joins: two of two, revision 1

`BetaOrg` wants to operate `QA` with `AlphaOrg`. It reads revision 0 at `https://alpha.example/RNs/QA`, checks it
against the trust anchor of `AlphaOrg` as [section 7](#7-checking-qa-as-a-charging-station-operator-or-an-app) does,
makes its own keys, and sets up its own server with a copy of `QA`.

### The key statement of two holders

The key statement of the next version names both identity keys and a quorum of two. `AlphaOrg`, the one holder so far,
states it; `BetaOrg` signs it as well, so that it is signed by its own quorum, as every key statement of a version
must be:

```csharp
var beta                  = NewShareholder("BetaOrg", COSEKeyPair.NewEd448(), "beta.example");

var keyStatementBeta      = beta.CoSign(
                                alpha.StateKeys(
                                    qaId,
                                    occId,
                                    IdentityKeys:  [ alpha.IdentityPublicKey, beta.IdentityPublicKey ],
                                    MinConsensus:  minConsensus,
                                    SignedAt:      Timestamp.Now,
                                    ValidUntil:    now.AddYears(1)
                                )
                            );
```

```json
{
  "roamingNetworkId": "QA",
  "roamingNetworkOperatorId": "OCC",
  "identityKeys": [
    { "@id": "1da76b63…", "key": "pAECIAMh…", "holder": "AlphaOrg" },
    { "@id": "a684e60d…", "key": "owEBIAch…", "holder": "BetaOrg" }
  ],
  "minConsensus": 2,
  "signedAt": "2026-11-13T22:21:33.912Z",
  "validUntil": "2027-11-13T22:21:33.300Z",
  "signatures": [
    { "algorithm": "ES512", "keyId": "1da76b63…", "value": "AbJJoZYi…" },
    { "algorithm": "Ed448", "keyId": "a684e60d…", "value": "mZWgxXkh…" }
  ]
}
```

Every holder signs the same statement on its own, in any order. Keys are written in the order of their `@id`s.

### One change set

Who holds the keys of `OCC` is content of `QA`, so it changes as content does: by a change set, here of five changes.
Four change the operator: its identity keys, its key statement, the certificates of its signing keys and its
`minConsensus`. The fifth changes `QA` itself: its `urls` now name the copy at `BetaOrg` too. Between these changes
no version exists - two holders with a quorum of one would be no quorum at all - so the quorum is checked on the
version after all of them. `Admit` prepares this change set against the head: the new shareholder next to those so
far.

```csharp
var betaJoins             = history.Head.RoamingNetwork.Admit(
                                beta,
                                keyStatementBeta,
                                SigningKeyNotAfter:  now.AddYears(1),
                                Description:         ImmutableI18NString.Create(Languages.en, "BetaOrg joins AlphaOrg: two of two")
                            );
```

| Change | Of | Property | New value |
|---|---|---|---|
| 0 | `OCC` | `identityKeys` | the identity keys so far and the one of `BetaOrg` |
| 1 | `OCC` | `keyStatement` | `keyStatementBeta` |
| 2 | `OCC` | `signingKeyCertificates` | the certificates so far and the one of the signing key of `BetaOrg` |
| 3 | `OCC` | `minConsensus` | 2, as the key statement states it; left out when the quorum stays |
| 4 | `QA` | `urls` | the URLs so far and the one of `BetaOrg` |

Here one program has the private keys of `BetaOrg`, so it hands over the shareholder. `AlphaOrg`, knowing only what
`BetaOrg` publishes, prepares the same change set from that:
`Admit(identityKey, signingKeyCertificate, keyStatement, url)`.

### Who signs it

`AlphaOrg` signs the change set twice: with its identity key, as the one holder so far decides who holds the keys, and
with its signing key, as the key statement and the URLs are content of `OCC` and `QA`. That is the whole quorum of
revision 0, and still the change set is refused: a new identity key signs the change set stating it.

```csharp
betaJoins                 = alpha.SignWithSigningKey(alpha.SignWithIdentityKey(betaJoins));

var withoutBetaOrg        = history.Commit(betaJoins);
```

```
BetaOrg joins, signed by AlphaOrg only: Change 0: UpdateProperty of the RoamingNetworkOperator 'OCC' ('identityKeys') needs a signature by the new identity key 'a684e60d…' of the roaming network operator 'OCC', as the proof that it holds it!
```

`BetaOrg` downloads the change set at `AlphaOrg`, checks what it states - its own keys, the quorum of two, the URL of
its copy - signs it with its identity key and sends its signature back. From now on every commit is signed by both:

```csharp
betaJoins                 = beta.SignWithIdentityKey(betaJoins);
history.Signers           = [ alpha.CommitSignerFor(occId), beta.CommitSignerFor(occId) ];

var betaJoined            = history.Commit(betaJoins);

Publish(history, "BetaOrg joined, two of two decide");
```

```
Revision 1: BetaOrg joined, two of two decide
  holders AlphaOrg, BetaOrg; minConsensus 2; commit 0a4866f4…, signed by 2 signing key(s)
  urls https://alpha.example/RNs/QA, https://beta.example/RNs/QA
  trust anchor: QR code of 495 characters, 323 bytes of CBOR
```

### Revision 1

`published/QA-revision1.json`: two identity keys, the key statement of both, two certificates, `"minConsensus": 2`
and two `urls`:

```json
{
  "@id": "QA",
  "@context": "https://open.charging.cloud/contexts/wwcp/roamingNetwork/v1",
  "name": { "en": "Quality Assurance" },
  "description": { "en": "The roaming network of the examples" },
  "operator": {
    "@id": "OCC",
    "name": { "en": "Open Charging Cloud" },
    "minConsensus": 2,
    "keyStatement": {
      "roamingNetworkId": "QA",
      "roamingNetworkOperatorId": "OCC",
      "identityKeys": [
        { "@id": "1da76b63…", "key": "pAECIAMh…", "holder": "AlphaOrg" },
        { "@id": "a684e60d…", "key": "owEBIAch…", "holder": "BetaOrg" }
      ],
      "minConsensus": 2,
      "signedAt": "2026-11-13T22:21:33.912Z",
      "validUntil": "2027-11-13T22:21:33.300Z",
      "signatures": [
        { "algorithm": "ES512", "keyId": "1da76b63…", "value": "AbJJoZYi…" },
        { "algorithm": "Ed448", "keyId": "a684e60d…", "value": "mZWgxXkh…" }
      ]
    },
    "signingKeyCertificates": [
      {
        "roamingNetworkOperatorId": "OCC",
        "signingKey": { "@id": "40347caa…", "key": "pAECIAEh…", "notAfter": "2027-11-13T22:21:33.300Z", "holder": "BetaOrg", "usages": [ "party" ] },
        "signedAt": "2026-11-13T22:21:33.300Z",
        "algorithm": "Ed448",
        "keyId": "a684e60d…",
        "value": "7FuWSoa1…"
      },
      {
        "roamingNetworkOperatorId": "OCC",
        "signingKey": { "@id": "6a49d53d…", "key": "pAECIAEh…", "notAfter": "2027-11-13T22:21:33.300Z", "holder": "AlphaOrg", "usages": [ "party" ] },
        "signedAt": "2026-11-13T22:21:33.300Z",
        "algorithm": "ES512",
        "keyId": "1da76b63…",
        "value": "ABx+jh8Y…"
      }
    ],
    "identityKeys": [
      { "@id": "1da76b63…", "key": "pAECIAMh…", "holder": "AlphaOrg" },
      { "@id": "a684e60d…", "key": "owEBIAch…", "holder": "BetaOrg" }
    ],
    "created": "2026-11-13T22:21:33.573Z",
    "lastChange": "2026-11-13T22:21:33.965Z"
  },
  "urls": [ "https://alpha.example/RNs/QA", "https://beta.example/RNs/QA" ],
  "created": "2026-11-13T22:21:33.613Z",
  "lastChange": "2026-11-13T22:21:33.965Z",
  "eTags": [
    [ "json", "sha256", "base64", "7N7iAa0w…" ],
    [ "cbor", "sha256", "base64", "o+tRLfYs…" ],
    [ "effectiveTree", "sha256", "base64", "77L6bH60…" ]
  ]
}
```

`published/QA-revision1-commit.json`, the commit that led there. It names the genesis commit in its `parentIds` and
carries the change set as it was signed: its three `signatures`, by the identity key of `AlphaOrg`
(`1da76b63…`), its signing key (`6a49d53d…`) and the identity key of `BetaOrg`
(`a684e60d…`), and its five `changes`, each with the old value it found and the new one. Its
`beforeETags` are the ETags of revision 0, its `afterETags` those of revision 1: a change set applies only to the
version it names. The commit itself is signed by the signing keys of both holders:

```json
{
  "@id": "0a4866f4…",
  "@context": "https://open.charging.cloud/contexts/wwcp/roamingNetwork/commit/v3",
  "roamingNetworkId": "QA",
  "revision": 1,
  "eTags": [ [ "json", "sha256", "hex", "ecdee201…" ], [ "cbor", "sha256", "hex", "a3eb512d…" ], [ "effectiveTree", "sha256", "hex", "efb2fa6c…" ] ],
  "parentIds": [ "3bb78607…" ],
  "changeSet": {
    "@id": "6bfb83f2…",
    "roamingNetworkId": "QA",
    "createdAt": "2026-11-13T22:21:33.965Z",
    "description": { "en": "BetaOrg joins AlphaOrg: two of two" },
    "signatures": [
      { "algorithm": "ES512", "keyId": "1da76b63…", "value": "Ad0IAUPo…" },
      { "algorithm": "ES256", "keyId": "6a49d53d…", "value": "Dg6VI5mL…" },
      { "algorithm": "Ed448", "keyId": "a684e60d…", "value": "YdDbyywE…" }
    ],
    "changes": [
      {
        "kind": "UpdateProperty",
        "entityType": "RoamingNetworkOperator",
        "entityId": "OCC",
        "propertyName": "identityKeys",
        "oldValue": [ { "@id": "1da76b63…", "key": "pAECIAMh…", "holder": "AlphaOrg" } ],
        "newValue": [
          { "@id": "1da76b63…", "key": "pAECIAMh…", "holder": "AlphaOrg" },
          { "@id": "a684e60d…", "key": "owEBIAch…", "holder": "BetaOrg" }
        ]
      },
      {
        "kind": "UpdateProperty",
        "entityType": "RoamingNetworkOperator",
        "entityId": "OCC",
        "propertyName": "keyStatement",
        "oldValue": {
          "roamingNetworkId": "QA",
          "roamingNetworkOperatorId": "OCC",
          "identityKeys": [ { "@id": "1da76b63…", "key": "pAECIAMh…", "holder": "AlphaOrg" } ],
          "minConsensus": 1,
          "signedAt": "2026-11-13T22:21:33.300Z",
          "validUntil": "2027-11-13T22:21:33.300Z",
          "signatures": [ { "algorithm": "ES512", "keyId": "1da76b63…", "value": "Ab7ra0AK…" } ]
        },
        "newValue": {
          "roamingNetworkId": "QA",
          "roamingNetworkOperatorId": "OCC",
          "identityKeys": [
            { "@id": "1da76b63…", "key": "pAECIAMh…", "holder": "AlphaOrg" },
            { "@id": "a684e60d…", "key": "owEBIAch…", "holder": "BetaOrg" }
          ],
          "minConsensus": 2,
          "signedAt": "2026-11-13T22:21:33.912Z",
          "validUntil": "2027-11-13T22:21:33.300Z",
          "signatures": [
            { "algorithm": "ES512", "keyId": "1da76b63…", "value": "AbJJoZYi…" },
            { "algorithm": "Ed448", "keyId": "a684e60d…", "value": "mZWgxXkh…" }
          ]
        }
      },
      {
        "kind": "UpdateProperty",
        "entityType": "RoamingNetworkOperator",
        "entityId": "OCC",
        "propertyName": "signingKeyCertificates",
        "oldValue": [
          {
            "roamingNetworkOperatorId": "OCC",
            "signingKey": {
              "@id": "6a49d53d…",
              "key": "pAECIAEh…",
              "notAfter": "2027-11-13T22:21:33.300Z",
              "holder": "AlphaOrg",
              "usages": [ "party" ]
            },
            "signedAt": "2026-11-13T22:21:33.300Z",
            "algorithm": "ES512",
            "keyId": "1da76b63…",
            "value": "ABx+jh8Y…"
          }
        ],
        "newValue": [
          {
            "roamingNetworkOperatorId": "OCC",
            "signingKey": {
              "@id": "40347caa…",
              "key": "pAECIAEh…",
              "notAfter": "2027-11-13T22:21:33.300Z",
              "holder": "BetaOrg",
              "usages": [ "party" ]
            },
            "signedAt": "2026-11-13T22:21:33.300Z",
            "algorithm": "Ed448",
            "keyId": "a684e60d…",
            "value": "7FuWSoa1…"
          },
          {
            "roamingNetworkOperatorId": "OCC",
            "signingKey": {
              "@id": "6a49d53d…",
              "key": "pAECIAEh…",
              "notAfter": "2027-11-13T22:21:33.300Z",
              "holder": "AlphaOrg",
              "usages": [ "party" ]
            },
            "signedAt": "2026-11-13T22:21:33.300Z",
            "algorithm": "ES512",
            "keyId": "1da76b63…",
            "value": "ABx+jh8Y…"
          }
        ]
      },
      {
        "kind": "UpdateProperty",
        "entityType": "RoamingNetworkOperator",
        "entityId": "OCC",
        "propertyName": "minConsensus",
        "oldValue": null,
        "newValue": 2
      },
      {
        "kind": "UpdateProperty",
        "entityType": "RoamingNetwork",
        "entityId": "QA",
        "propertyName": "urls",
        "oldValue": [ "https://alpha.example/RNs/QA" ],
        "newValue": [ "https://alpha.example/RNs/QA", "https://beta.example/RNs/QA" ]
      }
    ],
    "beforeETags": [ [ "json", "sha256", "hex", "141a3128…" ], [ "cbor", "sha256", "hex", "cef655dc…" ] ],
    "afterETags": [ [ "json", "sha256", "hex", "ecdee201…" ], [ "cbor", "sha256", "hex", "a3eb512d…" ] ]
  },
  "signatures": [
    {
      "role": "roamingNetworkOperator",
      "signer": "OCC",
      "signedAt": "2026-11-13T22:21:34.160Z",
      "algorithm": "ES256",
      "keyId": "6a49d53d…",
      "value": "c50ZQnj8…"
    },
    {
      "role": "roamingNetworkOperator",
      "signer": "OCC",
      "signedAt": "2026-11-13T22:21:34.163Z",
      "algorithm": "ES256",
      "keyId": "40347caa…",
      "value": "crbDzlP9…"
    }
  ]
}
```

The trust anchor of revision 1 names two holders, a quorum of two and both URLs:

```json
{
  "@id": "QA",
  "@context": "https://open.charging.cloud/contexts/wwcp/roamingNetwork/trustAnchor/v2",
  "urls": [ "https://alpha.example/RNs/QA", "https://beta.example/RNs/QA" ],
  "keys": [ "1da76b63…", "a684e60d…" ],
  "name": { "en": "Quality Assurance" },
  "description": { "en": "The roaming network of the examples" },
  "minConsensus": 2
}
```


## 4. `GammaOrg` joins: two of three, revision 2

`GammaOrg` joins the same way, with an identity key of another kind again. The key statement of three holders, two of
them deciding, is signed by `AlphaOrg` and `BetaOrg`: the quorum so far, and a quorum of its own holders as well.

```csharp
var gamma                 = NewShareholder("GammaOrg", COSEKeyPair.NewMLDsa65(), "gamma.example");

var keyStatementGamma     = beta.CoSign(
                                alpha.StateKeys(
                                    qaId,
                                    occId,
                                    IdentityKeys:  [ alpha.IdentityPublicKey, beta.IdentityPublicKey, gamma.IdentityPublicKey ],
                                    MinConsensus:  minConsensus,
                                    SignedAt:      Timestamp.Now,
                                    ValidUntil:    now.AddYears(1)
                                )
                            );
```

```json
{
  "roamingNetworkId": "QA",
  "roamingNetworkOperatorId": "OCC",
  "identityKeys": [
    { "@id": "1da76b63…", "key": "pAECIAMh…", "holder": "AlphaOrg" },
    { "@id": "80c817d6…", "key": "owEHAzgw…", "holder": "GammaOrg" },
    { "@id": "a684e60d…", "key": "owEBIAch…", "holder": "BetaOrg" }
  ],
  "minConsensus": 2,
  "signedAt": "2026-11-13T22:21:34.205Z",
  "validUntil": "2027-11-13T22:21:33.300Z",
  "signatures": [
    { "algorithm": "ES512", "keyId": "1da76b63…", "value": "ACVTGzee…" },
    { "algorithm": "Ed448", "keyId": "a684e60d…", "value": "BbI546eD…" }
  ]
}
```

The change set has four changes: the quorum stays two, of three holders now, so `Admit` does not change
`minConsensus`.

```csharp
var gammaJoins            = history.Head.RoamingNetwork.Admit(
                                gamma,
                                keyStatementGamma,
                                SigningKeyNotAfter:  now.AddYears(1),
                                Description:         ImmutableI18NString.Create(Languages.en, "GammaOrg joins AlphaOrg and BetaOrg: two of three")
                            );
```

`GammaOrg` signs with its identity key, the proof that it holds it, and `AlphaOrg` with its identity key and its
signing key. That is one of the two holders of revision 1, so it is refused: two decide.

```csharp
gammaJoins                = gamma.SignWithIdentityKey(gammaJoins);
gammaJoins                = alpha.SignWithSigningKey(alpha.SignWithIdentityKey(gammaJoins));

var withoutBetaOrgAgain   = history.Commit(gammaJoins);
```

```
GammaOrg joins, signed by GammaOrg and AlphaOrg only: Change 0: UpdateProperty of the RoamingNetworkOperator 'OCC' ('identityKeys') needs signatures by identity keys of 2 holders of the roaming network operator 'OCC'!
```

`BetaOrg` signs as well, with its identity key and its signing key. Every commit is signed by all three from now on;
two of them would be enough.

```csharp
gammaJoins                = beta.SignWithSigningKey(beta.SignWithIdentityKey(gammaJoins));
history.Signers           = [ alpha.CommitSignerFor(occId), beta.CommitSignerFor(occId), gamma.CommitSignerFor(occId) ];

var gammaJoined           = history.Commit(gammaJoins);

var trustAnchor           = Publish(history, "GammaOrg joined, two of three decide");
```

```
Revision 2: GammaOrg joined, two of three decide
  holders AlphaOrg, BetaOrg, GammaOrg; minConsensus 2; commit 9eb5d1bc…, signed by 3 signing key(s)
  urls https://alpha.example/RNs/QA, https://beta.example/RNs/QA, https://gamma.example/RNs/QA
  trust anchor: QR code of 591 characters, 387 bytes of CBOR
```

`published/QA-revision2.json`, also published as `QA.json`, the head the examples after this one go on from:

```json
{
  "@id": "QA",
  "@context": "https://open.charging.cloud/contexts/wwcp/roamingNetwork/v1",
  "name": { "en": "Quality Assurance" },
  "description": { "en": "The roaming network of the examples" },
  "operator": {
    "@id": "OCC",
    "name": { "en": "Open Charging Cloud" },
    "minConsensus": 2,
    "keyStatement": {
      "roamingNetworkId": "QA",
      "roamingNetworkOperatorId": "OCC",
      "identityKeys": [
        { "@id": "1da76b63…", "key": "pAECIAMh…", "holder": "AlphaOrg" },
        { "@id": "80c817d6…", "key": "owEHAzgw…", "holder": "GammaOrg" },
        { "@id": "a684e60d…", "key": "owEBIAch…", "holder": "BetaOrg" }
      ],
      "minConsensus": 2,
      "signedAt": "2026-11-13T22:21:34.205Z",
      "validUntil": "2027-11-13T22:21:33.300Z",
      "signatures": [
        { "algorithm": "ES512", "keyId": "1da76b63…", "value": "ACVTGzee…" },
        { "algorithm": "Ed448", "keyId": "a684e60d…", "value": "BbI546eD…" }
      ]
    },
    "signingKeyCertificates": [
      {
        "roamingNetworkOperatorId": "OCC",
        "signingKey": { "@id": "40347caa…", "key": "pAECIAEh…", "notAfter": "2027-11-13T22:21:33.300Z", "holder": "BetaOrg", "usages": [ "party" ] },
        "signedAt": "2026-11-13T22:21:33.300Z",
        "algorithm": "Ed448",
        "keyId": "a684e60d…",
        "value": "7FuWSoa1…"
      },
      {
        "roamingNetworkOperatorId": "OCC",
        "signingKey": { "@id": "6a49d53d…", "key": "pAECIAEh…", "notAfter": "2027-11-13T22:21:33.300Z", "holder": "AlphaOrg", "usages": [ "party" ] },
        "signedAt": "2026-11-13T22:21:33.300Z",
        "algorithm": "ES512",
        "keyId": "1da76b63…",
        "value": "ABx+jh8Y…"
      },
      {
        "roamingNetworkOperatorId": "OCC",
        "signingKey": { "@id": "bf6017c7…", "key": "pAECIAEh…", "notAfter": "2027-11-13T22:21:33.300Z", "holder": "GammaOrg", "usages": [ "party" ] },
        "signedAt": "2026-11-13T22:21:33.300Z",
        "algorithm": "ML-DSA-65",
        "keyId": "80c817d6…",
        "value": "Tx7/KE+Z…"
      }
    ],
    "identityKeys": [
      { "@id": "1da76b63…", "key": "pAECIAMh…", "holder": "AlphaOrg" },
      { "@id": "80c817d6…", "key": "owEHAzgw…", "holder": "GammaOrg" },
      { "@id": "a684e60d…", "key": "owEBIAch…", "holder": "BetaOrg" }
    ],
    "created": "2026-11-13T22:21:33.573Z",
    "lastChange": "2026-11-13T22:21:34.253Z"
  },
  "urls": [ "https://alpha.example/RNs/QA", "https://beta.example/RNs/QA", "https://gamma.example/RNs/QA" ],
  "created": "2026-11-13T22:21:33.613Z",
  "lastChange": "2026-11-13T22:21:34.253Z",
  "eTags": [
    [ "json", "sha256", "base64", "JgRCDQM4…" ],
    [ "cbor", "sha256", "base64", "uVJQE6vR…" ],
    [ "effectiveTree", "sha256", "base64", "pnFpGrmB…" ]
  ]
}
```

`published/QA-revision2-commit.json`, also published as `QA-commit.json`. The change set has five signatures: the
identity key of `GammaOrg` (`80c817d6…`), the identity keys and the signing keys of `AlphaOrg` and
`BetaOrg`. The commit names the commit of revision 1 in its `parentIds` and is signed by the signing keys of all
three holders, the one of `GammaOrg` (`bf6017c7…`) certified in this very version:

```json
{
  "@id": "9eb5d1bc…",
  "@context": "https://open.charging.cloud/contexts/wwcp/roamingNetwork/commit/v3",
  "roamingNetworkId": "QA",
  "revision": 2,
  "eTags": [ [ "json", "sha256", "hex", "2604420d…" ], [ "cbor", "sha256", "hex", "b9525013…" ], [ "effectiveTree", "sha256", "hex", "a671691a…" ] ],
  "parentIds": [ "0a4866f4…" ],
  "changeSet": {
    "@id": "d2591af6…",
    "roamingNetworkId": "QA",
    "createdAt": "2026-11-13T22:21:34.253Z",
    "description": { "en": "GammaOrg joins AlphaOrg and BetaOrg: two of three" },
    "signatures": [
      { "algorithm": "ML-DSA-65", "keyId": "80c817d6…", "value": "K1u0ZyfA…" },
      { "algorithm": "ES512", "keyId": "1da76b63…", "value": "AVnz2VYe…" },
      { "algorithm": "ES256", "keyId": "6a49d53d…", "value": "GtznEo7q…" },
      { "algorithm": "Ed448", "keyId": "a684e60d…", "value": "Afi7n7R9…" },
      { "algorithm": "ES256", "keyId": "40347caa…", "value": "JHwmVk84…" }
    ],
    "changes": [
      {
        "kind": "UpdateProperty",
        "entityType": "RoamingNetworkOperator",
        "entityId": "OCC",
        "propertyName": "identityKeys",
        "oldValue": [
          { "@id": "1da76b63…", "key": "pAECIAMh…", "holder": "AlphaOrg" },
          { "@id": "a684e60d…", "key": "owEBIAch…", "holder": "BetaOrg" }
        ],
        "newValue": [
          { "@id": "1da76b63…", "key": "pAECIAMh…", "holder": "AlphaOrg" },
          { "@id": "80c817d6…", "key": "owEHAzgw…", "holder": "GammaOrg" },
          { "@id": "a684e60d…", "key": "owEBIAch…", "holder": "BetaOrg" }
        ]
      },
      {
        "kind": "UpdateProperty",
        "entityType": "RoamingNetworkOperator",
        "entityId": "OCC",
        "propertyName": "keyStatement",
        "oldValue": {
          "roamingNetworkId": "QA",
          "roamingNetworkOperatorId": "OCC",
          "identityKeys": [
            { "@id": "1da76b63…", "key": "pAECIAMh…", "holder": "AlphaOrg" },
            { "@id": "a684e60d…", "key": "owEBIAch…", "holder": "BetaOrg" }
          ],
          "minConsensus": 2,
          "signedAt": "2026-11-13T22:21:33.912Z",
          "validUntil": "2027-11-13T22:21:33.300Z",
          "signatures": [
            { "algorithm": "ES512", "keyId": "1da76b63…", "value": "AbJJoZYi…" },
            { "algorithm": "Ed448", "keyId": "a684e60d…", "value": "mZWgxXkh…" }
          ]
        },
        "newValue": {
          "roamingNetworkId": "QA",
          "roamingNetworkOperatorId": "OCC",
          "identityKeys": [
            { "@id": "1da76b63…", "key": "pAECIAMh…", "holder": "AlphaOrg" },
            { "@id": "80c817d6…", "key": "owEHAzgw…", "holder": "GammaOrg" },
            { "@id": "a684e60d…", "key": "owEBIAch…", "holder": "BetaOrg" }
          ],
          "minConsensus": 2,
          "signedAt": "2026-11-13T22:21:34.205Z",
          "validUntil": "2027-11-13T22:21:33.300Z",
          "signatures": [
            { "algorithm": "ES512", "keyId": "1da76b63…", "value": "ACVTGzee…" },
            { "algorithm": "Ed448", "keyId": "a684e60d…", "value": "BbI546eD…" }
          ]
        }
      },
      {
        "kind": "UpdateProperty",
        "entityType": "RoamingNetworkOperator",
        "entityId": "OCC",
        "propertyName": "signingKeyCertificates",
        "oldValue": [
          {
            "roamingNetworkOperatorId": "OCC",
            "signingKey": {
              "@id": "40347caa…",
              "key": "pAECIAEh…",
              "notAfter": "2027-11-13T22:21:33.300Z",
              "holder": "BetaOrg",
              "usages": [ "party" ]
            },
            "signedAt": "2026-11-13T22:21:33.300Z",
            "algorithm": "Ed448",
            "keyId": "a684e60d…",
            "value": "7FuWSoa1…"
          },
          {
            "roamingNetworkOperatorId": "OCC",
            "signingKey": {
              "@id": "6a49d53d…",
              "key": "pAECIAEh…",
              "notAfter": "2027-11-13T22:21:33.300Z",
              "holder": "AlphaOrg",
              "usages": [ "party" ]
            },
            "signedAt": "2026-11-13T22:21:33.300Z",
            "algorithm": "ES512",
            "keyId": "1da76b63…",
            "value": "ABx+jh8Y…"
          }
        ],
        "newValue": [
          {
            "roamingNetworkOperatorId": "OCC",
            "signingKey": {
              "@id": "40347caa…",
              "key": "pAECIAEh…",
              "notAfter": "2027-11-13T22:21:33.300Z",
              "holder": "BetaOrg",
              "usages": [ "party" ]
            },
            "signedAt": "2026-11-13T22:21:33.300Z",
            "algorithm": "Ed448",
            "keyId": "a684e60d…",
            "value": "7FuWSoa1…"
          },
          {
            "roamingNetworkOperatorId": "OCC",
            "signingKey": {
              "@id": "6a49d53d…",
              "key": "pAECIAEh…",
              "notAfter": "2027-11-13T22:21:33.300Z",
              "holder": "AlphaOrg",
              "usages": [ "party" ]
            },
            "signedAt": "2026-11-13T22:21:33.300Z",
            "algorithm": "ES512",
            "keyId": "1da76b63…",
            "value": "ABx+jh8Y…"
          },
          {
            "roamingNetworkOperatorId": "OCC",
            "signingKey": {
              "@id": "bf6017c7…",
              "key": "pAECIAEh…",
              "notAfter": "2027-11-13T22:21:33.300Z",
              "holder": "GammaOrg",
              "usages": [ "party" ]
            },
            "signedAt": "2026-11-13T22:21:33.300Z",
            "algorithm": "ML-DSA-65",
            "keyId": "80c817d6…",
            "value": "Tx7/KE+Z…"
          }
        ]
      },
      {
        "kind": "UpdateProperty",
        "entityType": "RoamingNetwork",
        "entityId": "QA",
        "propertyName": "urls",
        "oldValue": [ "https://alpha.example/RNs/QA", "https://beta.example/RNs/QA" ],
        "newValue": [ "https://alpha.example/RNs/QA", "https://beta.example/RNs/QA", "https://gamma.example/RNs/QA" ]
      }
    ],
    "beforeETags": [ [ "json", "sha256", "hex", "ecdee201…" ], [ "cbor", "sha256", "hex", "a3eb512d…" ] ],
    "afterETags": [ [ "json", "sha256", "hex", "2604420d…" ], [ "cbor", "sha256", "hex", "b9525013…" ] ]
  },
  "signatures": [
    {
      "role": "roamingNetworkOperator",
      "signer": "OCC",
      "signedAt": "2026-11-13T22:21:34.354Z",
      "algorithm": "ES256",
      "keyId": "6a49d53d…",
      "value": "PYvUvaDI…"
    },
    {
      "role": "roamingNetworkOperator",
      "signer": "OCC",
      "signedAt": "2026-11-13T22:21:34.356Z",
      "algorithm": "ES256",
      "keyId": "40347caa…",
      "value": "7sYr4+7h…"
    },
    {
      "role": "roamingNetworkOperator",
      "signer": "OCC",
      "signedAt": "2026-11-13T22:21:34.359Z",
      "algorithm": "ES256",
      "keyId": "bf6017c7…",
      "value": "ogLki5QV…"
    }
  ]
}
```

**The same way back.** A holder leaves as it joined: a change set of the identity keys, the key statement, the
certificates and the URLs without it, signed by the quorum before, which need not include the one leaving. From two
holders to one, both sign, and `minConsensus` is removed with the second key.


## 5. The trust anchor, as a QR code

What a verifier pins of `QA` (section 8.6): not one key, which would let its holder vouch for the network alone, but
the `@id`s of all identity keys, per holder, and the `minConsensus`, where its proofs are served, and the name and
description of the network to show. Each entry of `keys` is a holder: the `@id` of its key, or the array of the `@id`s
of its keys where it holds several, e.g. while it replaces one (example A11). A verifier counts the holders as the trust
anchor names them, never as a key statement names them, so no holder counts twice by naming a key of its own as
another's. `CreateTrustAnchor` takes the URLs the roaming network names, unless it is given others:

```csharp
var trustAnchor  = network.CreateTrustAnchor(network.Operator!.KeyStatement!);

File.WriteAllText($"published/{qaId}-trustAnchor-revision{head.Commit.Revision}.txt",  trustAnchor.ToQRCode());
```

The trust anchor of revision 2, also published as `QA-trustAnchor.txt`:

```json
{
  "@id": "QA",
  "@context": "https://open.charging.cloud/contexts/wwcp/roamingNetwork/trustAnchor/v2",
  "urls": [ "https://alpha.example/RNs/QA", "https://beta.example/RNs/QA", "https://gamma.example/RNs/QA" ],
  "keys": [
    "1da76b63b7e09f6b959623760eab4bca7dfc49950f8ecc1cc34f880a765a32df",
    "80c817d6da5793ce52a4e4f10a24c6c4700fdd9898e13ee16a248f820f0b6539",
    "a684e60d2d5969e7f216fccf6d7ce76330343f93710036ebf466c0d27ac33184"
  ],
  "name": { "en": "Quality Assurance" },
  "description": { "en": "The roaming network of the examples" },
  "minConsensus": 2
}
```

A verifier tries the next URL when one does not answer, e.g. a server down or attacked; the proofs are signed, so no
server needs to be trusted, and whoever reads the same head at all three need not believe any one of them. The QR code
holds `WWCP/TA-2:` and the Base45 (RFC 9285) of the deterministic CBOR of the same keys and values, the `@id`s as
their 32 bytes: here 387 bytes of CBOR, 591 characters, all of the alphanumeric mode of QR codes.

```
WWCP/TA-2:B7LJ683VCBCACVCC$C6REF6BVX3-PD2BN-6K:+IXL4K%17Q9W-FRD9M/17 P4VO-8HD/EIJ6F6BSCGR035RR%UI6KAJ.SVC1Y5PM7ES0SWEJW.7$IDI6IQ+1%ZCF6BD2LX3T:W5LHD9RU9/V%$DFBTA46U18%CEJ.6G+U.GOHNFVB6FVCBEC7%CYJCD/DIDAAEC-ED3EFFC8$QEVKE:.D  C6%E/VD-RGRQ3MWEW9E0G7O:5+VD 8D.%5$9FQ$DTVD9:5D-98:5KC8-K3MWEW9E0G7P:57$C%CCB$CBECP9ESZCAIACPEBCAD8FA9DIWENPEJ/553DN$D%CCB$CBECP9ESZCAIACPEBCA38DUPCG/DB$CQWEA19MWEW9E0G7%:5I9E -DNPCGECD3D3/D+%5*VD$$EQ:5-3E7WEBAFCPES4FVPC-:5S3EJ$D3/D:+9PWE04EYODKWE7%EGVE:.D59DQJEI.E.PD6$CXPC$EDBWE-3E4IK1$CK8F8UADZC0LEBECZEDU1D1/DPWE04EJODX3ES44+8DD44$9FQ$DTVDSQEJ$DC.D-3ELQEF/D7%E20
```

Without name and description it is also a URL, the holders separated by `,`, the keys of one holder by `;`, the
further URLs each an escaped `alsoAt`:

```
https://alpha.example/RNs/QA?keys=1da76b63…,80c817d6…,a684e60d…&minConsensus=2&alsoAt=https%3A%2F%2Fbeta.example%2FRNs%2FQA&alsoAt=https%3A%2F%2Fgamma.example%2FRNs%2FQA
```

POI_API serves it as `GET ~/RNs/QA/trustAnchor`, the URL of the request first, then the others `QA` names; the hub
shows it as a QR code on the page of the roaming network. A QR code is as trustworthy as the place it is found: on the
websites or in the contract documents of the three organizations, yes; one stuck on a charging station may be
anybody's.


## 6. The ETags, and what makes a version authentic and new

Every version of `QA` has exactly three ETags, all SHA-256 (section 3). Those of revision 2:

| Format | Digest over | Here (hex) |
|---|---|---|
| `json` | the canonical JSON (UTF-8) of the roaming network without its `eTags`, with its `@context` | `2604420d…` |
| `cbor` | the deterministic CBOR of the same JSON | `b9525013…` |
| `effectiveTree` | the root of the hash tree over its effective content (section 8): here the roaming network alone, its operator with keys, certificates, quorum and key statement included, as it has no charging station operators, pools or other children yet | `a671691a…` |

- **The same digests, twice:** the roaming network writes its ETags in Base64, the commit in hex - `JgRCDQM4…`
  is the Base64 of `2604420d…`. Commits and change sets always write hex, as their identifications and signatures
  are calculated over them as written; everywhere else Base64 is the default, and a reader takes both.
- **The commit identification** `9eb5d1bc…` is the SHA-256 of the canonical JSON of the commit without its
  `@id` and its signatures, with its `@context` (section 5.1). It names the version through its ETags: whoever has the
  identification knows the content.
- **The commit signatures** are each over the canonical JSON of the signature's own fields and the commit without its
  signatures (section 5.4): the holders sign the identification, and so the ETags, and so the content.
- **The quorum, the key statement, the certificates and the URLs** are part of the content: they are in all three
  ETags.

**Authentic.** So the chain is: identity keys of enough holders → key statement and certificates → signing keys →
commit signatures of enough holders → commit identification → ETags → content. A third party holding the trust anchor
checks it link by link, see the next section.

**New.** A commit names the commit before it in its `parentIds` and has the next `revision`: whoever knows revision 1
sees that revision 2 follows it, and takes nothing older. A commit alone does not say that it is still the head: for
that, the server attests its head, a statement of the commit, its revision and its ETag, signed by signing keys of
`minConsensus` holders and valid for minutes, at most a day (section 8.5, [example A10](exampleA10.md)). Over the
long run, checkpoints signed by independent witnesses show that a commit is part of the one history they saw
(section 10, examples [A4](exampleA4.md) to [A6](exampleA6.md)).


## 7. Checking `QA`, as a charging station operator or an app

`ExampleA1.Validate()` checks `QA` from the QR code and the two published files of the head alone, nothing of what
`Run()` made:

```csharp
// a) The trust anchor, as scanned.
if (!RoamingNetworkTrustAnchor.TryParse(File.ReadAllText("published/QA-trustAnchor.txt"), out var anchor, out var error))
    Helpers.Fail(error);

var networkJSON         = Helpers.ReadJSON("published/QA.json");
var commitJSON          = Helpers.ReadJSON("published/QA-commit.json");

// b) The roaming network: its @context, its content, the ETags it lists are those of its content, its quorum,
//    the key statement it embeds is signed by identity keys of enough of its holders, and every certificate of a
//    signing key by an identity key of the holder of that key.
if (!RoamingNetwork.TryParse(networkJSON, out var network, out error))
    Helpers.Fail(error);

if (network.Id != anchor.Id)
    Helpers.Fail("this is not the roaming network of the trust anchor");

// c) The commit: its @id is the digest of its content.
if (!RoamingNetworkCommit.TryParse(commitJSON, out var commit, out error))
    Helpers.Fail(error);

// d) The commit leads to this roaming network: its ETags are those of the network.
if (commit.RoamingNetworkId != network.Id || !network.MatchesCommitETags(commit.ETags))
    Helpers.Fail("the commit does not lead to the roaming network");

// e) The key statement is signed by trusted identity keys of as many holders as the trust anchor requires,
//    and is valid now.
var statement = network.Operator?.KeyStatement;

if (statement is null)
    Helpers.Fail("the roaming network operator states no keys");

//    The holders are counted as the trust anchor names them, and the key statement must name them so.
if (!statement.TryVerify(anchor.Holders,
                         network.Operator!.IdentityKeys,
                         out var holderOf,
                         out error,
                         anchor.MinConsensus,
                         At: Timestamp.Now))
    Helpers.Fail(error);

// f) The commit is signed, as the roaming network operator, by signing keys of as many holders, each certified by a
//    trusted identity key of its holder, valid when it signed, and one that may sign for the party. The identity keys
//    of the key statement checked in e) are trusted as well; a signature counts for the holder of the identity key
//    that certified its key, as e) knows it, never as the signing key names itself.
var trustedIds   = holderOf.Keys.ToHashSet();
var identityPool = statement.IdentityKeys.Concat(network.Operator.IdentityKeys).ToArray();
var signedBy     = new HashSet<String>();

foreach (var signature in commit.Signatures.Where(signature => signature.Role == RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator))
{

    var certificate = network.Operator.SigningKeyCertificates.
                          FirstOrDefault(certificate => certificate.SigningKey.Id == signature.KeyId         &&
                                                        certificate.SigningKey.IsValidAt(signature.SignedAt) &&
                                                        certificate.SigningKey.MaySign(POIKeyUsages.Party)   &&
                                                        certificate.TryVerify(identityPool, out _, out _, trustedIds));

    if (certificate is null)
        Helpers.Fail($"the key '{signature.KeyId}' is no signing key a trusted identity key of its holder certifies");

    if (!commit.VerifySignature(signature, certificate.SigningKey.Key, certificate.SigningKey.Id, out error))
        Helpers.Fail(error);

    signedBy.Add(holderOf[certificate.KeyId]);

}

if (signedBy.Count < Math.Max(anchor.MinConsensus, statement.MinConsensus))
    Helpers.Fail($"the commit is signed by {signedBy.Count} holder(s) of the roaming network operator only");

// g) What to show: the name and description of the checked network and the name of its operator, which the
//    signatures cover, not the texts of the QR code, which are not trusted.
Console.WriteLine($"Roaming network '{network.Id}'/'{network.Name.FirstText()}' ('{network.Description.FirstText()}'), operated by '{network.Operator.Id}'/'{network.Operator.Name.FirstText()}'");
```

What each step refuses:

| Step | Refuses |
|---|---|
| a) `RoamingNetworkTrustAnchor.TryParse` | a text that is neither `WWCP/TA-2:` and Base45 nor a URL of a trust anchor; `@id`s that are no 32 bytes; a URL that is not the one of the roaming network; a `minConsensus` above the number of identity keys |
| b) `RoamingNetwork.TryParse` | another `@context`; `eTags` that are not those of the content, e.g. after anything in it was changed; keys whose `@id` is not their thumbprint, or with private parts; `signingKeys` of the operator; a `minConsensus` of no more than half of the holders, or of more than there are; `urls` of another roaming network; a key statement or a certificate that does not fit the operator or is not signed as it must be |
| c) `RoamingNetworkCommit.TryParse` | an `@id` that is not the digest of the commit |
| d) `MatchesCommitETags` | a commit of another version or another roaming network |
| e) `TryVerify` | a statement not signed by pinned identity keys of as many holders as the trust anchor and the statement require - also a correct statement of somebody else; an expired one |
| f) certificates, `VerifySignature` | a commit signed by fewer holders than the quorum, by a key no trusted identity key of its holder certifies, one not valid at `signedAt` or not for the `party`, or whose signature does not verify |

Step b) alone does not protect anything: `eTags` and the key statement may be left out, and whoever changes `QA`
leaves them out. Step d) is what binds the roaming network to the commit the holders signed; step e) is what refuses a
version without a key statement, or with one of only some of the pinned holders. Step g) shows the texts the
signatures cover, and warns where the QR code names the network otherwise.

`Helpers.ReadJSON` reads the files as POI reads JSON, with `POIJSON.Read` (section 1 of the specification): timestamps
stay the text they are signed as, numbers the decimals they are, as written, and a property named twice, a number a
decimal cannot hold as written and a second value after the first are refused. Read with `JObject.Parse`, `signedAt`
would become a date, and the signatures would not verify. `RoamingNetwork.TryParse`, `RoamingNetworkCommit.TryParse`
and the others also take the text itself, and read it so.

The ETags can also be calculated by hand, from the JSON alone, without the POI model, except for the tree; the CBOR
from the canonical JSON as its bytes, so that a number with decimal places is a decimal fraction, as section 1 asks:

```csharp
var content    = (JObject) networkJSON.DeepClone();
content.Remove("eTags");

var json       = ETag.Compute(ETagFormat.JSON, CanonicalJSON.ToUTF8Bytes(content));
var cbor       = ETag.Compute(ETagFormat.CBOR, CBORJSON.ToCBOR(CanonicalJSON.ToUTF8Bytes(content)).ToByteArray(CBORWriterOptions.Canonical));
var tree       = network.EffectiveTreeETag(ETagHashAlgorithm.SHA256);
```

```
Roaming network 'QA'/'Quality Assurance' ('The roaming network of the examples'), operated by 'OCC'/'Open Charging Cloud'

json          json:sha256:hex:2604420d0338e40aa5a9d1282b3296d5b0d30f1b88ebf44034eccf42cb5f46a4
cbor          cbor:sha256:hex:b9525013abd1b1587399f28f7c536ef3b3319cdbf3e30f1d629d33fd7fcee233
effectiveTree effectiveTree:sha256:hex:a671691ab9814aa13466fdd34553a98e81f3867ee573c36c10ea2d3677b1f1c3
the commit's  JSON, CBOR, EffectiveTree: the same
```


## 8. From an older trust anchor to the keys of today

Somebody scanned the QR code of `AlphaOrg` at revision 0 and pinned one holder with a quorum of one. Today `QA` is
signed by two of three. The key statement of today alone does not get it there: it is signed by one holder it knows,
and two decide today. But every key statement was signed by the quorum of the one before it, so it follows them one
after the other. POI_API serves them as `GET ~/RNs/QA/keyStatements`, the oldest the server knows of first, the one of
the head last; `published/QA-keyStatements.json`:

```json
{
  "keyStatements": [
    {
      "roamingNetworkId": "QA",
      "roamingNetworkOperatorId": "OCC",
      "identityKeys": [ { "@id": "1da76b63…", "key": "pAECIAMh…", "holder": "AlphaOrg" } ],
      "minConsensus": 1,
      "signedAt": "2026-11-13T22:21:33.300Z",
      "validUntil": "2027-11-13T22:21:33.300Z",
      "signatures": [ { "algorithm": "ES512", "keyId": "1da76b63…", "value": "Ab7ra0AK…" } ]
    },
    {
      "roamingNetworkId": "QA",
      "roamingNetworkOperatorId": "OCC",
      "identityKeys": [
        { "@id": "1da76b63…", "key": "pAECIAMh…", "holder": "AlphaOrg" },
        { "@id": "a684e60d…", "key": "owEBIAch…", "holder": "BetaOrg" }
      ],
      "minConsensus": 2,
      "signedAt": "2026-11-13T22:21:33.912Z",
      "validUntil": "2027-11-13T22:21:33.300Z",
      "signatures": [
        { "algorithm": "ES512", "keyId": "1da76b63…", "value": "AbJJoZYi…" },
        { "algorithm": "Ed448", "keyId": "a684e60d…", "value": "mZWgxXkh…" }
      ]
    },
    {
      "roamingNetworkId": "QA",
      "roamingNetworkOperatorId": "OCC",
      "identityKeys": [
        { "@id": "1da76b63…", "key": "pAECIAMh…", "holder": "AlphaOrg" },
        { "@id": "80c817d6…", "key": "owEHAzgw…", "holder": "GammaOrg" },
        { "@id": "a684e60d…", "key": "owEBIAch…", "holder": "BetaOrg" }
      ],
      "minConsensus": 2,
      "signedAt": "2026-11-13T22:21:34.205Z",
      "validUntil": "2027-11-13T22:21:33.300Z",
      "signatures": [
        { "algorithm": "ES512", "keyId": "1da76b63…", "value": "ACVTGzee…" },
        { "algorithm": "Ed448", "keyId": "a684e60d…", "value": "BbI546eD…" }
      ]
    }
  ]
}
```

`ExampleA1.ValidateSuccession()` reads the QR code of revision 0, the key statements, and the QR code of today to
compare with:

```csharp
Console.WriteLine($"The key statement of today alone: {(keyStatements[^1].TryVerify(pinned.Holders, null, out _, out error, pinned.MinConsensus, At: Timestamp.Now) ? "accepted" : error)}");

// One after the other: each signed by the quorum of the one before, among the keys that one names, and by its own.
if (!RoamingNetworkKeyStatement.TryVerifyChain(keyStatements,
                                               pinned.Holders,
                                               out var verified,
                                               out var holderOf,
                                               out error,
                                               pinned.MinConsensus,
                                               At: Timestamp.Now))
    Helpers.Fail(error);
```

```
Pinned at revision 0: 1 holder, minConsensus 1; today: 3 holders, minConsensus 2; 3 key statements
The key statement of today alone: The key statement is signed by trusted identity keys of 1 holder, not of 2!
One after the other: the keys of AlphaOrg, BetaOrg, GammaOrg, minConsensus 2
They are the keys and the quorum of the trust anchor of today.
```

`TryVerifyChain` (section 8.6, Succession) takes the first statement the pinned keys verify as a single one is
verified, here the one of `AlphaOrg` alone, and then every statement after it: of the same roaming network and
operator, signed not before the one before it and while that one was valid, signed by identity keys the one before
names, of as many of its holders as *its* `minConsensus` requires, and signed by its own quorum. The last one must be
valid now. A statement missing where two had to sign, or one the holders before did not sign, ends the chain: nothing
of it is trusted then. A server that dropped older commits (example A7) or began from a snapshot gives the key
statements from the oldest it still knows of; whoever pinned keys older than that needs a newer trust anchor.

A server or a witness following the history itself needs none of this: it takes every commit, and with it every
change of the holders, checked as it is appended ([example A6](exampleA6.md)).


## 9. If `GammaOrg` left again: two of two

A consortium shrinks as it grows: by one change set, signed by the quorum of the version before it. What it took if
`GammaOrg` left is tried here on the head of `QA` and **not committed**: the examples after this one go on with the
three.

The key statement of the two that stay names their identity keys and a quorum of two. `AlphaOrg` and `BetaOrg` sign
it: the quorum so far, two of three, and its own, two of two.

```csharp
var keyStatement          = beta.CoSign(
                                alpha.StateKeys(
                                    qa.Id,
                                    qa.Operator!.Id,
                                    IdentityKeys:  [ alpha.IdentityPublicKey, beta.IdentityPublicKey ],
                                    MinConsensus:  minConsensus,
                                    SignedAt:      now,
                                    ValidUntil:    now.AddYears(1)
                                )
                            );
```

`Dismiss` prepares the change set, the opposite of `Admit`. It takes the name of the shareholder, as its keys name it
as their `holder`, and the URL of its copy: nothing of `GammaOrg` is needed but what `QA` names.

```csharp
var gammaLeaves           = qa.Dismiss(
                                A1.GammaOrg.Name,
                                keyStatement,
                                A1.GammaOrg.URL,
                                Description:  ImmutableI18NString.Create(Languages.en, "GammaOrg leaves AlphaOrg and BetaOrg: two of two")
                            );
```

| Change | Of | Property | New value |
|---|---|---|---|
| 0 | `OCC` | `identityKeys` | the identity keys without those of `GammaOrg` |
| 1 | `OCC` | `keyStatement` | the key statement of `AlphaOrg` and `BetaOrg` |
| 2 | `OCC` | `signingKeyCertificates` | the certificates without those of the signing keys of `GammaOrg` |
| 3 | `QA` | `urls` | the URLs without the one of `GammaOrg` |

The quorum stays two, of two now, so `minConsensus` is not changed. Where it changes it is the fourth change of
five; a `minConsensus` of one, as when the second of two leaves, is removed, as it is not written.

It is signed as every change of the keys: by identity keys of as many holders as the version before requires, and by
their signing keys, as the key statement and the URLs are content of `OCC` and `QA`. No new identity key enters, so
none proves anything.

```csharp
var byAlphaOrg            = alpha.SignWithSigningKey(alpha.SignWithIdentityKey(gammaLeaves));

qa.TryApplyChangeSet(byAlphaOrg, out _, out var error);

var byAlphaAndBetaOrg     = beta.SignWithSigningKey(beta.SignWithIdentityKey(byAlphaOrg));

qa.TryApplyChangeSet(byAlphaAndBetaOrg, out var without, out error);
```

```
GammaOrg leaves, signed by AlphaOrg only: Change 0: UpdateProperty of the RoamingNetworkOperator 'OCC' ('identityKeys') needs signatures by identity keys of 2 holders of the roaming network operator 'OCC'!
GammaOrg leaves, signed by AlphaOrg and BetaOrg: applies, 4 changes
  holders AlphaOrg, BetaOrg; minConsensus 2; 2 certificates
  urls https://alpha.example/RNs/QA, https://beta.example/RNs/QA
  trust anchor: QR code of 495 characters
```

**Dismissed, or resigning.** `GammaOrg` is not asked: two of three are the quorum, and they dismiss the third without
it. Of two deciding together it is otherwise: both are the quorum, so the one leaving signs as well. It resigns;
nobody is dismissed by the other one alone. And the last shareholder does not leave at all: `Dismiss` refuses it.

The trust anchor is the one of revision 1 again in what it pins, two holders and a quorum of two, with the URLs of
two copies. Whoever pinned the trust anchor of today, of three holders, follows the key statements to the two, as in
[section 8](#8-from-an-older-trust-anchor-to-the-keys-of-today): the one more is signed by two of the three it
trusts.

```csharp
RoamingNetworkKeyStatement.TryVerifyChain([ .. A1.History.KeyStatements(), keyStatement ],
                                          A1.TrustAnchor.Holders,
                                          out var verified,
                                          out var holderOf,
                                          out error,
                                          A1.TrustAnchor.MinConsensus,
                                          At: Timestamp.Now)
```

```
From the trust anchor of today: the keys of AlphaOrg, BetaOrg, minConsensus 2
QA stays at revision 2: AlphaOrg, BetaOrg, GammaOrg
```

Committed, it were revision 3, signed by the signing keys of `AlphaOrg` and `BetaOrg` only, as every commit after
it: `history.Signers` without the signer of `GammaOrg`, set before `history.Commit`. The version of revision 3
certifies the signing key of `GammaOrg` no longer, and whoever checks a commit checks its signatures in the version
it leads to. So a history refuses a commit signed by a key that very commit takes its role from:

```
The commit would be signed by roamingNetworkOperator OCC with '…' (ES256), a key it takes the role 'roamingNetworkOperator' from: the version it leads to does not let it sign in it. Change the signers of this history first!
```


## 10. Where example A2 starts

Checked, `QA` is a version a charging station operator may build on:

```
QA is checked: revision 2, commit 9eb5d1bc5d9e5cd3b8656dafae1d0948f722f1245989960b0858e4c5f4b1ae54, signed by AlphaOrg and BetaOrg and GammaOrg
beforeETags of the next change set: json:sha256:hex:2604420d0338e40aa5a9d1282b3296d5b0d30f1b88ebf44034eccf42cb5f46a4, cbor:sha256:hex:b9525013abd1b1587399f28f7c536ef3b3319cdbf3e30f1d629d33fd7fcee233
```

- **The head:** revision 2, commit `9eb5d1bc…`. The next commit names it in its `parentIds` and has revision 3.
- **The `beforeETags`** of the next change set are the `json` and `cbor` ETags of this version, in hex
  (`CalcChangeSetETags()`): a change set applies only to the version it names, and is refused on any other (section 4).
  The `effectiveTree` ETag is the commit's, not the change set's.
- **The signing keys of `OCC`** are trusted now, as long as their certificates and the key statement are: the next
  commits are checked as in step f). Adding a charging station operator is the RNO's, so its change set needs
  signatures of two holders as well - and of the identity keys of the new operator, see [example A2](exampleA2.md).

A server following `QA` starts its own history from the checked version with
`RoamingNetworkHistory.TryFromSnapshot(network, commit, …)`, which checks d) as well.
