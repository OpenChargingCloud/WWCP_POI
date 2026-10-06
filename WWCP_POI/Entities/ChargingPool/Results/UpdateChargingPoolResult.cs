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
    /// The result of an update charging pool request.
    /// </summary>
    public class UpdateChargingPoolResult : AEnitityResult<ChargingPool, ChargingPool_Id>
    {

        #region Properties

        public ChargingPool?             ChargingPool
            => Entity;

        public ChargingStationOperator?  ChargingStationOperator    { get; internal set; }

        #endregion

        #region Constructor(s)

        public UpdateChargingPoolResult(ChargingPool              ChargingPool,
                                        CommandResult        Result,
                                        EventTracking_Id?          EventTrackingId           = null,
                                        IId?                       SenderId                  = null,
                                        Object?                    Sender                    = null,
                                        ChargingStationOperator?  ChargingStationOperator   = null,
                                        I18NString?                Description               = null,
                                        IEnumerable<Warning>?      Warnings                  = null,
                                        TimeSpan?                  Runtime                   = null)

            : base(ChargingPool,
                   Result,
                   EventTrackingId,
                   SenderId,
                   Sender,
                   Description,
                   Warnings,
                   Runtime)

        {

            this.ChargingStationOperator = ChargingStationOperator;

        }

        public UpdateChargingPoolResult(ChargingPool_Id            ChargingPoolId,
                                        CommandResult        Result,
                                        EventTracking_Id?          EventTrackingId           = null,
                                        IId?                       SenderId                  = null,
                                        Object?                    Sender                    = null,
                                        ChargingStationOperator?  ChargingStationOperator   = null,
                                        I18NString?                Description               = null,
                                        IEnumerable<Warning>?      Warnings                  = null,
                                        TimeSpan?                  Runtime                   = null)

            : base(ChargingPoolId,
                   Result,
                   EventTrackingId,
                   SenderId,
                   Sender,
                   Description,
                   Warnings,
                   Runtime)

        {

            this.ChargingStationOperator = ChargingStationOperator;

        }

        #endregion


        #region (static) AdminDown    (ChargingPool, ...)

        public static UpdateChargingPoolResult

            AdminDown(ChargingPool              ChargingPool,
                      EventTracking_Id?          EventTrackingId           = null,
                      IId?                       SenderId                  = null,
                      Object?                    Sender                    = null,
                      ChargingStationOperator?  ChargingStationOperator   = null,
                      I18NString?                Description               = null,
                      IEnumerable<Warning>?      Warnings                  = null,
                      TimeSpan?                  Runtime                   = null)

                => new (ChargingPool,
                        CommandResult.AdminDown,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        ChargingStationOperator,
                        Description,
                        Warnings,
                        Runtime);

        #endregion

        #region (static) NoOperation  (ChargingPool, ...)

        public static UpdateChargingPoolResult

            NoOperation(ChargingPool              ChargingPool,
                        EventTracking_Id?          EventTrackingId           = null,
                        IId?                       SenderId                  = null,
                        Object?                    Sender                    = null,
                        ChargingStationOperator?  ChargingStationOperator   = null,
                        I18NString?                Description               = null,
                        IEnumerable<Warning>?      Warnings                  = null,
                        TimeSpan?                  Runtime                   = null)

                => new (ChargingPool,
                        CommandResult.NoOperation,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        ChargingStationOperator,
                        Description,
                        Warnings,
                        Runtime);

        #endregion


        #region (static) Enqueued     (ChargingPool, ...)

        public static UpdateChargingPoolResult

            Enqueued(ChargingPool              ChargingPool,
                     EventTracking_Id?          EventTrackingId           = null,
                     IId?                       SenderId                  = null,
                     Object?                    Sender                    = null,
                     ChargingStationOperator?  ChargingStationOperator   = null,
                     I18NString?                Description               = null,
                     IEnumerable<Warning>?      Warnings                  = null,
                     TimeSpan?                  Runtime                   = null)

                => new (ChargingPool,
                        CommandResult.Enqueued,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        ChargingStationOperator,
                        Description,
                        Warnings,
                        Runtime);

        #endregion

        #region (static) Success      (ChargingPool, ...)

        public static UpdateChargingPoolResult

            Success(ChargingPool              ChargingPool,
                    EventTracking_Id?          EventTrackingId           = null,
                    IId?                       SenderId                  = null,
                    Object?                    Sender                    = null,
                    ChargingStationOperator?  ChargingStationOperator   = null,
                    I18NString?                Description               = null,
                    IEnumerable<Warning>?      Warnings                  = null,
                    TimeSpan?                  Runtime                   = null)

                => new (ChargingPool,
                        CommandResult.Success,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        ChargingStationOperator,
                        Description,
                        Warnings,
                        Runtime);

        #endregion


        #region (static) ArgumentError(ChargingPool,   Description, ...)

        public static UpdateChargingPoolResult

            ArgumentError(ChargingPool              ChargingPool,
                          I18NString                 Description,
                          EventTracking_Id?          EventTrackingId           = null,
                          IId?                       SenderId                  = null,
                          Object?                    Sender                    = null,
                          ChargingStationOperator?  ChargingStationOperator   = null,
                          IEnumerable<Warning>?      Warnings                  = null,
                          TimeSpan?                  Runtime                   = null)

                => new (ChargingPool,
                        CommandResult.ArgumentError,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        ChargingStationOperator,
                        Description,
                        Warnings,
                        Runtime);

        #endregion

        #region (static) ArgumentError(ChargingPoolId, Description, ...)

        public static UpdateChargingPoolResult

            ArgumentError(ChargingPool_Id            ChargingPoolId,
                          I18NString                 Description,
                          EventTracking_Id?          EventTrackingId           = null,
                          IId?                       SenderId                  = null,
                          Object?                    Sender                    = null,
                          ChargingStationOperator?  ChargingStationOperator   = null,
                          IEnumerable<Warning>?      Warnings                  = null,
                          TimeSpan?                  Runtime                   = null)

                => new (ChargingPoolId,
                        CommandResult.ArgumentError,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        ChargingStationOperator,
                        Description,
                        Warnings,
                        Runtime);

        #endregion

        #region (static) Error        (ChargingPool,   Description, ...)

        public static UpdateChargingPoolResult

            Error(ChargingPool              ChargingPool,
                  I18NString                 Description,
                  EventTracking_Id?          EventTrackingId           = null,
                  IId?                       SenderId                  = null,
                  Object?                    Sender                    = null,
                  ChargingStationOperator?  ChargingStationOperator   = null,
                  IEnumerable<Warning>?      Warnings                  = null,
                  TimeSpan?                  Runtime                   = null)

                => new (ChargingPool,
                        CommandResult.Error,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        ChargingStationOperator,
                        Description,
                        Warnings,
                        Runtime);

        #endregion

        #region (static) Error        (ChargingPool,   Exception,   ...)

        public static UpdateChargingPoolResult

            Error(ChargingPool              ChargingPool,
                  Exception                  Exception,
                  EventTracking_Id?          EventTrackingId   = null,
                  IId?                       SenderId                  = null,
                  Object?                    Sender                    = null,
                  ChargingStationOperator?  ChargingStationOperator   = null,
                  IEnumerable<Warning>?      Warnings                  = null,
                  TimeSpan?                  Runtime                   = null)

                => new (ChargingPool,
                        CommandResult.Error,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        ChargingStationOperator,
                        Exception.Message.ToI18NString(),
                        Warnings,
                        Runtime);

        #endregion

        #region (static) Timeout      (ChargingPool,   Timeout,     ...)

        public static UpdateChargingPoolResult

            Timeout(ChargingPool              ChargingPool,
                    TimeSpan                   Timeout,
                    EventTracking_Id?          EventTrackingId           = null,
                    IId?                       SenderId                  = null,
                    Object?                    Sender                    = null,
                    ChargingStationOperator?  ChargingStationOperator   = null,
                    IEnumerable<Warning>?      Warnings                  = null,
                    TimeSpan?                  Runtime                   = null)

                => new (ChargingPool,
                        CommandResult.Timeout,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        ChargingStationOperator,
                        $"Timeout after {Timeout.TotalSeconds} seconds!".ToI18NString(),
                        Warnings,
                        Runtime);

        #endregion

        #region (static) LockTimeout  (ChargingPool,   Timeout,     ...)

        public static UpdateChargingPoolResult

            LockTimeout(ChargingPool              ChargingPool,
                        TimeSpan                   Timeout,
                        EventTracking_Id?          EventTrackingId           = null,
                        IId?                       SenderId                  = null,
                        Object?                    Sender                    = null,
                        ChargingStationOperator?  ChargingStationOperator   = null,
                        IEnumerable<Warning>?      Warnings                  = null,
                        TimeSpan?                  Runtime                   = null)

                => new (ChargingPool,
                        CommandResult.LockTimeout,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        ChargingStationOperator,
                        $"Lock timeout after {Timeout.TotalSeconds} seconds!".ToI18NString(),
                        Warnings,
                        Runtime);

        #endregion


    }

}
