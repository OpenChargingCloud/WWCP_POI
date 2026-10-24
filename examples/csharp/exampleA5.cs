using cloud.charging.open.protocols.WWCP;
using cloud.charging.open.protocols.WWCP.POI;


public static class ExampleA5
{

    // The roaming network operator makes the next checkpoint, of revision 9, with the AC stations of the pools 3 and 4 as
    // example A4 recorded them; then DE*GEF corrects them, 22 kW each with two EVSEs of 11 kW, and the roaming network
    // operator makes the checkpoint after, of revision 10. The data before the correction stays what the checkpoint of
    // revision 9 states.


    public static void Run(ExampleA1.Result A1, ExampleA2.Result A2)
    {

        #region 1. The second checkpoint: before the correction

        // It names the first one as the one before, and its commits are those since: the revisions 8 and 9.
        var checkpoint1  = ExampleA4.MakeCheckpoint(A1);

        if (!A1.History.TryGetCommitProof(ExampleA4.CommitOf(A1, 8).Id, out var proof8, out var error))
            Helpers.Fail(error);

        File.WriteAllText("published/QA-proof-revision-8.json", proof8.ToJSON().ToString());

        #endregion

        #region 2. The correction

        // The AC stations 6 and 8 have 22 kW of their own, in place of the 44 kW of their template, and their EVSEs 11 kW
        // each: one change set, so that no version has a station of more power than its EVSEs together.
        if (!A1.History.Head.RoamingNetwork.TryGetChargingStationOperatorById(ExampleA2.gefId, out var gef))
            Helpers.Fail($"QA has no charging station operator '{ExampleA2.gefId}'");

        var stations     = gef.ChargingPools.SelectMany(pool => pool.ChargingStations).
                                             Where     (station => station.Id.ToString() is "DE*GEF*S6" or "DE*GEF*S8").
                                             ToArray();

        ExampleA4.Commit(A1, A2, "The AC stations of Jena Lobeda and Jena Winzerla: 22 kW, two EVSEs of 11 kW",
                         [
                             .. stations.Select      (station => RoamingNetworkChange.UpdateProperty(station, "maxPower", null,  22000)),
                             .. stations.SelectMany  (station => station.EVSEs).
                                         Select      (evse    => RoamingNetworkChange.UpdateProperty(evse,    "maxPower", 22000, 11000))
                         ]);

        ExampleA4.Publish(A1);
        ExampleA4.PublishCommitsSince(A1, checkpoint1);

        #endregion

        #region 3. The third checkpoint: after the correction

        var checkpoint2  = ExampleA4.MakeCheckpoint(A1);

        if (!A1.History.TryGetCommitProof(A1.History.Head.Commit.Id, out var proof10, out error))
            Helpers.Fail(error);

        File.WriteAllText("published/QA-proof-revision-10.json", proof10.ToJSON().ToString());

        Console.WriteLine();

        #endregion

    }


    /// <summary>
    /// Checks the line of checkpoints of QA and the AC stations of DE*GEF at every checkpoint: from the first checkpoint
    /// commit by commit, and from each checkpoint on its own; from what is published only.
    /// </summary>
    public static void Validate()
    {

        #region 4. The line of checkpoints

        // a) QA at its head, as everybody checks it.
        ExampleA1.Validate();

        var (anchor, network) = ExampleA4.Checked();
        var verify            = ExampleA4.VerifyCheckpointSignature(anchor, network);
        String[] roles        = [ RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator ];

        // b) The checkpoints, each signed by the quorum; each one names the one before, and its sequence is one more.
        RoamingNetworkCheckpoint[] checkpoints = [
            ExampleA4.CheckedCheckpoint("published/QA-checkpoint-0.json", anchor, network),
            ExampleA4.CheckedCheckpoint("published/QA-checkpoint-1.json", anchor, network),
            ExampleA4.CheckedCheckpoint("published/QA-checkpoint-2.json", anchor, network)
        ];

        for (var i = 1; i < checkpoints.Length; i++)
            if (checkpoints[i].PreviousId != checkpoints[i - 1].Id || checkpoints[i].Sequence != checkpoints[i - 1].Sequence + 1 || checkpoints[i].Epoch != checkpoints[i - 1].Epoch)
                Helpers.Fail($"the checkpoint {checkpoints[i].Sequence} does not follow the one before");

        Console.WriteLine($"One line: {String.Join(" <- ", checkpoints.Select(checkpoint => $"{checkpoint.Sequence} (revision {checkpoint.Revision})"))}");
        Console.WriteLine();

        #endregion

        #region 5. From the first checkpoint, commit by commit

        // The joined history takes each commit as it comes, and each checkpoint where its commit is reached: it refuses it
        // unless its commit is that one, with its revision and ETags, its commits are those since the checkpoint before,
        // with the root it signs, and it names the checkpoint before. So the version at each checkpoint is the one it states.
        if (!RoamingNetworkHistory.TryFromCheckpointSnapshot(File.ReadAllText("published/QA-checkpoint-0-snapshot.json"), out var stepwise, out var error, verify, roles))
            Helpers.Fail(error);

        Console.WriteLine($"Revision {stepwise.Head.Commit.Revision}, checkpoint 0:");
        ExampleA4.ShowPools(stepwise.Head.RoamingNetwork, 3, 4);

        foreach (var commit in ExampleA4.CommitsSince(0).Concat(ExampleA4.CommitsSince(1)))
        {

            ExampleA4.Append(stepwise, commit, anchor, network);

            foreach (var checkpoint in checkpoints.Where(checkpoint => checkpoint.Revision == commit.Revision))
            {

                if (!stepwise.TryAddCheckpoint(checkpoint, out error, verify, roles))
                    Helpers.Fail(error);

                Console.WriteLine($"Revision {commit.Revision}, checkpoint {checkpoint.Sequence}, its ETags and those of the version reached: {(checkpoint.ETags.SequenceEqual(stepwise.Head.Commit.ETags) ? "the same" : "others")}");

            }

            ExampleA4.ShowPools(stepwise.Head.RoamingNetwork, 3, 4);

        }

        Console.WriteLine($"Checkpoints taken: {stepwise.Checkpoints.Count}");
        Console.WriteLine();

        #endregion

        #region 6. From each checkpoint on its own

        // A server joining later starts from the latest checkpoint, one asking about a past time from the one of that
        // time: the version of each snapshot is the one reached commit by commit, with the AC stations as they were then.
        foreach (var checkpoint in checkpoints.Skip(1))
        {

            if (!RoamingNetworkHistory.TryFromCheckpointSnapshot(File.ReadAllText($"published/QA-checkpoint-{checkpoint.Sequence}-snapshot.json"), out var joined, out error, verify, roles))
                Helpers.Fail(error);

            if (!stepwise.TryGetCheckpoint(checkpoint.Id, out var reached) || !reached.ETags.SequenceEqual(joined.Head.Commit.ETags))
                Helpers.Fail($"the snapshot of checkpoint {checkpoint.Sequence} is another version than the one reached");

            Console.WriteLine($"Joined from checkpoint {checkpoint.Sequence}, revision {joined.Head.Commit.Revision}, and the version reached commit by commit: the same");
            ExampleA4.ShowPools(joined.Head.RoamingNetwork, 3, 4);

        }

        Console.WriteLine();

        #endregion

        #region 7. The proofs, and what is refused

        // a) The commits of revisions 4, 8 and 10 in the checkpoints whose commits include them.
        foreach (var (revision, sequence) in new[] { (4, 0), (8, 1), (10, 2) })
        {

            if (!RoamingNetworkCheckpointProof.TryParse(Helpers.ReadJSON($"published/QA-proof-revision-{revision}.json"), out var proof, out error))
                Helpers.Fail(error);

            Console.WriteLine($"The proof of revision {proof.Revision} in checkpoint {sequence}: {(proof.Verify(checkpoints[sequence], out error) ? "valid" : error)}");

        }

        // b) The proof of the correction against the checkpoint before it: the correction is no commit of it.
        if (!RoamingNetworkCheckpointProof.TryParse(Helpers.ReadJSON("published/QA-proof-revision-10.json"), out var proof10, out error))
            Helpers.Fail(error);

        Console.WriteLine($"The proof of revision 10 in checkpoint 1: {(proof10.Verify(checkpoints[1], out error) ? "valid" : error)}");

        // c) A checkpoint skipping the one before: joined from checkpoint 0 and at revision 10, checkpoint 2 is refused
        //    without checkpoint 1.
        if (!RoamingNetworkHistory.TryFromCheckpointSnapshot(File.ReadAllText("published/QA-checkpoint-0-snapshot.json"), out var skipping, out error, verify, roles))
            Helpers.Fail(error);

        foreach (var commit in ExampleA4.CommitsSince(0).Concat(ExampleA4.CommitsSince(1)))
            if (!skipping.TryAppend([ commit ], out _, out error, VerifyCommitSignature: (c, signature) => ExampleA4.SigningKeyOf(anchor, network, signature) is POIPublicKey key && c.VerifySignature(signature, key.Key, key.Id, out _), RequiredSignatureRoles: roles))
                Helpers.Fail(error);

        Console.WriteLine($"Checkpoint 2 without checkpoint 1: {(skipping.TryAddCheckpoint(checkpoints[2], out error, verify, roles) ? "taken" : error)}");

        #endregion

    }

}
