using System.Text.Json;
using NUnit.Framework;
using cloud.charging.open.protocols.WWCP.POI;

namespace WWCP_POI_Tests.Json;

[TestFixture]
public sealed class ChangeSetJsonTests
{
    [TestCase("null")]
    [TestCase("false")]
    [TestCase("0")]
    [TestCase("\"\"")]
    [TestCase("[1,2,3]")]
    [TestCase("{\"de\":\"Name\",\"en\":\"Name\"}")]
    public void Property_update_roundtrip_preserves_json_value_kind(string text)
    {
        using var document = JsonDocument.Parse(text);
        var source = RoamingNetworkChange.UpdateProperty("ChargingStation", "station-1", "name", null, document.RootElement);
        var restored = JsonSerializer.Deserialize<RoamingNetworkChange>(JsonSerializer.Serialize(source))!;
        Assert.That(restored.NewValue.HasValue, Is.True);
        Assert.That(JsonElement.DeepEquals(restored.NewValue!.Value, document.RootElement), Is.True);
        Assert.That(restored.OldValue.HasValue, Is.False);
    }

    [Test]
    public void Expected_json_null_is_distinct_from_no_precondition()
    {
        using var nullDocument = JsonDocument.Parse("null");
        using var newDocument = JsonDocument.Parse("\"B\"");
        var source = RoamingNetworkChange.UpdateProperty("ChargingStation", "station-1", "name",
                                                         nullDocument.RootElement, newDocument.RootElement);
        var restored = JsonSerializer.Deserialize<RoamingNetworkChange>(JsonSerializer.Serialize(source))!;
        Assert.That(restored.OldValue.HasValue, Is.True);
        Assert.That(restored.OldValue!.Value.ValueKind, Is.EqualTo(JsonValueKind.Null));
    }

    [Test]
    public void Remove_roundtrip_omits_values_instead_of_creating_null_payloads()
    {
        var text = JsonSerializer.Serialize(RoamingNetworkChange.Remove("ChargingStation", "station-1"));
        using var json = JsonDocument.Parse(text);
        Assert.That(json.RootElement.TryGetProperty("NewValue", out _), Is.False);
        Assert.That(json.RootElement.TryGetProperty("OldValue", out _), Is.False);
        var restored = JsonSerializer.Deserialize<RoamingNetworkChange>(text)!;
        Assert.That(restored.NewValue.HasValue, Is.False);
        Assert.That(restored.OldValue.HasValue, Is.False);
    }

    [Test]
    public void Operation_owns_payload_after_the_source_document_is_disposed()
    {
        RoamingNetworkChange source;
        using (var document = JsonDocument.Parse("{\"name\":\"A\"}"))
        {
            source = new RoamingNetworkChange(RoamingNetworkChangeKind.Add, "ChargingStation", "station-1",
                                              null, null, document.RootElement);
        }
        Assert.That(source.NewValue!.Value.GetProperty("name").GetString(), Is.EqualTo("A"));
        Assert.That(() => JsonSerializer.Serialize(source), Throws.Nothing);
    }

    [TestCase("Id")]
    [TestCase("RoamingNetworkId")]
    [TestCase("BaseRevision")]
    [TestCase("CreatedAt")]
    [TestCase("Changes")]
    public void Deserializer_rejects_missing_changeset_header_fields(string field)
    {
        var source = new RoamingNetworkChangeSet("change-1", "rn-1", 0, DateTimeOffset.UtcNow, []);
        var json = System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(source))!.AsObject();
        json.Remove(field);
        Assert.That(() => JsonSerializer.Deserialize<RoamingNetworkChangeSet>(json.ToJsonString()), Throws.TypeOf<JsonException>());
    }

    [TestCase("Kind")]
    [TestCase("EntityType")]
    [TestCase("EntityId")]
    public void Deserializer_rejects_missing_operation_fields(string field)
    {
        var json = System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(
            RoamingNetworkChange.Remove("ChargingStation", "station-1")))!.AsObject();
        json.Remove(field);
        Assert.That(() => JsonSerializer.Deserialize<RoamingNetworkChange>(json.ToJsonString()), Throws.TypeOf<JsonException>());
    }

    [Test]
    public void Deserializer_rejects_update_without_a_value()
    {
        const string json = "{\"Kind\":\"UpdateProperty\",\"EntityType\":\"ChargingStation\",\"EntityId\":\"station-1\",\"PropertyName\":\"name\"}";
        Assert.That(() => JsonSerializer.Deserialize<RoamingNetworkChange>(json), Throws.ArgumentException);
    }

    [Test]
    public void Deserializer_rejects_unknown_numeric_operation()
    {
        const string json = "{\"Kind\":999,\"EntityType\":\"ChargingStation\",\"EntityId\":\"station-1\"}";
        Assert.That(() => JsonSerializer.Deserialize<RoamingNetworkChange>(json), Throws.TypeOf<ArgumentOutOfRangeException>());
    }
}
