/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// The operational schedule being updated.
/// </summary>
public enum POIRuntimeStatusKind { Status, AdminStatus }

/// <summary>
/// Insert a status entry or explicitly replace its entire history with one entry.
/// </summary>
public enum POIRuntimeUpdateMode { Insert, ReplaceHistory }

/// <summary>
/// An immutable status value with an explicit UTC-normalized timestamp.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record POIRuntimeStatusValue
{
    /// <summary>
    /// Create a status value; no local-clock timestamp is inferred.
    /// </summary>
    [JsonConstructor]
    public POIRuntimeStatusValue(String value, DateTimeOffset timestamp)
    {
        if (String.IsNullOrWhiteSpace(value)) throw new ArgumentException("A status value is required.", nameof(value));
        Value = value;
        Timestamp = timestamp.ToUniversalTime();
    }

    /// <summary>
    /// The domain status label, validated against the resolved entity on application.
    /// </summary>
    [JsonInclude, JsonRequired]
    public String Value { get; private init; }

    /// <summary>
    /// The explicit timestamp of the observed or scheduled status.
    /// </summary>
    [JsonInclude, JsonRequired, JsonConverter(typeof(POIRuntimeTimestampConverter))]
    public DateTimeOffset Timestamp { get; private init; }

    /// <summary>
    /// Capture a typed timestamped domain status without retaining a mutable entity.
    /// </summary>
    public static POIRuntimeStatusValue From<T>(Timestamped<T> value) where T : IComparable
        => new(value.Value.ToString()!, value.Timestamp);
}

internal sealed class POIRuntimeTimestampConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String) throw new JsonException("A timestamp string with an explicit UTC offset is required.");
        try
        {
            return InfrastructureJson.Date(new JObject(new JProperty("timestamp", reader.GetString())), "timestamp") ??
                   throw new JsonException("A timestamp is required.");
        }
        catch (ArgumentException exception) { throw new JsonException(exception.Message, exception); }
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
}

/// <summary>
/// One immutable runtime instruction. Application mutates a schedule, not the static POI version.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RoamingNetworkRuntimeUpdate
{
    /// <summary>
    /// Create a scoped runtime update with optional current-status and static-content preconditions.
    /// </summary>
    [JsonConstructor]
    public RoamingNetworkRuntimeUpdate(String roamingNetworkId, POIRuntimeTarget target,
                                       POIRuntimeStatusKind kind, POIRuntimeStatusValue newStatus,
                                       POIRuntimeStatusValue? expectedStatus = null,
                                       ImmutableArray<ETag> staticETags = default,
                                       POIRuntimeUpdateMode mode = POIRuntimeUpdateMode.Insert)
    {
        RoamingNetworkId = RoamingNetwork_Id.Parse(roamingNetworkId).ToString();
        Target = target ?? throw new ArgumentNullException(nameof(target));
        NewStatus = newStatus ?? throw new ArgumentNullException(nameof(newStatus));
        if (!Enum.IsDefined(kind) || !Enum.IsDefined(mode)) throw new ArgumentException("Invalid runtime update kind or mode.");
        Kind = kind;
        Mode = mode;
        ExpectedStatus = expectedStatus;
        StaticETags = staticETags.IsDefaultOrEmpty ? [] : ETag.ValidatePair(staticETags, nameof(staticETags));
    }

    /// <summary>
    /// The network containing the target.
    /// </summary>
    [JsonInclude, JsonRequired]
    public String RoamingNetworkId { get; private init; }

    /// <summary>
    /// The existing entity identity and ownership scope.
    /// </summary>
    [JsonInclude, JsonRequired]
    public POIRuntimeTarget Target { get; private init; }

    /// <summary>
    /// The operational or administrative schedule to update.
    /// </summary>
    [JsonInclude, JsonRequired, JsonConverter(typeof(POIRuntimeEnumConverter<POIRuntimeStatusKind>))]
    public POIRuntimeStatusKind Kind { get; private init; }

    /// <summary>
    /// The explicit timestamped value to insert.
    /// </summary>
    [JsonInclude, JsonRequired]
    public POIRuntimeStatusValue NewStatus { get; private init; }

    /// <summary>
    /// An optional expected current status, including its timestamp.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public POIRuntimeStatusValue? ExpectedStatus { get; }

    /// <summary>
    /// Optional expected static network identifiers; an empty array means no content precondition.
    /// </summary>
    public ImmutableArray<ETag> StaticETags { get; }

    /// <summary>
    /// Insert preserves history; ReplaceHistory explicitly resets it to the supplied entry.
    /// </summary>
    [JsonConverter(typeof(POIRuntimeEnumConverter<POIRuntimeUpdateMode>))]
    public POIRuntimeUpdateMode Mode { get; }
}
