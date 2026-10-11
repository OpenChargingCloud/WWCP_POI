/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Derived from WWCP Core's ChargingStationManufacturer.
 * Licensed under the Affero GPL license, Version 3.0.
 * http://www.gnu.org/licenses/agpl.html
 */

using System.Collections.Immutable;
using Newtonsoft.Json.Linq;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// Immutable manufacturer POI data and immutable cryptographic identities.
/// </summary>
public sealed partial class ChargingStationManufacturer : IHasId<ChargingStationManufacturer_Id>,
                                                   IEquatable<ChargingStationManufacturer>,
                                                   IComparable<ChargingStationManufacturer>
{
    public ChargingStationManufacturer(ChargingStationManufacturer_Id? Id = null,
                                       I18NString? Name = null,
                                       I18NString? Description = null,
                                       IEnumerable<CryptoKeyInfo>? CryptoKeys = null)
        : this(Id ?? ChargingStationManufacturer_Id.NewRandom(),
               new ImmutableI18NString(Name ?? I18NString.Empty),
               new ImmutableI18NString(Description ?? I18NString.Empty),
               (CryptoKeys ?? []).Select(value => new ImmutableCryptoKeyInfo(value)).ToImmutableArray())
    { }

    private ChargingStationManufacturer(ChargingStationManufacturer_Id id,
                                        ImmutableI18NString name,
                                        ImmutableI18NString description,
                                        ImmutableArray<ImmutableCryptoKeyInfo> keys)
    {
        if (id.IsNullOrEmpty)
            throw new ArgumentException("A manufacturer identifier is required.", nameof(id));
        if (keys.Select(key => key.PublicKey).Distinct(StringComparer.Ordinal).Count() != keys.Length)
            throw new ArgumentException("Duplicate manufacturer public key.", nameof(keys));
        Id = id;
        Name = name;
        Description = description;
        CryptoKeys = keys;
    }

    public ChargingStationManufacturer_Id Id { get; }
    public ImmutableI18NString Name { get; }
    public ImmutableI18NString Description { get; }
    public ImmutableArray<ImmutableCryptoKeyInfo> CryptoKeys { get; }

    public ChargingStationManufacturer WithCryptoKey(CryptoKeyInfo value)
        => new(Id, Name, Description, CryptoKeys.Add(new ImmutableCryptoKeyInfo(value)));

    public ChargingStationManufacturer Clone() => this;
    public JObject ToJSON() => POIRepresentation.AddETags(this, new(new JProperty("@id", Id.ToString()),
                                   new JProperty("name", Name.ToJSON()),
                                   new JProperty("description", Description.ToJSON()),
                                   new JProperty("cryptoKeys", new JArray(CryptoKeys.Select(key => key.ToJSON())))));
    public Boolean Equals(ChargingStationManufacturer? other) => other is not null && Id.Equals(other.Id);
    public override Boolean Equals(Object? other) => other is ChargingStationManufacturer value && Equals(value);
    public override Int32 GetHashCode() => Id.GetHashCode();
    public Int32 CompareTo(ChargingStationManufacturer? other) => other is null ? 1 : Id.CompareTo(other.Id);
    public Int32 CompareTo(Object? other) => other is ChargingStationManufacturer value
                                                ? CompareTo(value)
                                                : throw new ArgumentException("Expected a charging station manufacturer.", nameof(other));
    public override String ToString() => Id.ToString();
}
