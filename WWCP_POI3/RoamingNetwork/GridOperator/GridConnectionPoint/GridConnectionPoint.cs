/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 *
 * Licensed under the Affero GPL license, Version 3.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.gnu.org/licenses/agpl.html
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#region Usings

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Where charging pools are connected to the public grid: an immutable description,
    /// embedded completely in its grid operator. Charging pools name it by its identification,
    /// which is unique within the roaming network.
    /// </summary>
    /// <remarks>
    /// One party maintains it and signs its changes: its grid operator, or the charging station operator
    /// or parking operator named by maintainedBy, e.g. while the grid operator does not take part.
    /// The grid operator takes it over by changing maintainedBy.
    /// Its electrical values are static agreed ratings in their base units: nominalVoltage in V
    /// (line-to-line for three phases), nominalFrequency in Hz, the contracted powers in W and VA.
    /// Missing values are unknown. Live measurements belong to its energy meter.
    /// </remarks>
    public sealed class GridConnectionPoint
    {

        #region Data

        /// <summary>
        /// The parties besides its grid operator that may maintain a grid connection point.
        /// </summary>
        public static readonly IReadOnlySet<String> MaintainerTypes = new HashSet<String> {
            nameof(ChargingStationOperator),
            nameof(ParkingOperator)
        };

        #endregion

        #region Properties

        /// <summary>
        /// The identification of the connection point, assigned by the grid operator.
        /// </summary>
        public GridConnectionPoint_Id  Id                               { get; }

        /// <summary>
        /// The multi-language name.
        /// </summary>
        public ImmutableI18NString?    Name                             { get; }

        /// <summary>
        /// The multi-language description.
        /// </summary>
        public ImmutableI18NString?    Description                      { get; }

        /// <summary>
        /// The address of the connection.
        /// </summary>
        public Address?                Address                          { get; }

        /// <summary>
        /// The geographical location of the connection.
        /// </summary>
        public GeoCoordinate?          GeoLocation                      { get; }

        /// <summary>
        /// The voltage level of the grid, e.g. LowVoltage.
        /// </summary>
        public GridVoltageLevel?       VoltageLevel                     { get; }

        /// <summary>
        /// The type of the connection, e.g. AC_ThreePhases.
        /// </summary>
        public GridConnectionTypes?    ConnectionType                   { get; }

        /// <summary>
        /// The nominal voltage, line-to-line for three phases.
        /// </summary>
        public Volt?                   NominalVoltage                   { get; }

        /// <summary>
        /// The nominal frequency.
        /// </summary>
        public Hertz?                  NominalFrequency                 { get; }

        /// <summary>
        /// The agreed active power taken from the grid.
        /// </summary>
        public Watt?                   ContractedImportPower            { get; }

        /// <summary>
        /// The agreed active power supplied to the grid; zero means none.
        /// </summary>
        public Watt?                   ContractedExportPower            { get; }

        /// <summary>
        /// The agreed apparent power taken from the grid.
        /// </summary>
        public VoltAmpere?             ContractedImportApparentPower    { get; }

        /// <summary>
        /// The agreed apparent power supplied to the grid.
        /// </summary>
        public VoltAmpere?             ContractedExportApparentPower    { get; }

        /// <summary>
        /// The reference of the connection agreement with the grid operator.
        /// </summary>
        public String?                 ConnectionAgreementId            { get; }

        /// <summary>
        /// The identification of the network location, e.g. a NeLo.
        /// </summary>
        public String?                 NetworkLocationId                { get; }

        /// <summary>
        /// The identifications of the market locations, e.g. MaLos for consumption and generation.
        /// </summary>
        public ImmutableArray<String>  MarketLocationIds                { get; }

        /// <summary>
        /// The identifications of the metering locations, e.g. MeLos.
        /// </summary>
        public ImmutableArray<String>  MeteringLocationIds              { get; }

        /// <summary>
        /// The energy meter of the connection.
        /// </summary>
        public EnergyMeter?            EnergyMeter                      { get; }

        /// <summary>
        /// The charging station operator or parking operator maintaining it, or null for its grid operator.
        /// </summary>
        public POIPartyId?             MaintainedBy                     { get; }

        /// <summary>
        /// Custom data.
        /// </summary>
        public CustomDataNew?          CustomData                       { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new grid connection point.
        /// </summary>
        public GridConnectionPoint(GridConnectionPoint_Id  Id,
                                   ImmutableI18NString?    Name                            = null,
                                   ImmutableI18NString?    Description                     = null,
                                   Address?                Address                         = null,
                                   GeoCoordinate?          GeoLocation                     = null,
                                   GridVoltageLevel?       VoltageLevel                    = null,
                                   GridConnectionTypes?    ConnectionType                  = null,
                                   Volt?                   NominalVoltage                  = null,
                                   Hertz?                  NominalFrequency                = null,
                                   Watt?                   ContractedImportPower           = null,
                                   Watt?                   ContractedExportPower           = null,
                                   VoltAmpere?             ContractedImportApparentPower   = null,
                                   VoltAmpere?             ContractedExportApparentPower   = null,
                                   String?                 ConnectionAgreementId           = null,
                                   String?                 NetworkLocationId               = null,
                                   IEnumerable<String>?    MarketLocationIds               = null,
                                   IEnumerable<String>?    MeteringLocationIds             = null,
                                   EnergyMeter?            EnergyMeter                     = null,
                                   POIPartyId?             MaintainedBy                    = null,
                                   CustomDataNew?          CustomData                      = null)
        {

            if (MaintainedBy.HasValue && !MaintainerTypes.Contains(MaintainedBy.Value.EntityType))
                throw new ArgumentException($"The grid connection point '{Id}' can only be maintained by its grid operator, a charging station operator or a parking operator, not by {MaintainedBy}!");

            if (NominalVoltage?.Value <= 0 || NominalFrequency?.Value <= 0)
                throw new ArgumentException($"The nominal voltage and frequency of the grid connection point '{Id}' must be positive!");

            if (ContractedImportPower?.Value         < 0 || ContractedExportPower?.Value         < 0 ||
                ContractedImportApparentPower?.Value < 0 || ContractedExportApparentPower?.Value < 0)
            {
                throw new ArgumentException($"The contracted powers of the grid connection point '{Id}' must not be negative!");
            }

            if (ContractedImportPower?.Value > ContractedImportApparentPower?.Value ||
                ContractedExportPower?.Value > ContractedExportApparentPower?.Value)
            {
                throw new ArgumentException($"A contracted active power of the grid connection point '{Id}' must not exceed its apparent power!");
            }

            this.Id                             = Id;
            this.Name                           = Name;
            this.Description                    = Description;
            this.Address                        = Address;
            this.GeoLocation                    = GeoLocation;
            this.VoltageLevel                   = VoltageLevel;
            this.ConnectionType                 = ConnectionType;
            this.NominalVoltage                 = NominalVoltage;
            this.NominalFrequency               = NominalFrequency;
            this.ContractedImportPower          = ContractedImportPower;
            this.ContractedExportPower          = ContractedExportPower;
            this.ContractedImportApparentPower  = ContractedImportApparentPower;
            this.ContractedExportApparentPower  = ContractedExportApparentPower;
            this.ConnectionAgreementId          = Identifier(ConnectionAgreementId, "connection agreement identification");
            this.NetworkLocationId              = Identifier(NetworkLocationId,     "network location identification");
            this.MarketLocationIds              = Identifiers(MarketLocationIds,    "market location identification");
            this.MeteringLocationIds            = Identifiers(MeteringLocationIds,  "metering location identification");
            this.EnergyMeter                    = EnergyMeter;
            this.MaintainedBy                   = MaintainedBy;
            this.CustomData                     = CustomData;

        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this grid connection point, as its grid operator embeds it.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty("@id", Id.ToString())
                       );

            if (Name.IsNotNullOrEmpty())
                json.Add("name",                           Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add("description",                    Description.ToJSON());

            if (Address is not null)
                json.Add("address",                        Address.ToJSON(Embedded: true));

            if (GeoLocation.HasValue)
                json.Add("geoLocation",                    GeoLocation.Value.ToJSON(Embedded: true));

            if (VoltageLevel.HasValue)
                json.Add("voltageLevel",                   VoltageLevel.Value.ToString());

            if (ConnectionType.HasValue)
                json.Add("connectionType",                 ConnectionType.Value.ToString());

            if (NominalVoltage.HasValue)
                json.Add("nominalVoltage",                 POIJSON.Number(NominalVoltage.Value.Value));

            if (NominalFrequency.HasValue)
                json.Add("nominalFrequency",               POIJSON.Number(NominalFrequency.Value.Value));

            if (ContractedImportPower.HasValue)
                json.Add("contractedImportPower",          POIJSON.Number(ContractedImportPower.Value.Value));

            if (ContractedExportPower.HasValue)
                json.Add("contractedExportPower",          POIJSON.Number(ContractedExportPower.Value.Value));

            if (ContractedImportApparentPower.HasValue)
                json.Add("contractedImportApparentPower",  POIJSON.Number(ContractedImportApparentPower.Value.Value));

            if (ContractedExportApparentPower.HasValue)
                json.Add("contractedExportApparentPower",  POIJSON.Number(ContractedExportApparentPower.Value.Value));

            if (ConnectionAgreementId is not null)
                json.Add("connectionAgreementId",          ConnectionAgreementId);

            if (NetworkLocationId is not null)
                json.Add("networkLocationId",              NetworkLocationId);

            if (MarketLocationIds.Length > 0)
                json.Add("marketLocationIds",              new JArray(MarketLocationIds));

            if (MeteringLocationIds.Length > 0)
                json.Add("meteringLocationIds",            new JArray(MeteringLocationIds));

            if (EnergyMeter is not null)
                json.Add("energyMeter",                    EnergyMeter.ToJSON());

            if (MaintainedBy.HasValue)
                json.Add("maintainedBy",                   MaintainedBy.Value.ToJSON());

            if (CustomData is not null && CustomData.HasValues)
                json.Add("customData",                     CustomData.ToJObject());

            return json;

        }

        #endregion

        #region (static) TryParse(JSON, out GridConnectionPoint, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a grid connection point.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="GridConnectionPoint">The parsed grid connection point.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                        JSON,
                                       [NotNullWhen(true)]  out GridConnectionPoint?  GridConnectionPoint,
                                       [NotNullWhen(false)] out String?               ErrorResponse)
        {

            GridConnectionPoint = null;

            try
            {

                if (!JSON.ParseMandatory("@id",
                                         "grid connection point identification",
                                         GridConnectionPoint_Id.TryParse,
                                         out GridConnectionPoint_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON["gridOperator"] is not null || JSON["gridOperatorId"] is not null)
                {
                    ErrorResponse = $"The grid connection point '{id}' is embedded in its grid operator: it names none!";
                    return false;
                }

                POIPartyId? maintainedBy = null;

                if (JSON["maintainedBy"] is JToken maintainedByJSON)
                {

                    if (!POIPartyId.TryParse(maintainedByJSON, out var maintainer, out ErrorResponse))
                    {
                        ErrorResponse = $"Invalid maintainer of the grid connection point '{id}': {ErrorResponse}";
                        return false;
                    }

                    maintainedBy = maintainer;

                }

                if (!POIJSON.TryParseI18N       (JSON, "name",                   "grid connection point name",           out var name,                   out ErrorResponse) ||
                    !POIJSON.TryParseI18N       (JSON, "description",            "grid connection point description",    out var description,            out ErrorResponse) ||
                    !POIJSON.TryParseAddress    (JSON,                                                                   out var address,                out ErrorResponse) ||
                    !POIJSON.TryParseGeoLocation(JSON,                                                                   out var geoLocation,            out ErrorResponse) ||
                    !POIJSON.TryParseEnum       (JSON, "voltageLevel",           "voltage level",                        out GridVoltageLevel?    voltageLevel,    out ErrorResponse) ||
                    !POIJSON.TryParseEnum       (JSON, "connectionType",         "grid connection type",                 out GridConnectionTypes? connectionType,  out ErrorResponse) ||
                    !POIJSON.TryParseString     (JSON, "connectionAgreementId",  "connection agreement identification",  out var connectionAgreementId,  out ErrorResponse) ||
                    !POIJSON.TryParseString     (JSON, "networkLocationId",      "network location identification",      out var networkLocationId,      out ErrorResponse) ||
                    !POIJSON.TryParseStrings    (JSON, "marketLocationIds",      "market location identifications",      out var marketLocationIds,      out ErrorResponse) ||
                    !POIJSON.TryParseStrings    (JSON, "meteringLocationIds",    "metering location identifications",    out var meteringLocationIds,    out ErrorResponse) ||
                    !EnergyMeter.TryParseOne    (JSON, "energyMeter",                                                    out var energyMeter,            out ErrorResponse))
                {
                    return false;
                }

                if (JSON.ParseOptional("nominalVoltage",                 "nominal voltage",                   out Volt?        nominalVoltage,                 out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (JSON.ParseOptional("nominalFrequency",               "nominal frequency",                 out Hertz?       nominalFrequency,               out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (JSON.ParseOptional("contractedImportPower",          "contracted import power",           out Watt?        contractedImportPower,          out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (JSON.ParseOptional("contractedExportPower",          "contracted export power",           out Watt?        contractedExportPower,          out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (JSON.ParseOptional("contractedImportApparentPower",  "contracted import apparent power",  out VoltAmpere?  contractedImportApparentPower,  out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (JSON.ParseOptional("contractedExportApparentPower",  "contracted export apparent power",  out VoltAmpere?  contractedExportApparentPower,  out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (!POIEntityMetadata.TryParse(JSON,
                                                out var customData,
                                                out ErrorResponse))
                {
                    return false;
                }

                GridConnectionPoint = new GridConnectionPoint(id,
                                                              name,
                                                              description,
                                                              address,
                                                              geoLocation,
                                                              voltageLevel,
                                                              connectionType,
                                                              nominalVoltage,
                                                              nominalFrequency,
                                                              contractedImportPower,
                                                              contractedExportPower,
                                                              contractedImportApparentPower,
                                                              contractedExportApparentPower,
                                                              connectionAgreementId,
                                                              networkLocationId,
                                                              marketLocationIds,
                                                              meteringLocationIds,
                                                              energyMeter,
                                                              maintainedBy,
                                                              customData);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                GridConnectionPoint  = null;
                ErrorResponse        = "The given JSON representation of a grid connection point is invalid: " + e.Message;
                return false;
            }

        }

        #endregion


        #region (private static) Identifier (Text,  Description)

        private static String? Identifier(String?  Text,
                                          String   Description)
        {

            if (Text is null)
                return null;

            var trimmed = Text.Trim();

            return trimmed.Length > 0
                       ? trimmed
                       : throw new ArgumentException($"A {Description} must not be empty!");

        }

        #endregion

        #region (private static) Identifiers(Texts, Description)

        private static ImmutableArray<String> Identifiers(IEnumerable<String>?  Texts,
                                                          String                Description)
        {

            var identifiers = (Texts ?? []).Select(text => Identifier(text ?? throw new ArgumentException($"A {Description} must not be null!"), Description)!).ToArray();

            if (identifiers.Distinct().Count() != identifiers.Length)
                throw new ArgumentException($"A {Description} must not be given twice!");

            return [.. identifiers];

        }

        #endregion


        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

    }

}
