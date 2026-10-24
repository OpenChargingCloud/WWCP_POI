# Example A10: what a driver's app keeps as evidence

A driver charges at `DE*GEF*E1*1`, a DC EVSE of Jena Paradies. Months later the invoice is disputed: which price
applied when charging began? The driver's app answers it from one file it kept, without asking `DE*GEF` or the server of
`QA`: the proof of the charging station, the price statement of the EVSE, and the checkpoints the witnesses of
[example A6](exampleA6.md) signed, with the proofs of both in them (SPECIFICATION.md, sections 8.4, 8.5, 10.4, 12.3 and
12.4).

| Revision | Signed by | |
|---|---|---|
| 0 to 12 | see [example A1](exampleA1.md) to [example A9](exampleA9.md) | the version the app gets the station proof of, in checkpoint 4 |
| price statement, sequence 2 | `DE*GEF`, its `tariffs` key | 0.65 €/kWh at `DE*GEF*E1*1`, after checkpoint 4 |
| 13 | `DE*GEF`, its `tariffs` key | the DC charging tariff raised to 0.72 €/kWh, after the session |
| checkpoint 5 | `OCC`, and both witnesses, after they checked its price statement | of revision 13, naming the statement |

The code is [exampleA10.cs](csharp/exampleA10.cs), with the helpers of [exampleA4.cs](csharp/exampleA4.cs),
[exampleA6.cs](csharp/exampleA6.cs), [exampleA8.cs](csharp/exampleA8.cs) and [exampleA9.cs](csharp/exampleA9.cs), called
by [Program.cs](csharp/Program.cs) with what examples A1, A2 and A6 left:

```csharp
ExampleA10.Run(a1, a2, a6);

ExampleA10.Validate();
```

The app keeps what it gets in `driver/evidence-DE_GEF_E1_1.json`. The file is this example's own: the specification names
its parts, not how an app keeps them. The output is what one run printed.


## 1. The server of QA attests its head, and DE*GEF states a new price

The roaming network operator attests the head at its server with the signing key of each of its holders,
`AttestationSigners`, and every few minutes again, `RefreshAttestations()`. An attestation is valid for 15 minutes: a
proof made with it is fresh. The happy hour of [example A8](exampleA8.md) is over: `DE*GEF` states 0.65 €/kWh at
`DE*GEF*E1*1`, the next statement of its chain, made after checkpoint 4, valid for an hour.

```csharp
A1.History.AttestationSigners = [.. A1.Signers];

if (!A1.History.TryAttest(out var attestations, out var error))
    Helpers.Fail(error);
```

```
Revision 12 attested as OCC by AlphaOrg, BetaOrg, GammaOrg, until 2026-10-24T03:43:04.937Z
DE*GEF*E1*1, 0.65 €/kWh, sequence 2: Taken
```


## 2. Before charging: what the app gets, checks and keeps

a) As `GET ~/RNs/QA/EVSEs/DE*GEF*E1*1/proof`, the proof of its charging station with the attestations of the head, and as
`GET ~/RNs/QA/EVSEs/DE*GEF*E1*1/priceStatements`, the current statements of its pool, its station and the EVSE.

```csharp
if (!A1.History.TryGetChargingStationProof(EVSEId, out var proof, out error))
    Helpers.Fail(error);

var statements = A1.History.PriceStatementsOf(EVSEId);
```

The proof gives completely: the roaming network with `OCC`, its key statement and the certificates of its signing keys;
the skeleton of `DE*GEF` with its keys; the pool, the station and its EVSEs as they are at runtime, the properties of
their templates taken in, each EVSE with its energy meter and the public key it signs its measurements with; the
manufacturer of the station and of the meters of the station, `GraphDefined` and `KERNBERG`; and the charging tariffs the
EVSEs take. Every other child array is pruned to its Merkle root, also `SAALE`, which no meter of the station names.

b) The app checks them offline as charging begins, against the trust anchor it scanned: the proof attested by the
signing keys of two holders of `OCC` at least, each certified by a trusted identity key of its holder; each statement
signed by a signing key of `DE*GEF` the proof gives, one that may sign price statements, of an entity of `DE*GEF` the
proof gives, and valid now.

```csharp
foreach (var given in statements)
    if (!given.TryVerify(proof, anchor.Keys, out _, out error, started, anchor.MinConsensus))
        Helpers.Fail(error);
```

It shows the names of the verified proof, not those of the trust anchor (section 8.6), the station and the energy meter
of the EVSE with their manufacturers, and the price: the charging tariff the EVSE takes, from its EVSE template, and the
price the statement sets for it.

```
The app, at DE*GEF*E1*1: Quality Assurance of Open Charging Cloud, revision 12
DE*GEF*S1: GraphDefined Charging Stations GD-DC300
DE*GEF*M1*1, the energy meter of DE*GEF*E1*1: Kernberg DC Metering KDM-500, serial DCM-2026-001-1, OCMF, secp256r1 key 0cc14973…
DE*GEF*E1*1, 150 kW: DE*GEF*T_DC 0.69 €/kWh, by the price statement 0.65 €/kWh
Kept: driver/evidence-DE_GEF_E1_1.json, 31 KiB
```

c) It keeps the trust anchor, the EVSE, when charging began, the proof and the statements. When the session ends, the
energy is the one of the meter values `DE*GEF*M1*1` signed, in OCMF: a transparency software checks them with the key
the proof gives, not part of this example.


## 3. A week later: a commit, checkpoint 5, its witnesses

`DE*GEF` raises its DC charging tariff to 0.72 €/kWh, revision 13: the network changes, the evidence of the session does
not. Checkpoint 5 of revision 13 names the price statement of the session, taken since checkpoint 4 and signed before
checkpoint 5. The roaming network operator publishes the commits since checkpoint 4 and the statements of checkpoint 5;
the witnesses check them and sign checkpoint 5, as in [example A9](exampleA9.md).

```
Committed, revision 13: The DC charging tariff: 0.72 €/kWh
Checkpoint 5 of revision 13: 1 commit(s), 3 signatures, dfc1497a…
  naming 1 price statement(s)
Checkpoint 5, dfc1497a…: 2 signatures more, 5 in all
```


## 4. The app completes its evidence

Once checkpoint 5 is witnessed, the app gets three things.

- The proof of the commit of the station proof, revision 12, in the first checkpoint whose commits include it,
  checkpoint 4: `GET ~/RNs/QA/history/commits/{commitId}/proof`.
- The proof of the statement in checkpoint 5: `GET ~/RNs/QA/history/checkpoints/{checkpointId}/priceStatements/{digest}/proof`.
- Both checkpoints, with the signatures of the witnesses.

```csharp
if (!A1.History.TryGetCommitProof(proof.CommitId!.Value, out var commitProof, out error))
    Helpers.Fail(error);

if (!A1.History.TryGetPriceStatementProof(checkpoint5.Id, statement.Digest, out var statementProof, out error))
    Helpers.Fail(error);
```

```
Completed: driver/evidence-DE_GEF_E1_1.json, 40 KiB: the proof of revision 12 in checkpoint 4, of the statement in checkpoint 5, and both checkpoints
```

What the file holds, as JSON, in bytes:

| Part | Size | |
|---|---|---|
| `trustAnchor` | 0.5 K | the QR code of `QA` |
| `EVSEId`, `chargingStarted` | | the session |
| `chargingStationProof` | 21.6 K | with the energy meters of the station and their manufacturer, and its 3 attestations, one of each holder of `OCC` |
| `priceStatements` | 0.9 K | the statement of the EVSE |
| `commitProof` | 0.6 K | revision 12 in checkpoint 4 |
| `priceStatementProofs` | 1.0 K | the statement in checkpoint 5 |
| `checkpoints` | 5.0 K | 4 and 5, each with 3 signatures of `OCC` and 2 of witnesses |


## 5. The evidence, months later

`ExampleA10.Validate()` checks the evidence offline, from the file the app kept and the witnesses it trusts by
configuration only.

a) The checkpoints 4 and 5: signed as the roaming network operator by signing keys of as many holders as the quorum
requires, the certificates as the station proof gives them, and by both witnesses.

b) As charging began: the proof attested then, and the statement valid then. Months later the attestations prove nothing
on their own: they ended 15 minutes after they were signed.

c) The version of the station proof is one of the history, which the witnesses checked. Its commit is one of the
witnessed checkpoint 4, and the proof leads to the `effectiveTree` ETag of the commit:

```csharp
if (!commitProof.Verify(commitCheckpoint, out error))
    Helpers.Fail(error);

if (!proof.Verify(commitProof.ETags.Single(eTag => eTag.Format == ETagFormat.EffectiveTree && eTag.HashAlgorithm == proof.HashAlgorithm), out error))
    Helpers.Fail(error);
```

d) The statement was made after checkpoint 4, which it names, and before checkpoint 5, which names it and follows
checkpoint 4. The witnesses checked it with checkpoint 5: no other statement of its sequence was shown to them, and it
was not made afterwards.

e) What applied: the station and the energy meter of the EVSE, with the key the signed meter values of the session are
checked with, the tariff of the EVSE as the proof gives it, and the price the statement set for it.

```
Checkpoint 4: revision 12, signed by AlphaOrg, BetaOrg, GammaOrg, witnessed by calibration.example, association.example
Checkpoint 5: revision 13, signed by AlphaOrg, BetaOrg, GammaOrg, witnessed by calibration.example, association.example

At 2026-10-24T04:06:58.083Z, when charging began: the station proof attested, the price statement valid
2027-01-22T04:06:58.083Z, by its attestations alone: Valid attestations of trusted keys of 0 holders attest the root effectiveTree:sha256:hex:f6ab8661… of the proof, not of 2!
Revision 12, the version of the station proof: in checkpoint 4, witnessed
The price statement, sequence 2: after checkpoint 4, before checkpoint 5, witnessed

DE*GEF*S1: GraphDefined Charging Stations GD-DC300
DE*GEF*M1*1, the energy meter of DE*GEF*E1*1: Kernberg DC Metering KDM-500, serial DCM-2026-001-1, OCMF, secp256r1 key 0cc14973…
DE*GEF*E1*1, 150 kW: DE*GEF*T_DC 0.69 €/kWh, by the price statement 0.65 €/kWh
```

The DC tariff of `QA` is 0.72 €/kWh by now; the evidence shows the 0.65 €/kWh that applied.


## 6. What is refused

a) A statement of another price: its signature is not the one of its content. b) A session claimed to have begun an hour
earlier, before the statement was valid. c) A station proof of another power: it leads to another root than the commit
of checkpoint 4. d) A station proof naming the key of another energy meter, e.g. to pass off meter values signed
elsewhere: another root as well. e) Checkpoint 5 as it was before its witnesses signed it.

```
A statement of another price: The signature of the price statement is invalid: The signature is invalid!
Charging begun an hour earlier: The price statement is not valid at 2026-10-24T03:06:58.083Z: it is valid from 2026-10-24T04:06:58.034Z until 2026-10-24T05:06:58.034Z!
A station proof of 300 kW: The proof leads to effectiveTree:sha256:hex:3e4b7de5…, not to effectiveTree:sha256:hex:f6ab8661…!
A station proof naming another meter key: The proof leads to effectiveTree:sha256:hex:8ecc6ce4…, not to effectiveTree:sha256:hex:f6ab8661…!
Checkpoint 5 without its witnesses: witnessed by 0 of 2
```


## Limits

- The app checks the signatures of `OCC` on checkpoint 5 with the certificates of the version of revision 12, which the
  station proof gives. Had a holder of `OCC` renewed its signing key in between, the app would need the certificate of
  the version of checkpoint 5 as well, e.g. by a proof of `OCC` in that version.
- The app trusts the witnesses by configuration, as the verifier of [example A6](exampleA6.md). A signed list of them
  is open, as the trust lists of operators and their identity keys section 8.8 leaves open.
- The energy charged is the one of the signed meter values of the session, checked with the public key of the energy
  meter the station proof gives; the meter values, and the transparency software checking them, are not part of this
  example. `QA` names no transparency software for its meters yet (`transparencySoftwareStatus`).


## Next

- When cryptography ages: `QA` moves into an epoch of SHA-512 identifications and ML-DSA signatures, and this evidence,
  of epoch 1, proves what it proved: [example A11](exampleA11.md).
