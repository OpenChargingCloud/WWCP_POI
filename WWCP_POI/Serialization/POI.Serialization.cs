using System.Collections.Immutable;

namespace cloud.charging.open.protocols.WWCP.POI;

public partial class ChargingConnector : IImmutablePOI
{
    /// <summary>
    /// Content identifiers of the immutable POI data; runtime states are excluded.
    /// </summary>
    public ImmutableArray<ETag> ETags => POIRepresentation.GetETags(this);
}

public partial class ChargingCable : IImmutablePOI
{
    /// <summary>
    /// Content identifiers of the immutable POI data; runtime states are excluded.
    /// </summary>
    public ImmutableArray<ETag> ETags => POIRepresentation.GetETags(this);
}

public partial class GridConnectionPoint : IImmutablePOI
{
    /// <summary>
    /// Content identifiers of the immutable POI data; runtime states are excluded.
    /// </summary>
    public ImmutableArray<ETag> ETags => POIRepresentation.GetETags(this);
}

public partial class ChargingStationManufacturer : IImmutablePOI
{
    /// <summary>
    /// Content identifiers of the immutable POI data; runtime states are excluded.
    /// </summary>
    public ImmutableArray<ETag> ETags => POIRepresentation.GetETags(this);
}

public partial class Brand : IImmutablePOI
{
    /// <summary>
    /// Content identifiers of the immutable POI data; runtime states are excluded.
    /// </summary>
    public ImmutableArray<ETag> ETags => POIRepresentation.GetETags(this);
}

public partial class EnergyMix : IImmutablePOI
{
    /// <summary>
    /// Content identifiers of the immutable POI data; runtime states are excluded.
    /// </summary>
    public ImmutableArray<ETag> ETags => POIRepresentation.GetETags(this);
}

public partial class ChargingProduct : IImmutablePOI
{
    /// <summary>
    /// Content identifiers of the immutable POI data; runtime states are excluded.
    /// </summary>
    public ImmutableArray<ETag> ETags => POIRepresentation.GetETags(this);
}

public partial class ParkingProduct : IImmutablePOI
{
    /// <summary>
    /// Content identifiers of the immutable POI data; runtime states are excluded.
    /// </summary>
    public ImmutableArray<ETag> ETags => POIRepresentation.GetETags(this);
}

public partial class EVRoamingPartnerInfo : IImmutablePOI
{
    /// <summary>
    /// Content identifiers of the immutable POI data; runtime states are excluded.
    /// </summary>
    public ImmutableArray<ETag> ETags => POIRepresentation.GetETags(this);
}

public partial class RootCAInfo : IImmutablePOI
{
    /// <summary>
    /// Content identifiers of the immutable POI data; runtime states are excluded.
    /// </summary>
    public ImmutableArray<ETag> ETags => POIRepresentation.GetETags(this);
}

public partial class ImmutableCryptoKeyInfo : IImmutablePOI
{
    /// <summary>
    /// Content identifiers of the immutable POI data; runtime states are excluded.
    /// </summary>
    public ImmutableArray<ETag> ETags => POIRepresentation.GetETags(this);
}

public partial class AuthenticationModes : IImmutablePOI
{
    /// <summary>
    /// Content identifiers of the immutable POI data; runtime states are excluded.
    /// </summary>
    public ImmutableArray<ETag> ETags => POIRepresentation.GetETags(this);
}

public partial class PublicKey : IImmutablePOI
{
    /// <summary>
    /// Content identifiers of the immutable POI data; runtime states are excluded.
    /// </summary>
    public ImmutableArray<ETag> ETags => POIRepresentation.GetETags(this);
}

public partial class RoamingNetworkDataSnapshot : IImmutablePOI
{
    /// <summary>
    /// Lazily cached content identifiers of the immutable POI data; runtime states are excluded.
    /// </summary>
    public ImmutableArray<ETag> ETags => contentETags.Value;
}

public readonly partial struct AdditionalGeoLocation : IImmutablePOI
{
    /// <summary>
    /// Content identifiers of the immutable POI data; runtime states are excluded.
    /// </summary>
    public ImmutableArray<ETag> ETags => POIRepresentation.GetETags(this);
}

public readonly partial struct ChargingTariffElement : IImmutablePOI
{
    /// <summary>
    /// Content identifiers of the immutable POI data; runtime states are excluded.
    /// </summary>
    public ImmutableArray<ETag> ETags => POIRepresentation.GetETags(this);
}

public readonly partial struct ChargingPriceComponent : IImmutablePOI
{
    /// <summary>
    /// Content identifiers of the immutable POI data; runtime states are excluded.
    /// </summary>
    public ImmutableArray<ETag> ETags => POIRepresentation.GetETags(this);
}

public partial class ChargingTariffRestriction : IImmutablePOI
{
    /// <summary>
    /// Content identifiers of the immutable tariff restriction.
    /// </summary>
    public ImmutableArray<ETag> ETags => POIRepresentation.GetETags(this);
}
