/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// Unit-bearing JSON quantities with invariant, lossless formatting and explicit SI units.
/// </summary>
internal static class MetrologyJson
{
    internal static String Text<T>(T value) where T : struct, IMetrology<T>
    {
        var format = value switch
        {
            Watt quantity       => Scale(quantity.Value, "W", "kW", "MW", "GW"),
            WattHour quantity   => Scale(quantity.Value, "Wh", "kWh", "MWh", "GWh"),
            VoltAmpere quantity => Scale(quantity.Value, "VA", "kVA"),
            Volt quantity       => Scale(quantity.Value, "V", "kV"),
            Hertz quantity      => Scale(quantity.Value, "Hz", "kHz", "MHz", "GHz"),
            Ampere quantity     => Scale(quantity.Value, "A", "kA"),
            Ohm                 => "µΩ",
            _                   => null
        };
        String text;
        try { text = CanonicalText(value.ToString(format, CultureInfo.InvariantCulture)); }
        catch (OverflowException) { text = CanonicalText(value.ToString(null, CultureInfo.InvariantCulture)); }
        // A prefix conversion must not round a value with very high decimal precision.
        return T.TryParse(text, CultureInfo.InvariantCulture, out var parsed) && parsed.Equals(value)
                   ? text : CanonicalText(value.ToString(null, CultureInfo.InvariantCulture));
    }

    private static String CanonicalText(String text)
    {
        var separator = text.LastIndexOf(' ');
        if (separator < 0) return text;
        var number = text[..separator];
        if (number.Contains('.')) number = number.TrimEnd('0').TrimEnd('.');
        return number + text[separator..];
    }

    private static String Scale(Decimal value, params String[] units)
    {
        var magnitude = value < 0 ? -value : value;
        var index = 0;
        while (magnitude >= 1000m && index + 1 < units.Length)
        {
            magnitude /= 1000m;
            index++;
        }
        return units[index];
    }

    internal static T? Read<T>(JObject json, String field,
                               JsonValueParsing.ScalarParser<T> parse) where T : struct, IMetrology<T>
    {
        if (json[field] is not { } token || token.Type == JTokenType.Null) return null;
        var text = token.Type == JTokenType.String ? token.Value<String>()?.Trim() : null;
        if (text is not null && Regex.IsMatch(text, @"^[+-]?(?:[0-9]+(?:\.[0-9]+)?|\.[0-9]+)\s*[a-zA-ZµμΩΩ]+$") &&
            parse(text, out var quantity)) return quantity;
        throw new ArgumentException($"{field}: expected a valid {typeof(T).Name} value with its unit.");
    }

    internal static Boolean TryRead<T>(JObject json, String field,
                                       JsonValueParsing.ScalarParser<T> parse, out T? value, [NotNullWhen(false)] out String? error)
        where T : struct, IMetrology<T>
    {
        value = null;
        error = null;
        try { value = Read(json, field, parse); return true; }
        catch (Exception exception) { error = exception.Message; return false; }
    }

    internal static String Percent(Single value) => value.ToString("R", CultureInfo.InvariantCulture) + " %";

    internal static String AltitudeText(Double value)
    {
        if (!Double.IsFinite(value)) throw new ArgumentException("altitude: expected a finite length.");
        return value.ToString("R", CultureInfo.InvariantCulture) + " m";
    }

    internal static Double? ReadAltitude(JObject json, String field)
    {
        if (json[field] is not { } token || token.Type == JTokenType.Null) return null;
        var text = token.Type == JTokenType.String ? token.Value<String>()?.Trim() : null;
        if (text is null || !Regex.IsMatch(text, @"^[+-]?(?:[0-9]+(?:\.[0-9]+)?|\.[0-9]+)(?:[eE][+-]?[0-9]+)?\s*m$") ||
            !Double.TryParse(text[..^1].TrimEnd(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
            !Double.IsFinite(value)) throw new ArgumentException($"{field}: expected a finite altitude string with SI unit 'm'.");
        return value;
    }

    internal static String DurationText(TimeSpan value)
        => CanonicalText(((Decimal) value.Ticks / TimeSpan.TicksPerSecond).ToString(CultureInfo.InvariantCulture) + " s");

    internal static TimeSpan? ReadDuration(JObject json, String field)
    {
        if (json[field] is not { } token || token.Type == JTokenType.Null) return null;
        var text = token.Type == JTokenType.String ? token.Value<String>()?.Trim() : null;
        if (text is null || !Regex.IsMatch(text, @"^[+-]?(?:[0-9]+(?:\.[0-9]+)?|\.[0-9]+)\s*s$") ||
            !Decimal.TryParse(text[..^1].TrimEnd(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                              CultureInfo.InvariantCulture, out var seconds))
            throw new ArgumentException($"{field}: expected a duration string with SI unit 's'.");
        var ticks = checked(seconds * TimeSpan.TicksPerSecond);
        if (ticks != Decimal.Truncate(ticks)) throw new ArgumentException($"{field}: exceeds TimeSpan precision.");
        return TimeSpan.FromTicks(checked((Int64) ticks));
    }

    internal static Single ReadPercent(JObject json, String field)
    {
        var token = json[field];
        var text = token?.Type == JTokenType.String ? token.Value<String>()!.Trim() : null;
        if (text?.EndsWith('%') != true)
            throw new ArgumentException($"{field}: expected a percentage string with '%' unit.");
        text = text[..^1].TrimEnd();
        if (!Single.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
            !Single.IsFinite(value) || value < 0 || value > 100)
            throw new ArgumentException($"{field}: expected a percentage between zero and 100.");
        return value;
    }

    internal static JToken NormalizeProperty(InfrastructureEntityType type, String field, JToken value)
        => POIRepresentation.WithoutETags(() => NormalizePropertyCore(type, field, value));

    private static JToken NormalizePropertyCore(InfrastructureEntityType type, String field, JToken value)
    {
        if (value.Type == JTokenType.Null) return value;
        if (field == "geoLocation")
        {
            var location = (JObject) InfrastructureJson.Entry(value).DeepClone();
            if (ReadAltitude(location, "alt") is { } altitude) location["alt"] = AltitudeText(altitude);
            return location;
        }
        if (type == InfrastructureEntityType.EVSE && field is "maxVoltage" or "maxCurrent" or "maxPower" or "maxCapacity")
        {
            var json = new JObject(new JProperty(field, value.DeepClone()));
            switch (field)
            {
                case "maxVoltage": return new JValue(Text(Read<Volt>(json, field, Volt.TryParse)!.Value));
                case "maxCurrent": return new JValue(Text(Read<Ampere>(json, field, Ampere.TryParse)!.Value));
                case "maxPower": return new JValue(Text(Read<Watt>(json, field, Watt.TryParse)!.Value));
                case "maxCapacity": return new JValue(Text(Read<WattHour>(json, field, WattHour.TryParse)!.Value));
            }
        }
        if (type == InfrastructureEntityType.ChargingTariff && field == "elements")
        {
            if (value is not JArray elements) throw new ArgumentException("elements: expected an array.");
            return new JArray(elements.Select(token => ChargingTariffElement.Parse(InfrastructureJson.Entry(token)).ToJSON()));
        }
        if (type is InfrastructureEntityType.ChargingTariff or InfrastructureEntityType.EVSE && field == "energyMix")
            return EnergyMix.Parse(InfrastructureJson.Entry(value)).ToJSON();
        if (type == InfrastructureEntityType.ChargingConnector && field == "cable")
            return ChargingCable.Parse(InfrastructureJson.Entry(value)).ToJSON(Embedded: true)!;
        if (type == InfrastructureEntityType.ChargingPool && field == "gridConnectionPoint")
        {
            var point = (JObject) InfrastructureJson.Entry(value).DeepClone();
            Normalize<Volt>(point, "nominalVoltage", Volt.TryParse);
            Normalize<Hertz>(point, "nominalFrequency", Hertz.TryParse);
            Normalize<Watt>(point, "contractedImportPower", Watt.TryParse);
            Normalize<Watt>(point, "contractedExportPower", Watt.TryParse);
            Normalize<VoltAmpere>(point, "contractedImportApparentPower", VoltAmpere.TryParse);
            Normalize<VoltAmpere>(point, "contractedExportApparentPower", VoltAmpere.TryParse);
            if (point["geoLocation"] is { } location)
                point["geoLocation"] = NormalizeProperty(type, "geoLocation", location);
            return point;
        }
        return value;
    }

    internal static Boolean HasQuantities(InfrastructureEntityType type, String field)
        => field == "geoLocation" || type switch
        {
            InfrastructureEntityType.EVSE => field is "maxVoltage" or "maxCurrent" or "maxPower" or "maxCapacity" or "energyMix",
            InfrastructureEntityType.ChargingTariff => field is "elements" or "energyMix",
            InfrastructureEntityType.ChargingConnector => field == "cable",
            InfrastructureEntityType.ChargingPool => field == "gridConnectionPoint",
            _ => false
        };

    private static void Normalize<T>(JObject json, String field,
                                      JsonValueParsing.ScalarParser<T> parse) where T : struct, IMetrology<T>
    {
        if (Read(json, field, parse) is { } value) json[field] = Text(value);
    }

    internal static void NormalizeEntity(JObject json, InfrastructureEntityType type)
    {
        foreach (var property in json.Properties().ToArray())
        {
            var normalized = NormalizeProperty(type, property.Name, property.Value);
            if (!ReferenceEquals(normalized, property.Value)) property.Value = normalized;
        }
    }

    internal static JObject NormalizeHierarchy(JObject json, InfrastructureEntityType type)
    {
        var copy = (JObject) json.DeepClone();
        NormalizeEntity(copy, type);
        foreach (var relation in InfrastructureChangeSchema.Relations.Where(relation => relation.Value.Parent == type))
        {
            if (copy[relation.Value.Field] is not JArray children) continue;
            foreach (var child in children.OfType<JObject>().ToArray())
            {
                var normalized = NormalizeHierarchy(child, relation.Key);
                child.Replace(normalized);
            }
        }
        return copy;
    }
}
