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

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/gridConnectionPoint/v1";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its identification.
            /// </summary>
            public const String  Id                              = "@id";

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context                         = "@context";

            /// <summary>
            /// The property of its multilingual name.
            /// </summary>
            public const String  Name                            = "name";

            /// <summary>
            /// The property of its multilingual description.
            /// </summary>
            public const String  Description                     = "description";

            /// <summary>
            /// The property of its address.
            /// </summary>
            public const String  Address                         = "address";

            /// <summary>
            /// The property of its geographical location.
            /// </summary>
            public const String  GeoLocation                     = "geoLocation";

            /// <summary>
            /// The property of its voltage level.
            /// </summary>
            public const String  VoltageLevel                    = "voltageLevel";

            /// <summary>
            /// The property of its connection type.
            /// </summary>
            public const String  ConnectionType                  = "connectionType";

            /// <summary>
            /// The property of its nominal voltage.
            /// </summary>
            public const String  NominalVoltage                  = "nominalVoltage";

            /// <summary>
            /// The property of its nominal frequency.
            /// </summary>
            public const String  NominalFrequency                = "nominalFrequency";

            /// <summary>
            /// The property of its contracted import power.
            /// </summary>
            public const String  ContractedImportPower           = "contractedImportPower";

            /// <summary>
            /// The property of its contracted export power.
            /// </summary>
            public const String  ContractedExportPower           = "contractedExportPower";

            /// <summary>
            /// The property of its contracted import apparent power.
            /// </summary>
            public const String  ContractedImportApparentPower   = "contractedImportApparentPower";

            /// <summary>
            /// The property of its contracted export apparent power.
            /// </summary>
            public const String  ContractedExportApparentPower   = "contractedExportApparentPower";

            /// <summary>
            /// The property of its connection agreement identification.
            /// </summary>
            public const String  ConnectionAgreementId           = "connectionAgreementId";

            /// <summary>
            /// The property of its network location identification.
            /// </summary>
            public const String  NetworkLocationId               = "networkLocationId";

            /// <summary>
            /// The property of its market location identifications.
            /// </summary>
            public const String  MarketLocationIds               = "marketLocationIds";

            /// <summary>
            /// The property of its metering location identifications.
            /// </summary>
            public const String  MeteringLocationIds             = "meteringLocationIds";

            /// <summary>
            /// The property of its energy meter.
            /// </summary>
            public const String  EnergyMeter                     = "energyMeter";

            /// <summary>
            /// The property of its maintainer.
            /// </summary>
            public const String  MaintainedBy                    = "maintainedBy";

            /// <summary>
            /// The property of its custom data.
            /// </summary>
            public const String  CustomData                      = "customData";

            /// <summary>
            /// The property of its grid operator, refused: it is embedded in its grid operator.
            /// </summary>
            public const String  GridOperator                    = "gridOperator";

            /// <summary>
            /// The property of the identification of its grid operator, refused: it is embedded in its grid operator.
            /// </summary>
            public const String  GridOperatorId                  = "gridOperatorId";

        }

        #endregion


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


        #region (internal) WithEnergyMeter(EnergyMeter)

        /// <summary>
        /// This grid connection point with the given energy meter, e.g. its energy meter as it is at runtime.
        /// </summary>
        internal GridConnectionPoint WithEnergyMeter(EnergyMeter? EnergyMeter)

            => new (Id,
                    Name,
                    Description,
                    Address,
                    GeoLocation,
                    VoltageLevel,
                    ConnectionType,
                    NominalVoltage,
                    NominalFrequency,
                    ContractedImportPower,
                    ContractedExportPower,
                    ContractedImportApparentPower,
                    ContractedExportApparentPower,
                    ConnectionAgreementId,
                    NetworkLocationId,
                    MarketLocationIds,
                    MeteringLocationIds,
                    EnergyMeter,
                    MaintainedBy,
                    CustomData);

        #endregion


        #region ToJSON(Embedded = false, CustomGridConnectionPointSerializer = null)

        /// <summary>
        /// Return the JSON representation of this grid connection point, as its grid operator embeds it.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomGridConnectionPointSerializer">A delegate to serialize custom grid connection point JSON objects.</param>
        public JObject ToJSON(Boolean                                                Embedded                              = false,
                              CustomJObjectSerializerDelegate<GridConnectionPoint>?  CustomGridConnectionPointSerializer   = null)
        {

            var json = new JObject(
                           new JProperty(PropertyKeys.Id, Id.ToString())
                       );

            if (!Embedded)
                json.Add(PropertyKeys.Context,                        JSONLDContext);

            if (Name.IsNotNullOrEmpty())
                json.Add(PropertyKeys.Name,                           Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add(PropertyKeys.Description,                    Description.ToJSON());

            if (Address is not null)
                json.Add(PropertyKeys.Address,                        Address.ToJSON(Embedded: true));

            if (GeoLocation.HasValue)
                json.Add(PropertyKeys.GeoLocation,                    GeoLocation.Value.ToJSON(Embedded: true));

            if (VoltageLevel.HasValue)
                json.Add(PropertyKeys.VoltageLevel,                   VoltageLevel.Value.ToString());

            if (ConnectionType.HasValue)
                json.Add(PropertyKeys.ConnectionType,                 ConnectionType.Value.ToString());

            if (NominalVoltage.HasValue)
                json.Add(PropertyKeys.NominalVoltage,                 POIJSON.Number(NominalVoltage.Value.Value));

            if (NominalFrequency.HasValue)
                json.Add(PropertyKeys.NominalFrequency,               POIJSON.Number(NominalFrequency.Value.Value));

            if (ContractedImportPower.HasValue)
                json.Add(PropertyKeys.ContractedImportPower,          POIJSON.Number(ContractedImportPower.Value.Value));

            if (ContractedExportPower.HasValue)
                json.Add(PropertyKeys.ContractedExportPower,          POIJSON.Number(ContractedExportPower.Value.Value));

            if (ContractedImportApparentPower.HasValue)
                json.Add(PropertyKeys.ContractedImportApparentPower,  POIJSON.Number(ContractedImportApparentPower.Value.Value));

            if (ContractedExportApparentPower.HasValue)
                json.Add(PropertyKeys.ContractedExportApparentPower,  POIJSON.Number(ContractedExportApparentPower.Value.Value));

            if (ConnectionAgreementId is not null)
                json.Add(PropertyKeys.ConnectionAgreementId,          ConnectionAgreementId);

            if (NetworkLocationId is not null)
                json.Add(PropertyKeys.NetworkLocationId,              NetworkLocationId);

            if (MarketLocationIds.Length > 0)
                json.Add(PropertyKeys.MarketLocationIds,              new JArray(MarketLocationIds));

            if (MeteringLocationIds.Length > 0)
                json.Add(PropertyKeys.MeteringLocationIds,            new JArray(MeteringLocationIds));

            if (EnergyMeter is not null)
                json.Add(PropertyKeys.EnergyMeter,                    EnergyMeter.ToJSON(Embedded: true));

            if (MaintainedBy.HasValue)
                json.Add(PropertyKeys.MaintainedBy,                   MaintainedBy.Value.ToJSON(Embedded: true));

            if (CustomData is not null && CustomData.HasValues)
                json.Add(PropertyKeys.CustomData,                     CustomData.ToJObject());

            return CustomGridConnectionPointSerializer is not null
                       ? CustomGridConnectionPointSerializer(this, json)
                       : json;

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

                if (!JSON.ParseMandatory(PropertyKeys.Id,
                                         "grid connection point identification",
                                         GridConnectionPoint_Id.TryParse,
                                         out GridConnectionPoint_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the grid connection point '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                if (JSON[PropertyKeys.GridOperator] is not null || JSON[PropertyKeys.GridOperatorId] is not null)
                {
                    ErrorResponse = $"The grid connection point '{id}' is embedded in its grid operator: it names none!";
                    return false;
                }

                POIPartyId? maintainedBy = null;

                if (JSON[PropertyKeys.MaintainedBy] is JToken maintainedByJSON)
                {

                    if (!POIPartyId.TryParse(maintainedByJSON, out var maintainer, out ErrorResponse))
                    {
                        ErrorResponse = $"Invalid maintainer of the grid connection point '{id}': {ErrorResponse}";
                        return false;
                    }

                    maintainedBy = maintainer;

                }

                if (!POIJSON.TryParseI18N       (JSON, PropertyKeys.Name,                   "grid connection point name",           out var name,                   out ErrorResponse) ||
                    !POIJSON.TryParseI18N       (JSON, PropertyKeys.Description,            "grid connection point description",    out var description,            out ErrorResponse) ||
                    !POIJSON.TryParseAddress    (JSON, PropertyKeys.Address,                "address",                              out var address,                out ErrorResponse) ||
                    !POIJSON.TryParseGeoLocation(JSON, PropertyKeys.GeoLocation,            "geo location",                         out var geoLocation,            out ErrorResponse) ||
                    !POIJSON.TryParseEnum       (JSON, PropertyKeys.VoltageLevel,           "voltage level",                        out GridVoltageLevel?    voltageLevel,    out ErrorResponse) ||
                    !POIJSON.TryParseEnum       (JSON, PropertyKeys.ConnectionType,         "grid connection type",                 out GridConnectionTypes? connectionType,  out ErrorResponse) ||
                    !POIJSON.TryParseString     (JSON, PropertyKeys.ConnectionAgreementId,  "connection agreement identification",  out var connectionAgreementId,  out ErrorResponse) ||
                    !POIJSON.TryParseString     (JSON, PropertyKeys.NetworkLocationId,      "network location identification",      out var networkLocationId,      out ErrorResponse) ||
                    !POIJSON.TryParseStrings    (JSON, PropertyKeys.MarketLocationIds,      "market location identifications",      out var marketLocationIds,      out ErrorResponse) ||
                    !POIJSON.TryParseStrings    (JSON, PropertyKeys.MeteringLocationIds,    "metering location identifications",    out var meteringLocationIds,    out ErrorResponse) ||
                    !EnergyMeter.TryParseOne    (JSON, PropertyKeys.EnergyMeter,                                                    out var energyMeter,            out ErrorResponse))
                {
                    return false;
                }

                if (JSON.ParseOptional(PropertyKeys.NominalVoltage,                 "nominal voltage",                   out Volt?        nominalVoltage,                 out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (JSON.ParseOptional(PropertyKeys.NominalFrequency,               "nominal frequency",                 out Hertz?       nominalFrequency,               out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (JSON.ParseOptional(PropertyKeys.ContractedImportPower,          "contracted import power",           out Watt?        contractedImportPower,          out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (JSON.ParseOptional(PropertyKeys.ContractedExportPower,          "contracted export power",           out Watt?        contractedExportPower,          out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (JSON.ParseOptional(PropertyKeys.ContractedImportApparentPower,  "contracted import apparent power",  out VoltAmpere?  contractedImportApparentPower,  out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (JSON.ParseOptional(PropertyKeys.ContractedExportApparentPower,  "contracted export apparent power",  out VoltAmpere?  contractedExportApparentPower,  out ErrorResponse))
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
