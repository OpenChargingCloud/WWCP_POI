/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using cloud.charging.open.protocols.WWCP.POI.CSM;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// An immutable approval or compatibility document for a station model/version and software releases.
/// This is a domain document, not a cryptographic certificate.
/// </summary>
public sealed class TransparencySoftwareCertificate : IImmutablePOI
{
    /// <summary>
    /// The stable identity of the document.
    /// </summary>
    public TransparencySoftwareCertificate_Id Id { get; }

    /// <summary>
    /// The issuing organization.
    /// </summary>
    public String Issuer { get; }

    /// <summary>
    /// The station model to which this document applies.
    /// </summary>
    public String ChargingStationModel { get; }

    /// <summary>
    /// The model version to which this document applies.
    /// </summary>
    public String ChargingStationModelVersion { get; }

    /// <summary>
    /// Optional reference to the network's manufacturer registry.
    /// </summary>
    public ChargingStationManufacturer_Id? ChargingStationManufacturerId { get; }

    /// <summary>
    /// The issuer's document number, if different from the POI identity.
    /// </summary>
    public String? DocumentNumber { get; }

    /// <summary>
    /// Optional location of the approval document.
    /// </summary>
    public URL? DocumentURL { get; }

    /// <summary>
    /// Software releases officially checked with this station model/version.
    /// </summary>
    public ImmutableArray<TransparencySoftware_Id> VerifiedTransparencySoftwareIds { get; }

    /// <summary>
    /// Compatible additional releases without a claim of official verification by this document.
    /// </summary>
    public ImmutableArray<TransparencySoftware_Id> CompatibleTransparencySoftwareIds { get; }

    /// <summary>
    /// Optional start of document validity; this is static metadata.
    /// </summary>
    public DateTimeOffset? NotBefore { get; }

    /// <summary>
    /// Optional end of document validity; this is static metadata.
    /// </summary>
    public DateTimeOffset? NotAfter { get; }

    /// <summary>
    /// Canonical static content identifiers of this document, including its reference IDs.
    /// </summary>
    public ImmutableArray<ETag> ETags => POIRepresentation.GetETags(this);

    /// <summary>
    /// Create a model/version document with explicit verified and compatible release assignments.
    /// </summary>
    public TransparencySoftwareCertificate(TransparencySoftwareCertificate_Id id, String issuer,
        String chargingStationModel, String chargingStationModelVersion,
        IEnumerable<TransparencySoftware_Id>? verifiedTransparencySoftwareIds = null,
        IEnumerable<TransparencySoftware_Id>? compatibleTransparencySoftwareIds = null,
        ChargingStationManufacturer_Id? chargingStationManufacturerId = null, String? documentNumber = null,
        URL? documentURL = null, DateTimeOffset? notBefore = null, DateTimeOffset? notAfter = null)
    {
        if (id.IsNullOrEmpty) throw new ArgumentException("A certificate document identifier is required.", nameof(id));
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(chargingStationModel);
        ArgumentException.ThrowIfNullOrWhiteSpace(chargingStationModelVersion);
        if (chargingStationManufacturerId is { } manufacturer && manufacturer.IsNullOrEmpty)
            throw new ArgumentException("Invalid manufacturer identifier.");
        if (documentURL is { } link && !Uri.TryCreate(link.ToString(), UriKind.Absolute, out _))
            throw new ArgumentException("documentURL must be absolute.");
        if (notBefore > notAfter) throw new ArgumentException("notBefore must not exceed notAfter.");
        VerifiedTransparencySoftwareIds = POIGraphJSON.ReferenceIds(verifiedTransparencySoftwareIds ?? [], InfrastructureEntityType.TransparencySoftware);
        CompatibleTransparencySoftwareIds = POIGraphJSON.ReferenceIds(compatibleTransparencySoftwareIds ?? [], InfrastructureEntityType.TransparencySoftware);
        if (VerifiedTransparencySoftwareIds.IsEmpty && CompatibleTransparencySoftwareIds.IsEmpty)
            throw new ArgumentException("A document must identify at least one software release.");
        if (VerifiedTransparencySoftwareIds.Intersect(CompatibleTransparencySoftwareIds).Any())
            throw new ArgumentException("A release cannot be both verified and merely compatible in the same document.");
        Id = id; Issuer = issuer; ChargingStationModel = chargingStationModel;
        ChargingStationModelVersion = chargingStationModelVersion; ChargingStationManufacturerId = chargingStationManufacturerId;
        DocumentNumber = documentNumber; DocumentURL = documentURL;
        NotBefore = notBefore?.ToUniversalTime(); NotAfter = notAfter?.ToUniversalTime();
    }

    /// <summary>
    /// Whether this document names the software release as verified or compatible.
    /// </summary>
    public Boolean Covers(TransparencySoftware_Id id)
        => VerifiedTransparencySoftwareIds.Contains(id) || CompatibleTransparencySoftwareIds.Contains(id);

    /// <summary>
    /// Export reference IDs and document metadata without expanding catalog entries.
    /// </summary>
    public JObject ToJSON()
    {
        var json = new JObject(new JProperty("@id", Id.ToString()), new JProperty("issuer", Issuer),
            new JProperty("chargingStationModel", ChargingStationModel), new JProperty("chargingStationModelVersion", ChargingStationModelVersion),
            new JProperty("verifiedTransparencySoftwareIds", new JArray(VerifiedTransparencySoftwareIds.Select(id => id.ToString()).Order(StringComparer.Ordinal))),
            new JProperty("compatibleTransparencySoftwareIds", new JArray(CompatibleTransparencySoftwareIds.Select(id => id.ToString()).Order(StringComparer.Ordinal))));
        if (ChargingStationManufacturerId is { } manufacturer) json["chargingStationManufacturerId"] = manufacturer.ToString();
        if (DocumentNumber is not null) json["documentNumber"] = DocumentNumber;
        if (DocumentURL is { } url) json["documentURL"] = url.ToString();
        if (NotBefore is { } start) json["notBefore"] = start.ToString("O", CultureInfo.InvariantCulture);
        if (NotAfter is { } end) json["notAfter"] = end.ToString("O", CultureInfo.InvariantCulture);
        return POIRepresentation.AddETags(this, json);
    }

    /// <summary>
    /// Parse document metadata and resolve all software and manufacturer references in the supplied version.
    /// </summary>
    public static TransparencySoftwareCertificate Parse(JObject json, RoamingNetwork network)
    {
        ArgumentNullException.ThrowIfNull(network);
        InfrastructureJson.ValidateFields(json, "@id", "issuer", "chargingStationModel", "chargingStationModelVersion",
            "chargingStationManufacturerId", "documentNumber", "documentURL", "verifiedTransparencySoftwareIds",
            "compatibleTransparencySoftwareIds", "notBefore", "notAfter");
        var result = new TransparencySoftwareCertificate(
            TransparencySoftwareCertificate_Id.Parse(TransparencyJson.RequiredText(json, "@id")),
            TransparencyJson.RequiredText(json, "issuer"), TransparencyJson.RequiredText(json, "chargingStationModel"),
            TransparencyJson.RequiredText(json, "chargingStationModelVersion"),
            InfrastructureJson.Array(json, "verifiedTransparencySoftwareIds", token => TransparencySoftware_Id.Parse(POIReferenceJSON.Id(token))),
            InfrastructureJson.Array(json, "compatibleTransparencySoftwareIds", token => TransparencySoftware_Id.Parse(POIReferenceJSON.Id(token))),
            InfrastructureJson.Text(json, "chargingStationManufacturerId") is { } manufacturer ? ChargingStationManufacturer_Id.Parse(manufacturer) : null,
            InfrastructureJson.Text(json, "documentNumber"), TransparencyJson.Link(json, "documentURL"),
            TransparencyJson.Date(json, "notBefore"), TransparencyJson.Date(json, "notAfter"));
        foreach (var id in result.VerifiedTransparencySoftwareIds.Concat(result.CompatibleTransparencySoftwareIds))
            if (network.GetTransparencySoftwareById(id) is null) throw new ArgumentException($"Unresolved software reference '{id}'.");
        if (result.ChargingStationManufacturerId is { } manufacturerId && network.GetChargingStationManufacturerById(manufacturerId) is null)
            throw new ArgumentException($"Unresolved manufacturer reference '{manufacturerId}'.");
        return result;
    }

    /// <summary>
    /// Try to parse a document with its registry context.
    /// </summary>
    public static Boolean TryParse(JObject json, RoamingNetwork network,
        [NotNullWhen(true)] out TransparencySoftwareCertificate? value,
        [NotNullWhen(false)] out String? error)
    {
        try
        {
            value = Parse(json, network);
            error = null;
            return true;
        }
        catch (Exception exception)
        {
            value = null;
            error = exception.Message;
            return false;
        }
    }

    /// <summary>
    /// Parse the equivalent deterministic CBOR document with its registry context.
    /// </summary>
    public static TransparencySoftwareCertificate ParseCBOR(ReadOnlySpan<Byte> data, RoamingNetwork network)
        => POIRepresentation.ParseCBOR(data, json => Parse(json, network));

    /// <summary>
    /// Try to parse deterministic CBOR and validate its declared content identifiers.
    /// </summary>
    public static Boolean TryParseCBOR(ReadOnlySpan<Byte> data, RoamingNetwork network,
        [NotNullWhen(true)] out TransparencySoftwareCertificate? value,
        [NotNullWhen(false)] out String? error)
        => POIRepresentation.TryParseCBOR(data, json => Parse(json, network), out value, out error);
}
