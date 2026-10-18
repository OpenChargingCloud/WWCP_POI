# Examples

Three examples, each going on from the one before, from a fresh installation of the
[RoamingHubCLI](https://github.com/OpenChargingCloud/RoamingHubCLI) to a roaming network with an operator, its pools,
their templates and their groups. Each shows every way there is: at the prompt of the hub or over SSH, in its web
interface, over HTTP with curl or C#, and in C# with POI 3 alone, without a hub.

| | What it does | Revisions | Signed by |
|---|---|---|---|
| [Example 1](example1.md) | **The first start and the roaming network `QA`.** What the first start makes - the account `root`, the roaming network operator (RNO) the hub is with its keys, no roaming network -, who may make one, and `QA` made. | 0 | the hub, as the RNO |
| [Example 2](example2.md) | **The charging station operator `DE*GEF`.** The operator makes its keys and keeps them in `DE-GEF/`; the hub adds it to `QA` with the public halves. Also: what is refused, and the other parties. | 1 | the hub, as the RNO |
| [Example 3](example3.md) | **Two pools, their templates and their groups.** `DE*GEF` adds the templates of a DC and an AC station and their EVSEs, two pools of a DC and an AC station of two EVSEs each, and a station group and an EVSE group for each current; then the pools as they are at runtime. Only its own program can do it: the hub has no key of `DE*GEF`. | 2 to 6 | `DE*GEF` |

What each way needs:

| Way | Example 1 | Example 2 | Example 3 |
|---|---|---|---|
| prompt, SSH | `roamingNetworks add QA …` | `roamingNetworks QA add chargingStationOperator …` | shows: `roamingNetworks QA` |
| web interface | **New roaming network** | **New party of a roaming network** | shows: **Points of interest** |
| HTTP, as an administrator | `POST /api/v1/poi/RNs`, with an API key | `POST /api/v1/poi/RNs/QA/CSOs` | - |
| HTTP, by anybody | - | - | `COMMIT /poi/RNs/QA/history/changeSets`, a change set `DE*GEF` signs |
| C#, POI 3 alone | `RoamingNetworkHistory.Start` | `RoamingNetworkChange.Add(operator)`, signed by the RNO | `history.Commit`, signed by `DE*GEF` |

The C# programs are three console projects referencing `WWCP_POI3`: the administrator's (`Admin/`), the operator's
(`DE-GEF/`, examples 2 and 3) and the one without a hub (examples 1 to 3, in one directory). What POI 3 is, its
entities and its change sets, is in [docs/](../docs/README.md) and the [README](../README.md) of WWCP_POI.
