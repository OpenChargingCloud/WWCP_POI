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
    /// The results of a delete EVSEs request.
    /// </summary>
    public class DeleteEVSEsResult : AEnititiesResult<DeleteEVSEResult, EVSE, EVSE_Id>
    {

        #region Constructor(s)

        public DeleteEVSEsResult(CommandResult             Result,
                                 IEnumerable<DeleteEVSEResult>?  SuccessfulEVSEs   = null,
                                 IEnumerable<DeleteEVSEResult>?  RejectedEVSEs     = null,
                                 IId?                            SenderId          = null,
                                 Object?                         Sender            = null,
                                 EventTracking_Id?               EventTrackingId   = null,
                                 I18NString?                     Description       = null,
                                 IEnumerable<Warning>?           Warnings          = null,
                                 TimeSpan?                       Runtime           = null)

            : base(Result,
                   SuccessfulEVSEs,
                   RejectedEVSEs,
                   SenderId,
                   Sender,
                   EventTrackingId,
                   Description,
                   Warnings,
                   Runtime)

        { }

        #endregion


        #region (static) AdminDown    (RejectedEVSEs,   ...)

        public static DeleteEVSEsResult

            AdminDown(IEnumerable<EVSE>     RejectedEVSEs,
                      IId?                   SenderId          = null,
                      Object?                Sender            = null,
                      EventTracking_Id?      EventTrackingId   = null,
                      I18NString?            Description       = null,
                      IEnumerable<Warning>?  Warnings          = null,
                      TimeSpan?              Runtime           = null)

        {

            EventTrackingId ??= EventTracking_Id.New;

            return new (CommandResult.AdminDown,
                        Array.Empty<DeleteEVSEResult>(),
                        RejectedEVSEs.Select(evse => DeleteEVSEResult.AdminDown(evse,
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

        #region (static) NoOperation  (RejectedEVSEs,   ...)

        public static DeleteEVSEsResult

            NoOperation(IEnumerable<EVSE>     RejectedEVSEs,
                        IId?                   SenderId          = null,
                        Object?                Sender            = null,
                        EventTracking_Id?      EventTrackingId   = null,
                        I18NString?            Description       = null,
                        IEnumerable<Warning>?  Warnings          = null,
                        TimeSpan?              Runtime           = null)

        {

            EventTrackingId ??= EventTracking_Id.New;

            return new (CommandResult.NoOperation,
                        Array.Empty<DeleteEVSEResult>(),
                        RejectedEVSEs.Select(evse => DeleteEVSEResult.NoOperation(evse,
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


        #region (static) Enqueued     (SuccessfulEVSEs, ...)

        public static DeleteEVSEsResult

            Enqueued(IEnumerable<EVSE>     SuccessfulEVSEs,
                     IId?                   SenderId          = null,
                     Object?                Sender            = null,
                     EventTracking_Id?      EventTrackingId   = null,
                     I18NString?            Description       = null,
                     IEnumerable<Warning>?  Warnings          = null,
                     TimeSpan?              Runtime           = null)

        {

            EventTrackingId ??= EventTracking_Id.New;

            return new (CommandResult.Enqueued,
                        SuccessfulEVSEs.Select(evse => DeleteEVSEResult.Enqueued(evse,
                                                                                 EventTrackingId,
                                                                                 SenderId,
                                                                                 Sender)),
                        Array.Empty<DeleteEVSEResult>(),
                        SenderId,
                        Sender,
                        EventTrackingId,
                        Description,
                        Warnings,
                        Runtime);

        }

        #endregion

        #region (static) Success      (SuccessfulEVSEs, ...)

        public static DeleteEVSEsResult

            Success(IEnumerable<EVSE>     SuccessfulEVSEs,
                    IId?                   SenderId          = null,
                    Object?                Sender            = null,
                    EventTracking_Id?      EventTrackingId   = null,
                    I18NString?            Description       = null,
                    IEnumerable<Warning>?  Warnings          = null,
                    TimeSpan?              Runtime           = null)

        {

            EventTrackingId ??= EventTracking_Id.New;

            return new (CommandResult.Success,
                        SuccessfulEVSEs.Select(evse => DeleteEVSEResult.Success(evse,
                                                                                EventTrackingId,
                                                                                SenderId,
                                                                                Sender)),
                        Array.Empty<DeleteEVSEResult>(),
                        SenderId,
                        Sender,
                        EventTrackingId,
                        Description,
                        Warnings,
                        Runtime);

        }

        #endregion


        #region (static) ArgumentError(RejectedEVSEs, Description, ...)

        public static DeleteEVSEsResult

            ArgumentError(IEnumerable<EVSE>     RejectedEVSEs,
                          I18NString             Description,
                          EventTracking_Id?      EventTrackingId   = null,
                          IId?                   SenderId          = null,
                          Object?                Sender            = null,
                          IEnumerable<Warning>?  Warnings          = null,
                          TimeSpan?              Runtime           = null)

        {

            EventTrackingId ??= EventTracking_Id.New;

            return new (CommandResult.ArgumentError,
                        Array.Empty<DeleteEVSEResult>(),
                        RejectedEVSEs.Select(evse => DeleteEVSEResult.ArgumentError(evse,
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

        #region (static) Error        (RejectedEVSEs, Description, ...)

        public static DeleteEVSEsResult

            Error(IEnumerable<EVSE>     RejectedEVSEs,
                  I18NString             Description,
                  EventTracking_Id?      EventTrackingId   = null,
                  IId?                   SenderId          = null,
                  Object?                Sender            = null,
                  IEnumerable<Warning>?  Warnings          = null,
                  TimeSpan?              Runtime           = null)

        {

            EventTrackingId ??= EventTracking_Id.New;

            return new (CommandResult.Error,
                        Array.Empty<DeleteEVSEResult>(),
                        RejectedEVSEs.Select(evse => DeleteEVSEResult.Error(evse,
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

        #region (static) Error        (RejectedEVSEs, Exception,   ...)

        public static DeleteEVSEsResult

            Error(IEnumerable<EVSE>     RejectedEVSEs,
                  Exception              Exception,
                  EventTracking_Id?      EventTrackingId   = null,
                  IId?                   SenderId          = null,
                  Object?                Sender            = null,
                  IEnumerable<Warning>?  Warnings          = null,
                  TimeSpan?              Runtime           = null)

        {

            EventTrackingId ??= EventTracking_Id.New;

            return new (CommandResult.Error,
                        Array.Empty<DeleteEVSEResult>(),
                        RejectedEVSEs.Select(evse => DeleteEVSEResult.Error(evse,
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

        #region (static) Timeout      (RejectedEVSEs, Timeout, ...)

        public static DeleteEVSEsResult

            Timeout(IEnumerable<EVSE>     RejectedEVSEs,
                    TimeSpan               Timeout,
                    IId?                   SenderId          = null,
                    Object?                Sender            = null,
                    EventTracking_Id?      EventTrackingId   = null,
                    I18NString?            Description       = null,
                    IEnumerable<Warning>?  Warnings          = null,
                    TimeSpan?              Runtime           = null)

        {

            EventTrackingId ??= EventTracking_Id.New;

            return new (CommandResult.Timeout,
                        Array.Empty<DeleteEVSEResult>(),
                        RejectedEVSEs.Select(evse => DeleteEVSEResult.Timeout(evse,
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

        #region (static) LockTimeout  (RejectedEVSEs, Timeout, ...)

        public static DeleteEVSEsResult

            LockTimeout(IEnumerable<EVSE>     RejectedEVSEs,
                        TimeSpan               Timeout,
                        IId?                   SenderId          = null,
                        Object?                Sender            = null,
                        EventTracking_Id?      EventTrackingId   = null,
                        I18NString?            Description       = null,
                        IEnumerable<Warning>?  Warnings          = null,
                        TimeSpan?              Runtime           = null)

        {

            EventTrackingId ??= EventTracking_Id.New;

            return new (CommandResult.LockTimeout,
                        Array.Empty<DeleteEVSEResult>(),
                        RejectedEVSEs.Select(evse => DeleteEVSEResult.LockTimeout(evse,
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
