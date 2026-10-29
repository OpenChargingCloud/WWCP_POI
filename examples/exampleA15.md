# Example A15: EV Taler – the merchant of a charging station operator, and what a driver's app shows

`DE*GEF` accepts [GNU Taler](https://taler.net) at Jena Paradies, to be paid step by step, e.g. per kWh
([concepts/EVTaler.md](../concepts/EVTaler.md)). In GNU Taler, coins are those of an **exchange**, and they pay only
where the merchant accepts that exchange. So `DE*GEF` names its **merchant** at a GNU Taler merchant backend, `evTaler`,
and the exchanges whose coins it accepts, signed by a key for `payments` alone: whoever changes the merchant redirects
the payments of every station. The pool names the payment option `EVTaler`. The station proof the driver's app gets
gives the merchant: the app shows "EV Taler (provider)" for the exchanges in the currency of the EVSE's tariff, and
knows the merchant every offer must come from. Nothing here pays: offers, payments and refunds are GNU Taler's
([ChargingStationOperator](../docs/ChargingStationOperator.md#ev-taler-merchant), [ChargingPool](../docs/ChargingPool.md#ev-taler)).

| Revision of epoch 2 | Signed by | |
|---|---|---|
| 0 to 8 | see [example A11](exampleA11.md) to [example A14](exampleA14.md) | |
| 9 | `DE*GEF`, its `party` key | a new signing key of `DE*GEF` that may sign `payments` |
| 10 | `DE*GEF`, its `payments` key | its merchant at `https://taler.gef.example/instances/de-gef/`, accepting an exchange in euros and one in Swiss francs |
| 11 | `DE*GEF`, its `infrastructure` key | Jena Paradies, `DE*GEF*P1`: the payment option `EVTaler` |

The code is [exampleA15.cs](csharp/exampleA15.cs), called by [Program.cs](csharp/Program.cs) with what example A11 left:

```csharp
ExampleA15.Run(a11);

ExampleA15.Validate();
```

The new private key of `DE*GEF` is kept in `DE-GEF/signing-payments-mldsa-privateKey.pem`. The output is what one run
printed.


## 1. DE*GEF adds a signing key for payments

An ML-DSA key, as every key of epoch 2, that may sign `payments` and nothing else; `DE*GEF` can keep it apart from the
keys its systems sign with every day, e.g. offline. Its signing keys are rotated by its key for the party, in the
ordinal order of their identifications.

```csharp
var paymentsKey = COSEKeyPair.NewMLDsa65();

var addKey      = history.Head.RoamingNetwork.CreateChangeSet(
                      [ RoamingNetworkChange.UpdateProperty(gef, POIPartyKeys.SigningKeysProperty, null,
                                                            new JArray(gef.SigningKeys.Append(POIPublicKey.From(paymentsKey.Private, NotAfter: now.AddYears(1), Usages: [ POIKeyUsages.Payments ])).
                                                                                       OrderBy(key => key.Id.ToString(), StringComparer.Ordinal).
                                                                                       Select (key => key.ToJSON(Embedded: true)))) ],
                      Description: ImmutableI18NString.Create(Languages.en, "DE*GEF: a signing key for payments")
                  );
```


## 2. DE*GEF names its merchant and the exchanges it accepts

Its instance at a hosted merchant backend, by its base URL and its public key as GNU Taler writes it, and two exchanges,
each by its master public key, base URL, currency and name. The key of the exchange in euros is the master public key of
`exchange.demo.taler.net`; the others are made for the example. Signed by its key for the party, the change is refused.

```csharp
var merchant = new EVTalerMerchant(URL.Parse("https://taler.gef.example/instances/de-gef/"),
                                   MerchantKey,
                                   [ new EVTalerExchange(EURExchangeKey, URL.Parse("https://exchange.example.eu/"), Currency.EUR, ImmutableI18NString.Create(Languages.en, "Example Exchange")),
                                     new EVTalerExchange(CHFExchangeKey, URL.Parse("https://exchange.example.ch/"), Currency.CHF, ImmutableI18NString.Create(Languages.en, "Example Franc Exchange")) ]);

RoamingNetworkChange.UpdateProperty(gef, ChargingStationOperator.PropertyKeys.EVTaler, null, merchant.ToJSON(Embedded: true))
```

```json
"evTaler": {
  "merchantBaseURL":   "https://taler.gef.example/instances/de-gef/",
  "merchantPublicKey": "ESMBKNR5CMZPAYH9AD0QNKCPYSJSE0EN5MH4FMPKN683R63HMD3G",
  "exchanges": [
    { "masterPublicKey": "F80MFRG8HVH6R9CQ47KRFQSJP3T6DBJ4K1D9B703RJY3Z39TBMJ0", "baseURL": "https://exchange.example.eu/",
      "currency": "EUR", "name": { "en": "Example Exchange" } },
    { "masterPublicKey": "YJ7H5JVNVM5SG5FAE3BGQ35AEPHXBH27MWNRPFQ0SVW9BH0BN9B0", "baseURL": "https://exchange.example.ch/",
      "currency": "CHF", "name": { "en": "Example Franc Exchange" } }
  ]
}
```


## 3. Jena Paradies names the payment option EVTaler

`paymentOptions` is an open set of names; `EVTaler` is one of them, and a pool or pool template of an operator without
a merchant naming it is refused. The EV Taler tariff of `DE*GEF*E1*1` is `DE*GEF*T_DC` of [example A8](exampleA8.md): it
has no type and names no payment means. The server shows the exchanges in its currency, euros, with
`EVTalerExchangesAt`.

```csharp
RoamingNetworkChange.UpdateProperty(jenaParadies, ChargingPool.PropertyKeys.PaymentOptions, null, new JArray(PaymentOptions.EVTaler.ToString()))

foreach (var exchange in Operator(history).EVTalerExchangesAt(ExampleA10.EVSEId))
    Console.WriteLine($"At {ExampleA10.EVSEId}, as the server sees it: {exchange.PaymentOptionName.FirstText()}");
```


## 4. The station proof the app gets

The holders of `OCC` attest the head; the proof of the charging station of `DE*GEF*E1*1` gives `DE*GEF` with its own
properties, the merchant among them, the pool and the tariffs of the EVSE.


## 5. What the app shows, and the merchant of every offer

The app checks the proof against the trust anchor of epoch 2, reads the merchant from it, finds the EV Taler tariff of
the EVSE, its ad hoc tariff or else the one without a type, and shows the exchanges in its currency. It knows what every
offer for the EVSE must name: the merchant base URL in its `taler://pay` URI, and `merchant_pub` in its contract terms.
A merchant key changed on the way makes the proof one of no version the attestations name.

```
Committed, revision 9 of epoch 2: DE*GEF: a signing key for payments
Signed by the key of DE*GEF for the party: Change 0: UpdateProperty of the ChargingStationOperator 'DE*GEF' ('evTaler') needs a signature by a signing key of the ChargingStationOperator 'DE*GEF' that may sign 'payments'!
Committed, revision 10 of epoch 2: DE*GEF: its EV Taler merchant, accepting an exchange in euros and one in Swiss francs
Committed, revision 11 of epoch 2: Jena Paradies: EV Taler
At DE*GEF*E1*1, as the server sees it: EV Taler (Example Exchange, EUR)

The station proof of DE*GEF*E1*1, revision 11: 146 KiB

The station proof, revision 11, by its attestations: valid
DE*GEF*P1, Jena Paradies: payment options EVTaler
DE*GEF*E1*1: its EV Taler tariff DE*GEF*T_DC, in EUR
  EV Taler (Example Exchange, EUR): coins of the exchange F80MFRG8HVH6R9CQ47KRFQSJP3T6DBJ4K1D9B703RJY3Z39TBMJ0 at https://exchange.example.eu/
  not offered there: EV Taler (Example Franc Exchange, CHF)
An offer for DE*GEF*E1*1 is DE*GEF's when its taler://pay URI names https://taler.gef.example/instances/de-gef/ and its contract terms are signed by ESMBKNR5CMZPAYH9AD0QNKCPYSJSE0EN5MH4FMPKN683R63HMD3G
Another merchant key: Valid attestations of trusted keys of 0 holders attest the root effectiveTree:sha256:hex:a256aa5085bc650cf6b7b080466a41d60738ba4c08eea10dbefb246052ad3b70 of the proof, not of 2!
```


## Limits

- This is phase 1 of the concept: what the content says. Asking for offers, the steps, their settlement and the
  evidence of a paid session (concepts/EVTaler.md, sections 5 to 7) are not built yet.
- POI proves that `DE*GEF` names this merchant, not that the merchant backend or the exchanges exist, accept `DE*GEF`
  or are configured alike: the app checks the contract terms of every offer.
- The URLs, the merchant and the exchange in Swiss francs are examples; the master public key of the exchange in euros
  is the one `exchange.demo.taler.net` served, whose coins are of the demo currency `KUDOS`, not of euros.
- OCPI and DATEX II know no GNU Taler: their publishers leave `EVTaler` and the merchant out.
