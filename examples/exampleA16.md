# Example A16: EV Taler – a session paid in two steps, and its evidence

A driver charges at `DE*GEF*E1*1` and pays with GNU Taler, step by step ([concepts/EVTaler.md](../concepts/EVTaler.md),
sections 5 and 7): step 1 the first 5 kWh and a reserve, step 2 the next 5 kWh, each an order of the merchant of
`DE*GEF` that [example A15](exampleA15.md) named. Before the driver pays a step, the app checks its contract terms
against the station proof of example A15 and the price statement of the EVSE. The meter of the EVSE signs its energy
register at the start and at the end of the session; step 2 names the start value, and `DE*GEF` settles the session
only with both, signed by its key for `payments`. What the steps paid beyond the price is refunded. The app keeps all
of it in one file, which alone decides a dispute.

| | Signed by | |
|---|---|---|
| contract terms of step 1 and step 2 | the merchant instance of `DE*GEF`, at its merchant backend | not here: the wallet checks that signature |
| the start value and the end value | `DE*GEF*M1*1`, the energy meter of `DE*GEF*E1*1`, in OCMF | with the key the station proof gives |
| the settlement statement | `DE*GEF`, its `payments` key of example A15 | no content: no commit, no checkpoint |

The code is [exampleA16.cs](csharp/exampleA16.cs), called by [Program.cs](csharp/Program.cs) with what example A11 left:

```csharp
ExampleA16.Run(a11);

ExampleA16.Validate();
```

The example plays the meter: [example A3](csharp/exampleA3.cs) keeps its private key in
`DE-GEF/meter-DE_GEF_M1_1-privateKey.pem` for this example alone; a meter never gives it out. GNU Taler is not called:
the contract terms are as a merchant backend makes them, and nothing is paid. The app keeps what it gets in
`driver/evidence-DE_GEF_E1_1-evTaler.json`, a file of this example's own: the concept names its parts, not how an app
keeps them. The output is what one run printed.


## 1. What the app holds

The trust anchor of epoch 2, the station proof of example A15, and the price statement of `DE*GEF*E1*1` of example
A11, 0.59 €/kWh, signed with ML-DSA. The EV Taler tariff of the EVSE is `DE*GEF*T_DC`, and the statement prices it.

```csharp
VerifiedChargingStation.TryVerify(anchor, proof, out var station, out error, EVSEId: evseId, At: now)

station.TryGetCurrentPrices([ statement ], out var current, out error, now)

EVTalerContractTerms.TryGetEVTalerTariff(station, evseId, out var tariff, out error)
```


## 2. Step 1: the first 5 kWh and a reserve

`DE*GEF`'s backend makes the order: what the step covers at the price of the session, rounded up to the cent, and a
reserve of 1.00 €, refunded at the end as far as it is not used. Its contract terms carry the step in
`"extra": { "wwcp": … }`. Before the driver pays, the app checks them: the merchant base URL and key, and the exchange,
are those of `evTaler` in euros; the step names an EVSE of the station, its EV Taler tariff and the price statement
current when the session begins; the amount is the step at that price, plus the flat fee, and the rest the reserve.

```csharp
var step1  = new EVTalerStep(station.RoamingNetworkId, evseId, SessionId, 1, null, tariffId, statement.Digest, new EVTalerCovers(0, 5, EVTalerCovers.KWh), null);

terms1.TryVerify(station, [ statement ], null, null, out var verified1, out error)
```


## 3. Charging starts: the meter signs the start value

The meter signs the reading of its energy register at the start of the transaction (`TX` `B`) in OCMF, with ECDSA over
secp256r1; the station sends it with OCPP 2.1 in its `TransactionEvent` `Started`, as `signedMeterValue`, with the
public key of the meter (section 5.6 of the concept).


## 4. Step 2: the next 5 kWh, naming the start value

Every step from step 2 on names the order before it and the start value, so that the merchant's signature over its
contract terms binds `DE*GEF` to it. The app checks the start value with a transparency software, which knows OCMF:
WWCP_POI chooses the key, the one of the meter of the EVSE the station proof gives, and checks that the value names its
serial number. The transparency software of this example checks OCMF of one reading, as far as the example needs it;
Chargy checks OCMF and the other formats of meters.

```csharp
var step2  = new EVTalerStep(station.RoamingNetworkId, evseId, SessionId, 2, terms1.OrderId, tariffId, statement.Digest, new EVTalerCovers(5, 10, EVTalerCovers.KWh), start);

terms2.TryVerify(station, [ statement ], verified1, VerifyOCMF, out var verified2, out error)
```

```json
"extra": {
  "wwcp": {
    "@context":         "https://open.charging.cloud/contexts/wwcp/roamingNetwork/evTalerStep/v1",
    "roamingNetworkId": "QA",
    "EVSEId":           "DE*GEF*E1*1",
    "sessionId":        "kq3Vd0sY0f9hJ2bW8Qm1tA",
    "step":             2,
    "previousOrderId":  "2026.309-01GEF1",
    "chargingTariffId": "DE*GEF*T_DC",
    "priceStatement":   "281a351dfef83317049c24e6c2a9235ca8a83428aca58a4dbe38f35c4dc9ab22",
    "covers":           { "from": "5", "to": "10", "unit": "kWh" },
    "startMeterValue":  { "signedMeterData": "T0NNRnx7IkZWIjoiMS4wIiwi…", "signingMethod": "ECDSA-secp256r1-SHA256",
                          "encodingMethod":  "OCMF", "publicKey": "MFkwEwYHKoZIzj0CAQYIKoZI…" }
  }
}
```


## 5. Charging ends, and DE*GEF settles the session

The meter signs the end value (`TX` `E`). `DE*GEF` calculates the price from both values by the price statement, 8.214 kWh
at 0.59 €/kWh, rounded to the nearest cent, and signs the settlement statement with its key for `payments`: both signed
meter values, the energy, the orders of both steps, the price, what was paid and what was refunded. Its merchant backend
refunds the rest, from the last step back, and the wallet picks it up by itself.

```csharp
var settlement = EVTalerSettlement.Create(key, POIPublicKey.From(key).Id.ToString(), COSEAlgorithm.MLDsa65, ExampleA2.gefId,
                                          station.RoamingNetworkId, evseId, SessionId, ExampleA15.MerchantKey, tariffId,
                                          statement.Digest, startAt, endAt, start, end, energy, "EUR",
                                          [ (terms1.OrderId, terms1.Amount), (terms2.OrderId, terms2.Amount) ], price,
                                          SignedAt: endAt.AddMinutes(1));
```

```json
{ "@context": "https://open.charging.cloud/contexts/wwcp/roamingNetwork/evTalerSettlement/v1",
  "roamingNetworkId": "QA", "EVSEId": "DE*GEF*E1*1", "sessionId": "kq3Vd0sY0f9hJ2bW8Qm1tA",
  "merchantPublicKey": "ESMBKNR5CMZPAYH9AD0QNKCPYSJSE0EN5MH4FMPKN683R63HMD3G",
  "chargingTariffId": "DE*GEF*T_DC", "priceStatement": "281a351dfef83317049c24e6c2a9235ca8a83428aca58a4dbe38f35c4dc9ab22",
  "start": "2026-11-05T20:22:12.000Z", "end": "2026-11-05T20:56:12.000Z",
  "meterValues": [ { "signedMeterData": "T0NNRnx7IkZWIjoiMS4wIiwi…", "encodingMethod": "OCMF", … }, { … } ],
  "energy": "8.214", "currency": "EUR",
  "orders": [ { "orderId": "2026.309-01GEF1", "amount": "3.95" }, { "orderId": "2026.309-02GEF1", "amount": "2.95" } ],
  "price": "4.85", "paid": "6.90", "refunded": "2.05",
  "role": "chargingStationOperator", "signer": "DE*GEF", "signedAt": "2026-11-05T20:57:12.000Z",
  "algorithm": "ML-DSA-65", "keyId": "d7df9b888de8f9538ba113eb5139a5cc27efd883f9d0d3b23cd8ac709bc9ed91", "value": "3ksqmE8k…" }
```


## 6. The app keeps it

The station proof, the price statement, the contract terms of both steps and the settlement statement, in one file.


## 7. From the file alone

Months later the session is disputed. The file alone answers it, against the trust anchor of epoch 2: the station proof
as it was when the session began, every step one after the other, and the settlement statement against them, both
signed meter values checked again. A price changed after signing breaks the signature; without the end value there is
no settlement; an order of another merchant is refused; and a step 2 naming another start value, even one the meter
signed, is caught by the settlement, which names the start value the meter gave at the start.

```
The app, at DE*GEF*E1*1: the EV Taler tariff DE*GEF*T_DC, priced by the price statement 281a351d…, sequence 3: current True
Step 1, order 2026.309-01GEF1: 0 to 5 kWh at 0.59 €/kWh 2.95 €, flat fee 0.00 €, reserve 1.00 €, 3.95 €: exact
The start value of DE*GEF*M1*1, OCMF, as the station sends it with OCPP 2.1: OCMF|{"FV":"1.0","GI":"GraphDefined GD-DC300","GS":"DE*GEF*S…
Step 2, order 2026.309-02GEF1: 5 to 10 kWh, 2.95 €: exact; the start value 1234.567 kWh of DE*GEF*M1*1, serial DCM-2026-001-1
Settled by DE*GEF, ML-DSA-65: 8.214 kWh, 4.85 €; paid 6.90 €, refunded 2.05 €
Kept: driver/evidence-DE_GEF_E1_1-evTaler.json, 165 KiB

Was this the operator's price when the session began? DE*GEF*T_DC, 0.59 €/kWh by the price statement 281a351d…, signed by DE*GEF, current at 2026-11-05 20:21:12Z
Did I pay the operator? 2 orders, each of the merchant ESMBKNR5… the station proof gives, with exchanges of its evTaler
Did I get what I paid for? 1234.567 kWh to 1242.781 kWh, 8.214 kWh, both signed by DE*GEF*M1*1, serial DCM-2026-001-1, key 9daac24d…
Was the rest refunded? 4.85 € of 6.90 €, exact by the tariff; 2.05 € refunded, signed by DE*GEF with its key for payments

Another price: The signature of the settlement statement is invalid: The signature is invalid!
Without the end value: A settlement statement names both signed meter values, at the start and at the end!
Step 1 of another merchant: The step is offered by the merchant 'https://taler.gef.example/instances/de-gef/' (YJ7H5JVNVM5SG5FAE3BGQ35AEPHXBH27MWNRPFQ0SVW9BH0BN9B0), not by the operator's 'https://taler.gef.example/instances/de-gef/' (ESMBKNR5CMZPAYH9AD0QNKCPYSJSE0EN5MH4FMPKN683R63HMD3G)!
Step 2 naming the end value as the start: The signed meter value at the start is not the one the contract terms of the steps name!
```


## Limits

- Nothing is paid: there is no merchant backend, no exchange and no wallet. The contract terms are as a merchant
  backend makes them, its signature over them is the wallet's to check, with GNU Taler's hashing of contract terms;
  WWCP_POI checks that the merchant is the operator's. `h_wire` and `nonce` are made for the example.
- Asking for offers, the events and the start, limit and stop at the station (concepts/EVTaler.md, sections 5.6 and 6)
  are phase 3, not built yet: here the example plays the operator's backend, the station and its meter.
- The OCMF is the example's own and checked as far as it needs: one reading, ECDSA-secp256r1-SHA256. The meter of
  `DE*GEF*E1*1` signs in OCMF as example A3 says; the times are those of the run, the readings made for the example.
- The station proof is checked as it was when the session began; months later, its attestations are checked by
  checkpoints the witnesses signed, as in example A10.
