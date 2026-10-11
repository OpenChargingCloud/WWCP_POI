/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using cloud.charging.open.protocols.WWCP.POI;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace WWCP_POI_Tests;

internal static class JsonViews
{
    // Presentation/resolver views can omit immutable content and managed timestamps.
    // Derived validators belong to complete transport tests, not a view equality check.
    internal static Boolean EqualViews(JToken left, JToken right)
    {
        JToken Clean(JToken value)
        {
            var copy = value.DeepClone();
            if (copy is JObject document)
            {
                var kind = document.ContainsKey("chargingStationOperators") ? nameof(RoamingNetwork) :
                           document.ContainsKey("chargingPools") ? nameof(ChargingStationOperator) :
                           document.ContainsKey("chargingStations") ? nameof(ChargingPool) :
                           document.ContainsKey("EVSEs") ? nameof(ChargingStation) :
                           document.ContainsKey("currentType") || document.ContainsKey("socketOutlets") ? nameof(EVSE) :
                           document.ContainsKey("elements") ? nameof(ChargingTariff) :
                           document.ContainsKey("priceComponents") ? nameof(ChargingTariffElement) :
                           document.ContainsKey("transparencySoftware") ? nameof(EnergyMeter) : nameof(Brand);
                POIRepresentation.RemoveETags(document, kind);
            }
            return copy;
        }
        var actual = Clean(left); var expected = Clean(right);
        var equal = JToken.DeepEquals(actual, expected);
        if (!equal) TestContext.Out.WriteLine($"Actual view: {actual}\nExpected view: {expected}");
        return equal;
    }
}
