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

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The admin status of an energy meter group changed.
    /// </summary>
    public delegate Task OnEnergyMeterGroupAdminStatusChangedDelegate(DateTimeOffset                                 Timestamp,
                                                                      EventTracking_Id                               EventTrackingId,
                                                                      EnergyMeterGroup                               EnergyMeterGroup,
                                                                      Timestamped<EnergyMeterGroupAdminStatusType>   OldStatus,
                                                                      Timestamped<EnergyMeterGroupAdminStatusType>   NewStatus,
                                                                      Context?                                       DataSource);

    /// <summary>
    /// The status of an energy meter group changed.
    /// </summary>
    public delegate Task OnEnergyMeterGroupStatusChangedDelegate(DateTimeOffset                            Timestamp,
                                                                 EventTracking_Id                          EventTrackingId,
                                                                 EnergyMeterGroup                          EnergyMeterGroup,
                                                                 Timestamped<EnergyMeterGroupStatusType>   OldStatus,
                                                                 Timestamped<EnergyMeterGroupStatusType>   NewStatus,
                                                                 Context?                                  DataSource);


    /// <summary>
    /// A group of energy meters, embedded completely in its owner; groups may overlap.
    /// Its identification is unique within the roaming network.
    /// A charging station operator groups the energy meters of its charging pools, charging stations and EVSEs,
    /// a grid operator those of its grid connection points.
    /// It may give its members the properties of an energy meter, which they take at runtime, as the effective members
    /// of its owner: a value of their own wins, arrays are united, and of the groups of a member the one with the higher
    /// priority wins. What belongs to a single device, its serial number and its public keys, is no group's to give.
    /// </summary>
    public sealed class EnergyMeterGroup : AImmutableEMobilityEntity<EnergyMeterGroup_Id,
                                                                     EnergyMeterGroupAdminStatusType,
                                                                     EnergyMeterGroupStatusType>,
                                           IPOIPrioritizedGroup,
                                           IEquatable<EnergyMeterGroup>,
                                           IComparable<EnergyMeterGroup>
    {

        #region Properties

        /// <summary>
        /// Its energy meters, of the same owner, in the order of their identifications.
        /// </summary>
        public ImmutableArray<EnergyMeter_Id>  EnergyMeterIds  { get; }

        /// <summary>
        /// Its priority over the other groups of a member, where they give it different values: the higher wins;
        /// 0, unless given. Groups of the same member with the same priority must not give it different values.
        /// </summary>
        public Int32?  Priority  { get; }

        /// <inheritdoc/>
        String IPOIPrioritizedGroup.GroupId
            => Id.ToString();


        /// <summary>
        /// What its members measure, unless they tell it themselves.
        /// </summary>
        public EnergyMeterRole?                                Role                        { get; }

        /// <summary>
        /// The manufacturer of its members, one of its roaming network, unless they name their own.
        /// </summary>
        public EnergyMeterManufacturer_Id?                     ManufacturerId              { get; }

        /// <summary>
        /// The model of its members, as its manufacturer names it, unless they name their own.
        /// </summary>
        public EnergyMeterModel_Id?                            ModelId                     { get; }

        /// <summary>
        /// The homepage of the model of its members, unless they have their own.
        /// </summary>
        public URL?                                            ModelURL                    { get; }

        /// <summary>
        /// The hardware version of its members, unless they have their own.
        /// </summary>
        public String?                                         HardwareVersion             { get; }

        /// <summary>
        /// The firmware version of its members, unless they have their own.
        /// </summary>
        public String?                                         FirmwareVersion             { get; }

        /// <summary>
        /// Transparency software releases for the measurements of its members, with their legal status, besides their
        /// own; at most one per release, in the order of their identifications.
        /// </summary>
        public ImmutableArray<TransparencySoftwareAssignment>  TransparencySoftwareStatus  { get; }

        /// <summary>
        /// How its members sign their measurements, besides their own; in the order of their names, each once.
        /// </summary>
        public ImmutableArray<SignatureMethod>                 SignatureMethods            { get; }

        /// <summary>
        /// Whether it gives its members any property of an energy meter.
        /// </summary>
        public Boolean HasEnergyMeterProperties

            => Role.HasValue              || ManufacturerId.HasValue  || ModelId.HasValue                     || ModelURL.HasValue          ||
               HardwareVersion is not null || FirmwareVersion is not null || TransparencySoftwareStatus.Length > 0 || SignatureMethods.Length > 0;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new energy meter group.
        /// </summary>
        public EnergyMeterGroup(EnergyMeterGroup_Id                              Id,
                                I18NString?                                      Name                         = null,
                                I18NString?                                      Description                  = null,
                                IEnumerable<EnergyMeter_Id>?                     EnergyMeterIds               = null,
                                Int32?                                           Priority                     = null,

                                EnergyMeterRole?                                 Role                         = null,
                                EnergyMeterManufacturer_Id?                      ManufacturerId               = null,
                                EnergyMeterModel_Id?                             ModelId                      = null,
                                URL?                                             ModelURL                     = null,
                                String?                                          HardwareVersion              = null,
                                String?                                          FirmwareVersion              = null,
                                IEnumerable<TransparencySoftwareAssignment>?     TransparencySoftwareStatus   = null,
                                IEnumerable<SignatureMethod>?                    SignatureMethods             = null,

                                Timestamped<EnergyMeterGroupAdminStatusType>?    InitialAdminStatus           = null,
                                Timestamped<EnergyMeterGroupStatusType>?         InitialStatus                = null,
                                UInt16?                                          MaxAdminStatusScheduleSize   = null,
                                UInt16?                                          MaxStatusScheduleSize        = null,

                                String?                                          DataSource                   = null,
                                DateTimeOffset?                                  Created                      = null,
                                DateTimeOffset?                                  LastChange                   = null,

                                CustomDataNew?                                   CustomData                   = null,
                                UserDefinedDictionary?                           InternalData                 = null)

            : base(Id,
                   Name,
                   Description,

                   // Since its creation, unless told otherwise.
                   InitialAdminStatus         ?? new Timestamped<EnergyMeterGroupAdminStatusType>(Created ?? Timestamp.Now, EnergyMeterGroupAdminStatusType.Operational),
                   InitialStatus              ?? new Timestamped<EnergyMeterGroupStatusType>     (Created ?? Timestamp.Now, EnergyMeterGroupStatusType.Available),
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,

                   DataSource,
                   Created,
                   LastChange,

                   CustomData,
                   InternalData)

        {

            this.EnergyMeterIds              = POIJSON.OrderedIds(EnergyMeterIds,    $"member of the energy meter group '{Id}'");
            this.Priority                    = Priority;

            this.Role                        = Role;
            this.ManufacturerId              = ManufacturerId;
            this.ModelId                     = ModelId;
            this.ModelURL                    = ModelURL;
            this.HardwareVersion             = HardwareVersion;
            this.FirmwareVersion             = FirmwareVersion;
            this.TransparencySoftwareStatus  = TransparencySoftwareAssignment.Checked(TransparencySoftwareStatus, $"the energy meter group '{Id}'");
            this.SignatureMethods            = POIJSON.OrderedIds(SignatureMethods,  $"signature method of the energy meter group '{Id}'");

        }

        #endregion


        #region Status events

        private readonly Lock                                           statusEventsLock  = new ();
        private          OnEnergyMeterGroupAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnEnergyMeterGroupStatusChangedDelegate?       onStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnEnergyMeterGroupAdminStatusChangedDelegate? OnAdminStatusChanged
        {
            add    { lock (statusEventsLock) { onAdminStatusChanged += value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onAdminStatusChanged -= value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
        }

        /// <summary>
        /// An event fired whenever the status changed.
        /// </summary>
        public event OnEnergyMeterGroupStatusChangedDelegate? OnStatusChanged
        {
            add    { lock (statusEventsLock) { onStatusChanged += value; ListenToStatus(onStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onStatusChanged -= value; ListenToStatus(onStatusChanged is not null); } }
        }

        /// <inheritdoc/>
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                                 Timestamp,
                                                                   EventTracking_Id                               EventTrackingId,
                                                                   Timestamped<EnergyMeterGroupAdminStatusType>   NewStatus,
                                                                   Timestamped<EnergyMeterGroupAdminStatusType>   OldStatus,
                                                                   Context?                                       DataSource)
        {
            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset                            Timestamp,
                                                              EventTracking_Id                          EventTrackingId,
                                                              Timestamped<EnergyMeterGroupStatusType>   NewStatus,
                                                              Timestamped<EnergyMeterGroupStatusType>   OldStatus,
                                                              Context?                                  DataSource)
        {
            var onStatusChanged = this.onStatusChanged;
            if (onStatusChanged is not null)
                await onStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this energy meter group, as its owner embeds it.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty("@id", Id.ToString())
                       );

            if (Name.IsNotNullOrEmpty())
                json.Add("name",            Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add("description",     Description.ToJSON());

            if (DataSource is not null)
                json.Add("dataSource",      DataSource);

            if (EnergyMeterIds.Length > 0)
                json.Add("energyMeterIds",  POIJSON.IdsToJSON(EnergyMeterIds));

            if (Priority.HasValue)
                json.Add("priority",        Priority.Value);

            // The properties of an energy meter, as an energy meter writes them.
            if (Role.HasValue)
                json.Add("role",             Role.Value.ToString());

            if (ManufacturerId.HasValue)
                json.Add("manufacturerId",   ManufacturerId.Value.ToString());

            if (ModelId.HasValue)
                json.Add("modelId",          ModelId.Value.ToString());

            if (ModelURL.HasValue)
                json.Add("modelURL",         ModelURL.Value.ToString());

            if (HardwareVersion is not null)
                json.Add("hardwareVersion",  HardwareVersion);

            if (FirmwareVersion is not null)
                json.Add("firmwareVersion",  FirmwareVersion);

            if (TransparencySoftwareStatus.Length > 0)
                json.Add("transparencySoftwareStatus", new JArray(TransparencySoftwareStatus.Select(assignment => assignment.ToJSON())));

            if (SignatureMethods.Length > 0)
                json.Add("signatureMethods", POIJSON.IdsToJSON(SignatureMethods));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            return json;

        }

        #endregion

        #region (static) TryParse(JSON, out EnergyMeterGroup, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of an energy meter group.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="EnergyMeterGroup">The parsed energy meter group.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                      JSON,
                                       [NotNullWhen(true)]  out EnergyMeterGroup?   EnergyMeterGroup,
                                       [NotNullWhen(false)] out String?             ErrorResponse)
        {

            EnergyMeterGroup = null;

            try
            {

                if (!JSON.ParseMandatory("@id",
                                         "energy meter group identification",
                                         EnergyMeterGroup_Id.TryParse,
                                         out EnergyMeterGroup_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseI18N   (JSON, "name",            "energy meter group name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N   (JSON, "description",     "energy meter group description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString (JSON, "dataSource",      "data source",                     out var dataSource,   out ErrorResponse) ||
                    !POIJSON.TryParseIds    (JSON, "energyMeterIds",  "member identification",           EnergyMeter_Id.TryParse, out ImmutableArray<EnergyMeter_Id> members, out ErrorResponse) ||
                    !POIJSON.TryParseInteger(JSON, "priority",        "priority of the energy meter group", out var priority,  out ErrorResponse))
                {
                    return false;
                }

                // The properties of an energy meter, as an energy meter reads them.
                if (!POIJSON.TryParseText  (JSON, "role",             "energy meter role",           EnergyMeterRole.           TryParse, out EnergyMeterRole?            role,           out ErrorResponse) ||
                    !POIJSON.TryParseText  (JSON, "manufacturerId",   "manufacturer identification", EnergyMeterManufacturer_Id.TryParse, out EnergyMeterManufacturer_Id? manufacturerId, out ErrorResponse) ||
                    !POIJSON.TryParseText  (JSON, "modelId",          "model identification",        EnergyMeterModel_Id.       TryParse, out EnergyMeterModel_Id?        modelId,        out ErrorResponse) ||
                    !POIJSON.TryParseText  (JSON, "modelURL",         "model URL",                   URL.                       TryParse, out URL?                        modelURL,       out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, "hardwareVersion",  "hardware version",            out var hardwareVersion,  out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, "firmwareVersion",  "firmware version",            out var firmwareVersion,  out ErrorResponse) ||
                    !POIJSON.TryParseObjects<TransparencySoftwareAssignment>(JSON, "transparencySoftwareStatus", "transparency software status", TransparencySoftwareAssignment.TryParse, out var transparencySoftwareStatus, out ErrorResponse) ||
                    !POIJSON.TryParseIds<SignatureMethod>                   (JSON, "signatureMethods",           "signature method",             SignatureMethod.               TryParse, out var signatureMethods,           out ErrorResponse))
                {
                    return false;
                }

                if (!POIEntityMetadata.TryParse(JSON,
                                                out var created,
                                                out var lastChange,
                                                out var customData,
                                                out ErrorResponse))
                {
                    return false;
                }

                EnergyMeterGroup = new EnergyMeterGroup(id,
                                                        name,
                                                        description,
                                                        members,
                                                        priority,
                                                        role,
                                                        manufacturerId,
                                                        modelId,
                                                        modelURL,
                                                        hardwareVersion,
                                                        firmwareVersion,
                                                        transparencySoftwareStatus,
                                                        signatureMethods,
                                                        DataSource:  dataSource,
                                                        Created:     created,
                                                        LastChange:  lastChange,
                                                        CustomData:  customData);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                EnergyMeterGroup  = null;
                ErrorResponse     = "The given JSON representation of an energy meter group is invalid: " + e.Message;
                return false;
            }

        }

        #endregion


        #region (internal static) Effective(EnergyMeter, Groups)

        /// <summary>
        /// The given energy meter as it is at runtime, with the properties of the given groups: a single value is its
        /// own, else the one of its group with the highest priority giving one; an array is its own united with those of
        /// all its groups, of transparency software releases its own status, else the one of the group with the higher
        /// priority. Its serial number and public keys are its own. It has the runtime status of the energy meter; its last
        /// change is the latest of it and its groups.
        /// </summary>
        /// <param name="EnergyMeter">An energy meter.</param>
        /// <param name="Groups">The groups of the energy meter giving it properties.</param>
        internal static EnergyMeter Effective(EnergyMeter                       EnergyMeter,
                                              POIGroupValues<EnergyMeterGroup>  Groups)
        {

            try
            {

                var effective = new EnergyMeter(

                                    EnergyMeter.Id,
                                    EnergyMeter.Name,
                                    EnergyMeter.Description,

                                    Role:                        Groups.Single("role",             EnergyMeter.Role,             group => group.Role),
                                    ManufacturerId:              Groups.Single("manufacturerId",   EnergyMeter.ManufacturerId,   group => group.ManufacturerId),
                                    ModelId:                     Groups.Single("modelId",          EnergyMeter.ModelId,          group => group.ModelId),
                                    ModelURL:                    Groups.Single("modelURL",         EnergyMeter.ModelURL,         group => group.ModelURL),
                                    HardwareVersion:             Groups.Single("hardwareVersion",  EnergyMeter.HardwareVersion,  group => group.HardwareVersion),
                                    FirmwareVersion:             Groups.Single("firmwareVersion",  EnergyMeter.FirmwareVersion,  group => group.FirmwareVersion),

                                    TransparencySoftwareStatus:  Groups.Union ("transparency software status", EnergyMeter.TransparencySoftwareStatus, group => group.TransparencySoftwareStatus,
                                                                               assignment => assignment.TransparencySoftwareId, assignment => assignment.ToJSON()),
                                    SignatureMethods:            Groups.Union (EnergyMeter.SignatureMethods, group => group.SignatureMethods),

                                    // What belongs to the single device.
                                    SerialNumber:                EnergyMeter.SerialNumber,
                                    PublicKeys:                  EnergyMeter.PublicKeys,

                                    DataSource:                  EnergyMeter.DataSource,
                                    Created:                     EnergyMeter.Created,
                                    LastChange:                  Groups.LastChange(EnergyMeter.LastChangeDate),
                                    CustomData:                  EnergyMeter.CustomData

                                );

                effective.ShareRuntimeOf(EnergyMeter);

                return effective;

            }
            catch (ArgumentException e)
            {
                throw new ArgumentException($"The energy meter '{EnergyMeter.Id}' {Groups.With} is invalid: {e.Message}", e);
            }

        }

        #endregion


        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two energy meter groups for equality by their identifications.
        /// </summary>
        public static Boolean operator == (EnergyMeterGroup? EnergyMeterGroup1, EnergyMeterGroup? EnergyMeterGroup2)

            => ReferenceEquals(EnergyMeterGroup1, EnergyMeterGroup2) ||
               (EnergyMeterGroup1 is not null && EnergyMeterGroup1.Equals(EnergyMeterGroup2));

        /// <summary>
        /// Compares two energy meter groups for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (EnergyMeterGroup? EnergyMeterGroup1, EnergyMeterGroup? EnergyMeterGroup2)

            => !(EnergyMeterGroup1 == EnergyMeterGroup2);

        /// <inheritdoc/>
        public override Int32 CompareTo(Object? Object)

            => Object is EnergyMeterGroup energyMeterGroup
                   ? CompareTo(energyMeterGroup)
                   : throw new ArgumentException("The given object is not an energy meter group!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(EnergyMeterGroup? EnergyMeterGroup)

            => EnergyMeterGroup is null
                   ? throw new ArgumentNullException(nameof(EnergyMeterGroup), "The given energy meter group must not be null!")
                   : Id.CompareTo(EnergyMeterGroup.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is EnergyMeterGroup energyMeterGroup &&
               Equals(energyMeterGroup);

        /// <inheritdoc/>
        public Boolean Equals(EnergyMeterGroup? EnergyMeterGroup)

            => EnergyMeterGroup is not null &&
               Id.Equals(EnergyMeterGroup.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
