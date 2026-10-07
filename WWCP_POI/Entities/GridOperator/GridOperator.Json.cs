/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

public sealed partial class GridOperator
{
    /// <summary>
    /// Reconstruct an operator reference, optionally attached to a supplied network version.
    /// </summary>
    public static GridOperator Parse(JObject JSON, RoamingNetwork? RoamingNetwork = null)
    {
        if (TryParse(JSON, out var gridOperator, out var error, RoamingNetwork)) return gridOperator;
        throw new ArgumentException(error, nameof(JSON));
    }

    public static Boolean TryParse(JObject JSON,
                                   [NotNullWhen(true)] out GridOperator? GridOperator,
                                   [NotNullWhen(false)] out String? Error,
                                   RoamingNetwork? RoamingNetwork = null)
    {
        GridOperator = null;
        Error = null;
        try
        {
            InfrastructureJson.Validate(JSON, JSONLDContext);
            var idText = InfrastructureJson.Text(JSON, "id");
            if (idText is null || !GridOperator_Id.TryParse(idText, out var id))
                throw new ArgumentException("id: invalid or missing grid operator identifier.");
            var network = RoamingNetwork ?? new POI.RoamingNetwork(RoamingNetwork_Id.Parse(
                InfrastructureJson.Text(JSON, "roamingNetworkId") ??
                    throw new ArgumentException("roamingNetworkId: required without a supplied network.")));
            InfrastructureJson.Parent(JSON, "roamingNetwork", network.Id.ToString());

            var logos = InfrastructureJson.Array(JSON, "logos", token => InfrastructureJson.Text(InfrastructureJson.Entry(token), "uri") ??
                throw new ArgumentException("uri: required."));
            if (logos.Count > 1) throw new ArgumentException("logos: only one grid operator logo is supported.");
            GridOperatorPriority? priority = null;
            if (JSON["priority"] is { } priorityToken && priorityToken.Type != JTokenType.Null)
            {
                if (priorityToken.Type != JTokenType.Integer)
                    throw new ArgumentException("priority: expected an integer.");
                priority = new(priorityToken.Value<Int32>());
            }

            var parsed = new GridOperator(
                id, network,
                Name: InfrastructureJson.Name(JSON, "name"),
                Description: InfrastructureJson.Name(JSON, "description"),
                Priority: priority,
                DataSource: InfrastructureJson.Text(JSON, "dataSource"),
                Created: InfrastructureJson.Date(JSON, "created")?.UtcDateTime,
                LastChange: InfrastructureJson.Date(JSON, "lastChange")?.UtcDateTime,
                CustomData: InfrastructureJson.CustomData(JSON),
                Logo: logos.FirstOrDefault(),
                Address: InfrastructureJson.Address(JSON),
                GeoLocation: InfrastructureJson.Location(JSON),
                Telephone: InfrastructureJson.Text(JSON, "telephone"),
                EMailAddress: InfrastructureJson.Text(JSON, "eMailAddress"),
                Homepage: InfrastructureJson.Text(JSON, "homepage"),
                HotlinePhoneNumber: InfrastructureJson.Text(JSON, "hotline"),
                DataLicenses: InfrastructureJson.Licenses(JSON));
            InfrastructureJson.RestoreMetadata(JSON, parsed, GridOperatorAdminStatusTypes.TryParse, GridOperatorStatusTypes.TryParse);
            GridOperator = parsed;
            return true;
        }
        catch (Exception exception)
        {
            Error = $"GridOperator: {exception.Message}";
            return false;
        }
    }

    /// <summary>
    /// Copy static data and independent runtime histories into the supplied network version.
    /// </summary>
    internal GridOperator CloneToNetwork(RoamingNetwork network)
    {
        var clone = new GridOperator(Id, network, Name, Description, Priority,
                                     AdminStatus.Value, Status.Value,
                                     adminStatusSchedule.MaxStatusHistorySize, statusSchedule.MaxStatusHistorySize,
                                     DataSource, Created.UtcDateTime, LastChangeDate.UtcDateTime, CustomData, InternalData,
                                     Logo, Address, GeoLocation, Telephone, EMailAddress, Homepage, HotlinePhoneNumber, DataLicenses);
        clone.SetAdminStatus(AdminStatusSchedule());
        clone.SetStatus(StatusSchedule());
        return clone;
    }
}
