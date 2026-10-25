using System.Globalization;
using System.Security.Cryptography;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

using cloud.charging.open.protocols.WWCP;
using cloud.charging.open.protocols.WWCP.POI;


public static class ExampleA10
{

    // A driver charges at DE*GEF*E1*1. Before charging, the driver's app gets the proof of the charging station, attested by
    // the roaming network operator, and the current price statement of the EVSE, and checks both offline against the trust
    // anchor of QA it scanned. It keeps them. Later, when a checkpoint witnessed by both witnesses names the statement, it
    // gets the proof of the commit of the station proof in a checkpoint, the proof of the statement in the next one, and the
    // two checkpoints. Then the evidence shows, months later and without asking DE*GEF or the server of QA, that exactly this
    // tariff and this price applied at exactly this charging station when charging began.


    internal static readonly EVSE_Id  EVSEId    = EVSE_Id.Parse("DE*GEF*E1*1");
    internal const           String   Evidence  = "driver/evidence-DE_GEF_E1_1.json";


    public static void Run(ExampleA1.Result A1, ExampleA2.Result A2, ExampleA6.Result A6)
    {

        Directory.CreateDirectory("driver");

        var tariffsKey   = A2.SigningKeys[POIKeyUsages.Tariffs];
        var checkpoint4  = A1.History.Checkpoints[4];

        #region 1. The server of QA attests its head, and DE*GEF states a new price

        // The roaming network operator attests the head at its server with the signing keys of its holders, again every
        // few minutes: valid for 15 minutes, so a proof is fresh.
        A1.History.AttestationSigners = [.. A1.Signers];

        if (!A1.History.TryAttest(out var attestations, out var error))
            Helpers.Fail(error);

        var holders = attestations.Select(attestation => A1.RoamingNetwork.Operator!.SigningKeyCertificates.Single(certificate => certificate.SigningKey.Id == attestation.KeyId).SigningKey.HolderOrUnnamed);

        Console.WriteLine($"Revision {A1.History.Head.Commit.Revision} attested as {attestations[0].Signer} by {String.Join(", ", holders)}, until {attestations[0].ValidUntil.ToISO8601()}");

        // The happy hour of example A8 is over: 0.65 €/kWh at DE*GEF*E1*1, the next statement of its chain, made after
        // checkpoint 4.
        var now       = DateTimeOffset.FromUnixTimeMilliseconds(Timestamp.Now.ToUnixTimeMilliseconds());
        var previous  = A1.History.PriceStatementsOf(EVSEId).Single(statement => statement.IsOf(nameof(EVSE), EVSEId.ToString()));

        var statement = RoamingNetworkPriceStatement.Create(tariffsKey.Private,
                                                            POIPublicKey.From(tariffsKey.Private).Id,
                                                            tariffsKey.Algorithm,
                                                            A1.RoamingNetwork.Id,
                                                            nameof(EVSE),
                                                            EVSEId.ToString(),
                                                            ExampleA8.Prices(ExampleA8.DC, 0.65m),
                                                            previous.Sequence + 1,
                                                            previous.Digest,
                                                            SignedAt:    now,
                                                            ValidFrom:   now,
                                                            ValidUntil:  now.AddHours(1),
                                                            After:       checkpoint4.Id);

        Console.WriteLine($"{EVSEId}, 0.65 €/kWh, sequence {statement.Sequence}: {A1.History.TryAddPriceStatement(statement, out error)}{(error is null ? "" : $", {error}")}");
        Console.WriteLine();

        #endregion

        #region 2. Before charging: what the app gets, checks and keeps

        // a) As GET ~/RNs/QA/EVSEs/DE*GEF*E1*1/proof and GET ~/RNs/QA/EVSEs/DE*GEF*E1*1/priceStatements: the proof of its
        //    station with the attestations of the head, and the current statements of its pool, its station and the EVSE.
        if (!A1.History.TryGetChargingStationProof(EVSEId, out var proof, out error))
            Helpers.Fail(error);

        var statements = A1.History.PriceStatementsOf(EVSEId);

        // b) The app checks them offline against the trust anchor it scanned, as charging begins: the proof attested by the
        //    quorum of OCC, each statement signed by a signing key of DE*GEF the proof gives, of an entity the proof gives,
        //    valid now.
        var anchor  = A1.TrustAnchor;
        var started = DateTimeOffset.FromUnixTimeMilliseconds(Timestamp.Now.ToUnixTimeMilliseconds());

        foreach (var given in statements)
            if (!given.TryVerify(proof, anchor.Keys, out _, out error, started, anchor.MinConsensus))
                Helpers.Fail(error);

        Console.WriteLine($"The app, at {EVSEId}: {proof.RoamingNetworkName.FirstText()} of {proof.RoamingNetworkOperatorName.FirstText()}, revision {proof.Revision}{(proof.TrustAnchorTextMismatch(anchor) is String mismatch ? $", {mismatch}" : "")}");
        ShowStation(proof);
        ShowPrice(proof, statements);

        // c) It keeps them, with when charging began; the energy is the one of the meter values the energy meter signs, which
        //    a transparency software checks with its public key, not of this example.
        File.WriteAllText(Evidence, new JObject(
                                        new JProperty("trustAnchor",           anchor.ToQRCode()),
                                        new JProperty("EVSEId",                EVSEId.ToString()),
                                        new JProperty("chargingStarted",       started.ToISO8601()),
                                        new JProperty("chargingStationProof",  proof.ToJSON()),
                                        new JProperty("priceStatements",       new JArray(statements.Select(given => given.ToJSON())))
                                    ).ToString());

        Console.WriteLine($"Kept: {Evidence}, {new FileInfo(Evidence).Length / 1024} KiB");
        Console.WriteLine();

        #endregion

        #region 3. A week later: a commit, checkpoint 5, its witnesses

        // DE*GEF raises its DC tariff: the network changes, the evidence of the session does not.
        if (!A1.History.Head.RoamingNetwork.TryGetChargingStationOperatorById(ExampleA2.gefId, out var gef))
            Helpers.Fail($"QA has no charging station operator '{ExampleA2.gefId}'");

        ExampleA4.CommitWith(A1, tariffsKey, "The DC charging tariff: 0.72 €/kWh",
                             RoamingNetworkChange.UpdateProperty(gef.ChargingTariffs.Single(tariff => tariff.Id == ExampleA8.DC), "elements", null, new JArray(new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.72m, StepSize: 1) ]).ToJSON())));

        // Checkpoint 5 names the statement of the session, taken since checkpoint 4.
        var checkpoint5 = ExampleA4.MakeCheckpoint(A1);

        Console.WriteLine($"  naming {checkpoint5.PriceStatementsSize} price statement(s)");

        var named = A1.History.PriceStatementsOf(checkpoint5.Id);

        if (named is null)
            Helpers.Fail("the checkpoint names no price statements here");

        File.WriteAllText("published/QA-checkpoint-5-priceStatements.json", new JArray(named.Select(given => given.ToJSON())).ToString());

        ExampleA4.PublishCommitsSince(A1, checkpoint4);
        ExampleA4.Publish(A1);

        // The witnesses check the commit, the checkpoint and its statement, and sign it, as in example A9.
        var signatures = new List<(RoamingNetworkCheckpoint_Id CheckpointId, RoamingNetworkCommitSignature Signature)>();

        foreach (var (witness, history) in A6.Histories)
        {

            ExampleA6.AppendCommits(history, "published/QA-commits-since-checkpoint-4.json");

            if (!ExampleA6.TryTakeCheckpoint(history, 5, out var taken, out error, ExampleA9.Statements("published/QA-checkpoint-5-priceStatements.json")))
                Helpers.Fail(error);

            if (!history.TryWitness(taken.Id, ExampleA6.WitnessOf(A6, witness, A6.Keys[witness]), out _, out var signature, out error))
                Helpers.Fail(error);

            signatures.Add((taken.Id, signature));

        }

        ExampleA9.TakeSignatures(A1, signatures);

        Console.WriteLine();

        #endregion

        #region 4. The app completes its evidence

        // As GET ~/RNs/QA/history/commits/{commitId}/proof, the proof of the commit of the station proof in the first
        // checkpoint whose commits include it, checkpoint 4; as GET ~/RNs/QA/history/checkpoints/{checkpointId}/
        // priceStatements/{digest}/proof, the proof of the statement in checkpoint 5; and both checkpoints, with the
        // signatures of the witnesses.
        if (!A1.History.TryGetCommitProof(proof.CommitId!.Value, out var commitProof, out error))
            Helpers.Fail(error);

        if (!A1.History.TryGetPriceStatementProof(checkpoint5.Id, statement.Digest, out var statementProof, out error))
            Helpers.Fail(error);

        var evidence = Helpers.ReadJSON(Evidence);

        evidence.Add("commitProof",           commitProof.ToJSON());
        evidence.Add("priceStatementProofs",  new JArray(statementProof.ToJSON()));
        evidence.Add("checkpoints",           new JArray(Helpers.ReadJSON("published/QA-checkpoint-4.json"),
                                                         Helpers.ReadJSON("published/QA-checkpoint-5.json")));

        File.WriteAllText(Evidence, evidence.ToString());

        Console.WriteLine($"Completed: {Evidence}, {new FileInfo(Evidence).Length / 1024} KiB: the proof of revision {commitProof.Revision} in checkpoint {SequenceOf(A1, commitProof.CheckpointId)}, of the statement in checkpoint {SequenceOf(A1, statementProof.CheckpointId)}, and both checkpoints");
        Console.WriteLine();

        #endregion

    }


    /// <summary>
    /// Checks the evidence of the driver months later, offline, from the file the app kept and the witnesses it trusts only:
    /// the price that applied at DE*GEF*E1*1 when charging began.
    /// </summary>
    public static void Validate()
    {

        #region 5. The evidence, months later

        // a) What the app kept, and the witnesses it trusts.
        var evidence   = Helpers.ReadJSON(Evidence);
        var witnesses  = ExampleA6.TrustedWitnesses();

        if (!RoamingNetworkTrustAnchor.TryParse(evidence["trustAnchor"]!.Value<String>()!, out var anchor, out var error))
            Helpers.Fail(error);

        if (!RoamingNetworkProof.TryParse((JObject) evidence["chargingStationProof"]!, out var proof, out error))
            Helpers.Fail(error);

        if (!RoamingNetworkCheckpointProof.TryParse((JObject) evidence["commitProof"]!, out var commitProof, out error))
            Helpers.Fail(error);

        var started          = DateTimeOffset.Parse(evidence["chargingStarted"]!.Value<String>()!, CultureInfo.InvariantCulture);
        var monthsLater      = started.AddDays(90);
        var statements       = Statements((JArray) evidence["priceStatements"]!);
        var statementProofs  = new List<RoamingNetworkPriceStatementProof>();

        foreach (var json in ((JArray) evidence["priceStatementProofs"]!).Cast<JObject>())
        {

            if (!RoamingNetworkPriceStatementProof.TryParse(json, out var parsed, out error))
                Helpers.Fail(error);

            statementProofs.Add(parsed);

        }

        // The checkpoints signed by the quorum of OCC, its keys as the station proof gives them, and by both witnesses.
        var checkpoints      = ((JArray) evidence["checkpoints"]!).Cast<JObject>().Select(json => Witnessed(json, anchor, proof, witnesses, started)).ToDictionary(checkpoint => checkpoint.Id);

        Console.WriteLine();

        // b) As charging began: the proof attested by the quorum of OCC then, each statement signed by DE*GEF, valid then.
        //    Its attestations ended 15 minutes after they were signed: months later they prove nothing on their own.
        foreach (var given in statements)
            if (!given.TryVerify(proof, anchor.Keys, out _, out error, started, anchor.MinConsensus))
                Helpers.Fail(error);

        Console.WriteLine($"At {started.ToISO8601()}, when charging began: the station proof attested, the price statement valid");
        Console.WriteLine($"{monthsLater.ToISO8601()}, by its attestations alone: {(proof.Verify(anchor, out _, out error, monthsLater) ? "valid" : error)}");

        // c) The version of the station proof is one of the history, which the witnesses checked: its commit is one of a
        //    witnessed checkpoint, and the proof leads to the effectiveTree ETag of the commit.
        if (proof.CommitId != commitProof.CommitId || proof.Revision != commitProof.Revision)
            Helpers.Fail("the commit proof is not the one of the commit of the station proof");

        if (!checkpoints.TryGetValue(commitProof.CheckpointId, out var commitCheckpoint) || !commitProof.Verify(commitCheckpoint, out error))
            Helpers.Fail(error ?? "the checkpoint of the commit proof is not kept");

        if (!proof.Verify(commitProof.ETags.Single(eTag => eTag.Format == ETagFormat.EffectiveTree && eTag.HashAlgorithm == proof.HashAlgorithm), out error))
            Helpers.Fail(error);

        Console.WriteLine($"Revision {proof.Revision}, the version of the station proof: in checkpoint {commitCheckpoint.Sequence}, witnessed");

        // d) The statement of the EVSE was made after the checkpoint it names and before the one naming it, which follows it:
        //    no other statement of its sequence was shown to others, and it was not made afterwards.
        var statement       = statements.Single(given => given.IsOf(nameof(EVSE), evidence["EVSEId"]!.Value<String>()!));
        var statementProof  = statementProofs.Single(given => given.PriceStatement.Digest == statement.Digest);

        if (!checkpoints.TryGetValue(statementProof.CheckpointId, out var statementCheckpoint) || !statementProof.Verify(statementCheckpoint, out error))
            Helpers.Fail(error ?? "the checkpoint of the price statement proof is not kept");

        var afterCheckpoint = statement.After is RoamingNetworkCheckpoint_Id after ? checkpoints.GetValueOrDefault(after) : null;

        if (afterCheckpoint is null || statementCheckpoint.PreviousId != afterCheckpoint.Id)
            Helpers.Fail("the price statement names no kept checkpoint right before the one naming it");

        Console.WriteLine($"The price statement, sequence {statement.Sequence}: after checkpoint {afterCheckpoint.Sequence}, before checkpoint {statementCheckpoint.Sequence}, witnessed");
        Console.WriteLine();

        // e) What applied: at this charging station, its energy meter signing with this key, this price.
        ShowStation(proof);
        ShowPrice(proof, statements);
        Console.WriteLine();

        #endregion

        #region 6. What is refused

        // a) A statement of another price: its signature is not the one of its content.
        var changed = (JObject) evidence["priceStatements"]![0]!.DeepClone();
        changed["priceStatus"]!["prices"]![0]!["elements"]![0]!["priceComponents"]![0]!["price"] = 0.09m;

        Console.WriteLine($"A statement of another price: {(RoamingNetworkPriceStatement.TryParse(changed, out var changedStatement, out error) && changedStatement.TryVerify(proof, out error, started) ? "valid" : error)}");

        // b) A session claimed to have begun an hour earlier, before the statement was valid.
        Console.WriteLine($"Charging begun an hour earlier: {(statement.TryVerify(proof, out error, started.AddHours(-1)) ? "valid" : error)}");

        // c) A station proof of another power: it leads to another root than the commit.
        var other = (JObject) evidence["chargingStationProof"]!.DeepClone();
        var evse  = other.SelectTokens("$..EVSEs[?(@['@id'] == 'DE*GEF*E1*1')]").Cast<JObject>().Single();
        evse["maxPower"] = 300000;

        Console.WriteLine($"A station proof of 300 kW: {(RoamingNetworkProof.TryParse(other, out var otherProof, out error) && otherProof.Verify(commitProof.ETags.Single(eTag => eTag.Format == ETagFormat.EffectiveTree && eTag.HashAlgorithm == otherProof.HashAlgorithm), out error) ? "valid" : error)}");

        // d) A station proof naming the key of another energy meter, e.g. of meter values signed elsewhere: another root.
        using var otherMeter = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        var otherKey    = otherMeter.ExportSubjectPublicKeyInfo();
        var anotherKey  = (JObject) evidence["chargingStationProof"]!.DeepClone();
        var key         = anotherKey.SelectTokens("$..EVSEs[?(@['@id'] == 'DE*GEF*E1*1')].energyMeter.publicKeys[0]").Cast<JObject>().Single();
        key["@id"]      = Convert.ToHexStringLower(SHA256.HashData(otherKey));
        key["key"]      = Convert.ToBase64String(otherKey);

        Console.WriteLine($"A station proof naming another meter key: {(RoamingNetworkProof.TryParse(anotherKey, out otherProof, out error) && otherProof.Verify(commitProof.ETags.Single(eTag => eTag.Format == ETagFormat.EffectiveTree && eTag.HashAlgorithm == otherProof.HashAlgorithm), out error) ? "valid" : error)}");

        // e) Checkpoint 5 as it was before its witnesses signed it.
        var unwitnessed = (JObject) evidence["checkpoints"]![1]!.DeepClone();

        foreach (var signature in ((JArray) unwitnessed["signatures"]!).Where(signature => signature["role"]?.Value<String>() == RoamingNetworkCommitSignatureRoles.Witness).ToArray())
            signature.Remove();

        Console.WriteLine($"Checkpoint 5 without its witnesses: {(RoamingNetworkCheckpoint.TryParse(unwitnessed, out var bare, out error) ? $"witnessed by {ExampleA6.WitnessedBy(bare, witnesses).Count} of {witnesses.Count}" : error)}");

        #endregion

    }


    #region Helpers

    // The charging station of the EVSE and the energy meter of the EVSE as the proof gives them, with their manufacturers:
    // the station a driver sees, and the key the signed meter values of the session are checked with.
    private static void ShowStation(RoamingNetworkProof Proof)
    {

        var gef      = Proof.Given(RoamingNetwork.PropertyKeys.ChargingStationOperators).Single(json => json["@id"]?.Value<String>() == ExampleA2.gefId.ToString());
        var station  = Proof.GivenBelow(gef, ChargingStationOperator.PropertyKeys.ChargingPools, ChargingPool.PropertyKeys.ChargingStations).Single(json => json[ChargingStation.PropertyKeys.EVSEs]?.Any(evse => evse["@id"]?.Value<String>() == EVSEId.ToString()) == true);
        var meter    = (JObject) station[ChargingStation.PropertyKeys.EVSEs]!.Single(evse => evse["@id"]?.Value<String>() == EVSEId.ToString())["energyMeter"]!;

        String NameOf(String ChildArray, JToken? Id)
            => Proof.Given(ChildArray).SingleOrDefault(json => json["@id"]?.Value<String>() == Id?.Value<String>())?["name"]?["en"]?.Value<String>() ?? "an unknown manufacturer";

        if (!EnergyMeterPublicKey.TryParse((JObject) meter["publicKeys"]![0]!, out var publicKey, out var error))
            Helpers.Fail(error);

        Console.WriteLine($"{station["@id"]}: {NameOf(RoamingNetwork.PropertyKeys.ChargingStationManufacturers, station["manufacturerId"])} {station["modelId"]}");
        Console.WriteLine($"{meter["@id"]}, the energy meter of {EVSEId}: {NameOf(RoamingNetwork.PropertyKeys.EnergyMeterManufacturers, meter["manufacturerId"])} {meter["modelId"]}, serial {meter["serialNumber"]}, {String.Join(", ", meter["signatureMethods"]!.Values<String>())}, {publicKey.Algorithm} key {publicKey.Id[..8]}…");

    }

    // The charging tariffs the EVSE takes as the proof gives them, and the price the statements set for each.
    private static void ShowPrice(RoamingNetworkProof Proof, IEnumerable<RoamingNetworkPriceStatement> Statements)
    {

        var gef    = Proof.Given(RoamingNetwork.PropertyKeys.ChargingStationOperators).Single(json => json["@id"]?.Value<String>() == ExampleA2.gefId.ToString());
        var evse   = Proof.GivenBelow(gef, ChargingStationOperator.PropertyKeys.ChargingPools, ChargingPool.PropertyKeys.ChargingStations, ChargingStation.PropertyKeys.EVSEs).Single(json => json["@id"]?.Value<String>() == EVSEId.ToString());
        var tariff = Proof.GivenBelow(gef, ChargingStationOperator.PropertyKeys.ChargingTariffs).ToDictionary(json => json["@id"]!.Value<String>()!);

        foreach (var tariffId in evse[EVSE.PropertyKeys.ChargingTariffIds]!.Values<String>().OfType<String>())
        {

            var price = Statements.SelectMany(statement => statement.PriceStatus.Prices).FirstOrDefault(prices => prices.ChargingTariffId.ToString() == tariffId);

            Console.WriteLine($"{EVSEId}, {evse["maxPower"]!.Value<Decimal>() / 1000} kW: {tariffId} {tariff[tariffId]["elements"]![0]!["priceComponents"]![0]!["price"]!.Value<Decimal>().Euro()} €/kWh, by the price statement {(price is null ? "none" : $"{price.Elements[0].PriceComponents[0].Price.Euro()} €/kWh")}");

        }

    }

    // The checkpoint of the given JSON: of the roaming network of the trust anchor, signed as the roaming network operator by
    // signing keys of as many holders as the quorum requires, certified by trusted identity keys of their holders as the
    // station proof gives them, and by every witness the app trusts.
    private static RoamingNetworkCheckpoint Witnessed(JObject JSON, RoamingNetworkTrustAnchor Anchor, RoamingNetworkProof Proof, IReadOnlyDictionary<String, POIPublicKey> Witnesses, DateTimeOffset At)
    {

        if (!RoamingNetworkCheckpoint.TryParse(JSON, out var checkpoint, out var error))
            Helpers.Fail(error);

        if (checkpoint.RoamingNetworkId != Anchor.Id)
            Helpers.Fail($"the checkpoint is one of '{checkpoint.RoamingNetworkId}'");

        if (!Proof.TryGetKeyStatement(out var keyStatement, out var identityKeys, out error))
            Helpers.Fail(error);

        if (!Proof.TryGetSigningKeyCertificates(out var certificates, out error))
            Helpers.Fail(error);

        // The holders as the trust anchor names them, and the key statement must name them so.
        if (!keyStatement.TryVerify(Anchor.Holders, identityKeys, out var holderOf, out error, Anchor.MinConsensus, At))
            Helpers.Fail(error);

        var trustedIds    = holderOf.Keys.ToHashSet();
        var identityPool  = keyStatement.IdentityKeys.Concat(identityKeys).ToArray();

        // A signature counts for the holder of the identity key that certified its key, never as the signing key names itself.
        var signedBy = checkpoint.Signatures.Where (signature => signature.Role == RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator).
                                             Select(signature => certificates.FirstOrDefault(certificate => certificate.SigningKey.Id == signature.KeyId         &&
                                                                                                            certificate.SigningKey.IsValidAt(signature.SignedAt) &&
                                                                                                            certificate.SigningKey.MaySign(POIKeyUsages.Party)   &&
                                                                                                            certificate.TryVerify(identityPool, out _, out _, trustedIds)) is RoamingNetworkSigningKeyCertificate certificate &&
                                                                 checkpoint.VerifySignature(signature, certificate.SigningKey.Key, certificate.SigningKey.Id, out _)
                                                                     ? holderOf[certificate.KeyId]
                                                                     : null).
                                             OfType<String>().
                                             ToHashSet();

        if (signedBy.Count < Math.Max(Anchor.MinConsensus, keyStatement.MinConsensus))
            Helpers.Fail($"the checkpoint {checkpoint.Sequence} is signed by {signedBy.Count} holder(s) of the roaming network operator only");

        var witnessed = ExampleA6.WitnessedBy(checkpoint, Witnesses);

        if (witnessed.Count < Witnesses.Count)
            Helpers.Fail($"the checkpoint {checkpoint.Sequence} is witnessed by {witnessed.Count} of the {Witnesses.Count} witnesses only");

        Console.WriteLine($"Checkpoint {checkpoint.Sequence}: revision {checkpoint.Revision}, signed by {String.Join(", ", signedBy.Order())}, witnessed by {String.Join(", ", witnessed)}");

        return checkpoint;

    }

    // The price statements of the given JSON array.
    private static IReadOnlyList<RoamingNetworkPriceStatement> Statements(JArray JSON)
    {

        var statements = new List<RoamingNetworkPriceStatement>();

        foreach (var json in JSON.Cast<JObject>())
        {

            if (!RoamingNetworkPriceStatement.TryParse(json, out var statement, out var error))
                Helpers.Fail(error);

            statements.Add(statement);

        }

        return statements;

    }

    // The sequence of the checkpoint of the given @id.
    private static UInt64 SequenceOf(ExampleA1.Result A1, RoamingNetworkCheckpoint_Id CheckpointId)

        => A1.History.Checkpoints.Single(checkpoint => checkpoint.Id == CheckpointId).Sequence;

    #endregion

}
