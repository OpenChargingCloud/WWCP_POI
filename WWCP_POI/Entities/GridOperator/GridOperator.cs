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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using System.Collections.Concurrent;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A grid operator.
    /// </summary>
    public sealed partial class GridOperator : AImmutableEMobilityEntity<GridOperator_Id,
                                                 GridOperatorAdminStatusTypes,
                                                 GridOperatorStatusTypes>,
                                IEquatable <GridOperator>,
                                IComparable<GridOperator>,
                                IComparable
    {

        #region Data

        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/gridOperator";

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


        #region Data licenses

        private readonly System.Collections.Immutable.ImmutableArray<DataLicense> dataLicenses;

        /// <summary>
        /// The license of the roaming network data.
        /// </summary>
        [Mandatory]
        public IEnumerable<DataLicense> DataLicenses
            => ImmutablePOIValues.CopyItems(dataLicenses);

        #endregion


        public GridOperatorPriority Priority { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new e-mobility (service) provider having the given
        /// unique identification.
        /// </summary>
        /// <param name="Id">The unique e-mobility provider identification.</param>
        /// <param name="RoamingNetwork">The associated roaming network.</param>
        public GridOperator(GridOperator_Id                     Id,
                            RoamingNetwork                      RoamingNetwork,
                            I18NString?                         Name                         = null,
                            I18NString?                         Description                  = null,
                            GridOperatorPriority?               Priority                     = null,
                            GridOperatorAdminStatusTypes?       InitialAdminStatus           = null,
                            GridOperatorStatusTypes?            InitialStatus                = null,
                            UInt16?                             MaxAdminStatusScheduleSize   = DefaultMaxAdminStatusScheduleSize,
                            UInt16?                             MaxStatusScheduleSize        = DefaultMaxStatusScheduleSize,

                            String?                             DataSource                   = null,
                            DateTime?                           Created                      = null,
                            DateTime?                           LastChange                   = null,

                            CustomDataNew?                      CustomData                   = null,
                            UserDefinedDictionary?              InternalData                 = null,

                            String? Logo = null, Address? Address = null, GeoCoordinate? GeoLocation = null,
                            String? Telephone = null, String? EMailAddress = null, String? Homepage = null,
                            String? HotlinePhoneNumber = null, IEnumerable<DataLicense>? DataLicenses = null)

            : base(Id,
                   Name,
                   Description,

                   InitialAdminStatus         ?? GridOperatorAdminStatusTypes.Available,
                   InitialStatus              ?? GridOperatorStatusTypes.Available,
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,

                   DataSource,
                   Created,
                   LastChange,

                   CustomData,
                   InternalData)

        {

            ArgumentNullException.ThrowIfNull(RoamingNetwork);
            if (Id.IsNullOrEmpty)
                throw new ArgumentException("A grid operator must have a non-empty ID.", nameof(Id));
            this.RoamingNetwork = RoamingNetwork;
            this._Logo = Logo; this._Address = ImmutablePOIValues.Copy(Address);
            this._GeoLocation = GeoLocation ?? default; this._Telephone = Telephone;
            this._EMailAddress = EMailAddress; this._Homepage = Homepage; this._HotlinePhoneNumber = HotlinePhoneNumber;
            this.Priority = Priority ?? default;
            this.dataLicenses = ImmutablePOIValues.CopyItems(DataLicenses);

        }

        #endregion


        #region ToJSON(Embedded = false, ExpandChargingRoamingNetworkId = false)

        public JObject ToJSON(Boolean  Embedded                         = false,
                              Boolean  ExpandChargingRoamingNetworkId   = false)

        {

             var json = JSONObject.Create(

                         new JProperty("id",                        Id.ToString()),

                         Embedded
                             ? null
                             : ExpandChargingRoamingNetworkId
                                   ? new JProperty("roamingNetwork",      RoamingNetwork.ToJSON())
                                   : new JProperty("roamingNetworkId",    RoamingNetwork.Id.ToString()),

                         new JProperty("name",                  Name.       ToJSON()),
                         new JProperty("description",           Description.ToJSON()),

                         // Address
                         // LogoURI
                         // API - RobotKeys, Endpoints, DNS SRV
                         // MainKeys

                         Logo.IsNotNullOrEmpty()
                             ? new JProperty("logos",               JSONArray.Create(
                                                                        JSONObject.Create(
                                                                            new JProperty("uri",          Logo),
                                                                            new JProperty("description",  I18NString.Empty.ToJSON())
                                                                        )
                                                                    ))
                             : null,

                         Homepage.IsNotNullOrEmpty()
                             ? new JProperty("homepage",            Homepage)
                             : null,

                         HotlinePhoneNumber.IsNotNullOrEmpty()
                             ? new JProperty("hotline",             HotlinePhoneNumber)
                             : null,

                         DataLicenses.Any()
                             ? new JProperty("dataLicenses",        new JArray(DataLicenses.Select(license => license.ToJSON())))
                             : null

                         //new JProperty("chargingPools",         ExpandChargingPoolIds
                         //                                           ? new JArray(ChargingPools.     ToJSON(Embedded: true))
                         //                                           : new JArray(ChargingPoolIds.   Select(id => id.ToString()))),

                         //new JProperty("chargingStations",      ExpandChargingStationIds
                         //                                           ? new JArray(ChargingStations.  ToJSON(Embedded: true))
                         //                                           : new JArray(ChargingStationIds.Select(id => id.ToString()))),

                         //new JProperty("evses",                 ExpandEVSEIds
                         //                                           ? new JArray(EVSEs.             ToJSON(Embedded: true))
                         //                                           : new JArray(EVSEIds.           Select(id => id.ToString())))

                     );

            if (!Embedded) json["@context"] = JSONLDContext;
            if (_Address is not null) json["address"] = _Address.ToJSON(Embedded: true);
            if (!_GeoLocation.Equals(default(GeoCoordinate))) json["geoLocation"] = InfrastructureJson.LocationJSON(_GeoLocation, true);
            if (Telephone.IsNotNullOrEmpty()) json["telephone"] = Telephone;
            if (EMailAddress.IsNotNullOrEmpty()) json["eMailAddress"] = EMailAddress;
            if (Priority is not null) json["priority"] = Priority.Value;
            return POIRepresentation.AddETags(this, InfrastructureJson.SnapshotMetadata(json, this));

        }

        #endregion


        #region IComparable<EVSE_Operator> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        public override Int32 CompareTo(Object Object)
        {

            if (Object is null)
                throw new ArgumentNullException("The given object must not be null!");

            // Check if the given object is an EVSE_Operator.
            var EVSE_Operator = Object as GridOperator;
            if ((Object) EVSE_Operator is null)
                throw new ArgumentException("The given object is not an EVSE_Operator!");

            return CompareTo(EVSE_Operator);

        }

        #endregion

        #region CompareTo(EVSE_Operator)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSE_Operator">An EVSE_Operator object to compare with.</param>
        public Int32 CompareTo(GridOperator EVSE_Operator)
        {

            if ((Object) EVSE_Operator is null)
                throw new ArgumentNullException("The given EVSE_Operator must not be null!");

            return Id.CompareTo(EVSE_Operator.Id);

        }

        #endregion

        #endregion

        #region IEquatable<EVSE_Operator> Members

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

            // Check if the given object is an EVSE_Operator.
            var EVSE_Operator = Object as GridOperator;
            if ((Object) EVSE_Operator is null)
                return false;

            return this.Equals(EVSE_Operator);

        }

        #endregion

        #region Equals(EVSE_Operator)

        /// <summary>
        /// Compares two EVSE_Operator for equality.
        /// </summary>
        /// <param name="EVSE_Operator">An EVSE_Operator to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public Boolean Equals(GridOperator EVSE_Operator)
        {

            if ((Object) EVSE_Operator is null)
                return false;

            return Id.Equals(EVSE_Operator.Id);

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
