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

namespace cloud.charging.open.protocols.WWCP.POI;

public sealed partial class ChargingStation
{
    /// <summary>
    /// Copy POI values and runtime schedules into an independent child of the supplied pool.
    /// </summary>
    internal ChargingStation CloneToParent(ChargingPool parent)
    {
        var clone = new ChargingStation(Id, parent, Name, Description,
                                        Address: address, GeoLocation: geoLocation, OpeningTimes: openingTimes,
                                        ChargingWhenClosed: chargingWhenClosed, Accessibility: accessibility,
                                        LocationLanguage: locationLanguage, PhysicalReference: physicalReference,
                                        HotlinePhoneNumber: hotlinePhoneNumber, AuthenticationModes: AuthenticationModes,
                                        PaymentOptions: PaymentOptions, Features: Features, VehicleTypes: VehicleTypes, Images: Images,
                                        ServiceIdentification: ServiceIdentification, ModelCode: ModelCode,
                                        Published: Published, Disabled: Disabled, Brands: Brands, MobilityRootCAs: MobilityRootCAs,
                                        EVRoamingPartners: EVRoamingPartners, CertificationInfo: CertificationInfo, CalibrationInfo: CalibrationInfo,
                                        InitialAdminStatus: AdminStatus, InitialStatus: Status,
                                        MaxAdminStatusScheduleSize: adminStatusSchedule.MaxStatusHistorySize,
                                        MaxStatusScheduleSize: statusSchedule.MaxStatusHistorySize,
                                        DataSource: DataSource, Created: Created, LastChange: LastChangeDate,
                                        CustomData: CustomData, InternalData: InternalData, DataLicenses: DataLicenses,
                                        EnergyMeters: EnergyMeters, MaxCurrent: MaxCurrent, MaxPower: MaxPower, MaxCapacity: MaxCapacity);
        clone.openStreetMapNodeId = openStreetMapNodeId;
        clone.entranceAddress = ImmutablePOIValues.Copy(entranceAddress);
        clone.entranceLocation = entranceLocation;
        clone.exitAddress = ImmutablePOIValues.Copy(exitAddress);
        clone.exitLocation = exitLocation;
        clone.immutableArrivalInstructions = immutableArrivalInstructions;
        clone.immutableUIFeatures = immutableUIFeatures;
        clone.immutablePhotoURLs = immutablePhotoURLs;
        clone.gridConnection = gridConnection;
        clone.energyMix = ImmutablePOIValues.Copy(energyMix);
        clone.maxReservationDuration = maxReservationDuration;
        clone.isFreeOfCharge = isFreeOfCharge;
        clone._IsHubjectCompatible = _IsHubjectCompatible;
        clone._DynamicInfoAvailable = _DynamicInfoAvailable;
        clone.hubjectStationId = hubjectStationId;
        clone.StatusAggregationDelegate = StatusAggregationDelegate;
        clone.MaxCurrentRealTime = MaxCurrentRealTime;
        clone.MaxPowerRealTime = MaxPowerRealTime;
        clone.MaxCapacityRealTime = MaxCapacityRealTime;
        clone.EnergyMixRealTime = ImmutablePOIValues.Copy(EnergyMixRealTime);
        clone.energyMixPrognoses = ImmutablePOIValues.Copy(energyMixPrognoses);
        clone.MaxCurrentPrognoses.Replace(MaxCurrentPrognoses);
        clone.MaxPowerPrognoses.Replace(MaxPowerPrognoses);
        clone.MaxCapacityPrognoses.Replace(MaxCapacityPrognoses);
        clone.SetAdminStatus(AdminStatusSchedule());
        clone.SetStatus(StatusSchedule());
        foreach (var evse in EVSEs)
            clone.evses.TryAdd(evse.CloneToParent(clone), clone.Connect);
        return clone;
    }
}
