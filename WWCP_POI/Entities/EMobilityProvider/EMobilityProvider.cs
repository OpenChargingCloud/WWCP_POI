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

using System.Runtime.CompilerServices;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Styx.Arrows;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Hermod.Mail;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Concurrent;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// WWCP JSON I/O.
    /// </summary>
    public static partial class JSON_IO
    {

        #region ToJSON(this eMobilityProvider,                      Embedded = false, ...)

        /// <summary>
        /// Return a JSON representation for the given e-mobility provider.
        /// </summary>
        /// <param name="eMobilityProvider">An e-mobility provider.</param>
        /// <param name="Embedded">Whether this data is embedded into another data structure, e.g. into a roaming network.</param>
        public static JObject? ToJSON(this EMobilityProvider  eMobilityProvider,
                                      Boolean                  Embedded                 = false,
                                      InfoStatus               ExpandRoamingNetworkId   = InfoStatus.ShowIdOnly,
                                      InfoStatus               ExpandBrandIds           = InfoStatus.ShowIdOnly,
                                      InfoStatus               ExpandDataLicenses       = InfoStatus.ShowIdOnly)


            => eMobilityProvider is null

                   ? null

                   : POIRepresentation.AddETags(eMobilityProvider, JSONObject.Create(

                         new JProperty("@id",  eMobilityProvider.Id.ToString()),

                         !Embedded
                             ? new JProperty("@context",  "https://open.charging.cloud/contexts/wwcp+json/eMobilityProvider")
                             : null,

                         new JProperty("name",  eMobilityProvider.Name.ToJSON()),

                         eMobilityProvider.Description.IsNotNullOrEmpty()
                             ? new JProperty("description", eMobilityProvider.Description.ToJSON())
                             : null,

                         eMobilityProvider.DataSource is not null
                             ? new JProperty("dataSource", eMobilityProvider.DataSource)
                             : null,

                         ExpandDataLicenses.Switch(
                             () => new JProperty("dataLicenseIds",  new JArray(eMobilityProvider.DataLicenses.SafeSelect(license => license.Id.ToString()))),
                             () => new JProperty("dataLicenses",    eMobilityProvider.DataLicenses.ToJSON())),

                         #region Embedded means it is served as a substructure of e.g. a charging station operator

                         Embedded
                             ? null
                             : ExpandRoamingNetworkId.Switch(
                                   () => new JProperty("roamingNetworkId",   eMobilityProvider.RoamingNetwork.Id. ToString()),
                                   () => new JProperty("roamingNetwork",     eMobilityProvider.RoamingNetwork.    ToJSON(Embedded:                   true,
                                                                                                                         ExpandEMobilityProviderId:  InfoStatus.Hidden,
                                                                                                                         ExpandChargingPoolIds:      InfoStatus.Hidden,
                                                                                                                         ExpandChargingStationIds:   InfoStatus.Hidden,
                                                                                                                         ExpandEVSEIds:              InfoStatus.Hidden,
                                                                                                                         ExpandBrandIds:             InfoStatus.Hidden,
                                                                                                                         ExpandDataLicenses:         InfoStatus.Hidden))),

                         #endregion

                         eMobilityProvider.Address is not null
                             ? new JProperty("address",             eMobilityProvider.Address.ToJSON())
                             : null,

                         // LogoURI
                         // API
                         // MainKeys
                         // RobotKeys
                         // Endpoints
                         // DNS SRV

                         eMobilityProvider.Logo.IsNotNullOrEmpty()
                             ? new JProperty("logos",               JSONArray.Create(
                                                                        JSONObject.Create(
                                                                                  new JProperty("uri",          eMobilityProvider.Logo),
                                                                            new JProperty("description",  I18NString.Empty.ToJSON())
                                                                        )
                                                                    ))
                             : null,

                         eMobilityProvider.Homepage.HasValue
                             ? new JProperty("homepage",            eMobilityProvider.Homepage.ToString())
                             : null,

                         eMobilityProvider.HotlinePhoneNumber.HasValue
                             ? new JProperty("hotline",             eMobilityProvider.HotlinePhoneNumber.ToString())
                             : null

                     ));

        #endregion

        #region ToJSON(this eMobilityProviders, Skip = null, Take = null, Embedded = false, ...)

        /// <summary>
        /// Return a JSON representation for the given enumeration of e-mobility providers.
        /// </summary>
        /// <param name="eMobilityProviders">An enumeration of e-mobility providers.</param>
        /// <param name="Skip">The optional number of e-mobility providers to skip.</param>
        /// <param name="Take">The optional number of e-mobility providers to return.</param>
        public static JArray ToJSON(this IEnumerable<EMobilityProvider>  eMobilityProviders,
                                    UInt64?                               Skip                     = null,
                                    UInt64?                               Take                     = null,
                                    Boolean                               Embedded                 = false,
                                    InfoStatus                            ExpandRoamingNetworkId   = InfoStatus.ShowIdOnly,
                                    InfoStatus                            ExpandBrandIds           = InfoStatus.ShowIdOnly,
                                    InfoStatus                            ExpandDataLicenses       = InfoStatus.ShowIdOnly)


            => eMobilityProviders is null

                   ? new JArray()

                   : new JArray(eMobilityProviders.
                                    Where     (emp => emp is not null).
                                    OrderBy   (emp => emp.Id).
                                    SkipTakeFilter(Skip, Take).
                                    SafeSelect(emp => emp.ToJSON(Embedded,
                                                                 ExpandRoamingNetworkId,
                                                                 ExpandBrandIds,
                                                                 ExpandDataLicenses)));

        #endregion


        #region ToJSON(this eMobilityProviderAdminStatus, Skip = null, Take = null, HistorySize = 1)

        public static JObject ToJSON(this IEnumerable<KeyValuePair<EMobilityProvider_Id, IEnumerable<Timestamped<EMobilityProviderAdminStatusTypes>>>>  eMobilityProviderAdminStatus,
                                     UInt64?                                                                                                            Skip         = null,
                                     UInt64?                                                                                                            Take         = null,
                                     UInt64                                                                                                             HistorySize  = 1)

        {

            #region Initial checks

            if (eMobilityProviderAdminStatus is null || !eMobilityProviderAdminStatus.Any())
                return new JObject();

            var _eMobilityProviderAdminStatus = new Dictionary<EMobilityProvider_Id, IEnumerable<Timestamped<EMobilityProviderAdminStatusTypes>>>();

            #endregion

            #region Maybe there are duplicate eMobilityProvider identifications in the enumeration... take the newest one!

            foreach (var csostatus in Take.HasValue ? eMobilityProviderAdminStatus.Skip(Skip).Take(Take)
                                                    : eMobilityProviderAdminStatus.Skip(Skip))
            {

                if (!_eMobilityProviderAdminStatus.ContainsKey(csostatus.Key))
                    _eMobilityProviderAdminStatus.Add(csostatus.Key, csostatus.Value);

                else if (csostatus.Value.FirstOrDefault().Timestamp > _eMobilityProviderAdminStatus[csostatus.Key].FirstOrDefault().Timestamp)
                    _eMobilityProviderAdminStatus[csostatus.Key] = csostatus.Value;

            }

            #endregion

            return _eMobilityProviderAdminStatus.Count == 0

                   ? new JObject()

                   : new JObject(_eMobilityProviderAdminStatus.
                                     SafeSelect(statuslist => new JProperty(statuslist.Key.ToString(),
                                                                  new JObject(statuslist.Value.

                                                                              // Will filter multiple cso status having the exact same ISO 8601 timestamp!
                                                                              GroupBy          (tsv   => tsv.  Timestamp.ToISO8601()).
                                                                              Select           (group => group.First()).

                                                                              OrderByDescending(tsv   => tsv.Timestamp).
                                                                              Take             (HistorySize).
                                                                              Select           (tsv   => new JProperty(tsv.Timestamp.ToISO8601(),
                                                                                                                       tsv.Value.    ToString())))

                                                              )));

        }

        #endregion


        #region ToJSON(this eMobilityProviderStatus,      Skip = null, Take = null, HistorySize = 1)

        public static JObject ToJSON(this IEnumerable<KeyValuePair<EMobilityProvider_Id, IEnumerable<Timestamped<EMobilityProviderStatusTypes>>>>  eMobilityProviderStatus,
                                     UInt64?                                                                                                       Skip         = null,
                                     UInt64?                                                                                                       Take         = null,
                                     UInt64?                                                                                                       HistorySize  = 1)

        {

            #region Initial checks

            if (eMobilityProviderStatus is null || !eMobilityProviderStatus.Any())
                return new JObject();

            var _eMobilityProviderStatus = new Dictionary<EMobilityProvider_Id, IEnumerable<Timestamped<EMobilityProviderStatusTypes>>>();

            #endregion

            #region Maybe there are duplicate eMobilityProvider identifications in the enumeration... take the newest one!

            foreach (var csostatus in Take.HasValue ? eMobilityProviderStatus.Skip(Skip).Take(Take)
                                                    : eMobilityProviderStatus.Skip(Skip))
            {

                if (!_eMobilityProviderStatus.ContainsKey(csostatus.Key))
                    _eMobilityProviderStatus.Add(csostatus.Key, csostatus.Value);

                else if (csostatus.Value.FirstOrDefault().Timestamp > _eMobilityProviderStatus[csostatus.Key].FirstOrDefault().Timestamp)
                    _eMobilityProviderStatus[csostatus.Key] = csostatus.Value;

            }

            #endregion

            return _eMobilityProviderStatus.Count == 0

                   ? new JObject()

                   : new JObject(_eMobilityProviderStatus.
                                     SafeSelect(statuslist => new JProperty(statuslist.Key.ToString(),
                                                                  new JObject(statuslist.Value.

                                                                              // Will filter multiple cso status having the exact same ISO 8601 timestamp!
                                                                              GroupBy          (tsv   => tsv.  Timestamp.ToISO8601()).
                                                                              Select           (group => group.First()).

                                                                              OrderByDescending(tsv   => tsv.Timestamp).
                                                                              Take             (HistorySize).
                                                                              Select           (tsv   => new JProperty(tsv.Timestamp.ToISO8601(),
                                                                                                                       tsv.Value.    ToString())))

                                                              )));

        }

        #endregion


    }


    /// <summary>
    /// An E-Mobility Provider for lookups which allows to connect
    /// an optional remote E-Mobility Provider.
    /// </summary>
    public sealed partial class EMobilityProvider : AImmutableEMobilityEntity<EMobilityProvider_Id,
                                                      EMobilityProviderAdminStatusTypes,
                                                      EMobilityProviderStatusTypes>
    {


        #region Properties

        /// <summary>
        /// The optional immutable provider priority.
        /// </summary>
        public EMobilityProviderPriority? Priority { get; }


        public RoamingNetwork  RoamingNetwork { get; }


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


        }

        #endregion

        #region Data licenses

        private readonly ConcurrentDictionary<DataLicense_Id, DataLicense> dataLicenses = [];

        /// <summary>
        /// The license of the roaming network data.
        /// </summary>
        [Mandatory]
        public IEnumerable<DataLicense> DataLicenses
            => ImmutablePOIValues.CopyItems(dataLicenses.Values);

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


        }

        #endregion

        #region Telephone

        private PhoneNumber? _Telephone;

        /// <summary>
        /// The telephone number of the operator's (sales) office.
        /// </summary>
        [Optional]
        public PhoneNumber? Telephone
        {

            get
            {
                return _Telephone;
            }


        }

        #endregion

        #region EMailAddress

        private SimpleEMailAddress? _EMailAddress;

        /// <summary>
        /// The e-mail address of the operator's (sales) office.
        /// </summary>
        [Optional]
        public SimpleEMailAddress? EMailAddress
        {

            get
            {
                return _EMailAddress;
            }


        }

        #endregion

        #region Homepage

        private URL? _Homepage;

        /// <summary>
        /// The homepage of this evse operator.
        /// </summary>
        [Optional]
        public URL? Homepage
        {

            get
            {
                return _Homepage;
            }


        }

        #endregion

        #region HotlinePhoneNumber

        private PhoneNumber? _HotlinePhoneNumber;

        /// <summary>
        /// The telephone number of the Charging Station Operator hotline.
        /// </summary>
        [Optional]
        public PhoneNumber? HotlinePhoneNumber
        {

            get
            {
                return _HotlinePhoneNumber;
            }


        }

        #endregion

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new e-mobility (service) provider having the given
        /// unique identification.
        /// </summary>
        /// <param name="Id">The unique e-mobility provider identification.</param>
        /// <param name="RoamingNetwork">The associated roaming network.</param>
        public EMobilityProvider(EMobilityProvider_Id                Id,
                                 RoamingNetwork                      RoamingNetwork,

                                 I18NString?                         Name                             = null,
                                 I18NString?                         Description                      = null,
                                 EMobilityProviderPriority?          Priority                         = null,
                                 EMobilityProviderAdminStatusTypes?  InitialAdminStatus               = null,
                                 EMobilityProviderStatusTypes?       InitialStatus                    = null,
                                 UInt16?                             MaxAdminStatusScheduleSize       = null,
                                 UInt16?                             MaxStatusScheduleSize            = null,

                                 String?                             DataSource                       = null,
                                 DateTimeOffset?                     Created                          = null,
                                 DateTimeOffset?                     LastChange                       = null,

                                 CustomDataNew?                      CustomData                       = null,
                                 UserDefinedDictionary?              InternalData                     = null)

            : base(Id,
                   Name,
                   Description,

                   InitialAdminStatus         ?? EMobilityProviderAdminStatusTypes.Operational,
                   InitialStatus              ?? EMobilityProviderStatusTypes.Available,
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,

                   DataSource,
                   Created,
                   LastChange,

                   CustomData,
                   InternalData)

        {

            this.RoamingNetwork = RoamingNetwork;
            this.Priority = Priority;

        }

        #endregion


        #region (private) LogEvent(Logger, LogHandler, ...)

        private Task LogEvent<TDelegate>(TDelegate?                                         Logger,
                                         Func<TDelegate, Task>                              LogHandler,
                                         [CallerArgumentExpression(nameof(Logger))] String  EventName   = "",
                                         [CallerMemberName()]                       String  Command     = "")

            where TDelegate : Delegate

                => LogEvent(
                       nameof(EMobilityProvider),
                       Logger,
                       LogHandler,
                       EventName,
                       Command
                   );


        private async Task LogEvent<TDelegate>(String                                             WWCPIO,
                                               TDelegate?                                         Logger,
                                               Func<TDelegate, Task>                              LogHandler,
                                               [CallerArgumentExpression(nameof(Logger))] String  EventName   = "",
                                               [CallerMemberName()]                       String  Command     = "")

            where TDelegate : Delegate

        {
            if (Logger is not null)
            {
                try
                {

                    await Task.WhenAll(
                              Logger.GetInvocationList().
                                     OfType<TDelegate>().
                                     Select(LogHandler)
                          );

                }
                catch (Exception e)
                {
                    await HandleErrors(WWCPIO, $"{Command}.{EventName}", e);
                }
            }
        }

        #endregion

        #region (virtual) HandleErrors(Module, Caller, ErrorResponse)

        public override Task HandleErrors(String  Module,
                                         String  Caller,
                                         String  ErrorResponse)
        {

            return Task.CompletedTask;

        }

        #endregion

        #region (virtual) HandleErrors(Module, Caller, ExceptionOccurred)

        public override Task HandleErrors(String     Module,
                                         String     Caller,
                                         Exception  ExceptionOccurred)
        {

            return Task.CompletedTask;

        }

        #endregion


        #region IComparable<eMobilityProvider> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        public override Int32 CompareTo(Object? Object)
        {

            if (Object is null)
                throw new ArgumentNullException("The given object must not be null!");

            if (!(Object is EMobilityProvider eMobilityProvider))
                throw new ArgumentException("The given object is not an eMobilityProvider!");

            return CompareTo(eMobilityProvider);

        }

        #endregion

        #region CompareTo(eMobilityProvider)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="eMobilityProvider">An EVSE_Operator object to compare with.</param>
        public Int32 CompareTo(EMobilityProvider? eMobilityProvider)
        {

            if (eMobilityProvider is null)
                throw new ArgumentNullException("The given EVSE_Operator must not be null!");

            return Id.CompareTo(eMobilityProvider.Id);

        }

        #endregion

        #endregion

        #region IEquatable<eMobilityProvider> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public override Boolean Equals(Object? Object)
        {

            if (Object is null)
                return false;

            if (!(Object is EMobilityProvider eMobilityProvider))
                return false;

            return Equals(eMobilityProvider);

        }

        #endregion

        #region Equals(EVSE_Operator)

        /// <summary>
        /// Compares two eMobilityProviders for equality.
        /// </summary>
        /// <param name="eMobilityProvider">An eMobilityProvider to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public Boolean Equals(EMobilityProvider? eMobilityProvider)
        {

            if (eMobilityProvider is null)
                return false;

            return Id.Equals(eMobilityProvider.Id);

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
