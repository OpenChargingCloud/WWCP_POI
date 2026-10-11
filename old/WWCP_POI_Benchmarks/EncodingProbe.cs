/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Reflection;
using System.Text;
using System.Text.Json;
using cloud.charging.open.protocols.WWCP.POI;
using Newtonsoft.Json.Linq;
using org.GraphDefined.Vanaheimr.Illias;

namespace WWCP_POI_Benchmarks;

// Benchmark-only stage access: bind existing functions once, outside every measured call.
// Fail if their signatures change. No production API or duplicated encoding implementation.
internal sealed class EncodingProbe : IDisposable
{
    internal static readonly String[] Operations = ["snapshot-cbor", "snapshot-json-document",
        "snapshot-reading-paths", "snapshot-json-canonical", "snapshot-cbor-tree", "snapshot-etag-tree",
        "snapshot-cbor-write", "changeset-cbor", "changeset-json", "changeset-validate-paths",
        "changeset-cbor-tree", "changeset-etag-tree", "changeset-cbor-write"];

    private const String Kind = nameof(RoamingNetwork);
    private readonly Func<OperationResult> action;
    private readonly Action verify;
    private JsonDocument? document;

    internal Func<OperationResult> Action => action;
    internal void VerifyOutput() => verify();

    internal EncodingProbe(String operation, RoamingNetworkDataSnapshot snapshot, RoamingNetworkChangeSet batch)
    {
        if (operation.StartsWith("snapshot-", StringComparison.Ordinal))
        {
            var convert = Bind<Func<CBORValue, String, Boolean, CBORValue>>(typeof(POIRepresentation), "ConvertETagCBOR");
            var json = snapshot.ToJSONWithETags(IncludeVersionMetadata: true);
            var canonical = CanonicalJSON.ToUTF8Bytes(json);
            var paths = ReadingPaths(json);
            var tree = CBORJSON.ToCBOR(canonical, Options(paths));
            var tagged = convert(tree, Kind, true);
            var bytes = snapshot.ToCBOR(IncludeVersionMetadata: true);
            Equal(tagged.ToByteArray(CBORWriterOptions.Canonical), bytes);
            (action, verify) = operation switch {
                "snapshot-cbor" => Complete(() => snapshot.ToCBOR(IncludeVersionMetadata: true), bytes),
                "snapshot-json-document" => Stage(() => snapshot.ToJSONWithETags(IncludeVersionMetadata: true),
                    value => CanonicalJSON.ToUTF8Bytes(value), canonical, ETagFormat.JSON),
                "snapshot-reading-paths" => Paths(() => ReadingPaths(json), paths),
                "snapshot-json-canonical" => Stage(() => CanonicalJSON.ToUTF8Bytes(json), value => value, canonical, ETagFormat.JSON),
                "snapshot-cbor-tree" => Stage(() => CBORJSON.ToCBOR(canonical, Options(paths)),
                    value => value.ToByteArray(CBORWriterOptions.Canonical), tree.ToByteArray(CBORWriterOptions.Canonical)),
                "snapshot-etag-tree" => Stage(() => convert(tree, Kind, true),
                    value => value.ToByteArray(CBORWriterOptions.Canonical), bytes),
                "snapshot-cbor-write" => Stage(() => tagged.ToByteArray(CBORWriterOptions.Canonical), value => value, bytes),
                _ => throw new ArgumentException("Unknown snapshot stage: " + operation)
            };
        }
        else
        {
            var validate = Bind<Action<JsonElement>>(typeof(RoamingNetworkChangeSet), "ValidateSigningJSON");
            var pathsForBatch = typeof(RoamingNetworkChangeSet).GetMethod("MeasurementPaths", BindingFlags.Instance | BindingFlags.NonPublic)!
                .CreateDelegate<Func<HashSet<String>>>(batch);
            var encode = Bind<Func<JsonElement, String, HashSet<String>, CBORValue>>(typeof(RoamingNetworkChangeSet), "EncodeTransportJSON");
            var convert = Bind<Func<CBORValue, Boolean, CBORValue>>(typeof(RoamingNetworkChangeSet), "ConvertTransportETags");
            var json = JsonSerializer.SerializeToUtf8Bytes(batch);
            document = JsonDocument.Parse(json);
            validate(document.RootElement);
            var paths = pathsForBatch();
            var tree = encode(document.RootElement, "", paths);
            var tagged = convert(tree, true);
            var bytes = batch.ToCBOR();
            Equal(tagged.ToByteArray(CBORWriterOptions.Canonical), bytes);
            (action, verify) = operation switch {
                "changeset-cbor" => Complete(batch.ToCBOR, bytes),
                "changeset-json" => Stage(() => JsonSerializer.SerializeToUtf8Bytes(batch), value => value, json, ETagFormat.JSON),
                "changeset-validate-paths" => Paths(() => { validate(document.RootElement); return pathsForBatch(); }, paths),
                "changeset-cbor-tree" => Stage(() => encode(document.RootElement, "", paths),
                    value => value.ToByteArray(CBORWriterOptions.Canonical), tree.ToByteArray(CBORWriterOptions.Canonical)),
                "changeset-etag-tree" => Stage(() => convert(tree, true),
                    value => value.ToByteArray(CBORWriterOptions.Canonical), bytes),
                "changeset-cbor-write" => Stage(() => tagged.ToByteArray(CBORWriterOptions.Canonical), value => value, bytes),
                _ => throw new ArgumentException("Unknown ChangeSet stage: " + operation)
            };
        }
    }

    private static (Func<OperationResult>, Action) Complete(Func<Byte[]> encode, Byte[] expected)
    {
        Byte[]? latest = null;
        return (() => { latest = encode(); return Result(latest, ETagFormat.CBOR); }, () => Equal(latest!, expected));
    }

    private static (Func<OperationResult>, Action) Stage<T>(Func<T> compute, Func<T, Byte[]> encodeOutput,
        Byte[] expected, ETagFormat format = ETagFormat.CBOR)
    {
        T latest = default!;
        var identity = Result(expected, format);
        // The actual intermediate output is consumed and checked outside the timed/allocation window.
        return (() => { latest = compute(); return identity; }, () => Equal(encodeOutput(latest), expected));
    }

    private static (Func<OperationResult>, Action) Paths(Func<HashSet<String>> compute, HashSet<String> expected)
    {
        HashSet<String>? latest = null;
        var identity = new OperationResult(0, 0, $"paths:{expected.Count}:" +
            ETag.Compute(ETagFormat.JSON, Encoding.UTF8.GetBytes(String.Join("\n", expected.Order(StringComparer.Ordinal)))));
        return (() => { latest = compute(); return identity; }, () => {
            if (!expected.SetEquals(latest!)) throw new InvalidOperationException("Metrological schema paths changed.");
        });
    }

    private static CBORJSONOptions Options(HashSet<String> paths)
        => new() { DetectMetrologicalValues = (path, _) => paths.Contains(path) };

    private static readonly Action<JObject, String, String, Action<JObject, String, String>, Boolean> Visit =
        Bind<Action<JObject, String, String, Action<JObject, String, String>, Boolean>>(typeof(POIRepresentation), "Visit");
    private static readonly Func<String, String, Boolean> IsMeasurement =
        Bind<Func<String, String, Boolean>>(typeof(POIRepresentation), "IsMeasurement");

    // Exactly the path collection and validation loop in POIRepresentation.Encode, using its schema visitor.
    private static HashSet<String> ReadingPaths(JObject json)
    {
        var paths = new HashSet<String>(StringComparer.Ordinal);
        Visit(json, Kind, "", (value, kind, path) => {
            foreach (var property in value.Properties())
                if (property.Value.Type == JTokenType.String && IsMeasurement(kind, property.Name))
                {
                    if (!MetrologicalValue.TryParse(property.Value.Value<String>()!, out _, out var error))
                        throw new ArgumentException(error);
                    paths.Add(path + "/" + property.Name.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal));
                }
        }, false);
        return paths;
    }

    private static T Bind<T>(Type owner, String name) where T : Delegate
        => owner.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!.CreateDelegate<T>();

    private static OperationResult Result(Byte[] bytes, ETagFormat format)
        => new(bytes.Length, 0, ETag.Compute(format, bytes).ToString());

    private static void Equal(Byte[] actual, Byte[] expected)
    {
        if (!actual.AsSpan().SequenceEqual(expected)) throw new InvalidOperationException("Encoder stage changed exact output bytes.");
    }

    public void Dispose() => document?.Dispose();
}
