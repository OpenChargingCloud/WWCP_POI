# Examples

Three examples, each going on from the one before: a roaming network whose operator three organizations hold, a
charging station operator asking to be added to it, and its pools, their templates and their groups. Examples A1 and A2
are C# with POI 3 alone, without a hub, in one console project, [csharp/](csharp/); A2 also shows the same with the
[RoamingHubCLI](https://github.com/OpenChargingCloud/RoamingHubCLI): at its prompt or over SSH, in its web interface and
over HTTP.

| | What it does | Revisions | Signed by |
|---|---|---|---|
| [Example A1](exampleA1.md) | **The creation of the roaming network `QA`, and how to check it.** The roaming network operator `OCC`, whose keys three organizations hold, two of them deciding via a 2/3 quorum, states its identity keys and quorum in a key statement, each organization certifies its own signing key, `QA` is made, and its trust anchor published as a QR code. Then `QA` is checked from the QR code and what is published alone. | 0 | `OCC`, its three holders |
| [Example A2](exampleA2.md) | **The charging station operator `DE*GEF`.** The operator makes two identity keys and a signing key for each usage, keeps them in `DE-GEF/`, makes the change set adding it and signs it with its identity keys, as the proof that it holds them; two holders of `OCC` sign it as well. With the hub: it signs as the RNO and lets the change set wait for the signatures of the party. | 1 | the change set: `DE*GEF`'s identity keys and two holders of `OCC`; the commit: `OCC` |
| [Example A3](exampleA3.md) | **Two pools, their templates and their groups.** `DE*GEF` adds the templates of a DC and an AC station and their EVSEs, two pools of a DC and an AC station of two EVSEs each, and a station group and an EVSE group for each current; then the pools as they are at runtime. Only its own program can do it: nobody else has a key of `DE*GEF`. Not yet part of [csharp/](csharp/). | 2 to 6 | `DE*GEF` |

What each way needs:

| Way | Example A1 | Example A2 | Example A3 |
|---|---|---|---|
| C#, POI 3 alone ([csharp/](csharp/)) | `RoamingNetworkKeyStatement.Create`, `RoamingNetworkSigningKeyCertificate.Create`, `RoamingNetworkHistory.Start` | `CreateChangeSet([ Add(operator) ])`, signed by `DE*GEF`'s identity keys and two holders of `OCC` | `history.Commit`, signed by `DE*GEF` |
| prompt, SSH | `roamingNetworks add QA …` | `roamingNetworks QA add chargingStationOperator …` | shows: `roamingNetworks QA` |
| web interface | **New roaming network** | **New party of a roaming network** | shows: **Points of interest** |
| HTTP, as an administrator | `POST /api/v1/poi/RNs`, with an API key | `POST /api/v1/poi/RNs/QA/CSOs`, `202` | - |
| HTTP, by anybody | - | `POST /poi/RNs/QA/history/changeSets/pending/{id}/signatures`, the signatures of `DE*GEF`'s identity keys | `COMMIT /poi/RNs/QA/history/changeSets`, a change set `DE*GEF` signs |

Run examples A1 and A2 in [csharp/](csharp/), where its output stays:

```
dotnet run
```

A run writes the private keys of the organizations and of `DE*GEF` into directories of their own, what is published into
`published/`, and the history of `QA` into `poi/QA/`, all ignored by git. What POI 3 is, its entities and its change
sets, is in [docs/](../docs/README.md) and the [README](../README.md) of WWCP_POI.
