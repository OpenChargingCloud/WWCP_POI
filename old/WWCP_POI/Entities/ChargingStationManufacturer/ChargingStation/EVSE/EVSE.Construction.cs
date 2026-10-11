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

using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

public sealed partial class EVSE
{
    internal EnergyMixPrognosis? OwnEnergyMixPrognoses
    {
        get => energyMixPrognoses;
        set => energyMixPrognoses = value;
    }

    /// <summary>
    /// Build an independent entity under a new parent without reattaching the input.
    /// </summary>
    internal EVSE CloneToParent(ChargingStation parent)
    {
        var clone = new EVSE(Id, parent, Name, Description,
                             PhysicalReference: PhysicalReference, GeoLocation: GeoLocation,
                             PhotoURLs: PhotoURLs, Brands: Brands, MobilityRootCAs: MobilityRootCAs,
                             DataLicenses: DataLicenses, ChargingModes: ChargingModes, ChargingTariffs: ChargingTariffs,
                             CurrentType: CurrentType, MaxVoltage: MaxVoltage, MaxVoltageRealTime: MaxVoltageRealTime,
                             MaxVoltagePrognoses: MaxVoltagePrognoses, MaxCurrent: MaxCurrent,
                             MaxCurrentRealTime: MaxCurrentRealTime, MaxCurrentPrognoses: MaxCurrentPrognoses,
                             MaxPower: MaxPower, MaxPowerRealTime: MaxPowerRealTime, MaxPowerPrognoses: MaxPowerPrognoses,
                             MaxCapacity: MaxCapacity, MaxCapacityRealTime: MaxCapacityRealTime, MaxCapacityPrognoses: MaxCapacityPrognoses,
                             EnergyMix: energyMix, EnergyMixRealTime: ImmutablePOIValues.Copy(EnergyMixRealTime),
                             EnergyMixPrognoses: ImmutablePOIValues.Copy(energyMixPrognoses), EnergyMeter: EnergyMeter,
                             IsFreeOfCharge: IsFreeOfCharge, CalibrationInfo: CalibrationInfo, ChargingConnectors: ChargingConnectors,
                             InitialAdminStatus: AdminStatus, InitialStatus: Status,
                             MaxAdminStatusScheduleSize: adminStatusSchedule.MaxStatusHistorySize,
                             MaxStatusScheduleSize: statusSchedule.MaxStatusHistorySize, LastStatusUpdate: LastStatusUpdate,
                             DataSource: DataSource, Created: Created, LastChange: LastChangeDate,
                             CustomData: CustomData, InternalData: InternalData);
        clone.ChargingTariffIds = ChargingTariffIds;
        clone.maxReservationDuration = maxReservationDuration;
        clone.SetAdminStatus(AdminStatusSchedule());
        clone.SetStatus(StatusSchedule());
        return clone;
    }
}
