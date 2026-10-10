using Newtonsoft.Json.Linq;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// Persist constructor-supplied static POI values that are absent from compact presentation views.
/// </summary>
internal static class POIAdditionalProperties
{
    internal static Languages Language(JToken token)
        => token.Type == JTokenType.String && Enum.TryParse<Languages>(token.Value<String>(), out var language) && Enum.IsDefined(language)
               ? language : throw new ArgumentException("Expected a defined language identifier.");

    internal static void Write(JObject json, IImmutablePOI entity)
    {
        switch (entity)
        {
            case EMobilityProvider value:
                if (value.Priority is { } priority) json["priority"] = priority.Value;
                break;
            case ChargingPool value:
            {
                if (value.TimeZone is { } TimeZoneValue) json["timeZone"] = JToken.FromObject(TimeZoneValue.ToString());
                if (value.ChargingWhenClosed is { } ChargingWhenClosedValue) json["chargingWhenClosed"] = JToken.FromObject(ChargingWhenClosedValue);
                if (value.LocationLanguages.Any()) json["locationLanguages"] = new JArray(value.LocationLanguages.Select(item => item.ToString()));
                if (value.Facilities.Any()) json["facilities"] = new JArray(value.Facilities.Select(item => item.ToString()));
                if (value.Services.Any()) json["services"] = new JArray(value.Services.Select(item => item.ToString()));
                if (value.RelatedLocations.Any()) json["relatedLocations"] = new JArray(value.RelatedLocations.Select(item => POIJSON.Document(item)));
                if (value.MobilityRootCAs.Any()) json["mobilityRootCAs"] = new JArray(value.MobilityRootCAs.Select(item => POIJSON.Document(item)));
                if (value.EVRoamingPartners.Any()) json["evRoamingPartners"] = new JArray(value.EVRoamingPartners.Select(item => POIJSON.Document(item)));
                break;
            }
            case ChargingStation value:
            {
                if (value.ChargingWhenClosed is { } ChargingWhenClosedValue) json["chargingWhenClosed"] = JToken.FromObject(ChargingWhenClosedValue);
                if (value.Accessibility is { } AccessibilityValue) json["accessibility"] = JToken.FromObject(AccessibilityValue.ToString());
                if (value.LocationLanguage is { } LocationLanguageValue) json["locationLanguage"] = JToken.FromObject(LocationLanguageValue.ToString());
                if (value.PhysicalReference is { } PhysicalReferenceValue) json["physicalReference"] = JToken.FromObject(PhysicalReferenceValue);
                if (value.PaymentOptions.Any()) json["paymentOptions"] = new JArray(value.PaymentOptions.Select(item => item.ToString()));
                if (value.Features.Any()) json["features"] = new JArray(value.Features.Select(item => item.ToString()));
                if (value.VehicleTypes.Any()) json["vehicleTypes"] = new JArray(value.VehicleTypes.Select(item => item.ToString()));
                if (value.Images.Any()) json["images"] = new JArray(value.Images.Select(item => item.ToJSON()));
                if (value.ServiceIdentification is { } ServiceIdentificationValue) json["serviceIdentification"] = JToken.FromObject(ServiceIdentificationValue);
                if (value.ModelCode is { } ModelCodeValue) json["modelCode"] = JToken.FromObject(ModelCodeValue);
                if (value.Published is { } PublishedValue) json["published"] = JToken.FromObject(PublishedValue);
                if (value.Disabled is { } DisabledValue) json["disabled"] = JToken.FromObject(DisabledValue);
                if (value.MobilityRootCAs.Any()) json["mobilityRootCAs"] = new JArray(value.MobilityRootCAs.Select(item => POIJSON.Document(item)));
                if (value.EVRoamingPartners.Any()) json["evRoamingPartners"] = new JArray(value.EVRoamingPartners.Select(item => POIJSON.Document(item)));
                if (value.CertificationInfo is { } CertificationInfoValue) json["certificationInfo"] = JToken.FromObject(CertificationInfoValue.ToString());
                if (value.CalibrationInfo is { } CalibrationInfoValue) json["calibrationInfo"] = JToken.FromObject(CalibrationInfoValue.ToString());
                break;
            }
            case EVSE value:
            {
                if (value.PhotoURLs.Any()) json["photoURLs"] = new JArray(value.PhotoURLs.Select(item => item.ToString()));
                if (value.MobilityRootCAs.Any()) json["mobilityRootCAs"] = new JArray(value.MobilityRootCAs.Select(item => POIJSON.Document(item)));
                if (value.EnergyMix is { } EnergyMixValue) json["energyMix"] = JToken.FromObject(POIJSON.Document(EnergyMixValue));
                if (value.CalibrationInfo is { } CalibrationInfoValue) json["calibrationInfo"] = JToken.FromObject(CalibrationInfoValue.ToString());
                break;
            }
        }
    }
}
