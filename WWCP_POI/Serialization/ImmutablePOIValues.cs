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

using System.Collections.Immutable;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// Detach mutable value objects at the infrastructure API boundary.
/// </summary>
internal static class ImmutablePOIValues
{
    internal static T Copy<T>(T value)
        => (T) (value switch
        {
            I18NString text => text.Clone(),
            Address address => new Address(address.Street, address.PostalCode, address.City.Clone(), address.Country.Clone(),
                                           address.HouseNumber, address.FloorLevel, address.Region, address.PostalCodeSub,
                                           address.TimeZone, address.OfficialLanguages.ToImmutableArray(), address.Comment.Clone(),
                                           address.CustomData.Clone(), null, address.LastChangeDate),
            OpeningTimes times => new ImmutableOpeningTimes(times).ToMutable(),
            DataLicense license => license.Clone(),
            Brand brand => brand,
            EnergyMix mix => mix,
            Timestamped<EnergyMix> mix => new Timestamped<EnergyMix>(mix.Timestamp, Copy(mix.Value)),
            EnergyMixPrognosis forecast => new EnergyMixPrognosis(
                forecast.EnergySources.ToImmutableArray(), forecast.EnvironmentalImpacts.ToImmutableArray(),
                forecast.SupplierName.Clone(), forecast.ProductName.Clone(), forecast.Timestamp.UtcDateTime,
                forecast.AdditionalRemarks?.Clone()),
            EnergyMeter meter => meter.Clone(),
            PublicKey key => new PublicKey(key.Value.ToArray(), key.Algorithm, key.Serialization, key.Encoding,
                                           key.CustomData is { } custom ? CustomData.Parse(custom.ToJSON()) : null),
            ChargingCable cable => cable,
            ChargingTariffElement element => element.Clone(),
            AdditionalGeoLocation location => location.Clone(),
            RootCAInfo root => root.Clone(),
            EVRoamingPartnerInfo partner => partner.Clone(),
            Image image => image.Clone(),
            AuthenticationModes modes => AuthenticationModes.Parse(modes.ToJSON()),
            _ => (Object?) value
        })!;

    internal static ImmutableArray<T> CopyItems<T>(IEnumerable<T>? values)
        => values is null ? [] : values.Select(Copy).ToImmutableArray();

    internal static ImmutableArray<EnergyMeter> CopyEnergyMeters(IEnumerable<EnergyMeter>? values)
    {
        var meters = values?.ToArray() ?? [];
        var ids = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
        foreach (var meter in meters)
        {
            if (meter is null || meter.Id.IsNullOrEmpty)
                throw new ArgumentException("energyMeters: every entry must be a meter with a non-empty ID.", nameof(values));
            if (!ids.Add(meter.Id.ToString()))
                throw new ArgumentException($"energyMeters: duplicate identifier '{meter.Id}'.", nameof(values));
        }
        return CopyItems(meters);
    }

}
