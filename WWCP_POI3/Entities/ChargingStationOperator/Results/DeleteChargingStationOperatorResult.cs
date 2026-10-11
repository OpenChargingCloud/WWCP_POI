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
    /// The result of a delete charging station operator request.
    /// </summary>
    public class DeleteChargingStationOperatorResult : AEnitityResult<ChargingStationOperator, ChargingStationOperator_Id>
    {

        #region Properties

        public ChargingStationOperator?  ChargingStationOperator
            => Entity;

        public RoamingNetwork?           RoamingNetwork    { get; internal set; }

        #endregion

        #region Constructor(s)

        public DeleteChargingStationOperatorResult(ChargingStationOperator  ChargingStationOperator,
                                                   CommandResult       Result,
                                                   EventTracking_Id?         EventTrackingId   = null,
                                                   IId?                      SenderId          = null,
                                                   Object?                   Sender            = null,
                                                   RoamingNetwork?          RoamingNetwork    = null,
                                                   I18NString?               Description       = null,
                                                   IEnumerable<Warning>?     Warnings          = null,
                                                   TimeSpan?                 Runtime           = null)

            : base(ChargingStationOperator,
                   Result,
                   EventTrackingId,
                   SenderId,
                   Sender,
                   Description,
                   Warnings,
                   Runtime)

        {

            this.RoamingNetwork = RoamingNetwork;

        }


        public DeleteChargingStationOperatorResult(ChargingStationOperator_Id  ChargingStationOperatorId,
                                                   CommandResult         Result,
                                                   EventTracking_Id?           EventTrackingId   = null,
                                                   IId?                        SenderId          = null,
                                                   Object?                     Sender            = null,
                                                   RoamingNetwork?            RoamingNetwork    = null,
                                                   I18NString?                 Description       = null,
                                                   IEnumerable<Warning>?       Warnings          = null,
                                                   TimeSpan?                   Runtime           = null)

            : base(ChargingStationOperatorId,
                   Result,
                   EventTrackingId,
                   SenderId,
                   Sender,
                   Description,
                   Warnings,
                   Runtime)

        {

            this.RoamingNetwork = RoamingNetwork;

        }

        #endregion


        #region (static) AdminDown      (ChargingStationOperator, ...)

        public static DeleteChargingStationOperatorResult

            AdminDown(ChargingStationOperator  ChargingStationOperator,
                      EventTracking_Id?         EventTrackingId   = null,
                      IId?                      SenderId          = null,
                      Object?                   Sender            = null,
                      RoamingNetwork?          RoamingNetwork    = null,
                      I18NString?               Description       = null,
                      IEnumerable<Warning>?     Warnings          = null,
                      TimeSpan?                 Runtime           = null)

                => new (ChargingStationOperator,
                        CommandResult.AdminDown,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        RoamingNetwork,
                        Description,
                        Warnings,
                        Runtime);

        #endregion

        #region (static) NoOperation    (ChargingStationOperator, ...)

        public static DeleteChargingStationOperatorResult

            NoOperation(ChargingStationOperator  ChargingStationOperator,
                        EventTracking_Id?         EventTrackingId   = null,
                        IId?                      SenderId          = null,
                        Object?                   Sender            = null,
                        RoamingNetwork?          RoamingNetwork    = null,
                        I18NString?               Description       = null,
                        IEnumerable<Warning>?     Warnings          = null,
                        TimeSpan?                 Runtime           = null)

                => new (ChargingStationOperator,
                        CommandResult.NoOperation,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        RoamingNetwork,
                        Description,
                        Warnings,
                        Runtime);

        #endregion


        #region (static) Enqueued       (ChargingStationOperator, ...)

        public static DeleteChargingStationOperatorResult

            Enqueued(ChargingStationOperator  ChargingStationOperator,
                     EventTracking_Id?         EventTrackingId   = null,
                     IId?                      SenderId          = null,
                     Object?                   Sender            = null,
                     RoamingNetwork?          RoamingNetwork    = null,
                     I18NString?               Description       = null,
                     IEnumerable<Warning>?     Warnings          = null,
                     TimeSpan?                 Runtime           = null)

                => new (ChargingStationOperator,
                        CommandResult.Enqueued,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        RoamingNetwork,
                        Description,
                        Warnings,
                        Runtime);

        #endregion

        #region (static) Success        (ChargingStationOperator, ...)

        public static DeleteChargingStationOperatorResult

            Success(ChargingStationOperator  ChargingStationOperator,
                    EventTracking_Id?         EventTrackingId   = null,
                    IId?                      SenderId          = null,
                    Object?                   Sender            = null,
                    RoamingNetwork?          RoamingNetwork    = null,
                    I18NString?               Description       = null,
                    IEnumerable<Warning>?     Warnings          = null,
                    TimeSpan?                 Runtime           = null)

                => new (ChargingStationOperator,
                        CommandResult.Success,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        RoamingNetwork,
                        Description,
                        Warnings,
                        Runtime);

        #endregion


        #region (static) CanNotBeRemoved(ChargingStationOperator, ...)

        public static DeleteChargingStationOperatorResult

            CanNotBeRemoved(ChargingStationOperator  ChargingStationOperator,
                            EventTracking_Id?         EventTrackingId   = null,
                            IId?                      SenderId          = null,
                            Object?                   Sender            = null,
                            RoamingNetwork?          RoamingNetwork    = null,
                            I18NString?               Description       = null,
                            IEnumerable<Warning>?     Warnings          = null,
                            TimeSpan?                 Runtime           = null)

                => new (ChargingStationOperator,
                        CommandResult.CanNotBeRemoved,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        RoamingNetwork,
                        Description,
                        Warnings,
                        Runtime);

        #endregion


        #region (static) ArgumentError  (ChargingStationOperator,   Description, ...)

        public static DeleteChargingStationOperatorResult

            ArgumentError(ChargingStationOperator  ChargingStationOperator,
                          I18NString                Description,
                          EventTracking_Id?         EventTrackingId   = null,
                          IId?                      SenderId          = null,
                          Object?                   Sender            = null,
                          RoamingNetwork?          RoamingNetwork    = null,
                          IEnumerable<Warning>?     Warnings          = null,
                          TimeSpan?                 Runtime           = null)

                => new (ChargingStationOperator,
                        CommandResult.ArgumentError,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        RoamingNetwork,
                        Description,
                        Warnings,
                        Runtime);

        #endregion

        #region (static) ArgumentError  (ChargingStationOperatorId, Description, ...)

        public static DeleteChargingStationOperatorResult

            ArgumentError(ChargingStationOperator_Id  ChargingStationOperatorId,
                          I18NString                  Description,
                          EventTracking_Id?           EventTrackingId   = null,
                          IId?                        SenderId          = null,
                          Object?                     Sender            = null,
                          RoamingNetwork?            RoamingNetwork    = null,
                          IEnumerable<Warning>?       Warnings          = null,
                          TimeSpan?                   Runtime           = null)

                => new (ChargingStationOperatorId,
                        CommandResult.ArgumentError,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        RoamingNetwork,
                        Description,
                        Warnings,
                        Runtime);

        #endregion

        #region (static) Error          (ChargingStationOperator,   Description, ...)

        public static DeleteChargingStationOperatorResult

            Error(ChargingStationOperator  ChargingStationOperator,
                  I18NString                Description,
                  EventTracking_Id?         EventTrackingId   = null,
                  IId?                      SenderId          = null,
                  Object?                   Sender            = null,
                  RoamingNetwork?          RoamingNetwork    = null,
                  IEnumerable<Warning>?     Warnings          = null,
                  TimeSpan?                 Runtime           = null)

                => new (ChargingStationOperator,
                        CommandResult.Error,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        RoamingNetwork,
                        Description,
                        Warnings,
                        Runtime);

        #endregion

        #region (static) Error          (ChargingStationOperator,   Exception,   ...)

        public static DeleteChargingStationOperatorResult

            Error(ChargingStationOperator  ChargingStationOperator,
                  Exception                 Exception,
                  EventTracking_Id?         EventTrackingId   = null,
                  IId?                      SenderId          = null,
                  Object?                   Sender            = null,
                  RoamingNetwork?          RoamingNetwork    = null,
                  IEnumerable<Warning>?     Warnings          = null,
                  TimeSpan?                 Runtime           = null)

                => new (ChargingStationOperator,
                        CommandResult.Error,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        RoamingNetwork,
                        Exception.Message.ToI18NString(),
                        Warnings,
                        Runtime);

        #endregion

        #region (static) Timeout        (ChargingStationOperator,   Timeout,     ...)

        public static DeleteChargingStationOperatorResult

            Timeout(ChargingStationOperator  ChargingStationOperator,
                    TimeSpan                  Timeout,
                    EventTracking_Id?         EventTrackingId   = null,
                    IId?                      SenderId          = null,
                    Object?                   Sender            = null,
                    RoamingNetwork?          RoamingNetwork    = null,
                    IEnumerable<Warning>?     Warnings          = null,
                    TimeSpan?                 Runtime           = null)

                => new (ChargingStationOperator,
                        CommandResult.Timeout,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        RoamingNetwork,
                        $"Timeout after {Timeout.TotalSeconds} seconds!".ToI18NString(),
                        Warnings,
                        Runtime);

        #endregion

        #region (static) LockTimeout    (ChargingStationOperator,   Timeout,     ...)

        public static DeleteChargingStationOperatorResult

            LockTimeout(ChargingStationOperator  ChargingStationOperator,
                        TimeSpan                  Timeout,
                        EventTracking_Id?         EventTrackingId   = null,
                        IId?                      SenderId          = null,
                        Object?                   Sender            = null,
                        RoamingNetwork?          RoamingNetwork    = null,
                        IEnumerable<Warning>?     Warnings          = null,
                        TimeSpan?                 Runtime           = null)

                => new (ChargingStationOperator,
                        CommandResult.LockTimeout,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        RoamingNetwork,
                        $"Lock timeout after {Timeout.TotalSeconds} seconds!".ToI18NString(),
                        Warnings,
                        Runtime);

        #endregion


    }

}
