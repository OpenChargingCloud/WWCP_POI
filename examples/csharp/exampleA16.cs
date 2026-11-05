using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using static System.FormattableString;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

using cloud.charging.open.protocols.WWCP;
using cloud.charging.open.protocols.WWCP.POI;


public static class ExampleA16
{

    // EV Taler, a session paid in two steps (concepts/EVTaler.md, 5 and 7): a driver charges at DE*GEF*E1*1 and pays the
    // first 5 kWh with a reserve, then the next 5 kWh, each step an order of DE*GEF's merchant; the meter signs the energy
    // register at the start and at the end; DE*GEF settles the session with its key for payments, and the rest is refunded.
    // The app checks every step against the station proof and the price statement before the driver pays, and keeps
    // what decides a dispute in one file. GNU Taler pays; here, the contract terms are as a merchant backend makes them.


    /// <summary>
    /// The private key of the meter of DE*GEF*E1*1, which example A3 keeps for this example alone: the example plays the
    /// meter.
    /// </summary>
    internal const String MeterKey   = "DE-GEF/meter-DE_GEF_M1_1-privateKey.pem";

    /// <summary>
    /// What the app keeps of the session.
    /// </summary>
    internal const String Evidence   = "driver/evidence-DE_GEF_E1_1-evTaler.json";

    /// <summary>
    /// The key of DE*GEF for payments, of example A15.
    /// </summary>
    private  const String PaymentsKey  = "DE-GEF/signing-payments-mldsa-privateKey.pem";

    /// <summary>
    /// The session at the offers server: 128 random bits, base64url.
    /// </summary>
    private  const String SessionId    = "kq3Vd0sY0f9hJ2bW8Qm1tA";


    public static void Run(ExampleA11.Result A11)
    {

        // In whole seconds, as GNU Taler writes times; the next one, so that the attestations of example A15 are before it.
        var now     = DateTimeOffset.FromUnixTimeSeconds(Timestamp.Now.ToUnixTimeSeconds() + 1);
        var evseId  = ExampleA10.EVSEId.ToString();

        #region 1. What the app holds: the station proof and the price statement of the EVSE

        if (!RoamingNetworkTrustAnchor.TryParse(File.ReadAllText("published/QA-trustAnchor-mldsa.txt"), out var anchor, out var error))
            Helpers.Fail(error);

        if (!RoamingNetworkProof.TryParse(Helpers.ReadJSON(ExampleA15.Proof), out var proof, out error))
            Helpers.Fail(error);

        if (!VerifiedChargingStation.TryVerify(anchor, proof, out var station, out error, EVSEId: evseId, At: now))
            Helpers.Fail(error);

        // The latest price statement of the EVSE, of example A11: ML-DSA, 0.59 €/kWh.
        var statement = A11.History.PriceStatementsOf(ExampleA10.EVSEId).
                            Where  (given => given.IsOf(nameof(EVSE), evseId)).
                            OrderBy(given => given.Sequence).
                            Last();

        if (!station.TryGetCurrentPrices([ statement ], out var current, out error, now))
            Helpers.Fail(error);

        if (!EVTalerContractTerms.TryGetEVTalerTariff(station, evseId, out var tariff, out error))
            Helpers.Fail(error);

        var tariffId  = tariff["@id"]!.Value<String>()!;
        var elements  = ((statement.PriceStatusJSON["prices"] as JArray)!.
                            OfType<JObject>().
                            Single(prices => prices["chargingTariffId"]!.Value<String>() == tariffId)["elements"] as JArray)!.
                            OfType<JObject>().
                            ToArray();

        Console.WriteLine($"The app, at {evseId}: the EV Taler tariff {tariffId}, priced by the price statement {statement.Digest[..8]}…, sequence {statement.Sequence}: current {current.Values.Any(price => price.Digest == statement.Digest)}");

        #endregion

        #region 2. Step 1: the first 5 kWh and a reserve

        // DE*GEF's backend makes the order at its merchant instance: what the step covers at the price of the session.
        if (!EVTalerRates.TryOf("EUR", tariff, elements, out var rates, out var reason))
            Helpers.Fail(reason);

        var step1     = new EVTalerStep(station.RoamingNetworkId, evseId, SessionId, 1, null, tariffId, statement.Digest, new EVTalerCovers(0, 5, EVTalerCovers.KWh), null);

        if (!rates.TryGetStepPrice(step1.Covers, out var stepPrice, out error))
            Helpers.Fail(error);

        var terms1    = ContractTerms(step1, "2026.309-01GEF1", stepPrice + 1.00m, now);

        // The app, before the driver pays: the merchant and the exchanges of the operator, the tariff and the price
        // statement, the amount.
        if (!terms1.TryVerify(station, [ statement ], null, null, out var verified1, out error))
            Helpers.Fail(error);

        Console.WriteLine(Invariant($"Step 1, order {terms1.OrderId}: {step1.Covers} at {rates.EnergyPerKWh} €/kWh {verified1.StepPrice:F2} €, flat fee {verified1.FlatFee:F2} €, reserve {verified1.Reserve:F2} €, {terms1.Amount:F2} €: {verified1.AmountCheck}"));

        #endregion

        #region 3. Charging starts: the meter signs the start value

        using var meter = ECDsa.Create();
        meter.ImportFromPem(File.ReadAllText(MeterKey));

        var startAt   = now.AddMinutes(1);
        var start     = OCMF(meter, "T1", "B", 1234.567m, startAt);

        Console.WriteLine($"The start value of DE*GEF*M1*1, OCMF, as the station sends it with OCPP 2.1: {Encoding.UTF8.GetString(Convert.FromBase64String(start.SignedMeterData))[..60]}…");

        #endregion

        #region 4. Step 2: the next 5 kWh, naming the start value

        var step2     = new EVTalerStep(station.RoamingNetworkId, evseId, SessionId, 2, terms1.OrderId, tariffId, statement.Digest, new EVTalerCovers(5, 10, EVTalerCovers.KWh), start);

        if (!rates.TryGetStepPrice(step2.Covers, out stepPrice, out error))
            Helpers.Fail(error);

        var terms2    = ContractTerms(step2, "2026.309-02GEF1", stepPrice, now.AddMinutes(20));

        // The start value, checked by a transparency software with the key of the meter the station proof gives.
        if (!terms2.TryVerify(station, [ statement ], verified1, VerifyOCMF, out var verified2, out error))
            Helpers.Fail(error);

        Console.WriteLine(Invariant($"Step 2, order {terms2.OrderId}: {step2.Covers}, {terms2.Amount:F2} €: {verified2.AmountCheck}; the start value {verified2.Start!.Reading.Energy} kWh of {verified2.Start.SignedBy}, serial {verified2.Start.Reading.MeterSerialNumber}"));

        #endregion

        #region 5. Charging ends: the meter signs the end value

        var endAt     = now.AddMinutes(35);
        var end       = OCMF(meter, "T2", "E", 1242.781m, endAt);

        #endregion

        #region 6. DE*GEF settles the session with its key for payments

        var key        = Helpers.ReadKey(PaymentsKey);
        var energy     = 1242.781m - 1234.567m;
        var price      = rates.SessionPrice(energy, endAt - startAt);

        var settlement = EVTalerSettlement.Create(key,
                                                  POIPublicKey.From(key).Id.ToString(),
                                                  COSEAlgorithm.MLDsa65,
                                                  ExampleA2.gefId,
                                                  station.RoamingNetworkId,
                                                  evseId,
                                                  SessionId,
                                                  ExampleA15.MerchantKey,
                                                  tariffId,
                                                  statement.Digest,
                                                  startAt,
                                                  endAt,
                                                  start,
                                                  end,
                                                  energy,
                                                  "EUR",
                                                  [ (terms1.OrderId, terms1.Amount), (terms2.OrderId, terms2.Amount) ],
                                                  price,
                                                  SignedAt: endAt.AddMinutes(1));

        Console.WriteLine(Invariant($"Settled by DE*GEF, {settlement.Algorithm}: {settlement.Energy} kWh, {settlement.Price:F2} €; paid {settlement.Paid:F2} €, refunded {settlement.Refunded:F2} €"));

        #endregion

        #region 7. The app keeps it

        // The file is this example's own: the concept names its parts, not how an app keeps them.
        File.WriteAllText(Evidence,
                          new JObject(new JProperty("EVSEId",           evseId),
                                      new JProperty("sessionStarted",   now.ToISO8601()),
                                      new JProperty("stationProof",     proof.ToJSON()),
                                      new JProperty("priceStatements",  new JArray(statement.ToJSON())),
                                      new JProperty("contractTerms",    new JArray(terms1.JSON, terms2.JSON)),
                                      new JProperty("settlement",       settlement.JSON)).ToString());

        Console.WriteLine($"Kept: {Evidence}, {new FileInfo(Evidence).Length / 1024} KiB");
        Console.WriteLine();

        #endregion

    }


    /// <summary>
    /// Months later, the session is disputed: the file alone decides it, against the trust anchor of epoch 2.
    /// </summary>
    public static void Validate()
    {

        #region 8. From the file alone

        if (!RoamingNetworkTrustAnchor.TryParse(File.ReadAllText("published/QA-trustAnchor-mldsa.txt"), out var anchor, out var error))
            Helpers.Fail(error);

        var evidence  = Helpers.ReadJSON(Evidence);
        var evseId    = evidence["EVSEId"]!.Value<String>()!;
        var started   = DateTimeOffset.Parse(evidence["sessionStarted"]!.Value<String>()!, CultureInfo.InvariantCulture);

        if (!RoamingNetworkProof.TryParse((JObject) evidence["stationProof"]!, out var proof, out error))
            Helpers.Fail(error);

        if (!VerifiedChargingStation.TryVerify(anchor, proof, out var station, out error, EVSEId: evseId, At: started))
            Helpers.Fail(error);

        var statements = new List<RoamingNetworkPriceStatement>();

        foreach (var json in (JArray) evidence["priceStatements"]!)
        {
            if (!RoamingNetworkPriceStatement.TryParse((JObject) json, out var parsed, out error))
                Helpers.Fail(error);
            statements.Add(parsed);
        }

        var steps = new List<EVTalerVerifiedStep>();

        foreach (var json in (JArray) evidence["contractTerms"]!)
        {

            if (!EVTalerContractTerms.TryParse(json, out var terms, out error))
                Helpers.Fail(error);

            if (!terms.TryVerify(station, statements, steps.LastOrDefault(), VerifyOCMF, out var verified, out error))
                Helpers.Fail(error);

            steps.Add(verified);

        }

        if (!EVTalerSettlement.TryParse(evidence["settlement"], out var settlement, out error))
            Helpers.Fail(error);

        if (!settlement.TryVerify(station, steps, VerifyOCMF, out var verification, out error))
            Helpers.Fail(error);

        var first = steps[0];

        Console.WriteLine(Invariant($"Was this the operator's price when the session began? {first.Step.ChargingTariffId}, {first.Rates!.EnergyPerKWh} €/kWh by the price statement {first.PriceStatement!.Digest[..8]}…, signed by {first.PriceStatement.Statement.Signer}, current at {started:u}"));
        Console.WriteLine($"Did I pay the operator? {steps.Count} orders, each of the merchant {first.Merchant.MerchantPublicKey[..8]}… the station proof gives, with exchanges of its evTaler");
        Console.WriteLine(Invariant($"Did I get what I paid for? {verification.Start.Reading.Energy} kWh to {verification.End.Reading.Energy} kWh, {verification.Energy} kWh, both signed by {verification.Start.SignedBy}, serial {verification.Start.Reading.MeterSerialNumber}, key {verification.Start.KeyId[..8]}…"));
        Console.WriteLine(Invariant($"Was the rest refunded? {settlement.Price:F2} € of {settlement.Paid:F2} €, {verification.PriceCheck} by the tariff; {settlement.Refunded:F2} € refunded, signed by {settlement.Signer} with its key for payments"));
        Console.WriteLine();

        #endregion

        #region 9. What is refused

        // A price lowered after signing, the refund raised with it.
        var cheaper = (JObject) settlement.JSON.DeepClone();
        cheaper["price"]    = "4.00";
        cheaper["refunded"] = EVTalerAmounts.Write(settlement.Paid - 4.00m, "EUR");

        Console.WriteLine($"Another price: {(EVTalerSettlement.TryParse(cheaper, out var changed, out error) && changed.TryVerify(station, steps, VerifyOCMF, out _, out error) ? "valid" : error)}");

        // The end value left out.
        var onlyStart = (JObject) settlement.JSON.DeepClone();
        ((JArray) onlyStart["meterValues"]!).RemoveAt(1);

        Console.WriteLine($"Without the end value: {(EVTalerSettlement.TryParse(onlyStart, out _, out error) ? "valid" : error)}");

        // An offer of another merchant.
        var foreign = (JObject) ((JArray) evidence["contractTerms"]!)[0].DeepClone();
        foreign["merchant_pub"] = ExampleA15.CHFExchangeKey;

        Console.WriteLine($"Step 1 of another merchant: {(EVTalerContractTerms.TryParse(foreign, out var foreignTerms, out error) && foreignTerms.TryVerify(station, statements, null, null, out _, out error) ? "valid" : error)}");

        // Step 2 naming another start value, signed by the meter all the same: its end value. The step alone does not tell,
        // the settlement does.
        var other = (JObject) ((JArray) evidence["contractTerms"]!)[1].DeepClone();
        other["extra"]!["wwcp"]!["startMeterValue"] = settlement.JSON["meterValues"]![1]!.DeepClone();

        Console.WriteLine($"Step 2 naming the end value as the start: {(EVTalerContractTerms.TryParse(other, out var otherTerms, out error) &&
                                                                         otherTerms.TryVerify(station, statements, steps[0], VerifyOCMF, out var otherStep, out error) &&
                                                                         settlement.TryVerify(station, [ steps[0], otherStep ], VerifyOCMF, out _, out error) ? "valid" : error)}");

        #endregion

    }


    #region Helpers

    // The contract terms of a step, as a merchant backend makes them, contract terms of version 0: what the step costs,
    // when it is paid, refunded and wired, the merchant and its exchange in euros, and the step in "extra": { "wwcp": … }.
    // The merchant signs them; the wallet checks that signature, not WWCP_POI. h_wire and nonce are made for the example.
    private static EVTalerContractTerms ContractTerms(EVTalerStep     Step,
                                                      String          OrderId,
                                                      Decimal         Amount,
                                                      DateTimeOffset  At)
    {

        var seconds = At.ToUnixTimeSeconds();

        var json    = new JObject(new JProperty("version",                 0),
                                  new JProperty("order_id",                OrderId),
                                  new JProperty("summary",                 $"Charging at {Step.EVSEId}, step {Step.Step}: {Step.Covers}"),
                                  new JProperty("amount",                  $"EUR:{EVTalerAmounts.Write(Amount, "EUR")}"),
                                  new JProperty("max_fee",                 "EUR:0.10"),
                                  new JProperty("products",                new JArray(new JObject(new JProperty("description", $"Charging, {Step.Covers}"),
                                                                                                  new JProperty("quantity",    Step.Covers.To - Step.Covers.From),
                                                                                                  new JProperty("unit",        Step.Covers.Unit)))),
                                  new JProperty("timestamp",               new JObject(new JProperty("t_s", seconds))),
                                  new JProperty("pay_deadline",            new JObject(new JProperty("t_s", seconds + 120))),
                                  new JProperty("refund_deadline",         new JObject(new JProperty("t_s", seconds + 86400))),
                                  new JProperty("wire_transfer_deadline",  new JObject(new JProperty("t_s", seconds + 2 * 86400))),
                                  new JProperty("auto_refund",             new JObject(new JProperty("d_us", 86400L * 1000000))),
                                  new JProperty("merchant_pub",            ExampleA15.MerchantKey),
                                  new JProperty("merchant_base_url",       "https://taler.gef.example/instances/de-gef/"),
                                  new JProperty("merchant",                new JObject(new JProperty("name", "GraphDefined"))),
                                  new JProperty("h_wire",                  "T6VBQ2P4BYZXTXJ6ZR0K5S9SXHFA7E7EK4N5Y7M03W7XF4R0X1D1JS9Q3RJGH6E4RDE5CHP1P85T3KVCY7ZAGAZJ1J2VK3T0DN0Q0R0"),
                                  new JProperty("wire_method",             "iban"),
                                  new JProperty("exchanges",               new JArray(new JObject(new JProperty("url",        "https://exchange.example.eu/"),
                                                                                                  new JProperty("priority",   1024),
                                                                                                  new JProperty("master_pub", ExampleA15.EURExchangeKey)))),
                                  new JProperty("nonce",                   "3BDCNJK7XWB1V4KN4ZQ8F0Q0YG2N6YVGQ5S8M5T1W1Q9G0K3PT20"),
                                  new JProperty("extra",                   new JObject(new JProperty("wwcp", Step.ToJSON()))));

        if (!EVTalerContractTerms.TryParse(json, out var terms, out var error))
            Helpers.Fail(error);

        return terms;

    }

    // A signed meter value in OCMF, as the meter of DE*GEF*E1*1 signs it and the station sends it with OCPP 2.1: the
    // payload, one reading of the energy register, and its ECDSA signature over the payload, hexadecimal DER; Base64
    // encoded, with the public key of the meter.
    private static EVTalerSignedMeterValue OCMF(ECDsa           Meter,
                                                String          Pagination,
                                                String          Transaction,
                                                Decimal         Energy,
                                                DateTimeOffset  At)
    {

        var payload    = new JObject(new JProperty("FV", "1.0"),
                                     new JProperty("GI", "GraphDefined GD-DC300"),
                                     new JProperty("GS", "DE*GEF*S1"),
                                     new JProperty("PG", Pagination),
                                     new JProperty("MV", "Kernberg DC Metering"),
                                     new JProperty("MM", "KDM-500"),
                                     new JProperty("MS", "DCM-2026-001-1"),
                                     new JProperty("IS", false),
                                     new JProperty("RD", new JArray(new JObject(new JProperty("TM", At.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss,fff", CultureInfo.InvariantCulture) + "+0000 S"),
                                                                                new JProperty("TX", Transaction),
                                                                                new JProperty("RV", Energy),
                                                                                new JProperty("RI", "1-b:1.8.0"),
                                                                                new JProperty("RU", "kWh"),
                                                                                new JProperty("RT", "DC"),
                                                                                new JProperty("ST", "G"))))).ToString(Newtonsoft.Json.Formatting.None);

        var signature  = Meter.SignData(Encoding.UTF8.GetBytes(payload), HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        var ocmf       = $"OCMF|{payload}|{{\"SA\":\"ECDSA-secp256r1-SHA256\",\"SD\":\"{Convert.ToHexString(signature)}\"}}";

        return new EVTalerSignedMeterValue(Convert.ToBase64String(Encoding.UTF8.GetBytes(ocmf)),
                                           "OCMF",
                                           "ECDSA-secp256r1-SHA256",
                                           Convert.ToBase64String(Meter.ExportSubjectPublicKeyInfo()));

    }

    // The transparency software of this example: it verifies OCMF of one reading signed with ECDSA-secp256r1-SHA256, as
    // far as this example needs; Chargy verifies OCMF and the other formats of meters.
    private static Boolean VerifyOCMF(EVTalerSignedMeterValue                        Value,
                                      MeasurementPublicKey                           Key,
                                      [NotNullWhen(true)]  out EVTalerMeterReading?  Reading,
                                      [NotNullWhen(false)] out String?               ErrorResponse)
    {

        Reading = null;

        var text   = Value.EncodingMethod == "OCMF" ? Encoding.UTF8.GetString(Convert.FromBase64String(Value.SignedMeterData)) : "";
        var last   = text.LastIndexOf('|');

        if (!text.StartsWith("OCMF|", StringComparison.Ordinal) || last <= 5)
        {
            ErrorResponse = "Not OCMF!";
            return false;
        }

        var payload    = text[5..last];
        var signature  = (JObject) POIJSON.Read(text[(last + 1)..]);

        if (signature["SA"]?.Value<String>() is not null and not "ECDSA-secp256r1-SHA256")
        {
            ErrorResponse = $"Unknown signature algorithm '{signature["SA"]}'!";
            return false;
        }

        using var publicKey = ECDsa.Create();
        publicKey.ImportSubjectPublicKeyInfo(Key.Key.AsSpan(), out _);

        if (!publicKey.VerifyData(Encoding.UTF8.GetBytes(payload), Convert.FromHexString(signature["SD"]!.Value<String>()!), HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence))
        {
            ErrorResponse = "The OCMF signature is invalid!";
            return false;
        }

        var json     = (JObject) POIJSON.Read(payload);
        var reading  = (JObject) ((JArray) json["RD"]!).Single();

        if (reading["ST"]?.Value<String>() != "G" || reading["RU"]?.Value<String>() != "kWh")
        {
            ErrorResponse = "Only a good reading in kWh is taken!";
            return false;
        }

        // "2026-11-05T21:10:00,000+0000 S": the time, its offset written without a colon, and its synchronisation.
        var time     = reading["TM"]!.Value<String>()!.Split(' ')[0];

        Reading        = new EVTalerMeterReading(json["MS"]!.Value<String>()!,
                                                 DateTimeOffset.ParseExact(time[..^2] + ":" + time[^2..], "yyyy-MM-dd'T'HH:mm:ss,fffzzz", CultureInfo.InvariantCulture),
                                                 reading["RV"]!.Value<Decimal>());
        ErrorResponse  = null;
        return true;

    }

    #endregion

}
