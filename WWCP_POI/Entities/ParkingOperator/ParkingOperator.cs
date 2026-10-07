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

using System.Collections;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.Passkeys;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Illias.Votes;
using org.GraphDefined.Vanaheimr.Styx.Arrows;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// WWCP JSON I/O.
    /// </summary>
    public static partial class JSON_IO
    {

        #region ToJSON(this ParkingOperator, Embedded = false, ExpandChargingRoamingNetworkId = false, ExpandChargingStationIds = false, ExpandChargingStationIds = false, ExpandEVSEIds = false)

        public static JObject ToJSON(this ParkingOperator  ParkingOperator,
                                     Boolean               Embedded                        = false,
                                     Boolean               ExpandChargingRoamingNetworkId  = false,
                                     Boolean               ExpandChargingPoolIds           = false,
                                     Boolean               ExpandChargingStationIds        = false,
                                     Boolean               ExpandEVSEIds                   = false)

            => ParkingOperator is not null
                   ? POIRepresentation.AddETags(ParkingOperator, JSONObject.Create(

                         new JProperty("id",                        ParkingOperator.Id.ToString()),

                         Embedded
                             ? null
                             : ExpandChargingRoamingNetworkId
                                   ? new JProperty("roamingNetwork",      ParkingOperator.RoamingNetwork.ToJSON())
                                   : new JProperty("roamingNetworkId",    ParkingOperator.RoamingNetwork.Id.ToString()),

                         new JProperty("name",                  ParkingOperator.Name.       ToJSON()),
                         new JProperty("description",           ParkingOperator.Description.ToJSON()),

                         // Address
                         // LogoURI
                         // API - RobotKeys, Endpoints, DNS SRV
                         // MainKeys

                         ParkingOperator.Logo.IsNotNullOrEmpty()
                             ? new JProperty("logos",               JSONArray.Create(
                                                                        JSONObject.Create(
                                                                            new JProperty("uri",          ParkingOperator.Logo),
                                                                            new JProperty("description",  I18NString.Empty.ToJSON())
                                                                        )
                                                                    ))
                             : null,

                         ParkingOperator.Homepage.IsNotNullOrEmpty()
                             ? new JProperty("homepage",            ParkingOperator.Homepage)
                             : null,

                         ParkingOperator.HotlinePhoneNumber.IsNotNullOrEmpty()
                             ? new JProperty("hotline",             ParkingOperator.HotlinePhoneNumber)
                             : null,

                         ParkingOperator.DataLicenses.Any()
                             ? new JProperty("dataLicenses",        new JArray(ParkingOperator.DataLicenses.Select(license => license.ToJSON())))
                             : null

                         //new JProperty("chargingPools",         ExpandChargingPoolIds
                         //                                           ? new JArray(ParkingOperator.ChargingPools.     ToJSON(Embedded: true))
                         //                                           : new JArray(ParkingOperator.ChargingPoolIds.   Select(id => id.ToString()))),

                         //new JProperty("chargingStations",      ExpandChargingStationIds
                         //                                           ? new JArray(ParkingOperator.ChargingStations.  ToJSON(Embedded: true))
                         //                                           : new JArray(ParkingOperator.ChargingStationIds.Select(id => id.ToString()))),

                         //new JProperty("evses",                 ExpandEVSEIds
                         //                                           ? new JArray(ParkingOperator.EVSEs.             ToJSON(Embedded: true))
                         //                                           : new JArray(ParkingOperator.EVSEIds.           Select(id => id.ToString())))

                     ))
                   : null;

        #endregion

        #region ToJSON(this ParkingOperator, JPropertyKey)

        public static JProperty ToJSON(this ParkingOperator ParkingOperator, String JPropertyKey)

            => ParkingOperator is not null
                   ? new JProperty(JPropertyKey, ParkingOperator.ToJSON())
                   : null;

        #endregion

        #region ToJSON(this ParkingOperators, Skip = null, Take = null, Embedded = false, ExpandChargingRoamingNetworkId = false, ExpandChargingStationIds = false, ExpandChargingStationIds = false, ExpandEVSEIds = false)

        /// <summary>
        /// Return a JSON representation for the given enumeration of Charging Station Operators.
        /// </summary>
        /// <param name="ParkingOperators">An enumeration of Charging Station Operators.</param>
        /// <param name="Skip">The optional number of Charging Station Operators to skip.</param>
        /// <param name="Take">The optional number of Charging Station Operators to return.</param>
        public static JArray ToJSON(this IEnumerable<ParkingOperator>  ParkingOperators,
                                    UInt64?                            Skip                            = null,
                                    UInt64?                            Take                            = null,
                                    Boolean                            Embedded                        = false,
                                    Boolean                            ExpandChargingRoamingNetworkId  = false,
                                    Boolean                            ExpandChargingPoolIds           = false,
                                    Boolean                            ExpandChargingStationIds        = false,
                                    Boolean                            ExpandEVSEIds                   = false)
        {

            #region Initial checks

            if (ParkingOperators is null)
                return new JArray();

            #endregion

            return new JArray(ParkingOperators.
                                  Where     (cso => cso is not null).
                                  OrderBy   (cso => cso.Id).
                                  SkipTakeFilter(Skip, Take).
                                  SafeSelect(cso => cso.ToJSON(Embedded,
                                                               ExpandChargingRoamingNetworkId,
                                                               ExpandChargingPoolIds,
                                                               ExpandChargingStationIds,
                                                               ExpandEVSEIds)));

        }

        #endregion

        #region ToJSON(this ParkingOperators, JPropertyKey)

        public static JProperty ToJSON(this IEnumerable<ParkingOperator> ParkingOperators, String JPropertyKey)
        {

            #region Initial checks

            if (JPropertyKey.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(JPropertyKey), "The json property key must not be null or empty!");

            #endregion

            return ParkingOperators is not null
                       ? new JProperty(JPropertyKey, ParkingOperators.ToJSON())
                       : null;

        }

        #endregion

        #region ToJSON(this ParkingOperatorAdminStatus, Skip = null, Take = null, HistorySize = 1)

        public static JObject ToJSON(this IEnumerable<Timestamped<ParkingOperatorAdminStatusTypes>>  ParkingOperatorAdminStatus,
                                     UInt64?                                                        Skip         = null,
                                     UInt64?                                                        Take         = null,
                                     UInt64?                                                        HistorySize  = 1)

        {

            if (ParkingOperatorAdminStatus is null)
                return new JObject();

            try
            {

                return new JObject(ParkingOperatorAdminStatus.
                                       SkipTakeFilter(Skip, Take).

                                       // Will filter multiple charging station status having the exact same ISO 8601 timestamp!
                                       GroupBy          (tsv   => tsv.  Timestamp.ToISO8601()).
                                       Select           (group => group.First()).

                                       OrderByDescending(tsv   => tsv.Timestamp).
                                       Take             (HistorySize).
                                       Select           (tsv   => new JProperty(tsv.Timestamp.ToISO8601(),
                                                                                tsv.Value.    ToString())));

            }
            catch
            {
                // e.g. when a Stack behind ParkingOperatorAdminStatus is empty!
                return new JObject();
            }

        }

        #endregion

        #region ToJSON(this ParkingOperatorAdminStatus, Skip = null, Take = null, HistorySize = 1)

        public static JObject ToJSON(this IEnumerable<KeyValuePair<ParkingOperator_Id, IEnumerable<Timestamped<ParkingOperatorAdminStatusTypes>>>>  ParkingOperatorAdminStatus,
                                     UInt64?                                                                                                       Skip         = null,
                                     UInt64?                                                                                                       Take         = null,
                                     UInt64?                                                                                                       HistorySize  = 1)

        {

            if (ParkingOperatorAdminStatus is null)
                return new JObject();

            try
            {

                return new JObject(ParkingOperatorAdminStatus.
                                       SkipTakeFilter(Skip, Take).
                                       SafeSelect(statuslist => new JProperty(statuslist.Key.ToString(),
                                                                    new JObject(statuslist.Value.

                                                                                // Will filter multiple charging station status having the exact same ISO 8601 timestamp!
                                                                                GroupBy          (tsv   => tsv.  Timestamp.ToISO8601()).
                                                                                Select           (group => group.First()).

                                                                                OrderByDescending(tsv   => tsv.Timestamp).
                                                                                Take             (HistorySize).
                                                                                Select           (tsv   => new JProperty(tsv.Timestamp.ToISO8601(),
                                                                                                                         tsv.Value.    ToString())))

                                                          )));

            }
            catch
            {
                // e.g. when a Stack behind ParkingOperatorAdminStatus is empty!
                return new JObject();
            }

        }

        #endregion

        #region ToJSON(this ParkingOperatorStatus,      Skip = null, Take = null, HistorySize = 1)

        public static JObject ToJSON(this IEnumerable<Timestamped<ParkingOperatorStatusTypes>>  ParkingOperatorStatus,
                                     UInt64?                                                   Skip         = null,
                                     UInt64?                                                   Take         = null,
                                     UInt64?                                                   HistorySize  = 1)

        {

            if (ParkingOperatorStatus is null)
                return new JObject();

            try
            {

                return new JObject(ParkingOperatorStatus.
                                       SkipTakeFilter(Skip, Take).

                                       // Will filter multiple charging station status having the exact same ISO 8601 timestamp!
                                       GroupBy          (tsv   => tsv.  Timestamp.ToISO8601()).
                                       Select           (group => group.First()).

                                       OrderByDescending(tsv   => tsv.Timestamp).
                                       Take             (HistorySize).
                                       Select           (tsv   => new JProperty(tsv.Timestamp.ToISO8601(),
                                                                                tsv.Value.    ToString())));

            }
            catch
            {
                // e.g. when a Stack behind ParkingOperatorStatus is empty!
                return new JObject();
            }

        }

        #endregion

        #region ToJSON(this ParkingOperatorStatus,      Skip = null, Take = null, HistorySize = 1)

        public static JObject ToJSON(this IEnumerable<KeyValuePair<ParkingOperator_Id, IEnumerable<Timestamped<ParkingOperatorStatusTypes>>>>  ParkingOperatorStatus,
                                     UInt64?                                                                                                  Skip         = null,
                                     UInt64?                                                                                                  Take         = null,
                                     UInt64?                                                                                                  HistorySize  = 1)

        {

            if (ParkingOperatorStatus is null)
                return new JObject();

            try
            {

                return new JObject(ParkingOperatorStatus.
                                       SkipTakeFilter(Skip, Take).
                                       SafeSelect(statuslist => new JProperty(statuslist.Key.ToString(),
                                                                    new JObject(statuslist.Value.

                                                                                // Will filter multiple charging station status having the exact same ISO 8601 timestamp!
                                                                                GroupBy          (tsv   => tsv.  Timestamp.ToISO8601()).
                                                                                Select           (group => group.First()).

                                                                                OrderByDescending(tsv   => tsv.Timestamp).
                                                                                Take             (HistorySize).
                                                                                Select           (tsv   => new JProperty(tsv.Timestamp.ToISO8601(),
                                                                                                                         tsv.Value.    ToString())))

                                                                )));

            }
            catch
            {
                // e.g. when a Stack behind ParkingOperatorStatus is empty!
                return new JObject();
            }

        }

        #endregion

    }


    /// <summary>
    /// The parking operator is responsible for operating parking spaces.
    /// </summary>
    public sealed partial class ParkingOperator : AImmutableEMobilityEntity<ParkingOperator_Id,
                                                    ParkingOperatorAdminStatusTypes,
                                                    ParkingOperatorStatusTypes>,
                                   IEquatable<ParkingOperator>, IComparable<ParkingOperator>, IComparable,
                                   IEnumerable<ParkingGarage>
    {

        #region Data

        /// <summary>
        /// The default max size of the admin status list.
        /// </summary>
        public const UInt16 DefaultMaxAdminStatusScheduleSize   = 15;

        /// <summary>
        /// The default max size of the status list.
        /// </summary>
        public const UInt16 DefaultMaxStatusScheduleSize        = 15;

        #endregion

        #region Properties

        public RoamingNetwork RoamingNetwork { get; }


        #region Logo

        private String _Logo;

        /// <summary>
        /// The logo of this evse operator.
        /// </summary>
        [Optional]
        public String Logo
        {

            get
            {
                return _Logo;
            }

            private set
            {
                if (_Logo != value)
                    SetProperty(ref _Logo, value);
            }

        }

        #endregion

        #region Address

        private Address _Address;

        /// <summary>
        /// The address of the operators headquarter.
        /// </summary>
        [Optional]
        public Address Address
        {

            get
            {
                return ImmutablePOIValues.Copy(_Address);
            }

            private set
            {

                if (value is null)
                    _Address = value;

                if (_Address != value)
                    SetProperty(ref _Address, ImmutablePOIValues.Copy(value));

            }

        }

        #endregion

        #region GeoLocation

        private GeoCoordinate _GeoLocation;

        /// <summary>
        /// The geographical location of this operator.
        /// </summary>
        [Optional]
        public GeoCoordinate GeoLocation
        {

            get
            {
                return _GeoLocation;
            }

            private set
            {

                //if (value is null)
                //    value = new GeoCoordinate(Latitude.Parse(0), Longitude.Parse(0));

                if (_GeoLocation != value)
                    SetProperty(ref _GeoLocation, value);

            }

        }

        #endregion

        #region Telephone

        private String _Telephone;

        /// <summary>
        /// The telephone number of the operator's (sales) office.
        /// </summary>
        [Optional]
        public String Telephone
        {

            get
            {
                return _Telephone;
            }

            private set
            {
                if (_Telephone != value)
                    SetProperty(ref _Telephone, value);
            }

        }

        #endregion

        #region EMailAddress

        private String _EMailAddress;

        /// <summary>
        /// The e-mail address of the operator's (sales) office.
        /// </summary>
        [Optional]
        public String EMailAddress
        {

            get
            {
                return _EMailAddress;
            }

            private set
            {
                if (_EMailAddress != value)
                    SetProperty(ref _EMailAddress, value);
            }

        }

        #endregion

        #region Homepage

        private String _Homepage;

        /// <summary>
        /// The homepage of this evse operator.
        /// </summary>
        [Optional]
        public String Homepage
        {

            get
            {
                return _Homepage;
            }

            private set
            {
                if (_Homepage != value)
                    SetProperty(ref _Homepage, value);
            }

        }

        #endregion

        #region HotlinePhoneNumber

        private String _HotlinePhoneNumber;

        /// <summary>
        /// The telephone number of the Charging Station Operator hotline.
        /// </summary>
        [Optional]
        public String HotlinePhoneNumber
        {

            get
            {
                return _HotlinePhoneNumber;
            }

            private set
            {
                if (_HotlinePhoneNumber != value)
                    SetProperty(ref _HotlinePhoneNumber, value);
            }

        }

        #endregion


        #region DataLicense

        private readonly System.Collections.Immutable.ImmutableArray<DataLicense> _DataLicenses;

        /// <summary>
        /// The license of the charging station operator data.
        /// </summary>
        [Mandatory]
        public IEnumerable<DataLicense> DataLicenses
            => ImmutablePOIValues.CopyItems(_DataLicenses);

        #endregion

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging station operator (CSO) having the given
        /// charging station operator identification (CSO Id).
        /// </summary>
        /// <param name="Id">The unique identification of the Charging Station Operator.</param>
        /// <param name="Name">The official (multi-language) name of the ParkingSpace Operator.</param>
        /// <param name="Description">An optional (multi-language) description of the ParkingSpace Operator.</param>
        /// <param name="RoamingNetwork">The associated roaming network.</param>
        public ParkingOperator(ParkingOperator_Id                Id,
                               RoamingNetwork                    RoamingNetwork,
                               I18NString?                       Name                         = null,
                               I18NString?                       Description                  = null,
                               ParkingOperatorAdminStatusTypes?  InitialAdminStatus           = ParkingOperatorAdminStatusTypes.Operational,
                               ParkingOperatorStatusTypes?       InitialStatus                = ParkingOperatorStatusTypes.Available,
                               UInt16?                           MaxAdminStatusScheduleSize   = DefaultMaxAdminStatusScheduleSize,
                               UInt16?                           MaxStatusScheduleSize        = DefaultMaxStatusScheduleSize,

                               String?                           DataSource                   = null,
                               DateTimeOffset?                   Created                      = null,
                               DateTimeOffset?                   LastChange                   = null,

                               CustomDataNew?                    CustomData                   = null,
                               UserDefinedDictionary?            InternalData                 = null,

                            String? Logo = null, Address? Address = null, GeoCoordinate? GeoLocation = null,
                            String? Telephone = null, String? EMailAddress = null, String? Homepage = null,
                            String? HotlinePhoneNumber = null, IEnumerable<DataLicense>? DataLicenses = null,
                            IEnumerable<ParkingGarage>? ParkingGarages = null,
                            IEnumerable<ParkingSpace_Id>? InvalidParkingSpaceIds = null,
                            IEnumerable<ParkingSpace_Id>? LocalParkingSpaceIds = null)

            : base(Id,
                   Name,
                   Description,

                   InitialAdminStatus         ?? ParkingOperatorAdminStatusTypes.Operational,
                   InitialStatus              ?? ParkingOperatorStatusTypes.Available,
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,

                   DataSource,
                   Created,
                   LastChange,

                   CustomData,
                   InternalData)

        {

            this.RoamingNetwork = RoamingNetwork;
            this._Logo = Logo; this._Address = ImmutablePOIValues.Copy(Address);
            this._GeoLocation = GeoLocation ?? default; this._Telephone = Telephone;
            this._EMailAddress = EMailAddress; this._Homepage = Homepage; this._HotlinePhoneNumber = HotlinePhoneNumber;
            this._DataLicenses = ImmutablePOIValues.CopyItems(DataLicenses);
            this.InvalidParkingSpaceIds = ImmutablePOIValues.CopyItems(InvalidParkingSpaceIds);
            this.LocalParkingSpaceIds = ImmutablePOIValues.CopyItems(LocalParkingSpaceIds);
            this._ParkingGarages = new EntityHashSet<ParkingOperator, ParkingGarage_Id, ParkingGarage>(this);
            foreach (var garage in ParkingGarages ?? [])
                if (_ParkingGarages.TryAdd(garage).Result != CommandResult.Success)
                    throw new ArgumentException("Duplicate parking garage identifier.", nameof(ParkingGarages));

        }

        #endregion


        #region Parking Garage

        #region ParkingGarageAddition

        internal readonly IVotingNotificator<DateTimeOffset, ParkingOperator, ParkingGarage, Boolean> ParkingGarageAddition;

        /// <summary>
        /// Called whenever an charging pool will be or was added.
        /// </summary>
        public IVotingSender<DateTimeOffset, ParkingOperator, ParkingGarage, Boolean> OnParkingGarageAddition

            => ParkingGarageAddition;

        #endregion


        #region ParkingGarages

        private readonly EntityHashSet<ParkingOperator, ParkingGarage_Id, ParkingGarage> _ParkingGarages;

        public IEnumerable<ParkingGarage> ParkingGarages

            => System.Collections.Immutable.ImmutableArray.CreateRange(_ParkingGarages);

        #endregion


        #region ContainsParkingGarage(ParkingGarage)

        /// <summary>
        /// Check if the given ParkingGarage is already present within the Charging Station Operator.
        /// </summary>
        /// <param name="ParkingGarage">A charging pool.</param>
        public Boolean ContainsParkingGarage(ParkingGarage ParkingGarage)

            => _ParkingGarages.Contains(ParkingGarage);

        #endregion

        #region ContainsParkingGarage(ParkingGarageId)

        /// <summary>
        /// Check if the given ParkingGarage identification is already present within the Charging Station Operator.
        /// </summary>
        /// <param name="ParkingGarageId">The unique identification of the charging pool.</param>
        public Boolean ContainsParkingGarage(ParkingGarage_Id ParkingGarageId)

            => _ParkingGarages.ContainsId(ParkingGarageId);

        #endregion

        #region GetParkingGaragebyId(ParkingGarageId)

        public ParkingGarage GetParkingGaragebyId(ParkingGarage_Id ParkingGarageId)

            => _ParkingGarages.GetById(ParkingGarageId);

        #endregion

        #region TryGetParkingGaragebyId(ParkingGarageId, out ParkingGarage)

        public Boolean TryGetParkingGaragebyId(ParkingGarage_Id ParkingGarageId, out ParkingGarage ParkingGarage)

            => _ParkingGarages.TryGet(ParkingGarageId, out ParkingGarage);

        #endregion


        #region ParkingGarageRemoval

        internal readonly IVotingNotificator<DateTimeOffset, ParkingOperator, ParkingGarage, Boolean> ParkingGarageRemoval;

        /// <summary>
        /// Called whenever a charging station will be or was removed.
        /// </summary>
        public IVotingSender<DateTimeOffset, ParkingOperator, ParkingGarage, Boolean> OnParkingGarageRemoval

            => ParkingGarageRemoval;

        #endregion


        #region IEnumerable<ParkingGarage> Members

        IEnumerator IEnumerable.GetEnumerator()

            => _ParkingGarages.GetEnumerator();

        public IEnumerator<ParkingGarage> GetEnumerator()

            => _ParkingGarages.GetEnumerator();

        #endregion

        #endregion

        #region Charging stations


        #region ParkingSpaceAddition

        internal readonly IVotingNotificator<DateTimeOffset, ParkingGarage, ParkingSpace, Boolean> ParkingSpaceAddition;

        /// <summary>
        /// Called whenever an ParkingSpace will be or was added.
        /// </summary>
        public IVotingSender<DateTimeOffset, ParkingGarage, ParkingSpace, Boolean> OnParkingSpaceAddition

            => ParkingSpaceAddition;

        #endregion

        #region ParkingSpaceRemoval

        internal readonly IVotingNotificator<DateTimeOffset, ParkingGarage, ParkingSpace, Boolean> ParkingSpaceRemoval;

        /// <summary>
        /// Called whenever an ParkingSpace will be or was removed.
        /// </summary>
        public IVotingSender<DateTimeOffset, ParkingGarage, ParkingSpace, Boolean> OnParkingSpaceRemoval

            => ParkingSpaceRemoval;

        #endregion


        #endregion


        #region ParkingSpaces


        #region InvalidParkingSpaceIds

        /// <summary>
        /// A list of invalid ParkingSpace Ids.
        /// </summary>
        public System.Collections.Immutable.ImmutableArray<ParkingSpace_Id> InvalidParkingSpaceIds { get; }

        #endregion

        #region LocalParkingSpaceIds

        /// <summary>
        /// A list of manual ParkingSpace Ids which will not be touched automagically.
        /// </summary>
        public System.Collections.Immutable.ImmutableArray<ParkingSpace_Id> LocalParkingSpaceIds { get; }

        #endregion


        #region ParkingSensorAddition

        internal readonly IVotingNotificator<DateTimeOffset, ParkingSpace, ParkingSensor, Boolean> ParkingSensorAddition;

        /// <summary>
        /// Called whenever a socket outlet will be or was added.
        /// </summary>
        public IVotingSender<DateTimeOffset, ParkingSpace, ParkingSensor, Boolean> OnParkingSensorAddition

            => ParkingSensorAddition;

        #endregion

        #region ParkingSensorRemoval

        internal readonly IVotingNotificator<DateTimeOffset, ParkingSpace, ParkingSensor, Boolean> ParkingSensorRemoval;

        /// <summary>
        /// Called whenever a socket outlet will be or was removed.
        /// </summary>
        public IVotingSender<DateTimeOffset, ParkingSpace, ParkingSensor, Boolean> OnParkingSensorRemoval
        {
            get
            {
                return ParkingSensorRemoval;
            }
        }

        #endregion


        #endregion


        #region IComparable<ParkingOperator> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        public override Int32 CompareTo(Object Object)
        {

            if (Object is null)
                throw new ArgumentNullException("The given object must not be null!");

            // Check if the given object is an ParkingSpace_Operator.
            var ParkingSpace_Operator = Object as ParkingOperator;
            if ((Object) ParkingSpace_Operator is null)
                throw new ArgumentException("The given object is not an ParkingSpace_Operator!");

            return CompareTo(ParkingSpace_Operator);

        }

        #endregion

        #region CompareTo(Operator)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Operator">An Charging Station Operator object to compare with.</param>
        public Int32 CompareTo(ParkingOperator Operator)
        {

            if ((Object) Operator is null)
                throw new ArgumentNullException("The given Charging Station Operator must not be null!");

            return Id.CompareTo(Operator.Id);

        }

        #endregion

        #endregion

        #region IEquatable<ParkingOperator> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public override Boolean Equals(Object Object)
        {

            if (Object is null)
                return false;

            // Check if the given object is an ParkingOperator.
            var ParkingSpace_Operator = Object as ParkingOperator;
            if ((Object) ParkingSpace_Operator is null)
                return false;

            return this.Equals(ParkingSpace_Operator);

        }

        #endregion

        #region Equals(Operator)

        /// <summary>
        /// Compares two Charging Station Operators for equality.
        /// </summary>
        /// <param name="Operator">An Charging Station Operator to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public Boolean Equals(ParkingOperator Operator)
        {

            if ((Object) Operator is null)
                return false;

            return Id.Equals(Operator.Id);

        }

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Get the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
            => Id.GetHashCode();

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => Id.ToString();

        #endregion

    }

}
