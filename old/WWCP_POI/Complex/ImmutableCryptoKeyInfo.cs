/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 * http://www.gnu.org/licenses/agpl.html
 */

using System.Text.Json;
using System.Collections.Immutable;
using Newtonsoft.Json.Linq;
using org.GraphDefined.Vanaheimr.Hermod;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// A detached immutable representation of Hermod's mutable key information.
/// </summary>
public sealed partial class ImmutableCryptoKeyInfo
{
    private readonly JsonElement document;
    private readonly JsonElement publicDocument;

    public ImmutableCryptoKeyInfo(CryptoKeyInfo value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var json = value.ToJSON();
        if (value.NotBefore is { } notBefore) json["notBefore"] = notBefore.ToUniversalTime().ToString("O");
        if (value.NotAfter is { } notAfter) json["notAfter"] = notAfter.ToUniversalTime().ToString("O");
        json["priority"] = value.Priority;
        document = JsonSerializer.Deserialize<JsonElement>(json.ToString(Newtonsoft.Json.Formatting.None));
        json.Remove("privateKey");
        publicDocument = JsonSerializer.Deserialize<JsonElement>(json.ToString(Newtonsoft.Json.Formatting.None));
        PublicKey = value.PublicKey;
        NotBefore = value.NotBefore;
        NotAfter = value.NotAfter;
        Priority = value.Priority;
        KeyType = value.KeyType.ToString();
        KeyEncoding = value.KeyEncoding.ToString();
        KeyUsages = value.KeyUsages.Select(usage => usage.ToString()).ToImmutableArray();
    }

    public String PublicKey { get; }
    public DateTimeOffset? NotBefore { get; }
    public DateTimeOffset? NotAfter { get; }
    public UInt32 Priority { get; }
    public String KeyType { get; }
    public String KeyEncoding { get; }
    public ImmutableArray<String> KeyUsages { get; }
    public JsonElement Document => publicDocument;
    public JObject ToJSON(Boolean IncludePrivateKey = false)
        => POIRepresentation.AddETags(this, JObject.Parse((IncludePrivateKey ? document : publicDocument).GetRawText()));
}
