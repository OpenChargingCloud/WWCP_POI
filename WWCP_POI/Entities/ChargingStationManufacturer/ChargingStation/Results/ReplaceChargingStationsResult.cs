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

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The results of a replace charging stations request.
    /// </summary>
    public class ReplaceChargingStationsResult : AEnititiesResult<AddOrUpdateChargingStationResult,
                                                                  ChargingStation,
                                                                  ChargingStation_Id>
    {

        #region Constructor(s)

        public ReplaceChargingStationsResult(CommandResult                                   Result,
                                             IEnumerable<AddOrUpdateChargingStationResult>?  SuccessfulChargingStations   = null,
                                             IEnumerable<AddOrUpdateChargingStationResult>?  RejectedChargingStations     = null,
                                             IId?                                            SenderId                     = null,
                                             Object?                                         Sender                       = null,
                                             EventTracking_Id?                               EventTrackingId              = null,
                                             I18NString?                                     Description                  = null,
                                             IEnumerable<Warning>?                           Warnings                     = null,
                                             TimeSpan?                                       Runtime                      = null)

            : base(Result,
                   SuccessfulChargingStations,
                   RejectedChargingStations,
                   SenderId,
                   Sender,
                   EventTrackingId,
                   Description,
                   Warnings,
                   Runtime)

        { }

        #endregion


        #region (static) AdminDown    (RejectedChargingStations,   ...)

        public static ReplaceChargingStationsResult

            AdminDown(IEnumerable<ChargingStation>  RejectedChargingStations,
                      IId?                           SenderId          = null,
                      Object?                        Sender            = null,
                      EventTracking_Id?              EventTrackingId   = null,
                      I18NString?                    Description       = null,
                      IEnumerable<Warning>?          Warnings          = null,
                      TimeSpan?                      Runtime           = null)

        {

            EventTrackingId ??= EventTracking_Id.New;

            return new (CommandResult.AdminDown,
                        Array.Empty<AddOrUpdateChargingStationResult>(),
                        RejectedChargingStations.Select(evse => AddOrUpdateChargingStationResult.AdminDown(evse,
                                                                                                           EventTrackingId,
                                                                                                           SenderId,
                                                                                                           Sender)),
                        SenderId,
                        Sender,
                        EventTrackingId,
                        Description,
                        Warnings,
                        Runtime);

        }

        #endregion

        #region (static) NoOperation  (RejectedChargingStations,   ...)

        public static ReplaceChargingStationsResult

            NoOperation(IEnumerable<ChargingStation>  RejectedChargingStations,
                        IId?                           SenderId          = null,
                        Object?                        Sender            = null,
                        EventTracking_Id?              EventTrackingId   = null,
                        I18NString?                    Description       = null,
                        IEnumerable<Warning>?          Warnings          = null,
                        TimeSpan?                      Runtime           = null)

        {

            EventTrackingId ??= EventTracking_Id.New;

            return new (CommandResult.NoOperation,
                        Array.Empty<AddOrUpdateChargingStationResult>(),
                        RejectedChargingStations.Select(chargingStation => AddOrUpdateChargingStationResult.NoOperation(chargingStation,
                                                                                                                        EventTrackingId,
                                                                                                                        SenderId,
                                                                                                                        Sender)),
                        SenderId,
                        Sender,
                        EventTrackingId,
                        Description,
                        Warnings,
                        Runtime);

        }

        #endregion


        #region (static) Enqueued     (SuccessfulChargingStations, ...)

        public static ReplaceChargingStationsResult

            Enqueued(IEnumerable<ChargingStation>  SuccessfulChargingStations,
                     IId?                           SenderId          = null,
                     Object?                        Sender            = null,
                     EventTracking_Id?              EventTrackingId   = null,
                     I18NString?                    Description       = null,
                     IEnumerable<Warning>?          Warnings          = null,
                     TimeSpan?                      Runtime           = null)

        {

            EventTrackingId ??= EventTracking_Id.New;

            return new (CommandResult.Enqueued,
                        SuccessfulChargingStations.Select(chargingStation => AddOrUpdateChargingStationResult.Enqueued(chargingStation,
                                                                                                                       EventTrackingId,
                                                                                                                       SenderId,
                                                                                                                       Sender)),
                        Array.Empty<AddOrUpdateChargingStationResult>(),
                        SenderId,
                        Sender,
                        EventTrackingId,
                        Description,
                        Warnings,
                        Runtime);

        }

        #endregion

        #region (static) Added        (SuccessfulChargingStations, ...)

        public static ReplaceChargingStationsResult

            Added(IEnumerable<ChargingStation>  SuccessfulChargingStations,
                  IId?                           SenderId          = null,
                  Object?                        Sender            = null,
                  EventTracking_Id?              EventTrackingId   = null,
                  I18NString?                    Description       = null,
                  IEnumerable<Warning>?          Warnings          = null,
                  TimeSpan?                      Runtime           = null)

        {

            EventTrackingId ??= EventTracking_Id.New;

            return new (CommandResult.Success,
                        SuccessfulChargingStations.Select(chargingStation => AddOrUpdateChargingStationResult.Added(chargingStation,
                                                                                                                    EventTrackingId,
                                                                                                                    SenderId,
                                                                                                                    Sender)),
                        Array.Empty<AddOrUpdateChargingStationResult>(),
                        SenderId,
                        Sender,
                        EventTrackingId,
                        Description,
                        Warnings,
                        Runtime);

        }

        #endregion

        #region (static) Updated      (SuccessfulChargingStations, ...)

        public static ReplaceChargingStationsResult

            Updated(IEnumerable<ChargingStation>  SuccessfulChargingStations,
                    IId?                           SenderId          = null,
                    Object?                        Sender            = null,
                    EventTracking_Id?              EventTrackingId   = null,
                    I18NString?                    Description       = null,
                    IEnumerable<Warning>?          Warnings          = null,
                    TimeSpan?                      Runtime           = null)

        {

            EventTrackingId ??= EventTracking_Id.New;

            return new (CommandResult.Success,
                        SuccessfulChargingStations.Select(chargingStation => AddOrUpdateChargingStationResult.Updated(chargingStation,
                                                                                                                      EventTrackingId,
                                                                                                                      SenderId,
                                                                                                                      Sender)),
                        Array.Empty<AddOrUpdateChargingStationResult>(),
                        SenderId,
                        Sender,
                        EventTrackingId,
                        Description,
                        Warnings,
                        Runtime);

        }

        #endregion


        #region (static) ArgumentError(RejectedChargingStations, Description, ...)

        public static ReplaceChargingStationsResult

            ArgumentError(IEnumerable<ChargingStation>  RejectedChargingStations,
                          I18NString                     Description,
                          EventTracking_Id?              EventTrackingId   = null,
                          IId?                           SenderId          = null,
                          Object?                        Sender            = null,
                          IEnumerable<Warning>?          Warnings          = null,
                          TimeSpan?                      Runtime           = null)

        {

            EventTrackingId ??= EventTracking_Id.New;

            return new (CommandResult.ArgumentError,
                        Array.Empty<AddOrUpdateChargingStationResult>(),
                        RejectedChargingStations.Select(chargingStation => AddOrUpdateChargingStationResult.ArgumentError(chargingStation,
                                                                                                                          Description,
                                                                                                                          EventTrackingId,
                                                                                                                          SenderId,
                                                                                                                          Sender)),
                        SenderId,
                        Sender,
                        EventTrackingId,
                        Description,
                        Warnings,
                        Runtime);

        }

        #endregion

        #region (static) Error        (RejectedChargingStations, Description, ...)

        public static ReplaceChargingStationsResult

            Error(IEnumerable<ChargingStation>  RejectedChargingStations,
                  I18NString                     Description,
                  EventTracking_Id?              EventTrackingId   = null,
                  IId?                           SenderId          = null,
                  Object?                        Sender            = null,
                  IEnumerable<Warning>?          Warnings          = null,
                  TimeSpan?                      Runtime           = null)

        {

            EventTrackingId ??= EventTracking_Id.New;

            return new (CommandResult.Error,
                        Array.Empty<AddOrUpdateChargingStationResult>(),
                        RejectedChargingStations.Select(chargingStation => AddOrUpdateChargingStationResult.Error(chargingStation,
                                                                                                                  Description,
                                                                                                                  EventTrackingId,
                                                                                                                  SenderId,
                                                                                                                  Sender)),
                        SenderId,
                        Sender,
                        EventTrackingId,
                        Description,
                        Warnings,
                        Runtime);

        }

        #endregion

        #region (static) Error        (RejectedChargingStations, Exception,   ...)

        public static ReplaceChargingStationsResult

            Error(IEnumerable<ChargingStation>  RejectedChargingStations,
                  Exception                      Exception,
                  EventTracking_Id?              EventTrackingId   = null,
                  IId?                           SenderId          = null,
                  Object?                        Sender            = null,
                  IEnumerable<Warning>?          Warnings          = null,
                  TimeSpan?                      Runtime           = null)

        {

            EventTrackingId ??= EventTracking_Id.New;

            return new (CommandResult.Error,
                        Array.Empty<AddOrUpdateChargingStationResult>(),
                        RejectedChargingStations.Select(chargingStation => AddOrUpdateChargingStationResult.Error(chargingStation,
                                                                                                                  Exception,
                                                                                                                  EventTrackingId,
                                                                                                                  SenderId,
                                                                                                                  Sender)),
                        SenderId,
                        Sender,
                        EventTrackingId,
                        Exception.Message.ToI18NString(),
                        Warnings,
                        Runtime);

        }

        #endregion

        #region (static) Timeout      (RejectedChargingStations, Timeout, ...)

        public static ReplaceChargingStationsResult

            Timeout(IEnumerable<ChargingStation>  RejectedChargingStations,
                    TimeSpan                       Timeout,
                    IId?                           SenderId          = null,
                    Object?                        Sender            = null,
                    EventTracking_Id?              EventTrackingId   = null,
                    I18NString?                    Description       = null,
                    IEnumerable<Warning>?          Warnings          = null,
                    TimeSpan?                      Runtime           = null)

        {

            EventTrackingId ??= EventTracking_Id.New;

            return new (CommandResult.Timeout,
                        Array.Empty<AddOrUpdateChargingStationResult>(),
                        RejectedChargingStations.Select(chargingStation => AddOrUpdateChargingStationResult.Timeout(chargingStation,
                                                                                                                    Timeout,
                                                                                                                    EventTrackingId,
                                                                                                                    SenderId,
                                                                                                                    Sender)),
                        SenderId,
                        Sender,
                        EventTrackingId,
                        Description,
                        Warnings,
                        Runtime);

        }

        #endregion

        #region (static) LockTimeout  (RejectedChargingStations, Timeout, ...)

        public static ReplaceChargingStationsResult

            LockTimeout(IEnumerable<ChargingStation>  RejectedChargingStations,
                        TimeSpan                       Timeout,
                        IId?                           SenderId          = null,
                        Object?                        Sender            = null,
                        EventTracking_Id?              EventTrackingId   = null,
                        I18NString?                    Description       = null,
                        IEnumerable<Warning>?          Warnings          = null,
                        TimeSpan?                      Runtime           = null)

        {

            EventTrackingId ??= EventTracking_Id.New;

            return new (CommandResult.LockTimeout,
                        Array.Empty<AddOrUpdateChargingStationResult>(),
                        RejectedChargingStations.Select(chargingStation => AddOrUpdateChargingStationResult.LockTimeout(chargingStation,
                                                                                                                        Timeout,
                                                                                                                        EventTrackingId,
                                                                                                                        SenderId,
                                                                                                                        Sender)),
                        SenderId,
                        Sender,
                        EventTrackingId,
                        Description,
                        Warnings,
                        Runtime);

        }

        #endregion


    }

}
