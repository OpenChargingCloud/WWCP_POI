/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using cloud.charging.open.protocols.WWCP.POI;

namespace WWCP_POI_Benchmarks;

internal sealed record RunResult(Shape Shape, String Operation, Int32 Entities, Int32 RetainedCommits,
    Int32 Receipts, Int32 CatalogIds, String StaticIdentity, String ArchiveIdentity, Sample[] Samples,
    ChildETagRetention? ChildETagRetention = null, ChildETagState? ChildETagState = null,
    RecoveryInventory? RecoveryInventory = null, ReplayRetention? ReplayRetention = null,
    DomainInventory? DomainInventory = null);

internal static class Program
{
    private static readonly JsonSerializerOptions JSON = new() { WriteIndented = true };
    private static readonly String[] Operations = [.. SignatureCopyProbe.Operations, .. ArchiveRecoveryProbe.AllOperations, .. ArchiveBatchProbe.Operations, .. ChangeSetBatchProbe.Operations, .. ChildETagProbe.Operations, .. EncodingProbe.Operations, "archive-json-stream", "archive-cbor-stream", "hash-json", "hash-cbor", "etag-recompute", "static-update",
        "runtime-capture", "runtime-materialize", "archive-json", "archive-cbor", "replay-json", "replay-cbor",
        "snapshot-prepare", "persist-rewrite", "bootstrap-export", "bootstrap-transfer", "cold-read", "catalog-create"];

    private static Int32 Main(String[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        try
        {
            var options = Options(args);
            if (options.ContainsKey("help"))
            {
                Console.WriteLine("WWCP POI scaling measurements (Release build).\n" +
                    "--suite smoke|scaling|catalog|changesets|batcharchives|recovery|domainrecovery --samples 3 --processes 3 --warmups 1 --label NAME --output FILE\n" +
                    "--operations " + String.Join(",", Operations) + "\n" +
                    "Custom shape: --evses N --changes N --branches N --snapshot-every N --retentions N --locations N\n" +
                    "Compare: --compare-before FILE --compare-after FILE --summary FILE\n" +
                    "Encoding summary: --encoding-report FILE --summary FILE\n" +
                    "Configurable warmups per fresh worker; setup excluded. JSON records every sample and content identity.");
                return 0;
            }
            if (options.TryGetValue("compare-before", out var before))
            {
                Report.Compare(before, options["compare-after"], options["summary"]); return 0;
            }
            if (options.TryGetValue("encoding-report", out var encodingReport))
            {
                Report.Encoding(encodingReport, options["summary"]); return 0;
            }
            var samples = Number(options, "samples", 3, 1, 100);
            var warmups = Number(options, "warmups", 1, 1, 100);
            if (options.ContainsKey("worker"))
            {
                var shape = JsonSerializer.Deserialize<Shape>(options["shape"])!;
                var operation = options["operation"];
                if (SignatureCopyProbe.Operations.Contains(operation))
                {
                    var copies = new SignatureCopyProbe(shape, operation);
                    Console.WriteLine(JsonSerializer.Serialize(copies.Run(warmups, samples)));
                    return 0;
                }
                if (ArchiveRecoveryProbe.AllOperations.Contains(operation))
                {
                    var domain = shape.Name.StartsWith("domain-v", StringComparison.Ordinal)
                        ? new DomainRecoveryFixture(shape.EVSEs, Int32.Parse(shape.Name.AsSpan(8, 1), CultureInfo.InvariantCulture)) : null;
                    using var recovery = new ArchiveRecoveryProbe(shape, operation, domain);
                    Console.WriteLine(JsonSerializer.Serialize(recovery.Run(warmups, samples)));
                    return 0;
                }
                if (ArchiveBatchProbe.Operations.Contains(operation))
                {
                    using var archives = new ArchiveBatchProbe(shape);
                    Console.WriteLine(JsonSerializer.Serialize(archives.Run(operation, warmups, samples)));
                    return 0;
                }
                if (ChangeSetBatchProbe.Operations.Contains(operation))
                {
                    var batches = new ChangeSetBatchProbe(shape, operation);
                    Console.WriteLine(JsonSerializer.Serialize(batches.Run(operation, warmups, samples)));
                    return 0;
                }
                using var workload = new Workload(shape, operation);
                var action = workload.Operation(operation);
                workload.PrepareSample();
                var warmup = action(); workload.VerifyOutput();
                for (var i = 1; i < warmups; i++)
                {
                    workload.PrepareSample();
                    var next = action(); workload.VerifyOutput();
                    if (next != warmup) throw new InvalidOperationException("Nondeterministic warmup output.");
                }
                var measured = Enumerable.Range(0, samples).Select(_ => {
                    workload.PrepareSample();
                    var value = Measurement.Run(action);
                    workload.VerifyOutput(); return value;
                }).ToArray();
                if (measured.Any(sample => sample.Result != warmup)) throw new InvalidOperationException("Nondeterministic operation output.");
                var result = new RunResult(shape, operation, workload.EntityCount, workload.CommitCount,
                    workload.ReceiptCount, workload.CatalogIdCount, workload.StaticIdentity, workload.ArchiveIdentity, measured,
                    workload.ChildETagRetention, workload.ChildETagState);
                Console.WriteLine(JsonSerializer.Serialize(result));
                return 0;
            }
            var suite = options.GetValueOrDefault("suite", "smoke");
            var processes = Number(options, "processes", 3, 1, 100);
            var selected = options.GetValueOrDefault("operations", String.Join(",", Operations)).Split(',');
            if (selected.Any(value => !Operations.Contains(value))) throw new ArgumentException("Unknown operation selection.");
            Shape[] shapes = options.ContainsKey("evses") ? [new("custom",
                Number(options, "evses", 16, 1, 1000000), Number(options, "changes", 8, 1, 100000),
                Number(options, "branches", 2, 0, 10000), Number(options, "snapshot-every", 4, 0, 100000),
                Number(options, "retentions", 0, 0, 4096), Number(options, "locations", 256, 1, 1000000))] : suite switch {
                    "smoke" => [new("smoke", 16, 4, 2, 4, 1, 16)],
                    "scaling" => [new("graph-128", 128, 8, 2, 4, 0, 256), new("graph-512", 512, 8, 2, 4, 0, 256),
                                  new("history-64", 16, 64, 8, 16, 0, 256), new("pruned-4", 16, 32, 4, 8, 4, 256)],
                    "changesets" => [.. new[] { 1, 64, 512, 2048 }.SelectMany(count => new[] { 1, 4 }.Select(peers =>
                        new Shape($"batch-{count}-peers-{peers}", Math.Min(count, 512), count, peers, 0, 0, 0)))],
                    "recovery" => [new("recover-graph-128", 128, 8, 2, 4, 0, 256), new("recover-graph-512", 512, 8, 2, 4, 0, 256),
                                   new("recover-history-64", 16, 64, 8, 16, 0, 256), new("recover-pruned-4", 16, 32, 4, 8, 4, 256),
                                   new("recover-boundary-16", 16, 8, 2, 4, 0, 256),
                                   new("recover-batch-1-peers-1", 1, 1, 1, 0, 0, 0), new("recover-batch-2048-peers-4", 512, 2048, 4, 0, 0, 0)],
                    "domainrecovery" => [.. new[] { 16, 64 }.SelectMany(evses => Enumerable.Range(1, 4).Select(profile =>
                        new Shape($"domain-v{profile}-evses-{evses}", evses, 7, 2, profile == 1 ? 0 : 7, profile == 4 ? 2 : 0, 0)))],
                    "batcharchives" => [.. new[] { 1, 64, 512, 2048 }.SelectMany(count => new[] { 1, 4 }.Select(peers =>
                        new Shape($"archive-batch-{count}-peers-{peers}", Math.Min(count, 512), count, peers, 0, 0, 0)))],
                    "catalog" => [new("catalog-16", 1, 1, 0, 0, 0, 16), new("catalog-256", 1, 1, 0, 0, 0, 256),
                                  new("catalog-4096", 1, 1, 0, 0, 0, 4096), new("catalog-16384", 1, 1, 0, 0, 0, 16384)],
                    _ => throw new ArgumentException("Unknown suite.")
                };
            if (suite == "catalog") selected = ["catalog-create"];
            if (suite == "domainrecovery" && !options.ContainsKey("operations")) selected = ["read-cbor-model", "read-restore", "read-cbor", "read-json"];
            if (suite == "domainrecovery" && selected.Any(value => !ArchiveRecoveryProbe.AllOperations.Contains(value)))
                throw new ArgumentException("The domainrecovery suite supports only archive read stages.");
            if (suite == "recovery" && !options.ContainsKey("operations")) selected = ArchiveRecoveryProbe.Operations;
            if (suite == "recovery" && selected.Any(value => !ArchiveRecoveryProbe.AllOperations.Contains(value)))
                throw new ArgumentException("The recovery suite supports only archive read stages.");
            if (suite == "batcharchives" && !options.ContainsKey("operations")) selected = ArchiveBatchProbe.Operations;
            if (suite == "batcharchives" && selected.Any(value => !ArchiveBatchProbe.Operations.Contains(value)))
                throw new ArgumentException("The batcharchives suite supports only signed archive batch operations.");
            if (suite == "changesets" && !options.ContainsKey("operations")) selected = ChangeSetBatchProbe.Operations;
            if (suite == "changesets" && selected.Any(value => !ChangeSetBatchProbe.Operations.Contains(value)))
                throw new ArgumentException("The changesets suite supports only batch encoding operations.");
            var output = Path.GetFullPath(options.GetValueOrDefault("output", "benchmark-results.json"));
            // Preserve prior measurement evidence; select another output name for a rerun.
            if (File.Exists(output)) throw new IOException("The result file already exists: " + output);
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            var source = FindRepository();
            var environment = new {
                TimestampUTC = DateTimeOffset.UtcNow, Label = options.GetValueOrDefault("label", "measurement"), Suite = suite,
                SamplesPerProcess = samples, ProcessesPerOperation = processes, WarmupsPerProcess = warmups,
                Runtime = RuntimeInformation.FrameworkDescription, OS = RuntimeInformation.OSDescription,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(), LogicalProcessors = Environment.ProcessorCount,
                ServerGC = GCSettings.IsServerGC, GCLatency = GCSettings.LatencyMode.ToString(),
                RuntimeTuning = new { TieredCompilation = Environment.GetEnvironmentVariable("DOTNET_TieredCompilation"),
                    TieredPGO = Environment.GetEnvironmentVariable("DOTNET_TieredPGO"), ReadyToRun = Environment.GetEnvironmentVariable("DOTNET_ReadyToRun") },
                Cpu = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"),
                BuildConfiguration = typeof(Program).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration,
                SourceRevision = Command("git", source, "rev-parse", "HEAD"), SourceStatus = Command("git", source, "status", "--porcelain"),
                ProductionSourceSHA256 = Fingerprint(Path.Combine(source, "WWCP_POI")),
                BenchmarkSourceSHA256 = Fingerprint(Path.Combine(source, "WWCP_POI_Benchmarks")),
                ProductionAssemblySHA256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(RoamingNetworkHistory).Assembly.Location))),
                Dependencies = new[] { "Styx", "Hermod", "WWCP_Core" }.Select(name => new {
                    Name = name, Revision = Command("git", Path.Combine(source, "..", name), "rev-parse", "HEAD"),
                    Status = Command("git", Path.Combine(source, "..", name), "status", "--porcelain") }).ToArray()
            };
            var runs = new List<RunResult>();
            foreach (var shape in shapes)
            foreach (var operation in selected.Where(value => value != "cold-read" || shape.Retentions > 0))
            for (var repeat = 0; repeat < processes; repeat++)
            {
                var watch = Stopwatch.StartNew();
                var executable = Environment.ProcessPath!;
                var launch = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
                if (Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                    launch.ArgumentList.Add(typeof(Program).Assembly.Location);
                foreach (var arg in new[] { "--worker", "--shape", JsonSerializer.Serialize(shape), "--operation", operation, "--samples", samples.ToString(CultureInfo.InvariantCulture), "--warmups", warmups.ToString(CultureInfo.InvariantCulture) })
                    launch.ArgumentList.Add(arg);
                using var child = Process.Start(launch)!;
                var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync();
                child.WaitForExit();
                if (child.ExitCode != 0) throw new InvalidOperationException($"{shape.Name}/{operation} failed: {stderr.GetAwaiter().GetResult()}");
                var result = JsonSerializer.Deserialize<RunResult>(stdout.GetAwaiter().GetResult())!;
                var prior = runs.FirstOrDefault(value => value.Shape == shape && value.Operation == operation);
                if (prior is not null && (prior.StaticIdentity != result.StaticIdentity || prior.ArchiveIdentity != result.ArchiveIdentity ||
                    prior.Samples[0].Result != result.Samples[0].Result)) throw new InvalidOperationException("Content differs across isolated processes.");
                runs.Add(result);
                // Persist completed results after each worker, including when a later workload fails.
                File.WriteAllText(output, JsonSerializer.Serialize(new { Environment = environment, Complete = false, Runs = runs }, JSON));
                Console.WriteLine($"{shape.Name,-15} {operation,-20} process {repeat + 1}/{processes}: {Median(result.Samples.Select(value => value.ElapsedMilliseconds)):F2} ms ({watch.Elapsed.TotalSeconds:F1}s including setup)");
            }
            File.WriteAllText(output, JsonSerializer.Serialize(new { Environment = environment, Complete = true, Runs = runs }, JSON));
            Console.WriteLine("Saved " + output); return 0;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }

    private static Dictionary<String, String> Options(String[] args)
    {
        var values = new Dictionary<String, String>(StringComparer.Ordinal);
        var names = new HashSet<String>(["help", "worker", "shape", "operation", "suite", "samples", "processes", "warmups", "label", "output",
            "operations", "evses", "changes", "branches", "snapshot-every", "retentions", "locations",
            "compare-before", "compare-after", "encoding-report", "summary"], StringComparer.Ordinal);
        for (var i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal) || !names.Contains(args[i][2..])) throw new ArgumentException("Unknown option: " + args[i]);
            var name = args[i][2..];
            values.Add(name, name is "worker" or "help" ? "true" : ++i < args.Length ? args[i] : throw new ArgumentException("Missing value: " + name));
        }
        return values;
    }

    private static Int32 Number(Dictionary<String, String> options, String name, Int32 fallback, Int32 minimum, Int32 maximum)
    {
        var value = options.TryGetValue(name, out var text) ? Int32.Parse(text, CultureInfo.InvariantCulture) : fallback;
        if (value < minimum || value > maximum) throw new ArgumentOutOfRangeException(name);
        return value;
    }

    private static Double Median(IEnumerable<Double> values)
    {
        var sorted = values.Order().ToArray(); return sorted[sorted.Length / 2];
    }

    private static String FindRepository()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "WWCP_POI")) && File.Exists(Path.Combine(current.FullName, "docs", "ROADMAP.md"))) return current.FullName;
            current = current.Parent;
        }
        throw new DirectoryNotFoundException("Run from a build inside the WWCP_POI repository.");
    }

    private static String Fingerprint(String directory)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Where(file => file.EndsWith(".cs", StringComparison.Ordinal) || file.EndsWith(".csproj", StringComparison.Ordinal))
            .Where(file => !Path.GetRelativePath(directory, file).Split(Path.DirectorySeparatorChar).Any(segment => segment is "bin" or "obj"))
            .OrderBy(file => Path.GetRelativePath(directory, file), StringComparer.Ordinal))
        {
            hash.AppendData(Encoding.UTF8.GetBytes(Path.GetRelativePath(directory, file).Replace('\\', '/') + "\0"));
            hash.AppendData(File.ReadAllBytes(file)); hash.AppendData([0]);
        }
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private static String Command(String executable, String directory, params String[] args)
    {
        var info = new ProcessStartInfo(executable) { WorkingDirectory = directory, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in args) info.ArgumentList.Add(arg);
        using var process = Process.Start(info)!;
        var text = process.StandardOutput.ReadToEnd(); var error = process.StandardError.ReadToEnd(); process.WaitForExit();
        return process.ExitCode == 0 ? text.TrimEnd() : "unavailable: " + error.Trim();
    }
}
