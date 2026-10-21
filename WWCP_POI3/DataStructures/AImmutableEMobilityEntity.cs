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
using System.Runtime.CompilerServices;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// An abstract e-mobility entity.
    /// </summary>
    public abstract class AImmutableEMobilityEntity<TId,
                                                    TAdminStatus,
                                                    TStatus> : AImmutableInternalData,
                                                               IImmutableEntity<TId>,
                                                               IHasId<TId>,
                                                               IAdminStatus<TAdminStatus>,
                                                               IStatus<TStatus>

        where TId          : IId
        where TAdminStatus : IComparable
        where TStatus      : IComparable

    {

        #region Data

        /// <summary>
        /// The default max size of the admin status schedule/history.
        /// </summary>
        public const  UInt16              DefaultMaxAdminStatusScheduleSize    = 50;

        /// <summary>
        /// The default max size of the status schedule/history.
        /// </summary>
        public const  UInt16              DefaultMaxStatusScheduleSize         = 50;

        /// <summary>
        /// Specifies the default encoding used for ETag digests.
        /// </summary>
        public const  ETagDigestEncoding  DefaultETagDigestEncoding            = ETagDigestEncoding.Base64;

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class BasePropertyKeys
        {

            /// <summary>
            /// The property of its identification.
            /// </summary>
            public const String  Id                    = "@id";

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context               = "@context";

            /// <summary>
            /// The property of its multilingual name.
            /// </summary>
            public const String  Name                  = "name";

            /// <summary>
            /// The property of its multilingual description.
            /// </summary>
            public const String  Description           = "description";

            /// <summary>
            /// The property of its data source.
            /// </summary>
            public const String  DataSource            = "dataSource";


            /// <summary>
            /// The property of its data licenses.
            /// </summary>
            public const String  DataLicenses          = "dataLicenses";

            /// <summary>
            /// The property of the identifications of its data licenses, written instead of them when they are not expanded.
            /// </summary>
            public const String  DataLicenseIds        = "dataLicenseIds";


            /// <summary>
            /// The property of its creation timestamp, as of every entity.
            /// </summary>
            public const String  Created               = "created";

            /// <summary>
            /// The property of its last change timestamp, as of every entity.
            /// </summary>
            public const String  LastChange            = "lastChange";

            /// <summary>
            /// The property of its custom data, as of every entity.
            /// </summary>
            public const String  CustomData            = "customData";

            /// <summary>
            /// The property of its ETags, which describe its content without them.
            /// </summary>
            public const String  ETags                 = "eTags";

        }

        #endregion


        /// <summary>
        /// The global unique identification of this entity.
        /// </summary>
        [Mandatory]
        public TId                   Id             { get; }

        /// <summary>
        /// The multi-language name of this entity.
        /// </summary>
        [Optional]
        public ImmutableI18NString   Name           { get; private set; }

        /// <summary>
        /// The multi-language description of this entity.
        /// </summary>
        [Optional]
        public ImmutableI18NString   Description    { get; private set; }

        /// <summary>
        /// The source of this information, e.g. the WWCP importer used.
        /// </summary>
        [Optional]
        public String?               DataSource     { get; private set; }

        /// <summary>
        /// Identifiers of the canonical JSON and deterministic CBOR representations.
        /// </summary>
        public ImmutableArray<ETag>  ETags          { get; protected set; } = [];

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new abstract entity.
        /// </summary>
        /// <param name="Id">The unique entity identification.</param>
        ///
        /// <param name="CustomData">Optional customer specific data, e.g. in combination with custom parsers and serializers.</param>
        /// <param name="InternalData">Optional internal data.</param>
        public AImmutableEMobilityEntity(TId                         Id,
                                         ImmutableI18NString?        Name                         = null,
                                         ImmutableI18NString?        Description                  = null,

                                         Timestamped<TAdminStatus>?  InitialAdminStatus           = null,
                                         Timestamped<TStatus>?       InitialStatus                = null,
                                         UInt16                      MaxAdminStatusScheduleSize   = DefaultMaxAdminStatusScheduleSize,
                                         UInt16                      MaxStatusScheduleSize        = DefaultMaxStatusScheduleSize,

                                         String?                     DataSource                   = null,
                                         DateTimeOffset?             Created                      = null,
                                         DateTimeOffset?             LastChange                   = null,

                                         CustomDataNew?              CustomData                   = null,
                                         UserDefinedDictionary?      InternalData                 = null)

            : base(CustomData,
                   InternalData,
                   Created,
                   LastChange)

        {

            #region Initial checks

            if (Id.IsNullOrEmpty)
                throw new ArgumentNullException(nameof(Id),  "The given identification must not be null or empty!");

            #endregion

            this.Id           = Id;
            this.Name         = Name        ?? ImmutableI18NString.Empty;
            this.Description  = Description ?? ImmutableI18NString.Empty;
            this.DataSource   = DataSource;

            #region AdminStatusSchedule

            this.adminStatusSchedule   = new RuntimeStatusSchedule<TAdminStatus>(MaxAdminStatusScheduleSize);

            if (InitialAdminStatus.HasValue)
                this.adminStatusSchedule.Insert(InitialAdminStatus.Value);

            #endregion

            #region StatusSchedule

            this.statusSchedule = new RuntimeStatusSchedule<TStatus>(MaxStatusScheduleSize);

            if (InitialStatus.     HasValue)
                this.statusSchedule.     Insert(InitialStatus.Value);

            #endregion

        }

        #endregion


        #region (Admin)Status

        internal (UInt16 Admin, UInt16 Status) RuntimeHistorySizes

            => (adminStatusSchedule.MaxStatusHistorySize,
                     statusSchedule.MaxStatusHistorySize);

        internal void RestoreRuntimeHistorySizes((UInt16 Admin, UInt16 Status) sizes)
        {
            adminStatusSchedule.MaxStatusHistorySize = sizes.Admin;
            statusSchedule.     MaxStatusHistorySize = sizes.Status;
        }

        // The admin status and the status belong to the entity, not to one of its versions:
        // a new version takes the schedules of the one before (ShareRuntimeOf), so all versions
        // of an entity show the same, current status. A version raises its status events only while
        // somebody listens to them, so that the versions nobody looks at any more are not kept alive
        // by the schedules they share.

        private readonly Lock runtimeLock = new ();


        /// <summary>
        /// Take the admin status schedule, the status schedule and the internal data of the given
        /// previous version of this entity. Only for a new version nobody listens to yet.
        /// </summary>
        /// <param name="Previous">The previous version of this entity.</param>
        internal void ShareRuntimeOf(AImmutableEMobilityEntity<TId, TAdminStatus, TStatus> Previous)
        {

            if (ReferenceEquals(Previous, this))
                return;

            lock (runtimeLock)
            {

                if (listensToAdminStatus || listensToStatus)
                    throw new InvalidOperationException("Only a new version nobody listens to yet can take the runtime status of the one before!");

                adminStatusSchedule  = Previous.adminStatusSchedule;
                statusSchedule       = Previous.statusSchedule;

            }

            ShareFurtherRuntimeOf(Previous);
            ShareInternalDataOf(Previous);

        }

        /// <summary>
        /// Take further runtime data of the given previous version of this entity, e.g. its energy status.
        /// </summary>
        /// <param name="Previous">The previous version of this entity.</param>
        protected virtual void ShareFurtherRuntimeOf(AImmutableEMobilityEntity<TId, TAdminStatus, TStatus> Previous)
        { }


        #region AdminStatus

        private Boolean listensToAdminStatus;

        #region AdminStatus

        /// <summary>
        /// The current charging station admin status.
        /// </summary>
        [InternalUseOnly]
        public Timestamped<TAdminStatus> AdminStatus
        {

            get
            {
                return adminStatusSchedule.CurrentStatus;
            }

            set
            {
                adminStatusSchedule.Insert(value);
            }

        }

        #endregion

        #region AdminStatusSchedule

        protected RuntimeStatusSchedule<TAdminStatus> adminStatusSchedule;

        /// <summary>
        /// The charging station admin status schedule.
        /// </summary>
        /// <param name="TimestampFilter">An optional admin status timestamp filter.</param>
        /// <param name="AdminStatusFilter">An optional admin status value filter.</param>
        /// <param name="Skip">The number of admin status entries to skip.</param>
        /// <param name="Take">The number of admin status entries to return.</param>
        public IEnumerable<Timestamped<TAdminStatus>> AdminStatusSchedule(Func<DateTimeOffset, Boolean>?  TimestampFilter     = null,
                                                                          Func<TAdminStatus,   Boolean>?  AdminStatusFilter   = null,
                                                                          UInt64?                         Skip                = null,
                                                                          UInt64?                         Take                = null)
        {

            TimestampFilter   ??= timestamp => true;
            AdminStatusFilter ??= status    => true;

            var filteredAdminStatusSchedule = adminStatusSchedule.
                                                  Where(adminStatus => TimestampFilter  (adminStatus.Timestamp)).
                                                  Where(adminStatus => AdminStatusFilter(adminStatus.Value)).
                                                  Skip (Skip ?? 0);

            return Take.HasValue
                       ? filteredAdminStatusSchedule.Take(Take)
                       : filteredAdminStatusSchedule;

        }

        #endregion


        private Task AdminStatusScheduleChanged(DateTimeOffset                       Timestamp,
                                                EventTracking_Id                     EventTrackingId,
                                                RuntimeStatusSchedule<TAdminStatus>  Schedule,
                                                Timestamped<TAdminStatus>            NewStatus,
                                                Timestamped<TAdminStatus>            OldStatus,
                                                Context?                             DataSource)

            => OnAdminStatusScheduleChanged(
                   Timestamp,
                   EventTrackingId,
                   NewStatus,
                   OldStatus,
                   DataSource
               );

        /// <summary>
        /// The current admin status changed: raise the admin status events of the subclass.
        /// </summary>
        protected virtual Task OnAdminStatusScheduleChanged(DateTimeOffset             Timestamp,
                                                            EventTracking_Id           EventTrackingId,
                                                            Timestamped<TAdminStatus>  NewStatus,
                                                            Timestamped<TAdminStatus>  OldStatus,
                                                            Context?                   DataSource)

            => Task.CompletedTask;


        #region SetAdminStatus(NewAdminStatus, DataSource = null)

        /// <summary>
        /// Set the admin status.
        /// </summary>
        /// <param name="NewAdminStatus">A new timestamped admin status.</param>
        /// <param name="DataSource">An optional data source or context for the status update.</param>
        public void SetAdminStatus(TAdminStatus  NewAdminStatus,
                                   Context?      DataSource   = null)
        {

            adminStatusSchedule.Insert(NewAdminStatus,
                                       DataSource);

        }

        #endregion

        #region SetAdminStatus(NewTimestampedAdminStatus, DataSource = null)

        /// <summary>
        /// Set the admin status.
        /// </summary>
        /// <param name="NewTimestampedAdminStatus">A new timestamped admin status.</param>
        /// <param name="DataSource">An optional data source or context for the status update.</param>
        public void SetAdminStatus(Timestamped<TAdminStatus>  NewTimestampedAdminStatus,
                                   Context?                   DataSource   = null)
        {

            adminStatusSchedule.Insert(NewTimestampedAdminStatus,
                                       DataSource);

        }

        #endregion

        #region SetAdminStatus(NewAdminStatus, Timestamp, DataSource = null)

        /// <summary>
        /// Set the admin status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="NewAdminStatus">A new admin status.</param>
        /// <param name="DataSource">An optional data source or context for the status update.</param>
        public void SetAdminStatus(TAdminStatus    NewAdminStatus,
                                   DateTimeOffset  Timestamp,
                                   Context?        DataSource   = null)
        {

            adminStatusSchedule.Insert(NewAdminStatus,
                                       Timestamp,
                                       DataSource);

        }

        #endregion

        #region SetAdminStatus(NewAdminStatusList, ChangeMethod = ChangeMethods.Replace, DataSource = null)

        /// <summary>
        /// Set the timestamped admin status.
        /// </summary>
        /// <param name="NewAdminStatusList">A list of new timestamped admin status.</param>
        /// <param name="ChangeMethod">The change mode.</param>
        /// <param name="DataSource">An optional data source or context for the status update.</param>
        public void SetAdminStatus(IEnumerable<Timestamped<TAdminStatus>>  NewAdminStatusList,
                                   ChangeMethods                           ChangeMethod   = ChangeMethods.Replace,
                                   Context?                                DataSource     = null)
        {

            adminStatusSchedule.Set(NewAdminStatusList,
                                    ChangeMethod,
                                    DataSource);

        }

        #endregion


        /// <summary>
        /// The current admin status, if there is one already.
        /// </summary>
        internal Timestamped<TAdminStatus>? CurrentAdminStatus
            => adminStatusSchedule.Any(status => status.Timestamp <= Timestamp.Now)
                   ? adminStatusSchedule.CurrentStatus
                   : null;

        /// <summary>
        /// The next planned admin status, if any.
        /// </summary>
        internal Timestamped<TAdminStatus>? NextAdminStatus
            => adminStatusSchedule.NextStatus;


        /// <summary>
        /// Raise the admin status events of this version while Listen is true.
        /// Called by the events of a subclass when they get their first handler, or lose their last one.
        /// </summary>
        protected void ListenToAdminStatus(Boolean Listen)
        {
            lock (runtimeLock)
            {

                if (Listen == listensToAdminStatus)
                    return;

                if (Listen)
                    adminStatusSchedule.OnStatusChanged += AdminStatusScheduleChanged;
                else
                    adminStatusSchedule.OnStatusChanged -= AdminStatusScheduleChanged;

                listensToAdminStatus = Listen;

            }
        }

        #endregion

        #region Status

        private Boolean listensToStatus;

        #region Status

        /// <summary>
        /// The current charging station status.
        /// </summary>
        [InternalUseOnly]
        public Timestamped<TStatus> Status
        {

            get
            {
                return statusSchedule.CurrentStatus;
            }

            set
            {
                statusSchedule.Insert(value);
            }

        }

        #endregion

        #region StatusSchedule

        protected RuntimeStatusSchedule<TStatus> statusSchedule;

        /// <summary>
        /// The charging station status schedule.
        /// </summary>
        /// <param name="TimestampFilter">An optional status timestamp filter.</param>
        /// <param name="StatusFilter">An optional status value filter.</param>
        /// <param name="Skip">The number of status entries to skip.</param>
        /// <param name="Take">The number of status entries to return.</param>
        public IEnumerable<Timestamped<TStatus>> StatusSchedule(Func<DateTimeOffset, Boolean>?  TimestampFilter   = null,
                                                                Func<TStatus,        Boolean>?  StatusFilter      = null,
                                                                UInt64?                         Skip              = null,
                                                                UInt64?                         Take              = null)
        {

             TimestampFilter ??= timestamp => true;
             StatusFilter    ??= status    => true;

             var filteredStatusSchedule = statusSchedule.
                                              Where(status => TimestampFilter(status.Timestamp)).
                                              Where(status => StatusFilter   (status.Value)).
                                              Skip (Skip ?? 0);

             return Take.HasValue
                        ? filteredStatusSchedule.Take(Take)
                        : filteredStatusSchedule;

        }

        #endregion

        private Task StatusScheduleChanged(DateTimeOffset                  Timestamp,
                                           EventTracking_Id                EventTrackingId,
                                           RuntimeStatusSchedule<TStatus>  Schedule,
                                           Timestamped<TStatus>            NewStatus,
                                           Timestamped<TStatus>            OldStatus,
                                           Context?                        DataSource)

            => OnStatusScheduleChanged(
                   Timestamp,
                   EventTrackingId,
                   NewStatus,
                   OldStatus,
                   DataSource
               );

        /// <summary>
        /// The current status changed: raise the status events of the subclass.
        /// </summary>
        protected virtual Task OnStatusScheduleChanged(DateTimeOffset        Timestamp,
                                                       EventTracking_Id      EventTrackingId,
                                                       Timestamped<TStatus>  NewStatus,
                                                       Timestamped<TStatus>  OldStatus,
                                                       Context?              DataSource)

            => Task.CompletedTask;


        #region SetStatus(NewStatus, DataSource = null)

        /// <summary>
        /// Set the current status.
        /// </summary>
        /// <param name="NewStatus">A new status.</param>
        /// <param name="DataSource">An optional data source or context for the status update.</param>
        public void SetStatus(TStatus   NewStatus,
                              Context?  DataSource   = null)
        {

            statusSchedule.Insert(NewStatus,
                                  DataSource);

        }

        #endregion

        #region SetStatus(NewTimestampedStatus, DataSource = null)

        /// <summary>
        /// Set the current status.
        /// </summary>
        /// <param name="NewTimestampedStatus">A new timestamped status.</param>
        /// <param name="DataSource">An optional data source or context for the status update.</param>
        public void SetStatus(Timestamped<TStatus>  NewTimestampedStatus,
                              Context?              DataSource   = null)
        {

            statusSchedule.Insert(NewTimestampedStatus,
                                  DataSource);

        }

        #endregion

        #region SetStatus(NewStatus, Timestamp, DataSource = null)

        /// <summary>
        /// Set the status.
        /// </summary>
        /// <param name="NewStatus">A new status.</param>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="DataSource">An optional data source or context for the status update.</param>
        public void SetStatus(TStatus         NewStatus,
                              DateTimeOffset  Timestamp,
                              Context?        DataSource   = null)
        {

            statusSchedule.Insert(NewStatus,
                                  Timestamp,
                                  DataSource);

        }

        #endregion

        #region SetStatus(NewStatusList, ChangeMethod = ChangeMethods.Replace, DataSource = null)

        /// <summary>
        /// Set the timestamped status.
        /// </summary>
        /// <param name="NewStatusList">A list of new timestamped status.</param>
        /// <param name="ChangeMethod">The change mode.</param>
        /// <param name="DataSource">An optional data source or context for the status update.</param>
        public void SetStatus(IEnumerable<Timestamped<TStatus>>  NewStatusList,
                              ChangeMethods                      ChangeMethod   = ChangeMethods.Replace,
                              Context?                           DataSource     = null)
        {

            statusSchedule.Set(NewStatusList,
                               ChangeMethod,
                               DataSource);

        }

        #endregion


        /// <summary>
        /// The current status, if there is one already.
        /// </summary>
        internal Timestamped<TStatus>? CurrentStatus
            => statusSchedule.Any(status => status.Timestamp <= Timestamp.Now)
                   ? statusSchedule.CurrentStatus
                   : null;

        /// <summary>
        /// The next planned status, if any.
        /// </summary>
        internal Timestamped<TStatus>? NextStatus
            => statusSchedule.NextStatus;


        /// <summary>
        /// Raise the status events of this version while Listen is true.
        /// Called by the events of a subclass when they get their first handler, or lose their last one.
        /// </summary>
        protected void ListenToStatus(Boolean Listen)
        {
            lock (runtimeLock)
            {

                if (Listen == listensToStatus)
                    return;

                if (Listen)
                    statusSchedule.OnStatusChanged += StatusScheduleChanged;
                else
                    statusSchedule.OnStatusChanged -= StatusScheduleChanged;

                listensToStatus = Listen;

            }
        }

        #endregion

        #endregion

        #region ETags

        #region (private) MergeWithETags(ETag)

        /// <summary>
        /// Merge the new ETag with the existing ones, replacing only the ETag of the same format
        /// and keeping the others, ordered by their format: JSON before CBOR.
        /// </summary>
        /// <param name="ETag">The new ETag.</param>
        private ImmutableArray<ETag> MergeWithETags(ETag ETag)
        {

            ETags = [ .. (ETags.IsDefault ? [] : ETags).
                             Where  (eTag => eTag.Format != ETag.Format).
                             Append (ETag).
                             OrderBy(eTag => eTag.Format) ];

            return ETags;

        }

        #endregion

        #region CalcETags (JSON, Replace = true)

        /// <summary>
        /// Calculate the ETag(s) of the given canonical JSON representation and store them in this entity.
        /// </summary>
        /// <param name="JSON">The canonical JSON representation.</param>
        /// <param name="Replace">Whether to replace the existing ETags or merge with them.</param>
        public ImmutableArray<ETag> CalcETags(JObject  JSON,
                                              Boolean  Replace   = true)
        {

            var eTag = ETag.Compute(
                           ETagFormat.JSON,
                           CanonicalJSON.ToUTF8Bytes(JSON)
                       );

            if (Replace)
            {
                ETags = [ eTag ];
                return ETags;
            }

            return MergeWithETags(eTag);

        }

        #endregion

        #region CalcETags (CBOR, Replace = true)

        /// <summary>
        /// Calculate the ETag(s) of the given deterministic CBOR representation and store them in this entity.
        /// </summary>
        /// <param name="CBOR">The deterministic CBOR representation.</param>
        /// <param name="Replace">Whether to replace the existing ETags or merge with them.</param>
        public ImmutableArray<ETag> CalcETags(CBORValue  CBOR,
                                              Boolean    Replace   = true)
        {

            var eTag = ETag.Compute(
                           ETagFormat.CBOR,
                           CBOR.ToByteArray(CBORWriterOptions.Canonical)
                       );

            if (Replace)
            {
                ETags = [ eTag ];
                return ETags;
            }

            return MergeWithETags(eTag);

        }

        #endregion

        #region CalcETags (JSON, CBOR)

        /// <summary>
        /// Calculate the ETag(s) of the given canonical JSON and deterministic CBOR representations
        /// and store them in this entity.
        /// </summary>
        /// <param name="JSON">The canonical JSON representation.</param>
        /// <param name="CBOR">The deterministic CBOR representation.</param>
        public ImmutableArray<ETag> CalcETags(JObject   JSON,
                                              CBORValue CBOR)
        {

            ETags = [

                        ETag.Compute(
                            ETagFormat.JSON,
                            CanonicalJSON.ToUTF8Bytes(JSON)
                        ),

                        ETag.Compute(
                            ETagFormat.CBOR,
                            CBOR.ToByteArray(CBORWriterOptions.Canonical)
                        )

                    ];

            return ETags;

        }

        #endregion


        #region ValidateETags (JSON, out ErrorResponse)

        /// <summary>
        /// Validate the ETags declared in the given JSON representation of this entity:
        /// The JSON ETag must be the ETag of the representation without its ETags,
        /// every other declared ETag must equal the stored ETag of its format, if one is stored.
        /// The representation must be the one the ETags were calculated over, e.g. not an embedded one.
        /// </summary>
        /// <param name="JSON">The JSON representation including its ETags.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public Boolean ValidateETags(JObject                           JSON,
                                     [NotNullWhen(false)] out String?  ErrorResponse)
        {

            if (JSON?[BasePropertyKeys.ETags] is not JArray eTagsJSON)
            {
                ErrorResponse = "The JSON representation has no ETags!";
                return false;
            }

            var declaredETags = new List<ETag>();

            foreach (var eTagJSON in eTagsJSON)
            {
                try
                {
                    declaredETags.Add(ETag.Parse(eTagJSON));
                }
                catch (Exception e)
                {
                    ErrorResponse = "Invalid ETag: " + e.Message;
                    return false;
                }
            }

            var content = (JObject) JSON.DeepClone();
            content.Remove(BasePropertyKeys.ETags);

            return ValidateETags(
                       declaredETags,
                       ETag.Compute(
                           ETagFormat.JSON,
                           CanonicalJSON.ToUTF8Bytes(content)
                       ),
                       out ErrorResponse
                   );

        }

        #endregion

        #region ValidateETags (CBOR, out ErrorResponse)

        /// <summary>
        /// Validate the ETags declared in the given CBOR representation of this entity:
        /// The CBOR ETag must be the ETag of the representation without its ETags,
        /// every other declared ETag must equal the stored ETag of its format, if one is stored.
        /// The representation must be the one the ETags were calculated over, e.g. not an embedded one.
        /// </summary>
        /// <param name="CBOR">The CBOR representation including its ETags.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public Boolean ValidateETags(CBORValue                         CBOR,
                                     [NotNullWhen(false)] out String?  ErrorResponse)
        {

            if (CBOR.Kind != CBORValueKind.Map ||
               !CBOR.TryGetValue(CBORValue.FromText(BasePropertyKeys.ETags), out var eTagsCBOR) ||
                eTagsCBOR.Kind != CBORValueKind.Array)
            {
                ErrorResponse = "The CBOR representation has no ETags!";
                return false;
            }

            var declaredETags = new List<ETag>();

            foreach (var eTagCBOR in eTagsCBOR.AsArray())
            {
                try
                {
                    declaredETags.Add(ETag.Parse(eTagCBOR));
                }
                catch (Exception e)
                {
                    ErrorResponse = "Invalid ETag: " + e.Message;
                    return false;
                }
            }

            var content = CBORValue.FromMap(CBOR.AsMap().Where(entry => !(entry.Key.TryGetText(out var key) && key == BasePropertyKeys.ETags)));

            return ValidateETags(
                       declaredETags,
                       ETag.Compute(
                           ETagFormat.CBOR,
                           content.ToByteArray(CBORWriterOptions.Canonical)
                       ),
                       out ErrorResponse
                   );

        }

        #endregion

        #region (private) ValidateETags (DeclaredETags, CalculatedETag, out ErrorResponse)

        /// <summary>
        /// Validate the declared ETags against the ETag calculated over the content
        /// and against the stored ETags of the other formats.
        /// </summary>
        /// <param name="DeclaredETags">The ETags declared in a representation.</param>
        /// <param name="CalculatedETag">The ETag calculated over the representation without its ETags.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        private Boolean ValidateETags(IReadOnlyList<ETag>               DeclaredETags,
                                      ETag                              CalculatedETag,
                                      [NotNullWhen(false)] out String?  ErrorResponse)
        {

            var duplicateFormat = DeclaredETags.GroupBy(eTag => eTag.Format).FirstOrDefault(group => group.Count() > 1);

            if (duplicateFormat is not null)
            {
                ErrorResponse = $"The {duplicateFormat.Key} ETag must not be declared twice!";
                return false;
            }

            var declaredETags = DeclaredETags.Where(eTag => eTag.Format == CalculatedETag.Format).ToArray();

            if (declaredETags.Length == 0)
            {
                ErrorResponse = $"The {CalculatedETag.Format} ETag is missing!";
                return false;
            }

            if (!declaredETags[0].Equals(CalculatedETag))
            {
                ErrorResponse = $"The {CalculatedETag.Format} ETag does not match the content!";
                return false;
            }

            foreach (var declaredETag in DeclaredETags.Where(eTag => eTag.Format != CalculatedETag.Format))
            {

                var storedETags = (ETags.IsDefault ? [] : ETags).Where(eTag => eTag.Format == declaredETag.Format).ToArray();

                if (storedETags.Length > 0 && !storedETags[0].Equals(declaredETag))
                {
                    ErrorResponse = $"The {declaredETag.Format} ETag does not match the stored one!";
                    return false;
                }

            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #endregion

        #region Timestamps

        /// <summary>
        /// Restore persisted timestamps on a newly parsed entity without recording a change.
        /// </summary>
        internal void RestoreSnapshotTimestamps(DateTimeOffset?  Created,
                                                DateTimeOffset?  LastChange)

            => RestoreTimestamps(
                   Created    ?? base.Created,
                   LastChange ?? base.LastChangeDate
               );

        #endregion


        #region (protected) LogEvent(Logger, LogHandler, ...)

        protected async Task LogEvent<TDelegate>(String                                             WWCPIO,
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

        public virtual Task HandleErrors(String  Module,
                                         String  Caller,
                                         String  ErrorResponse)
        {

            return Task.CompletedTask;

        }

        #endregion

        #region (virtual) HandleErrors(Module, Caller, ExceptionOccurred)

        public virtual Task HandleErrors(String     Module,
                                         String     Caller,
                                         Exception  ExceptionOccurred)
        {

            return Task.CompletedTask;

        }

        #endregion


        public abstract Int32 CompareTo(Object? obj);


    }

}
