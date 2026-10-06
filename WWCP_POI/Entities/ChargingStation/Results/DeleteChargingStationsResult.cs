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
    /// The results of a delete charging stations request.
    /// </summary>
    public class DeleteChargingStationsResult : AEnititiesResult<DeleteChargingStationResult, ChargingStation, ChargingStation_Id>
    {

        #region Constructor(s)

        public DeleteChargingStationsResult(CommandResult                              Result,
                                            IEnumerable<DeleteChargingStationResult>?  SuccessfulChargingStations   = null,
                                            IEnumerable<DeleteChargingStationResult>?  RejectedChargingStations     = null,
                                            IId?                                       SenderId                     = null,
                                            Object?                                    Sender                       = null,
                                            EventTracking_Id?                          EventTrackingId              = null,
                                            I18NString?                                Description                  = null,
                                            IEnumerable<Warning>?                      Warnings                     = null,
                                            TimeSpan?                                  Runtime                      = null)

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

        public static DeleteChargingStationsResult

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
                        Array.Empty<DeleteChargingStationResult>(),
                        RejectedChargingStations.Select(evse => DeleteChargingStationResult.AdminDown(evse,
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

        public static DeleteChargingStationsResult

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
                        Array.Empty<DeleteChargingStationResult>(),
                        RejectedChargingStations.Select(chargingStation => DeleteChargingStationResult.NoOperation(chargingStation,
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

        public static DeleteChargingStationsResult

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
                        SuccessfulChargingStations.Select(chargingStation => DeleteChargingStationResult.Enqueued(chargingStation,
                                                                                                                  EventTrackingId,
                                                                                                                  SenderId,
                                                                                                                  Sender)),
                        Array.Empty<DeleteChargingStationResult>(),
                        SenderId,
                        Sender,
                        EventTrackingId,
                        Description,
                        Warnings,
                        Runtime);

        }

        #endregion

        #region (static) Success      (SuccessfulChargingStations, ...)

        public static DeleteChargingStationsResult

            Success(IEnumerable<ChargingStation>  SuccessfulChargingStations,
                    IId?                           SenderId          = null,
                    Object?                        Sender            = null,
                    EventTracking_Id?              EventTrackingId   = null,
                    I18NString?                    Description       = null,
                    IEnumerable<Warning>?          Warnings          = null,
                    TimeSpan?                      Runtime           = null)

        {

            EventTrackingId ??= EventTracking_Id.New;

            return new (CommandResult.Success,
                        SuccessfulChargingStations.Select(chargingStation => DeleteChargingStationResult.Success(chargingStation,
                                                                                                                 EventTrackingId,
                                                                                                                 SenderId,
                                                                                                                 Sender)),
                        Array.Empty<DeleteChargingStationResult>(),
                        SenderId,
                        Sender,
                        EventTrackingId,
                        Description,
                        Warnings,
                        Runtime);

        }

        #endregion


        #region (static) ArgumentError(RejectedChargingStations, Description, ...)

        public static DeleteChargingStationsResult

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
                        Array.Empty<DeleteChargingStationResult>(),
                        RejectedChargingStations.Select(chargingStation => DeleteChargingStationResult.ArgumentError(chargingStation,
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

        public static DeleteChargingStationsResult

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
                        Array.Empty<DeleteChargingStationResult>(),
                        RejectedChargingStations.Select(chargingStation => DeleteChargingStationResult.Error(chargingStation,
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

        public static DeleteChargingStationsResult

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
                        Array.Empty<DeleteChargingStationResult>(),
                        RejectedChargingStations.Select(chargingStation => DeleteChargingStationResult.Error(chargingStation,
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

        public static DeleteChargingStationsResult

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
                        Array.Empty<DeleteChargingStationResult>(),
                        RejectedChargingStations.Select(chargingStation => DeleteChargingStationResult.Timeout(chargingStation,
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

        public static DeleteChargingStationsResult

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
                        Array.Empty<DeleteChargingStationResult>(),
                        RejectedChargingStations.Select(chargingStation => DeleteChargingStationResult.LockTimeout(chargingStation,
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
