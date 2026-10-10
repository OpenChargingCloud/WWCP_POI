/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// A stable, case-sensitive identifier of a transparency software release.
/// </summary>
public readonly record struct TransparencySoftware_Id : IId, IComparable<TransparencySoftware_Id>
{
    private readonly String? value;

    private TransparencySoftware_Id(String value) => this.value = value;

    /// <summary>
    /// Whether this identifier is empty.
    /// </summary>
    public Boolean IsNullOrEmpty => String.IsNullOrEmpty(value);

    /// <summary>
    /// Whether this identifier is present.
    /// </summary>
    public Boolean IsNotNullOrEmpty => !IsNullOrEmpty;

    /// <summary>
    /// The number of characters in this identifier.
    /// </summary>
    public UInt64 Length => (UInt64) (value?.Length ?? 0);

    /// <summary>
    /// Parse a nonempty opaque identifier without changing its spelling.
    /// </summary>
    public static TransparencySoftware_Id Parse(String text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        if (text != text.Trim())
            throw new ArgumentException("An identifier must not have surrounding whitespace.", nameof(text));
        return new(text);
    }

    /// <summary>
    /// Try to parse a software identifier.
    /// </summary>
    public static Boolean TryParse(String? text, out TransparencySoftware_Id id)
    {
        id = default;
        if (String.IsNullOrWhiteSpace(text) || text != text.Trim()) return false;
        id = new(text);
        return true;
    }

    /// <summary>
    /// Create a new identifier for a software release.
    /// </summary>
    public static TransparencySoftware_Id New => Parse(UUIDv7.Generate().ToString());

    /// <summary>
    /// Compare identifiers using ordinal spelling.
    /// </summary>
    public Int32 CompareTo(TransparencySoftware_Id other) => StringComparer.Ordinal.Compare(value, other.value);

    /// <summary>
    /// Compare with another software identifier.
    /// </summary>
    public Int32 CompareTo(Object? other) => other is TransparencySoftware_Id id ? CompareTo(id) :
        throw new ArgumentException("Expected a transparency software identifier.", nameof(other));

    /// <summary>
    /// Return the wire spelling.
    /// </summary>
    public override String ToString() => value ?? "";
}
