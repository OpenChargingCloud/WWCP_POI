/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Diagnostics;
using cloud.charging.open.protocols.WWCP.POI;
using NUnit.Framework;
using static WWCP_POI_Tests.Interoperability.InteropFixture;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture]
public sealed class PersistenceTests
{
    private sealed class ArchiveDirectory : IDisposable
    {
        internal readonly String DirectoryPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "history-test-" + Guid.NewGuid().ToString("N"));
        internal String ArchivePath => Path.Combine(DirectoryPath, "history.cbor");
        internal ArchiveDirectory() => Directory.CreateDirectory(DirectoryPath);
        public void Dispose()
        {
            // Only exact leaf files in this test's freshly created directory are removed.
            foreach (var file in Directory.EnumerateFiles(DirectoryPath)) File.Delete(file);
            Directory.Delete(DirectoryPath);
        }
    }

    [Test]
    public void Writer_lease_prevents_competing_writers_and_recovery_accepts_continuation()
    {
        using var directory = new ArchiveDirectory();
        RoamingNetworkCommit first;
        using (var history = RoamingNetworkHistory.CreatePersistent(directory.ArchivePath, Network()))
        {
            Assert.That(() => RoamingNetworkHistory.Open(directory.ArchivePath), Throws.TypeOf<IOException>());
            Assert.That(() => RoamingNetworkHistory.CreatePersistent(directory.ArchivePath, Network()), Throws.TypeOf<IOException>());
            first = history.PrepareCommit(history.Head.Id, Batch(history.Head.Snapshot, "persisted", Power("150 kW")));
            Assert.That(history.TryPublish(history.Head.Id, first, out var result), Is.True, result.Error);
        }
        using var restored = RoamingNetworkHistory.Open(directory.ArchivePath);
        Assert.That(restored.Head.Id, Is.EqualTo(first.Id));
        var second = restored.PrepareCommit(first.Id, Batch(restored.Head.Snapshot, "continued", Power("175 kW")));
        Assert.That(restored.TryPublish(first.Id, second, out var published), Is.True, published.Error);
        Assert.That(restored.TryPublish(first.Parents[0], first, out published), Is.True, published.Error);
        Assert.That(restored.Head.Id, Is.EqualTo(second.Id));
        restored.Dispose();
        using var again = RoamingNetworkHistory.Open(directory.ArchivePath);
        Assert.That(again.Head.Id, Is.EqualTo(second.Id));
    }

    [TestCase("BeforeTemporaryWrite")]
    [TestCase("TemporaryFileFlushed")]
    public void Failure_before_replacement_keeps_disk_and_memory_unchanged(String stageName)
    {
        var stage = Enum.Parse<ArchiveWriteStage>(stageName);
        using var directory = new ArchiveDirectory();
        using var history = RoamingNetworkHistory.CreatePersistent(directory.ArchivePath, Network());
        var before = File.ReadAllBytes(directory.ArchivePath); var root = history.Head.Id;
        var commit = history.PrepareCommit(root, Batch(history.Head.Snapshot, "failed", Power("150 kW")));
        history.ArchiveWriteObserver = reached => { if (reached == stage) throw new IOException("Injected archive failure"); };
        Assert.That(history.TryPublish(root, commit, out var result), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkHistoryOutcome.PersistenceFailure));
        Assert.That(history.Head.Id, Is.EqualTo(root)); Assert.That(history.Commits, Has.Length.EqualTo(1));
        Assert.That(File.ReadAllBytes(directory.ArchivePath), Is.EqualTo(before));
        Assert.That(Directory.EnumerateFiles(directory.DirectoryPath, "*.tmp-*"), Is.Empty);
        history.ArchiveWriteObserver = null;
        Assert.That(history.TryPublish(root, commit, out result), Is.True, result.Error);
    }

    [TestCase("TemporaryFileFlushed", false)]
    [TestCase("ArchiveReplaced", true)]
    public async Task Abrupt_process_exit_recovers_exactly_the_old_or_new_archive(String stage, Boolean replaced)
    {
        using var directory = new ArchiveDirectory();
        RoamingNetworkCommit expected; RoamingNetworkCommitId root;
        using (var initial = RoamingNetworkHistory.CreatePersistent(directory.ArchivePath, Network()))
        {
            root = initial.Head.Id;
            expected = initial.PrepareCommit(root, Batch(initial.Head.Snapshot, "crash-change", Power("150 kW")));
        }
        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in new[] {"test", typeof(PersistenceTests).Assembly.Location, "--nologo", "--filter", "FullyQualifiedName~ArchiveCrashWorker"}) start.ArgumentList.Add(argument);
        start.Environment["WWCP_INTEROP_CRASH_PATH"] = directory.ArchivePath;
        start.Environment["WWCP_INTEROP_CRASH_STAGE"] = stage;
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        try { await process.WaitForExitAsync(timeout.Token); }
        finally { if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); } }
        var diagnostic = await output + "\n" + await error;
        Assert.That(process.ExitCode, Is.Not.Zero, diagnostic);
        Assert.That(File.Exists(directory.ArchivePath + ".stage"), Is.True, diagnostic);
        Assert.That(File.ReadAllText(directory.ArchivePath + ".stage"), Is.EqualTo(stage));
        using var recovered = RoamingNetworkHistory.Open(directory.ArchivePath);
        Assert.That(recovered.Head.Id, Is.EqualTo(replaced ? expected.Id : root));
        Assert.That(recovered.Head.Snapshot.Revision, Is.EqualTo(replaced ? 1 : 0));
        Assert.That(recovered.TryPublish(root, expected, out var result), Is.True, result.Error);
        Assert.That(result.Outcome, Is.EqualTo(replaced ? RoamingNetworkHistoryOutcome.AlreadyPublished : RoamingNetworkHistoryOutcome.Published));
        Assert.That(recovered.Head.Snapshot.ETags, Is.EqualTo(expected.StateETags));
    }

    [Test]
    public void ArchiveCrashWorker()
    {
        var path = Environment.GetEnvironmentVariable("WWCP_INTEROP_CRASH_PATH");
        if (path is null) Assert.Ignore("Executed only in a child process by the crash-recovery tests.");
        var stage = Enum.Parse<ArchiveWriteStage>(Environment.GetEnvironmentVariable("WWCP_INTEROP_CRASH_STAGE")!);
        using var history = RoamingNetworkHistory.Open(path!);
        var commit = history.PrepareCommit(history.Head.Id, Batch(history.Head.Snapshot, "crash-change", Power("150 kW")));
        history.ArchiveWriteObserver = reached => {
            if (reached != stage) return;
            File.WriteAllText(path + ".stage", reached.ToString());
            // Exit immediately: no archive finally block, history disposal or test cleanup runs.
            Environment.Exit(77);
        };
        history.TryPublish(history.Head.Id, commit, out var result);
        Assert.Fail("The requested exit stage was not reached: " + result.Error);
    }
}
