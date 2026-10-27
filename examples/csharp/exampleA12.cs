using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.WWCP;
using cloud.charging.open.protocols.WWCP.POI;


public static class ExampleA12
{

    // The atlas and the traffic report: what A1 to A11 keep is the atlas, signed, versioned and provable. What changes every
    // minute, whether an EVSE is charging, the power it delivers, the price shown right now, is the traffic report: pushed
    // live over the WebSocket of the history to everybody who subscribed, neither signed nor kept in commits or checkpoints.
    // OCC serves QA, a mirror follows it, a driver's app subscribes at the mirror, and DE*GEF reports at OCC's server.


    /// <summary>
    /// What the driver's app received: the pushes, and the head before and after the status updates.
    /// </summary>
    internal const String Live = "driver/live.json";


    public static async Task Run(ExampleA11.Result A11)
    {

        var qa      = A11.History.RoamingNetworkId;
        var evseId  = ExampleA10.EVSEId.ToString();

        #region 1. OCC serves QA, and a mirror follows

        // The token OCC gave DE*GEF: with it, DE*GEF may set the status of its own EVSEs; without one, everybody may read and
        // subscribe, as everybody may read QA over HTTP; with another one, nobody gets in.
        var gefToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

        File.WriteAllText("DE-GEF/occ-token.txt", gefToken);

        await using var occServer  = new HTTPServer(IPAddress: IPv4Address.Localhost, TCPPort: IPPort.Zero, AutoStart: true);
        await using var occAPI     = new POI_API(occServer) {

                                         AuthenticateWebSocketClient   = (request, cancellationToken) => Task.FromResult(
                                                                             request.Authorization switch {
                                                                                 null                                                         => "anonymous",
                                                                                 HTTPBearerAuthentication bearer when bearer.Token == gefToken  => ExampleA2.gefId.ToString(),
                                                                                 _                                                            => (String?) null
                                                                             }),

                                         // Only of its own EVSEs.
                                         AuthorizeStatusUpdate         = (request, login, statusUpdate) => statusUpdate.EntityType == nameof(EVSE) &&
                                                                                                           EVSE_Id.TryParse(statusUpdate.EntityId, out var id) &&
                                                                                                           id.OperatorId.ToString() == login,

                                         // What a change set may change, its owners' signatures decide, as for every commit.
                                         AuthorizeChangeSetSubmission  = (request, changeSet) => true

                                     };

        occAPI.AddRoamingNetworkHistory(A11.History);

        var occURL = new Uri($"ws://127.0.0.1:{occServer.TCPPort}/RNs/{qa}/history/ws");

        Console.WriteLine($"OCC serves {qa}, revision {A11.History.Head.Commit.Revision} of epoch {A11.History.Epoch.Number}: {occURL}");

        // The mirror, e.g. of a National Access Point, trusts the trust anchor of epoch 2 it scanned: the snapshot it starts
        // from must name its keys, and every commit must be signed by signing keys of the quorum. It passes on, it changes
        // nothing: it pushes no commits, no status and no price statements back. It takes those of OCC: of the algorithms of
        // epoch 2, as OCC took along only those, as every server starting epoch 2 does.
        if (!RoamingNetworkTrustAnchor.TryParse(File.ReadAllText("published/QA-trustAnchor-mldsa.txt"), out var anchor, out var error))
            Helpers.Fail(error);

        // The version it checks the signing keys of OCC in: its own head, and QA as published before it has one, e.g. for the
        // head commit of the snapshot it starts from.
        if (!RoamingNetwork.TryParse(Helpers.ReadJSON($"published/{qa}.json"), out var published, out error))
            Helpers.Fail(error);

        RoamingNetworkPeer? follower = null;

        await using var mirror = new RoamingNetworkPeer(occURL,
                                                        Options: new RoamingNetworkPeerOptions {
                                                                     TrustedRoamingNetworkOperatorKeyIds  = anchor.Keys,
                                                                     VerifyCommitSignature                = (commit, signature) => ExampleA4.SigningKeyOf(anchor, follower?.History?.Head.RoamingNetwork ?? published, signature) is POIPublicKey key &&
                                                                                                                                  commit.VerifySignature(signature, key.Key, key.Id, out _),
                                                                     RequiredSignatureRoles               = [ RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator ],
                                                                     FollowCheckpoints                    = false,
                                                                     PushCommits                          = false,
                                                                     PushStatus                           = false,
                                                                     PushCheckpoints                      = false,
                                                                     PushPriceStatements                  = false
                                                                 });

        follower = mirror;

        mirror.OnError += (peer, description, exception) => Console.WriteLine($"The mirror: {description}");

        await mirror.StartAsync(new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token);

        var mirrored = mirror.History!;

        await using var mirrorServer  = new HTTPServer(IPAddress: IPv4Address.Localhost, TCPPort: IPPort.Zero, AutoStart: true);
        await using var mirrorAPI     = new POI_API(mirrorServer);

        mirrorAPI.AddRoamingNetworkHistory(mirrored);

        var mirrorURL = new Uri($"ws://127.0.0.1:{mirrorServer.TCPPort}/RNs/{qa}/history/ws");

        Console.WriteLine($"The mirror follows: revision {mirrored.Head.Commit.Revision} of epoch {mirrored.Epoch.Number}, the head of OCC: {mirrored.Head.Commit.Id == A11.History.Head.Commit.Id}, {mirrored.RuntimeStatus.All.Count()} runtime statuses and {mirrored.PriceStatements.Count} price statement(s) taken; it serves {mirrorURL}");
        Console.WriteLine();

        #endregion

        #region 2. The driver's app subscribes at the mirror

        await using var app = await LiveClient.Connect(mirrorURL);

        await app.Show("subscribe", new JObject(new JProperty("topics", new JArray(POI_API.StatusUpdatesTopic, POI_API.EnergyStatusUpdatesTopic, POI_API.PriceStatusUpdatesTopic, POI_API.CommitsTopic))));

        // What it shows before anything is pushed: the runtime status now, of the EVSE the driver looks at.
        var runtimeStatus = await app.Request("runtimeStatus");

        foreach (var status in StatusOf(runtimeStatus, evseId))
            Console.WriteLine($"{evseId} {POIStatusUpdate.KindText(status.Kind)}: {status.Value} since {status.Timestamp.ToISO8601()}");

        var headBefore = (await app.Request("head")).Payload!;

        Console.WriteLine($"The head: revision {headBefore["revision"]}");
        Console.WriteLine();

        #endregion

        #region 3. DE*GEF reports live, at OCC's server

        await using var gef = await LiveClient.Connect(occURL, gefToken);

        // A driver plugs in at DE*GEF*E1*1: charging. OCC's server pushes it to the mirror, the mirror to the app.
        await gef.Show("setStatus", StatusPayload(POIRuntimeStatusKind.Status, evseId, "charging"));
        await app.ShowPush("statusUpdate", evseId);

        // The power it delivers, and what a vehicle arriving now would still get at it.
        await gef.Show("setStatus", StatusPayload(POIRuntimeStatusKind.EnergyStatus, evseId, POIEnergyStatus.ToText(new EnergyInfo(Watt.FromKW(98), Watt.FromKW(52), CurrentVoltage: Volt.FromV(780), RemainingChargingTime: TimeSpan.FromMinutes(25)))));
        await app.ShowPush("energyStatusUpdate", evseId);

        // The price shown right now, ad hoc and unsigned: the signed price statement of example A11 stays the evidence.
        await gef.Show("setStatus", StatusPayload(POIRuntimeStatusKind.PriceStatus, evseId, ExampleA8.Prices(ExampleA8.DC, 0.49m).Text));
        await app.ShowPush("priceStatusUpdate", evseId);

        // The driver leaves: available again.
        await gef.Show("setStatus", StatusPayload(POIRuntimeStatusKind.Status, evseId, "available"));
        await app.ShowPush("statusUpdate", evseId);

        // A report that comes late, of five minutes ago: the history of the EVSE takes it, its status stays.
        var late = StatusPayload(POIRuntimeStatusKind.Status, evseId, "charging");
        late.Add(POIStatusUpdate.PropertyKeys.Timestamp, Timestamp.Now.AddMinutes(-5).ToISO8601());

        await gef.Show("setStatus", late);

        // Nobody else sets it: the app at OCC's server, and a wrong token.
        await using (var anonymous = await LiveClient.Connect(occURL))
            Console.WriteLine($"The app sets the status at OCC's server: {(await anonymous.Request("setStatus", StatusPayload(POIRuntimeStatusKind.Status, evseId, "outOfService"))).ErrorDescription}");

        try
        {
            await using var wrong = await LiveClient.Connect(occURL, "wrong");
        }
        catch (WebSocketException e)
        {
            Console.WriteLine($"Another token: {e.Message}");
        }

        // The status changed four times, the atlas not once.
        var headAfter = (await app.Request("head")).Payload!;

        Console.WriteLine($"The head after them: revision {headAfter["revision"]}, the same commit: {headAfter["@id"]!.Value<String>() == headBefore["@id"]!.Value<String>()}");
        Console.WriteLine();

        #endregion

        #region 4. A change of the atlas, pushed as well

        // DE*GEF lowers its DC tariff by a change set signed by its ML-DSA key for its tariffs, sent over the same WebSocket;
        // OCC commits it, signs the commit, and the commit reaches the mirror and the app.
        if (!A11.History.Head.RoamingNetwork.TryGetChargingStationOperatorById(ExampleA2.gefId, out var gefOperator))
            Helpers.Fail($"QA has no charging station operator '{ExampleA2.gefId}'");

        var tariffsKey  = A11.GEF.SigningKeys[POIKeyUsages.Tariffs];
        var lower       = A11.History.Head.RoamingNetwork.CreateChangeSet(
                              [ RoamingNetworkChange.UpdateProperty(gefOperator.ChargingTariffs.Single(tariff => tariff.Id == ExampleA8.DC), "elements", null, new JArray(new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.65m, StepSize: 1) ]).ToJSON())) ],
                              Description: ImmutableI18NString.Create(Languages.en, "The DC charging tariff: 0.65 €/kWh")
                          ).Sign(tariffsKey.Private, POIPublicKey.From(tariffsKey.Private).Id, tariffsKey.Algorithm);

        var committed   = await gef.Request("commitChangeSet", new JObject(new JProperty(POI_API.PropertyKeys.ChangeSet, lower.ToJSON(Embedded: true))));

        Console.WriteLine($"DE*GEF commits '{lower.Description.FirstText()}': {committed.Payload?["status"]}, revision {committed.Payload?["revision"]}");

        await app.ShowPush("commit");

        if (!await Eventually(() => mirrored.Head.Commit.Id == A11.History.Head.Commit.Id))
            Helpers.Fail("The mirror did not take the commit!");

        Console.WriteLine($"The mirror: revision {mirrored.Head.Commit.Revision}, the head of OCC: {mirrored.Head.Commit.Id == A11.History.Head.Commit.Id}");

        File.WriteAllText(Live, new JObject(new JProperty("headBefore",  headBefore),
                                            new JProperty("headAfter",   headAfter),
                                            new JProperty("pushes",      new JArray(app.All.Select(push => push.ToJSON())))).ToString());

        Console.WriteLine();

        #endregion

        #region 5. Subscriptions end with the connection

        // The app is away for a while, e.g. in a tunnel; meanwhile the next driver plugs in. Connected again, it is told
        // nothing until it subscribes, and nothing of what happened meanwhile: it asks for the status now.
        await app.DisposeAsync();

        await gef.Request("setStatus", StatusPayload(POIRuntimeStatusKind.Status, evseId, "charging"));

        await using var again = await LiveClient.Connect(mirrorURL);

        await Task.Delay(500);

        Console.WriteLine($"Connected again: {again.All.Count} pushes without subscribing");

        foreach (var status in StatusOf(await again.Request("runtimeStatus"), evseId).Where(status => status.Kind == POIRuntimeStatusKind.Status))
            Console.WriteLine($"Asked for: {evseId} {POIStatusUpdate.KindText(status.Kind)} {status.Value} since {status.Timestamp.ToISO8601()}");

        // What the mirror serves now: the head with the runtime status, as everybody may read it.
        File.WriteAllText("published/QA-live-snapshot.json", (await again.Request("snapshot")).Payload!.ToString());

        Console.WriteLine();

        #endregion

    }


    /// <summary>
    /// Checks from what the app received and what the mirror serves: the status is runtime data, unsigned and outside every
    /// version; the commit pushed with it is signed and checked against the trust anchor like every other.
    /// </summary>
    public static void Validate()
    {

        #region 6. The traffic report and the atlas

        // a) What was pushed: status updates without signatures, and the commit with them; the head unchanged by the status.
        var live      = Helpers.ReadJSON(Live);
        var pushes    = live["pushes"]!.Cast<JArray>().Select(json => POIWebSocketMessage.TryParse(json.ToString(Formatting.None), out var push, out var error) ? push! : throw new Exception(error?.ToString())).ToArray();
        var updates   = pushes.Where(push => push.Action != "commit").ToArray();

        Console.WriteLine($"The app got {updates.Length} status updates, {String.Join(", ", updates.GroupBy(push => push.Action).Select(group => $"{group.Count()} {group.Key}"))}, none signed: {updates.All(push => push.Payload!["signatures"] is null)}");
        Console.WriteLine($"The head before and after them: revision {live["headBefore"]!["revision"]} and {live["headAfter"]!["revision"]}, the same commit: {live["headBefore"]!["@id"]!.Value<String>() == live["headAfter"]!["@id"]!.Value<String>()}");

        // b) The commit pushed: signed by the quorum of OCC, checked against the trust anchor of epoch 2, the head of the mirror.
        if (!RoamingNetworkTrustAnchor.TryParse(File.ReadAllText("published/QA-trustAnchor-mldsa.txt"), out var anchor, out var error))
            Helpers.Fail(error);

        var snapshot  = Helpers.ReadJSON("published/QA-live-snapshot.json");

        if (!RoamingNetworkHistory.TryFromSnapshot(snapshot, out var mirrored, out error))
            Helpers.Fail(error);

        if (!RoamingNetworkCommit.TryParse(pushes.Single(push => push.Action == "commit").Payload!, out var commit, out error))
            Helpers.Fail(error);

        var verified  = commit.Signatures.Where(signature => ExampleA4.SigningKeyOf(anchor, mirrored.Head.RoamingNetwork, signature) is POIPublicKey key &&
                                                             commit.VerifySignature(signature, key.Key, key.Id, out _)).ToArray();

        Console.WriteLine($"The commit pushed: revision {commit.Revision}, '{commit.ChangeSet?.Description.FirstText()}', signed by DE*GEF with {commit.ChangeSet?.Signatures.Single().Algorithm}; {verified.Length} of its {commit.Signatures.Length} signatures by signing keys of {qaOperator(mirrored)}, {String.Join(", ", verified.Select(signature => signature.Algorithm).Distinct())}, checked against the trust anchor of epoch 2; the head of the mirror: {commit.Id == mirrored.Head.Commit.Id}");

        // c) The runtime status the mirror serves next to its head, outside the version: the version of revision 3 is the
        //    same with it and without it, and no checkpoint holds any.
        snapshot.Remove("runtimeStatus");

        if (!RoamingNetworkHistory.TryFromSnapshot(snapshot, out var withoutStatus, out error))
            Helpers.Fail(error);

        var evseId    = ExampleA10.EVSEId.ToString();
        var current   = mirrored.RuntimeStatus.All.Single(status => status.Kind == POIRuntimeStatusKind.Status && status.EntityId == evseId);

        Console.WriteLine($"The live snapshot: {evseId} {current.Value} since {current.Timestamp.ToISO8601()}; without its runtime status the same version: {withoutStatus.Head.Commit.Id == mirrored.Head.Commit.Id}");
        Console.WriteLine($"Checkpoint 0 of epoch 2 holds runtime status: {Helpers.ReadJSON("published/QA-epoch-2-checkpoint-0-snapshot.json")["runtimeStatus"] is not null}");

        #endregion

        static String qaOperator(RoamingNetworkHistory History)
            => History.Head.RoamingNetwork.Operator!.Id.ToString();

    }


    #region Helpers

    // The payload of a setStatus request.
    private static JObject StatusPayload(POIRuntimeStatusKind Kind, String EVSEId, String Value)

        => new (new JProperty(POIStatusUpdate.PropertyKeys.Kind,        POIStatusUpdate.KindText(Kind)),
                new JProperty(POIStatusUpdate.PropertyKeys.EntityType,  nameof(EVSE)),
                new JProperty(POIStatusUpdate.PropertyKeys.EntityId,    EVSEId),
                new JProperty(POIStatusUpdate.PropertyKeys.Value,       POIStatusUpdate.ValueToJSON(Kind, Value)));

    // The runtime status of the given entity, from the answer to runtimeStatus.
    private static IEnumerable<POIStatusUpdate> StatusOf(POIWebSocketMessage RuntimeStatus, String EntityId)

        => RuntimeStatus.Payload!["runtimeStatus"]!.
               Cast<JObject>().
               Select(json => POIStatusUpdate.TryParse(json, out var status, out _) ? status : null).
               Where (status => status?.EntityId == EntityId).
               Cast<POIStatusUpdate>();

    // Whether the given condition holds within five seconds.
    private static async Task<Boolean> Eventually(Func<Boolean> Condition)
    {

        for (var i = 0; i < 100 && !Condition(); i++)
            await Task.Delay(50);

        return Condition();

    }

    #endregion

    #region (class) LiveClient

    /// <summary>
    /// A client of the WebSocket of a history, as an app or the backend of an operator has it: requests are answered by
    /// their identifications, pushes are collected.
    /// </summary>
    private sealed class LiveClient : IAsyncDisposable
    {

        private readonly ClientWebSocket                                                webSocket  = new ();
        private readonly Dictionary<String, TaskCompletionSource<POIWebSocketMessage>>  pending    = [];
        private readonly Channel<POIWebSocketMessage>                                   pushes     = Channel.CreateUnbounded<POIWebSocketMessage>();
        private          Task?                                                          receiver;
        private          Int32                                                          nextId;

        /// <summary>
        /// All pushes received, in their order.
        /// </summary>
        public List<POIWebSocketMessage> All { get; } = [];

        public static async Task<LiveClient> Connect(Uri URL, String? Token = null)
        {

            var client = new LiveClient();

            if (Token is not null)
                client.webSocket.Options.SetRequestHeader("Authorization", "Bearer " + Token);

            await client.webSocket.ConnectAsync(URL, new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

            client.receiver = client.Receive();

            return client;

        }

        private async Task Receive()
        {

            var buffer = new Byte[64 * 1024];

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {

                    using var message = new MemoryStream();
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                        message.Write(buffer, 0, result.Count);
                    }
                    while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close)
                        return;

                    if (!POIWebSocketMessage.TryParse(Encoding.UTF8.GetString(message.ToArray()), out var received, out _))
                        continue;

                    if (received.MessageType == POIWebSocketMessageType.Send)
                    {
                        lock (All)
                            All.Add(received);
                        pushes.Writer.TryWrite(received);
                        continue;
                    }

                    TaskCompletionSource<POIWebSocketMessage>? waiting;

                    lock (pending)
                        pending.Remove(received.MessageId, out waiting);

                    waiting?.TrySetResult(received);

                }
            }
            catch (WebSocketException)
            { }

        }

        // Send a request and return its answer.
        public async Task<POIWebSocketMessage> Request(String Action, JObject? Payload = null)
            => await Send(POIWebSocketMessage.Request((++nextId).ToString(), Action, Payload));

        // Send a request and show it and its answer, as the frames on the wire.
        public async Task Show(String Action, JObject Payload)
        {

            var request = POIWebSocketMessage.Request((++nextId).ToString(), Action, Payload);

            Console.WriteLine($"→ {request.ToJSONText()}");
            Console.WriteLine($"← {(await Send(request)).ToJSONText()}");

        }

        // Wait for the push of the given action, of the given entity, and show it.
        public async Task ShowPush(String Action, String? EntityId = null)
        {

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            while (true)
            {

                var push = await pushes.Reader.ReadAsync(timeout.Token);

                if (push.Action == Action && (EntityId is null || push.Payload?[POIStatusUpdate.PropertyKeys.EntityId]?.Value<String>() == EntityId))
                {
                    Console.WriteLine($"⇐ {Shortened(push)}");
                    return;
                }

            }

        }

        private async Task<POIWebSocketMessage> Send(POIWebSocketMessage Request)
        {

            var waiting = new TaskCompletionSource<POIWebSocketMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

            lock (pending)
                pending[Request.MessageId] = waiting;

            await webSocket.SendAsync(Encoding.UTF8.GetBytes(Request.ToJSONText()), WebSocketMessageType.Text, true, CancellationToken.None);

            return await waiting.Task.WaitAsync(TimeSpan.FromSeconds(10));

        }

        // A push as text, a commit without its change set, as it is long.
        private static String Shortened(POIWebSocketMessage Push)
        {

            if (Push.Action != "commit")
                return Push.ToJSONText();

            var json = Push.ToJSON();
            json[3]!["changeSet"] = "…";
            json[3]!["signatures"] = $"… {Push.Payload!["signatures"]!.Count()} signatures";

            return json.ToString(Formatting.None);

        }

        public async ValueTask DisposeAsync()
        {

            try
            {
                if (webSocket.State == WebSocketState.Open)
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token);
            }
            catch
            { }

            webSocket.Dispose();

            if (receiver is not null)
                await receiver.WaitAsync(TimeSpan.FromSeconds(2)).ContinueWith(_ => { });

        }

    }

    #endregion

}
