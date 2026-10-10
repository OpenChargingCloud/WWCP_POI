using System.Diagnostics.CodeAnalysis;

namespace cloud.charging.open.protocols.WWCP.POI;

public partial class RoamingNetwork
{
    /// <summary>
    /// Parse a CBOR POI representation, including its metrological values and content identifiers.
    /// </summary>
    public static RoamingNetwork ParseCBOR(ReadOnlySpan<Byte> data)
        => POIRepresentation.ParseCBOR(data, json => RoamingNetwork.Parse(json));

    /// <summary>
    /// Try to parse a CBOR POI representation with the same parent context as JSON.
    /// </summary>
    public static Boolean TryParseCBOR(ReadOnlySpan<Byte> data,
                                       [NotNullWhen(true)] out RoamingNetwork? value,
                                       [NotNullWhen(false)] out String? error)
        => POIRepresentation.TryParseCBOR(data, json => RoamingNetwork.Parse(json), out value, out error);
}

public partial class ChargingConnector
{
    /// <summary>
    /// Parse a CBOR POI representation, including its metrological values and content identifiers.
    /// </summary>
    public static ChargingConnector ParseCBOR(ReadOnlySpan<Byte> data)
        => POIRepresentation.ParseCBOR(data, json => ChargingConnector.Parse(json));

    /// <summary>
    /// Try to parse a CBOR POI representation with the same parent context as JSON.
    /// </summary>
    public static Boolean TryParseCBOR(ReadOnlySpan<Byte> data,
                                       [NotNullWhen(true)] out ChargingConnector? value,
                                       [NotNullWhen(false)] out String? error)
        => POIRepresentation.TryParseCBOR(data, json => ChargingConnector.Parse(json), out value, out error);
}

public partial class ChargingCable
{
    /// <summary>
    /// Parse a CBOR POI representation, including its metrological values and content identifiers.
    /// </summary>
    public static ChargingCable ParseCBOR(ReadOnlySpan<Byte> data)
        => POIRepresentation.ParseCBOR(data, json => ChargingCable.Parse(json));

    /// <summary>
    /// Try to parse a CBOR POI representation with the same parent context as JSON.
    /// </summary>
    public static Boolean TryParseCBOR(ReadOnlySpan<Byte> data,
                                       [NotNullWhen(true)] out ChargingCable? value,
                                       [NotNullWhen(false)] out String? error)
        => POIRepresentation.TryParseCBOR(data, json => ChargingCable.Parse(json), out value, out error);
}

public partial class EnergyMeter
{
    /// <summary>
    /// Parse a CBOR POI representation, including its metrological values and content identifiers.
    /// </summary>
    public static EnergyMeter ParseCBOR(ReadOnlySpan<Byte> data, RoamingNetwork? network = null)
        => POIRepresentation.ParseCBOR(data, json => EnergyMeter.Parse(json, Network: network));

    /// <summary>
    /// Try to parse a CBOR POI representation with the same parent context as JSON.
    /// </summary>
    public static Boolean TryParseCBOR(ReadOnlySpan<Byte> data,
                                       [NotNullWhen(true)] out EnergyMeter? value,
                                       [NotNullWhen(false)] out String? error, RoamingNetwork? network = null)
        => POIRepresentation.TryParseCBOR(data, json => EnergyMeter.Parse(json, Network: network), out value, out error);
}

public partial class Brand
{
    /// <summary>
    /// Parse a CBOR POI representation, including its metrological values and content identifiers.
    /// </summary>
    public static Brand ParseCBOR(ReadOnlySpan<Byte> data)
        => POIRepresentation.ParseCBOR(data, json => Brand.Parse(json));

    /// <summary>
    /// Try to parse a CBOR POI representation with the same parent context as JSON.
    /// </summary>
    public static Boolean TryParseCBOR(ReadOnlySpan<Byte> data,
                                       [NotNullWhen(true)] out Brand? value,
                                       [NotNullWhen(false)] out String? error)
        => POIRepresentation.TryParseCBOR(data, json => Brand.Parse(json), out value, out error);
}

public partial class EnergyMix
{
    /// <summary>
    /// Parse a CBOR POI representation, including its metrological values and content identifiers.
    /// </summary>
    public static EnergyMix ParseCBOR(ReadOnlySpan<Byte> data)
        => POIRepresentation.ParseCBOR(data, json => EnergyMix.Parse(json));

    /// <summary>
    /// Try to parse a CBOR POI representation with the same parent context as JSON.
    /// </summary>
    public static Boolean TryParseCBOR(ReadOnlySpan<Byte> data,
                                       [NotNullWhen(true)] out EnergyMix? value,
                                       [NotNullWhen(false)] out String? error)
        => POIRepresentation.TryParseCBOR(data, json => EnergyMix.Parse(json), out value, out error);
}

public partial class ChargingProduct
{
    /// <summary>
    /// Parse a CBOR POI representation, including its metrological values and content identifiers.
    /// </summary>
    public static ChargingProduct ParseCBOR(ReadOnlySpan<Byte> data)
        => POIRepresentation.ParseCBOR(data, json => ChargingProduct.Parse(json));

    /// <summary>
    /// Try to parse a CBOR POI representation with the same parent context as JSON.
    /// </summary>
    public static Boolean TryParseCBOR(ReadOnlySpan<Byte> data,
                                       [NotNullWhen(true)] out ChargingProduct? value,
                                       [NotNullWhen(false)] out String? error)
        => POIRepresentation.TryParseCBOR(data, json => ChargingProduct.Parse(json), out value, out error);
}

public partial class ChargingTariffRestriction
{
    /// <summary>
    /// Parse a CBOR POI representation, including its metrological values and content identifiers.
    /// </summary>
    public static ChargingTariffRestriction ParseCBOR(ReadOnlySpan<Byte> data)
        => POIRepresentation.ParseCBOR(data, json => ChargingTariffRestriction.Parse(json));

    /// <summary>
    /// Try to parse a CBOR POI representation with the same parent context as JSON.
    /// </summary>
    public static Boolean TryParseCBOR(ReadOnlySpan<Byte> data,
                                       [NotNullWhen(true)] out ChargingTariffRestriction? value,
                                       [NotNullWhen(false)] out String? error)
        => POIRepresentation.TryParseCBOR(data, json => ChargingTariffRestriction.Parse(json), out value, out error);
}

public readonly partial struct ChargingTariffElement
{
    /// <summary>
    /// Parse a CBOR POI representation, including its metrological values and content identifiers.
    /// </summary>
    public static ChargingTariffElement ParseCBOR(ReadOnlySpan<Byte> data)
        => POIRepresentation.ParseCBOR(data, json => ChargingTariffElement.Parse(json));
}

public readonly partial struct ChargingPriceComponent
{
    /// <summary>
    /// Parse a CBOR POI representation, including its metrological values and content identifiers.
    /// </summary>
    public static ChargingPriceComponent ParseCBOR(ReadOnlySpan<Byte> data)
        => POIRepresentation.ParseCBOR(data, json => ChargingPriceComponent.Parse(json));
}

public readonly partial struct AdditionalGeoLocation
{
    /// <summary>
    /// Parse a CBOR POI representation, including its metrological values and content identifiers.
    /// </summary>
    public static AdditionalGeoLocation ParseCBOR(ReadOnlySpan<Byte> data)
        => POIRepresentation.ParseCBOR(data, json => AdditionalGeoLocation.Parse(json));
}

public partial class ChargingStationManufacturer
{
    /// <summary>
    /// Parse a CBOR POI representation, including its metrological values and content identifiers.
    /// </summary>
    public static ChargingStationManufacturer ParseCBOR(ReadOnlySpan<Byte> data)
        => POIRepresentation.ParseCBOR(data, json => ChargingStationManufacturer.Parse(json));

    /// <summary>
    /// Try to parse a CBOR POI representation with the same parent context as JSON.
    /// </summary>
    public static Boolean TryParseCBOR(ReadOnlySpan<Byte> data,
                                       [NotNullWhen(true)] out ChargingStationManufacturer? value,
                                       [NotNullWhen(false)] out String? error)
        => POIRepresentation.TryParseCBOR(data, json => ChargingStationManufacturer.Parse(json), out value, out error);
}

public partial class PublicKey
{
    /// <summary>
    /// Parse a CBOR POI representation, including its metrological values and content identifiers.
    /// </summary>
    public static PublicKey ParseCBOR(ReadOnlySpan<Byte> data)
        => POIRepresentation.ParseCBOR(data, json => PublicKey.Parse(json));

    /// <summary>
    /// Try to parse a CBOR POI representation with the same parent context as JSON.
    /// </summary>
    public static Boolean TryParseCBOR(ReadOnlySpan<Byte> data,
                                       [NotNullWhen(true)] out PublicKey? value,
                                       [NotNullWhen(false)] out String? error)
        => POIRepresentation.TryParseCBOR(data, json => PublicKey.Parse(json), out value, out error);
}

public partial class ParkingProduct
{
    /// <summary>
    /// Parse a CBOR POI representation, including its metrological values and content identifiers.
    /// </summary>
    public static ParkingProduct ParseCBOR(ReadOnlySpan<Byte> data)
        => POIRepresentation.ParseCBOR(data, json => ParkingProduct.Parse(json));

    /// <summary>
    /// Try to parse a CBOR POI representation with the same parent context as JSON.
    /// </summary>
    public static Boolean TryParseCBOR(ReadOnlySpan<Byte> data,
                                       [NotNullWhen(true)] out ParkingProduct? value,
                                       [NotNullWhen(false)] out String? error)
        => POIRepresentation.TryParseCBOR(data, json => ParkingProduct.Parse(json), out value, out error);
}

public partial class ImmutableCryptoKeyInfo
{
    /// <summary>
    /// Parse a CBOR POI representation, including its metrological values and content identifiers.
    /// </summary>
    public static ImmutableCryptoKeyInfo ParseCBOR(ReadOnlySpan<Byte> data)
        => POIRepresentation.ParseCBOR(data, json => ImmutableCryptoKeyInfo.Parse(json));

    /// <summary>
    /// Try to parse a CBOR POI representation with the same parent context as JSON.
    /// </summary>
    public static Boolean TryParseCBOR(ReadOnlySpan<Byte> data,
                                       [NotNullWhen(true)] out ImmutableCryptoKeyInfo? value,
                                       [NotNullWhen(false)] out String? error)
        => POIRepresentation.TryParseCBOR(data, json => ImmutableCryptoKeyInfo.Parse(json), out value, out error);
}

public partial class RootCAInfo
{
    /// <summary>
    /// Parse a CBOR POI representation, including its metrological values and content identifiers.
    /// </summary>
    public static RootCAInfo ParseCBOR(ReadOnlySpan<Byte> data)
        => POIRepresentation.ParseCBOR(data, json => RootCAInfo.Parse(json));

    /// <summary>
    /// Try to parse a CBOR POI representation with the same parent context as JSON.
    /// </summary>
    public static Boolean TryParseCBOR(ReadOnlySpan<Byte> data,
                                       [NotNullWhen(true)] out RootCAInfo? value,
                                       [NotNullWhen(false)] out String? error)
        => POIRepresentation.TryParseCBOR(data, json => RootCAInfo.Parse(json), out value, out error);
}

public partial class EVRoamingPartnerInfo
{
    /// <summary>
    /// Parse a CBOR POI representation, including its metrological values and content identifiers.
    /// </summary>
    public static EVRoamingPartnerInfo ParseCBOR(ReadOnlySpan<Byte> data)
        => POIRepresentation.ParseCBOR(data, json => EVRoamingPartnerInfo.Parse(json));

    /// <summary>
    /// Try to parse a CBOR POI representation with the same parent context as JSON.
    /// </summary>
    public static Boolean TryParseCBOR(ReadOnlySpan<Byte> data,
                                       [NotNullWhen(true)] out EVRoamingPartnerInfo? value,
                                       [NotNullWhen(false)] out String? error)
        => POIRepresentation.TryParseCBOR(data, json => EVRoamingPartnerInfo.Parse(json), out value, out error);
}

public partial class ChargingStationOperator
{
    /// <summary>
    /// Parse a CBOR POI representation, including its metrological values and content identifiers.
    /// </summary>
    public static ChargingStationOperator ParseCBOR(ReadOnlySpan<Byte> data, RoamingNetwork network)
        => POIRepresentation.ParseCBOR(data, json => ChargingStationOperator.Parse(json, network));

    /// <summary>
    /// Try to parse a CBOR POI representation with the same parent context as JSON.
    /// </summary>
    public static Boolean TryParseCBOR(ReadOnlySpan<Byte> data,
                                       [NotNullWhen(true)] out ChargingStationOperator? value,
                                       [NotNullWhen(false)] out String? error, RoamingNetwork network)
        => POIRepresentation.TryParseCBOR(data, json => ChargingStationOperator.Parse(json, network), out value, out error);
}

public partial class ChargingPool
{
    /// <summary>
    /// Parse a CBOR POI representation, including its metrological values and content identifiers.
    /// </summary>
    public static ChargingPool ParseCBOR(ReadOnlySpan<Byte> data, ChargingStationOperator op)
        => POIRepresentation.ParseCBOR(data, json => ChargingPool.Parse(json, op));

    /// <summary>
    /// Try to parse a CBOR POI representation with the same parent context as JSON.
    /// </summary>
    public static Boolean TryParseCBOR(ReadOnlySpan<Byte> data,
                                       [NotNullWhen(true)] out ChargingPool? value,
                                       [NotNullWhen(false)] out String? error, ChargingStationOperator op)
        => POIRepresentation.TryParseCBOR(data, json => ChargingPool.Parse(json, op), out value, out error);
}

public partial class ChargingStation
{
    /// <summary>
    /// Parse a CBOR POI representation, including its metrological values and content identifiers.
    /// </summary>
    public static ChargingStation ParseCBOR(ReadOnlySpan<Byte> data, ChargingPool? pool = null)
        => POIRepresentation.ParseCBOR(data, json => ChargingStation.Parse(json, pool));

    /// <summary>
    /// Try to parse a CBOR POI representation with the same parent context as JSON.
    /// </summary>
    public static Boolean TryParseCBOR(ReadOnlySpan<Byte> data,
                                       [NotNullWhen(true)] out ChargingStation? value,
                                       [NotNullWhen(false)] out String? error, ChargingPool? pool = null)
        => POIRepresentation.TryParseCBOR(data, json => ChargingStation.Parse(json, pool), out value, out error);
}

public partial class EVSE
{
    /// <summary>
    /// Parse a CBOR POI representation, including its metrological values and content identifiers.
    /// </summary>
    public static EVSE ParseCBOR(ReadOnlySpan<Byte> data, ChargingStation station)
        => POIRepresentation.ParseCBOR(data, json => EVSE.Parse(json, station));

    /// <summary>
    /// Try to parse a CBOR POI representation with the same parent context as JSON.
    /// </summary>
    public static Boolean TryParseCBOR(ReadOnlySpan<Byte> data,
                                       [NotNullWhen(true)] out EVSE? value,
                                       [NotNullWhen(false)] out String? error, ChargingStation station)
        => POIRepresentation.TryParseCBOR(data, json => EVSE.Parse(json, station), out value, out error);
}

public partial class EMobilityProvider
{
    /// <summary>
    /// Parse a CBOR POI representation, including its metrological values and content identifiers.
    /// </summary>
    public static EMobilityProvider ParseCBOR(ReadOnlySpan<Byte> data, RoamingNetwork network)
        => POIRepresentation.ParseCBOR(data, json => EMobilityProvider.Parse(json, network));

    /// <summary>
    /// Try to parse a CBOR POI representation with the same parent context as JSON.
    /// </summary>
    public static Boolean TryParseCBOR(ReadOnlySpan<Byte> data,
                                       [NotNullWhen(true)] out EMobilityProvider? value,
                                       [NotNullWhen(false)] out String? error, RoamingNetwork network)
        => POIRepresentation.TryParseCBOR(data, json => EMobilityProvider.Parse(json, network), out value, out error);
}

public partial class ChargingTariff
{
    /// <summary>
    /// Parse a CBOR POI representation, including its metrological values and content identifiers.
    /// </summary>
    public static ChargingTariff ParseCBOR(ReadOnlySpan<Byte> data, ChargingStationOperator op)
        => POIRepresentation.ParseCBOR(data, json => ChargingTariff.Parse(json, op));

    /// <summary>
    /// Try to parse a CBOR POI representation with the same parent context as JSON.
    /// </summary>
    public static Boolean TryParseCBOR(ReadOnlySpan<Byte> data,
                                       [NotNullWhen(true)] out ChargingTariff? value,
                                       [NotNullWhen(false)] out String? error, ChargingStationOperator op)
        => POIRepresentation.TryParseCBOR(data, json => ChargingTariff.Parse(json, op), out value, out error);
}

public partial class GridOperator
{
    /// <summary>
    /// Parse a CBOR POI representation, including its metrological values and content identifiers.
    /// </summary>
    public static GridOperator ParseCBOR(ReadOnlySpan<Byte> data, RoamingNetwork? network = null)
        => POIRepresentation.ParseCBOR(data, json => GridOperator.Parse(json, network));

    /// <summary>
    /// Try to parse a CBOR POI representation with the same parent context as JSON.
    /// </summary>
    public static Boolean TryParseCBOR(ReadOnlySpan<Byte> data,
                                       [NotNullWhen(true)] out GridOperator? value,
                                       [NotNullWhen(false)] out String? error, RoamingNetwork? network = null)
        => POIRepresentation.TryParseCBOR(data, json => GridOperator.Parse(json, network), out value, out error);
}

public partial class GridConnectionPoint
{
    /// <summary>
    /// Parse a CBOR POI representation, including its metrological values and content identifiers.
    /// </summary>
    public static GridConnectionPoint ParseCBOR(ReadOnlySpan<Byte> data, RoamingNetwork? network = null)
        => POIRepresentation.ParseCBOR(data, json => GridConnectionPoint.Parse(json, network));

    /// <summary>
    /// Try to parse a CBOR POI representation with the same parent context as JSON.
    /// </summary>
    public static Boolean TryParseCBOR(ReadOnlySpan<Byte> data,
                                       [NotNullWhen(true)] out GridConnectionPoint? value,
                                       [NotNullWhen(false)] out String? error, RoamingNetwork? network = null)
        => POIRepresentation.TryParseCBOR(data, json => GridConnectionPoint.Parse(json, network), out value, out error);
}
