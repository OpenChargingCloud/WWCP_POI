using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

using cloud.charging.open.protocols.WWCP.POI;


public static class ExampleA6
{

    // Two witnesses, independent of the roaming network operator, sign the three checkpoints of QA: a calibration
    // authority and an association of operators. Each keeps its own copy of the history of QA, taken commit by commit
    // since the genesis and each commit checked, and signs a checkpoint only after it checked it against that copy. The
    // roaming network operator takes their signatures and publishes the checkpoints with them; a verifier then requires
    // the signatures of the roaming network operator and of both witnesses.


    // The witnesses: the names they sign as, and their keys.
    internal static readonly String[] Witnesses = [ "calibration.example", "association.example" ];


    /// <summary>
    /// What the witnesses keep for the examples building on this one: their private keys, and their copies of the
    /// history of QA.
    /// </summary>
    /// <param name="Keys">The key of each witness.</param>
    /// <param name="Histories">The history of QA as each witness keeps it.</param>
    public sealed record Result(IReadOnlyDictionary<String, COSEKeyPair>            Keys,
                                IReadOnlyDictionary<String, RoamingNetworkHistory>  Histories);


    public static Result Run(ExampleA1.Result A1)
    {

        #region 1. The witnesses and their keys

        // Each witness makes its key itself; its public key is trusted by configuration, independently of QA, as the
        // keys of attestations are: here it is written to trusted/.
        var keys = new Dictionary<String, COSEKeyPair> {
                       { Witnesses[0], COSEKeyPair.NewEd25519() },
                       { Witnesses[1], COSEKeyPair.NewES384()   }
                   };

        Directory.CreateDirectory("trusted");

        File.WriteAllText("trusted/witnesses.json",
                          new JArray(keys.Select(witness => new JObject(new JProperty("witness", witness.Key),
                                                                        new JProperty("key",     POIPublicKey.From(witness.Value.Private).ToJSON())))).ToString());

        #endregion

        #region 2. What the roaming network operator publishes for them

        // The commits since the genesis, as GET ~/RNs/QA/history/commits?since={genesis} gives them. The genesis snapshot
        // is the one example A1 published, the checkpoints those of examples A4 and A5.
        if (!A1.History.TryGetCommitsSince(A1.History.Root.Id, out var commits, out var error))
            Helpers.Fail(error);

        File.WriteAllText("published/QA-commits-since-genesis.json", new JArray(commits.Select(commit => commit.ToJSON())).ToString());

        #endregion

        #region 3. Each witness checks QA and signs its checkpoints

        var signatures = new List<(RoamingNetworkCheckpoint_Id CheckpointId, RoamingNetworkCommitSignature Signature)>();
        var histories  = new Dictionary<String, RoamingNetworkHistory>();

        foreach (var (witness, key) in keys)
        {

            var history = WitnessHistory(witness);
            var signer  = SignerOf(witness, key);

            histories.Add(witness, history);

            foreach (var checkpoint in history.Checkpoints)
            {

                if (!history.TryWitness(checkpoint.Id, signer, out _, out var signature, out error))
                    Helpers.Fail(error);

                signatures.Add((checkpoint.Id, signature));

                Console.WriteLine($"{witness} signs checkpoint {checkpoint.Sequence}");

            }

        }

        Console.WriteLine();

        #endregion

        #region 4. The roaming network operator takes their signatures

        // The witnesses send their signatures to the server that made the checkpoints, e.g. POST
        // ~/RNs/QA/history/checkpoints/{checkpointId}/signatures. It takes those of the witnesses it trusts; they do not
        // change the identification of a checkpoint.
        var verify = VerifyCheckpointSignature(A1.TrustAnchor, A1.RoamingNetwork, TrustedWitnesses());

        foreach (var group in signatures.GroupBy(signature => signature.CheckpointId))
        {

            if (!A1.History.TryAddCheckpointSignatures(group.Key, [.. group.Select(signature => signature.Signature)], verify, out var signed, out var newSignatures, out error))
                Helpers.Fail(error);

            if (!A1.History.TryGetCheckpointSnapshotJSON(signed.Id, out var snapshot, out error))
                Helpers.Fail(error);

            File.WriteAllText($"published/QA-checkpoint-{signed.Sequence}.json",           signed.  ToJSON().ToString());
            File.WriteAllText($"published/QA-checkpoint-{signed.Sequence}-snapshot.json",  snapshot.ToString());

            Console.WriteLine($"Checkpoint {signed.Sequence}, {signed.Id.ToString()[..8]}…: {newSignatures.Count} signatures more, {signed.Signatures.Length} in all");

        }

        #endregion

        #region 5. A split view is refused

        // Another checkpoint of the sequence 2, of the same commit, made later: shown to some, it would let the operator
        // tell them another history than the one the witnesses saw. A witness takes no second checkpoint of a sequence, and
        // so never signs two.
        var latest     = A1.History.LatestCheckpoint!;
        var other      = RoamingNetworkCheckpoint.Create(latest.RoamingNetworkId,
                                                         latest.Epoch,
                                                         latest.Sequence,
                                                         latest.PreviousId,
                                                         A1.History.Head.Commit,
                                                         [ A1.History.Head.Commit ],
                                                         latest.CreatedAt.AddMinutes(1));

        other          = other.WithSignatures(A1.Signers.Select(signer => signer.Sign(other)));

        var calibration = WitnessHistory(Witnesses[0]);

        Console.WriteLine($"Another checkpoint {other.Sequence}, {other.Id.ToString()[..8]}…, at {Witnesses[0]}: {(calibration.TryAddCheckpoint(other, out error, ExampleA4.VerifyCheckpointSignature(A1.TrustAnchor, A1.RoamingNetwork), [ RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator ]) ? "taken" : error)}");
        Console.WriteLine();

        #endregion

        return new (keys, histories);

    }


    /// <summary>
    /// Checks the checkpoints of QA as a verifier requiring the roaming network operator and both witnesses, and the
    /// evidence they give: from what is published and the keys of the witnesses only.
    /// </summary>
    public static void Validate()
    {

        #region 6. Checkpoints witnessed

        // a) QA at its head, as everybody checks it, and the witnesses this verifier trusts.
        ExampleA1.Validate();

        var (anchor, network) = ExampleA4.Checked();
        var witnesses         = TrustedWitnesses();
        var verify            = VerifyCheckpointSignature(anchor, network, witnesses);
        String[] roles        = [ RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator, RoamingNetworkCommitSignatureRoles.Witness ];

        // b) Each checkpoint: signed by the quorum of the roaming network operator, as in A4, and by both witnesses.
        var checkpoints = new List<RoamingNetworkCheckpoint>();

        for (var sequence = 0; sequence <= 2; sequence++)
        {

            var checkpoint = ExampleA4.CheckedCheckpoint($"published/QA-checkpoint-{sequence}.json", anchor, network);
            var witnessed  = WitnessedBy(checkpoint, witnesses);

            if (witnessed.Count < Witnesses.Length)
                Helpers.Fail($"the checkpoint {checkpoint.Sequence} is witnessed by {witnessed.Count} of the {Witnesses.Length} witnesses only");

            Console.WriteLine($"  witnessed by {String.Join(", ", witnessed)}");

            checkpoints.Add(checkpoint);

        }

        Console.WriteLine();

        // c) Joining from the latest checkpoint, requiring both roles: every signature must be valid and of the roles
        //    required.
        if (!RoamingNetworkHistory.TryFromCheckpointSnapshot(File.ReadAllText("published/QA-checkpoint-2-snapshot.json"), out var joined, out var error, verify, roles))
            Helpers.Fail(error);

        Console.WriteLine($"Joined from checkpoint 2, signed by the roaming network operator and its witnesses: revision {joined.Head.Commit.Revision}");

        #endregion

        #region 7. Evidence

        // The proof of revision 8, the pool "Jena Lobeda" with its AC station as A4 recorded it, in checkpoint 1: kept,
        // e.g. by an e-mobility provider disputing an invoice of a session there, it shows without asking the server of QA
        // that this commit is part of the history both witnesses saw.
        if (!RoamingNetworkCheckpointProof.TryParse(Helpers.ReadJSON("published/QA-proof-revision-8.json"), out var proof, out error))
            Helpers.Fail(error);

        Console.WriteLine($"The proof of revision {proof.Revision} in checkpoint 1, witnessed by {String.Join(", ", WitnessedBy(checkpoints[1], witnesses))}: {(proof.Verify(checkpoints[1], out error) ? "valid" : error)}");
        Console.WriteLine();

        #endregion

        #region 8. What is refused

        // a) The snapshot of the checkpoint without the signatures of the witnesses: it is not signed in the role witness.
        var unwitnessed = Helpers.ReadJSON("published/QA-checkpoint-2-snapshot.json");
        var signatures  = (JArray) unwitnessed["checkpoint"]!["signatures"]!;

        foreach (var signature in signatures.Where(signature => signature["role"]?.Value<String>() == RoamingNetworkCommitSignatureRoles.Witness).ToArray())
            signature.Remove();

        Console.WriteLine($"Checkpoint 2 without its witnesses: {(RoamingNetworkHistory.TryFromCheckpointSnapshot(unwitnessed, out _, out error, verify, roles) ? "taken" : error)}");

        // b) A witness signature by a key nobody trusts, here one just made, naming a trusted witness.
        var stranger    = COSEKeyPair.NewEd25519();
        var forged      = checkpoints[2].CreateSignature(stranger.Private, POIPublicKey.From(stranger.Private).Id, stranger.Algorithm, RoamingNetworkCommitSignatureRoles.Witness, Witnesses[0]);

        Console.WriteLine($"A signature of {Witnesses[0]} by another key: {(verify(checkpoints[2], forged) ? "taken" : "refused, not the key trusted for it")}");

        #endregion

    }


    #region Helpers

    // The history of QA as the given witness keeps it: from the genesis snapshot, every commit since checked as every
    // commit is, and the checkpoints checked against it (SPECIFICATION.md, section 10.2).
    private static RoamingNetworkHistory WitnessHistory(String Witness)
    {

        // The trust anchor of QA, as scanned; the genesis version and its commit.
        if (!RoamingNetworkTrustAnchor.TryParse(File.ReadAllText("published/QA-trustAnchor.txt"), out var anchor, out var error))
            Helpers.Fail(error);

        if (!RoamingNetworkHistory.TryFromSnapshot(File.ReadAllText("published/QA-genesis.json"), out var history, out error))
            Helpers.Fail(error);

        var genesis  = history.Head.RoamingNetwork;

        if (genesis.Id != anchor.Id)
            Helpers.Fail("the genesis is not the one of the roaming network of the trust anchor");

        // Its key statement signed by enough holders, and the genesis commit by the quorum of the roaming network operator.
        if (!genesis.Operator!.KeyStatement!.TryVerify(anchor.Keys, genesis.Operator.IdentityKeys, out error, anchor.MinConsensus, At: Timestamp.Now))
            Helpers.Fail(error);

        var signedBy = history.Root.Signatures.Select(signature => ExampleA4.SigningKeyOf(anchor, genesis, signature) is POIPublicKey key &&
                                                                    history.Root.VerifySignature(signature, key.Key, key.Id, out _)
                                                                        ? key.HolderOrUnnamed
                                                                        : null).
                                               OfType<String>().
                                               ToHashSet();

        if (signedBy.Count < Math.Max(anchor.MinConsensus, genesis.Operator.KeyStatement.MinConsensus))
            Helpers.Fail("the genesis commit is not signed by the quorum of the roaming network operator");

        // Every commit since, and then every checkpoint: its commit held, with its revision and ETags, its commits those
        // since the checkpoint before, with the root it signs, signed by the roaming network operator.
        AppendCommits(history, "published/QA-commits-since-genesis.json");

        for (UInt64 sequence = 0; File.Exists($"published/QA-checkpoint-{sequence}.json"); sequence++)
            if (!TryTakeCheckpoint(history, sequence, out _, out error))
                Helpers.Fail(error);

        Console.WriteLine($"{Witness}: the genesis and {history.Head.Commit.Revision} commits checked, {history.Checkpoints.Count} checkpoints taken");

        return history;

    }

    // The witness signs as itself, in the role witness, with its own key.
    internal static RoamingNetworkCommitSigner SignerOf(String Witness, COSEKeyPair Key)

        => new (Key.Private,
                POIPublicKey.From(Key.Private).Id,
                Key.Algorithm,
                RoamingNetworkCommitSignatureRoles.Witness,
                Witness);

    // The trust anchor of QA, as scanned.
    private static RoamingNetworkTrustAnchor Anchor()
    {

        if (!RoamingNetworkTrustAnchor.TryParse(File.ReadAllText("published/QA-trustAnchor.txt"), out var anchor, out var error))
            Helpers.Fail(error);

        return anchor;

    }

    // The commits of the given file, appended to the history of a witness, each checked as every commit is: its change set
    // signed by its owners and leading to its ETags, the commit signed as the roaming network operator by its signing keys
    // in the version the witness holds.
    internal static void AppendCommits(RoamingNetworkHistory History, String File)
    {

        var anchor = Anchor();

        foreach (var json in Helpers.ReadJSONArray(File).Cast<JObject>())
        {

            if (!RoamingNetworkCommit.TryParse(json, out var commit, out var error))
                Helpers.Fail(error);

            if (!History.TryAppend([ commit ],
                                   out _,
                                   out error,
                                   VerifyCommitSignature:   (c, signature) => ExampleA4.SigningKeyOf(anchor, History.Head.RoamingNetwork, signature) is POIPublicKey key &&
                                                                              c.VerifySignature(signature, key.Key, key.Id, out _),
                                   RequiredSignatureRoles:  [ RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator ]))
                Helpers.Fail(error);

        }

    }

    // The published checkpoint of the given sequence, taken by the history of a witness after it checked it against it,
    // signed by the roaming network operator; with the price statements it names, when given, checked as well.
    internal static Boolean TryTakeCheckpoint(RoamingNetworkHistory                       History,
                                              UInt64                                      Sequence,
                                              out RoamingNetworkCheckpoint?               Checkpoint,
                                              out String?                                 ErrorResponse,
                                              IEnumerable<RoamingNetworkPriceStatement>?  PriceStatements   = null)
    {

        // Only the signatures of the roaming network operator: those of witnesses are not checked here.
        var json = Helpers.ReadJSON($"published/QA-checkpoint-{Sequence}.json");

        foreach (var signature in ((JArray) json["signatures"]!).Where(signature => signature["role"]?.Value<String>() == RoamingNetworkCommitSignatureRoles.Witness).ToArray())
            signature.Remove();

        if (!RoamingNetworkCheckpoint.TryParse(json, out Checkpoint, out ErrorResponse))
            return false;

        return History.TryAddCheckpoint(Checkpoint,
                                        out ErrorResponse,
                                        ExampleA4.VerifyCheckpointSignature(Anchor(), History.Head.RoamingNetwork),
                                        [ RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator ],
                                        PriceStatements);

    }

    // The witnesses a verifier trusts, by configuration: their names and keys.
    internal static IReadOnlyDictionary<String, POIPublicKey> TrustedWitnesses()
    {

        var witnesses = new Dictionary<String, POIPublicKey>();

        foreach (var json in Helpers.ReadJSONArray("trusted/witnesses.json").Cast<JObject>())
        {

            if (!POIPublicKey.TryParse((JObject) json["key"]!, out var key, out var error))
                Helpers.Fail(error);

            witnesses.Add(json["witness"]!.Value<String>()!, key);

        }

        return witnesses;

    }

    // The trusted witnesses whose valid signatures the given checkpoint carries.
    internal static IReadOnlyList<String> WitnessedBy(RoamingNetworkCheckpoint Checkpoint, IReadOnlyDictionary<String, POIPublicKey> Witnesses)

        => [.. Checkpoint.Signatures.Where (signature => signature.Role == RoamingNetworkCommitSignatureRoles.Witness &&
                                                         signature.Signer is String witness &&
                                                         Witnesses.TryGetValue(witness, out var key) &&
                                                         Checkpoint.VerifySignature(signature, key.Key, key.Id, out _)).
                                     Select(signature => signature.Signer!).
                                     Distinct()];

    // A verifier of the signatures of a checkpoint: of the roaming network operator by its signing keys, of a witness by
    // the key a verifier trusts for it.
    internal static VerifyCheckpointSignatureDelegate VerifyCheckpointSignature(RoamingNetworkTrustAnchor Anchor, RoamingNetwork Network, IReadOnlyDictionary<String, POIPublicKey> Witnesses)

        => (checkpoint, signature) => signature.Role == RoamingNetworkCommitSignatureRoles.Witness
                                          ? signature.Signer is String witness &&
                                            Witnesses.TryGetValue(witness, out var key) &&
                                            checkpoint.VerifySignature(signature, key.Key, key.Id, out _)
                                          : ExampleA4.VerifyCheckpointSignature(Anchor, Network)(checkpoint, signature);

    #endregion

}
