/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */
using System.Globalization;
using System.Text.Json;
using cloud.charging.open.protocols.WWCP.POI;
using org.GraphDefined.Vanaheimr.Illias;
namespace WWCP_POI_Tests.Interoperability;

// Frozen preceding canonical factories and unsigned writers; no preparation helper is used.
internal static class CanonicalPreparationOracle
{
    internal static Byte[] Identity(RoamingNetworkCommit value)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) WriteIdentity(writer, value);
        using var document = JsonDocument.Parse(stream.ToArray());
        return CanonicalJSON.ToUTF8Bytes(document);
    }
    private static String Required(String value, String parameterName)
        => !String.IsNullOrWhiteSpace(value) ? value : throw new ArgumentException("Value must not be empty.", parameterName);
    private static void WriteIdentity(Utf8JsonWriter writer, RoamingNetworkCommit value)
    {
        writer.WriteStartObject();
        writer.WriteString("Profile", value.Profile);
        writer.WriteString("ContentProfile", value.ContentProfile);
        writer.WriteString("RoamingNetworkId", value.RoamingNetworkId);
        writer.WriteNumber("Revision", value.Revision);
        writer.WritePropertyName("Parents");
        writer.WriteStartArray();
        foreach (var parent in value.Parents) parent.Hash.WriteTo(writer);
        writer.WriteEndArray();
        writer.WritePropertyName("StateETags");
        writer.WriteStartArray();
        foreach (var tag in value.StateETags) tag.WriteTo(writer);
        writer.WriteEndArray();
        writer.WriteString("AppliedChangeSetId", value.AppliedChangeSetId);
        if (value.Snapshot is { } snapshot)
        {
            writer.WritePropertyName("Snapshot"); snapshot.WriteTo(writer);
        }
        else
        {
            writer.WritePropertyName("ChangeSet");
            if (value.ChangeSet is null) writer.WriteNullValue();
            else WriteUnsignedContent(writer, value.ChangeSet);
        }
        writer.WriteEndObject();
    }

    private static void WriteUnsignedContent(Utf8JsonWriter writer, RoamingNetworkChangeSet value)
    {
        writer.WriteStartObject();
        writer.WriteString("Id", value.Id);
        writer.WriteString("RoamingNetworkId", value.RoamingNetworkId);
        writer.WriteNumber("BaseRevision", value.BaseRevision);
        writer.WriteString("CreatedAt", value.CreatedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        WriteSigningETags(writer, "BeforeETags", value.BeforeETags);
        WriteSigningETags(writer, "AfterETags", value.AfterETags);
        writer.WritePropertyName("Description");
        writer.WriteStartObject();
        foreach (var entry in value.Description)
            writer.WriteString(entry.Key, entry.Value);
        writer.WriteEndObject();
        writer.WritePropertyName("Metadata");
        writer.WriteStartObject();
        foreach (var entry in value.Metadata)
        {
            writer.WritePropertyName(entry.Key);
            ValidateSigningJSON(entry.Value);
            entry.Value.WriteTo(writer);
        }
        writer.WriteEndObject();
        writer.WritePropertyName("Changes");
        writer.WriteStartArray();
        foreach (var operation in value.Changes)
        {
            writer.WriteStartObject();
            writer.WriteString("Kind", operation.Kind.ToString());
            writer.WriteString("EntityType", operation.EntityType);
            writer.WriteString("EntityId", operation.EntityId);
            if (operation.PropertyName is { } property)
                writer.WriteString("PropertyName", property);
            if (operation.ParentEntityType is { } parentType)
                writer.WriteString("ParentEntityType", parentType);
            if (operation.ParentEntityId is { } parentId)
                writer.WriteString("ParentEntityId", parentId);
            writer.WritePropertyName("ElementPath");
            writer.WriteStartArray();
            foreach (var segment in operation.ElementPath)
            {
                writer.WriteStartObject();
                writer.WriteString("PropertyName", segment.PropertyName);
                if (segment.ElementId is { } elementId)
                    writer.WriteString("ElementId", elementId);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            WriteSigningValue(writer, "OldValue", operation.OldValue);
            WriteSigningValue(writer, "NewValue", operation.NewValue);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteSigningETags(Utf8JsonWriter writer, String field,
                                           System.Collections.Immutable.ImmutableArray<ETag> tags)
    {
        writer.WritePropertyName(field);
        writer.WriteStartArray();
        foreach (var tag in tags)
            tag.WriteTo(writer);
        writer.WriteEndArray();
    }

    private static void WriteSigningValue(Utf8JsonWriter writer, String field, JsonElement? value)
    {
        if (value is { } element)
        {
            ValidateSigningJSON(element);
            writer.WritePropertyName(field);
            element.WriteTo(writer);
        }
    }

    internal static void ValidateSigningJSON(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Undefined)
            throw new ArgumentException("Undefined JSON values cannot be signed or stored as metadata.");
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<String>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                if (!names.Add(property.Name))
                    throw new ArgumentException($"Duplicate JSON property '{property.Name}' is not allowed in signed content.");
                ValidateSigningJSON(property.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray())
                ValidateSigningJSON(item);
    }

    internal static Byte[] Signing(RoamingNetworkChangeSet value, COSEAlgorithm algorithm, String keyId)
    {
        if (!algorithm.IsSupportedForSigning)
            throw new ArgumentException("The algorithm is not supported for asymmetric signing.", nameof(algorithm));
        Required(keyId, nameof(keyId));

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("Profile", RoamingNetworkChangeSet.SigningProfile);
            writer.WriteString("Algorithm", algorithm.Name);
            writer.WriteString("KeyId", keyId);
            writer.WriteString("Encoding", "base64");
            writer.WritePropertyName("ChangeSet");
            WriteUnsignedContent(writer, value);
            writer.WriteEndObject();
        }
        using var document = JsonDocument.Parse(stream.ToArray());
        return CanonicalJSON.ToUTF8Bytes(document);
    }

    internal static Byte[] Signing(RoamingNetworkCommit value, COSEAlgorithm algorithm, String keyId)
    {
        if (!algorithm.IsSupportedForSigning) throw new ArgumentException("Unsupported signing algorithm.", nameof(algorithm));
        if (String.IsNullOrWhiteSpace(keyId)) throw new ArgumentException("KeyId must not be empty.", nameof(keyId));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("Profile", value.SignatureProfile);
            writer.WriteString("Algorithm", algorithm.Name);
            writer.WriteString("KeyId", keyId);
            writer.WriteString("Encoding", "base64");
            writer.WritePropertyName("CommitId"); value.Id.Hash.WriteTo(writer);
            writer.WritePropertyName("Commit"); WriteIdentity(writer, value);
            writer.WriteEndObject();
        }
        using var document = JsonDocument.Parse(stream.ToArray());
        return CanonicalJSON.ToUTF8Bytes(document);
    }
}
