using System.Security.Cryptography;
using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Crypto;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.WWCP.POI;

public static class Helpers
{

    // A private key in a PEM file, PKCS #8, as the hub keeps its own.
    public static void SaveKey(AsymmetricKeyParameter Key, String File)
        => System.IO.File.WriteAllText(File, new String(PemEncoding.Write("PRIVATE KEY", PrivateKeyInfoFactory.CreatePrivateKeyInfo(Key).GetEncoded())) + "\n");

    // A JSON file read as POI reads JSON: timestamps stay the text they are signed as, numbers the decimals they are.
    public static JObject ReadJSON(String File)
    {
        using var reader = new JsonTextReader(new StringReader(System.IO.File.ReadAllText(File))) {
                               DateParseHandling   = DateParseHandling.None,
                               FloatParseHandling  = FloatParseHandling.Decimal
                           };
        return JObject.Load(reader);
    }

    // A JSON file of an array, read as ReadJSON reads one of an object.
    public static JArray ReadJSONArray(String File)
    {
        using var reader = new JsonTextReader(new StringReader(System.IO.File.ReadAllText(File))) {
                               DateParseHandling   = DateParseHandling.None,
                               FloatParseHandling  = FloatParseHandling.Decimal
                           };
        return JArray.Load(reader);
    }

    // Stops with the given error.
    [DoesNotReturn]
    public static void Fail(String Error)
    {
        Console.WriteLine($"refused: {Error}");
        Environment.Exit(1);
    }

}
