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

using cloud.charging.open.protocols.WWCP.POI;

namespace cloud.charging.open.protocols.WWCP.POI
{
    public interface IEnergyMeterModel
    {
        I18NString Description { get; }
        EnergyMeterModel_Id Id { get; }
        EnergyMeterManufacturer_Id ManufacturerId { get; }
        I18NString Name { get; }

        IEnergyMeterModel Clone();
        int CompareTo(object? Object);
        int CompareTo(IEnergyMeterModel EnergyMeterModel);
        bool Equals(object? Object);
        bool Equals(IEnergyMeterModel EnergyMeterModel);
        int GetHashCode();
        string ToString();
    }
}