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

/// <summary>
/// Mutable operational measurements, outside immutable POI data.
/// </summary>
internal interface IRuntimeElectricalState
{
    Timestamped<Decimal>? MaxCurrentRealTime { get; set; }
    Timestamped<Decimal>? MaxPowerRealTime { get; set; }
    Timestamped<Decimal>? MaxCapacityRealTime { get; set; }
    ReactiveSet<Timestamped<Decimal>> MaxCurrentPrognoses { get; }
    ReactiveSet<Timestamped<Decimal>> MaxPowerPrognoses { get; }
    ReactiveSet<Timestamped<Decimal>> MaxCapacityPrognoses { get; }
    Timestamped<EnergyMix>? EnergyMixRealTime { get; set; }
    EnergyMixPrognosis? EnergyMixPrognoses { get; set; }
    EnergyMixPrognosis? OwnEnergyMixPrognoses { get; set; }
}

public sealed partial class ChargingPool : IRuntimeElectricalState
{
    EnergyMixPrognosis? IRuntimeElectricalState.OwnEnergyMixPrognoses
    {
        get => energyMixPrognoses;
        set => energyMixPrognoses = value;
    }
}
public sealed partial class ChargingStation : IRuntimeElectricalState
{
    EnergyMixPrognosis? IRuntimeElectricalState.OwnEnergyMixPrognoses
    {
        get => energyMixPrognoses;
        set => energyMixPrognoses = value;
    }
}
