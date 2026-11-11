/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 *
 * Licensed under the Affero GPL license, Version 3.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.gnu.org/licenses/agpl.html
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#region Usings

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The EV Taler offers of a POI server (concepts/EVTaler.md, section 6), for the operators its backend serves: the
    /// application hosting the server provides the backend, which talks to the operator's merchant instance and to its
    /// charging station management system. Elsewhere the endpoints answer 404, and an app goes to the offersURL of the
    /// operator's evTaler.
    /// </summary>
    /// <remarks>
    /// POST ~/RNs/{RN_Id}/EVSEs/{EVSE_Id}/evTaler/sessions
    ///      { "exchange": "&lt;masterPublicKey&gt;", "step": { "quantity": "5", "unit": "kWh" }, "cap": "30.00" }
    ///      → 201 { "sessionId", "secret", "events": "&lt;path&gt;", "offer": { … } }
    ///      → 404 where the EVSE offers no EV Taler, or no sessions are made here for its operator
    ///      → 409 when the EVSE takes no session now (no vehicle plugged in, one charging already)
    ///      → 422 when the step or the exchange is not accepted, with the step sizes that are
    ///      → 429 when the client has too many sessions open, or has made all the heavy reads it may for now: proving the
    ///            station takes one, after the limits above
    ///      → 502 when an offer of step 1 failed a check of this server, or the systems of the operator failed: what
    ///            failed there is reported with OnError, not told to the client
    ///
    /// GET  ~/RNs/{RN_Id}/evTaler/sessions/{sessionId}/events?after={seq}&amp;timeout_ms={ms}
    ///      Authorization: Bearer &lt;secret&gt;
    ///      → { "events": [ { "seq": 4, "type": "offer", … }, … ], "ended": false }   waits for the next one (long polling)
    ///
    /// POST ~/RNs/{RN_Id}/evTaler/sessions/{sessionId}/stop
    ///      Authorization: Bearer &lt;secret&gt;
    ///      → 202
    ///
    /// Events: offer, paid, unpaid, refused (an offer of the backend failed a check and was not relayed: the session
    /// ends), meterValue, stopped, settlement. Sessions are kept in memory: a restart of the server ends them for it.
    /// </remarks>
    public sealed class EVTalerOffersAPI
    {

        #region Data

        /// <summary>
        /// The longest a read of the events waits for the next one, by default.
        /// </summary>
        public static readonly TimeSpan  DefaultMaxEventWait            = TimeSpan.FromSeconds(60);

        /// <summary>
        /// How long the events of a settled session are kept, by default: as long as a wallet picks up its refund by itself
        /// after the settlement.
        /// </summary>
        public static readonly TimeSpan  DefaultKeepSettledSessions     = TimeSpan.FromDays(7);

        /// <summary>
        /// How long the events of a session settled without any step paid are kept, by default: long enough for its app to
        /// read how it ended, as there is no refund to pick up.
        /// </summary>
        public static readonly TimeSpan  DefaultKeepUnpaidSessions      = TimeSpan.FromMinutes(10);

        /// <summary>
        /// How many sessions a client may have open at a time, by default.
        /// </summary>
        public const           Int32     DefaultMaxOpenSessionsPerClient  = 4;

        /// <summary>
        /// How many sessions are held at most, by default: beyond, the settled ones are forgotten, those settled first
        /// first.
        /// </summary>
        public const           Int32     DefaultMaxSessions             = 100_000;

        /// <summary>
        /// How often the sessions no longer kept are forgotten, by default.
        /// </summary>
        public static readonly TimeSpan  DefaultPurgeInterval           = TimeSpan.FromSeconds(10);

        /// <summary>
        /// What a client is told where the systems of the operator failed to start a session: what failed is reported with
        /// OnError only.
        /// </summary>
        public const           String    NotStarted                     = "The session is not started: the systems of the operator failed. Try again later!";

        private readonly Dictionary<RoamingNetwork_Id, RoamingNetworkTrustAnchor>  trustAnchors;
        private readonly ConcurrentDictionary<String,  EVTalerOfferSession>  sessions        = new (StringComparer.Ordinal);
        private readonly ConcurrentDictionary<EVSE_Id, EVTalerOfferSession>  sessionAtEVSE   = new ();
        private readonly Dictionary<String, Int32>                           openPerClient   = new (StringComparer.Ordinal);
        private readonly Lock                                                startLock       = new ();
        private          Int64                                               nextPurge;

        #endregion

        #region Properties

        /// <summary>
        /// The POI API it serves its endpoints with, and whose histories it checks offers against.
        /// </summary>
        public POI_API                     POIAPI                      { get; }

        /// <summary>
        /// The backend of the operators it makes offers for.
        /// </summary>
        public IEVTalerBackend             Backend                     { get; }

        /// <summary>
        /// The trust anchors of the roaming networks it makes offers in: the station of an EVSE is proven as an app proves
        /// it, against the identity keys of its roaming network operator.
        /// </summary>
        public IEnumerable<RoamingNetworkTrustAnchor> TrustAnchors
            => trustAnchors.Values;

        /// <summary>
        /// A transparency software verifying signed meter values: from step 2 on, an offer names the signed meter value at
        /// the start, which is checked with a measurement key of the station proof before the offer is relayed.
        /// </summary>
        public EVTalerMeterValueVerifier?  MeterValueVerifier          { get; }

        /// <summary>
        /// The time provider.
        /// </summary>
        public TimeProvider                TimeProvider                { get; }

        /// <summary>
        /// The longest a read of the events waits for the next one.
        /// </summary>
        public TimeSpan                    MaxEventWait                { get; set; } = DefaultMaxEventWait;

        /// <summary>
        /// How long the events of a settled session are kept, when a step of it was paid.
        /// </summary>
        public TimeSpan                    KeepSettledSessions         { get; set; } = DefaultKeepSettledSessions;

        /// <summary>
        /// How long the events of a session settled without any step paid are kept.
        /// </summary>
        public TimeSpan                    KeepUnpaidSessions          { get; set; } = DefaultKeepUnpaidSessions;

        /// <summary>
        /// How many sessions a client may have open at a time, not settled yet: the client as the POI API counts its heavy
        /// reads, by its address, an IPv6 one by its /64, behind a trusted proxy the one its ClientAddressOf says.
        /// </summary>
        public Int32                       MaxOpenSessionsPerClient    { get; set; } = DefaultMaxOpenSessionsPerClient;

        /// <summary>
        /// How many sessions are held at most: beyond, the settled ones are forgotten, those settled first first. Open
        /// sessions are never forgotten; there are no more of them than EVSEs.
        /// </summary>
        public Int32                       MaxSessions                 { get; set; } = DefaultMaxSessions;

        /// <summary>
        /// How often the sessions no longer kept are forgotten.
        /// </summary>
        public TimeSpan                    PurgeInterval               { get; set; } = DefaultPurgeInterval;

        /// <summary>
        /// The sessions it holds.
        /// </summary>
        public IEnumerable<EVTalerOfferSession> Sessions
            => sessions.Values;

        #endregion

        #region Events

        /// <summary>
        /// A session was not started because the backend, the merchant backend or the charging station management system
        /// failed: the session, and what failed, which the client is not told.
        /// </summary>
        public event Action<String, String>?   OnError;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Serve the EV Taler offers of the given backend with the given POI API.
        /// </summary>
        /// <param name="POIAPI">The POI API: its histories prove the stations and price the sessions.</param>
        /// <param name="Backend">The backend of the operators it makes offers for.</param>
        /// <param name="TrustAnchors">The trust anchors of the roaming networks it makes offers in, as an app pins them.</param>
        /// <param name="MeterValueVerifier">A transparency software verifying signed meter values, for the offers from step 2 on.</param>
        /// <param name="TimeProvider">The time provider, the one of the POI API by default.</param>
        public EVTalerOffersAPI(POI_API                                 POIAPI,
                                IEVTalerBackend                         Backend,
                                IEnumerable<RoamingNetworkTrustAnchor>  TrustAnchors,
                                EVTalerMeterValueVerifier?              MeterValueVerifier   = null,
                                TimeProvider?                           TimeProvider         = null)
        {

            this.trustAnchors        = TrustAnchors.ToDictionary(trustAnchor => trustAnchor.Id);
            this.POIAPI              = POIAPI;
            this.Backend             = Backend;
            this.MeterValueVerifier  = MeterValueVerifier;
            this.TimeProvider        = TimeProvider ?? POIAPI.TimeProvider;

            RegisterURLTemplates();

        }

        #endregion


        #region (private) RegisterURLTemplates()

        private void RegisterURLTemplates()
        {

            var startPath     = HTTPPath.Root + "RNs/{roamingNetworkId}/EVSEs/{id}/evTaler/sessions";
            var sessionPath   = HTTPPath.Root + "RNs/{roamingNetworkId}/evTaler/sessions/{sessionId}";

            #region POST ~/RNs/{RN_Id}/EVSEs/{EVSE_Id}/evTaler/sessions

            AddPreflight(startPath, HTTPMethod.POST);

            POIAPI.AddHandler(HTTPMethod.POST,
                              startPath,
                              StartSession);

            #endregion

            #region GET  ~/RNs/{RN_Id}/evTaler/sessions/{sessionId}/events

            AddPreflight(sessionPath + "/events", HTTPMethod.GET);

            POIAPI.AddHandler(HTTPMethod.GET,
                              sessionPath + "/events",
                              ReadEvents);

            #endregion

            #region POST ~/RNs/{RN_Id}/evTaler/sessions/{sessionId}/stop

            AddPreflight(sessionPath + "/stop", HTTPMethod.POST);

            POIAPI.AddHandler(HTTPMethod.POST,
                              sessionPath + "/stop",
                              StopSession);

            #endregion

        }

        #endregion


        #region (private) StartSession(Request)

        private async Task<HTTPResponse> StartSession(HTTPRequest Request)
        {

            Purge();

            #region The request

            if (!String.Equals(Request.ContentType?.MediaType, HTTPContentType.Application.JSON_UTF8.MediaType, StringComparison.OrdinalIgnoreCase))
                return Error(Request, HTTPStatusCode.UnsupportedMediaType, "A session is asked for with an application/json body!");

            if (!RoamingNetwork_Id.TryParse(Request.ParsedURLParameters.ElementAtOrDefault(0) ?? "", out var roamingNetworkId) ||
                !POIAPI.TryGetRoamingNetworkHistory(roamingNetworkId, out var history))
                return Error(Request, HTTPStatusCode.NotFound, "The roaming network is unknown!");

            if (!EVSE_Id.TryParse(Request.ParsedURLParameters.ElementAtOrDefault(1) ?? "", out var evseId))
                return Error(Request, HTTPStatusCode.BadRequest, "Invalid EVSE identification!");

            JObject? body;

            try
            {
                body = Request.HTTPBody is { Length: > 0 } bytes
                           ? POIJSON.Read(Encoding.UTF8.GetString(bytes)) as JObject
                           : null;
            }
            catch (Exception)
            {
                body = null;
            }

            if (body is null)
                return Error(Request, HTTPStatusCode.BadRequest, "A session is asked for with a JSON object!");

            if (!EVTalerSessionRequest.TryParse(body, roamingNetworkId, evseId, out var sessionRequest, out var requestError))
                return Error(Request, HTTPStatusCode.BadRequest, requestError);

            #endregion

            #region The EVSE, its EV Taler tariff, its operator and its exchanges, as the version held gives them

            var network = history.Head.RoamingNetwork;

            if (!network.TryGetChargingStationOperatorById(evseId.OperatorId, out var chargingStationOperator) ||
                !chargingStationOperator.TryGetEVTalerTariff(evseId, out var chargingTariff))
                return Error(Request, HTTPStatusCode.NotFound, $"The EVSE '{evseId}' offers no EV Taler!");

            if (!Backend.Serves(roamingNetworkId, chargingStationOperator.Id))
                return Error(Request, HTTPStatusCode.NotFound, $"No EV Taler sessions are made here for the operator '{chargingStationOperator.Id}': ask the offersURL of its evTaler!");

            var exchanges = chargingStationOperator.EVTalerExchangesAt(evseId).ToArray();

            if (!exchanges.Any(exchange => exchange.MasterPublicKey == sessionRequest.Exchange))
                return Error(Request,
                             HTTPStatusCode.UnprocessableEntity,
                             $"The exchange '{sessionRequest.Exchange}' is not one the operator '{chargingStationOperator.Id}' accepts in '{chargingTariff.Currency!.ISOCode}'!",
                             new JProperty("exchanges", new JArray(exchanges.Select(exchange => exchange.MasterPublicKey))));

            #endregion

            #region The limits, before the proof: a few sessions per client, one at an EVSE

            if (!trustAnchors.TryGetValue(roamingNetworkId, out var trustAnchor))
                return Error(Request, HTTPStatusCode.ServiceUnavailable, $"No trust anchor of the roaming network '{roamingNetworkId}' is pinned here: its stations are not proven!");

            var client = POIAPI.ClientKeyOf(Request);

            lock (startLock)
            {
                if (RefusedByLimits(Request, client, evseId) is HTTPResponse refused)
                    return refused;
            }

            // Proving the station checks its signatures: the start takes one of the heavy reads its client may make.
            if (POIAPI.TooManyHeavyReads(Request) is HTTPResponse tooManyHeavyReads)
                return tooManyHeavyReads;

            #endregion

            #region The proof of the station and the current price statements

            var now = TimeProvider.GetUtcNow();

            if (!history.TryGetChargingStationProof(evseId, out var proof, out var proofError, null, now) ||
                !VerifiedChargingStation.TryVerify(trustAnchor, proof, out var station, out proofError, EVSEId: evseId.ToString(), At: now))
                return Error(Request, HTTPStatusCode.ServiceUnavailable, $"The charging station of the EVSE '{evseId}' is not proven here now: {proofError}");

            var priceStatements = history.PriceStatementsOf(evseId);

            #endregion

            #region The session, held where the limits still let it be

            var session = new EVTalerOfferSession(this,
                                                  NewRandom(16),
                                                  NewRandom(32, out var secret),
                                                  sessionRequest,
                                                  station,
                                                  priceStatements,
                                                  client,
                                                  now);

            lock (startLock)
            {

                if (RefusedByLimits(Request, client, evseId) is HTTPResponse refused)
                    return refused;

                if (!sessionAtEVSE.TryAdd(evseId, session))
                    return Error(Request, HTTPStatusCode.Conflict, $"The EVSE '{evseId}' has an EV Taler session already!");

                sessions[session.SessionId]  = session;
                openPerClient[client]        = openPerClient.GetValueOrDefault(client) + 1;

            }

            #endregion

            #region The backend makes step 1

            EVTalerStartResult result;

            try
            {
                result = await Backend.StartSession(new EVTalerSessionContext(session.SessionId,
                                                                              sessionRequest,
                                                                              station,
                                                                              priceStatements,
                                                                              now,
                                                                              session),
                                                    CancellationToken.None);
            }
            catch (Exception e)
            {
                result = EVTalerStartResult.Failed($"The backend failed: {e.Message}");
            }

            var (events, _) = session.EventsAfter(0);
            var offer       = events.FirstOrDefault(json => json["type"]?.Value<String>() == "offer");

            if (!result.Started || offer is null)
            {

                Forget(session);

                if (result.Started)
                    await Backend.StopSession(session.SessionId, CancellationToken.None);

                // The client is told what its driver may act on: a step or an exchange not accepted (422), the EVSE taking
                // no session now (409), and an offer of step 1 failing a check of this server, in this server's words. What
                // failed in the systems of the operator is reported with OnError, not told to the client.
                var refusal     = events.FirstOrDefault(json => json["type"]?.Value<String>() == "refused");

                var statusCode  = result.Started
                                      ? HTTPStatusCode.BadGateway
                                      : result.StatusCode switch {
                                            409 => HTTPStatusCode.Conflict,
                                            422 => HTTPStatusCode.UnprocessableEntity,
                                            _   => HTTPStatusCode.BadGateway
                                        };

                var description = statusCode != HTTPStatusCode.BadGateway
                                      ? result.Description ?? "The session is not started!"
                                      : refusal is not null
                                            ? $"The offer of step 1 was refused: {refusal["description"]?.Value<String>()}"
                                            : NotStarted;

                if (statusCode == HTTPStatusCode.BadGateway)
                    Report(session.SessionId,
                           result.Started
                               ? "The backend started the session without offering step 1!"
                               : result.Description ?? "The session is not started!");

                return result.AcceptedSteps.Count > 0
                           ? Error(Request, statusCode, description, new JProperty("acceptedSteps", new JArray(result.AcceptedSteps.Select(step => step.ToJSON()))))
                           : Error(Request, statusCode, description);

            }

            offer.Remove("seq");
            offer.Remove("type");

            return JSONResponse(Request,
                                HTTPStatusCode.Created,
                                new JObject(new JProperty("sessionId",  session.SessionId),
                                            new JProperty("secret",     secret),
                                            new JProperty("events",     EventsPath(roamingNetworkId, session.SessionId)),
                                            new JProperty("offer",      offer)));

            #endregion

        }

        #endregion

        #region (private) ReadEvents(Request)

        private async Task<HTTPResponse> ReadEvents(HTTPRequest Request)
        {

            Purge();

            if (!TryGetSession(Request, out var session, out var errorResponse))
                return errorResponse;

            var after    = Request.QueryString.GetInt64("after") is Int64 seq && seq > 0 ? (UInt64) seq : 0UL;
            var waitMs   = Request.QueryString.GetInt64("timeout_ms") is Int64 ms && ms > 0 ? ms : 0L;
            var wait     = TimeSpan.FromMilliseconds(Math.Min(waitMs, (Int64) MaxEventWait.TotalMilliseconds));

            var (events, next) = session.EventsAfter(after);

            if (events.Count == 0 && wait > TimeSpan.Zero && !session.SettledAt.HasValue)
            {
                await Task.WhenAny(next, Task.Delay(wait, TimeProvider));
                (events, _) = session.EventsAfter(after);
            }

            return JSONResponse(Request,
                                HTTPStatusCode.OK,
                                new JObject(new JProperty("events",  new JArray(events)),
                                            new JProperty("ended",   session.SettledAt.HasValue)));

        }

        #endregion

        #region (private) StopSession(Request)

        private async Task<HTTPResponse> StopSession(HTTPRequest Request)
        {

            if (!TryGetSession(Request, out var session, out var errorResponse))
                return errorResponse;

            if (!session.StoppedAt.HasValue)
                await Backend.StopSession(session.SessionId, CancellationToken.None);

            return JSONResponse(Request, HTTPStatusCode.Accepted, new JObject());

        }

        #endregion


        #region (internal) EndRefused(Session, Step)

        /// <summary>
        /// An offer of the backend failed a check: the session ends.
        /// </summary>
        /// <param name="Session">The session.</param>
        /// <param name="Step">The step refused: from step 2 on charging is stopped; a refused step 1 is answered to the
        /// backend's start, which then ends the session itself.</param>
        internal Task EndRefused(EVTalerOfferSession  Session,
                                 UInt32               Step)

            => Step > 1 && sessions.ContainsKey(Session.SessionId)
                   ? Backend.StopSession(Session.SessionId, CancellationToken.None)
                   : Task.CompletedTask;

        #endregion

        #region (internal) Release(Session)

        /// <summary>
        /// Charging of the session ended: its EVSE takes another session.
        /// </summary>
        internal void Release(EVTalerOfferSession Session)

            => sessionAtEVSE.TryRemove(new KeyValuePair<EVSE_Id, EVTalerOfferSession>(Session.Request.EVSEId, Session));

        #endregion

        #region (internal) Settled(Session)

        /// <summary>
        /// The session was settled: its EVSE takes another session, and it no longer counts against its client.
        /// </summary>
        internal void Settled(EVTalerOfferSession Session)
        {
            Release(Session);
            Close(Session);
        }

        #endregion

        #region (private) Forget(Session)

        /// <summary>
        /// The session was not started: it is not held, its EVSE takes another session, and it does not count against its
        /// client.
        /// </summary>
        private void Forget(EVTalerOfferSession Session)
        {
            sessions.TryRemove(new KeyValuePair<String, EVTalerOfferSession>(Session.SessionId, Session));
            Release(Session);
            Close(Session);
        }

        #endregion

        #region (private) Close(Session)

        /// <summary>
        /// The session no longer counts against its client, once; it forgets the client.
        /// </summary>
        private void Close(EVTalerOfferSession Session)
        {

            if (Session.Close() is not String client)
                return;

            lock (startLock)
            {
                if (openPerClient.TryGetValue(client, out var open))
                {
                    if (open > 1)
                        openPerClient[client] = open - 1;
                    else
                        openPerClient.Remove(client);
                }
            }

        }

        #endregion

        #region (private) RefusedByLimits(Request, Client, EVSEId)

        /// <summary>
        /// Whether the limits refuse a new session of the given client at the given EVSE: 429 when the client has as many
        /// sessions open as it may, 409 when the EVSE has one. Called under the start lock.
        /// </summary>
        private HTTPResponse? RefusedByLimits(HTTPRequest  Request,
                                              String       Client,
                                              EVSE_Id      EVSEId)
        {

            if (openPerClient.GetValueOrDefault(Client) >= MaxOpenSessionsPerClient)
                return Error(Request, HTTPStatusCode.TooManyRequests, "Too many EV Taler sessions of this client are open!");

            if (sessionAtEVSE.ContainsKey(EVSEId))
                return Error(Request, HTTPStatusCode.Conflict, $"The EVSE '{EVSEId}' has an EV Taler session already!");

            return null;

        }

        #endregion

        #region (private) Purge()

        /// <summary>
        /// Forget the sessions no longer kept, every PurgeInterval at most: those settled longer ago than KeepSettledSessions,
        /// or than KeepUnpaidSessions without any step paid; and beyond MaxSessions the settled ones, those settled first
        /// first.
        /// </summary>
        private void Purge()
        {

            var now   = TimeProvider.GetUtcNow();
            var next  = Interlocked.Read(ref nextPurge);

            if (now.UtcTicks < next ||
                Interlocked.CompareExchange(ref nextPurge, (now + PurgeInterval).UtcTicks, next) != next)
                return;

            var settledBefore  = now - KeepSettledSessions;
            var unpaidBefore   = now - KeepUnpaidSessions;

            foreach (var session in sessions.Values)
                if (session.SettledAt is DateTimeOffset settledAt &&
                    settledAt < (session.AnyStepPaid ? settledBefore : unpaidBefore))
                    sessions.TryRemove(new KeyValuePair<String, EVTalerOfferSession>(session.SessionId, session));

            var beyond = sessions.Count - MaxSessions;

            if (beyond > 0)
                foreach (var session in sessions.Values.Where  (session => session.SettledAt.HasValue).
                                                       OrderBy(session => session.SettledAt).
                                                       Take   (beyond).
                                                       ToArray())
                    sessions.TryRemove(new KeyValuePair<String, EVTalerOfferSession>(session.SessionId, session));

        }

        #endregion

        #region (private) Report(SessionId, Description)

        private void Report(String  SessionId,
                            String  Description)
        {
            try
            {
                OnError?.Invoke(SessionId, Description);
            }
            catch
            { }
        }

        #endregion

        #region (private) TryGetSession(Request, out Session, out ErrorResponse)

        /// <summary>
        /// The session of the request, read with its secret: Authorization: Bearer &lt;secret&gt;. An unknown session and a
        /// wrong secret are answered alike.
        /// </summary>
        private Boolean TryGetSession(HTTPRequest                                                   Request,
                                      [System.Diagnostics.CodeAnalysis.NotNullWhen(true)]  out EVTalerOfferSession?  Session,
                                      [System.Diagnostics.CodeAnalysis.NotNullWhen(false)] out HTTPResponse?         ErrorResponse)
        {

            Session        = null;
            ErrorResponse  = null;

            var secret = Request.Authorization is HTTPBearerAuthentication bearer
                             ? bearer.Token
                             : null;

            if (secret is null)
            {
                ErrorResponse = Unauthorized(Request);
                return false;
            }

            if (!RoamingNetwork_Id.TryParse(Request.ParsedURLParameters.ElementAtOrDefault(0) ?? "", out var roamingNetworkId) ||
                !sessions.TryGetValue(Request.ParsedURLParameters.ElementAtOrDefault(1) ?? "", out var session) ||
                session.Request.RoamingNetworkId != roamingNetworkId ||
                !session.IsSecret(secret))
            {
                ErrorResponse = Unauthorized(Request);
                return false;
            }

            Session = session;
            return true;

        }

        #endregion


        #region (private) EventsPath(RoamingNetworkId, SessionId)

        private String EventsPath(RoamingNetwork_Id  RoamingNetworkId,
                                  String             SessionId)

            => $"{POIAPI.RootPath.ToString().TrimEnd('/')}/RNs/{RoamingNetworkId}/evTaler/sessions/{SessionId}/events";

        #endregion

        #region (private) AddPreflight(Path, Method)

        /// <summary>
        /// Answer OPTIONS of an endpoint of the offers for every origin: its method, with the headers Authorization and
        /// Content-Type. A session is read with the secret only its driver's app was given, sent as a header, never a
        /// cookie a browser would send by itself.
        /// </summary>
        private void AddPreflight(HTTPPath    Path,
                                  HTTPMethod  Method)

            => POIAPI.AddHandler(HTTPMethod.OPTIONS,
                                 Path,
                                 request => Task.FromResult(
                                                new HTTPResponse.Builder(request) {
                                                    HTTPStatusCode             = HTTPStatusCode.NoContent,
                                                    Allow                      = [ HTTPMethod.OPTIONS, Method ],
                                                    AccessControlAllowOrigin   = "*",
                                                    AccessControlAllowMethods  = [ HTTPMethod.OPTIONS, Method ],
                                                    AccessControlAllowHeaders  = [ "Authorization", "Content-Type" ],
                                                    AccessControlMaxAge        = 86400
                                                }.AsImmutable
                                            ));

        #endregion

        #region (private static) NewRandom(Bytes, out Text)

        private static String NewRandom(Int32 Bytes)
            => NewRandom(Bytes, out _);

        private static String NewRandom(Int32       Bytes,
                                        out String  Text)
        {
            Text = Convert.ToBase64String(RandomNumberGenerator.GetBytes(Bytes)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return Text;
        }

        #endregion

        #region (private static) JSONResponse(Request, StatusCode, JSON)

        private static HTTPResponse JSONResponse(HTTPRequest     Request,
                                                 HTTPStatusCode  StatusCode,
                                                 JObject         JSON)

            => new HTTPResponse.Builder(Request) {
                   HTTPStatusCode            = StatusCode,
                   ContentType               = HTTPContentType.Application.JSON_UTF8,
                   Content                   = Encoding.UTF8.GetBytes(JSON.ToString(Formatting.None)),
                   CacheControl              = "no-store",
                   AccessControlAllowOrigin  = "*"
               }.AsImmutable;

        #endregion

        #region (private static) Error(Request, StatusCode, Description, params More)

        private static HTTPResponse Error(HTTPRequest         Request,
                                          HTTPStatusCode      StatusCode,
                                          String              Description,
                                          params JProperty[]  More)
        {

            var json = new JObject(new JProperty("description", Description));

            foreach (var property in More)
                json.Add(property);

            return JSONResponse(Request, StatusCode, json);

        }

        #endregion

        #region (private static) Unauthorized(Request)

        private static HTTPResponse Unauthorized(HTTPRequest Request)

            => new HTTPResponse.Builder(Request) {
                   HTTPStatusCode            = HTTPStatusCode.Unauthorized,
                   ContentType               = HTTPContentType.Application.JSON_UTF8,
                   Content                   = Encoding.UTF8.GetBytes(new JObject(new JProperty("description", "A session is read with its secret: Authorization: Bearer <secret>!")).ToString(Formatting.None)),
                   CacheControl              = "no-store",
                   AccessControlAllowOrigin  = "*"
               }.Set("WWW-Authenticate", "Bearer").AsImmutable;

        #endregion

    }

}
