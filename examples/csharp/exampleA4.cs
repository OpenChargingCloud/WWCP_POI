using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;

using cloud.charging.open.protocols.WWCP;
using cloud.charging.open.protocols.WWCP.POI;


public static class ExampleA4
{

    // The roaming network operator "OCC" makes the first checkpoint of "QA", of its revision 7: a signed statement that
    // this commit and every commit before it are the history of QA. Then "DE*GEF" adds two pools more, after the pattern
    // of example A3; their AC stations are recorded as those of A3, with the AC template and EVSEs of 22 kW, though they
    // have 11 kW only. Example A5 corrects them.


    public static void Run(ExampleA1.Result A1, ExampleA2.Result A2)
    {

        #region 1. The first checkpoint

        // The roaming network operator signs its checkpoints as it signs its commits: with the signing keys of AlphaOrg,
        // BetaOrg and GammaOrg. A server of the RNO makes one when there are new commits and the last one is older than an
        // interval, e.g. one hour; here it is made by hand.
        A1.History.CheckpointSigners = [.. A1.Signers];

        var checkpoint0 = MakeCheckpoint(A1);

        // The proof that the commit of revision 4, the one adding the pool "Jena Paradies", is one of its commits.
        var revision4   = CommitOf(A1, 4);

        if (!A1.History.TryGetCommitProof(revision4.Id, out var proof, out var error))
            Helpers.Fail(error);

        File.WriteAllText("published/QA-proof-revision-4.json", proof.ToJSON().ToString());

        Console.WriteLine($"The commit of revision 4 is the commit {proof.Index} of the {proof.Size} of checkpoint {checkpoint0.Sequence}");
        Console.WriteLine();

        #endregion

        #region 2. Two pools more, after the pattern of A3

        // As in A3: pool n has the DC station 2n-1 and the AC station 2n, two EVSEs each. Their AC stations take the AC
        // template, 44 kW, and their EVSEs 22 kW each, as in A3: wrong, they have two EVSEs of 11 kW, 22 kW together.
        Commit(A1, A2, "Jena Lobeda, a DC and an AC station",   RoamingNetworkChange.Add(ExampleA3.Pool(3, "Jena Lobeda",   50.8858, 11.6157, ACkW: 22)));
        Commit(A1, A2, "Jena Winzerla, a DC and an AC station", RoamingNetworkChange.Add(ExampleA3.Pool(4, "Jena Winzerla", 50.9105, 11.5555, ACkW: 22)));

        // A commit after the latest checkpoint is part of none yet: there is no proof of it.
        if (A1.History.TryGetCommitProof(A1.History.Head.Commit.Id, out _, out var notYet))
            Helpers.Fail("a commit after the latest checkpoint has a proof");

        Console.WriteLine($"The proof of revision {A1.History.Head.Commit.Revision}: {notYet}");
        Console.WriteLine();

        // What everybody may read now: the roaming network with its commit, and the commits since the checkpoint.
        Publish(A1);
        PublishCommitsSince(A1, checkpoint0);

        #endregion

    }


    /// <summary>
    /// Joins QA as a server or an app does that has no history of it: from the published checkpoint, the version of its
    /// commit and the commits since, instead of all commits since the genesis; from what is published only.
    /// </summary>
    public static void Validate()
    {

        #region 3. Joining from the checkpoint

        // a) QA at its head, as everybody checks it: from the QR code, the roaming network and its commit. Its operator's
        //    signing keys and their certificates are the ones checked there.
        ExampleA1.Validate();

        var (anchor, network) = Checked();

        // b) The checkpoint: its @id is the digest of its content, TryParse calculates it and refuses it when it differs.
        //    It is signed as the roaming network operator by as many holders as the quorum requires.
        var checkpoint0       = CheckedCheckpoint("published/QA-checkpoint-0.json", anchor, network);

        // c) The version of its commit, from the snapshot of the checkpoint: it must lead to the ETags of the checkpoint,
        //    whose signatures are checked again. The new history starts its line of checkpoints with it.
        if (!RoamingNetworkHistory.TryFromCheckpointSnapshot(File.ReadAllText("published/QA-checkpoint-0-snapshot.json"),
                                                             out var joined,
                                                             out var error,
                                                             VerifyCheckpointSignature(anchor, network),
                                                             [ RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator ]))
            Helpers.Fail(error);

        Console.WriteLine($"Joined at revision {joined.Head.Commit.Revision}, the checkpoint's: {(joined.Head.Commit.Id == checkpoint0.CommitId ? "the same" : "another")}; checkpoints: {joined.Checkpoints.Count}");

        // d) The commits since, each checked as every commit is: its change set signed by its owners and leading to its
        //    ETags, the commit signed as the roaming network operator.
        foreach (var commit in CommitsSince(0))
            Append(joined, commit, anchor, network);

        // e) The head is the published one.
        if (!RoamingNetworkCommit.TryParse(Helpers.ReadJSON("published/QA-commit.json"), out var head, out error))
            Helpers.Fail(error);

        Console.WriteLine($"The head of the joined history and the published commit: {(joined.Head.Commit.Id == head.Id && joined.Head.Commit.ETags.SequenceEqual(head.ETags) ? "the same" : "others")}");
        Console.WriteLine();

        #endregion

        #region 4. The proof of a commit

        // The commit of revision 4 is one of the commits of the checkpoint: its leaf, its commit id, revision and ETags,
        // leads with the audit path to the root of the commits the checkpoint signs. The joined history does not hold this
        // commit, but whoever kept it, or a proof of a charging station of its version, shows with this proof that it is
        // part of the history the checkpoint states.
        if (!RoamingNetworkCheckpointProof.TryParse(Helpers.ReadJSON("published/QA-proof-revision-4.json"), out var proof, out error))
            Helpers.Fail(error);

        Console.WriteLine($"The proof of the commit of revision {proof.Revision} in checkpoint {checkpoint0.Sequence}: {(proof.Verify(checkpoint0, out error) ? "valid" : error)}");

        #endregion

        #region 5. What is refused

        // a) A checkpoint changed: its @id is no longer the digest of its content.
        var changedCheckpoint = Helpers.ReadJSON("published/QA-checkpoint-0.json");
        changedCheckpoint["revision"] = 5;

        Console.WriteLine($"A checkpoint of another revision: {(RoamingNetworkCheckpoint.TryParse(changedCheckpoint, out _, out error) ? "taken" : error)}");

        // b) A snapshot whose version is changed: it does not lead to the ETags of the checkpoint.
        var changedSnapshot   = Helpers.ReadJSON("published/QA-checkpoint-0-snapshot.json");
        changedSnapshot["roamingNetwork"]!["chargingStationOperators"]![0]!["chargingPools"]![0]!["name"]!["en"] = "Jena Paradies, moved";

        Console.WriteLine($"A version of the checkpoint changed: {(RoamingNetworkHistory.TryFromCheckpointSnapshot(changedSnapshot, out _, out error, VerifyCheckpointSignature(anchor, network), [ RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator ]) ? "taken" : error)}");

        // c) A proof of another position: it leads to another root.
        var changedProof      = Helpers.ReadJSON("published/QA-proof-revision-4.json");
        changedProof["@index"] = changedProof["@index"]!.Value<Int32>() + 1;

        Console.WriteLine($"A proof of another position: {(RoamingNetworkCheckpointProof.TryParse(changedProof, out var otherProof, out error) && otherProof.Verify(checkpoint0, out error) ? "valid" : error)}");
        Console.WriteLine();

        #endregion

        #region 6. The new pools, as they are at runtime

        // As recorded: the AC stations with 44 kW of their template, their EVSEs with 22 kW.
        ShowPools(joined.Head.RoamingNetwork, 3, 4);

        #endregion

    }


    #region Helpers, also of example A5

    // The commit of the given revision of the history of QA.
    internal static RoamingNetworkCommit CommitOf(ExampleA1.Result A1, UInt64 Revision)
    {

        if (!A1.History.TryGetCommitsSince(A1.History.Root.Id, out var commits, out var error))
            Helpers.Fail(error);

        return Revision == 0 ? A1.History.Root : commits.Single(commit => commit.Revision == Revision);

    }

    // The next checkpoint of the latest commit, and what everybody may read of it: the checkpoint and its snapshot, the
    // version of its commit.
    internal static RoamingNetworkCheckpoint MakeCheckpoint(ExampleA1.Result A1)
    {

        if (!A1.History.TryMakeCheckpoint(out var checkpoint, out var error))
            Helpers.Fail(error);

        if (!A1.History.TryGetCheckpointSnapshotJSON(checkpoint.Id, out var snapshot, out error))
            Helpers.Fail(error);

        File.WriteAllText($"published/QA-checkpoint-{checkpoint.Sequence}.json",           checkpoint.ToJSON().ToString());
        File.WriteAllText($"published/QA-checkpoint-{checkpoint.Sequence}-snapshot.json",  snapshot.          ToString());

        Console.WriteLine($"Checkpoint {checkpoint.Sequence} of revision {checkpoint.Revision}: {checkpoint.CommitsSize} commit(s), {checkpoint.Signatures.Length} signatures, {checkpoint.Id}");

        return checkpoint;

    }

    // The given changes, as a change set of the head signed by DE*GEF's key for its infrastructure, committed.
    internal static void Commit(ExampleA1.Result A1, ExampleA2.Result A2, String Description, params RoamingNetworkChange[] Changes)

        => CommitWith(A1, A2.SigningKeys[POIKeyUsages.Infrastructure], Description, Changes);

    // The given changes, as a change set of the head signed by the given key of DE*GEF, committed.
    internal static void CommitWith(ExampleA1.Result A1, COSEKeyPair Key, String Description, params RoamingNetworkChange[] Changes)
    {

        var key     = Key;

        var result  = A1.History.Commit(A1.History.Head.RoamingNetwork.
                                            CreateChangeSet(Changes, Description: ImmutableI18NString.Create(Languages.en, Description)).
                                            Sign(key.Private, POIPublicKey.From(key.Private).Id, key.Algorithm));

        if (!result.IsCommitted)
            Helpers.Fail(result.ErrorResponse ?? result.Status.ToString());

        Console.WriteLine($"{result.Status}, revision {result.Commit?.Revision}: {Description}");

    }

    // What everybody may read: the roaming network with its ETags, and the commit leading to it.
    internal static void Publish(ExampleA1.Result A1)
    {
        File.WriteAllText($"published/{A1.RoamingNetwork.Id}.json",         A1.RoamingNetwork.  ToJSON(IncludeETags: true).ToString());
        File.WriteAllText($"published/{A1.RoamingNetwork.Id}-commit.json",  A1.History.Head.Commit.ToJSON().                  ToString());
    }

    // The commits since the given checkpoint, in their order, as GET ~/RNs/QA/history/commits?since=… gives them.
    internal static void PublishCommitsSince(ExampleA1.Result A1, RoamingNetworkCheckpoint Checkpoint)
    {

        if (!A1.History.TryGetCommitsSince(Checkpoint.CommitId, out var commits, out var error))
            Helpers.Fail(error);

        File.WriteAllText($"published/QA-commits-since-checkpoint-{Checkpoint.Sequence}.json", new JArray(commits.Select(commit => commit.ToJSON())).ToString());

    }

    // The published commits since the given checkpoint.
    internal static IEnumerable<RoamingNetworkCommit> CommitsSince(UInt64 Sequence)
    {
        foreach (var json in Helpers.ReadJSONArray($"published/QA-commits-since-checkpoint-{Sequence}.json").Cast<JObject>())
        {

            if (!RoamingNetworkCommit.TryParse(json, out var commit, out var error))
                Helpers.Fail(error);

            yield return commit;

        }
    }

    // The trust anchor and the roaming network at its head, both checked by ExampleA1.Validate().
    internal static (RoamingNetworkTrustAnchor Anchor, RoamingNetwork Network) Checked()
    {

        if (!RoamingNetworkTrustAnchor.TryParse(File.ReadAllText("published/QA-trustAnchor.txt"), out var anchor, out var error))
            Helpers.Fail(error);

        if (!RoamingNetwork.TryParse(Helpers.ReadJSON("published/QA.json"), out var network, out error))
            Helpers.Fail(error);

        return (anchor, network);

    }

    // The certificate of the signing key of the roaming network operator a signature names, as example A1 checks those of a
    // commit: by a trusted identity key of its holder, the key valid when it signed, and one that may sign for the party.
    // The identity keys trusted are those of the trust anchor and those the key statement of the version names, once it is
    // verified against the trust anchor: a key statement nobody checked names no key to trust (csharp-review 19).
    private static RoamingNetworkSigningKeyCertificate? CertificateOf(RoamingNetworkTrustAnchor Anchor, RoamingNetwork Network, RoamingNetworkCommitSignature Signature)
    {

        var networkOperator = Network.Operator!;

        if (Signature.Role != RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator ||
            networkOperator.KeyStatement is not RoamingNetworkKeyStatement statement    ||
            !statement.TryVerify(Anchor.Holders, networkOperator.IdentityKeys, out var holderOf, out _, Anchor.MinConsensus))
        {
            return null;
        }

        var trustedIds    = holderOf.Keys.ToHashSet();
        var identityPool  = statement.IdentityKeys.Concat(networkOperator.IdentityKeys).ToArray();

        return networkOperator.SigningKeyCertificates.
                   FirstOrDefault(certificate => certificate.SigningKey.Id == Signature.KeyId         &&
                                                 certificate.SigningKey.IsValidAt(Signature.SignedAt) &&
                                                 certificate.SigningKey.MaySign(POIKeyUsages.Party)   &&
                                                 certificate.TryVerify(identityPool, out _, out _, trustedIds));

    }

    // The trust anchor a verifier following the history holds after the given version: once the key statement of the version
    // is verified against the given one, the one that statement states, its keys, their holders and its quorum; else the
    // given one. So it follows the key statement by succession, only through versions it checked (SPECIFICATION.md, 8.6).
    internal static RoamingNetworkTrustAnchor Followed(RoamingNetworkTrustAnchor Anchor, RoamingNetwork Network)

        => Network.Operator?.KeyStatement is RoamingNetworkKeyStatement statement &&
           statement.TryVerify(Anchor.Holders, Network.Operator.IdentityKeys, out _, out _, Anchor.MinConsensus)
               ? Network.CreateTrustAnchor(statement, Anchor.URLs)
               : Anchor;

    // The signing key of the roaming network operator a signature names, as example A1 checks those of a commit: certified
    // by a trusted identity key of its holder, valid when it signed, and one that may sign for the party.
    internal static POIPublicKey? SigningKeyOf(RoamingNetworkTrustAnchor Anchor, RoamingNetwork Network, RoamingNetworkCommitSignature Signature)

        => CertificateOf(Anchor, Network, Signature)?.SigningKey;

    // The holder of the signing key a signature names, as the verifier knows it: the one of the trusted identity key that
    // certified it, as the key statement names it once checked against the holders of the trust anchor, never as the
    // signing key names itself (SPECIFICATION.md, section 8.6).
    internal static String? HolderOf(RoamingNetworkTrustAnchor Anchor, RoamingNetwork Network, RoamingNetworkCommitSignature Signature)

        => CertificateOf(Anchor, Network, Signature) is RoamingNetworkSigningKeyCertificate certificate &&
           Network.Operator!.KeyStatement!.TryVerify(Anchor.Holders, Network.Operator.IdentityKeys, out var holderOf, out _, Anchor.MinConsensus)
               ? holderOf[certificate.KeyId]
               : null;

    // A verifier of the signatures of a checkpoint, by the signing keys of the roaming network operator.
    internal static VerifyCheckpointSignatureDelegate VerifyCheckpointSignature(RoamingNetworkTrustAnchor Anchor, RoamingNetwork Network)

        => (checkpoint, signature) => SigningKeyOf(Anchor, Network, signature) is POIPublicKey key &&
                                      checkpoint.VerifySignature(signature, key.Key, key.Id, out _);

    // The checkpoint of the given file: its @id the digest of its content, of QA, signed as the roaming network operator
    // by signing keys of as many holders as the quorum requires. Signatures of witnesses are checked by those trusting
    // them, see example A6.
    internal static RoamingNetworkCheckpoint CheckedCheckpoint(String File, RoamingNetworkTrustAnchor Anchor, RoamingNetwork Network)
    {

        if (!RoamingNetworkCheckpoint.TryParse(Helpers.ReadJSON(File), out var checkpoint, out var error))
            Helpers.Fail(error);

        if (checkpoint.RoamingNetworkId != Network.Id)
            Helpers.Fail($"the checkpoint is one of '{checkpoint.RoamingNetworkId}'");

        var signedBy = new HashSet<String>();

        foreach (var signature in checkpoint.Signatures.Where(signature => signature.Role == RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator))
        {

            var key = SigningKeyOf(Anchor, Network, signature);

            if (key is null)
                Helpers.Fail($"the key '{signature.KeyId}' is no signing key a trusted identity key of its holder certifies");

            if (!checkpoint.VerifySignature(signature, key.Key, key.Id, out error))
                Helpers.Fail(error);

            signedBy.Add(HolderOf(Anchor, Network, signature)!);

        }

        if (signedBy.Count < Math.Max(Anchor.MinConsensus, Network.Operator!.KeyStatement!.MinConsensus))
            Helpers.Fail($"the checkpoint is signed by {signedBy.Count} holder(s) of the roaming network operator only");

        Console.WriteLine($"Checkpoint {checkpoint.Sequence} of epoch {checkpoint.Epoch}: revision {checkpoint.Revision}, {checkpoint.CommitsSize} commit(s){(checkpoint.PreviousId is RoamingNetworkCheckpoint_Id previous ? $" since checkpoint {previous.ToString()[..8]}…" : " since the genesis")}, signed by {String.Join(", ", signedBy.Order())}");

        return checkpoint;

    }

    // Appends the given commit of the server of QA to the given history, checked as every commit is.
    internal static void Append(RoamingNetworkHistory History, RoamingNetworkCommit Commit, RoamingNetworkTrustAnchor Anchor, RoamingNetwork Network)
    {

        if (!History.TryAppend([ Commit ],
                               out _,
                               out var error,
                               VerifyCommitSignature:   (commit, signature) => SigningKeyOf(Anchor, Network, signature) is POIPublicKey key &&
                                                                               commit.VerifySignature(signature, key.Key, key.Id, out _),
                               RequiredSignatureRoles:  [ RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator ]))
            Helpers.Fail(error);

        Console.WriteLine($"Appended revision {Commit.Revision}: {Commit.ChangeSet?.Description.FirstText()}");

    }

    // The given pools of DE*GEF as they are at runtime: the AC stations with their power, their EVSEs with theirs.
    internal static void ShowPools(RoamingNetwork Network, params Int32[] Pools)
    {

        if (!Network.TryGetChargingStationOperatorById(ExampleA2.gefId, out var gef))
            Helpers.Fail($"QA has no charging station operator '{ExampleA2.gefId}'");

        var pools = gef.EffectiveChargingPools.Where(pool => Pools.Any(n => pool.Id.ToString() == $"DE*GEF*P{n}")).ToArray();

        if (pools.Length == 0)
            Console.WriteLine($"no pool {String.Join(" or ", Pools)} yet");

        foreach (var pool in pools)
        {

            Console.WriteLine($"{pool.Id}  {pool.Name.FirstText()}");

            foreach (var station in pool.ChargingStations)
                Console.WriteLine($"  {station.Id}  {station.MaxPower?.kW} kW: {String.Join(", ", station.EVSEs.Select(evse => $"{evse.Id} {evse.CurrentTypes} {evse.MaxPower?.kW} kW"))}");

        }

    }

    #endregion

}
