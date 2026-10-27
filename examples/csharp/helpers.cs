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

    // A JSON file read as POI reads JSON, with POIJSON.Read: timestamps stay the text they are signed as, numbers the
    // decimals they are; a property named twice, a number a decimal cannot hold as written, and what RFC 8259 does not
    // call JSON, e.g. a second value after the first, are refused.
    public static JObject ReadJSON(String File)
        => POIJSON.Read(System.IO.File.ReadAllText(File)) as JObject
               ?? throw new JsonReaderException($"{File} holds no JSON object!");

    // A JSON file of an array, read as ReadJSON reads one of an object.
    public static JArray ReadJSONArray(String File)
        => POIJSON.Read(System.IO.File.ReadAllText(File)) as JArray
               ?? throw new JsonReaderException($"{File} holds no JSON array!");

    // Stops with the given error.
    [DoesNotReturn]
    public static void Fail(String Error)
    {
        Console.WriteLine($"refused: {Error}");
        Environment.Exit(1);
    }

    // A private key from a PEM file SaveKey wrote, as its organization reads it to sign with it.
    public static AsymmetricKeyParameter ReadKey(String File)
    {
        var text = System.IO.File.ReadAllText(File);
        return Org.BouncyCastle.Security.PrivateKeyFactory.CreateKey(Convert.FromBase64String(text[PemEncoding.Find(text).Base64Data]));
    }

}
