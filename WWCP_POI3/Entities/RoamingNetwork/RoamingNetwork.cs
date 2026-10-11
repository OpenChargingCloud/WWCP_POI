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

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Styx;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Styx.Arrows;


#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A delegate for filtering roaming networks.
    /// </summary>
    /// <param name="RoamingNetwork">A roaming network to include.</param>
    public delegate Boolean IncludeRoamingNetworkDelegate(RoamingNetwork RoamingNetwork);


    /// <summary>
    /// Extension methods for the roaming networks.
    /// </summary>
    public static class RoamingNetworkExtensions2
    {

        #region ToJSON(this RoamingNetworks, Embedded = false, ...)

        /// <summary>
        /// Return a JSON representation for the given roaming networks collection.
        /// </summary>
        /// <param name="RoamingNetworks">An enumeration of roaming networks.</param>
        /// <param name="Embedded">Whether this roaming network is embedded into another data structure.</param>
        /// <param name="Skip">The optional number of roaming networks to skip.</param>
        /// <param name="Take">The optional number of roaming networks to return.</param>
        public static JArray ToJSON(this IEnumerable<RoamingNetwork>                           RoamingNetworks,
                                    Boolean                                                    Embedded                                  = false,
                                    UInt64?                                                    Skip                                      = null,
                                    UInt64?                                                    Take                                      = null,

                                    InfoStatus                                                 ExpandRoamingNetworkIds                   = InfoStatus.ShowIdOnly,
                                    InfoStatus                                                 ExpandChargingStationOperatorIds          = InfoStatus.ShowIdOnly,
                                    InfoStatus                                                 ExpandChargingPoolIds                     = InfoStatus.ShowIdOnly,
                                    InfoStatus                                                 ExpandChargingStationIds                  = InfoStatus.ShowIdOnly,
                                    InfoStatus                                                 ExpandEVSEIds                             = InfoStatus.ShowIdOnly,
                                    InfoStatus                                                 ExpandBrandIds                            = InfoStatus.ShowIdOnly,
                                    InfoStatus                                                 ExpandDataLicenses                        = InfoStatus.ShowIdOnly,
                                    InfoStatus                                                 ExpandEMobilityProviderId                 = InfoStatus.ShowIdOnly,

                                    CustomJObjectSerializerDelegate<RoamingNetwork>?           CustomRoamingNetworkSerializer            = null)
                                    //CustomJObjectSerializerDelegate<ChargingStationOperator>?  CustomChargingStationOperatorSerializer   = null,
                                    //CustomJObjectSerializerDelegate<ChargingPool>?             CustomChargingPoolSerializer              = null,
                                    //CustomJObjectSerializerDelegate<ChargingStation>?          CustomChargingStationSerializer           = null,
                                    //CustomJObjectSerializerDelegate<EVSE>?                     CustomEVSESerializer                      = null)
        {

            #region Initial checks

            if (RoamingNetworks is null)
                return [];

            #endregion

            return new JArray(
                       RoamingNetworks.
                           SkipTakeFilter(Skip, Take).
                           Select        (roamingNetwork => roamingNetwork.ToJSON(

                                                                Embedded,

                                                                ExpandRoamingNetworkIds,
                                                                ExpandChargingStationOperatorIds,
                                                                ExpandChargingPoolIds,
                                                                ExpandChargingStationIds,
                                                                ExpandEVSEIds,
                                                                ExpandBrandIds,
                                                                ExpandDataLicenses,
                                                                ExpandEMobilityProviderId,

                                                                CustomRoamingNetworkSerializer
                                                                //CustomChargingStationOperatorSerializer,
                                                                //CustomChargingPoolSerializer,
                                                                //CustomChargingStationSerializer,
                                                                //CustomEVSESerializer

                                                            ))
                   );

        }

        #endregion

    }


    /// <summary>
    /// A Electric Vehicle Roaming Network is a service abstraction to allow multiple
    /// independent roaming services to be delivered over the same infrastructure.
    /// This can e.g. be a differentation of service levels (premiun, basic,
    /// discount) or allow a simplified testing (production, qa, featureX, ...)
    /// </summary>
    public sealed partial class RoamingNetwork : AImmutableEMobilityEntity<RoamingNetwork_Id,
                                                                           RoamingNetworkAdminStatusType,
                                                                           RoamingNetworkStatusType>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const               String                                             JSONLDContext                          = "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork";

        protected static readonly  SemaphoreSlim                                      eMobilityProvidersSemaphore            = new (1, 1);
        protected static readonly  SemaphoreSlim                                      chargingStationOperatorsSemaphore      = new (1, 1);

        protected static readonly  TimeSpan                                           SemaphoreSlimTimeout                   = TimeSpan.FromSeconds(5);

        protected static readonly  Byte                                               MinEMobilityProviderIdLength           = 5;
        protected static readonly  Byte                                               MinEMobilityProviderNameLength         = 5;

        protected static readonly  Byte                                               MinChargingStationOperatorIdLength     = 5;
        protected static readonly  Byte                                               MinChargingStationOperatorNameLength   = 5;

        #endregion

        #region Properties

        #region Data licenses

        private readonly ConcurrentDictionary<DataLicense_Id, DataLicense> dataLicenses = [];

        /// <summary>
        /// The license of the roaming network data.
        /// </summary>
        [Mandatory]
        public IEnumerable<DataLicense> DataLicenses
            => dataLicenses.Values;

        #endregion

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new roaming network having the given unique roaming network identification.
        /// </summary>
        /// <param name="Id">The unique identification of the roaming network.</param>
        /// <param name="Name">The multi-language name of the roaming network.</param>
        /// <param name="Description">An optional multi-language description of the roaming network.</param>
        /// <param name="InitialAdminStatus">The initial admin status of the roaming network.</param>
        /// <param name="InitialStatus">The initial status of the roaming network.</param>
        /// <param name="MaxAdminStatusScheduleSize">The maximum number of entries in the admin status history.</param>
        /// <param name="MaxStatusScheduleSize">The maximum number of entries in the status history.</param>
        public RoamingNetwork(RoamingNetwork_Id               Id,
                              I18NString?                     Name                         = null,
                              I18NString?                     Description                  = null,
                              RoamingNetworkAdminStatusType?  InitialAdminStatus           = null,
                              RoamingNetworkStatusType?       InitialStatus                = null,
                              UInt16?                         MaxAdminStatusScheduleSize   = null,
                              UInt16?                         MaxStatusScheduleSize        = null,

                              String?                         DataSource                   = null,
                              DateTimeOffset?                 Created                      = null,
                              DateTimeOffset?                 LastChange                   = null,

                              CustomDataNew?                  CustomData                   = null,
                              UserDefinedDictionary?          InternalData                 = null)

            : base(Id,
                   Name,
                   Description,
                   InitialAdminStatus         ?? RoamingNetworkAdminStatusType.Operational,
                   InitialStatus              ?? RoamingNetworkStatusType.Available,
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,
                   DataSource,
                   Created,
                   LastChange,
                   CustomData,
                   InternalData)

        {

            #region Link events

            this.adminStatusSchedule.OnStatusChanged += async (timestamp, eventTrackingId, statusSchedule, newStatus, oldStatus, dataSource) => {
                var onAdminStatusChanged = OnAdminStatusChanged;
                if (onAdminStatusChanged is not null)
                    await onAdminStatusChanged(timestamp, eventTrackingId, this, newStatus, oldStatus, dataSource);
            };

            this.statusSchedule.     OnStatusChanged += async (timestamp, eventTrackingId, statusSchedule, newStatus, oldStatus, dataSource) => {
                var onStatusChanged = OnStatusChanged;
                if (onStatusChanged is not null)
                    await onStatusChanged(timestamp, eventTrackingId, this, newStatus, oldStatus, dataSource);
            };

            #endregion

        }

        #endregion


        #region Data/(Admin-)Status management

        /// <summary>
        /// An event fired whenever the dynamic status changed.
        /// </summary>
        public event OnRoamingNetworkStatusChangedDelegate?       OnStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnRoamingNetworkAdminStatusChangedDelegate?  OnAdminStatusChanged;

        #endregion



        #region ToJSON(this RoamingNetwork, Embedded = false, ...)

        /// <summary>
        /// Return a JSON representation of the given roaming network.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        public JObject ToJSON(Boolean                                                     Embedded                                  = false,

                              InfoStatus                                                  ExpandRoamingNetworkIds                   = InfoStatus.ShowIdOnly,
                              InfoStatus                                                  ExpandChargingStationOperatorIds          = InfoStatus.ShowIdOnly,
                              InfoStatus                                                  ExpandChargingPoolIds                     = InfoStatus.ShowIdOnly,
                              InfoStatus                                                  ExpandChargingStationIds                  = InfoStatus.ShowIdOnly,
                              InfoStatus                                                  ExpandEVSEIds                             = InfoStatus.ShowIdOnly,
                              InfoStatus                                                  ExpandBrandIds                            = InfoStatus.ShowIdOnly,
                              InfoStatus                                                  ExpandDataLicenses                        = InfoStatus.ShowIdOnly,
                              InfoStatus                                                  ExpandEMobilityProviderId                 = InfoStatus.ShowIdOnly,

                              CustomJObjectSerializerDelegate<RoamingNetwork>?            CustomRoamingNetworkSerializer            = null)
                              //CustomJObjectSerializerDelegate<ChargingStationOperator>?   CustomChargingStationOperatorSerializer   = null,
                              //CustomJObjectSerializerDelegate<ChargingPool>?              CustomChargingPoolSerializer              = null,
                              //CustomJObjectSerializerDelegate<ChargingStation>?           CustomChargingStationSerializer           = null,
                              //CustomJObjectSerializerDelegate<EVSE>?                      CustomEVSESerializer                      = null)
        {

            var JSON = JSONObject.Create(

                               new JProperty("@id",             Id.         ToString()),

                         !Embedded
                             ? new JProperty("@context",        JSONLDContext)
                             : null,

                               new JProperty("name",            Name.       ToJSON()),

                         Description.IsNotNullOrEmpty()
                             ? new JProperty("description",     Description.ToJSON())
                             : null,

                         DataSource is not null && DataSource.IsNeitherNullNorEmpty()
                             ? new JProperty("dataSource",      DataSource)
                             : null,

                         DataLicenses.Any()
                             ? ExpandDataLicenses.Switch(
                                 () => new JProperty("dataLicenseIds",  new JArray(DataLicenses.SafeSelect(license => license.Id.ToString()))),
                                 () => new JProperty("dataLicenses",    DataLicenses.ToJSON()))
                             : null

            //             ChargingStationOperators.Any()
            //                 ? ExpandChargingStationOperatorIds.Switch(

            //                       () => new JProperty("chargingStationOperatorIds",  new JArray(ChargingStationOperators.Select(op => op.Id).
            //                                                                                                    OrderBy(id => id).
            //                                                                                                    Select (id => id.ToString()))),

            //                       () => new JProperty("chargingStationOperators",    new JArray(ChargingStationOperators.
            //                                                                                                    OrderBy(cso => cso).
            //                                                                                                    ToJSON (Embedded: true,
            //                                                                                                            ExpandRoamingNetworkId:                   InfoStatus.Hidden,
            //                                                                                                            ExpandChargingPoolIds:                    ExpandChargingPoolIds,
            //                                                                                                            ExpandChargingStationIds:                 ExpandChargingStationIds,
            //                                                                                                            ExpandEVSEIds:                            ExpandEVSEIds,
            //                                                                                                            ExpandBrandIds:                           ExpandBrandIds,
            //                                                                                                            ExpandDataLicenses:                       ExpandDataLicenses,
            //                                                                                                            CustomChargingStationOperatorSerializer:  CustomChargingStationOperatorSerializer,
            //                                                                                                            CustomChargingPoolSerializer:             CustomChargingPoolSerializer,
            //                                                                                                            CustomChargingStationSerializer:          CustomChargingStationSerializer,
            //                                                                                                            CustomEVSESerializer:                     CustomEVSESerializer))))
            //                 : null,

            //             !ChargingStationOperators.Any() || ExpandChargingStationOperatorIds == InfoStatus.Expanded
            //                 ? null
            //                 : ExpandChargingPoolIds.Switch(
            //                       () => new JProperty("chargingPoolIds",             new JArray(ChargingPools.Select(pool => pool.Id).
            //                                                                                                    OrderBy(id => id).
            //                                                                                                    Select (id => id.ToString()))),

            //                       () => new JProperty("chargingPools",               new JArray(ChargingPools.
            //                                                                                                    OrderBy(pool => pool).
            //                                                                                                    ToJSON (Embedded: true,
            //                                                                                                            ExpandRoamingNetworkId:           InfoStatus.Hidden,
            //                                                                                                            ExpandChargingStationOperatorId:  InfoStatus.Hidden,
            //                                                                                                            ExpandChargingStationIds:         InfoStatus.Hidden,
            //                                                                                                            ExpandEVSEIds:                    InfoStatus.Hidden,
            //                                                                                                            ExpandBrandIds:                   InfoStatus.Hidden,
            //                                                                                                            ExpandDataLicenses:               InfoStatus.Hidden,
            //                                                                                                            CustomChargingPoolSerializer:     CustomChargingPoolSerializer,
            //                                                                                                            CustomChargingStationSerializer:  CustomChargingStationSerializer,
            //                                                                                                            CustomEVSESerializer:             CustomEVSESerializer)))),

            //             !ChargingStationOperators.Any() || (ExpandChargingPoolIds == InfoStatus.Expanded || ExpandChargingStationOperatorIds == InfoStatus.Expanded)
            //                 ? null
            //                 : ExpandChargingStationIds.Switch(
            //                       () => new JProperty("chargingStationIds",          new JArray(ChargingStationIds().
            //                                                                                                    OrderBy(id => id).
            //                                                                                                    Select (id => id.ToString()))),

            //                       () => new JProperty("chargingStations",            new JArray(ChargingStations.
            //                                                                                                    OrderBy(station => station).
            //                                                                                                    ToJSON (Embedded: true,
            //                                                                                                            ExpandRoamingNetworkId:           InfoStatus.Hidden,
            //                                                                                                            ExpandChargingStationOperatorId:  InfoStatus.Hidden,
            //                                                                                                            ExpandChargingPoolId:             InfoStatus.Hidden,
            //                                                                                                            ExpandEVSEIds:                    InfoStatus.Hidden,
            //                                                                                                            ExpandBrandIds:                   InfoStatus.Hidden,
            //                                                                                                            ExpandDataLicenses:               InfoStatus.Hidden,
            //                                                                                                            CustomChargingStationSerializer:  CustomChargingStationSerializer,
            //                                                                                                            CustomEVSESerializer:             CustomEVSESerializer)))),

            //             !ChargingStationOperators.Any() || (ExpandChargingStationIds == InfoStatus.Expanded || ExpandChargingPoolIds == InfoStatus.Expanded || ExpandChargingStationOperatorIds == InfoStatus.Expanded)
            //                 ? null
            //                 : ExpandEVSEIds.Switch(
            //                       () => new JProperty("EVSEIds",                     new JArray(EVSEIds().
            //                                                                                                    OrderBy(id => id).
            //                                                                                                    Select (id => id.ToString()))),

            //                       () => new JProperty("EVSEs",                       new JArray(EVSEs.
            //                                                                                                    OrderBy(evse => evse).
            //                                                                                                    ToJSON (Embedded: true,
            //                                                                                                            ExpandRoamingNetworkId:           InfoStatus.Hidden,
            //                                                                                                            ExpandChargingStationOperatorId:  InfoStatus.Hidden,
            //                                                                                                            ExpandChargingPoolId:             InfoStatus.Hidden,
            //                                                                                                            ExpandChargingStationId:          InfoStatus.Hidden,
            //                                                                                                            ExpandBrandIds:                   InfoStatus.Hidden,
            //                                                                                                            ExpandDataLicenses:               InfoStatus.Hidden)))),


            //             EMobilityProviders.Any()
            //                 ? ExpandEMobilityProviderId.Switch(
            //                       () => new JProperty("eMobilityProviderIds",        new JArray(EMobilityProviders.Select(emp => emp.Id).
            //                                                                                                    OrderBy(id => id).
            //                                                                                                    Select (id => id.ToString()))),

            //                       () => new JProperty("eMobilityProviders",          new JArray(EMobilityProviders.
            //                                                                                                    OrderBy(emp => emp).
            //                                                                                                    ToJSON (Embedded: true,
            //                                                                                                            ExpandRoamingNetworkId:           InfoStatus.Hidden,
            //                                                                                                            ExpandBrandIds:                   ExpandBrandIds,
            //                                                                                                            ExpandDataLicenses:               ExpandDataLicenses))))
            //                 : null

            //             );

            //JSON["gridOperators"] = POIJSON.Children(GridOperators);
            //JSON["transparencySoftware"] = POIJSON.Values(TransparencySoftware, value => value.ToJSON());
            //JSON["transparencySoftwareCertificates"] = POIJSON.Values(TransparencySoftwareCertificates, value => value.ToJSON());
            //JSON["parkingOperators"] = POIJSON.Children(ParkingOperators);
            //JSON["chargingStationManufacturers"] = POIJSON.Children(ChargingStationManufacturers);

            //return POIRepresentation.AddETags(this, CustomRoamingNetworkSerializer is not null
            //           ? CustomRoamingNetworkSerializer(this, JSON)
            //           : JSON);

                             );

            return CustomRoamingNetworkSerializer is not null
                       ? CustomRoamingNetworkSerializer(this, JSON)
                       : JSON;

        }

        #endregion


        #region (private) LogEvent(Logger, LogHandler, ...)

        private Task LogEvent<TDelegate>(TDelegate?                                         Logger,
                                         Func<TDelegate, Task>                              LogHandler,
                                         [CallerArgumentExpression(nameof(Logger))] String  EventName   = "",
                                         [CallerMemberName()]                       String  Command     = "")

            where TDelegate : Delegate

                => LogEvent(
                       nameof(RoamingNetwork),
                       Logger,
                       LogHandler,
                       EventName,
                       Command
                   );

        #endregion


        #region Operator overloading

        #region Operator == (RoamingNetwork1, RoamingNetwork2)

        /// <summary>
        /// Compares two roaming networks for equality.
        /// </summary>
        /// <param name="RoamingNetwork1">A roaming network.</param>
        /// <param name="RoamingNetwork2">Another roaming network.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (RoamingNetwork? RoamingNetwork1,
                                           RoamingNetwork? RoamingNetwork2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(RoamingNetwork1, RoamingNetwork2))
                return true;

            // If one is null, but not both, return false.
            if (RoamingNetwork1 is null || RoamingNetwork2 is null)
                return false;

            return RoamingNetwork1.Equals(RoamingNetwork2);

        }

        #endregion

        #region Operator != (RoamingNetwork1, RoamingNetwork2)

        /// <summary>
        /// Compares two roaming networks for inequality.
        /// </summary>
        /// <param name="RoamingNetwork1">A roaming network.</param>
        /// <param name="RoamingNetwork2">Another roaming network.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (RoamingNetwork? RoamingNetwork1,
                                           RoamingNetwork? RoamingNetwork2)

            => !(RoamingNetwork1 == RoamingNetwork2);

        #endregion

        #region Operator <  (RoamingNetwork1, RoamingNetwork2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetwork1">A roaming network.</param>
        /// <param name="RoamingNetwork2">Another roaming network.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (RoamingNetwork? RoamingNetwork1,
                                          RoamingNetwork? RoamingNetwork2)
        {

            if (RoamingNetwork1 is null)
                throw new ArgumentNullException(nameof(RoamingNetwork1), "The given roaming network must not be null!");

            return RoamingNetwork1.CompareTo(RoamingNetwork2) < 0;

        }

        #endregion

        #region Operator <= (RoamingNetwork1, RoamingNetwork2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetwork1">A roaming network.</param>
        /// <param name="RoamingNetwork2">Another roaming network.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (RoamingNetwork? RoamingNetwork1,
                                           RoamingNetwork? RoamingNetwork2)

            => !(RoamingNetwork1 > RoamingNetwork2);

        #endregion

        #region Operator >  (RoamingNetwork1, RoamingNetwork2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetwork1">A roaming network.</param>
        /// <param name="RoamingNetwork2">Another roaming network.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (RoamingNetwork? RoamingNetwork1,
                                          RoamingNetwork? RoamingNetwork2)
        {

            if (RoamingNetwork1 is null)
                throw new ArgumentNullException(nameof(RoamingNetwork1), "The given roaming network must not be null!");

            return RoamingNetwork1.CompareTo(RoamingNetwork2) > 0;

        }

        #endregion

        #region Operator >= (RoamingNetwork1, RoamingNetwork2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetwork1">A roaming network.</param>
        /// <param name="RoamingNetwork2">Another roaming network.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (RoamingNetwork? RoamingNetwork1,
                                           RoamingNetwork? RoamingNetwork2)

            => !(RoamingNetwork1 < RoamingNetwork2);

        #endregion

        #endregion

        #region IComparable<RoamingNetwork> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two roaming networks.
        /// </summary>
        /// <param name="Object">A roaming network to compare with.</param>
        public override Int32 CompareTo(Object? Object)

            => Object is RoamingNetwork roamingNetwork
                   ? CompareTo(roamingNetwork)
                   : throw new ArgumentException("The given object is not a roaming network!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(RoamingNetwork)

        /// <summary>
        /// Compares two roaming networks.
        /// </summary>
        /// <param name="RoamingNetwork">A roaming network to compare with.</param>
        public Int32 CompareTo(RoamingNetwork? RoamingNetwork)
        {

            if (RoamingNetwork is null)
                throw new ArgumentNullException(nameof(RoamingNetwork),
                                                "The given roaming network must not be null!");

            return Id.CompareTo(RoamingNetwork.Id);

            //ToDo: Compare more properties!

        }

        #endregion

        #endregion

        #region IEquatable<RoamingNetwork> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two roaming networks for equality.
        /// </summary>
        /// <param name="Object">A roaming network to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is RoamingNetwork roamingNetwork &&
                   Equals(roamingNetwork);

        #endregion

        #region Equals(RoamingNetwork)

        /// <summary>
        /// Compares two roaming networks for equality.
        /// </summary>
        /// <param name="RoamingNetwork">A roaming network to compare with.</param>
        public Boolean Equals(RoamingNetwork? RoamingNetwork)

            => RoamingNetwork is not null &&
                   Id.Equals(RoamingNetwork.Id);

        //ToDo: Compare more properties!

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

            => $"'{Name.FirstText()}' ({Id})";

        #endregion


    }

}
