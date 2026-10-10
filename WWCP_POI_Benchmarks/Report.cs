/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Text;
using System.Text.Json;

namespace WWCP_POI_Benchmarks;

internal sealed record MeasurementReport(JsonElement Environment, Boolean Complete, RunResult[] Runs);

internal static class Report
{
    internal static void Encoding(String reportPath, String output)
    {
        var report = JsonSerializer.Deserialize<MeasurementReport>(File.ReadAllText(reportPath))!;
        if (!report.Complete || report.Runs.Length == 0) throw new ArgumentException("A complete encoding measurement is required.");
        var processes = report.Environment.GetProperty("ProcessesPerOperation").GetInt32();
        var samples = report.Environment.GetProperty("SamplesPerProcess").GetInt32();
        var groups = report.Runs.GroupBy(value => (value.Shape, value.Operation)).ToDictionary(value => value.Key, value => value.ToArray());
        var required = EncodingProbe.Operations.Concat(["archive-json", "archive-json-stream", "archive-cbor", "archive-cbor-stream"]).ToHashSet(StringComparer.Ordinal);
        var text = new StringBuilder("# Individual encoding and borrowed stream measurements\n\n");
        text.AppendLine($"Input: `{Path.GetFileName(reportPath)}`. {processes} isolated processes per operation, {samples} measured samples and {report.Environment.GetProperty("WarmupsPerProcess").GetInt32()} excluded warmups per process.");
        text.AppendLine("\nAll samples and inventories are checked. Buffered/stream archive bytes and digests must agree. Snapshot JSON document/canonical stages and each final CBOR stage must agree with their complete encoders. Intermediate tree stages retain textual ETags and have their own verified digests.");
        text.AppendLine("\nStage inputs are prebuilt; consuming/encoding and byte verification occur after the allocation/time window. Complete encoder and archive operations include counting and SHA-256. Stage medians must not be added or presented as fractions of complete calls. Managed peaks include retained setup inputs and post-checks from earlier calls; 10 ms sampling misses short-lived peaks. Lifetime working set includes setup and validation.");
        foreach (var shape in report.Runs.Select(value => value.Shape).Distinct())
        {
            var runs = report.Runs.Where(value => value.Shape == shape).ToArray();
            if (!runs.Select(value => value.Operation).ToHashSet(StringComparer.Ordinal).SetEquals(required))
                throw new InvalidOperationException("Encoding operations are missing or unexpected for " + shape.Name);
            var reference = runs[0];
            foreach (var run in runs)
                if (run.StaticIdentity != reference.StaticIdentity || run.ArchiveIdentity != reference.ArchiveIdentity ||
                    run.Entities != reference.Entities || run.RetainedCommits != reference.RetainedCommits ||
                    run.Receipts != reference.Receipts || run.CatalogIds != reference.CatalogIds || run.Samples.Length != samples)
                    throw new InvalidOperationException("Encoding workload inventory changed: " + shape.Name);
            foreach (var operation in required)
            {
                var group = groups[(shape, operation)];
                if (group.Length != processes || group.SelectMany(value => value.Samples).Any(value => value.Result != group[0].Samples[0].Result))
                    throw new InvalidOperationException("Encoding operation is incomplete or nondeterministic: " + operation);
            }
            OperationResult Result(String operation) => groups[(shape, operation)][0].Samples[0].Result;
            foreach (var pair in new[] {
                ("archive-json", "archive-json-stream"), ("archive-cbor", "archive-cbor-stream"),
                ("snapshot-json-document", "snapshot-json-canonical"),
                ("snapshot-cbor", "snapshot-etag-tree"), ("snapshot-cbor", "snapshot-cbor-write"),
                ("changeset-cbor", "changeset-etag-tree"), ("changeset-cbor", "changeset-cbor-write") })
                if (Result(pair.Item1) != Result(pair.Item2)) throw new InvalidOperationException("Encoding outputs differ: " + pair);
            text.AppendLine($"\n## {shape.Name}\n\nEVSEs: {shape.EVSEs}; graph nodes: {reference.Entities}; commits: {reference.RetainedCommits}; receipts: {reference.Receipts}; catalog IDs: {reference.CatalogIds}.");
            text.AppendLine("\n| Operation | Median ms | Min–max ms | Median CPU ms | Median allocation MiB | Largest sampled managed peak MiB | Output bytes | Max Gen0/1/2 |");
            text.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |");
            foreach (var operation in required)
            {
                var measured = groups[(shape, operation)].SelectMany(value => value.Samples).ToArray();
                text.AppendLine($"| {operation} | {Median(measured.Select(value => value.ElapsedMilliseconds)):F3} | {measured.Min(value => value.ElapsedMilliseconds):F3}–{measured.Max(value => value.ElapsedMilliseconds):F3} | {Median(measured.Select(value => value.CpuMilliseconds)):F2} | {MiB(Median(measured.Select(value => (Double) value.AllocatedBytes))):F4} | {MiB(measured.Max(value => value.SampledManagedPeakBytes)):F1} | {measured[0].Result.OutputBytes} | {measured.Max(value => value.Gen0)}/{measured.Max(value => value.Gen1)}/{measured.Max(value => value.Gen2)} |");
            }
        }
        var destination = Path.GetFullPath(output);
        if (File.Exists(destination)) throw new IOException("The encoding summary already exists: " + destination);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.WriteAllText(destination, text.ToString());
        Console.WriteLine("Verified encoding outputs and saved " + destination);
    }

    internal static void Compare(String beforePath, String afterPath, String output)
    {
        var before = JsonSerializer.Deserialize<MeasurementReport>(File.ReadAllText(beforePath))!;
        var after = JsonSerializer.Deserialize<MeasurementReport>(File.ReadAllText(afterPath))!;
        if (!before.Complete || !after.Complete) throw new ArgumentException("Both measurements must be complete.");
        var first = before.Runs.GroupBy(value => (value.Shape, value.Operation)).ToDictionary(value => value.Key, value => value.ToArray());
        var second = after.Runs.GroupBy(value => (value.Shape, value.Operation)).ToDictionary(value => value.Key, value => value.ToArray());
        if (!first.Keys.ToHashSet().SetEquals(second.Keys)) throw new ArgumentException("Measurements have different workload sets.");
        var text = new StringBuilder("# Measured comparison\n\n");
        text.AppendLine($"Before: `{Path.GetFileName(beforePath)}`; after: `{Path.GetFileName(afterPath)}`.");
        text.AppendLine("\nMedians include every measured sample across isolated processes. Peak columns are the largest sampled operation-window values; lifetime peaks include setup.");
        text.AppendLine("\nAll static identities, archive digests, operation identities and byte counts must match before this report is written. A smaller elapsed value is an observation for this run, not a performance guarantee.");
        foreach (var shape in first.Keys.Select(key => key.Shape).Distinct())
        {
            var observed = first.First(pair => pair.Key.Shape == shape).Value[0];
            text.AppendLine($"\n## {shape.Name}\n\nEVSEs: {shape.EVSEs}; graph nodes: {observed.Entities}; retained commits: {observed.RetainedCommits}; receipts: {observed.Receipts}; catalog IDs: {observed.CatalogIds}.");
            text.AppendLine("\n| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |");
            text.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
            foreach (var key in first.Keys.Where(key => key.Shape == shape))
            {
                var left = first[key]; var right = second[key]; var reference = left[0];
                foreach (var run in left.Concat(right))
                    if (run.StaticIdentity != reference.StaticIdentity || run.ArchiveIdentity != reference.ArchiveIdentity ||
                        run.Entities != reference.Entities || run.RetainedCommits != reference.RetainedCommits ||
                        run.Receipts != reference.Receipts || run.CatalogIds != reference.CatalogIds ||
                        run.Samples.Any(value => value.Result != reference.Samples[0].Result))
                        throw new InvalidOperationException("Content or byte count changed: " + key);
                var oldSamples = left.SelectMany(run => run.Samples).ToArray();
                var newSamples = right.SelectMany(run => run.Samples).ToArray();
                var oldTime = Median(oldSamples.Select(sample => sample.ElapsedMilliseconds));
                var newTime = Median(newSamples.Select(sample => sample.ElapsedMilliseconds));
                text.AppendLine($"| {key.Operation} | {oldTime:F2} | {newTime:F2} | {100 * (1 - newTime / oldTime):F1}% | {MiB(Median(oldSamples.Select(sample => (Double) sample.AllocatedBytes))):F3} | {MiB(Median(newSamples.Select(sample => (Double) sample.AllocatedBytes))):F3} | {MiB(newSamples.Max(sample => sample.SampledManagedPeakBytes)):F1} | {newSamples[0].Result.OutputBytes} | {newSamples[0].Result.WrittenBytes} |");
            }
        }
        var destination = Path.GetFullPath(output);
        if (File.Exists(destination)) throw new IOException("The comparison already exists: " + destination);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.WriteAllText(destination, text.ToString());
        Console.WriteLine("Verified identical content and saved " + destination);
    }

    private static Double MiB(Double value) => value / (1024 * 1024);

    private static Double Median(IEnumerable<Double> values)
    {
        var sorted = values.Order().ToArray();
        return sorted.Length % 2 == 0 ? (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2 : sorted[sorted.Length / 2];
    }
}
