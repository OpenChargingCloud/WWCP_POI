# Example A1: The creation of the roaming network `QA`, and how to check it

The roaming network operator (RNO) *"Open Charging Cloud (OCC)"* is not a single company. In this example three
organizations, `AlphaOrg`, `BetaOrg` and `GammaOrg`, hold the keys of the operator, and two of them decide: a 2/3
quorum. They create the roaming network `QA` and publish it with its ETags, its genesis commit and its trust anchor.
The trust anchor fits into a QR code, so that it is easily handed to everybody who uses `QA`. A charging station
operator (CSO), or the app of an EV driver, who knows nothing of `QA` but this QR code, then checks what it reads from
any source: that at least two of the three holders of the operator's keys signed it, and that nothing in it changed
since. Once the user trusts the **trust anchor**, every later change of the roaming network can be checked the same way.

This checked version of `QA` is where [example A2](exampleA2.md) starts: the charging station operator `DE*GEF` asks
to be added to it.


## The program

The examples are one console project, [csharp/](csharp/), referencing `WWCP_POI3`: one file per example,
[exampleA1.cs](csharp/exampleA1.cs), [exampleA2.cs](csharp/exampleA2.cs), [exampleA3.cs](csharp/exampleA3.cs), and
[Program.cs](csharp/Program.cs) calling them in their order. What one example makes, the next is given:
`ExampleA1.Run()` returns the private keys of the three organizations, the trust anchor and the history of `QA`, whose
head is the current version of `QA`. Each check, `ExampleA1.Validate()` here, reads only what is published, as a
charging station operator or an app would.

```csharp
using var a1 = ExampleA1.Run();

ExampleA1.Validate();
```

```
dotnet run
```

Run in [csharp/](csharp/), it writes the private keys of each organization into a directory of its own
(`AlphaOrg/`, `BetaOrg/`, `GammaOrg/`), what everybody may read into `published/`, and the history of `QA` into
`poi/QA/`; all of them are ignored by git. The output below is what one run printed, `…` for elided keys, signatures and digests: the keys are
new in every run, and so are all digests.


## 1. The identity keys of the holders

Every party of a roaming network has two kinds of keys ([SPECIFICATION.md](../SPECIFICATION.md), section 4.1):

- **identity keys**, the long-term keys: they state the keys and the quorum of the operator, and certify its signing
  keys. They are what a verifier pins, and they are kept offline as far as possible.
- **signing keys**, the everyday keys: they sign commits, change sets and attestations.

Each organization makes its identity key, and keeps its private key to itself. Each chooses its own kind of key:

```csharp
var identityKeys          = new Dictionary<String, COSEKeyPair> {
                                { "AlphaOrg", COSEKeyPair.NewES512()   },     // ECDSA P-521
                                { "BetaOrg",  COSEKeyPair.NewEd448()   },     // EdDSA Ed448
                                { "GammaOrg", COSEKeyPair.NewMLDsa65() }      // ML-DSA-65, post-quantum
                            };

foreach (var organization in identityKeys.Keys)
{
    Directory.CreateDirectory(organization);
    Helpers.SaveKey(identityKeys[organization].Private, $"{organization}/identity-privateKey.pem");
}

// The public halves, each naming its organization.
var publicIdentityKeys    = identityKeys.Keys.Select(organization => POIPublicKey.From(identityKeys[organization].Private, Holder: organization)).ToArray();
```

A `COSEKeyPair` is a key pair with its signature algorithm: whatever it signs, it signs with the algorithm of its
kind. In reality each organization runs its part on its own machine. A public key is written as its `@id`, the
SHA-256 thumbprint of the COSE key (RFC 9679), its `key`, the Base64 of its deterministic CBOR, and its `holder`, which
is no part of the thumbprint.

**The quorum.** `minConsensus` 2 of 3 holders: wherever the RNO must sign - the roaming network itself, adding and
removing parties, its own keys - signatures of two different holders are needed. Holders are counted, not keys: a
holder renewing its key, or naming two identity keys, still has one voice. `minConsensus` must be more than half of
the holders, so that any two quorums share a holder: two contradicting decisions need one holder signing both, and
that holder can be named. An odd number of holders is not needed, 3 of 4 is fine as well; a single holder is
`minConsensus` 1, which is not written.


## 2. The key statement

The identity keys sign a **key statement** for `QA` (section 8.6): these are the identity keys of `OCC` in `QA`, and
this is its quorum, until `validUntil`. `AlphaOrg` makes and signs it, `BetaOrg` adds its signature to the same
statement: the quorum of two holders is reached.

```csharp
var now                   = Timestamp.Now;
var occId                 = RoamingNetworkOperator_Id.Parse("OCC");
var qaId                  = RoamingNetwork_Id.        Parse("QA");

// AlphaOrg creates and signs the key statement.
var keyStatementAlphaOrg  = RoamingNetworkKeyStatement.Create(
                                identityKeys["AlphaOrg"],
                                minConsensus,
                                qaId,
                                occId,
                                IdentityKeys:  publicIdentityKeys,
                                SignedAt:      now,
                                ValidUntil:    now.AddYears(1)
                            );

// BetaOrg later also signs the key statement: 2/3 quorum reached!
var keyStatementFinal     = keyStatementAlphaOrg.WithSignature(identityKeys["BetaOrg"]);
```

```json
{
  "roamingNetworkId":         "QA",
  "roamingNetworkOperatorId": "OCC",
  "identityKeys": [
    { "@id": "7c985fc5…", "key": "owEBIAch…", "holder": "BetaOrg"  },
    { "@id": "ae7801c5…", "key": "owEHAzgw…", "holder": "GammaOrg" },
    { "@id": "cdac20e0…", "key": "pAECIAMh…", "holder": "AlphaOrg" }
  ],
  "minConsensus": 2,
  "signedAt":     "2026-10-22T18:21:14.119Z",
  "validUntil":   "2027-10-22T18:21:14.119Z",
  "signatures": [
    { "algorithm": "ES512", "keyId": "cdac20e0…", "value": "AW7Lv9/a…" },
    { "algorithm": "Ed448", "keyId": "7c985fc5…", "value": "3tVIUqSq…" }
  ]
}
```

Each signature is over the canonical JSON of the statement without `signatures`, with its own `algorithm` and `keyId`
and the `@context` `…/roamingNetwork/keyStatement/v3`: every holder signs the same statement on its own, in any
order. Keys are written in the order of their `@id`s. The statement names no signing keys: each holder certifies its
own, see the next step.


## 3. The signing keys, each certified by its holder

Each organization makes a signing key for everyday use (P-256), for the `party` only: it signs what the operator owns,
see `POIKeyUsages`. Each certifies its own signing key with its identity key, alone: renewing it later needs nobody
else, as a signing key gives its holder one voice only, however it comes to be trusted.

```csharp
var signingKeys           = identityKeys.Keys.ToDictionary(organization => organization,
                                                           organization => COSEKeyPair.NewES256());

var publicSigningKeys     = identityKeys.Keys.Select(organization => POIPublicKey.From(signingKeys[organization].Private,
                                                                                       NotAfter:  now.AddYears(1),
                                                                                       Holder:    organization,
                                                                                       Usages:    [ POIKeyUsages.Party ])).ToArray();

var certificates          = publicSigningKeys.Select(signingKey => RoamingNetworkSigningKeyCertificate.Create(
                                                                       identityKeys[signingKey.Holder!],
                                                                       occId,
                                                                       signingKey,
                                                                       now
                                                                   )).ToArray();
```

```json
{
  "roamingNetworkOperatorId": "OCC",
  "signingKey": { "@id": "3b4eb255…", "key": "pAECIAEh…", "notAfter": "2027-10-22T18:21:14.119Z", "holder": "AlphaOrg", "usages": [ "party" ] },
  "signedAt":   "2026-10-22T18:21:14.119Z",
  "algorithm":  "ES512",
  "keyId":      "cdac20e0…",
  "value":      "AdxtnQcb…"
}
```

A certificate (`…/roamingNetwork/signingKeyCertificate/v2`) names the operator, not a network: the same signing keys
serve in every roaming network `OCC` operates. Those of `BetaOrg` and `GammaOrg` are signed with Ed448 and ML-DSA-65,
by their identity keys.


## 4. `OCC` and `QA`

The operator takes its identity keys and its quorum from the key statement, and its signing keys from their
certificates; the roaming network embeds its operator and names no keys of its own:

```csharp
var occ                   = new RoamingNetworkOperator(
                                keyStatementFinal,
                                Name:                    I18NString.Create("Open Charging Cloud"),
                                SigningKeyCertificates:  certificates
                            );

var qa                    = occ.CreateRoamingNetwork(
                                keyStatementFinal.RoamingNetworkId,
                                Name:          ImmutableI18NString.Create("Quality Assurance"),
                                Description:   ImmutableI18NString.Create("The roaming network of the examples")
                            );
```


## 5. The trust anchor, as a QR code

What a verifier pins of `QA` (section 8.6): not one key, which would let its holder vouch for the network alone, but
the `@id`s of all three identity keys and the `minConsensus`, where its proofs are served, and the name and description
of the network to show:

```csharp
var trustAnchor           = qa.CreateTrustAnchor(
                                keyStatementFinal,
                                URL.Parse("https://open.charging.cloud/RNs/QA")
                            );

Directory.CreateDirectory("published");
File.WriteAllText("published/QA-trustAnchor.txt", trustAnchor.ToQRCode());
```

```json
{
  "@id":      "QA",
  "@context": "https://open.charging.cloud/contexts/wwcp/roamingNetwork/trustAnchor/v2",
  "urls":     [ "https://open.charging.cloud/RNs/QA" ],
  "keys": [
    "7c985fc5204d46bb259cf8225949911e648f21655bf1abebc872c8cbce9e3b5f",
    "ae7801c51c82228f21b02d7a01300d4757a4c15c51abbf1886fd8b422370c09e",
    "cdac20e06feeee1fb59b4a3b7179e316c1b0b3a03b782e216bcd977b944d585a"
  ],
  "name":         { "en": "Quality Assurance" },
  "description":  { "en": "The roaming network of the examples" },
  "minConsensus": 2
}
```

`CreateTrustAnchor` takes more than one URL as well, the preferred one first, e.g. a mirror for when one server is down
or attacked; the proofs are signed, so no server needs to be trusted. The QR code holds `WWCP/TA-2:` and the Base45
(RFC 9285) of the deterministic CBOR of the same keys and values, the `@id`s as their 32 bytes: here 334 bytes of CBOR,
511 characters, all of the alphanumeric mode of QR codes.

```
WWCP/TA-2:B7LJ683VCBCACVCC$C6REF6B XF$4CY34H.8/X4RGV.CBPFI3WC:942SB1XLEFPDHPJ5QYM7F6BO2M3A08R3RG4TB4WX5Y60OU1Q3B0KOREA57O.2HARHRL4ZFOF6B20Q174Y6ET4U6/MDH9OFE*VS*LO*VMEN7J$5CSDY6JUXIS7BFVCBEC7%CYJCD/DIDAAEC-ED3EFFC8$QEVKE:.D  C6%E/VDOGGXF4MWEW9E0G7%:5I9E -DNPCGECD3D3/D+%5*VD$$E9:5D-98:54C8D68-3E7WEBAFB9FA9DIWENPEJ/5/3E1$C+%5 8D$KEZED:1DRPC34E.TCUPCG/DB$CLWE10684F98E0LEBECZEDV2D7$CK4F-KE/:56LE QEAC8NPC04E006$G66VCJQE*KEX9E EDG:DYJCK/D:K4+8DQ44S3EJ$D3/DM447$CK4F-KEN44:+CAWEDZCB$CBECP9E6$C$VDZEDSN8F/D1$C$QECOE
```

Without name and description it is also a URL, the further URLs each an escaped `alsoAt`:

```
https://open.charging.cloud/RNs/QA?keys=7c985fc5…,ae7801c5…,cdac20e0…&minConsensus=2
```

POI_API serves it as `GET ~/RNs/QA/trustAnchor`, the hub shows it as a QR code on the page of the roaming network. A QR
code is as trustworthy as the place it is found: on the website or in the contract documents of `OCC`, yes; one stuck
on a charging station may be anybody's.


## 6. The history and its genesis commit

`RoamingNetworkHistory.Start` makes the genesis commit, revision 0, signed by the signing keys of all three
organizations in the role of the roaming network operator, and keeps the history in `poi/QA`. The store stays open for
the examples building on this one; disposing the result of `Run()` closes it.

```csharp
RoamingNetworkCommitSigner SignerOf(String Holder)

    => new (signingKeys[Holder].Private,
            POIPublicKey.From(signingKeys[Holder].Private).Id,
            signingKeys[Holder].Algorithm,
            RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator,
            occId.ToString());

RoamingNetworkCommitSigner[] signers = [
    SignerOf("AlphaOrg"),
    SignerOf("BetaOrg"),
    SignerOf("GammaOrg")
];

var store                 = new RoamingNetworkHistoryFileStore($"poi/{qa.Id}");
var history               = RoamingNetworkHistory.Start(
                                qa,
                                Signers:  signers,
                                Store:    store
                            );

// What everybody may read: the roaming network with its ETags, and the commit leading to it.
File.WriteAllText($"published/{qa.Id}.json",         history.Head.RoamingNetwork.ToJSON(IncludeETags: true).ToString());
File.WriteAllText($"published/{qa.Id}-commit.json",  history.Head.Commit.        ToJSON().                  ToString());
```

`published/` stands for what everybody may read; POI_API serves the same as `GET ~/RNs/QA` and
`GET ~/RNs/QA/history/commits/{commitId}`. `published/QA.json`, the roaming network:

```json
{
  "@id":         "QA",
  "@context":    "https://open.charging.cloud/contexts/wwcp/roamingNetwork/v1",
  "name":        { "en": "Quality Assurance" },
  "description": { "en": "The roaming network of the examples" },
  "operator": {
    "@id":          "OCC",
    "name":         { "en": "Open Charging Cloud" },
    "minConsensus": 2,
    "keyStatement": { "roamingNetworkId": "QA", "roamingNetworkOperatorId": "OCC", … },
    "signingKeyCertificates": [
      { "roamingNetworkOperatorId": "OCC", "signingKey": { "@id": "3b4eb255…", …, "holder": "AlphaOrg", "usages": [ "party" ] }, "algorithm": "ES512",     "keyId": "cdac20e0…", … },
      { "roamingNetworkOperatorId": "OCC", "signingKey": { "@id": "9d2fa172…", …, "holder": "BetaOrg",  "usages": [ "party" ] }, "algorithm": "Ed448",     "keyId": "7c985fc5…", … },
      { "roamingNetworkOperatorId": "OCC", "signingKey": { "@id": "ea86663c…", …, "holder": "GammaOrg", "usages": [ "party" ] }, "algorithm": "ML-DSA-65", "keyId": "ae7801c5…", … }
    ],
    "identityKeys": [ { "@id": "7c985fc5…", …, "holder": "BetaOrg" }, { "@id": "ae7801c5…", …, "holder": "GammaOrg" }, { "@id": "cdac20e0…", …, "holder": "AlphaOrg" } ],
    "created":      "2026-10-22T18:21:14.281Z",
    "lastChange":   "2026-10-22T18:21:14.281Z"
  },
  "created":    "2026-10-22T18:21:14.324Z",
  "lastChange": "2026-10-22T18:21:14.324Z",
  "eTags": [
    [ "json",          "sha256", "base64", "7TO7e4dX…" ],
    [ "cbor",          "sha256", "base64", "5AUuX8S8…" ],
    [ "effectiveTree", "sha256", "base64", "hEcguIgh…" ]
  ]
}
```

The operator writes no `signingKeys`: they are those of its `signingKeyCertificates`, and a representation naming
`signingKeys` of the operator is refused. `published/QA-commit.json`, the genesis commit:

```json
{
  "@id":              "be1e93ba91b381903808ed7f68e4f454f9459ed286171377c440e2331cb58704",
  "@context":         "https://open.charging.cloud/contexts/wwcp/roamingNetwork/commit/v3",
  "roamingNetworkId": "QA",
  "revision":         0,
  "eTags": [
    [ "json",          "sha256", "hex", "ed33bb7b8757e8ac0fc44f2febf980528f0775c710aa2a3633ba4369ae406917" ],
    [ "cbor",          "sha256", "hex", "e4052e5fc4bce9a4440e721a5cbb5e3a6fddd61ab9f84c61471b534c656a3711" ],
    [ "effectiveTree", "sha256", "hex", "844720b88821f5e2e3dcbba882475fe54005feb7ba61301834fd7061fbac5001" ]
  ],
  "signatures": [
    { "role": "roamingNetworkOperator", "signer": "OCC", "signedAt": "2026-10-22T18:21:14.570Z", "algorithm": "ES256", "keyId": "3b4eb255…", "value": "tWnXvQkp…" },
    { "role": "roamingNetworkOperator", "signer": "OCC", "signedAt": "2026-10-22T18:21:14.575Z", "algorithm": "ES256", "keyId": "9d2fa172…", "value": "ki5q2/kk…" },
    { "role": "roamingNetworkOperator", "signer": "OCC", "signedAt": "2026-10-22T18:21:14.578Z", "algorithm": "ES256", "keyId": "ea86663c…", "value": "xpLOyP7H…" }
  ]
}
```

A genesis commit has neither `parentIds` nor a `changeSet`: it is the version itself, not a change of another. Its
three signatures are by the signing keys of `AlphaOrg` (`3b4eb255…`), `BetaOrg` (`9d2fa172…`) and `GammaOrg`
(`ea86663c…`); two of them would be enough.


## 7. The ETags after `QA` is made

After the genesis, `QA` has exactly three ETags, all SHA-256 (section 3):

| Format | Digest over | Here (hex) |
|---|---|---|
| `json` | the canonical JSON (UTF-8) of the roaming network without its `eTags`, with its `@context` | `ed33bb7b…` |
| `cbor` | the deterministic CBOR of the same JSON | `e4052e5f…` |
| `effectiveTree` | the root of the hash tree over its effective content (section 8): here the roaming network alone, its operator with keys, certificates, quorum and key statement included, as it has no charging station operators, pools or other children yet | `844720b8…` |

- **The same digests, twice:** the roaming network writes its ETags in Base64, the commit in hex - `7TO7e4dX…` is the
  Base64 of `ed33bb7b…`. Commits and change sets always write hex, as their identifications and signatures are
  calculated over them as written; everywhere else Base64 is the default, and a reader takes both.
- **The commit identification** `be1e93ba…` is the SHA-256 of the canonical JSON of the commit without its `@id` and
  its signatures, with its `@context` (section 5.1). It names the version through its ETags: whoever has the
  identification knows the content.
- **The commit signatures** are each over the canonical JSON of the signature's own fields and the commit without its
  signatures (section 5.4): the holders sign the identification, and so the ETags, and so the content.
- **The quorum, the key statement and the certificates** are part of the content: they are in all three ETags.

So the chain is: identity keys of two holders → key statement and certificates → signing keys → commit signatures of
two holders → commit identification → ETags → content.


## 8. Checking `QA`, as a charging station operator or an app

`ExampleA1.Validate()` checks `QA` from the QR code and the two published files alone, nothing of what `Run()` made:

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

if (!statement.TryVerify(anchor.Keys,
                         network.Operator!.IdentityKeys,
                         out error,
                         anchor.MinConsensus,
                         At: Timestamp.Now))
    Helpers.Fail(error);

// f) The commit is signed, as the roaming network operator, by signing keys of as many holders, each certified by a
//    trusted identity key of its holder, valid when it signed, and one that may sign for the party. The identity keys
//    of the key statement checked in e) are trusted as well.
var trustedIds   = anchor.Keys.Concat(statement.IdentityKeys.Select(identityKey => identityKey.Id)).ToHashSet();
var identityPool = statement.IdentityKeys.Concat(network.Operator.IdentityKeys).ToArray();
var signedBy     = new HashSet<String>();

foreach (var signature in commit.Signatures.Where(signature => signature.Role == RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator))
{

    var key = network.Operator.SigningKeyCertificates.
                    FirstOrDefault(certificate => certificate.SigningKey.Id == signature.KeyId         &&
                                                  certificate.SigningKey.IsValidAt(signature.SignedAt) &&
                                                  certificate.SigningKey.MaySign(POIKeyUsages.Party)   &&
                                                  certificate.TryVerify(identityPool, out _, out _, trustedIds))?.
                    SigningKey;

    if (key is null)
        Helpers.Fail($"the key '{signature.KeyId}' is no signing key a trusted identity key of its holder certifies");

    if (!commit.VerifySignature(signature, key.Key, key.Id, out error))
        Helpers.Fail(error);

    signedBy.Add(key.HolderOrUnnamed);

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
| b) `RoamingNetwork.TryParse` | another `@context`; `eTags` that are not those of the content, e.g. after anything in it was changed; keys whose `@id` is not their thumbprint, or with private parts; `signingKeys` of the operator; a `minConsensus` of no more than half of the holders; a key statement naming another roaming network or operator, other identity keys or another `minConsensus`, or not signed by identity keys of `minConsensus` holders; a certificate not signed by an identity key of the holder of its signing key. Not a roaming network without `eTags`: they may be left out |
| c) `RoamingNetworkCommit.TryParse` | an `@id` that is not the digest of the commit |
| d) `MatchesCommitETags` | a commit of another version or another roaming network |
| e) `TryVerify` | a statement not signed by pinned identity keys of as many holders as the trust anchor and the statement require - also a correct statement of somebody else; an expired one |
| f) certificates, `VerifySignature` | a commit signed by fewer holders than the quorum, by a key no trusted identity key of its holder certifies, one not valid at `signedAt` or not for the `party`, or whose signature does not verify |

Step b) alone does not protect anything: `eTags` and the key statement may be left out, and whoever changes `QA`
leaves them out. Step d) is what binds the roaming network to the commit the holders signed; step e) is what refuses a
version without a key statement, or with one of only some of the pinned holders. Step g) shows the texts the
signatures cover, and warns where the QR code names the network otherwise.

`Helpers.ReadJSON` reads the files as POI reads JSON: with `DateParseHandling.None`, so that timestamps stay the text
they are signed as, and `FloatParseHandling.Decimal`, so that numbers stay the decimals they are. Read with
`JObject.Parse`, `signedAt` would become a date, and the signatures would not verify.

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

json          json:sha256:hex:ed33bb7b8757e8ac0fc44f2febf980528f0775c710aa2a3633ba4369ae406917
cbor          cbor:sha256:hex:e4052e5fc4bce9a4440e721a5cbb5e3a6fddd61ab9f84c61471b534c656a3711
effectiveTree effectiveTree:sha256:hex:844720b88821f5e2e3dcbba882475fe54005feb7ba61301834fd7061fbac5001
the commit's  JSON, CBOR, EffectiveTree: the same
```


## 9. Where example A2 starts

Checked, `QA` is a version a charging station operator may build on:

```
QA is checked: revision 0, commit be1e93ba91b381903808ed7f68e4f454f9459ed286171377c440e2331cb58704, signed by AlphaOrg and BetaOrg and GammaOrg
beforeETags of the next change set: json:sha256:hex:ed33bb7b8757e8ac0fc44f2febf980528f0775c710aa2a3633ba4369ae406917, cbor:sha256:hex:e4052e5fc4bce9a4440e721a5cbb5e3a6fddd61ab9f84c61471b534c656a3711
```

- **The head:** revision 0, commit `be1e93ba…`. The next commit names it in its `parentIds` and has revision 1.
- **The `beforeETags`** of the next change set are the `json` and `cbor` ETags of this version, in hex
  (`CalcChangeSetETags()`): a change set applies only to the version it names, and is refused on any other (section 4).
  The `effectiveTree` ETag is the commit's, not the change set's.
- **The signing keys of `OCC`** are trusted now, as long as their certificates and the key statement are: the next
  commits are checked as in step f). Adding a charging station operator is the RNO's, so its change set needs
  signatures of two holders as well - and of the identity keys of the new operator, see [example A2](exampleA2.md).

A server following `QA` starts its own history from the checked version with
`RoamingNetworkHistory.TryFromSnapshot(network, commit, …)`, which checks d) as well.
