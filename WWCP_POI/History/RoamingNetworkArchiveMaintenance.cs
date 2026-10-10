/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// Explicit local inventory and cleanup of interrupted writes, coordinated with archive or staging writers.
/// Only direct regular files matching the selected writer namespace can be temporary candidates.
/// </summary>
public static class RoamingNetworkArchiveMaintenance
{
    /// <summary>
    /// Review an archive and its exact GUID temporary siblings under the archive writer lease.
    /// The parent directory must exist; the final archive may be absent after interrupted activation.
    /// </summary>
    public static Boolean TryInspectArchive(String path, [NotNullWhen(true)] out RoamingNetworkArchiveCleanupPlan? plan,
        out RoamingNetworkArchiveCleanupResult result, RoamingNetworkArchiveCleanupLimits? limits = null)
        => Inspect(RoamingNetworkArchiveCleanupScope.Archive, path, out plan, out result, limits);

    /// <summary>
    /// Review installed manifest/chunk files and their exact GUID temporary siblings under the staging writer lease.
    /// The staging directory must exist; an interrupted initial manifest may leave no installed manifest.
    /// </summary>
    public static Boolean TryInspectBootstrap(String directory, [NotNullWhen(true)] out RoamingNetworkArchiveCleanupPlan? plan,
        out RoamingNetworkArchiveCleanupResult result, RoamingNetworkArchiveCleanupLimits? limits = null)
        => Inspect(RoamingNetworkArchiveCleanupScope.Bootstrap, directory, out plan, out result, limits);

    private static Boolean Inspect(RoamingNetworkArchiveCleanupScope scope, String target,
        [NotNullWhen(true)] out RoamingNetworkArchiveCleanupPlan? plan, out RoamingNetworkArchiveCleanupResult result,
        RoamingNetworkArchiveCleanupLimits? limits)
    {
        plan = null;
        try
        {
            var resolved = Resolve(scope, target);
            using var lease = Lease(scope, resolved);
            plan = Scan(scope, resolved, limits ?? new());
            result = new(RoamingNetworkArchiveCleanupOutcome.Inspected, plan); return true;
        }
        catch (Exception exception) { result = Failure(exception); return false; }
    }

    /// <summary>
    /// Reacquire the selected writer lease and reject a changed complete recognized inventory before deleting anything.
    /// The default only verifies availability; cleanup=true explicitly removes reviewed temporary files in ordinal order.
    /// An I/O failure reports exact partial progress; obtain a fresh review before retrying.
    /// </summary>
    public static Boolean TryExecute(RoamingNetworkArchiveCleanupPlan plan, out RoamingNetworkArchiveCleanupResult result,
        Boolean cleanup = false)
        => ExecuteObserved(plan, out result, cleanup, null);

    internal static Boolean ExecuteObserved(RoamingNetworkArchiveCleanupPlan plan, out RoamingNetworkArchiveCleanupResult result,
        Boolean cleanup, Action<String>? beforeDelete)
    {
        var removed = ImmutableArray.CreateBuilder<String>();
        String? affected = null;
        try
        {
            ArgumentNullException.ThrowIfNull(plan);
            var resolved = Resolve(plan.Scope, plan.Target);
            using var lease = Lease(plan.Scope, resolved);
            if (Scan(plan.Scope, resolved, plan.Limits).Id != plan.Id)
            {
                result = new(RoamingNetworkArchiveCleanupOutcome.InventoryChanged, plan, "The reviewed inventory changed."); return false;
            }
            if (!cleanup)
            {
                result = new(RoamingNetworkArchiveCleanupOutcome.CleanupAvailable, plan); return true;
            }
            var directory = DirectoryPath(plan.Scope, resolved);
            foreach (var file in plan.TemporaryFiles)
            {
                affected = Path.Combine(directory, file.Name);
                beforeDelete?.Invoke(affected);
                Int64 bytes = 0;
                if (!ReadState(affected, file.Kind, plan.Limits, ref bytes).SameAs(file))
                {
                    result = new(RoamingNetworkArchiveCleanupOutcome.InventoryChanged, plan,
                        "A reviewed temporary file changed before deletion.", removed.ToImmutable(), affected); return false;
                }
                File.Delete(affected); removed.Add(file.Name);
            }
            affected = null;
            var remaining = Scan(plan.Scope, resolved, plan.Limits).Files;
            var protectedFiles = plan.Files.Where(file => file.Kind == RoamingNetworkArchiveFileKind.Installed).ToArray();
            if (remaining.Length != protectedFiles.Length || remaining.Where((file, index) => !file.SameAs(protectedFiles[index])).Any())
            {
                result = new(RoamingNetworkArchiveCleanupOutcome.InventoryChanged, plan,
                    "The recognized inventory changed during cleanup.", removed.ToImmutable()); return false;
            }
            result = new(removed.Count == 0 ? RoamingNetworkArchiveCleanupOutcome.NothingToClean : RoamingNetworkArchiveCleanupOutcome.Cleaned,
                plan, RemovedFiles: removed.ToImmutable()); return true;
        }
        catch (Exception exception) { result = Failure(exception, plan, removed.ToImmutable(), affected); return false; }
    }

    private static String DirectoryPath(RoamingNetworkArchiveCleanupScope scope, String target)
        => scope == RoamingNetworkArchiveCleanupScope.Archive ? Path.GetDirectoryName(target)! : target;

    private static String Resolve(RoamingNetworkArchiveCleanupScope scope, String target)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(target);
        var resolved = Path.TrimEndingDirectorySeparator(Path.GetFullPath(target));
        var directory = DirectoryPath(scope, resolved);
        if (String.IsNullOrEmpty(directory) || !Directory.Exists(directory)) throw new DirectoryNotFoundException("The selected maintenance directory must exist.");
        for (var parent = new DirectoryInfo(directory); parent is not null; parent = parent.Parent)
            if ((parent.Attributes & FileAttributes.ReparsePoint) != 0) throw new UnsupportedFileException(parent.FullName);
        return resolved;
    }

    private static FileStream Lease(RoamingNetworkArchiveCleanupScope scope, String target)
    {
        var path = scope == RoamingNetworkArchiveCleanupScope.Archive ? target + ".lock" : Path.Combine(target, "bootstrap.lock");
        try { RequireRegular(path); }
        catch (FileNotFoundException) { }
        return new(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
    }

    private static RoamingNetworkArchiveCleanupPlan Scan(RoamingNetworkArchiveCleanupScope scope, String target,
        RoamingNetworkArchiveCleanupLimits limits)
    {
        var selected = new List<(String Path, RoamingNetworkArchiveFileKind Kind)>();
        Int64 entries = 0;
        foreach (var path in Directory.EnumerateFileSystemEntries(DirectoryPath(scope, target)))
        {
            Require(RoamingNetworkArchiveCleanupLimitKind.DirectoryEntries, limits.MaxDirectoryEntries, ++entries);
            if (Classify(scope, target, Path.GetFileName(path)) is not { } kind) continue;
            Require(RoamingNetworkArchiveCleanupLimitKind.Files, limits.MaxFiles, selected.Count + 1L);
            RequireRegular(path); selected.Add((path, kind));
        }
        var files = ImmutableArray.CreateBuilder<RoamingNetworkArchiveFileState>();
        Int64 total = 0;
        foreach (var (path, kind) in selected.OrderBy(file => Path.GetFileName(file.Path), StringComparer.Ordinal))
            files.Add(ReadState(path, kind, limits, ref total));
        return new(scope, target, limits, files.ToImmutable());
    }

    private static RoamingNetworkArchiveFileKind? Classify(RoamingNetworkArchiveCleanupScope scope, String target, String name)
    {
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (scope == RoamingNetworkArchiveCleanupScope.Archive && String.Equals(name, Path.GetFileName(target), comparison))
            return RoamingNetworkArchiveFileKind.Installed;
        var marker = name.LastIndexOf(".tmp-", StringComparison.Ordinal);
        var temporary = marker >= 0 && Guid.TryParseExact(name[(marker + 5)..], "N", out var guid) &&
                        guid.ToString("N") == name[(marker + 5)..];
        var owner = temporary ? name[..marker] : name;
        if (scope == RoamingNetworkArchiveCleanupScope.Archive)
        {
            if (!temporary || !String.Equals(owner, Path.GetFileName(target), comparison)) return null;
        }
        else if (!String.Equals(owner, "manifest.cbor", comparison) && !ChunkName(owner, comparison)) return null;
        return temporary ? RoamingNetworkArchiveFileKind.Temporary : RoamingNetworkArchiveFileKind.Installed;
    }

    private static Boolean ChunkName(String name, StringComparison comparison)
        => name.EndsWith(".chunk", comparison) &&
           Int32.TryParse(name[..^6], NumberStyles.None, CultureInfo.InvariantCulture, out var index) && index >= 0 &&
           String.Equals(name, index.ToString("D8", CultureInfo.InvariantCulture) + ".chunk", comparison);

    private static void RequireRegular(String path)
    {
        if ((File.GetAttributes(path) & (FileAttributes.Directory | FileAttributes.ReparsePoint | FileAttributes.Device)) != 0)
            throw new UnsupportedFileException(path);
    }

    private static RoamingNetworkArchiveFileState ReadState(String path, RoamingNetworkArchiveFileKind kind,
        RoamingNetworkArchiveCleanupLimits limits, ref Int64 total)
    {
        RequireRegular(path);
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var info = new FileInfo(path); var length = file.Length; var created = info.CreationTimeUtc; var modified = info.LastWriteTimeUtc; var attributes = info.Attributes;
        Require(RoamingNetworkArchiveCleanupLimitKind.FileBytes, limits.MaxFileBytes, length);
        Require(RoamingNetworkArchiveCleanupLimitKind.TotalBytes, limits.MaxTotalBytes, checked(total + length)); total += length;
        var digest = SHA256.HashData(file);
        RequireRegular(path); info.Refresh();
        if (file.Length != length || file.Position != length || info.Length != length || info.CreationTimeUtc != created || info.LastWriteTimeUtc != modified || info.Attributes != attributes)
            throw new IOException("A recognized file changed during inventory hashing.");
        return new(Path.GetFileName(path), kind, length, created, modified, attributes, digest);
    }

    private static void Require(RoamingNetworkArchiveCleanupLimitKind kind, Int64 maximum, Int64 observed)
    {
        if (observed > maximum) throw new CleanupLimitException(new(kind, maximum, observed));
    }

    private static RoamingNetworkArchiveCleanupResult Failure(Exception exception, RoamingNetworkArchiveCleanupPlan? plan = null,
        ImmutableArray<String> removed = default, String? affected = null)
        => exception switch {
            CleanupLimitException limit => new(RoamingNetworkArchiveCleanupOutcome.LimitExceeded, plan, limit.Message, removed, affected, limit.Violation),
            UnsupportedFileException unsupported => new(RoamingNetworkArchiveCleanupOutcome.UnsupportedFile, plan, unsupported.Message, removed, unsupported.Path),
            IOException or UnauthorizedAccessException => new(RoamingNetworkArchiveCleanupOutcome.PersistenceFailure, plan, exception.Message, removed, affected),
            _ => new(RoamingNetworkArchiveCleanupOutcome.InvalidInput, plan, exception.Message, removed, affected)
        };

    private sealed class UnsupportedFileException(String path) : IOException("Maintenance requires regular files and directories without symbolic links: " + path)
    {
        internal String Path { get; } = path;
    }

    private sealed class CleanupLimitException(RoamingNetworkArchiveCleanupLimitViolation violation)
        : IOException($"Maintenance inventory exceeds {violation.Kind}: observed {violation.Observed}, maximum {violation.Maximum}.")
    {
        internal RoamingNetworkArchiveCleanupLimitViolation Violation { get; } = violation;
    }
}
