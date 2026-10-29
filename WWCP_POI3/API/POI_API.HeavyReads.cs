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

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The reads that cost what they return, not a lookup: a snapshot, a page of commits or of their metadata, the
    /// snapshot of a checkpoint, a segment of the archive, and a view without parts not kept (SPECIFICATION.md, section
    /// 5.5). Each takes one of the heavy reads a client may make per minute, MaxHeavyReadsPerMinute, over HTTP and the
    /// WebSocket together. Beyond, HTTP answers 429 Too Many Requests with Retry-After, and the WebSocket waits for the
    /// next one, MaxHeavyReadWait at most, and answers GenericError where that is not enough.
    /// </summary>
    /// <remarks>
    /// The client is the address the request came from, an IPv6 one by its /64, which one host has to itself; behind a
    /// trusted proxy the one ClientAddressOf says.
    /// </remarks>
    public partial class POI_API
    {

        #region Data

        /// <summary>
        /// The default maximum number of heavy reads per client and minute: a server catching up with a million
        /// commits, MaxCommitsPerRequest of them per page, has them within two minutes.
        /// </summary>
        public const           Int32     DefaultMaxHeavyReadsPerMinute  = 600;

        /// <summary>
        /// The default maximum time a request over the WebSocket waits for its heavy read.
        /// </summary>
        public static readonly TimeSpan  DefaultMaxHeavyReadWait        = TimeSpan.FromSeconds(10);

        private volatile InMemoryTokenBucketRateLimiter?  heavyReads              = NewHeavyReadLimiter(DefaultMaxHeavyReadsPerMinute);
        private          Int32                            maxHeavyReadsPerMinute  = DefaultMaxHeavyReadsPerMinute;
        private          Int64                            heavyReadsRefused;

        #endregion

        #region Properties

        /// <summary>
        /// The maximum number of heavy reads a client may make per minute, all at once as well; 0 for no limit.
        /// Setting it starts every client anew.
        /// </summary>
        public Int32                             MaxHeavyReadsPerMinute
        {

            get
                => maxHeavyReadsPerMinute;

            set
            {
                maxHeavyReadsPerMinute  = value;
                heavyReads              = NewHeavyReadLimiter(value);
            }

        }

        /// <summary>
        /// The maximum time a request over the WebSocket waits for its heavy read, e.g. a page of commits a server
        /// catching up asks for; beyond, it is answered with GenericError.
        /// </summary>
        public TimeSpan                          MaxHeavyReadWait       { get; set; } = DefaultMaxHeavyReadWait;

        /// <summary>
        /// The address of the client a request comes from, where the TCP peer is not it: behind a reverse proxy that
        /// says so in a header of its own. Null, or a function that answers null, takes the TCP peer. Set it only
        /// behind a proxy that sets the header itself: anyone else can write one.
        /// </summary>
        public Func<HTTPRequest, IPAddress?>?    ClientAddressOf        { get; set; }

        /// <summary>
        /// The number of heavy reads refused, over HTTP with 429, over the WebSocket after waiting MaxHeavyReadWait.
        /// </summary>
        public Int64                             HeavyReadsRefused
            => Interlocked.Read(ref heavyReadsRefused);

        #endregion


        #region (private static) NewHeavyReadLimiter(MaxPerMinute)

        private static InMemoryTokenBucketRateLimiter? NewHeavyReadLimiter(Int32 MaxPerMinute)

            => MaxPerMinute > 0
                   ? new InMemoryTokenBucketRateLimiter(Capacity:        MaxPerMinute,
                                                        RefillPeriod:    TimeSpan.FromMinutes(1),
                                                        MaximumBuckets:  10_000,
                                                        BucketLifetime:  TimeSpan.FromMinutes(10))
                   : null;

        #endregion

        #region (private)        HeavyRead(Handler), HeavyCommand(Command)

        /// <summary>
        /// The given handler of a heavy read over HTTP, refusing the request with 429 Too Many Requests where its
        /// client has made all the heavy reads it may for now.
        /// </summary>
        private HTTPDelegate HeavyRead(HTTPDelegate Handler)

            => request => TooManyHeavyReads(request) is HTTPResponse refused
                              ? Task.FromResult(refused)
                              : Handler(request);


        /// <summary>
        /// The given command of a heavy read over the WebSocket, waiting for the next heavy read its client may make,
        /// MaxHeavyReadWait at most.
        /// </summary>
        private POIWebSocketCommandDelegate HeavyCommand(POIWebSocketCommandDelegate Command)

            => async request => await WaitForHeavyRead(request) ?? await Command(request);

        #endregion

        #region (private)        TooManyHeavyReads(Request)

        /// <summary>
        /// Take one of the heavy reads the client of the given request may make: null where there was one,
        /// otherwise 429 Too Many Requests with the seconds to wait, in Retry-After as well; readable from every
        /// origin, as what it refuses is.
        /// </summary>
        private HTTPResponse? TooManyHeavyReads(HTTPRequest Request)
        {

            if (heavyReads?.TryAcquire(ClientKeyOf(Request)) is not { Allowed: false } decision)
                return null;

            Interlocked.Increment(ref heavyReadsRefused);

            var retryAfter  = Math.Max(1, (Int32) Math.Ceiling(decision.RetryAfter.TotalSeconds));
            var json        = new JObject(new JProperty(PropertyKeys.Description, $"Too many heavy reads: try again in {retryAfter} seconds!"));

            var response    = new HTTPResponse.Builder(Request) {
                                  HTTPStatusCode            = HTTPStatusCode.TooManyRequests,
                                  ContentType               = WantsCBOR(Request)
                                                                  ? HTTPContentType.Application.CBOR
                                                                  : HTTPContentType.Application.JSON_UTF8,
                                  Content                   = WantsCBOR(Request)
                                                                  ? CBORJSON.ToCBOR(json).ToByteArray(CBORWriterOptions.Canonical)
                                                                  : Encoding.UTF8.GetBytes(POIJSON.ToText(json)),
                                  RetryAfter                = retryAfter.ToString(CultureInfo.InvariantCulture),
                                  CacheControl              = "no-store",
                                  Vary                      = "Accept",
                                  AccessControlAllowOrigin  = "*"
                              };

            // Not a CORS-safelisted response header: a script of another origin sees it only when exposed.
            response.Set("Access-Control-Expose-Headers", "Retry-After");

            return response.AsImmutable;

        }

        #endregion

        #region (private)        WaitForHeavyRead (Request)

        /// <summary>
        /// Wait for the next heavy read the client of the given request over the WebSocket may make: null once it may,
        /// or GenericError where it would wait longer than MaxHeavyReadWait.
        /// </summary>
        private async Task<POIWebSocketMessage?> WaitForHeavyRead(POIWebSocketRequest Request)
        {

            var limiter  = heavyReads;
            var client   = Request.Session.UpgradeRequest is HTTPRequest upgradeRequest
                               ? ClientKeyOf(upgradeRequest)
                               : ClientKeyOf(Request.Session.Connection.RemoteSocket.IPAddress.ToString());
            var waited   = TimeSpan.Zero;

            while (limiter?.TryAcquire(client) is { Allowed: false } decision)
            {

                if (waited + decision.RetryAfter > MaxHeavyReadWait)
                {
                    Interlocked.Increment(ref heavyReadsRefused);
                    return Request.Error(POIWebSocketErrorCodes.GenericError,
                                         $"Too many heavy reads: try again in {Math.Max(1, (Int32) Math.Ceiling(decision.RetryAfter.TotalSeconds))} seconds!");
                }

                await Task.Delay(decision.RetryAfter, Request.CancellationToken);

                waited += decision.RetryAfter;

            }

            return null;

        }

        #endregion

        #region (private)        ClientKeyOf      (Request)

        /// <summary>
        /// Whom a heavy read of the given request is counted against: its client's address, an IPv6 one by its /64.
        /// </summary>
        private String ClientKeyOf(HTTPRequest Request)

            => ClientAddressOf?.Invoke(Request) is IPAddress address
                   ? ClientKeyOf(address)
                   : ClientKeyOf(Request.RemoteSocket.IPAddress.ToString());


        private static String ClientKeyOf(String Address)

            => IPAddress.TryParse(Address, out var address)
                   ? ClientKeyOf(address)
                   : Address;


        private static String ClientKeyOf(IPAddress Address)
        {

            var address = Address.IsIPv4MappedToIPv6
                              ? Address.MapToIPv4()
                              : Address;

            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                var octets = address.GetAddressBytes();
                Array.Clear(octets, 8, 8);
                return new IPAddress(octets) + "/64";
            }

            return address.ToString();

        }

        #endregion

    }

}
