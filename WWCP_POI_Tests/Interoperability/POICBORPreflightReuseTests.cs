/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Buffers;
using System.Text.Json;
using cloud.charging.open.protocols.WWCP.POI;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using org.GraphDefined.Vanaheimr.Illias;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture, NonParallelizable]
public sealed class POICBORPreflightReuseTests
{
    [Test, Combinatorial]
    public void Reuse_keeps_each_occurrences_writer_and_reader_depth_checks(
        [Values(0, 1, 4)] Int32 envelopeDepth, [Values(59, 60, 61, 62, 63, 64)] Int32 count,
        [Values("measurement", "etag-hex", "etag-base64")] String scalar)
    {
        var tags = new JArray(ETag.Compute(ETagFormat.JSON, [1]).ToJSON(scalar == "etag-base64" ? ETagDigestEncoding.Base64 : ETagDigestEncoding.HEX),
                             ETag.Compute(ETagFormat.CBOR, [2]).ToJSON(scalar == "etag-base64" ? ETagDigestEncoding.Base64 : ETagDigestEncoding.HEX));
        var json = scalar == "measurement" ? "{\"alt\":\"1.10 m\"}" : new JObject(new JProperty("ETags", tags)).ToString();
        for (var index = 0; index < count; index++) json = "{\"geoLocation\":" + json + "}";
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 128 });
        Byte[]? reference = null;
        try
        {
            var output = new ArrayBufferWriter<Byte>(); new POICBORWriter(output).Write(document.RootElement, "GeoLocation");
            var reader = new CBORReader(output.WrittenMemory.ToArray(), new CBORReaderOptions { MaxDepth = 64 - envelopeDepth });
            reader.SkipValue(); reference = output.WrittenSpan.ToArray();
        }
        catch (CBORException) { }
        var cache = new POICBORReadingCache(); cache.Read("1.10 m", "/warm");
        var actual = new ArrayBufferWriter<Byte>();
        void Encode()
        {
            new POICBORPreflight(64 - envelopeDepth, cache).Validate(document.RootElement, "GeoLocation");
            new POICBORWriter(actual, cache).Write(document.RootElement, "GeoLocation");
        }
        if (reference is null)
        {
            Assert.Throws<CBORException>(Encode);
            Assert.That(actual.WrittenCount, Is.Zero);
        }
        else { Encode(); Assert.That(actual.WrittenSpan.ToArray(), Is.EqualTo(reference)); }
        Assert.That(cache.Entries, Is.EqualTo(1));
    }

    [TestCase(128)] [TestCase(129)]
    public void Completed_and_failed_maps_clear_names_and_discard_oversized_sets(Int32 count)
    {
        var fields = String.Join(",", Enumerable.Range(0, count).Select(index => $"\"key{index}\":{index}"));
        using var valid = JsonDocument.Parse("{" + fields + "}");
        using var duplicate = JsonDocument.Parse("{" + fields + ",\"key0\":0}");
        var preflight = new POICBORPreflight(60);
        preflight.Validate(valid.RootElement, nameof(EVSE));
        Assert.That(preflight.PooledMapSets, Is.EqualTo(count == 128 ? 1 : 0));
        Assert.Throws<CBORException>(() => preflight.Validate(duplicate.RootElement, nameof(EVSE)));
        Assert.That(preflight.PooledMapSets, Is.EqualTo(count == 128 ? 1 : 0));
        preflight.Validate(valid.RootElement, nameof(EVSE));
        using var sibling = JsonDocument.Parse("[{\"key0\":0},{\"key0\":1}]");
        Assert.That(() => preflight.Validate(sibling.RootElement, nameof(EVSE)), Throws.Nothing);
    }

    [TestCase("{\"a\":1,\"\\u0061\":2}")]
    [TestCase("{\"a\":{\"b\":0},\"a\":1}")]
    [TestCase("{\"geoLocation\":{\"alt\":\"invalid\"}}")]
    [TestCase("{\"ETags\":[]}")]
    [TestCase("{\"value\":1e65537}")]
    public void Failed_validation_and_reader_limit_do_not_poison_valid_retry(String json)
    {
        var cache = new POICBORReadingCache(); var preflight = new POICBORPreflight(2, cache);
        using var bad = JsonDocument.Parse(json);
        Assert.Catch<Exception>(() => preflight.Validate(bad.RootElement, nameof(EVSE)));
        using var deep = JsonDocument.Parse("{\"customData\":[[1]]}");
        Assert.Throws<CBORException>(() => preflight.Validate(deep.RootElement, nameof(EVSE)));
        using var valid = JsonDocument.Parse("{\"customData\":{\"a\":0}}");
        Assert.That(() => preflight.Validate(valid.RootElement, nameof(EVSE)), Throws.Nothing);
    }

    [Test]
    public void Repeated_schema_readings_reuse_scalars_without_interpreting_customer_fields()
    {
        const String json = "{\"maxPower\":\"250 kW\",\"EVSEs\":[{\"maxPower\":\"250 kW\"},{\"maxPower\":\"251 kW\"}],\"customData\":{\"maxPower\":\"invalid\",\"ETags\":[],\"EVSEs\":[{\"maxPower\":\"invalid\"}]}}";
        using var document = JsonDocument.Parse(json);
        var cache = new POICBORReadingCache();
        new POICBORPreflight(60, cache).Validate(document.RootElement, nameof(ChargingStation));
        Assert.That(cache.Entries, Is.EqualTo(2));
        var expected = new ArrayBufferWriter<Byte>(); new POICBORWriter(expected).Write(document.RootElement, nameof(ChargingStation));
        var actual = new ArrayBufferWriter<Byte>(); new POICBORWriter(actual, cache).Write(document.RootElement, nameof(ChargingStation));
        Assert.That(actual.WrittenSpan.ToArray(), Is.EqualTo(expected.WrittenSpan.ToArray()));
        Assert.That(cache.Entries, Is.EqualTo(2));
        // The same JSON/text has no metrology meaning in an unknown schema context.
        var generic = new ArrayBufferWriter<Byte>(); new POICBORWriter(generic, cache).Write(document.RootElement, "Customer");
        var genericExpected = new ArrayBufferWriter<Byte>(); new POICBORWriter(genericExpected).Write(document.RootElement, "Customer");
        Assert.That(generic.WrittenSpan.ToArray(), Is.EqualTo(genericExpected.WrittenSpan.ToArray()));
        Assert.That(new POICBORReadingCache().Entries, Is.Zero);
    }

    [Test]
    public void Cache_entry_limit_preserves_hits_and_overflow_output()
    {
        var cache = new POICBORReadingCache();
        for (var index = 0; index < 40; index++)
        {
            var text = $"{index} kW";
            var actual = cache.Read(text, "/power").ToByteArray(CBORWriterOptions.Canonical);
            Assert.That(MetrologicalValue.TryParse(text, out var reading, out _), Is.True);
            Assert.That(actual, Is.EqualTo(reading.ToCBOR().ToByteArray(CBORWriterOptions.Canonical)));
        }
        Assert.That(cache.Entries, Is.EqualTo(POICBORReadingCache.MaxEntries));
        var count = cache.TextCharacters; cache.Read("0 kW", "/again"); cache.Read("39 kW", "/overflow-again");
        Assert.That(cache.TextCharacters, Is.EqualTo(count));
        var error = Assert.Throws<ArgumentException>(() => cache.Read("invalid", "/failed"));
        Assert.That(error!.Message, Does.StartWith("/failed:"));
        Assert.That(cache.Entries, Is.EqualTo(32));
    }

    [TestCase(256)] [TestCase(257)]
    public void Individual_text_and_total_text_limits_do_not_change_encoding(Int32 length)
    {
        var cache = new POICBORReadingCache();
        for (var index = 0; index < 18; index++)
        {
            var text = ($"{index} kW").PadRight(length);
            var value = cache.Read(text, "/power");
            Assert.That(MetrologicalValue.TryParse(text, out var reading, out _), Is.True);
            Assert.That(value.ToByteArray(CBORWriterOptions.Canonical), Is.EqualTo(reading.ToCBOR().ToByteArray(CBORWriterOptions.Canonical)));
        }
        Assert.That(cache.Entries, Is.EqualTo(length == 256 ? 16 : 0));
        Assert.That(cache.TextCharacters, Is.EqualTo(length == 256 ? 4096 : 0));
    }

    [TestCase("nodes", 128, true)] [TestCase("nodes", 129, false)]
    [TestCase("text", 256, true)] [TestCase("text", 257, false)]
    [TestCase("bytes", 256, true)] [TestCase("bytes", 257, false)]
    public void Cached_tree_admission_has_exact_node_and_scalar_payload_limits(String kind, Int32 count, Boolean expected)
    {
        var value = kind switch {
            "nodes" => CBORValue.FromArray(Enumerable.Repeat(CBORValue.FromText("a"), count - 1)),
            "text" => CBORValue.FromText(new String('a', count)),
            _ => CBORValue.FromBytes(new Byte[count])
        };
        Assert.That(POICBORReadingCache.CanRetain(value), Is.EqualTo(expected));
    }
}
