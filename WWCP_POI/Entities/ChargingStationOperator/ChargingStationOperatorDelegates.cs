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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{


    public delegate String ChargingStationOperatorNameSelectorDelegate(I18NString I18NText);


    /// <summary>
    /// A delegate for filtering charging station operator identifications.
    /// </summary>
    /// <param name="ChargingStationOperatorId">A charging station operator identification to include.</param>
    public delegate Boolean IncludeChargingStationOperatorIdDelegate(ChargingStationOperator_Id  ChargingStationOperatorId);

    /// <summary>
    /// A delegate for filtering charging station operators.
    /// </summary>
    /// <param name="ChargingStationOperator">A charging station operator to include.</param>
    public delegate Boolean IncludeChargingStationOperatorDelegate  (ChargingStationOperator     ChargingStationOperator);




    /// <summary>
    /// A delegate called whenever the static data of the charging station operator changed.
    /// </summary>
    /// <param name="Timestamp">The timestamp when this change was detected.</param>
    /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
    /// <param name="ChargingStationOperator">The updated charging station operator operator.</param>
    /// <param name="PropertyName">The name of the changed property.</param>
    /// <param name="OldValue">The old value of the changed property.</param>
    /// <param name="NewValue">The new value of the changed property.</param>
    public delegate Task OnChargingStationOperatorDataChangedDelegate(DateTimeOffset            Timestamp,
                                                                      EventTracking_Id          EventTrackingId,
                                                                      ChargingStationOperator  ChargingStationOperator,
                                                                      String                    PropertyName,
                                                                      Object?                   OldValue,
                                                                      Object?                   NewValue);

    /// <summary>
    /// A delegate called whenever the admin status of the charging station operator changed.
    /// </summary>
    /// <param name="Timestamp">The timestamp when this change was detected.</param>
    /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
    /// <param name="ChargingStationOperator">The updated charging station operator.</param>
    /// <param name="OldStatus">The old timestamped status of the charging station operator.</param>
    /// <param name="NewStatus">The new timestamped status of the charging station operator.</param>
    public delegate Task OnChargingStationOperatorAdminStatusChangedDelegate(DateTimeOffset                                        Timestamp,
                                                                             EventTracking_Id                                      EventTrackingId,
                                                                             ChargingStationOperator                              ChargingStationOperator,
                                                                             Timestamped<ChargingStationOperatorAdminStatusTypes>  OldStatus,
                                                                             Timestamped<ChargingStationOperatorAdminStatusTypes>  NewStatus);

    /// <summary>
    /// A delegate called whenever the dynamic status of the charging station operator changed.
    /// </summary>
    /// <param name="Timestamp">The timestamp when this change was detected.</param>
    /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
    /// <param name="ChargingStationOperator">The updated charging station operator.</param>
    /// <param name="OldStatus">The old timestamped status of the charging station operator.</param>
    /// <param name="NewStatus">The new timestamped status of the charging station operator.</param>
    public delegate Task OnChargingStationOperatorStatusChangedDelegate(DateTimeOffset                                   Timestamp,
                                                                        EventTracking_Id                                 EventTrackingId,
                                                                        ChargingStationOperator                         ChargingStationOperator,
                                                                        Timestamped<ChargingStationOperatorStatusTypes>  OldStatus,
                                                                        Timestamped<ChargingStationOperatorStatusTypes>  NewStatus);



}
