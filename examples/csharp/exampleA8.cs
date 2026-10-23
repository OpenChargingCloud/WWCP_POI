using System.Globalization;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

using cloud.charging.open.protocols.WWCP;
using cloud.charging.open.protocols.WWCP.POI;


public static class ExampleA8
{

    // DE*GEF adds its charging tariffs, an AC and a DC one, and its EVSE templates name them; the roaming network
    // operator makes checkpoint 3. Then DE*GEF states ad hoc prices of the DC EVSEs of Jena Paradies, which change by the
    // minute and are no content: price statements, signed by its key for its tariffs and prices, chained per EVSE. Then it
    // raises its AC tariff, a change of content, and the roaming network operator makes checkpoint 4, which names the price
    // statements taken since checkpoint 3: so they were made between the two.


    internal static readonly ChargingTariff_Id  AC      = ChargingTariff_Id.Parse("DE*GEF*T_AC");
    internal static readonly ChargingTariff_Id  DC      = ChargingTariff_Id.Parse("DE*GEF*T_DC");
    internal static readonly String[]           EVSEs   = [ "DE*GEF*E1*1", "DE*GEF*E1*2" ];


    public static void Run(ExampleA1.Result A1, ExampleA2.Result A2)
    {

        // Its key for its tariffs may sign price statements as well (example A2): a key stolen from the system setting the
        // prices signs no charging pools.
        var tariffsKey = A2.SigningKeys[POIKeyUsages.Tariffs];

        #region 1. The charging tariffs, and the templates naming them

        // Tariffs are content and change slowly: added by a change set signed by the key of DE*GEF for its tariffs.
        ExampleA4.CommitWith(A1, tariffsKey, "The AC and the DC charging tariffs",
                             RoamingNetworkChange.Add(Tariff(AC, "AC charging", 0.49m)),
                             RoamingNetworkChange.Add(Tariff(DC, "DC charging", 0.69m)));

        // An EVSE takes the tariff of the first of its EVSE templates naming one: the templates are infrastructure.
        if (!A1.History.Head.RoamingNetwork.TryGetChargingStationOperatorById(ExampleA2.gefId, out var gef))
            Helpers.Fail($"QA has no charging station operator '{ExampleA2.gefId}'");

        ExampleA4.Commit(A1, A2, "The EVSE templates name the tariffs",
                         RoamingNetworkChange.UpdateProperty(gef.EVSETemplates.Single(template => template.Id.ToString() == "DE*GEF*ET_AC"), "chargingTariffId", null, AC.ToString()),
                         RoamingNetworkChange.UpdateProperty(gef.EVSETemplates.Single(template => template.Id.ToString() == "DE*GEF*ET_DC"), "chargingTariffId", null, DC.ToString()));

        #endregion

        #region 2. Checkpoint 3

        var checkpoint3 = ExampleA4.MakeCheckpoint(A1);

        Console.WriteLine();

        #endregion

        #region 3. Price statements

        // DE*GEF states prices of the DC EVSEs of Jena Paradies, its own program signing them; the server of QA takes them.
        // Each statement names the latest checkpoint DE*GEF knew, so it was made after it. The first one of an EVSE has the
        // sequence 0, every next one names the digest of the one before.
        var now          = DateTimeOffset.FromUnixTimeMilliseconds(Timestamp.Now.ToUnixTimeMilliseconds());
        var statements   = new List<RoamingNetworkPriceStatement>();

        RoamingNetworkPriceStatement State(String EVSE, Decimal Price, Int32 Millisecond, RoamingNetworkPriceStatement? Previous, COSEKeyPair? Key = null)
        {

            var key = Key ?? tariffsKey;

            return RoamingNetworkPriceStatement.Create(key.Private,
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

        }

        void Take(RoamingNetworkPriceStatement Statement, String What)
        {

            var result = A1.History.TryAddPriceStatement(Statement, out var error);

            Console.WriteLine($"{What}: {result}{(error is null ? "" : $", {error}")}");

            if (result == POIPriceStatementResult.Taken)
                statements.Add(Statement);

        }

        // a) A happy hour: 0.59 instead of 0.69 per kWh at both DC EVSEs.
        var e11First  = State(EVSEs[0], 0.59m, 0, null);
        var e12First  = State(EVSEs[1], 0.59m, 0, null);

        Take(e11First, $"{EVSEs[0]}, 0.59 €/kWh, sequence 0");
        Take(e12First, $"{EVSEs[1]}, 0.59 €/kWh, sequence 0");

        // b) Then a higher price at the first one, naming the statement before.
        var e11Second = State(EVSEs[0], 0.79m, 1, e11First);

        Take(e11Second, $"{EVSEs[0]}, 0.79 €/kWh, sequence 1");

        // c) What is not taken: the first statement again, another statement of the sequence 1, a split view, and one
        //    signed by the key of DE*GEF for its infrastructure, which may not sign prices.
        Take(e11First,                                                    $"{EVSEs[0]}, sequence 0 again");
        Take(State(EVSEs[0], 0.39m, 2, e11First),                         $"{EVSEs[0]}, another statement of the sequence 1");
        Take(State(EVSEs[1], 0.39m, 2, e12First, A2.SigningKeys[POIKeyUsages.Infrastructure]), $"{EVSEs[1]}, signed by the infrastructure key");

        Console.WriteLine();

        // What a driver's app gets of an EVSE, as GET ~/RNs/QA/EVSEs/{EVSE_Id}/priceStatements: the current statements of
        // its pool, its station and the EVSE.
        File.WriteAllText($"published/QA-priceStatements-{EVSEs[0]}.json".Replace('*', '_'),
                          new JArray(A1.History.PriceStatementsOf(EVSE_Id.Parse(EVSEs[0])).Select(statement => statement.ToJSON())).ToString());

        #endregion

        #region 4. A change of a tariff, and checkpoint 4

        // The AC tariff becomes 0.52 per kWh: a change of content, by a commit.
        if (!A1.History.Head.RoamingNetwork.TryGetChargingStationOperatorById(ExampleA2.gefId, out gef))
            Helpers.Fail($"QA has no charging station operator '{ExampleA2.gefId}'");

        ExampleA4.CommitWith(A1, tariffsKey, "The AC charging tariff: 0.52 €/kWh",
                             RoamingNetworkChange.UpdateProperty(gef.ChargingTariffs.Single(tariff => tariff.Id == AC), "elements", null, Elements(0.52m)));

        // Checkpoint 4 names the price statements taken since checkpoint 3 and signed before it, by the root of their tree.
        var checkpoint4 = ExampleA4.MakeCheckpoint(A1);

        Console.WriteLine($"  naming {checkpoint4.PriceStatementsSize} price statements, root {checkpoint4.PriceStatementsRoot?[..8]}…");

        // The statements it names, as GET ~/RNs/QA/history/checkpoints/{checkpointId}/priceStatements, and the proof of the
        // happy hour at the first EVSE.
        var named = A1.History.PriceStatementsOf(checkpoint4.Id);

        if (named is null)
            Helpers.Fail("the checkpoint names no price statements here");

        File.WriteAllText("published/QA-checkpoint-4-priceStatements.json", new JArray(named.Select(statement => statement.ToJSON())).ToString());

        if (!A1.History.TryGetPriceStatementProof(checkpoint4.Id, e11First.Digest, out var proof, out var proofError))
            Helpers.Fail(proofError);

        File.WriteAllText("published/QA-priceStatementProof-E1_1-0.json", proof.ToJSON().ToString());

        ExampleA4.Publish(A1);

        Console.WriteLine();

        #endregion

    }


    /// <summary>
    /// Checks the price statements of QA as a driver's app or an e-mobility provider does: each against the signing keys of
    /// DE*GEF and the one before it, the ones checkpoint 4 names against its tree, the proof of one of them, and joining
    /// from checkpoint 4 with its statements; from what is published only.
    /// </summary>
    public static void Validate()
    {

        #region 5. The checkpoints and the tariffs

        // a) QA at its head, as everybody checks it, and the checkpoints 3 and 4, signed by the quorum of OCC.
        ExampleA1.Validate();

        var (anchor, network) = ExampleA4.Checked();
        var checkpoint3       = ExampleA4.CheckedCheckpoint("published/QA-checkpoint-3.json", anchor, network);
        var checkpoint4       = ExampleA4.CheckedCheckpoint("published/QA-checkpoint-4.json", anchor, network);

        if (!network.TryGetChargingStationOperatorById(ExampleA2.gefId, out var gef))
            Helpers.Fail($"QA has no charging station operator '{ExampleA2.gefId}'");

        // b) The tariffs of the DC EVSEs of Jena Paradies, as they are at runtime: from their EVSE template.
        foreach (var evse in gef.EffectiveChargingPools.SelectMany(pool => pool.ChargingStations).SelectMany(station => station.EVSEs).Where(evse => EVSEs.Contains(evse.Id.ToString())))
            Console.WriteLine($"{evse.Id}: {String.Join(", ", evse.ChargingTariffIds.Select(tariffId => $"{tariffId} {gef.ChargingTariffs.Single(tariff => tariff.Id == tariffId).Elements[0].PriceComponents[0].Price.Euro()} €/kWh"))}");

        Console.WriteLine($"{AC}: {gef.ChargingTariffs.Single(tariff => tariff.Id == AC).Elements[0].PriceComponents[0].Price.Euro()} €/kWh");
        Console.WriteLine();

        #endregion

        #region 6. The price statements of an EVSE, as an app gets them

        // Each signed by a signing key of DE*GEF, the operator of its EVSE, valid when it signed and one that may sign
        // prices; valid now; and made after checkpoint 3, which it names.
        var signingKeys = gef.SigningKeys;

        foreach (var statement in Statements($"published/QA-priceStatements-{EVSEs[0]}.json".Replace('*', '_')))
        {

            if (!statement.TryVerify(signingKeys, out var statementError))
                Helpers.Fail(statementError);

            Console.WriteLine($"{statement.EntityType} {statement.EntityId}, sequence {statement.Sequence}: {String.Join(", ", statement.PriceStatus.Prices.Select(prices => $"{prices.ChargingTariffId} {prices.Elements[0].PriceComponents[0].Price.Euro()} €/kWh"))}, signed by {statement.Signer}, valid now: {statement.IsValidAt(Timestamp.Now)}, after checkpoint {(statement.After == checkpoint3.Id ? 3 : -1)}");

        }

        Console.WriteLine();

        #endregion

        #region 7. The price statements of checkpoint 4

        // Each signed by a signing key of DE*GEF before the checkpoint, after checkpoint 3; each one following the one
        // before of its EVSE; and their tree is the one the checkpoint signs.
        var named = Statements("published/QA-checkpoint-4-priceStatements.json");

        foreach (var statement in named)
        {

            if (!statement.TryVerify(signingKeys, out var statementError))
                Helpers.Fail(statementError);

            if (statement.SignedAt > checkpoint4.CreatedAt || statement.After != checkpoint3.Id)
                Helpers.Fail($"the price statement {statement.Digest} was not made between the checkpoints 3 and 4");

            if (statement.Sequence > 0 && named.FirstOrDefault(other => other.IsOf(statement.EntityType, statement.EntityId) && other.Sequence == statement.Sequence - 1) is RoamingNetworkPriceStatement previous && !statement.Follows(previous))
                Helpers.Fail($"the price statement {statement.Digest} does not follow the one before");

        }

        var root = RoamingNetworkPriceStatement.RootOf(RoamingNetworkPriceStatement.InOrder(named), checkpoint4.IdentificationHash);

        Console.WriteLine($"Checkpoint 4 names {checkpoint4.PriceStatementsSize} price statements; the root of those published: {(root == checkpoint4.PriceStatementsRoot && (UInt64) named.Count == checkpoint4.PriceStatementsSize ? "the same" : "another")}");

        #endregion

        #region 8. Evidence of one statement

        // The proof of the happy hour at the first EVSE: kept with the statement, it shows that the statement was made after
        // checkpoint 3, which it names, and before checkpoint 4, which names it; with witnesses, as in example A6, without
        // asking DE*GEF or the server of QA.
        if (!RoamingNetworkPriceStatementProof.TryParse(Helpers.ReadJSON("published/QA-priceStatementProof-E1_1-0.json"), out var proof, out var proofError))
            Helpers.Fail(proofError);

        Console.WriteLine($"The proof of {proof.PriceStatement.EntityId}, sequence {proof.PriceStatement.Sequence}, in checkpoint 4: {(proof.Verify(checkpoint4, out proofError) ? "valid" : proofError)}");
        Console.WriteLine();

        #endregion

        #region 9. Joining from checkpoint 4, with its price statements, and what is refused

        var verify     = ExampleA4.VerifyCheckpointSignature(anchor, network);
        String[] roles = [ RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator ];
        var snapshot   = File.ReadAllText("published/QA-checkpoint-4-snapshot.json");

        // a) With the statements it names: their tree must be the one of the checkpoint.
        Console.WriteLine($"Joined from checkpoint 4 with its price statements: {(RoamingNetworkHistory.TryFromCheckpointSnapshot(snapshot, out var joined, out var error, verify, roles, named) ? $"revision {joined.Head.Commit.Revision}, {joined.PriceStatementsOf(checkpoint4.Id)?.Count} statements" : error)}");

        // b) Without one of them: the tree of the others is another one.
        Console.WriteLine($"Joined with a statement less: {(RoamingNetworkHistory.TryFromCheckpointSnapshot(snapshot, out _, out error, verify, roles, named.Skip(1)) ? "taken" : error)}");

        // c) A statement of another price: its signature is not the one of its content.
        var changed = (JObject) Helpers.ReadJSONArray("published/QA-checkpoint-4-priceStatements.json")[0];
        changed!["priceStatus"]!["prices"]![0]!["elements"]![0]!["priceComponents"]![0]!["price"] = 0.09m;

        Console.WriteLine($"A statement of another price: {(RoamingNetworkPriceStatement.TryParse(changed, out var changedStatement, out error) && changedStatement.TryVerify(signingKeys, out error) ? "valid" : error)}");

        #endregion

    }


    #region Helpers

    // A charging tariff of DE*GEF of the given price per kWh.
    private static ChargingTariff Tariff(ChargingTariff_Id Id, String Name, Decimal Price)

        => new (Id,
                Currency.EUR,
                [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, Price, StepSize: 1) ]) ],
                Name: I18NString.Create(Languages.en, Name));

    // The elements of a charging tariff of the given price per kWh, as JSON.
    private static JArray Elements(Decimal Price)

        => new (new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, Price, StepSize: 1) ]).ToJSON());

    // A price status of a new price per kWh of the given charging tariff.
    private static ChargingPrices Prices(ChargingTariff_Id TariffId, Decimal Price)

        => new ([ new ChargingTariffPrices(TariffId, [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, Price, StepSize: 1) ]) ]) ]);

    // The price statements of the given file.
    private static IReadOnlyList<RoamingNetworkPriceStatement> Statements(String File)
    {

        var statements = new List<RoamingNetworkPriceStatement>();

        foreach (var json in Helpers.ReadJSONArray(File).Cast<JObject>())
        {

            if (!RoamingNetworkPriceStatement.TryParse(json, out var statement, out var error))
                Helpers.Fail(error);

            statements.Add(statement);

        }

        return statements;

    }

    #endregion

}


public static class Euros
{

    // A price as the examples print it, whatever the culture of the machine: 0.69.
    public static String Euro(this Decimal Price)
        => Price.ToString("0.00", CultureInfo.InvariantCulture);

}
