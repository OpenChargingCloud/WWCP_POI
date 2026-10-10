using System.Buffers;
using System.Collections.Immutable;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using cloud.charging.open.protocols.WWCP.POI;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using org.GraphDefined.Vanaheimr.Illias;
using static WWCP_POI_Tests.Interoperability.InteropFixture;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture, NonParallelizable]
public sealed class DirectPOICBORTests
{
    [Test, Combinatorial]
    public void Complete_network_matches_previous_tree_encoder(
        [Values(false, true)] Boolean runtime, [Values(false, true)] Boolean version,
        [Values(ETagDigestEncoding.HEX, ETagDigestEncoding.Base64)] ETagDigestEncoding encoding)
    {
        var network = Network();
        var json = network.ToJSONWithETags(runtime, encoding, version);
        Assert.That(Encode(json, nameof(RoamingNetwork), null), Is.EqualTo(Reference(json, nameof(RoamingNetwork))));
        Assert.That(network.ToCBOR(runtime, version), Is.EqualTo(Reference(json, nameof(RoamingNetwork))));
        Assert.That(network.ToCanonicalCBOR(), Is.EqualTo(Reference(Content(network), nameof(RoamingNetwork))));
    }

    [TestCase("EVSE", "maxPower", "250 kW")]
    [TestCase("ChargingStation", "maxVoltage", "400 V")]
    [TestCase("ChargingPool", "maxCapacity", "1.10 kWh")]
    [TestCase("GridConnectionPoint", "nominalFrequency", "50 Hz")]
    [TestCase("GridConnectionPoint", "contractedImportApparentPower", "500 kVA")]
    [TestCase("ChargingCable", "resistance", "0.02 Ω")]
    [TestCase("ChargingCable", "length", "4.50 m")]
    [TestCase("ChargingProduct", "minDuration", "60 s")]
    [TestCase("ParkingProduct", "stopParkingAfterTime", "600 s")]
    [TestCase("ChargingTariffRestriction", "minEnergy", "1 kWh")]
    [TestCase("ChargingPriceComponent", "stepSize", "0.10 kWh")]
    [TestCase("EnergySource", "percentage", "50 %")]
    [TestCase("EnvironmentalImpact", "percentage", "25 %")]
    [TestCase("GeoLocation", "alt", "150 m")]
    [TestCase("AdditionalGeoLocation", "altitude", "150 m")]
    [TestCase("EVSE", "maxVoltageRealTime", "(230.00 ±0.12) V, k=2, p=0.95, dist=normal")]
    [TestCase("EVSE", "maxCurrent", "5.00 mA")]
    public void Native_metrological_values_match_tree(String kind, String field, String text)
    {
        var json = new JObject { [field] = text, ["customData"] = new JObject { [field] = text } };
        var bytes = Encode(json, kind, null);
        Assert.That(bytes, Is.EqualTo(Reference(json, kind)));
        var entries = CBORValue.Parse(bytes).AsMap();
        Assert.That(entries.Single(e => e.Key.AsText() == field).Value.Kind, Is.EqualTo(CBORValueKind.Tagged));
        Assert.That(entries.Single(e => e.Key.AsText() == "customData").Value.AsMap()[0].Value.Kind, Is.EqualTo(CBORValueKind.TextString));
    }

    [TestCase("-9223372036854775808")]
    [TestCase("9223372036854775807")]
    [TestCase("18446744073709551615")]
    [TestCase("18446744073709551616")]
    [TestCase("-9223372036854775809")]
    [TestCase("1.0")]
    [TestCase("1.10")]
    [TestCase("1e0")]
    [TestCase("1e4")]
    [TestCase("1e-300")]
    [TestCase("1e308")]
    [TestCase("-0")]
    [TestCase("-0.00")]
    [TestCase("123456789012345678901234567890.12345")]
    public void Exact_JSON_number_grammar_matches_tree(String number)
        => Assert.That(Raw("{\"number\":" + number + "}"), Is.EqualTo(CBORJSON.ToCBOR("{\"number\":" + number + "}").ToByteArray(CBORWriterOptions.Canonical)));

    [Test]
    public void Canonical_JSON_scalar_spelling_and_customer_fields_are_preserved()
    {
        var json = new JObject {
            ["decimal"] = new JValue(1.10m), ["double"] = new JValue(1e-300),
            ["big"] = new JValue(BigInteger.Pow(10, 40)), ["date"] = new JValue(InteropFixture.Time),
            ["bytes"] = new JValue(new Byte[] { 1, 2, 3 }), ["null"] = JValue.CreateNull(),
            ["é"] = false, ["e\u0301"] = true, ["😀"] = "unicode", ["a/~"] = "250 kW",
            ["customData"] = new JObject { ["ETags"] = "arbitrary", ["maxPower"] = "250 kW",
                ["geoLocation"] = new JObject { ["alt"] = "not a reading" } },
            ["EVSEs"] = new JArray { new JArray { new JObject { ["maxPower"] = "not a reading", ["ETags"] = "customer" } } }
        };
        Assert.That(Encode(json, nameof(ChargingStation), null), Is.EqualTo(Reference(json, nameof(ChargingStation))));
    }

    [TestCase(0)] [TestCase(23)] [TestCase(24)] [TestCase(255)] [TestCase(256)] [TestCase(65536)]
    public void Definite_array_counts_match_tree(Int32 count)
    {
        var json = new JObject { ["items"] = new JArray(Enumerable.Repeat(0, count)) };
        Assert.That(Encode(json, "Customer", null), Is.EqualTo(Reference(json, "Customer")));
    }

    [TestCase(150, 1)] [TestCase(150, 200)]
    public void Key_cache_saturation_preserves_canonical_order(Int32 count, Int32 length)
    {
        var json = new JObject();
        foreach (var i in Enumerable.Range(0, count).Reverse()) json[new String('x', length) + i] = new JObject { ["same"] = i };
        Assert.That(Encode(json, "Customer", null), Is.EqualTo(Reference(json, "Customer")));
    }

    [TestCase(60)] [TestCase(62)] [TestCase(63)] [TestCase(64)] [TestCase(65)]
    public void Container_and_metrological_tag_depth_limits_match_tree(Int32 depth)
    {
        foreach (var measurement in new[] { false, true })
        {
            var json = measurement ? new JObject { ["alt"] = "1.10 m" } : new JObject { ["value"] = 1 };
            for (var i = 1; i < depth; i++) json = new JObject { ["geoLocation"] = json };
            Byte[]? expected = null;
            try { expected = Reference(json, "GeoLocation"); } catch (Exception) { }
            if (expected is null) Assert.Catch<Exception>(() => Encode(json, "GeoLocation", null));
            else Assert.That(Encode(json, "GeoLocation", null), Is.EqualTo(expected));
        }
    }

    [TestCase("[]")]
    [TestCase("[[\"json\",\"sha256\",\"hex\",\"00\"],[\"cbor\",\"sha256\",\"hex\",\"00\"]]")]
    [TestCase("[1,2]")]
    [TestCase("[[\"json\",\"sha256\",\"hex\",null],[]]")]
    public void Invalid_schema_ETags_fail_and_customer_ETags_remain_ordinary(String text)
    {
        var json = new JObject { ["ETags"] = JToken.Parse(text) };
        Assert.Catch<ArgumentException>(() => Reference(json, nameof(EVSE)));
        Assert.Catch<ArgumentException>(() => Encode(json, nameof(EVSE), null));
        var customer = new JObject { ["customData"] = json };
        Assert.That(Encode(customer, nameof(EVSE), null), Is.EqualTo(Reference(customer, nameof(EVSE))));
    }

    [Test]
    public void Duplicate_keys_and_unsupported_exponents_are_rejected()
    {
        Assert.Throws<CBORException>(() => Raw("{\"x\":1,\"x\":2}"));
        Assert.Throws<CBORException>(() => Raw("{\"x\":1e-65537}"));
        Assert.Throws<CBORException>(() => Raw("{\"x\":1e65537}"));
    }

    [Test]
    public void Invalid_measurement_is_reported_before_nonfinite_customer_value()
    {
        var json = new JObject { ["maxPower"] = "invalid", ["customData"] = new JObject { ["number"] = Double.NaN } };
        Assert.That(Assert.Throws<ArgumentException>(() => Encode(json, nameof(EVSE), null))!.Message, Does.Contain("metrological"));
        Assert.That(Assert.Throws<ArgumentException>(() => Reference(json, nameof(EVSE)))!.Message, Does.Contain("metrological"));
    }

    private static Byte[] Raw(String json)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 64 });
        var buffer = new ArrayBufferWriter<Byte>(); new POICBORWriter(buffer).Write(document.RootElement, "Customer");
        return buffer.WrittenSpan.ToArray();
    }

    private static readonly Func<JObject, String, Byte[]?, Byte[]> Encode = typeof(POIRepresentation)
        .GetMethod("Encode", BindingFlags.Static | BindingFlags.NonPublic)!.CreateDelegate<Func<JObject, String, Byte[]?, Byte[]>>();
    private static readonly Func<IImmutablePOI, JObject> Content = typeof(POIRepresentation)
        .GetMethod("Content", BindingFlags.Static | BindingFlags.NonPublic)!.CreateDelegate<Func<IImmutablePOI, JObject>>();
    private static readonly Func<CBORValue, String, Boolean, CBORValue> ConvertTags = typeof(POIRepresentation)
        .GetMethod("ConvertETagCBOR", BindingFlags.Static | BindingFlags.NonPublic)!.CreateDelegate<Func<CBORValue, String, Boolean, CBORValue>>();

    // Previous complete-tree algorithm, independent of the new Encode/POICBORWriter implementation.
    private static Byte[] Reference(JObject json, String kind)
    {
        var paths = new HashSet<String>(StringComparer.Ordinal); var hasTags = false;
        POIRepresentation.Visit(json, kind, "", (node, type, path) => {
            hasTags |= node["ETags"] is not null;
            foreach (var field in node.Properties())
                if (field.Value.Type == JTokenType.String && POIRepresentation.IsMeasurement(type, field.Name))
                {
                    if (!MetrologicalValue.TryParse(field.Value.Value<String>()!, out _, out var error))
                        throw new ArgumentException($"{path}/{field.Name}: cannot encode a Styx metrological value: {error}");
                    paths.Add(path + "/" + field.Name.Replace("~", "~0").Replace("/", "~1"));
                }
        });
        var tree = CBORJSON.ToCBOR(CanonicalJSON.ToUTF8Bytes(json), new CBORJSONOptions {
            DetectMetrologicalValues = (path, _) => paths.Contains(path)
        });
        return (hasTags ? ConvertTags(tree, kind, true) : tree).ToByteArray(CBORWriterOptions.Canonical);
    }
}
