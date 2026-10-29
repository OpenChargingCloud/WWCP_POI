using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.WWCP;
using cloud.charging.open.protocols.WWCP.POI;


public static class ExampleA15
{

    // EV Taler: DE*GEF accepts GNU Taler at Jena Paradies, to be paid step by step (concepts/EVTaler.md). It names its
    // merchant at a GNU Taler merchant backend and the exchanges whose coins it accepts, signed by a key for payments alone:
    // whoever changes the merchant redirects the payments of every station. The pool names the payment option EVTaler, and
    // the station proof a driver's app gets gives the merchant: the app shows "EV Taler (provider)" for the exchanges in the
    // currency of the EVSE's tariff, and knows which merchant an offer must come from. Nothing here pays; GNU Taler does.


    internal static readonly ChargingPool_Id  JenaParadies  = ChargingPool_Id.Parse("DE*GEF*P1");

    /// <summary>
    /// The master public key of the exchange exchange.demo.taler.net, as it served it: a key as GNU Taler writes it.
    /// </summary>
    internal const String EURExchangeKey  = "F80MFRG8HVH6R9CQ47KRFQSJP3T6DBJ4K1D9B703RJY3Z39TBMJ0";

    /// <summary>
    /// Keys made for the example: the SHA-256 of a text, written as GNU Taler writes a key.
    /// </summary>
    internal const String CHFExchangeKey  = "YJ7H5JVNVM5SG5FAE3BGQ35AEPHXBH27MWNRPFQ0SVW9BH0BN9B0";
    internal const String MerchantKey     = "ESMBKNR5CMZPAYH9AD0QNKCPYSJSE0EN5MH4FMPKN683R63HMD3G";

    /// <summary>
    /// The station proof of DE*GEF*E1*1 the app gets, giving the merchant.
    /// </summary>
    internal const String Proof           = "published/QA-DE-GEF-E1-1-evTaler-proof.json";


    public static void Run(ExampleA11.Result A11)
    {

        var now      = Timestamp.Now;
        var history  = A11.History;
        var partyKey = A11.GEF.SigningKeys[POIKeyUsages.Party];

        #region 1. DE*GEF adds a signing key for payments

        // ML-DSA, as all its keys of epoch 2; kept apart from the others, e.g. offline.
        var paymentsKey = COSEKeyPair.NewMLDsa65();

        Helpers.SaveKey(paymentsKey.Private, "DE-GEF/signing-payments-mldsa-privateKey.pem");

        var gef         = Operator(history);

        // Its signing keys are rotated by its key for the party, in the ordinal order of their identifications.
        var addKey      = history.Head.RoamingNetwork.CreateChangeSet(
                              [ RoamingNetworkChange.UpdateProperty(gef, POIPartyKeys.SigningKeysProperty, null,
                                                                    new JArray(gef.SigningKeys.Append(POIPublicKey.From(paymentsKey.Private, NotAfter: now.AddYears(1), Usages: [ POIKeyUsages.Payments ])).
                                                                                               OrderBy(key => key.Id.ToString(), StringComparer.Ordinal).
                                                                                               Select (key => key.ToJSON(Embedded: true)))) ],
                              Description: ImmutableI18NString.Create(Languages.en, "DE*GEF: a signing key for payments")
                          );

        Committed(history, addKey.Sign(partyKey.Private, POIPublicKey.From(partyKey.Private).Id, partyKey.Algorithm));

        #endregion

        #region 2. DE*GEF names its merchant and the exchanges it accepts

        // Its instance at a hosted merchant backend, and two exchanges: one in euros, one in Swiss francs.
        var merchant     = new EVTalerMerchant(URL.Parse("https://taler.gef.example/instances/de-gef/"),
                                               MerchantKey,
                                               [ new EVTalerExchange(EURExchangeKey, URL.Parse("https://exchange.example.eu/"), Currency.EUR, ImmutableI18NString.Create(Languages.en, "Example Exchange")),
                                                 new EVTalerExchange(CHFExchangeKey, URL.Parse("https://exchange.example.ch/"), Currency.CHF, ImmutableI18NString.Create(Languages.en, "Example Franc Exchange")) ]);

        var nameMerchant = history.Head.RoamingNetwork.CreateChangeSet(
                               [ RoamingNetworkChange.UpdateProperty(Operator(history), ChargingStationOperator.PropertyKeys.EVTaler, null, merchant.ToJSON(Embedded: true)) ],
                               Description: ImmutableI18NString.Create(Languages.en, "DE*GEF: its EV Taler merchant, accepting an exchange in euros and one in Swiss francs")
                           );

        // Signed by its key for the party: refused, the merchant takes the payments.
        Console.WriteLine($"Signed by the key of DE*GEF for the party: {history.Commit(nameMerchant.Sign(partyKey.Private, POIPublicKey.From(partyKey.Private).Id, partyKey.Algorithm)).ErrorResponse}");

        Committed(history, nameMerchant.Sign(paymentsKey.Private, POIPublicKey.From(paymentsKey.Private).Id, paymentsKey.Algorithm));

        #endregion

        #region 3. Jena Paradies names the payment option EVTaler

        var infrastructureKey = A11.GEF.SigningKeys[POIKeyUsages.Infrastructure];
        var jenaParadies      = Operator(history).ChargingPools.Single(chargingPool => chargingPool.Id == JenaParadies);

        var nameOption        = history.Head.RoamingNetwork.CreateChangeSet(
                                    [ RoamingNetworkChange.UpdateProperty(jenaParadies, ChargingPool.PropertyKeys.PaymentOptions, null, new JArray(PaymentOptions.EVTaler.ToString())) ],
                                    Description: ImmutableI18NString.Create(Languages.en, "Jena Paradies: EV Taler")
                                );

        Committed(history, nameOption.Sign(infrastructureKey.Private, POIPublicKey.From(infrastructureKey.Private).Id, infrastructureKey.Algorithm));

        foreach (var exchange in Operator(history).EVTalerExchangesAt(ExampleA10.EVSEId))
            Console.WriteLine($"At {ExampleA10.EVSEId}, as the server sees it: {exchange.PaymentOptionName.FirstText()}");

        Console.WriteLine();

        #endregion

        #region 4. The station proof the app gets

        history.AttestationSigners = [ .. A11.Signers ];

        if (!history.TryAttest(out _, out var error))
            Helpers.Fail(error);

        if (!history.TryGetChargingStationProof(ExampleA10.EVSEId, out var proof, out error))
            Helpers.Fail(error);

        File.WriteAllText(Proof, proof.ToJSON().ToString());

        Console.WriteLine($"The station proof of {ExampleA10.EVSEId}, revision {proof.Revision}: {new FileInfo(Proof).Length / 1024} KiB");
        Console.WriteLine();

        #endregion

    }


    /// <summary>
    /// The app checks the station proof against the trust anchor of epoch 2, and shows how charging at DE*GEF*E1*1 can be
    /// paid with EV Taler: the exchanges of the merchant in the currency of the EVSE's tariff; and the merchant an offer
    /// must come from.
    /// </summary>
    public static void Validate()
    {

        #region 5. What the app shows, and the merchant of every offer

        if (!RoamingNetworkTrustAnchor.TryParse(File.ReadAllText("published/QA-trustAnchor-mldsa.txt"), out var anchor, out var error))
            Helpers.Fail(error);

        if (!RoamingNetworkProof.TryParse(Helpers.ReadJSON(Proof), out var proof, out error))
            Helpers.Fail(error);

        Console.WriteLine($"The station proof, revision {proof.Revision}, by its attestations: {(proof.Verify(anchor, out _, out error) ? "valid" : error)}");

        var gef      = proof.Given(RoamingNetwork.PropertyKeys.ChargingStationOperators).Single(json => json["@id"]?.Value<String>() == ExampleA2.gefId.ToString());
        var pool     = proof.GivenBelow(gef, ChargingStationOperator.PropertyKeys.ChargingPools).Single(json => json["@id"]?.Value<String>() == JenaParadies.ToString());
        var evse     = proof.GivenBelow(gef, ChargingStationOperator.PropertyKeys.ChargingPools, ChargingPool.PropertyKeys.ChargingStations, ChargingStation.PropertyKeys.EVSEs).Single(json => json["@id"]?.Value<String>() == ExampleA10.EVSEId.ToString());
        var tariffs  = proof.GivenBelow(gef, ChargingStationOperator.PropertyKeys.ChargingTariffs).ToDictionary(json => json["@id"]!.Value<String>()!);

        var merchantJSON = gef[ChargingStationOperator.PropertyKeys.EVTaler] as JObject;

        if (merchantJSON is null)
            Helpers.Fail($"The proof gives no EV Taler merchant of {ExampleA2.gefId}");

        if (!EVTalerMerchant.TryParse(merchantJSON, out var merchant, out error))
            Helpers.Fail(error);

        Console.WriteLine($"{pool["@id"]}, {pool["name"]?["en"]}: payment options {String.Join(", ", pool[ChargingPool.PropertyKeys.PaymentOptions]?.Values<String>() ?? [])}");

        // The EV Taler tariff of the EVSE: its ad hoc tariff, else the one without a type, naming no payment means or EV_TALER.
        var applying = evse[EVSE.PropertyKeys.ChargingTariffIds]!.Values<String>().OfType<String>().Select(id => tariffs[id]).ToArray();
        var tariff   = (applying.FirstOrDefault(json => json["tariffType"]?.Value<String>() == "AD_HOC_PAYMENT") ?? applying.FirstOrDefault(json => json["tariffType"] is null)) is JObject candidate &&
                       (candidate["paymentMeans"] is not JArray means || means.Values<String>().Contains(PaymentMeans.EV_TALER.ToString()))
                           ? candidate
                           : null;

        if (tariff is null)
            Helpers.Fail($"{ExampleA10.EVSEId} has no EV Taler tariff");

        if (!Currency.TryParseISO(tariff["currency"]!.Value<String>()!, out var currency))
            Helpers.Fail($"The EV Taler tariff {tariff["@id"]} has no ISO 4217 currency");

        Console.WriteLine($"{ExampleA10.EVSEId}: its EV Taler tariff {tariff["@id"]}, in {currency.ISOCode}");

        foreach (var exchange in merchant.ExchangesFor(currency))
            Console.WriteLine($"  {exchange.PaymentOptionName.FirstText()}: coins of the exchange {exchange.MasterPublicKey} at {exchange.BaseURL}");

        Console.WriteLine($"  not offered there: {String.Join(", ", merchant.Exchanges.Where(exchange => exchange.Currency.ISOCode != currency.ISOCode).Select(exchange => exchange.PaymentOptionName.FirstText()))}");

        // What every offer for this EVSE must name: the merchant base URL in its taler://pay URI, the merchant_pub of its
        // contract terms, and among their exchanges only these.
        Console.WriteLine($"An offer for {ExampleA10.EVSEId} is DE*GEF's when its taler://pay URI names {merchant.MerchantBaseURL} and its contract terms are signed by {merchant.MerchantPublicKey}");

        // A merchant changed on the way, e.g. to send the payments elsewhere: the proof is no longer one of the version the
        // attestations name.
        var changed = Helpers.ReadJSON(Proof);
        var changedMerchant = changed.Descendants().OfType<JProperty>().Single(property => property.Name == ChargingStationOperator.PropertyKeys.EVTaler).Value as JObject;
        changedMerchant![EVTalerMerchant.PropertyKeys.MerchantPublicKey] = CHFExchangeKey;

        Console.WriteLine($"Another merchant key: {(RoamingNetworkProof.TryParse(changed, out var changedProof, out error) && changedProof.Verify(anchor, out _, out error) ? "valid" : error)}");

        #endregion

    }


    #region Helpers

    // DE*GEF in the head of the given history.
    private static ChargingStationOperator Operator(RoamingNetworkHistory History)
    {

        if (!History.Head.RoamingNetwork.TryGetChargingStationOperatorById(ExampleA2.gefId, out var gef))
            Helpers.Fail($"QA has no charging station operator '{ExampleA2.gefId}'");

        return gef;

    }

    // The given change set committed to the given history.
    private static void Committed(RoamingNetworkHistory History, RoamingNetworkChangeSet ChangeSet)
    {

        var result = History.Commit(ChangeSet);

        if (!result.IsCommitted)
            Helpers.Fail(result.ErrorResponse ?? result.Status.ToString());

        Console.WriteLine($"{result.Status}, revision {result.Commit?.Revision} of epoch {History.Epoch.Number}: {ChangeSet.Description.FirstText()}");

    }

    #endregion

}
