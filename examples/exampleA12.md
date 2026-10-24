# Example A12: the atlas and the traffic report – live over the WebSocket

Examples A1 to A11 keep the **atlas**: where a station is, what it has, who operates it, its tariffs and keys – signed,
versioned and provable months later. What changes every minute is the **traffic report**: whether an EVSE is charging
right now, the power it delivers, the price shown at this moment. POI 3 keeps both, but apart: the atlas in commits and
checkpoints, the traffic report as the **runtime status** of the head, pushed over the WebSocket of the history to
everybody who subscribed, the moment it changes (`POI_API.WebSocket.cs`; part 8 of the stories,
[The atlas and the traffic report](../stories/08-the-atlas-and-the-traffic-report/README.md)).

`OCC` serves `QA` as [example A11](exampleA11.md) left it, epoch 2, revision 2. A mirror, e.g. of a National Access
Point, follows it and serves its copy. A driver's app subscribes at the mirror. `DE*GEF` reports the status of
`DE*GEF*E1*1` at the server of `OCC`, and lowers a tariff over the same connection.

| Who | Where | What |
|---|---|---|
| `OCC` | its server, `POI_API` | serves `QA`; `DE*GEF` may set the status of its own EVSEs, everybody may read and subscribe |
| the mirror | a `RoamingNetworkPeer` of `OCC`'s server, served by a `POI_API` of its own | trusts the trust anchor of epoch 2, takes commits and status, pushes nothing back |
| the driver's app | a WebSocket to the mirror | subscribes to `statusUpdates`, `energyStatusUpdates`, `priceStatusUpdates` and `commits` |
| `DE*GEF` | a WebSocket to `OCC`'s server, with its token | `setStatus` of `DE*GEF*E1*1`; `commitChangeSet`: revision 3, signed by its ML-DSA `tariffs` key |

The code is [exampleA12.cs](csharp/exampleA12.cs), with the helpers of [exampleA4.cs](csharp/exampleA4.cs), called by
[Program.cs](csharp/Program.cs) with what example A11 left:

```csharp
await ExampleA12.Run(a11);

ExampleA12.Validate();
```

Both servers listen on `127.0.0.1`, on ports the system picks. The frames are those on the wire, OCPP-J style:
`[2, id, action, payload]` a request, `[3, id, payload]` its response, `[6, id, action, payload]` a push. The output is
what one run printed, `…` for elided digests.


## 1. OCC serves QA, and a mirror follows

`OCC` serves the history of epoch 2 by its `POI_API`. Over the WebSocket `ws://…/RNs/QA/history/ws` everybody may read
and subscribe, as everybody may read `QA` over HTTP; `DE*GEF` comes with the token `OCC` gave it, and may set the status
of its own EVSEs only. A change set it sends is committed when its owners' signatures allow it, as every change set.

```csharp
var occAPI = new POI_API(occServer) {

                 AuthenticateWebSocketClient   = (request, cancellationToken) => Task.FromResult(
                                                     request.Authorization switch {
                                                         null                                                         => "anonymous",
                                                         HTTPBearerAuthentication bearer when bearer.Token == gefToken  => ExampleA2.gefId.ToString(),
                                                         _                                                            => (String?) null
                                                     }),

                 AuthorizeStatusUpdate         = (request, login, statusUpdate) => statusUpdate.EntityType == nameof(EVSE) &&
                                                                                   EVSE_Id.TryParse(statusUpdate.EntityId, out var id) &&
                                                                                   id.OperatorId.ToString() == login,

                 AuthorizeChangeSetSubmission  = (request, changeSet) => true

             };

occAPI.AddRoamingNetworkHistory(A11.History);
```

The mirror trusts the trust anchor of epoch 2 it scanned, `published/QA-trustAnchor-mldsa.txt`: the snapshot it starts
from must name its keys, and every commit must be signed by signing keys of `OCC` certified by them. It passes on and
changes nothing, so it pushes nothing back. Once it caught up, its own `POI_API` serves its copy, with a WebSocket of
its own.

```csharp
var mirror = new RoamingNetworkPeer(occURL,
                                    Options: new RoamingNetworkPeerOptions {
                                                 TrustedRoamingNetworkOperatorKeyIds  = anchor.Keys,
                                                 VerifyCommitSignature                = (commit, signature) => ExampleA4.SigningKeyOf(anchor, follower?.History?.Head.RoamingNetwork ?? published, signature) is POIPublicKey key &&
                                                                                                              commit.VerifySignature(signature, key.Key, key.Id, out _),
                                                 RequiredSignatureRoles               = [ RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator ],
                                                 FollowCheckpoints                    = false,
                                                 PushCommits                          = false,
                                                 PushStatus                           = false,
                                                 PushCheckpoints                      = false,
                                                 FollowPriceStatements                = false,
                                                 PushPriceStatements                  = false
                                             });

await mirror.StartAsync(…);

mirrorAPI.AddRoamingNetworkHistory(mirror.History!);
```

```
OCC serves QA, revision 2 of epoch 2: ws://127.0.0.1:64802/RNs/QA/history/ws
The mirror follows: revision 2 of epoch 2, the head of OCC: True, 114 runtime statuses taken; it serves ws://127.0.0.1:64804/RNs/QA/history/ws
```

The runtime statuses come with the snapshot, next to the version, not in it.


## 2. The driver's app subscribes at the mirror

The app subscribes to the topics it shows, then asks once for the status now: a push tells of a change, not of what was
before.

```
→ [2,"1","subscribe",{"topics":["statusUpdates","energyStatusUpdates","priceStatusUpdates","commits"]}]
← [3,"1",{"topics":["commits","energyStatusUpdates","priceStatusUpdates","statusUpdates"]}]
DE*GEF*E1*1 status: available since 2026-10-24T12:21:26.744Z
DE*GEF*E1*1 adminStatus: operational since 2026-10-24T12:21:26.744Z
DE*GEF*E1*1 priceStatus: {"prices":[{"chargingTariffId":"DE*GEF*T_DC","elements":[{"priceComponents":[{"type":"ENERGY","price":0.59,"stepSize":1}]}]}]} since 2026-10-24T12:21:33.089Z
The head: revision 2
```

The price status of `DE*GEF*E1*1` is the price of its price statement of [example A11](exampleA11.md), 0.59 €/kWh: a
statement taken sets the price status as well.


## 3. DE*GEF reports live, at OCC's server

A driver plugs in at `DE*GEF*E1*1`. `DE*GEF` sets its status at the server of `OCC`, which answers with the status now;
the server pushes it to the mirror, the mirror to the app (`⇐`). Then the power the EVSE delivers and what a vehicle
arriving now would still get, its energy status; the price shown right now, 0.49 €/kWh, a price status; and when the
driver leaves, available again.

```csharp
await gef.Show("setStatus", StatusPayload(POIRuntimeStatusKind.Status,       evseId, "charging"));
await gef.Show("setStatus", StatusPayload(POIRuntimeStatusKind.EnergyStatus, evseId, POIEnergyStatus.ToText(new EnergyInfo(Watt.FromKW(98), Watt.FromKW(52), CurrentVoltage: Volt.FromV(780), RemainingChargingTime: TimeSpan.FromMinutes(25)))));
await gef.Show("setStatus", StatusPayload(POIRuntimeStatusKind.PriceStatus,  evseId, ExampleA8.Prices(ExampleA8.DC, 0.49m).Text));
await gef.Show("setStatus", StatusPayload(POIRuntimeStatusKind.Status,       evseId, "available"));
```

```
→ [2,"1","setStatus",{"entityId":"DE*GEF*E1*1","entityType":"EVSE","kind":"status","value":"charging"}]
← [3,"1",{"current":{"entityId":"DE*GEF*E1*1","entityType":"EVSE","kind":"status","timestamp":"2026-10-24T12:21:34.542Z","value":"charging"},"result":"Updated"}]
⇐ [6,"p1","statusUpdate",{"@context":"https://open.charging.cloud/contexts/wwcp/roamingNetwork/statusUpdate/v1","entityId":"DE*GEF*E1*1","entityType":"EVSE","kind":"status","oldTimestamp":"2026-10-24T12:21:26.744Z","oldValue":"available","timestamp":"2026-10-24T12:21:34.542Z","value":"charging"}]
→ [2,"2","setStatus",{"entityId":"DE*GEF*E1*1","entityType":"EVSE","kind":"energyStatus","value":{"available":52000,"currentVoltage":780,"remainingChargingTime":1500,"used":98000}}]
← [3,"2",{"current":{"entityId":"DE*GEF*E1*1","entityType":"EVSE","kind":"energyStatus","timestamp":"2026-10-24T12:21:34.559Z","value":{"available":52000,"currentVoltage":780,"remainingChargingTime":1500,"used":98000}},"result":"Updated"}]
⇐ [6,"p2","energyStatusUpdate",{"@context":"https://open.charging.cloud/contexts/wwcp/roamingNetwork/statusUpdate/v1","entityId":"DE*GEF*E1*1","entityType":"EVSE","kind":"energyStatus","timestamp":"2026-10-24T12:21:34.559Z","value":{"available":52000,"currentVoltage":780,"remainingChargingTime":1500,"used":98000}}]
→ [2,"3","setStatus",{"entityId":"DE*GEF*E1*1","entityType":"EVSE","kind":"priceStatus","value":{"prices":[{"chargingTariffId":"DE*GEF*T_DC","elements":[{"priceComponents":[{"price":0.49,"stepSize":1,"type":"ENERGY"}]}]}]}}]
← [3,"3",{"current":{"entityId":"DE*GEF*E1*1","entityType":"EVSE","kind":"priceStatus","timestamp":"2026-10-24T12:21:34.574Z","value":{"prices":[{"chargingTariffId":"DE*GEF*T_DC","elements":[{"priceComponents":[{"price":0.49,"stepSize":1,"type":"ENERGY"}]}]}]}},"result":"Updated"}]
⇐ [6,"p3","priceStatusUpdate",{"@context":"https://open.charging.cloud/contexts/wwcp/roamingNetwork/statusUpdate/v1","entityId":"DE*GEF*E1*1","entityType":"EVSE","kind":"priceStatus","oldTimestamp":"2026-10-24T12:21:33.089Z","oldValue":{"prices":[{"chargingTariffId":"DE*GEF*T_DC","elements":[{"priceComponents":[{"price":0.59,"stepSize":1,"type":"ENERGY"}]}]}]},"timestamp":"2026-10-24T12:21:34.574Z","value":{"prices":[{"chargingTariffId":"DE*GEF*T_DC","elements":[{"priceComponents":[{"price":0.49,"stepSize":1,"type":"ENERGY"}]}]}]}}]
→ [2,"4","setStatus",{"entityId":"DE*GEF*E1*1","entityType":"EVSE","kind":"status","value":"available"}]
← [3,"4",{"current":{"entityId":"DE*GEF*E1*1","entityType":"EVSE","kind":"status","timestamp":"2026-10-24T12:21:34.577Z","value":"available"},"result":"Updated"}]
⇐ [6,"p4","statusUpdate",{"@context":"https://open.charging.cloud/contexts/wwcp/roamingNetwork/statusUpdate/v1","entityId":"DE*GEF*E1*1","entityType":"EVSE","kind":"status","oldTimestamp":"2026-10-24T12:21:34.542Z","oldValue":"charging","timestamp":"2026-10-24T12:21:34.577Z","value":"available"}]
```

Each push came over two servers, without polling. The price status of 0.49 €/kWh is what an app
shows right now; unsigned, it is no evidence: the price statement of [example A11](exampleA11.md), signed and named by
a checkpoint, is.

A report that comes late, of five minutes ago, goes into the history of the EVSE, and its status stays: updates arriving
out of order end with the latest one. Nobody else sets the status of `DE*GEF*E1*1`: not the app, at the server of
`OCC`, and nobody with another token, whose upgrade is refused. Four changes of the status, and the atlas did not change
once: the head is the same commit.

```
→ [2,"5","setStatus",{"entityId":"DE*GEF*E1*1","entityType":"EVSE","kind":"status","timestamp":"2026-10-24T12:16:34.578Z","value":"charging"}]
← [3,"5",{"current":{"entityId":"DE*GEF*E1*1","entityType":"EVSE","kind":"status","timestamp":"2026-10-24T12:21:34.577Z","value":"available"},"description":"The EVSE 'DE*GEF*E1*1' has a status since 2026-10-24T12:21:34.577Z!","result":"Outdated"}]
The app sets the status at OCC's server: The sender may not set this status!
Another token: The server returned status code '401' when status code '101' was expected.
The head after them: revision 2, the same commit: True
```


## 4. A change of the atlas, pushed as well

`DE*GEF` lowers its DC tariff to 0.65 €/kWh, by a change set signed by its ML-DSA `tariffs` key, sent over the same
WebSocket. `OCC` commits it as revision 3 and signs the commit; the mirror checks it against the trust anchor and takes
it, and pushes it to the app: the complete commit, here without its change set and signatures.

```csharp
var committed = await gef.Request("commitChangeSet", new JObject(new JProperty(POI_API.PropertyKeys.ChangeSet, lower.ToJSON(Embedded: true))));
```

```
DE*GEF commits 'The DC charging tariff: 0.65 €/kWh': Committed, revision 3
⇐ [6,"p5","commit",{"@context":"https://open.charging.cloud/contexts/wwcp/roamingNetwork/commit/v3","@id":"42949051fa016581…270278ac2","changeSet":"…","eTags":[["json","sha256","hex","9055916e…d0b2d57e"],["cbor","sha256","hex","83617bdf…8b764b09"],["effectiveTree","sha256","hex","c92a459e…89a789ba5"]],"epoch":2,"parentIds":["41786f1288ded5dc…c3b0bcae0"],"revision":3,"roamingNetworkId":"QA","signatures":"… 3 signatures"}]
The mirror: revision 3, the head of OCC: True
```


## 5. Subscriptions end with the connection

The app is away for a while, e.g. in a tunnel, and the next driver plugs in meanwhile. Connected again, it is told
nothing until it subscribes, and nothing of what happened meanwhile: it asks for the status now. What the mirror serves
then, the head with its runtime status, is published as `published/QA-live-snapshot.json`; what the app got before, as
`driver/live.json`.

```
Connected again: 0 pushes without subscribing
Asked for: DE*GEF*E1*1 status charging since 2026-10-24T12:21:34.651Z
```


## 6. The traffic report and the atlas

`Validate()` reads what the app got and what the mirror serves. The status updates are runtime data: none is signed, and
the head before and after them is the same commit. The commit pushed with them is signed like every other: by `DE*GEF`'s
`tariffs` key, and by signing keys of `OCC`, checked against the trust anchor of epoch 2; it is the head of the mirror.
The runtime status the mirror serves next to its head is outside the version: without it, the snapshot has the same
version. And no checkpoint holds any.

```
The app got 4 status updates, 2 statusUpdate, 1 energyStatusUpdate, 1 priceStatusUpdate, none signed: True
The head before and after them: revision 2 and 2, the same commit: True
The commit pushed: revision 3, 'The DC charging tariff: 0.65 €/kWh', signed by DE*GEF with ML-DSA-65; 3 of its 3 signatures by signing keys of OCC, ML-DSA-65, checked against the trust anchor of epoch 2; the head of the mirror: True
The live snapshot: DE*GEF*E1*1 charging since 2026-10-24T12:21:34.651Z; without its runtime status the same version: True
Checkpoint 0 of epoch 2 holds runtime status: False
```


## Limits

- The runtime status is as trustworthy as the server telling it and the operator setting it: it is neither signed nor
  provable later. What has to be provable, the price that applied, is a price statement ([example A8](exampleA8.md));
  signed statements of the status, e.g. of the availability of a station, are open
  ([SPECIFICATION.md](../SPECIFICATION.md), section 8.8, point 4, and section 12.6, point 3).
- The mirror takes no price statements: those of epoch 1 that `OCC` took along into epoch 2 (section 11.4) are signed
  with ES256, not with an algorithm of epoch 2, and a peer starting in epoch 2 refuses them. How a peer takes them is
  open.
- A verifier of commit signatures is given the commit and the signature, not the version it is checked in; before the
  mirror has a history of its own, e.g. for the head commit of the snapshot it starts from, it checks the signing keys of
  `OCC` in `QA` as published.
- A token per operator is the simplest login; a server may as well use client certificates
  (`RoamingNetworkPeerOptions.ConfigureWebSocket`, `AuthenticateWebSocketClient`).
- The hub serves its roaming networks by the same `POI_API`, with their WebSockets; it lets nobody set a status over
  them yet (`AuthorizeStatusUpdate`), and this example uses `POI_API` without a hub.

