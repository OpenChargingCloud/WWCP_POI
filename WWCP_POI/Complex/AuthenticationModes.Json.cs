using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

public partial class AuthenticationModes
{
    /// <summary>Parse an authentication mode, including RFID, SMS and phone-call details.</summary>
    public static AuthenticationModes Parse(JObject JSON,
        CustomJObjectParserDelegate<AuthenticationModes>? CustomAuthenticationModeParser = null)
    {
        if (TryParse(JSON, out var mode, out var error, CustomAuthenticationModeParser))
            return mode;
        throw new ArgumentException($"Invalid authentication mode JSON: {error}", nameof(JSON));
    }

    /// <summary>Try to parse an authentication mode without throwing for invalid input.</summary>
    public static bool TryParse(JObject JSON,
        [NotNullWhen(true)] out AuthenticationModes? mode,
        [NotNullWhen(false)] out string? error)
        => TryParse(JSON, out mode, out error, null);

    /// <summary>Try to parse an authentication mode and optionally apply a custom parser.</summary>
    public static bool TryParse(JObject JSON,
        [NotNullWhen(true)] out AuthenticationModes? mode,
        [NotNullWhen(false)] out string? error,
        CustomJObjectParserDelegate<AuthenticationModes>? CustomAuthenticationModeParser)
    {
        mode = null;
        error = null;
        try
        {
            if (JSON is null || JSON["type"]?.Type != JTokenType.String ||
                string.IsNullOrWhiteSpace(JSON["type"]!.Value<string>()))
            {
                error = "Missing or invalid 'type': expected a non-empty string.";
                return false;
            }

            var type = JSON["type"]!.Value<string>()!;
            switch (type)
            {
                case "RFID":
                    var cards = new List<RFIDCardTypes>();
                    var brands = new List<Brand_Id>();
                    if (JSON["cardTypes"] is { } cardToken && cardToken.Type != JTokenType.Null)
                    {
                        if (cardToken is not JArray cardArray)
                            throw new ArgumentException("'cardTypes' must be an array.");
                        foreach (var card in cardArray)
                        {
                            if (card.Type != JTokenType.String ||
                                !Enum.GetNames<RFIDCardTypes>().Contains(card.Value<string>(), StringComparer.Ordinal) ||
                                !Enum.TryParse<RFIDCardTypes>(card.Value<string>(), out var parsed))
                                throw new ArgumentException("Invalid RFID card type.");
                            cards.Add(parsed);
                        }
                    }
                    if (JSON["brandIds"] is { } brandToken && brandToken.Type != JTokenType.Null)
                    {
                        if (brandToken is not JArray brandArray)
                            throw new ArgumentException("'brandIds' must be an array.");
                        foreach (var brand in brandArray)
                        {
                            if (brand.Type != JTokenType.String || !Brand_Id.TryParse(brand.Value<string>()!, out var parsed))
                                throw new ArgumentException("Invalid RFID brand identification.");
                            brands.Add(parsed);
                        }
                    }
                    mode = new RFID(cards, brands);
                    break;

                case "SMS":
                    var stationCode = JSON["StationCode"];
                    if (stationCode is not null && stationCode.Type is not (JTokenType.String or JTokenType.Null))
                        throw new ArgumentException("'StationCode' must be a string.");
                    mode = new SMS(ReadNumber(JSON), stationCode?.Value<string>());
                    break;

                case "PhoneCall":
                    mode = new PhoneCall(ReadNumber(JSON));
                    break;

                default:
                    mode = type switch
                    {
                        "Free Charging" => new FreeCharging(),
                        "PINPAD" => new PINPAD(),
                        "ISO/IEC 15118 PLC" => new ISO15118_PLC(),
                        "ISO/IEC 15118 Over-the-Air" => new ISO15118_Air(),
                        "REMOTE" => new REMOTE(),
                        "CreditCard" => new CreditCard(),
                        "DebitCard" => new DebitCard(),
                        "PrepaidCard" => new PrepaidCard(),
                        "NFC" => new NFC(),
                        "Bluetooth" => new Bluetooth(),
                        "WLAN" => new WLAN(),
                        "No authentication required" => new NoAuthenticationRequired(),
                        // The base type keeps vendor-defined authentication type names extensible.
                        _ => new AuthenticationModes(type)
                    };
                    break;
            }

            if (CustomAuthenticationModeParser is not null)
                mode = CustomAuthenticationModeParser(JSON, mode)
                       ?? throw new ArgumentException("The custom parser returned null.");
            return true;
        }
        catch (Exception exception)
        {
            mode = null;
            error = $"Invalid authentication mode JSON: {exception.Message}";
            return false;
        }
    }

    private static string ReadNumber(JObject json)
        => json["Number"]?.Type == JTokenType.String && !string.IsNullOrWhiteSpace(json["Number"]!.Value<string>())
               ? json["Number"]!.Value<string>()!
               : throw new ArgumentException("Missing or invalid 'Number': expected a non-empty string.");
}
