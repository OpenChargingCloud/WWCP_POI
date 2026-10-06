using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

namespace cloud.charging.open.protocols.WWCP.POI;

public partial class Brand
{
    /// <summary>Parse a brand. ID-only licenses retain their identity; expanded licenses retain their metadata.</summary>
    public static Brand Parse(JObject JSON, CustomJObjectParserDelegate<Brand>? CustomBrandParser = null)
    {
        if (TryParse(JSON, out var brand, out var error, CustomBrandParser))
            return brand;
        throw new ArgumentException($"Invalid brand JSON: {error}", nameof(JSON));
    }

    /// <summary>Try to parse a brand without throwing for invalid input.</summary>
    public static bool TryParse(JObject JSON, [NotNullWhen(true)] out Brand? brand,
                                [NotNullWhen(false)] out string? error)
        => TryParse(JSON, out brand, out error, null);

    /// <summary>Try to parse a brand and optionally apply a custom parser.</summary>
    public static bool TryParse(JObject JSON, [NotNullWhen(true)] out Brand? brand,
                                [NotNullWhen(false)] out string? error,
                                CustomJObjectParserDelegate<Brand>? CustomBrandParser)
    {
        brand = null;
        error = null;
        try
        {
            if (JSON is null)
            {
                error = "The given JSON object must not be null.";
                return false;
            }
            if (!JSON.ParseMandatory("id", "brand identification", Brand_Id.TryParse, out Brand_Id id, out error) ||
                !JSON.ParseMandatoryJSON("name", "brand name", I18NString.TryParse, out I18NString? name, out error))
                return false;

            JSON.ParseOptionalJSON("description", "brand description", I18NString.TryParse, out I18NString? description, out error);
            if (error is not null)
                return false;
            JSON.ParseOptional("logo", "brand logo", URL.TryParse, out URL? logo, out error);
            if (error is not null)
                return false;
            JSON.ParseOptional("homepage", "brand homepage", URL.TryParse, out URL? homepage, out error);
            if (error is not null)
                return false;

            var licenses = new List<DataLicense>();
            if (JSON["dataLicenses"] is { } token && token.Type != JTokenType.Null)
            {
                if (token is not JArray array)
                    throw new ArgumentException("'dataLicenses' must be an array.");
                foreach (var entry in array)
                {
                    if (entry.Type == JTokenType.String && DataLicense_Id.TryParse(entry.Value<string>()!, out var licenseId))
                        licenses.Add(new DataLicense(licenseId));
                    else if (entry is JObject licenseJSON)
                    {
                        // Hermod writes '@id' but its license parser currently expects 'id'.
                        // Normalize a copy, preserving the caller's original document.
                        var document = (JObject) licenseJSON.DeepClone();
                        document["id"] ??= document["@id"]?.DeepClone();
                        document["URLs"] ??= new JArray();
                        if (!DataLicense.TryParse(document, out var license, out var licenseError) || license is null)
                            throw new ArgumentException($"Invalid data license: {licenseError}");
                        licenses.Add(license);
                    }
                    else
                        throw new ArgumentException("Invalid data license: expected an identifier or an object.");
                }
            }

            brand = new Brand(id, name!, description, logo, homepage, licenses);
            if (CustomBrandParser is not null)
                brand = CustomBrandParser(JSON, brand) ?? throw new ArgumentException("The custom parser returned null.");
            return true;
        }
        catch (Exception exception)
        {
            brand = null;
            error = $"Invalid brand JSON: {exception.Message}";
            return false;
        }
    }
}
