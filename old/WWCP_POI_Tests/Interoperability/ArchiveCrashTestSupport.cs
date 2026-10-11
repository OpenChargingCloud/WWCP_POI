/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Diagnostics;
using System.Text;
using NUnit.Framework;

namespace WWCP_POI_Tests.Interoperability;

internal static class ArchiveCrashTestSupport
{
    internal static async Task Run(String path, String stage, String? operation = null)
    {
        var start = new ProcessStartInfo("dotnet") {
            RedirectStandardOutput = true, RedirectStandardError = true,
            UseShellExecute = false, CreateNoWindow = true
        };
        foreach (var argument in new[] { "test", typeof(PersistenceTests).Assembly.Location, "--nologo", "--filter",
            "FullyQualifiedName=WWCP_POI_Tests.Interoperability.PersistenceTests.ArchiveCrashWorker" })
            start.ArgumentList.Add(argument);
        start.Environment["WWCP_INTEROP_CRASH_PATH"] = path;
        start.Environment["WWCP_INTEROP_CRASH_STAGE"] = stage;
        start.Environment.Remove("WWCP_INTEROP_CRASH_OPERATION");
        if (operation is not null) start.Environment["WWCP_INTEROP_CRASH_OPERATION"] = operation;
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        try { await process.WaitForExitAsync(timeout.Token); }
        finally { if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); } }
        var diagnostic = await output + "\n" + await error;
        Assert.That(process.ExitCode, Is.Not.Zero, diagnostic);
        Assert.That(File.Exists(path + ".stage"), Is.True, diagnostic);
        Assert.That(File.ReadAllText(path + ".stage"), Is.EqualTo(stage), diagnostic);
    }

    internal static void Exit(String path, String stage)
    {
        using (var file = new FileStream(path + ".stage", FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            file.Write(Encoding.UTF8.GetBytes(stage)); file.Flush(flushToDisk: true);
        }
        // No archive finally block, history disposal or test cleanup runs in this process.
        Environment.Exit(77);
    }
}
