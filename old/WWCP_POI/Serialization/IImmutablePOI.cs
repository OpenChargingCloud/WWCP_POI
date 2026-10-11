using System.Collections.Immutable;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// Immutable POI content. Runtime status and measurements are outside this contract.
/// </summary>
public interface IImmutablePOI
{
    /// <summary>
    /// SHA-256 identifiers of the canonical JSON and deterministic CBOR POI representations.
    /// </summary>
    ImmutableArray<ETag> ETags { get; }
}
