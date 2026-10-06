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
    /// The result of an add e-mobility provider request.
    /// </summary>
    public class AddEMobilityProviderResult : AEnitityResult<EMobilityProvider, EMobilityProvider_Id>
    {

        #region Properties

        public EMobilityProvider?  EMobilityProvider
            => Entity;

        public RoamingNetwork?     RoamingNetwork    { get; internal set; }

        #endregion

        #region Constructor(s)

        public AddEMobilityProviderResult(EMobilityProvider     EMobilityProvider,
                                          CommandResult          Result,
                                          EventTracking_Id?      EventTrackingId   = null,
                                          IId?                   SenderId          = null,
                                          Object?                Sender            = null,
                                          RoamingNetwork?       RoamingNetwork    = null,
                                          I18NString?            Description       = null,
                                          IEnumerable<Warning>?  Warnings          = null,
                                          TimeSpan?              Runtime           = null)

            : base(EMobilityProvider,
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


        #region (static) AdminDown    (ChargingStation, ...)

        public static AddEMobilityProviderResult

            AdminDown(EMobilityProvider     EMobilityProvider,
                      EventTracking_Id?      EventTrackingId   = null,
                      IId?                   SenderId          = null,
                      Object?                Sender            = null,
                      RoamingNetwork?       RoamingNetwork    = null,
                      I18NString?            Description       = null,
                      IEnumerable<Warning>?  Warnings          = null,
                      TimeSpan?              Runtime           = null)

                => new (EMobilityProvider,
                        CommandResult.AdminDown,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        RoamingNetwork,
                        Description,
                        Warnings,
                        Runtime);

        #endregion

        #region (static) NoOperation  (ChargingStation, ...)

        public static AddEMobilityProviderResult

            NoOperation(EMobilityProvider     EMobilityProvider,
                        EventTracking_Id?      EventTrackingId   = null,
                        IId?                   SenderId          = null,
                        Object?                Sender            = null,
                        RoamingNetwork?       RoamingNetwork    = null,
                        I18NString?            Description       = null,
                        IEnumerable<Warning>?  Warnings          = null,
                        TimeSpan?              Runtime           = null)

                => new (EMobilityProvider,
                        CommandResult.NoOperation,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        RoamingNetwork,
                        Description,
                        Warnings,
                        Runtime);

        #endregion


        #region (static) Enqueued     (ChargingStation, ...)

        public static AddEMobilityProviderResult

            Enqueued(EMobilityProvider     EMobilityProvider,
                     EventTracking_Id?      EventTrackingId   = null,
                     IId?                   SenderId          = null,
                     Object?                Sender            = null,
                     RoamingNetwork?       RoamingNetwork    = null,
                     I18NString?            Description       = null,
                     IEnumerable<Warning>?  Warnings          = null,
                     TimeSpan?              Runtime           = null)

                => new (EMobilityProvider,
                        CommandResult.Enqueued,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        RoamingNetwork,
                        Description,
                        Warnings,
                        Runtime);

        #endregion

        #region (static) Success      (ChargingStation, ...)

        public static AddEMobilityProviderResult

            Success(EMobilityProvider     EMobilityProvider,
                    EventTracking_Id?      EventTrackingId   = null,
                    IId?                   SenderId          = null,
                    Object?                Sender            = null,
                    RoamingNetwork?       RoamingNetwork    = null,
                    I18NString?            Description       = null,
                    IEnumerable<Warning>?  Warnings          = null,
                    TimeSpan?              Runtime           = null)

                => new (EMobilityProvider,
                        CommandResult.Success,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        RoamingNetwork,
                        Description,
                        Warnings,
                        Runtime);

        #endregion


        #region (static) ArgumentError(ChargingStation, Description, ...)

        public static AddEMobilityProviderResult

            ArgumentError(EMobilityProvider     EMobilityProvider,
                          I18NString             Description,
                          EventTracking_Id?      EventTrackingId   = null,
                          IId?                   SenderId          = null,
                          Object?                Sender            = null,
                          RoamingNetwork?       RoamingNetwork    = null,
                          IEnumerable<Warning>?  Warnings          = null,
                          TimeSpan?              Runtime           = null)

                => new (EMobilityProvider,
                        CommandResult.ArgumentError,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        RoamingNetwork,
                        Description,
                        Warnings,
                        Runtime);

        #endregion

        #region (static) Error        (ChargingStation, Description, ...)

        public static AddEMobilityProviderResult

            Error(EMobilityProvider     EMobilityProvider,
                  I18NString             Description,
                  EventTracking_Id?      EventTrackingId   = null,
                  IId?                   SenderId          = null,
                  Object?                Sender            = null,
                  RoamingNetwork?       RoamingNetwork    = null,
                  IEnumerable<Warning>?  Warnings          = null,
                  TimeSpan?              Runtime           = null)

                => new (EMobilityProvider,
                        CommandResult.Error,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        RoamingNetwork,
                        Description,
                        Warnings,
                        Runtime);

        #endregion

        #region (static) Error        (ChargingStation, Exception,   ...)

        public static AddEMobilityProviderResult

            Error(EMobilityProvider     EMobilityProvider,
                  Exception              Exception,
                  EventTracking_Id?      EventTrackingId   = null,
                  IId?                   SenderId          = null,
                  Object?                Sender            = null,
                  RoamingNetwork?       RoamingNetwork    = null,
                  IEnumerable<Warning>?  Warnings          = null,
                  TimeSpan?              Runtime           = null)

                => new (EMobilityProvider,
                        CommandResult.Error,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        RoamingNetwork,
                        Exception.Message.ToI18NString(),
                        Warnings,
                        Runtime);

        #endregion

        #region (static) Timeout      (ChargingStation, Timeout,     ...)

        public static AddEMobilityProviderResult

            Timeout(EMobilityProvider     EMobilityProvider,
                    TimeSpan               Timeout,
                    EventTracking_Id?      EventTrackingId   = null,
                    IId?                   SenderId          = null,
                    Object?                Sender            = null,
                    RoamingNetwork?       RoamingNetwork    = null,
                    IEnumerable<Warning>?  Warnings          = null,
                    TimeSpan?              Runtime           = null)

                => new (EMobilityProvider,
                        CommandResult.Timeout,
                        EventTrackingId,
                        SenderId,
                        Sender,
                        RoamingNetwork,
                        $"Timeout after {Timeout.TotalSeconds} seconds!".ToI18NString(),
                        Warnings,
                        Runtime);

        #endregion

        #region (static) LockTimeout  (ChargingStation, Timeout,     ...)

        public static AddEMobilityProviderResult

            LockTimeout(EMobilityProvider     EMobilityProvider,
                        TimeSpan               Timeout,
                        EventTracking_Id?      EventTrackingId   = null,
                        IId?                   SenderId          = null,
                        Object?                Sender            = null,
                        RoamingNetwork?       RoamingNetwork    = null,
                        IEnumerable<Warning>?  Warnings          = null,
                        TimeSpan?              Runtime           = null)

                => new (EMobilityProvider,
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
