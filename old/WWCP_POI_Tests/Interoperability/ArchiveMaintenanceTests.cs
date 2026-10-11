/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using cloud.charging.open.protocols.WWCP.POI;
using NUnit.Framework;
using org.GraphDefined.Vanaheimr.Illias;
using static WWCP_POI_Tests.Interoperability.InteropFixture;
using static WWCP_POI_Tests.Interoperability.ReplicationTestSupport;
using static WWCP_POI_Tests.Interoperability.SnapshotTestSupport;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture]
public sealed class ArchiveMaintenanceTests
{
    private static readonly String GuidOne = "0123456789abcdef0123456789abcdef";
    private static String Temporary(String owner, Int32 number = 1) => owner + ".tmp-" + number.ToString("x32", CultureInfo.InvariantCulture);

    private sealed class Files(Boolean bootstrap, Int32 temporaryCount = 2, Boolean installed = true) : IDisposable
    {
        internal readonly ArchiveDirectory Directory = new();
        internal Boolean Bootstrap { get; } = bootstrap;
        internal String Target => Bootstrap ? Directory.DirectoryPath : Directory.ArchivePath;
        internal String Owner => Bootstrap ? Path.Combine(Directory.DirectoryPath, "manifest.cbor") : Target;
        internal String Lease => Bootstrap ? Path.Combine(Target, "bootstrap.lock") : Target + ".lock";
        internal String Child => Path.Combine(Directory.DirectoryPath, "unrelated-child");
        internal String[] Temps { get; private set; } = [];
        private Boolean initialized;

        internal Files Initialize()
        {
            if (initialized) return this;
            initialized = true;
            if (installed)
            {
                File.WriteAllBytes(Owner, [10, 11]);
                if (Bootstrap) File.WriteAllBytes(Path.Combine(Target, "00000000.chunk"), [12]);
            }
            Temps = Enumerable.Range(1, temporaryCount).Select(index => Temporary(Owner, index)).ToArray();
            foreach (var temp in Temps) File.WriteAllBytes(temp, [20, 21]);
            foreach (var name in new[] { "other.cbor.tmp-" + GuidOne, Path.GetFileName(Owner) + ".tmp-bad",
                Path.GetFileName(Owner) + ".tmp-" + GuidOne.ToUpperInvariant(), "0000000.chunk.tmp-" + GuidOne,
                "000000000.chunk.tmp-" + GuidOne, "2147483648.chunk.tmp-" + GuidOne,
                "-0000001.chunk.tmp-" + GuidOne, "README.txt" })
                File.WriteAllBytes(Path.Combine(Directory.DirectoryPath, name), [30, 31]);
            System.IO.Directory.CreateDirectory(Child);
            File.WriteAllBytes(Path.Combine(Child, Path.GetFileName(Temporary(Owner, 9))), [40]);
            return this;
        }

        internal Boolean Inspect(out RoamingNetworkArchiveCleanupPlan? plan, out RoamingNetworkArchiveCleanupResult result,
            RoamingNetworkArchiveCleanupLimits? limits = null)
            => Bootstrap ? RoamingNetworkArchiveMaintenance.TryInspectBootstrap(Target, out plan, out result, limits)
                         : RoamingNetworkArchiveMaintenance.TryInspectArchive(Target, out plan, out result, limits);

        internal RoamingNetworkArchiveCleanupPlan Review()
        {
            Assert.That(Inspect(out var plan, out var result), Is.True, result.Error);
            Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkArchiveCleanupOutcome.Inspected)); return plan!;
        }

        internal Dictionary<String, Byte[]> Bytes()
            => System.IO.Directory.GetFiles(Directory.DirectoryPath).Where(path => path != Lease).ToDictionary(Path.GetFileName, File.ReadAllBytes);

        internal void AssertBytes(Dictionary<String, Byte[]> expected)
        {
            var actual = Bytes(); Assert.That(actual.Keys, Is.EquivalentTo(expected.Keys));
            foreach (var (name, bytes) in expected) Assert.That(actual[name], Is.EqualTo(bytes), name);
            Assert.That(File.ReadAllBytes(System.IO.Path.Combine(Child, Path.GetFileName(Temporary(Owner, 9)))), Is.EqualTo(new Byte[] { 40 }));
        }

        public void Dispose()
        {
            foreach (var path in System.IO.Directory.GetFiles(Directory.DirectoryPath)) File.SetAttributes(path, FileAttributes.Normal);
            File.Delete(Path.Combine(Child, Path.GetFileName(Temporary(Owner, 9)))); System.IO.Directory.Delete(Child);
            Directory.Dispose();
        }
    }

    [TestCase(false, 0)]
    [TestCase(false, 1)]
    [TestCase(false, 3)]
    [TestCase(true, 0)]
    [TestCase(true, 1)]
    [TestCase(true, 3)]
    public void Review_and_default_execution_preserve_all_bytes_and_explicit_cleanup_removes_only_recognized_temporary_files(Boolean bootstrap, Int32 count)
    {
        using var files = new Files(bootstrap, count).Initialize(); var before = files.Bytes(); var plan = files.Review();
        Assert.That(plan.Scope, Is.EqualTo(bootstrap ? RoamingNetworkArchiveCleanupScope.Bootstrap : RoamingNetworkArchiveCleanupScope.Archive));
        Assert.That(plan.TemporaryFiles.Select(file => file.Name), Is.EqualTo(files.Temps.Select(Path.GetFileName)));
        foreach (var file in plan.Files)
            Assert.That(file.SHA256Digest.ToArray(), Is.EqualTo(SHA256.HashData(before[file.Name])));
        files.AssertBytes(before);
        Assert.That(RoamingNetworkArchiveMaintenance.TryExecute(plan, out var preview), Is.True, preview.Error);
        Assert.That(preview.Outcome, Is.EqualTo(RoamingNetworkArchiveCleanupOutcome.CleanupAvailable)); Assert.That(preview.RemovedFiles, Is.Empty);
        files.AssertBytes(before); Assert.That(files.Review().Id, Is.EqualTo(plan.Id));
        Assert.That(RoamingNetworkArchiveMaintenance.TryExecute(plan, out var result, cleanup: true), Is.True, result.Error);
        Assert.That(result.Outcome, Is.EqualTo(count == 0 ? RoamingNetworkArchiveCleanupOutcome.NothingToClean : RoamingNetworkArchiveCleanupOutcome.Cleaned));
        Assert.That(result.RemovedFiles, Is.EqualTo(plan.TemporaryFiles.Select(file => file.Name)));
        foreach (var name in result.RemovedFiles) before.Remove(name); files.AssertBytes(before);
        Assert.That(File.Exists(files.Lease), Is.True); Assert.That(files.Review().TemporaryFiles, Is.Empty);
        Assert.That(RoamingNetworkArchiveMaintenance.TryExecute(plan, out var old, cleanup: true), Is.EqualTo(count == 0));
        Assert.That(old.Outcome, Is.EqualTo(count == 0 ? RoamingNetworkArchiveCleanupOutcome.NothingToClean : RoamingNetworkArchiveCleanupOutcome.InventoryChanged));
    }

    private static IEnumerable<TestCaseData> StaleCases()
    {
        foreach (var bootstrap in new[] { false, true })
            foreach (var change in new[] { "Add", "Remove", "Rename", "Bytes", "Length", "Timestamp", "Attributes", "Installed", "InstalledRemoved" })
                yield return new(bootstrap, change);
    }

    [TestCaseSource(nameof(StaleCases))]
    public void Changed_reviewed_inventories_are_rejected_before_any_deletion_even_when_byte_lengths_and_timestamps_match(Boolean bootstrap, String change)
    {
        using var files = new Files(bootstrap).Initialize(); var plan = files.Review(); var first = files.Temps[0];
        switch (change)
        {
            case "Add": File.WriteAllBytes(Temporary(files.Owner, 9), [22]); break;
            case "Remove": File.Delete(first); break;
            case "Rename": File.Move(first, Temporary(files.Owner, 9)); break;
            case "Bytes": var modified = File.GetLastWriteTimeUtc(first); File.WriteAllBytes(first, [99, 98]); File.SetLastWriteTimeUtc(first, modified); break;
            case "Length": File.WriteAllBytes(first, [99]); break;
            case "Timestamp": File.SetLastWriteTimeUtc(first, File.GetLastWriteTimeUtc(first).AddMinutes(-1)); break;
            case "Attributes": File.SetAttributes(first, File.GetAttributes(first) | FileAttributes.ReadOnly); break;
            case "Installed": File.WriteAllBytes(files.Owner, [99, 98]); break;
            case "InstalledRemoved": File.Delete(files.Owner); break;
        }
        var changed = files.Bytes();
        foreach (var execute in new[] { false, true })
        {
            Assert.That(RoamingNetworkArchiveMaintenance.TryExecute(plan, out var result, cleanup: execute), Is.False);
            Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkArchiveCleanupOutcome.InventoryChanged)); Assert.That(result.RemovedFiles, Is.Empty);
            files.AssertBytes(changed);
        }
        Assert.That(files.Review().Id, Is.Not.EqualTo(plan.Id));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Newly_installed_data_invalidates_an_earlier_review_of_an_unpublished_write(Boolean bootstrap)
    {
        using var files = new Files(bootstrap, installed: false).Initialize(); var plan = files.Review();
        Assert.That(plan.Files.All(file => file.Kind == RoamingNetworkArchiveFileKind.Temporary), Is.True);
        File.WriteAllBytes(files.Owner, [10, 11]); var bytes = files.Bytes();
        Assert.That(RoamingNetworkArchiveMaintenance.TryExecute(plan, out var result, cleanup: true), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkArchiveCleanupOutcome.InventoryChanged)); Assert.That(result.RemovedFiles, Is.Empty); files.AssertBytes(bytes);
    }

    [Test]
    public void An_archive_whose_own_name_looks_temporary_is_still_protected_as_the_selected_installed_file()
    {
        using var directory = new ArchiveDirectory(); var archive = Temporary(directory.ArchivePath, 1); var temp = Temporary(archive, 2);
        File.WriteAllBytes(archive, [1]); File.WriteAllBytes(temp, [2]);
        Assert.That(RoamingNetworkArchiveMaintenance.TryInspectArchive(archive, out var plan, out var review), Is.True, review.Error);
        Assert.That(plan!.Files.Single(file => file.Name == Path.GetFileName(archive)).Kind, Is.EqualTo(RoamingNetworkArchiveFileKind.Installed));
        Assert.That(RoamingNetworkArchiveMaintenance.TryExecute(plan, out var result, cleanup: true), Is.True, result.Error);
        Assert.That(result.RemovedFiles, Is.EqualTo(new[] { Path.GetFileName(temp) })); Assert.That(File.ReadAllBytes(archive), Is.EqualTo(new Byte[] { 1 }));
    }

    [TestCase(false, "Lease")]
    [TestCase(false, "Temporary")]
    [TestCase(false, "Installed")]
    [TestCase(true, "Lease")]
    [TestCase(true, "Temporary")]
    [TestCase(true, "Installed")]
    public void Held_writer_or_file_handles_reject_inspection_and_execution_and_failure_releases_the_maintenance_lease(Boolean bootstrap, String heldKind)
    {
        using var files = new Files(bootstrap).Initialize(); var plan = files.Review(); var bytes = files.Bytes();
        var path = heldKind switch { "Lease" => files.Lease, "Temporary" => files.Temps[0], _ => files.Owner };
        using (var held = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.That(files.Inspect(out var absent, out var review), Is.False); Assert.That(absent, Is.Null);
            Assert.That(review.Outcome, Is.EqualTo(RoamingNetworkArchiveCleanupOutcome.PersistenceFailure));
            Assert.That(RoamingNetworkArchiveMaintenance.TryExecute(plan, out var failed, cleanup: true), Is.False);
            Assert.That(failed.Outcome, Is.EqualTo(RoamingNetworkArchiveCleanupOutcome.PersistenceFailure)); Assert.That(failed.RemovedFiles, Is.Empty);
        }
        files.AssertBytes(bytes);
        using (var lease = new FileStream(files.Lease, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        Assert.That(RoamingNetworkArchiveMaintenance.TryExecute(plan, out var retry, cleanup: true), Is.True, retry.Error);
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void Recognized_directories_are_rejected_and_their_contents_are_never_traversed_or_removed(Boolean bootstrap, Boolean installed)
    {
        using var files = new Files(bootstrap).Initialize(); var path = installed ? files.Owner : Temporary(files.Owner, 9);
        if (installed) File.Delete(path);
        System.IO.Directory.CreateDirectory(path); var child = Path.Combine(path, "valuable.txt"); File.WriteAllBytes(child, [7]);
        try
        {
            Assert.That(files.Inspect(out var absent, out var result), Is.False); Assert.That(absent, Is.Null);
            Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkArchiveCleanupOutcome.UnsupportedFile)); Assert.That(result.AffectedPath, Is.EqualTo(path));
            Assert.That(File.ReadAllBytes(child), Is.EqualTo(new Byte[] { 7 })); Assert.That(files.Temps.All(File.Exists), Is.True);
        }
        finally { File.Delete(child); System.IO.Directory.Delete(path); }
    }

    [TestCase(false, "Failure", 1)]
    [TestCase(false, "Failure", 2)]
    [TestCase(false, "Changed", 1)]
    [TestCase(false, "Changed", 2)]
    [TestCase(true, "Failure", 1)]
    [TestCase(true, "Failure", 2)]
    [TestCase(true, "Changed", 1)]
    [TestCase(true, "Changed", 2)]
    public void Mid_cleanup_failures_report_exact_partial_progress_and_require_a_new_review_for_retry(Boolean bootstrap, String fault, Int32 at)
    {
        using var files = new Files(bootstrap, 3).Initialize(); var plan = files.Review(); var count = 0;
        Assert.That(RoamingNetworkArchiveMaintenance.ExecuteObserved(plan, out var result, true, path => {
            Assert.That(files.Inspect(out _, out var nested), Is.False);
            Assert.That(nested.Outcome, Is.EqualTo(RoamingNetworkArchiveCleanupOutcome.PersistenceFailure));
            if (++count != at) return;
            if (fault == "Failure") throw new IOException("Injected cleanup failure");
            File.WriteAllBytes(path, [99, 98]);
        }), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(fault == "Failure" ? RoamingNetworkArchiveCleanupOutcome.PersistenceFailure : RoamingNetworkArchiveCleanupOutcome.InventoryChanged));
        Assert.That(result.RemovedFiles, Is.EqualTo(plan.TemporaryFiles.Take(at - 1).Select(file => file.Name)));
        Assert.That(result.AffectedPath, Is.EqualTo(files.Temps[at - 1]));
        Assert.That(File.Exists(files.Owner), Is.True); Assert.That(files.Temps.Take(at - 1).Any(File.Exists), Is.False);
        Assert.That(files.Temps.Skip(at - 1).All(File.Exists), Is.True);
        if (fault == "Changed") Assert.That(File.ReadAllBytes(files.Temps[at - 1]), Is.EqualTo(new Byte[] { 99, 98 }));
        using (var lease = new FileStream(files.Lease, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        var fresh = files.Review(); Assert.That(RoamingNetworkArchiveMaintenance.TryExecute(fresh, out var retry, cleanup: true), Is.True, retry.Error);
        Assert.That(retry.RemovedFiles.Length, Is.EqualTo(4 - at));
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void Unexpected_changes_during_deletion_are_reported_after_cleanup_without_removing_new_or_installed_files(Boolean bootstrap, Boolean installed)
    {
        using var files = new Files(bootstrap).Initialize(); var plan = files.Review(); var changed = false;
        var unexpected = installed ? files.Owner : Temporary(files.Owner, 9);
        Assert.That(RoamingNetworkArchiveMaintenance.ExecuteObserved(plan, out var result, true, _ => {
            if (changed) return; changed = true; File.WriteAllBytes(unexpected, [99]);
        }), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkArchiveCleanupOutcome.InventoryChanged));
        Assert.That(result.RemovedFiles, Is.EqualTo(plan.TemporaryFiles.Select(file => file.Name)));
        Assert.That(File.ReadAllBytes(unexpected), Is.EqualTo(new Byte[] { 99 }));
        Assert.That(RoamingNetworkArchiveMaintenance.TryExecute(files.Review(), out var retry, cleanup: true), Is.True, retry.Error);
        Assert.That(File.Exists(files.Owner), Is.True);
    }

    private static IEnumerable<TestCaseData> BudgetCases()
    {
        foreach (var bootstrap in new[] { false, true })
            foreach (var budget in Enum.GetNames<RoamingNetworkArchiveCleanupLimitKind>()) yield return new(bootstrap, budget);
    }

    [TestCaseSource(nameof(BudgetCases))]
    public void Inventory_limits_accept_exact_budgets_and_reject_excess_without_deleting_data(Boolean bootstrap, String budget)
    {
        using var files = new Files(bootstrap).Initialize(); var baseline = files.Review(); var bytes = files.Bytes();
        var entryCount = System.IO.Directory.GetFileSystemEntries(files.Directory.DirectoryPath).Length;
        var exact = new RoamingNetworkArchiveCleanupLimits(entryCount, baseline.Files.Length, baseline.Files.Max(file => file.Length), baseline.Files.Sum(file => file.Length));
        Assert.That(files.Inspect(out var plan, out var result, exact), Is.True, result.Error);
        Assert.That(RoamingNetworkArchiveMaintenance.TryExecute(plan!, out var preview), Is.True, preview.Error);
        var limit = new RoamingNetworkArchiveCleanupLimits(budget == "DirectoryEntries" ? entryCount - 1 : entryCount,
            budget == "Files" ? baseline.Files.Length - 1 : baseline.Files.Length,
            budget == "FileBytes" ? 1 : exact.MaxFileBytes, budget == "TotalBytes" ? exact.MaxTotalBytes - 1 : exact.MaxTotalBytes);
        Assert.That(files.Inspect(out var absent, out result, limit), Is.False); Assert.That(absent, Is.Null);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkArchiveCleanupOutcome.LimitExceeded));
        Assert.That(result.LimitViolation!.Kind.ToString(), Is.EqualTo(budget)); Assert.That(result.LimitViolation.Observed, Is.GreaterThan(result.LimitViolation.Maximum));
        files.AssertBytes(bytes); using var lease = new FileStream(files.Lease, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

    [TestCaseSource(nameof(BudgetCases))]
    public void Execution_reapplies_reviewed_budgets_and_rejects_new_excess_before_any_deletion(Boolean bootstrap, String budget)
    {
        using var files = new Files(bootstrap).Initialize(); var initial = files.Review();
        var entries = System.IO.Directory.GetFileSystemEntries(files.Directory.DirectoryPath).Length;
        var limits = new RoamingNetworkArchiveCleanupLimits(budget == "DirectoryEntries" ? entries : 100000,
            budget == "Files" ? initial.Files.Length : 8192, budget == "FileBytes" ? 2 : 1024,
            budget == "TotalBytes" ? initial.Files.Sum(file => file.Length) : 1024);
        Assert.That(files.Inspect(out var plan, out var review, limits), Is.True, review.Error);
        if (budget == "DirectoryEntries") File.WriteAllBytes(Path.Combine(files.Directory.DirectoryPath, "new-unrelated.txt"), [1]);
        else if (budget == "Files") File.WriteAllBytes(Temporary(files.Owner, 9), [1]);
        else File.WriteAllBytes(files.Temps[0], [1, 2, 3]);
        var changed = files.Bytes();
        foreach (var cleanup in new[] { false, true })
        {
            Assert.That(RoamingNetworkArchiveMaintenance.TryExecute(plan!, out var result, cleanup), Is.False);
            Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkArchiveCleanupOutcome.LimitExceeded)); Assert.That(result.RemovedFiles, Is.Empty);
            Assert.That(result.LimitViolation!.Kind.ToString(), Is.EqualTo(budget)); files.AssertBytes(changed);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Unrelated_file_changes_do_not_authorize_their_deletion_or_invalidate_recognized_reviewed_data(Boolean bootstrap)
    {
        using var files = new Files(bootstrap).Initialize(); var plan = files.Review();
        File.WriteAllBytes(Path.Combine(files.Directory.DirectoryPath, "README.txt"), [99]);
        File.WriteAllBytes(Temporary(files.Owner, 9) + ".extra", [98]); var changed = files.Bytes();
        Assert.That(RoamingNetworkArchiveMaintenance.TryExecute(plan, out var result, cleanup: true), Is.True, result.Error);
        foreach (var name in result.RemovedFiles) changed.Remove(name); files.AssertBytes(changed);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Installed_filename_case_follows_platform_path_comparison_and_remains_protected(Boolean bootstrap)
    {
        using var files = new Files(bootstrap).Initialize(); var upper = Path.Combine(files.Directory.DirectoryPath, Path.GetFileName(files.Owner).ToUpperInvariant());
        var interim = Path.Combine(files.Directory.DirectoryPath, "rename-intermediate"); File.Move(files.Owner, interim); File.Move(interim, upper);
        var plan = files.Review(); var installed = plan.Files.Where(file => file.Kind == RoamingNetworkArchiveFileKind.Installed).ToArray();
        Assert.That(installed.Length, Is.EqualTo((bootstrap ? 1 : 0) + (OperatingSystem.IsWindows() ? 1 : 0)));
        if (OperatingSystem.IsWindows()) Assert.That(installed.Select(file => file.Name), Does.Contain(Path.GetFileName(upper)));
        Assert.That(RoamingNetworkArchiveMaintenance.TryExecute(plan, out var result, cleanup: true), Is.True, result.Error);
        Assert.That(File.ReadAllBytes(upper), Is.EqualTo(new Byte[] { 10, 11 }));
    }

    [TestCase("DirectoryEntries", 0)]
    [TestCase("DirectoryEntries", -1)]
    [TestCase("Files", 0)]
    [TestCase("Files", -1)]
    [TestCase("FileBytes", 0)]
    [TestCase("FileBytes", -1)]
    [TestCase("TotalBytes", 0)]
    [TestCase("TotalBytes", -1)]
    public void Inventory_limits_require_positive_configuration(String budget, Int32 value)
        => Assert.That(() => new RoamingNetworkArchiveCleanupLimits(budget == "DirectoryEntries" ? value : 1,
            budget == "Files" ? value : 1, budget == "FileBytes" ? value : 1, budget == "TotalBytes" ? value : 1), Throws.InstanceOf<ArgumentException>());

    [TestCase(false)]
    [TestCase(true)]
    public void Invalid_paths_null_plans_and_missing_directories_cannot_create_a_maintenance_destination(Boolean bootstrap)
    {
        using var directory = new ArchiveDirectory(); var missing = Path.Combine(directory.DirectoryPath, "missing", "archive.cbor");
        foreach (var path in new[] { null, "", " ", missing })
        {
            var success = bootstrap ? RoamingNetworkArchiveMaintenance.TryInspectBootstrap(path!, out var plan, out var result)
                                    : RoamingNetworkArchiveMaintenance.TryInspectArchive(path!, out plan, out result);
            Assert.That(success, Is.False); Assert.That(plan, Is.Null); Assert.That(result.RemovedFiles, Is.Empty);
        }
        Assert.That(System.IO.Directory.GetFileSystemEntries(directory.DirectoryPath), Is.Empty);
        Assert.That(RoamingNetworkArchiveMaintenance.TryExecute(null!, out var invalid, cleanup: true), Is.False);
        Assert.That(invalid.Outcome, Is.EqualTo(RoamingNetworkArchiveCleanupOutcome.InvalidInput));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void A_real_file_handle_failure_after_one_deletion_reports_partial_progress_and_preserves_remaining_files(Boolean bootstrap)
    {
        using var files = new Files(bootstrap, 3).Initialize(); var plan = files.Review(); FileStream? held = null;
        try
        {
            Assert.That(RoamingNetworkArchiveMaintenance.ExecuteObserved(plan, out var result, true, path => {
                if (path == files.Temps[1]) held = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
            }), Is.False);
            Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkArchiveCleanupOutcome.PersistenceFailure));
            Assert.That(result.RemovedFiles, Is.EqualTo(new[] { Path.GetFileName(files.Temps[0]) }));
            Assert.That(result.AffectedPath, Is.EqualTo(files.Temps[1]));
        }
        finally { held?.Dispose(); }
        Assert.That(files.Temps.Skip(1).All(File.Exists), Is.True);
        Assert.That(RoamingNetworkArchiveMaintenance.TryExecute(plan, out var stale, cleanup: true), Is.False);
        Assert.That(stale.Outcome, Is.EqualTo(RoamingNetworkArchiveCleanupOutcome.InventoryChanged)); Assert.That(stale.RemovedFiles, Is.Empty);
        Assert.That(RoamingNetworkArchiveMaintenance.TryExecute(files.Review(), out var retry, cleanup: true), Is.True, retry.Error);
        Assert.That(retry.RemovedFiles, Has.Length.EqualTo(2));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Symbolic_files_leases_and_directory_aliases_are_rejected_without_following_or_deleting_their_targets(Boolean bootstrap)
    {
        using var files = new Files(bootstrap).Initialize(); using var outside = new ArchiveDirectory(); File.WriteAllBytes(outside.ArchivePath, [77]);
        foreach (var path in new[] { files.Temps[0], files.Owner, files.Lease })
        {
            if (File.Exists(path)) File.Delete(path);
            try
            {
                try { File.CreateSymbolicLink(path, outside.ArchivePath); }
                catch (Exception exception) when (exception is UnauthorizedAccessException or PlatformNotSupportedException ||
                    exception is IOException && OperatingSystem.IsWindows() && (exception.HResult & 0xffff) == 1314)
                { Assert.Ignore("Filesystem cannot create symbolic links: " + exception.Message); }
                Assert.That(files.Inspect(out var absent, out var result), Is.False); Assert.That(absent, Is.Null);
                Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkArchiveCleanupOutcome.UnsupportedFile));
                Assert.That(File.Exists(path), Is.True); Assert.That(File.ReadAllBytes(outside.ArchivePath), Is.EqualTo(new Byte[] { 77 }));
            }
            finally { File.Delete(path); }
        }
        var alias = Path.Combine(outside.DirectoryPath, "alias");
        try
        {
            System.IO.Directory.CreateSymbolicLink(alias, files.Directory.DirectoryPath);
            var success = bootstrap ? RoamingNetworkArchiveMaintenance.TryInspectBootstrap(alias, out var plan, out var result)
                : RoamingNetworkArchiveMaintenance.TryInspectArchive(Path.Combine(alias, "history.cbor"), out plan, out result);
            Assert.That(success, Is.False); Assert.That(plan, Is.Null); Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkArchiveCleanupOutcome.UnsupportedFile));
            Assert.That(File.ReadAllBytes(outside.ArchivePath), Is.EqualTo(new Byte[] { 77 }));
        }
        finally { if (System.IO.Directory.Exists(alias)) System.IO.Directory.Delete(alias); }
    }

    [Test]
    public void Bootstrap_inventory_recognizes_exact_canonical_chunk_indices_including_more_than_eight_digits()
    {
        using var files = new Files(true, 0).Initialize(); var indices = new[] { 0, 1, 99999999, 100000000, Int32.MaxValue };
        foreach (var index in indices) File.WriteAllBytes(Temporary(Path.Combine(files.Target, index.ToString("D8", CultureInfo.InvariantCulture) + ".chunk")), [1]);
        var before = files.Bytes(); var plan = files.Review(); Assert.That(plan.TemporaryFiles, Has.Length.EqualTo(indices.Length));
        Assert.That(RoamingNetworkArchiveMaintenance.TryExecute(plan, out var result, cleanup: true), Is.True, result.Error);
        foreach (var name in result.RemovedFiles) before.Remove(name); files.AssertBytes(before);
    }

    [Test]
    public void Live_history_writer_blocks_maintenance_without_changing_its_head_or_runtime_and_closed_archive_reopens_after_cleanup()
    {
        using var directory = new ArchiveDirectory(); var orphan = Temporary(directory.ArchivePath);
        using var history = RoamingNetworkHistory.CreatePersistent(directory.ArchivePath, Network(), VerifyBatch, VerifyCommit); LinearHistory(history);
        Status(history, EvseTarget, "charging"); var before = new Observation(history); var bytes = File.ReadAllBytes(directory.ArchivePath);
        File.WriteAllBytes(orphan, [1]);
        Assert.That(RoamingNetworkArchiveMaintenance.TryInspectArchive(directory.ArchivePath, out var absent, out var blocked), Is.False);
        Assert.That(absent, Is.Null); Assert.That(blocked.Outcome, Is.EqualTo(RoamingNetworkArchiveCleanupOutcome.PersistenceFailure)); before.AssertUnchanged(history);
        history.Dispose();
        Assert.That(RoamingNetworkArchiveMaintenance.TryInspectArchive(directory.ArchivePath, out var plan, out var result), Is.True, result.Error);
        Assert.That(RoamingNetworkArchiveMaintenance.TryExecute(plan!, out result, cleanup: true), Is.True, result.Error);
        Assert.That(File.ReadAllBytes(directory.ArchivePath), Is.EqualTo(bytes));
        using var reopened = RoamingNetworkHistory.Open(directory.ArchivePath, VerifyBatch, VerifyCommit);
        Publish(reopened, Prepare(reopened, reopened.Head.Id, "after-maintenance", Power("225 kW")));
    }

    [Test]
    public void Live_bootstrap_writer_blocks_maintenance_and_verified_progress_resumes_after_closing_and_cleanup()
    {
        using var sender = History(); var source = sender.CreateBootstrap(256); using var directory = new ArchiveDirectory();
        using var receiver = RoamingNetworkBootstrapReceiver.Create(directory.DirectoryPath, source.Manifest);
        Assert.That(receiver.TryAcceptChunk(source.CreateChunk(0), out _), Is.True);
        var orphan = Temporary(Path.Combine(directory.DirectoryPath, "00000001.chunk")); File.WriteAllBytes(orphan, source.CreateChunk(1).Data.ToArray());
        Assert.That(RoamingNetworkArchiveMaintenance.TryInspectBootstrap(directory.DirectoryPath, out _, out var blocked), Is.False);
        Assert.That(blocked.Outcome, Is.EqualTo(RoamingNetworkArchiveCleanupOutcome.PersistenceFailure)); Assert.That(receiver.NextChunk, Is.EqualTo(1));
        receiver.Dispose();
        Assert.That(RoamingNetworkArchiveMaintenance.TryInspectBootstrap(directory.DirectoryPath, out var plan, out var result), Is.True, result.Error);
        Assert.That(RoamingNetworkArchiveMaintenance.TryExecute(plan!, out result, cleanup: true), Is.True, result.Error);
        using var reopened = RoamingNetworkBootstrapReceiver.Open(directory.DirectoryPath, source.Manifest.Id);
        Assert.That(reopened.NextChunk, Is.EqualTo(1)); Finish(reopened, source, true);
        Assert.That(reopened.TryActivate(source.Manifest.Id, out var activated, out var activation, activate: true), Is.True, activation.Error); activated!.Dispose();
    }

    [Test]
    public async Task Real_archive_process_exit_orphan_is_reviewed_and_removed_before_retrying_the_original_commit()
    {
        using var directory = new ArchiveDirectory(); RoamingNetworkCommit candidate; Byte[] original;
        using (var history = RoamingNetworkHistory.CreatePersistent(directory.ArchivePath, Network()))
        {
            candidate = history.PrepareCommit(history.Head.Id, Batch(history.Head.Snapshot, "crash-change", Power("150 kW")));
            original = history.ToCBOR();
        }
        await ArchiveCrashTestSupport.Run(directory.ArchivePath, "TemporaryFileFlushed");
        Assert.That(RoamingNetworkArchiveMaintenance.TryInspectArchive(directory.ArchivePath, out var plan, out var review), Is.True, review.Error);
        Assert.That(plan!.TemporaryFiles, Has.Length.EqualTo(1));
        Assert.That(RoamingNetworkArchiveMaintenance.TryExecute(plan, out var cleaned, cleanup: true), Is.True, cleaned.Error);
        Assert.That(File.ReadAllBytes(directory.ArchivePath), Is.EqualTo(original));
        using var reopened = RoamingNetworkHistory.Open(directory.ArchivePath);
        Assert.That(reopened.TryPublish(candidate.Parents[0], candidate, out var published), Is.True, published.Error);
    }

    [Test]
    public async Task Real_cold_archive_process_exit_orphan_is_removed_before_explicit_retention_retry()
    {
        using var directory = new ArchiveDirectory(); RoamingNetworkCommit snapshot; RoamingNetworkRetentionPlan retention; Byte[] original;
        using (var history = RoamingNetworkHistory.CreatePersistent(directory.ArchivePath, Network(), VerifyBatch, VerifyCommit))
        {
            snapshot = LinearHistory(history); retention = Plan(history, snapshot); original = history.ToCBOR();
            File.WriteAllLines(directory.ArchivePath + ".pins", [history.CheckpointId.ToString(), history.AnchorId.ToString(), snapshot.Id.ToString()]);
            File.WriteAllLines(directory.ArchivePath + ".released", []); File.WriteAllText(directory.ArchivePath + ".plan-id", retention.Id.ToString());
        }
        var cold = Path.Combine(directory.DirectoryPath, "cold.cbor");
        await ArchiveCrashTestSupport.Run(directory.ArchivePath, "ColdTemporaryFileFlushed", "Retention");
        Assert.That(RoamingNetworkArchiveMaintenance.TryInspectArchive(cold, out var plan, out var review), Is.True, review.Error);
        Assert.That(plan!.TemporaryFiles, Has.Length.EqualTo(1)); Assert.That(plan.Files, Has.Length.EqualTo(1));
        Assert.That(RoamingNetworkArchiveMaintenance.TryExecute(plan, out var cleaned, cleanup: true), Is.True, cleaned.Error);
        Assert.That(File.Exists(cold), Is.False); Assert.That(File.ReadAllBytes(directory.ArchivePath), Is.EqualTo(original));
        using var reopened = RoamingNetworkHistory.Open(directory.ArchivePath, VerifyBatch, VerifyCommit);
        Prune(reopened, retention, cold); Assert.That(File.ReadAllBytes(cold), Is.EqualTo(original));
    }

    [TestCase("BootstrapManifest")]
    [TestCase("BootstrapChunk")]
    [TestCase("BootstrapActivation")]
    public async Task Real_bootstrap_process_exit_orphans_are_explicitly_cleaned_and_the_same_frozen_transfer_continues(String operation)
    {
        using var sender = History(); LinearHistory(sender); var source = sender.CreateBootstrap(512);
        using var control = new ArchiveDirectory(); using var staging = new ArchiveDirectory(); using var destination = new ArchiveDirectory();
        File.WriteAllBytes(control.ArchivePath + ".manifest", source.Manifest.ToCBOR());
        File.WriteAllText(control.ArchivePath + ".expected-id", source.Manifest.Id.ToString());
        File.WriteAllLines(control.ArchivePath + ".paths", [staging.DirectoryPath, destination.ArchivePath]);
        File.WriteAllBytes(control.ArchivePath + ".fragment", source.CreateChunk(1).Data.ToArray());
        if (operation != "BootstrapManifest")
        {
            using var initial = RoamingNetworkBootstrapReceiver.Create(staging.DirectoryPath, source.Manifest);
            if (operation == "BootstrapChunk") Assert.That(initial.TryAcceptChunk(source.CreateChunk(0), out _), Is.True);
            else Finish(initial, source, true);
        }
        await ArchiveCrashTestSupport.Run(control.ArchivePath, "TemporaryFileFlushed", operation);
        var success = operation == "BootstrapActivation" ? RoamingNetworkArchiveMaintenance.TryInspectArchive(destination.ArchivePath, out var plan, out var review)
            : RoamingNetworkArchiveMaintenance.TryInspectBootstrap(staging.DirectoryPath, out plan, out review);
        Assert.That(success, Is.True, review.Error); Assert.That(plan!.TemporaryFiles, Has.Length.EqualTo(1));
        var protectedBytes = plan.Files.Where(file => file.Kind == RoamingNetworkArchiveFileKind.Installed).ToDictionary(file => file.Name,
            file => File.ReadAllBytes(Path.Combine(staging.DirectoryPath, file.Name)));
        Assert.That(RoamingNetworkArchiveMaintenance.TryExecute(plan, out var cleaned, cleanup: true), Is.True, cleaned.Error);
        foreach (var (name, bytes) in protectedBytes) Assert.That(File.ReadAllBytes(Path.Combine(staging.DirectoryPath, name)), Is.EqualTo(bytes));
        using var receiver = operation == "BootstrapManifest" ? RoamingNetworkBootstrapReceiver.Create(staging.DirectoryPath, source.Manifest)
            : RoamingNetworkBootstrapReceiver.Open(staging.DirectoryPath, source.Manifest.Id);
        Assert.That(receiver.NextChunk, Is.EqualTo(operation == "BootstrapChunk" ? 1 : operation == "BootstrapManifest" ? 0 : source.Manifest.ChunkCount));
        Finish(receiver, source, true);
        Assert.That(receiver.TryActivate(source.Manifest.Id, out var activated, out var activation, activate: true, archivePath: destination.ArchivePath,
            verifyBatchSignature: VerifyBatch, verifyCommitSignature: VerifyCommit), Is.True, activation.Error);
        using (activated!) { Assert.That(activated!.ToCBOR(), Is.EqualTo(sender.ToCBOR())); }
        Assert.That(File.ReadAllBytes(destination.ArchivePath), Is.EqualTo(sender.ToCBOR()));
    }

    [TestCase("en-US")]
    [TestCase("de-DE")]
    [TestCase("tr-TR")]
    public void Immutable_review_json_and_identity_are_culture_independent(String culture)
    {
        using var files = new Files(true).Initialize(); var initial = files.Review(); var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture); var review = files.Review();
            Assert.That(review.Id, Is.EqualTo(initial.Id)); Assert.That(review.ToJSON(), Is.EqualTo(initial.ToJSON()));
            using var json = JsonDocument.Parse(review.ToJSON()); Assert.That(json.RootElement.GetProperty("Files").GetArrayLength(), Is.EqualTo(review.Files.Length));
            foreach (var type in new[] { typeof(RoamingNetworkArchiveCleanupPlan), typeof(RoamingNetworkArchiveCleanupLimits), typeof(RoamingNetworkArchiveFileState) })
                Assert.That(type.GetProperties().Where(property => property.SetMethod?.IsPublic == true), Is.Empty);
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }
}
