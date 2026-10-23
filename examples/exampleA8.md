# Example A8: price statements, named by checkpoints

`DE*GEF` adds its charging tariffs, an AC and a DC one, and its EVSE templates name them; the roaming network operator
`OCC` makes checkpoint 3. Then `DE*GEF` states **ad hoc prices** of the DC EVSEs of Jena Paradies, which change by the
minute and are no content: **price statements**, signed by its key for its tariffs and prices, chained per EVSE. Then it
raises its AC tariff, a change of content, and `OCC` makes checkpoint 4, which names the price statements taken since
checkpoint 3. A driver's app or an e-mobility provider checks the statements against the keys of `DE*GEF` and the tree
of checkpoint 4, and keeps the proof of one: it was made between the two checkpoints.

| Revision | Signed by | |
|---|---|---|
| 0 to 9 | see [example A1](exampleA1.md) to [example A5](exampleA5.md) | checkpoints 0 to 2 of [example A4](exampleA4.md) and [example A5](exampleA5.md) |
| 10 | `DE*GEF`, its `tariffs` key | `Add` of the charging tariffs `DE*GEF*T_AC`, 0.49 €/kWh, and `DE*GEF*T_DC`, 0.69 €/kWh |
| 11 | `DE*GEF`, its `infrastructure` key | `UpdateProperty` of `chargingTariffId` of the EVSE templates `DE*GEF*ET_AC` and `DE*GEF*ET_DC` |
| | `OCC` | **checkpoint 3**, of revision 11 |
| no commit | `DE*GEF`, its `tariffs` key | **price statements:** `DE*GEF*E1*1` and `DE*GEF*E1*2` 0.59 €/kWh, then `DE*GEF*E1*1` 0.79 €/kWh |
| 12 | `DE*GEF`, its `tariffs` key | `UpdateProperty` of the `elements` of `DE*GEF*T_AC`: 0.52 €/kWh |
| | `OCC` | **checkpoint 4**, of revision 12, naming the 3 price statements |

Two kinds of prices (SPECIFICATION.md, section 12): **charging tariffs** are content, they change slowly, by commits, and
checkpoints prove them as all content. Ad hoc prices, like status messages in DATEX II, are the **price status** of
charging pools, stations and EVSEs: runtime data without versions, replacing the elements of charging tariffs of their
operator. Their evidence is a price statement, signed by the charging station operator when it sets the price, and a
checkpoint naming it.

The code is [exampleA8.cs](csharp/exampleA8.cs), with the helpers of [exampleA4.cs](csharp/exampleA4.cs), called by
[Program.cs](csharp/Program.cs) after example A7:

```csharp
ExampleA8.Run(a1, a2);

ExampleA8.Validate();
```

The output is what one run printed, `…` for elided digests.


## 1. The charging tariffs, and the templates naming them

A charging tariff is of its operator and signed by its key for its tariffs; an EVSE takes the tariff of the first of its
EVSE templates naming one (section 9.2), and the templates are infrastructure. So both keys of `DE*GEF` sign, one commit
each. `CommitWith` is the `Commit` of example A3 with the key given.

```csharp
ExampleA4.CommitWith(A1, tariffsKey, "The AC and the DC charging tariffs",
                     RoamingNetworkChange.Add(Tariff(AC, "AC charging", 0.49m)),
                     RoamingNetworkChange.Add(Tariff(DC, "DC charging", 0.69m)));

ExampleA4.Commit(A1, A2, "The EVSE templates name the tariffs",
                 RoamingNetworkChange.UpdateProperty(gef.EVSETemplates.Single(template => template.Id.ToString() == "DE*GEF*ET_AC"), "chargingTariffId", null, AC.ToString()),
                 RoamingNetworkChange.UpdateProperty(gef.EVSETemplates.Single(template => template.Id.ToString() == "DE*GEF*ET_DC"), "chargingTariffId", null, DC.ToString()));
```

```csharp
private static ChargingTariff Tariff(ChargingTariff_Id Id, String Name, Decimal Price)

    => new (Id,
            Currency.EUR,
            [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, Price, StepSize: 1) ]) ],
            Name: I18NString.Create(Languages.en, Name));
```

```
Committed, revision 10: The AC and the DC charging tariffs
Committed, revision 11: The EVSE templates name the tariffs
```


## 2. Checkpoint 3

```
Checkpoint 3 of revision 11: 2 commit(s), 3 signatures, c452cdc0…
```


## 3. Price statements

`DE*GEF` states the prices in its own program, with its key for its tariffs, which may sign `priceStatements` as well
([example A2](exampleA2.md)): a key stolen from the system setting the prices signs no charging pools. The server of
`QA` takes them, `TryAddPriceStatement`. Each statement names the latest checkpoint `DE*GEF` knew, `after`, so it was
made after it. The first one of an EVSE has the `sequence` 0, every next one names the digest of the one before,
`previous`, so statements are neither reordered nor withdrawn unnoticed.

```csharp
RoamingNetworkPriceStatement.Create(key.Private,
                                    POIPublicKey.From(key.Private).Id,
                                    key.Algorithm,
                                    A1.RoamingNetwork.Id,
                                    nameof(EVSE),
                                    EVSE,
                                    Prices(DC, Price),
                                    Previous is null ? 0 : Previous.Sequence + 1,
                                    Previous?.Digest,
                                    SignedAt:    now.AddMilliseconds(Millisecond),
                                    ValidFrom:   now.AddMilliseconds(Millisecond),
                                    ValidUntil:  now.AddHours(1),
                                    After:       checkpoint3.Id);
```

a) A happy hour, 0.59 instead of 0.69 per kWh at both DC EVSEs; b) then a higher price at the first one, naming the
statement before; c) what is not taken: the first statement again, out of date; another statement of the sequence 1, a
split view; and one signed by the key of `DE*GEF` for its infrastructure, which may not sign prices.

```
DE*GEF*E1*1, 0.59 €/kWh, sequence 0: Taken
DE*GEF*E1*2, 0.59 €/kWh, sequence 0: Taken
DE*GEF*E1*1, 0.79 €/kWh, sequence 1: Taken
DE*GEF*E1*1, sequence 0 again: Outdated, The EVSE 'DE*GEF*E1*1' has the price statement 1 already, a later one than 0!
DE*GEF*E1*1, another statement of the sequence 1: Refused, The EVSE 'DE*GEF*E1*1' has another price statement 1: two statements of one sequence are a split view!
DE*GEF*E1*2, signed by the infrastructure key: Refused, The signing key '…' of the charging station operator 'DE*GEF' may not sign 'priceStatements', only 'infrastructure'!
```

The first statement, as `published/QA-checkpoint-4-priceStatements.json` has it:

```json
{
  "@context": "https://open.charging.cloud/contexts/wwcp/roamingNetwork/priceStatement/v1",
  "roamingNetworkId": "QA",
  "entityType": "EVSE",
  "entityId": "DE*GEF*E1*1",
  "priceStatus": { "prices": [ { "chargingTariffId": "DE*GEF*T_DC", "elements": [ { "priceComponents": [ { "type": "ENERGY", "price": 0.59, "stepSize": 1 } ] } ] } ] },
  "validFrom": "…",
  "validUntil": "…",
  "sequence": 0,
  "after": "c452cdc0…",
  "role": "chargingStationOperator",
  "signer": "DE*GEF",
  "signedAt": "…",
  "algorithm": "ES256",
  "keyId": "7d3baad4…",
  "value": "…"
}
```

What a driver's app gets of an EVSE, `published/QA-priceStatements-DE_GEF_E1_1.json`, as
`GET ~/RNs/QA/EVSEs/DE*GEF*E1*1/priceStatements`: the current statements of its pool, its station and the EVSE.


## 4. A change of a tariff, and checkpoint 4

The AC tariff becomes 0.52 per kWh: a change of content, by a commit. Checkpoint 4 names the price statements its
server took since checkpoint 3 and that were signed before it, by the Merkle root of their tree, `priceStatements`
(section 12.3); a checkpoint naming none has no `priceStatements`, as the checkpoints 0 to 3.

```
Committed, revision 12: The AC charging tariff: 0.52 €/kWh
Checkpoint 4 of revision 12: 1 commit(s), 3 signatures, 88412483…
  naming 3 price statements, root be3b574a…
```

```json
"commits":         { "@merkleRoot": "fa62dd4d…", "@size": 1 },
"priceStatements": { "@merkleRoot": "be3b574a…", "@size": 3 },
```

Published as well: the statements it names, `published/QA-checkpoint-4-priceStatements.json`, as
`GET ~/RNs/QA/history/checkpoints/{checkpointId}/priceStatements`, and the proof of the happy hour at `DE*GEF*E1*1`,
`published/QA-priceStatementProof-E1_1-0.json`, as `…/priceStatements/{digest}/proof`.


## 5. The checkpoints and the tariffs

`ExampleA8.Validate()` checks from what is published only. a) `QA` at its head and the checkpoints 3 and 4, signed by
the quorum of `OCC`. b) The tariffs of the DC EVSEs of Jena Paradies as they are at runtime, from their EVSE template,
and the AC tariff after its change:

```
DE*GEF*E1*1: DE*GEF*T_DC 0.69 €/kWh
DE*GEF*E1*2: DE*GEF*T_DC 0.69 €/kWh
DE*GEF*T_AC: 0.52 €/kWh
```


## 6. The price statements of an EVSE, as an app gets them

Each one signed by a signing key of `DE*GEF`, the operator of its EVSE, valid when it signed and one that may sign
prices, `TryVerify`; valid now; and made after checkpoint 3, which it names. Its prices replace the elements of the
tariff it names: at `DE*GEF*E1*1`, `DE*GEF*T_DC` costs 0.79 €/kWh now, not 0.69.

```
EVSE DE*GEF*E1*1, sequence 1: DE*GEF*T_DC 0.79 €/kWh, signed by DE*GEF, valid now: True, after checkpoint 3
```

An app checks such a statement offline also against the proof of the charging station (section 8.4): its entity below
its operator, the signing key of the operator in the proof, and that it is valid when charging begins (section 12.4).


## 7. The price statements of checkpoint 4

Each one signed by a signing key of `DE*GEF` before the checkpoint and after checkpoint 3, each one following the one
before of its EVSE, `Follows`, and their tree, in the order of section 12.3, is the one the checkpoint signs:

```csharp
var root = RoamingNetworkPriceStatement.RootOf(RoamingNetworkPriceStatement.InOrder(named), checkpoint4.IdentificationHash);
```

```
Checkpoint 4 names 3 price statements; the root of those published: the same
```


## 8. Evidence of one statement

The proof of the happy hour at the first EVSE: the statement with its index and audit path to the `priceStatements`
root of checkpoint 4. Kept with the statement, it shows that the statement was made after checkpoint 3, which it names,
and before checkpoint 4, which names it; with witnesses, as in [example A6](exampleA6.md), without asking `DE*GEF` or
the server of `QA`.

```
The proof of DE*GEF*E1*1, sequence 0, in checkpoint 4: valid
```


## 9. Joining from checkpoint 4, with its price statements, and what is refused

a) A server joining from checkpoint 4 with the statements it names, `TryFromCheckpointSnapshot(…, PriceStatements)`:
their tree must be the one of the checkpoint. b) Without one of them, the tree is another one. c) A statement of
another price: its signature is not the one of its content.

```
Joined from checkpoint 4 with its price statements: revision 12, 3 statements
Joined with a statement less: The checkpoint of the snapshot can not be taken: The price statements are not those the checkpoint '88412483…' names!
A statement of another price: The signature of the price statement is invalid: The signature is invalid!
```


## With the hub

POI_API serves and takes price statements as section 12.5 says: `GET ~/RNs/QA/EVSEs/{EVSE_Id}/priceStatements`,
`GET`/`POST ~/RNs/QA/history/priceStatements`, the statements of a checkpoint and their proofs; over the WebSocket
`priceStatements`, `addPriceStatements`, `checkpointPriceStatements` and `priceStatementProof`. A statement needs no
authorization: it is signed by the operator of its entity. A server holding a signing key of an operator, its
`PriceStatementSigners`, states every change of the price status of the entities of that operator itself. As the hub
makes no checkpoints yet ([example A4](exampleA4.md#with-the-hub)), it names no statements by a checkpoint of its own.


## Next

- Witnesses check the price statements a checkpoint names before they sign it (sections 10.2, 12.3): given them with the
  checkpoint, `TryAddCheckpoint(…, PriceStatements)`.
- Statements of a charging pool or a station: `entityType` `ChargingPool` or `ChargingStation`; an app gets those of
  the pool, the station and the EVSE together.
- Retention of price statements, and whether the archive takes them, is open (section 12.6).
