/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using Newtonsoft.Json.Linq;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// A tariff price and its dimension-specific, explicitly typed billing increment.
/// </summary>
public readonly partial struct ChargingPriceComponent
{
    /// <summary>
    /// The billed tariff dimension.
    /// </summary>
    public ChargingDimensionTypes Type { get; }

    /// <summary>
    /// The monetary price per tariff unit.
    /// </summary>
    public Decimal Price { get; }

    /// <summary>
    /// The energy billing increment, present for energy pricing.
    /// </summary>
    public WattHour? EnergyStep { get; }

    /// <summary>
    /// The current billing increment, present for current pricing.
    /// </summary>
    public Ampere? CurrentStep { get; }

    /// <summary>
    /// The time billing increment, present for charging or parking time pricing.
    /// </summary>
    public TimeSpan? DurationStep { get; }

    private ChargingPriceComponent(ChargingDimensionTypes type, Decimal price,
                                    WattHour? energyStep = null, Ampere? currentStep = null, TimeSpan? durationStep = null)
    {
        Type = type;
        Price = price;
        EnergyStep = energyStep;
        CurrentStep = currentStep;
        DurationStep = durationStep;
        if (!IsValid) throw new ArgumentException("A billing increment must be positive and match the tariff dimension.");
    }

    internal Boolean IsValid => Type switch
    {
        ChargingDimensionTypes.FLAT => EnergyStep is null && CurrentStep is null && DurationStep is null,
        ChargingDimensionTypes.ENERGY => EnergyStep > WattHour.AdditiveIdentity && CurrentStep is null && DurationStep is null,
        ChargingDimensionTypes.MAX_CURRENT or ChargingDimensionTypes.MIN_CURRENT =>
            CurrentStep > Ampere.AdditiveIdentity && EnergyStep is null && DurationStep is null,
        ChargingDimensionTypes.TIME or ChargingDimensionTypes.PARKING_TIME =>
            DurationStep > TimeSpan.Zero && EnergyStep is null && CurrentStep is null,
        _ => false
    };

    /// <summary>
    /// Create a flat fee without a physical billing increment.
    /// </summary>
    public static ChargingPriceComponent FlatRate(Decimal Price) => new(ChargingDimensionTypes.FLAT, Price);

    /// <summary>
    /// Create energy pricing with a typed energy increment.
    /// </summary>
    public static ChargingPriceComponent Energy(Decimal Price, WattHour BillingIncrement)
        => new(ChargingDimensionTypes.ENERGY, Price, energyStep: BillingIncrement);

    /// <summary>
    /// Create maximum-current pricing with a typed current increment.
    /// </summary>
    public static ChargingPriceComponent MaximumCurrent(Decimal Price, Ampere BillingIncrement)
        => new(ChargingDimensionTypes.MAX_CURRENT, Price, currentStep: BillingIncrement);

    /// <summary>
    /// Create minimum-current pricing with a typed current increment.
    /// </summary>
    public static ChargingPriceComponent MinimumCurrent(Decimal Price, Ampere BillingIncrement)
        => new(ChargingDimensionTypes.MIN_CURRENT, Price, currentStep: BillingIncrement);

    /// <summary>
    /// Create charging-time pricing with a typed duration increment.
    /// </summary>
    public static ChargingPriceComponent ChargingTime(Decimal Price, TimeSpan BillingIncrement)
        => new(ChargingDimensionTypes.TIME, Price, durationStep: BillingIncrement);

    /// <summary>
    /// Create parking-time pricing with a typed duration increment.
    /// </summary>
    public static ChargingPriceComponent ParkingTime(Decimal Price, TimeSpan BillingIncrement)
        => new(ChargingDimensionTypes.PARKING_TIME, Price, durationStep: BillingIncrement);

    /// <summary>
    /// Return JSON with an explicit SI unit on each physical billing increment.
    /// </summary>
    public JObject ToJSON()
    {
        if (!IsValid) throw new InvalidOperationException("Invalid price component.");
        var json = new JObject(new JProperty("type", Type.ToString()), new JProperty("price", Price));
        if (EnergyStep is { } energy) json["stepSize"] = MetrologyJson.Text(energy);
        if (CurrentStep is { } current) json["stepSize"] = MetrologyJson.Text(current);
        if (DurationStep is { } duration) json["stepSize"] = MetrologyJson.DurationText(duration);
        return POIRepresentation.AddETags(this, json);
    }

    /// <summary>
    /// Return this immutable value.
    /// </summary>
    public ChargingPriceComponent Clone() => this;

    public override Int32 GetHashCode() => HashCode.Combine(Type, Price, EnergyStep, CurrentStep, DurationStep);

    public override String ToString() => ToJSON().ToString(Newtonsoft.Json.Formatting.None);
}
