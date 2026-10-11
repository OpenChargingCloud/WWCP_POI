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

using NUnit.Framework;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Math;
using org.GraphDefined.Vanaheimr.Illias;
using cloud.charging.open.protocols.WWCP;
using PublicKey    = cloud.charging.open.protocols.WWCP.POI.PublicKey;
using ECCPublicKey = cloud.charging.open.protocols.WWCP.POI.ECCPublicKey;

namespace WWCP_POI_Tests;

[TestFixture]
public sealed class PublicKeyParsingTests
{

    private static Byte[] Point(String Curve, Int32 PrivateKey, Boolean Compressed)
        => ECNamedCurveTable.GetByName(Curve).G.Multiply(BigInteger.ValueOf(PrivateKey)).Normalize().GetEncoded(Compressed);

    [Test]
    public void A_public_key_parses_in_the_encoding_it_names()
    {
        var bytes = Point("secp256r1", 42, false);

        foreach (var (text, encoding) in new[] { (Convert.ToHexString(bytes), CryptoEncoding.HEX),
                                                 (bytes.ToBase32(),           CryptoEncoding.BASE32),
                                                 (Convert.ToBase64String(bytes), CryptoEncoding.BASE64) })
        {
            Assert.That(PublicKey.TryParse(text, out var publicKey, out var errorResponse, Encoding: encoding), Is.True, errorResponse);
            Assert.That(publicKey!.Value,    Is.EqualTo(bytes));
            Assert.That(publicKey.Encoding,  Is.EqualTo(encoding));
        }
    }

    /// <summary>
    /// An uncompressed point lies on one curve only, and that curve is the key's algorithm.
    /// </summary>
    [Test]
    public void Auto_detection_keeps_the_detected_algorithm()
    {
        var text = Convert.ToBase64String(Point("secp256k1", 42, false));

        Assert.That(ECCPublicKey.TryParse(text, out var publicKey, out var errorResponse,
                                          Encoding: CryptoEncoding.BASE64, AutoDetectAlgorithmUsed: true), Is.True, errorResponse);
        Assert.That(publicKey!.Algorithm, Is.EqualTo(CryptoAlgorithm.Secp256k1));
    }

    /// <summary>
    /// A compressed point whose x lies on both 256-bit curves names no algorithm.
    /// </summary>
    [Test]
    public void Auto_detection_names_no_algorithm_for_an_ambiguous_point()
    {
        var privateKey = 1;
        while (!OnCurve("secp256r1", Point("secp256k1", privateKey, true)))
            privateKey++;

        var text = Convert.ToBase64String(Point("secp256k1", privateKey, true));

        Assert.That(ECCPublicKey.TryParse(text, out var publicKey, out var errorResponse,
                                          Encoding: CryptoEncoding.BASE64, AutoDetectAlgorithmUsed: true), Is.True, errorResponse);
        Assert.That(publicKey!.Algorithm, Is.Null);
    }

    private static Boolean OnCurve(String Curve, Byte[] Point)
    {
        try { ECNamedCurveTable.GetByName(Curve).Curve.DecodePoint(Point); return true; }
        catch { return false; }
    }

    [Test]
    public void Different_ECC_public_keys_have_different_hash_codes()
    {
        var first  = ECCPublicKey.Parse(Convert.ToBase64String(Point("secp256r1", 42, false)), Encoding: CryptoEncoding.BASE64);
        var again  = ECCPublicKey.Parse(Convert.ToBase64String(Point("secp256r1", 42, false)), Encoding: CryptoEncoding.BASE64);
        var other  = ECCPublicKey.Parse(Convert.ToBase64String(Point("secp256r1", 43, false)), Encoding: CryptoEncoding.BASE64);

        Assert.That(first, Is.EqualTo(again));
        Assert.That(first.GetHashCode(), Is.EqualTo(again.GetHashCode()));
        Assert.That(first.GetHashCode(), Is.Not.EqualTo(other.GetHashCode()));
    }

}
