# WWCP Point-of-Interest

This project is a spin-off from **WWCP-Core** redefining central entities *RoamingNetwork*, *ChargingStationOperator*, *ChargingPool*, *ChargingStation*, *EVSE*, and related location and status information—for efficient synchronization across distributed systems.

The project is based on the observation that most current EV-roaming protocols, as well as the relevant EU DATEX II exchange formats, are primarily designed as data-query interfaces. Consumers repeatedly request filtered subsets of remote datasets and then attempt to infer changes by comparing successive responses.

As a result, efficient, low-latency synchronization is merely an indirect side effect of recurring data retrieval rather than an explicit protocol capability. This leads to unnecessary network traffic, high processing overhead, delayed updates, ambiguous ordering, incomplete change detection, and frequent inconsistencies between participating systems.

WWCP Point-of-Interest therefore models charging-infrastructure data as a synchronizable distributed dataset. Its entities have stable identities, explicit ownership, defined lifecycle states, version information, timestamps, and well-defined update semantics. Changes can be represented as discrete events or deltas snapshots instead of requiring the repeated transfer and comparison of complete resource representations.

This makes synchronization an explicit, testable, and interoperable capability rather than an unreliable by-product of periodic polling or opportunistic pushing.
