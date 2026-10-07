/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// An immutable checkpoint or transition binding state, ordered ancestry and unsigned batch content.
/// Batch and commit peer signatures are retained independently of the commit identity.
/// </summary>
public sealed partial class RoamingNetworkCommit
{
    /// <summary>
    /// The domain-separated canonical JSON commit identity profile.
    /// </summary>
    public const String IdentityProfile = "wwcp-poi-commit-json-v1";

    /// <summary>
    /// The deterministic identity of this commit's unsigned content.
    /// </summary>
    public RoamingNetworkCommitId Id { get; }

    /// <summary>
    /// The identity profile carried by the wire document.
    /// </summary>
    public String Profile => IdentityProfile;

    /// <summary>
    /// The static content profile bound by this commit identity and its ancestry signatures.
    /// </summary>
    public String ContentProfile => POIContentProfile.Id;

    /// <summary>
    /// The network's wire identifier, inherited from the history checkpoint.
    /// </summary>
    public String RoamingNetworkId { get; }

    /// <summary>
    /// The resulting revision along the first-parent chain.
    /// </summary>
    public Int64 Revision { get; }

    /// <summary>
    /// Ordered predecessor identities; the first parent is the batch's source version.
    /// </summary>
    public ImmutableArray<RoamingNetworkCommitId> Parents { get; }

    /// <summary>
    /// The resulting static JSON and CBOR content identifiers.
    /// </summary>
    public ImmutableArray<ETag> StateETags { get; }

    /// <summary>
    /// The resulting snapshot's last applied batch identifier, including imported checkpoints.
    /// </summary>
    public String? AppliedChangeSetId { get; }

    /// <summary>
    /// The original batch with all peer envelopes, absent for the checkpoint.
    /// </summary>
    public RoamingNetworkChangeSet? ChangeSet { get; }

    /// <summary>
    /// Equal peer signatures authenticating the commit, including its ancestry.
    /// </summary>
    public ImmutableArray<RoamingNetworkChangeSetSignature> Signatures { get; }

    private RoamingNetworkCommit(String networkId, Int64 revision,
                                ImmutableArray<RoamingNetworkCommitId> parents, ImmutableArray<ETag> stateETags,
                                String? appliedChangeSetId, RoamingNetworkChangeSet? changeSet,
                                ImmutableArray<RoamingNetworkChangeSetSignature> signatures,
                                RoamingNetworkCommitId? declaredId = null)
    {
        InfrastructureChangeSchema.Id(InfrastructureEntityType.RoamingNetwork, networkId);
        if (revision < 0) throw new ArgumentOutOfRangeException(nameof(revision));
        if (parents.IsDefault || parents.Any(id => !id.IsValid) || parents.Distinct().Count() != parents.Length)
            throw new ArgumentException("Parents must be initialized, valid and distinct.", nameof(parents));
        if (parents.IsEmpty != (changeSet is null))
            throw new ArgumentException("Only a checkpoint has no parents and no ChangeSet.");
        if (appliedChangeSetId is not null && String.IsNullOrWhiteSpace(appliedChangeSetId))
            throw new ArgumentException("AppliedChangeSetId must be absent or nonempty.");
        RoamingNetworkId = networkId;
        Revision = revision;
        Parents = parents;
        StateETags = ETag.ValidatePair(stateETags, nameof(stateETags));
        AppliedChangeSetId = appliedChangeSetId;
        ChangeSet = changeSet;
        Signatures = signatures.IsDefault ? [] : signatures;
        if (Signatures.Any(signature => signature is null))
            throw new ArgumentException("Signatures must not contain null entries.");
        if (changeSet is not null &&
            (!InfrastructureChangeSchema.SameId(InfrastructureEntityType.RoamingNetwork, networkId, changeSet.RoamingNetworkId) ||
             revision != checked(changeSet.BaseRevision + 1) || appliedChangeSetId != changeSet.Id ||
             !StateETags.SequenceEqual(changeSet.AfterETags)))
            throw new ArgumentException("The commit header must match its batch's target state and revision.");
        Id = new(ETag.Compute(ETagFormat.JSON, GetIdentityBytes()));
        if (declaredId is { } expected && expected != Id)
            throw new ArgumentException("The declared commit identity does not match the content.");
    }

    /// <summary>
    /// Anchor an imported static snapshot without inventing a timestamp or earlier ancestry.
    /// </summary>
    public static RoamingNetworkCommit CreateCheckpoint(RoamingNetworkDataSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new(snapshot.Root.Id, snapshot.Revision, [], snapshot.ETags, snapshot.AppliedChangeSetId, null, []);
    }

    /// <summary>
    /// Prepare a transition header; history storage additionally validates and applies the batch.
    /// Additional parents record explicit ancestry and do not merge their data automatically.
    /// </summary>
    public static RoamingNetworkCommit Create(RoamingNetworkCommit parent, RoamingNetworkChangeSet changeSet,
                                              IEnumerable<RoamingNetworkCommitId>? additionalParents = null)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(changeSet);
        if (changeSet.BaseRevision != parent.Revision || !changeSet.BeforeETags.SequenceEqual(parent.StateETags))
            throw new ArgumentException("The batch must start at the first parent's revision and state.");
        return new(parent.RoamingNetworkId, checked(parent.Revision + 1),
                   ImmutableArray.Create(parent.Id).AddRange(additionalParents ?? []),
                   changeSet.AfterETags, changeSet.Id, changeSet, []);
    }

    /// <summary>
    /// Return a copy with one more peer signature and the same commit identity.
    /// </summary>
    public RoamingNetworkCommit WithSignature(RoamingNetworkChangeSetSignature signature)
    {
        ArgumentNullException.ThrowIfNull(signature);
        return WithSignatures(Signatures.Add(signature));
    }

    /// <summary>
    /// Replace the commit signature array without changing unsigned content.
    /// </summary>
    public RoamingNetworkCommit WithSignatures(ImmutableArray<RoamingNetworkChangeSetSignature> signatures)
        => new(RoamingNetworkId, Revision, Parents, StateETags, AppliedChangeSetId, ChangeSet, signatures, Id);

    /// <summary>
    /// Replace only batch peer envelopes, rejecting any change to identity-bearing content.
    /// </summary>
    public RoamingNetworkCommit WithChangeSet(RoamingNetworkChangeSet changeSet)
    {
        ArgumentNullException.ThrowIfNull(changeSet);
        if (ChangeSet is null) throw new InvalidOperationException("A checkpoint has no batch.");
        return new(RoamingNetworkId, Revision, Parents, StateETags, AppliedChangeSetId, changeSet, Signatures, Id);
    }

    /// <summary>
    /// Return the fixed profile's canonical unsigned UTF-8 JSON, independent of peer signatures.
    /// </summary>
    public Byte[] GetIdentityBytes()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) WriteIdentity(writer);
        using var document = JsonDocument.Parse(stream.ToArray());
        return CanonicalJSON.ToUTF8Bytes(document);
    }

    private void WriteIdentity(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("Profile", IdentityProfile);
        writer.WriteString("ContentProfile", ContentProfile);
        writer.WriteString("RoamingNetworkId", RoamingNetworkId);
        writer.WriteNumber("Revision", Revision);
        writer.WritePropertyName("Parents");
        writer.WriteStartArray();
        foreach (var parent in Parents) parent.Hash.WriteTo(writer);
        writer.WriteEndArray();
        writer.WritePropertyName("StateETags");
        writer.WriteStartArray();
        foreach (var tag in StateETags) tag.WriteTo(writer);
        writer.WriteEndArray();
        writer.WriteString("AppliedChangeSetId", AppliedChangeSetId);
        writer.WritePropertyName("ChangeSet");
        if (ChangeSet is null) writer.WriteNullValue();
        else ChangeSet.WriteUnsignedContent(writer);
        writer.WriteEndObject();
    }

    /// <summary>
    /// Serialize the complete commit and all batch/commit peer signatures as JSON.
    /// </summary>
    public String ToJSON()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) WriteTo(writer);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Write the complete fixed wire contract without caller naming-policy changes.
    /// </summary>
    public void WriteTo(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("Profile", IdentityProfile);
        writer.WriteString("ContentProfile", ContentProfile);
        writer.WritePropertyName("Id"); Id.Hash.WriteTo(writer);
        writer.WriteString("RoamingNetworkId", RoamingNetworkId);
        writer.WriteNumber("Revision", Revision);
        writer.WritePropertyName("Parents"); JsonSerializer.Serialize(writer, Parents);
        writer.WritePropertyName("StateETags"); JsonSerializer.Serialize(writer, StateETags);
        writer.WriteString("AppliedChangeSetId", AppliedChangeSetId);
        writer.WritePropertyName("ChangeSet"); JsonSerializer.Serialize(writer, ChangeSet);
        writer.WritePropertyName("Signatures"); JsonSerializer.Serialize(writer, Signatures);
        writer.WriteEndObject();
    }

    /// <summary>
    /// Decode a JSON commit, checking its header and deterministic identity but not application trust.
    /// </summary>
    public static RoamingNetworkCommit Parse(String json)
    {
        using var document = JsonDocument.Parse(json);
        return Parse(document.RootElement);
    }

    internal static RoamingNetworkCommit Parse(JsonElement json)
    {
        RequireFields(json, "Profile", "ContentProfile", "Id", "RoamingNetworkId", "Revision", "Parents", "StateETags",
                      "AppliedChangeSetId", "ChangeSet", "Signatures");
        if (json.GetProperty("Profile").GetString() != IdentityProfile)
            throw new ArgumentException("Unsupported commit identity profile.");
        POIContentProfile.Require(json.GetProperty("ContentProfile").GetString());
        foreach (var field in new[] { "Parents", "StateETags", "Signatures" })
            if (json.GetProperty(field).ValueKind != JsonValueKind.Array)
                throw new ArgumentException($"{field} must be an array.");
        var changeSet = json.GetProperty("ChangeSet");
        return new(json.GetProperty("RoamingNetworkId").GetString()!, json.GetProperty("Revision").GetInt64(),
                   JsonSerializer.Deserialize<ImmutableArray<RoamingNetworkCommitId>>(json.GetProperty("Parents")),
                   JsonSerializer.Deserialize<ImmutableArray<ETag>>(json.GetProperty("StateETags")),
                   json.GetProperty("AppliedChangeSetId").GetString(),
                   changeSet.ValueKind == JsonValueKind.Null ? null : JsonSerializer.Deserialize<RoamingNetworkChangeSet>(changeSet),
                   JsonSerializer.Deserialize<ImmutableArray<RoamingNetworkChangeSetSignature>>(json.GetProperty("Signatures")),
                   JsonSerializer.Deserialize<RoamingNetworkCommitId>(json.GetProperty("Id")));
    }

    /// <summary>
    /// Serialize as deterministic native CBOR with binary digests and the lossless batch codec.
    /// </summary>
    public Byte[] ToCBOR() => ToCBORValue().ToByteArray(CBORWriterOptions.Canonical);

    internal CBORValue ToCBORValue()
        => Map(("Profile", CBORValue.FromText(Profile)), ("ContentProfile", CBORValue.FromText(ContentProfile)), ("Id", Id.ToCBOR()),
               ("RoamingNetworkId", CBORValue.FromText(RoamingNetworkId)), ("Revision", CBORValue.FromInt64(Revision)),
               ("Parents", CBORValue.FromArray(Parents.Select(id => id.ToCBOR()))),
               ("StateETags", CBORValue.FromArray(StateETags.Select(tag => tag.ToCBOR()))),
               ("AppliedChangeSetId", AppliedChangeSetId is null ? CBORValue.Null : CBORValue.FromText(AppliedChangeSetId)),
               ("ChangeSet", ChangeSet is null ? CBORValue.Null : CBORValue.Parse(ChangeSet.ToCBOR())),
               ("Signatures", CBORValue.FromArray(Signatures.Select(signature => Map(
                   ("Profile", CBORValue.FromText(signature.Profile)), ("Algorithm", CBORValue.FromText(signature.Algorithm)),
                   ("KeyId", CBORValue.FromText(signature.KeyId)), ("Encoding", CBORValue.FromText(signature.Encoding)),
                   ("Value", CBORValue.FromText(signature.Value)))))));

    /// <summary>
    /// Decode a complete CBOR commit and validate its declared identity.
    /// </summary>
    public static RoamingNetworkCommit ParseCBOR(ReadOnlySpan<Byte> bytes) => ParseCBORValue(CBORValue.Parse(bytes));

    internal static RoamingNetworkCommit ParseCBORValue(CBORValue value)
    {
        var fields = Fields(value, "Profile", "ContentProfile", "Id", "RoamingNetworkId", "Revision", "Parents", "StateETags",
                            "AppliedChangeSetId", "ChangeSet", "Signatures");
        if (Text(fields["Profile"]) != IdentityProfile) throw new ArgumentException("Unsupported commit identity profile.");
        POIContentProfile.Require(Text(fields["ContentProfile"]));
        if (!fields["Revision"].TryGetInt64(out var revision)) throw new ArgumentException("Revision must be an Int64.");
        var signatures = fields["Signatures"].AsArray().Select(item => {
            var signature = Fields(item, "Profile", "Algorithm", "KeyId", "Encoding", "Value");
            return new RoamingNetworkChangeSetSignature(Text(signature["Algorithm"]), Text(signature["KeyId"]),
                Text(signature["Value"]), Text(signature["Profile"]), Text(signature["Encoding"]));
        }).ToImmutableArray();
        return new(Text(fields["RoamingNetworkId"]), revision,
                   fields["Parents"].AsArray().Select(item => new RoamingNetworkCommitId(ETag.Parse(item))).ToImmutableArray(),
                   fields["StateETags"].AsArray().Select(ETag.Parse).ToImmutableArray(),
                   fields["AppliedChangeSetId"].Kind == CBORValueKind.Null ? null : Text(fields["AppliedChangeSetId"]),
                   fields["ChangeSet"].Kind == CBORValueKind.Null ? null :
                       RoamingNetworkChangeSet.ParseCBOR(fields["ChangeSet"].ToByteArray(CBORWriterOptions.Canonical)),
                   signatures, new(ETag.Parse(fields["Id"])));
    }

    internal static void RequireFields(JsonElement value, params String[] expected)
    {
        RoamingNetworkChangeSet.ValidateSigningJSON(value);
        if (value.ValueKind != JsonValueKind.Object ||
            !value.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal).SetEquals(expected))
            throw new ArgumentException("The JSON object must contain exactly: " + String.Join(", ", expected));
    }

    internal static CBORValue Map(params (String Key, CBORValue Value)[] fields)
        => CBORValue.FromMap(fields.Select(field => new KeyValuePair<CBORValue, CBORValue>(CBORValue.FromText(field.Key), field.Value)));

    internal static Dictionary<String, CBORValue> Fields(CBORValue value, params String[] expected)
    {
        var result = new Dictionary<String, CBORValue>(StringComparer.Ordinal);
        foreach (var entry in value.AsMap())
            if (!result.TryAdd(Text(entry.Key), entry.Value)) throw new ArgumentException("Duplicate CBOR map key.");
        if (!result.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(expected))
            throw new ArgumentException("The CBOR map must contain exactly: " + String.Join(", ", expected));
        return result;
    }

    internal static String Text(CBORValue value)
        => value.TryGetText(out var text) ? text : throw new ArgumentException("Expected CBOR text.");
}
