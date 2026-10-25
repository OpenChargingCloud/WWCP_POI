using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

using cloud.charging.open.protocols.WWCP;
using cloud.charging.open.protocols.WWCP.POI;


public static class ExampleA9
{

    // The two witnesses of example A6 go on with their copies of the history of QA: they take the commits of example A8,
    // the checkpoints 3 and 4, and sign them. Checkpoint 4 names price statements: a witness signs it only after it checked
    // them, their tree the one of the checkpoint, each one signed before it by a signing key of its operator in the version
    // of the checkpoint or of the one before. So a proof of a price statement in checkpoint 4 is witnessed evidence.


    public static void Run(ExampleA1.Result A1, ExampleA6.Result A6)
    {

        #region 1. What the roaming network operator publishes for them

        // The commits since checkpoint 2, the head the witnesses hold, as GET ~/RNs/QA/history/commits?since={commit} gives
        // them; the checkpoints 3 and 4 and the price statements of checkpoint 4 are those of example A8.
        ExampleA4.PublishCommitsSince(A1, A1.History.Checkpoints[2]);

        #endregion

        #region 2. Each witness checks and signs

        var signatures = new List<(RoamingNetworkCheckpoint_Id CheckpointId, RoamingNetworkCommitSignature Signature)>();
        var statements = Statements("published/QA-checkpoint-4-priceStatements.json");

        foreach (var (witness, history) in A6.Histories)
        {

            var signer = ExampleA6.WitnessOf(A6, witness, A6.Keys[witness]);

            // a) The commits of example A8, each checked, and checkpoint 3, which names no price statements.
            ExampleA6.AppendCommits(history, "published/QA-commits-since-checkpoint-2.json");

            if (!ExampleA6.TryTakeCheckpoint(history, 3, out var checkpoint3, out var error))
                Helpers.Fail(error);

            if (!history.TryWitness(checkpoint3.Id, signer, out _, out var signature3, out error))
                Helpers.Fail(error);

            signatures.Add((checkpoint3.Id, signature3));

            // b) Checkpoint 4, first without its price statements: a server takes it all the same, a witness does not sign
            //    it.
            if (!ExampleA6.TryTakeCheckpoint(history, 4, out var checkpoint4, out error))
                Helpers.Fail(error);

            Console.WriteLine($"{witness}, checkpoint 4 without its price statements: {(history.TryWitness(checkpoint4.Id, signer, out _, out _, out error) ? "signed" : error)}");

            // c) Its price statements, one of them missing, or one changed by a forger: the tree of those given is not the one
            //    the checkpoint signs.
            Console.WriteLine($"{witness}, a price statement missing: {(history.TryAddCheckpointPriceStatements(checkpoint4.Id, statements.Skip(1), out error) ? "taken" : error)}");
            Console.WriteLine($"{witness}, a price statement changed: {(history.TryAddCheckpointPriceStatements(checkpoint4.Id, [ .. statements.Skip(1), Changed(statements[0], A6.Keys[witness]) ], out error) ? "taken" : error)}");

            // d) Its price statements as published: checked, and then the checkpoint is signed.
            if (!history.TryAddCheckpointPriceStatements(checkpoint4.Id, statements, out error))
                Helpers.Fail(error);

            if (!history.TryWitness(checkpoint4.Id, signer, out _, out var signature4, out error))
                Helpers.Fail(error);

            signatures.Add((checkpoint4.Id, signature4));

            Console.WriteLine($"{witness}: revision {history.Head.Commit.Revision}, signs checkpoint 3, and checkpoint 4 with its {history.PriceStatementsOf(checkpoint4.Id)?.Count} price statements checked");
            Console.WriteLine();

        }

        #endregion

        #region 3. The roaming network operator takes their signatures

        TakeSignatures(A1, signatures);

        Console.WriteLine();

        #endregion

    }


    /// <summary>
    /// Checks the checkpoints 3 and 4 as a verifier requiring the roaming network operator and both witnesses, and the
    /// proof of a price statement against the witnessed checkpoint 4; from what is published and the keys of the witnesses
    /// only.
    /// </summary>
    public static void Validate()
    {

        #region 4. Price statements witnessed

        // a) QA at its head, as everybody checks it, and the witnesses this verifier trusts.
        ExampleA1.Validate();

        var (anchor, network) = ExampleA4.Checked();
        var witnesses         = ExampleA6.TrustedWitnesses();
        var verify            = ExampleA6.VerifyCheckpointSignature(anchor, network, witnesses);
        String[] roles        = [ RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator, RoamingNetworkCommitSignatureRoles.Witness ];

        // b) The checkpoints 3 and 4: signed by the quorum of OCC and by both witnesses.
        var checkpoints = new List<RoamingNetworkCheckpoint>();

        foreach (var sequence in new[] { 3, 4 })
        {

            var checkpoint = ExampleA4.CheckedCheckpoint($"published/QA-checkpoint-{sequence}.json", anchor, network);
            var witnessed  = ExampleA6.WitnessedBy(checkpoint, witnesses);

            if (witnessed.Count < ExampleA6.Witnesses.Length)
                Helpers.Fail($"the checkpoint {checkpoint.Sequence} is witnessed by {witnessed.Count} of the {ExampleA6.Witnesses.Length} witnesses only");

            Console.WriteLine($"  witnessed by {String.Join(", ", witnessed)}{(checkpoint.PriceStatementsSize > 0 ? $", its {checkpoint.PriceStatementsSize} price statements checked" : "")}");

            checkpoints.Add(checkpoint);

        }

        Console.WriteLine();

        // c) The proof of the happy hour at DE*GEF*E1*1 of example A8, now in a witnessed checkpoint: the statement was made
        //    after checkpoint 3 and before checkpoint 4, and both witnesses checked it, without asking DE*GEF or the server
        //    of QA.
        if (!RoamingNetworkPriceStatementProof.TryParse(Helpers.ReadJSON("published/QA-priceStatementProof-E1_1-0.json"), out var proof, out var error))
            Helpers.Fail(error);

        Console.WriteLine($"The proof of {proof.PriceStatement.EntityId}, sequence {proof.PriceStatement.Sequence}, {proof.PriceStatement.PriceStatus.Prices[0].Elements[0].PriceComponents[0].Price.Euro()} €/kWh, in checkpoint 4, witnessed by {String.Join(", ", ExampleA6.WitnessedBy(checkpoints[1], witnesses))}: {(proof.Verify(checkpoints[1], out error) ? "valid" : error)}");

        // d) Joining from checkpoint 4 with its price statements, requiring both roles.
        if (!RoamingNetworkHistory.TryFromCheckpointSnapshot(File.ReadAllText("published/QA-checkpoint-4-snapshot.json"), out var joined, out error, verify, roles, Statements("published/QA-checkpoint-4-priceStatements.json")))
            Helpers.Fail(error);

        Console.WriteLine($"Joined from checkpoint 4, signed by the roaming network operator and its witnesses: revision {joined.Head.Commit.Revision}, {joined.PriceStatementsOf(checkpoints[1].Id)?.Count} price statements");

        #endregion

    }


    #region Helpers, also of example A10

    // The signatures of the witnesses, taken by the roaming network operator with the witnesses it trusts, and the
    // checkpoints and their snapshots published again, their @ids unchanged.
    internal static void TakeSignatures(ExampleA1.Result A1, IEnumerable<(RoamingNetworkCheckpoint_Id CheckpointId, RoamingNetworkCommitSignature Signature)> Signatures)
    {

        var verify = ExampleA6.VerifyCheckpointSignature(A1.TrustAnchor, A1.RoamingNetwork, ExampleA6.TrustedWitnesses());

        foreach (var group in Signatures.GroupBy(signature => signature.CheckpointId))
        {

            if (!A1.History.TryAddCheckpointSignatures(group.Key, [.. group.Select(signature => signature.Signature)], verify, out var signed, out var newSignatures, out var error))
                Helpers.Fail(error);

            if (!A1.History.TryGetCheckpointSnapshotJSON(signed.Id, out var snapshot, out error))
                Helpers.Fail(error);

            File.WriteAllText($"published/QA-checkpoint-{signed.Sequence}.json",           signed.  ToJSON().ToString());
            File.WriteAllText($"published/QA-checkpoint-{signed.Sequence}-snapshot.json",  snapshot.ToString());

            Console.WriteLine($"Checkpoint {signed.Sequence}, {signed.Id.ToString()[..8]}…: {newSignatures.Count} signatures more, {signed.Signatures.Length} in all");

        }

    }

    // The price statements of the given file.
    internal static IReadOnlyList<RoamingNetworkPriceStatement> Statements(String File)
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

    // The given price statement of another price, signed anew by a key that is not one of DE*GEF, naming the key of
    // DE*GEF: as a forger would make it.
    private static RoamingNetworkPriceStatement Changed(RoamingNetworkPriceStatement Statement, COSEKeyPair Key)

        => RoamingNetworkPriceStatement.Create(Key.Private,
                                               Statement.KeyId,
                                               Key.Algorithm,
                                               Statement.RoamingNetworkId,
                                               Statement.EntityType,
                                               Statement.EntityId,
                                               new ChargingPrices([ new ChargingTariffPrices(Statement.PriceStatus.Prices[0].ChargingTariffId, [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.09m, StepSize: 1) ]) ]) ]),
                                               Statement.Sequence,
                                               Statement.Previous,
                                               Statement.SignedAt,
                                               Statement.ValidFrom,
                                               Statement.ValidUntil,
                                               Statement.After);

    #endregion

}
