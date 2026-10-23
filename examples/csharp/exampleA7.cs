using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

using cloud.charging.open.protocols.WWCP.POI;


public static class ExampleA7
{

    // The roaming network operator compacts the history of QA twice, after its checkpoints: first to the commit of
    // checkpoint 0, revision 6, then to the one of checkpoint 1, revision 8. A history with checkpoints drops older commits
    // only before the commit of one of them, and keeps all checkpoints. It archives what it drops, one segment per
    // compaction, so that the checkpoints still prove what someone can show: the commits stay available, and so do their
    // proofs.


    public static void Run(ExampleA1.Result A1)
    {

        #region 1. The archive and what to keep

        // The archive keeps one segment per compaction in poi/QA-archive: the snapshot of the root before, and every commit
        // dropped after it, ending with the new root. Without an archive, the commits dropped would be gone.
        A1.History.Archive = new RoamingNetworkHistoryFileArchive($"poi/{A1.RoamingNetwork.Id}-archive");

        Console.WriteLine($"Before: root revision {A1.History.Root.Revision}, head revision {A1.History.Head.Commit.Revision}, {A1.History.Checkpoints.Count} checkpoints");

        #endregion

        #region 2. Compacting after the checkpoints

        // The commits of at least RetainCommits revisions behind the head are kept: the new root is the latest commit of a
        // checkpoint at least that far behind it, that every commit kept follows. A server of the RNO compacts itself once
        // its log has CompactAfterLogEntries entries; here it is done by hand.
        foreach (var retainCommits in new UInt64[] { 3, 1 })
        {

            A1.History.Retention = A1.History.Retention with { RetainCommits = retainCommits };

            if (!A1.History.Compact(out var compactError))
                Helpers.Fail(compactError);

            var segment = A1.History.ArchiveSegments[^1];

            Console.WriteLine($"Compacted, keeping {retainCommits} revision(s) behind the head: root revision {A1.History.Root.Revision}; segment {segment.Number} archives the revisions {segment.FromRevision} to {segment.ToRevision}, {segment.CommitIds.Length} commit(s) after its root");

        }

        Console.WriteLine($"After: root revision {A1.History.Root.Revision}, head revision {A1.History.Head.Commit.Revision}, {A1.History.Checkpoints.Count} checkpoints");

        #endregion

        #region 3. What the history gives after the compaction

        // a) Commits since a dropped one: none, a server knowing only older commits starts again from a snapshot.
        if (!RoamingNetworkHistory.TryFromSnapshot(File.ReadAllText("published/QA-genesis.json"), out var genesis, out var error))
            Helpers.Fail(error);

        Console.WriteLine($"Commits since the genesis: {(A1.History.TryGetCommitsSince(genesis.Root.Id, out _, out error) ? "given" : error)}");

        // b) A dropped commit from the archive, and its proof: the proof of revision 3 is the one of example A4.
        if (!RoamingNetworkCheckpointProof.TryParse(Helpers.ReadJSON("published/QA-proof-revision-3.json"), out var proof3, out error))
            Helpers.Fail(error);

        if (!A1.History.TryGetArchivedCommit(proof3.CommitId, out var archived))
            Helpers.Fail("the commit of revision 3 is not in the archive");

        if (!A1.History.TryGetCommitProof(archived.Id, out var proof, out error))
            Helpers.Fail(error);

        File.WriteAllText("published/QA-proof-revision-3-archived.json", proof.ToJSON().ToString());

        Console.WriteLine($"The commit of revision {archived.Revision} from the archive: {archived.ChangeSet?.Description.FirstText()}; its proof, as before the compaction: {(proof.ToJSON().ToString() == proof3.ToJSON().ToString() ? "the same" : "another")}");

        // c) After a restart: the store holds the snapshot of the new root as its base and the commits since, the
        //    checkpoints all; the archive the rest. Here copies of both are loaded, beside the store and the archive
        //    still open, each of which one history only may write.
        CopyDirectory($"poi/{A1.RoamingNetwork.Id}",          $"poi/restarted/{A1.RoamingNetwork.Id}");
        CopyDirectory($"poi/{A1.RoamingNetwork.Id}-archive",  $"poi/restarted/{A1.RoamingNetwork.Id}-archive");

        using var restartedStore    = new RoamingNetworkHistoryFileStore  ($"poi/restarted/{A1.RoamingNetwork.Id}");
        using var restartedArchive  = new RoamingNetworkHistoryFileArchive($"poi/restarted/{A1.RoamingNetwork.Id}-archive");

        if (!RoamingNetworkHistory.TryLoad(restartedStore, out var restarted, out error, Archive: restartedArchive))
            Helpers.Fail(error);

        if (!restarted.TryGetCommitProof(proof3.CommitId, out var restartedProof, out error))
            Helpers.Fail(error);

        Console.WriteLine($"Restarted: root revision {restarted.Root.Revision}, head revision {restarted.Head.Commit.Revision}, {restarted.Checkpoints.Count} checkpoints, the proof of revision 3 {(restartedProof.ToJSON().ToString() == proof3.ToJSON().ToString() ? "the same" : "another")}");
        Console.WriteLine();

        #endregion

        #region 4. What everybody may read of the archive

        // The list of the segments, as GET ~/RNs/QA/history/archive gives it, and each segment, as
        // GET ~/RNs/QA/history/archive/{number}: { info, base, commits }.
        File.WriteAllText("published/QA-archive.json", new JArray(A1.History.ArchiveSegments.Select(info => info.ToJSON())).ToString());

        foreach (var info in A1.History.ArchiveSegments)
        {

            if (!A1.History.TryGetArchiveSegment(info.Number, out var segment))
                Helpers.Fail($"the archive has no segment {info.Number}");

            File.WriteAllText($"published/QA-archive-{info.Number}.json", segment.ToJSON().ToString());

        }

        #endregion

    }


    /// <summary>
    /// Checks the archive of QA: its segments follow each other, each alone gives every version from its root to its end
    /// again, its end is the commit of a checkpoint with the ETags the checkpoint states, and its commits prove the commits of
    /// the checkpoint; from what is published only.
    /// </summary>
    public static void Validate()
    {

        #region 5. The segments

        // a) QA at its head, as everybody checks it, and its checkpoints, signed by the quorum of OCC.
        ExampleA1.Validate();

        var (anchor, network) = ExampleA4.Checked();

        RoamingNetworkCheckpoint[] checkpoints = [
            ExampleA4.CheckedCheckpoint("published/QA-checkpoint-0.json", anchor, network),
            ExampleA4.CheckedCheckpoint("published/QA-checkpoint-1.json", anchor, network),
            ExampleA4.CheckedCheckpoint("published/QA-checkpoint-2.json", anchor, network)
        ];

        Console.WriteLine();

        // b) The segments follow each other: the first one begins at the genesis, each other one at the end of the one
        //    before, and each ends with the commit of a checkpoint.
        if (!RoamingNetworkHistory.TryFromSnapshot(File.ReadAllText("published/QA-genesis.json"), out var genesis, out var error))
            Helpers.Fail(error);

        var infos = new List<RoamingNetworkArchiveSegmentInfo>();

        foreach (var json in Helpers.ReadJSONArray("published/QA-archive.json").Cast<JObject>())
        {

            if (!RoamingNetworkArchiveSegmentInfo.TryParse(json, out var info, out error))
                Helpers.Fail(error);

            var rootExpected = infos.Count == 0 ? genesis.Root.Id : infos[^1].EndId;

            if (info.RootId != rootExpected || info.Number != infos.Count + 1)
                Helpers.Fail($"the segment {info.Number} does not follow the one before");

            if (!checkpoints.Any(checkpoint => checkpoint.CommitId == info.EndId))
                Helpers.Fail($"the segment {info.Number} does not end with the commit of a checkpoint");

            infos.Add(info);

        }

        Console.WriteLine($"Segments: {String.Join(", ", infos.Select(info => $"{info.Number}: revisions {info.FromRevision} to {info.ToRevision}"))}, one after the other");
        Console.WriteLine();

        #endregion

        #region 6. Each segment on its own

        // Its base, the snapshot of its root, must lead to the ETags of its commit; its commits are taken each checked, as
        // every commit is. The version at its end is the one the checkpoint of its end states, and its commits, with its
        // root, are the commits of that checkpoint: their Merkle root is the one it signs.
        foreach (var info in infos)
        {

            var segment = Segment(info.Number);

            if (!RoamingNetworkHistory.TryFromSnapshot(segment.Base, out var history, out error))
                Helpers.Fail(error);

            Console.WriteLine($"Segment {info.Number}, from revision {history.Head.Commit.Revision}:");

            foreach (var commit in segment.Commits)
                ExampleA4.Append(history, commit, anchor, network);

            var checkpoint  = checkpoints.Single(checkpoint => checkpoint.CommitId == info.EndId);

            // The commits of a checkpoint: those since the checkpoint before, the root of the segment for the first one.
            var ofCheckpoint = RoamingNetworkCheckpoint.InOrder(checkpoint.Sequence == 0 ? [ history.Root, .. segment.Commits ] : segment.Commits);

            Console.WriteLine($"  its end and checkpoint {checkpoint.Sequence}: ETags {(checkpoint.ETags.SequenceEqual(history.Head.Commit.ETags) ? "the same" : "others")}, the root of its {ofCheckpoint.Count} commits {(RoamingNetworkCheckpoint.CommitsRootOf(ofCheckpoint, checkpoint.IdentificationHash) == checkpoint.CommitsRoot ? "the same" : "another")}");

            if (info.Number == infos[^1].Number)
                ExampleA4.ShowPools(history.Head.RoamingNetwork, 3, 4);

        }

        Console.WriteLine();

        #endregion

        #region 7. Proofs after the compaction, and what is refused

        // a) The proof of revision 3, given from the archive: valid in checkpoint 0.
        if (!RoamingNetworkCheckpointProof.TryParse(Helpers.ReadJSON("published/QA-proof-revision-3-archived.json"), out var proof, out error))
            Helpers.Fail(error);

        Console.WriteLine($"The proof of revision {proof.Revision}, from the archive, in checkpoint 0: {(proof.Verify(checkpoints[0], out error) ? "valid" : error)}");

        // b) A segment with a commit changed: its @id is no longer the digest of its content.
        var changed = Helpers.ReadJSON("published/QA-archive-2.json");
        changed["commits"]![0]!["revision"] = 17;

        Console.WriteLine($"A segment with a commit changed: {(RoamingNetworkCommit.TryParse((JObject) changed["commits"]![0]!, out _, out error) ? "taken" : error)}");

        // c) A segment with another base: it does not lead to the ETags of its root.
        var otherBase = Helpers.ReadJSON("published/QA-archive-2.json");
        otherBase["base"]!["roamingNetwork"]!["chargingStationOperators"]![0]!["chargingPools"]![0]!["name"]!["en"] = "Jena Paradies, moved";

        Console.WriteLine($"A segment with another base: {(RoamingNetworkHistory.TryFromSnapshot((JObject) otherBase["base"]!, out _, out error) ? "taken" : error)}");

        #endregion

    }


    #region (private static) CopyDirectory(From, To)

    // Copies the given directory, without the LOCK files the open store and archive hold.
    private static void CopyDirectory(String From, String To)
    {

        Directory.CreateDirectory(To);

        foreach (var file in Directory.GetFiles(From).Where(file => Path.GetFileName(file) != "LOCK"))
        {
            using var source = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var target = File.Create(Path.Combine(To, Path.GetFileName(file)));
            source.CopyTo(target);
        }

        foreach (var directory in Directory.GetDirectories(From))
            CopyDirectory(directory, Path.Combine(To, Path.GetFileName(directory)));

    }

    #endregion

    #region (private static) Segment(Number)

    // The published segment of the given number: its information, the snapshot of its root and its commits.
    private static RoamingNetworkArchiveSegment Segment(UInt32 Number)
    {

        var json = Helpers.ReadJSON($"published/QA-archive-{Number}.json");

        if (!RoamingNetworkArchiveSegmentInfo.TryParse((JObject) json["info"]!, out var info, out var error))
            Helpers.Fail(error);

        var commits = new List<RoamingNetworkCommit>();

        foreach (var commitJSON in ((JArray) json["commits"]!).Cast<JObject>())
        {

            if (!RoamingNetworkCommit.TryParse(commitJSON, out var commit, out error))
                Helpers.Fail(error);

            commits.Add(commit);

        }

        var segment = new RoamingNetworkArchiveSegment(info, (JObject) json["base"]!, commits);

        // Its commits are the ones its information names, in its order, and its base is of its root.
        if (!segment.Check(out error))
            Helpers.Fail(error);

        return segment;

    }

    #endregion

}
