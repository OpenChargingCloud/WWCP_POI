# JSON tests

Run the NUnit test project from the repository root:

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj
```

The tests build the actual POI assembly and its local WWCP_CoreData, Hermod and Styx dependencies.

Coverage includes cable/connector payloads, images, additional coordinates, authentication variants,
brands with ID-only and expanded licenses, charging products, and change sets. Assertions compare
individual fields and JSON documents where entity equality only checks identity or selected fields.
Tests exercise embedded representations, omitted optional values, zero and false values, invalid
input, nested parser callbacks and decimal values under different cultures.

## Wire conventions

- Cable length is measured in metres and resistance in microohms.
- Additional coordinates retain their existing string latitude/longitude fields with invariant decimal
  separators. Optional altitude is a number in metres; coordinates must be finite and within WGS84 bounds.
- Product durations are seconds, power is watts and energy is watt-hours. The legacy
  `stopChargingAfterKWh` field still carries watt-hours, matching the existing serializer.
- Authentication modes retain the existing type names and `Number`/`StationCode` casing.
- Brand license strings represent identity only. Expanded license documents also preserve metadata.
- Change sets use System.Text.Json. Omitted `OldValue` means no precondition; a present JSON null
  means the expected previous value is null. Omitted `NewValue` is absent; a present JSON null
  explicitly clears a property. Absent values are now omitted when writing change sets.
  Operations validate their required values on construction and own copies of their JSON payloads.

This suite does not yet establish complete snapshot deserialization of the operator/pool/station/EVSE
hierarchy or canonical JSON for signature verification.
