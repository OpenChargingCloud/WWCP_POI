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

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.loadtests
{

    /// <summary>
    /// A charging station of a load test: its number within its operator, and the number of its charging pool.
    /// </summary>
    internal readonly record struct LoadStation(Int32  StationNo,
                                                Int32  PoolNo);


    /// <summary>
    /// The changes of one change set, and what they do to the charging stations an operator knows it has, once the
    /// change set is committed.
    /// </summary>
    internal sealed record LoadChangePlan(IReadOnlyList<RoamingNetworkChange>  Changes,
                                          Action                               Apply);


    /// <summary>
    /// A charging station operator of a load test: its keys, and the charging stations it has. Only the one writer it
    /// belongs to changes them, and only once a change set of it is committed.
    /// </summary>
    internal sealed class LoadOperator
    {

        #region Properties

        public Int32                       Number            { get; }
        public ChargingStationOperator_Id  Id                { get; }
        public COSEKeyPair                 SigningKey        { get; }
        public String                      SigningKeyId      { get; }
        public COSEKeyPair                 IdentityKey       { get; }
        public PhoneNumber                 Hotline           { get; }

        /// <summary>
        /// The charging stations it has at the start, numbered 1 to InitialStations.
        /// </summary>
        public Int32                       InitialStations   { get; }

        /// <summary>
        /// The charging pools it has at the start, numbered 1 to InitialPools.
        /// </summary>
        public Int32                       InitialPools      { get; }

        /// <summary>
        /// The charging stations it has now.
        /// </summary>
        public List<LoadStation>           Stations          { get; }

        public Int32                       NextStationNo     { get; set; }
        public Int32                       NextPoolNo        { get; set; }

        #endregion

        #region Constructor(s)

        public LoadOperator(Int32  Number,
                            Int32  Stations,
                            Int32  StationsPerPool)
        {

            this.Number           = Number;
            this.Id               = ChargingStationOperator_Id.Parse($"DE*{Base36(Number)}");
            this.SigningKey       = COSEKeyPair.NewES256();
            this.SigningKeyId     = POIPublicKey.From(SigningKey.Private).Id;
            this.IdentityKey      = COSEKeyPair.NewES512();
            this.Hotline          = PhoneNumber.Parse($"+49 800 {100000 + Number}");
            this.InitialStations  = Stations;
            this.InitialPools     = (Stations + StationsPerPool - 1) / StationsPerPool;
            this.Stations         = new List<LoadStation>(Stations);

            for (var station = 1; station <= Stations; station++)
                this.Stations.Add(new LoadStation(station, (station - 1) / StationsPerPool + 1));

            NextStationNo         = Stations     + 1;
            NextPoolNo            = InitialPools + 1;

        }

        #endregion


        #region Identifications

        public ChargingPool_Id             PoolId                 (Int32 PoolNo)                     => ChargingPool_Id.           Parse($"{Id}*P{PoolNo}");
        public ChargingStation_Id          StationId              (Int32 StationNo)                  => ChargingStation_Id.        Parse($"{Id}*S{StationNo}");
        public EVSE_Id                     EVSEId                 (Int32 StationNo, Int32 EVSENo)    => EVSE_Id.                   Parse($"{Id}*E{StationNo}*{EVSENo}");
        public EVSETemplate_Id             EVSETemplateId                                            => EVSETemplate_Id.           Parse($"{Id}*ET1");
        public ChargingStationTemplate_Id  StationTemplateId                                         => ChargingStationTemplate_Id.Parse($"{Id}*ST1");
        public ChargingPoolTemplate_Id     PoolTemplateId                                            => ChargingPoolTemplate_Id.   Parse($"{Id}*PT1");

        /// <summary>
        /// Three characters of 0-9 and A-Z, as the identification of an operator has them: up to 46,655 operators.
        /// </summary>
        private static String Base36(Int32 Number)
        {

            const String digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            if (Number is < 1 or >= 36 * 36 * 36)
                throw new ArgumentOutOfRangeException(nameof(Number), "There are at most 46,655 charging station operators!");

            return new String([ digits[Number / 1296], digits[Number / 36 % 36], digits[Number % 36] ]);

        }

        #endregion

        #region Sign(ChangeSet)

        /// <summary>
        /// The given change set signed by this operator, as its owner.
        /// </summary>
        public RoamingNetworkChangeSet Sign(RoamingNetworkChangeSet ChangeSet)

            => ChangeSet.Sign(SigningKey.Private,
                              SigningKeyId,
                              SigningKey.Algorithm);

        #endregion

        public override String ToString()
            => $"{Id} ({InitialStations:N0} charging stations)";

    }


    /// <summary>
    /// The roaming network of a load test: its operator with its keys, the charging station operators with their keys,
    /// and the entities they have, made alike on every machine. Sizes of operators, charging stations of a charging pool,
    /// EVSEs of a charging station and the use of templates are those of the options; every fifth charging station is a DC
    /// one of 150 kW with CCS, the others AC ones of 22 kW with Type 2, each EVSE with one connector. Keys are new on
    /// every run, and of the same sizes.
    /// </summary>
    internal sealed class LoadNetwork
    {

        #region Data

        public  static readonly DateTimeOffset             Created     = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        public  static readonly RoamingNetwork_Id          NetworkId   = RoamingNetwork_Id.        Parse("LOADTEST");
        public  static readonly RoamingNetworkOperator_Id  OperatorId  = RoamingNetworkOperator_Id.Parse("RNO-LOADTEST");

        private static readonly String[]                   Streets     = [ "Hauptstraße", "Bahnhofstraße", "Schulstraße", "Gartenstraße", "Dorfstraße", "Bergstraße", "Lindenstraße", "Kirchstraße",
                                                                           "Waldstraße", "Ringstraße", "Mühlenweg", "Am Markt", "Industriestraße", "Parkstraße", "Feldweg", "Rathausplatz" ];
        private static readonly String[]                   Cities      = [ "Berlin", "Hamburg", "München", "Köln", "Frankfurt am Main", "Stuttgart", "Düsseldorf", "Leipzig",
                                                                           "Dortmund", "Essen", "Bremen", "Dresden", "Hannover", "Nürnberg", "Jena", "Erfurt" ];

        private readonly COSEKeyPair  rnoIdentityKey  = COSEKeyPair.NewES512();
        private readonly COSEKeyPair  rnoSigningKey   = COSEKeyPair.NewES256();
        private readonly Int64[]      cumulative;
        private readonly Int32[]      mix;
        private          Int64        nextReference;

        #endregion

        #region Properties

        public LoadTestOptions             Options        { get; }

        /// <summary>
        /// The charging station operators, the largest first.
        /// </summary>
        public IReadOnlyList<LoadOperator> Operators      { get; }

        public Int32                       Stations       { get; }

        /// <summary>
        /// What signs every commit and every attestation of a head, as the roaming network operator: the hub's role.
        /// </summary>
        public RoamingNetworkCommitSigner  CommitSigner   { get; }

        #endregion

        #region Constructor(s)

        public LoadNetwork(LoadTestOptions  Options,
                           Int32            Stations)
        {

            this.Options    = Options;
            this.Stations   = Stations;
            this.Operators  = [ .. Sizes(Stations, Options.Operators, Options.Zipf).Select((size, index) => new LoadOperator(index + 1, size, Options.StationsPerPool)) ];
            this.cumulative = CumulativeOf(Operators);
            this.mix        = [ .. Enum.GetValues<LoadTestChange>().SelectMany(kind => Enumerable.Repeat((Int32) kind, Options.Mix.GetValueOrDefault(kind))) ];

            this.CommitSigner = new RoamingNetworkCommitSigner(rnoSigningKey.Private,
                                                               POIPublicKey.From(rnoSigningKey.Private).Id,
                                                               rnoSigningKey.Algorithm,
                                                               RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator,
                                                               OperatorId.ToString());

        }

        #endregion


        #region (static) Sizes(Stations, Operators, Zipf)

        /// <summary>
        /// How many charging stations each operator has, the largest first: the i-th a share of 1/i^Zipf, at least one.
        /// </summary>
        public static Int32[] Sizes(Int32   Stations,
                                    Int32   Operators,
                                    Double  Zipf)
        {

            var operators  = Math.Min(Operators, Stations);
            var weights    = Enumerable.Range(1, operators).Select(i => 1 / Math.Pow(i, Zipf)).ToArray();
            var total      = weights.Sum();
            var sizes      = weights.Select(weight => Math.Max(1, (Int32) Math.Floor(Stations * weight / total))).ToArray();

            // What rounding left, given to the largest ones, one each, or taken from them.
            for (var i = 0; sizes.Sum() != Stations; i = (i + 1) % operators)
            {
                if (sizes.Sum() < Stations)
                    sizes[i]++;
                else if (sizes[i] > 1)
                    sizes[i]--;
            }

            return sizes;

        }

        #endregion

        #region (static) CumulativeOf(Operators) / Pick(Operators, Cumulative, Random)

        /// <summary>
        /// The running total of the charging stations of the given operators at the start.
        /// </summary>
        public static Int64[] CumulativeOf(IReadOnlyList<LoadOperator> Operators)
        {

            var cumulative  = new Int64[Operators.Count];
            var sum         = 0L;

            for (var i = 0; i < Operators.Count; i++)
                cumulative[i] = sum += Operators[i].InitialStations;

            return cumulative;

        }

        /// <summary>
        /// One of the given operators, by its share of their charging stations at the start: a charging station picked
        /// at random belongs to it.
        /// </summary>
        public static LoadOperator Pick(IReadOnlyList<LoadOperator>  Operators,
                                        Int64[]                      Cumulative,
                                        Random                       Random)
        {

            var station  = Random.NextInt64(Cumulative[^1]);
            var index    = Array.BinarySearch(Cumulative, station + 1);

            return Operators[index >= 0 ? index : ~index];

        }

        /// <summary>
        /// An operator, by its share of all charging stations at the start.
        /// </summary>
        public LoadOperator Pick(Random Random)
            => Pick(Operators, cumulative, Random);

        #endregion


        #region BuildOperator(Operator)

        /// <summary>
        /// The given operator with its keys, its charging pools, charging stations and EVSEs at the start, and its
        /// templates, if they are used.
        /// </summary>
        public ChargingStationOperator BuildOperator(LoadOperator Operator)
        {

            var pools = new List<ChargingPool>(Operator.InitialPools);

            for (var poolNo = 1; poolNo <= Operator.InitialPools; poolNo++)
            {

                var first  = (poolNo - 1) * Options.StationsPerPool + 1;
                var last   = Math.Min(poolNo * Options.StationsPerPool, Operator.InitialStations);

                pools.Add(NewPool(Operator, poolNo, Enumerable.Range(first, last - first + 1).Select(stationNo => NewStation(Operator, stationNo))));

            }

            var templates = Options.Templates;

            return new ChargingStationOperator(
                       Operator.Id,
                       Name:                      I18NString.Create(Languages.de, $"Ladenetz {Operator.Number}"),
                       HotlinePhoneNumber:        Operator.Hotline,
                       ChargingPools:             pools,
                       EVSETemplates:             templates ? [ new EVSETemplate           (Operator.EVSETemplateId,    MaxPower:            Watt.FromW(22000),    Created: Created) ] : null,
                       ChargingStationTemplates:  templates ? [ new ChargingStationTemplate(Operator.StationTemplateId, HotlinePhoneNumber:  Operator.Hotline,     Created: Created) ] : null,
                       ChargingPoolTemplates:     templates ? [ new ChargingPoolTemplate   (Operator.PoolTemplateId,    Facilities:          [ Facility.CAFE ],    Created: Created) ] : null,
                       IdentityKeys:              [ POIPublicKey.From(Operator.IdentityKey.Private) ],
                       SigningKeys:               [ POIPublicKey.From(Operator.SigningKey. Private) ],
                       Created:                   Created
                   );

        }

        #endregion

        #region BuildNetwork(Operators)

        /// <summary>
        /// The roaming network of the given operators, operated by the roaming network operator of the load test, whose
        /// signing key is certified by its identity key.
        /// </summary>
        public RoamingNetwork BuildNetwork(IEnumerable<ChargingStationOperator> Operators)
        {

            var networkOperator = new RoamingNetworkOperator(
                                      OperatorId,
                                      I18NString.Create(Languages.en, "WWCP POI load test"),
                                      IdentityKeys:            [ POIPublicKey.From(rnoIdentityKey.Private) ],
                                      SigningKeyCertificates:  [ RoamingNetworkSigningKeyCertificate.Create(rnoIdentityKey,
                                                                                                            OperatorId,
                                                                                                            POIPublicKey.From(rnoSigningKey.Private),
                                                                                                            Created) ],
                                      Created:                 Created
                                  );

            return networkOperator.CreateRoamingNetwork(
                       NetworkId,
                       Name:                      ImmutableI18NString.Create("WWCP POI load test"),
                       Created:                   Created,
                       ChargingStationOperators:  Operators
                   );

        }

        #endregion

        #region NewPool(Operator, PoolNo, Stations)

        /// <summary>
        /// A charging pool of the given operator: a name, an address and a location somewhere in Germany, the same on
        /// every machine.
        /// </summary>
        public ChargingPool NewPool(LoadOperator                  Operator,
                                    Int32                         PoolNo,
                                    IEnumerable<ChargingStation>  Stations)
        {

            var seed = ((UInt64) Operator.Number << 32) | (UInt32) PoolNo;

            return new ChargingPool(
                       Operator.PoolId(PoolNo),
                       Name:              I18NString.Create(Languages.de, $"Ladepark {Cities[(Int32) (Mix64(seed) % (UInt64) Cities.Length)]} {PoolNo}"),
                       Address:           new Address(Streets[(Int32) (Mix64(seed + 1) % (UInt64) Streets.Length)],
                                                      $"{1067 + (Int32) (Mix64(seed + 2) % 98000):D5}",
                                                      I18NString.Create(Languages.de, Cities[(Int32) (Mix64(seed) % (UInt64) Cities.Length)]),
                                                      Country.Germany,
                                                      HouseNumber: $"{1 + Mix64(seed + 3) % 199}"),
                       GeoLocation:       GeoCoordinate.Parse(Math.Round(47.3 + 7.7 * Unit(seed + 4), 6),
                                                              Math.Round( 5.9 + 9.1 * Unit(seed + 5), 6)),
                       ChargingStations:  Stations,
                       Facilities:        Options.Templates ? null : [ Facility.CAFE ],
                       TemplateIds:       Options.Templates ? [ Operator.PoolTemplateId ] : null,
                       Created:           Created
                   );

        }

        #endregion

        #region NewStation(Operator, StationNo)

        /// <summary>
        /// A charging station of the given operator with its EVSEs: every fifth a DC one.
        /// </summary>
        public ChargingStation NewStation(LoadOperator  Operator,
                                          Int32         StationNo)
        {

            var dc     = StationNo % 5 == 0;
            var evses  = new List<EVSE>(Options.EVSEsPerStation);

            for (var evseNo = 1; evseNo <= Options.EVSEsPerStation; evseNo++)
                evses.Add(new EVSE(Operator.EVSEId(StationNo, evseNo),
                                   MaxPower:            dc ? Watt.FromW(150000) : Options.Templates ? null : Watt.FromW(22000),
                                   ChargingConnectors:  [ new ChargingConnector(ChargingConnector_Id.Parse("1"),
                                                                                dc ? ChargingConnectorType.IEC_62196_T2_COMBO : ChargingConnectorType.IEC_62196_T2) ],
                                   TemplateIds:         Options.Templates ? [ Operator.EVSETemplateId ] : null,
                                   Created:             Created));

            return new ChargingStation(
                       Operator.StationId(StationNo),
                       EVSEs:               evses,
                       HotlinePhoneNumber:  Options.Templates ? null : Operator.Hotline,
                       TemplateIds:         Options.Templates ? [ Operator.StationTemplateId ] : null,
                       Created:             Created
                   );

        }

        #endregion


        #region Plan(Operator, Random, Count)

        /// <summary>
        /// The given number of changes of the given operator, by the shares of the mix: a change of a property of one of
        /// its EVSEs to a value it never had, a new charging station in one of its charging pools, the removal of one of
        /// its charging stations, the last one excepted, or a new charging pool.
        /// </summary>
        public LoadChangePlan Plan(LoadOperator  Operator,
                                   Random        Random,
                                   Int32         Count)
        {

            var changes  = new List<RoamingNetworkChange>(Count);
            var added    = new List<LoadStation>();
            var removed  = new List<LoadStation>();

            LoadStation AnyStation()
            {
                LoadStation station;
                do station = Operator.Stations[Random.Next(Operator.Stations.Count)]; while (removed.Contains(station));
                return station;
            }

            for (var i = 0; i < Count; i++)
            {

                switch ((LoadTestChange) mix[Random.Next(mix.Length)])
                {

                    case LoadTestChange.AddStation:
                        {
                            var poolNo     = 1 + Random.Next(Operator.NextPoolNo - 1);
                            var stationNo  = Operator.NextStationNo++;
                            changes.Add(RoamingNetworkChange.Add(NewStation(Operator, stationNo), Operator.PoolId(poolNo)));
                            added.  Add(new LoadStation(stationNo, poolNo));
                        }
                        break;

                    case LoadTestChange.RemoveStation when Operator.Stations.Count - removed.Count > 1:
                        {
                            var station = AnyStation();
                            changes.Add(RoamingNetworkChange.Remove(Operator.StationId(station.StationNo)));
                            removed.Add(station);
                        }
                        break;

                    case LoadTestChange.AddPool:
                        {
                            var poolNo     = Operator.NextPoolNo++;
                            var stations   = Enumerable.Range(0, Options.StationsPerPool).Select(_ => Operator.NextStationNo++).ToArray();
                            changes.Add     (RoamingNetworkChange.Add(NewPool(Operator, poolNo, stations.Select(stationNo => NewStation(Operator, stationNo)))));
                            added.  AddRange(stations.Select(stationNo => new LoadStation(stationNo, poolNo)));
                        }
                        break;

                    // An EVSE, also instead of removing the last charging station.
                    default:
                        {
                            var station = AnyStation();
                            changes.Add(RoamingNetworkChange.UpdateProperty(nameof(EVSE),
                                                                            Operator.EVSEId(station.StationNo, 1 + Random.Next(Options.EVSEsPerStation)).ToString(),
                                                                            EVSE.PropertyKeys.PhysicalReference,
                                                                            null,
                                                                            $"LT-{Interlocked.Increment(ref nextReference)}"));
                        }
                        break;

                }

            }

            return new LoadChangePlan(changes,
                                      () => {
                                          foreach (var station in removed)
                                              Operator.Stations.Remove(station);
                                          Operator.Stations.AddRange(added);
                                      });

        }

        #endregion


        #region (private, static) Mix64(Seed) / Unit(Seed)

        /// <summary>
        /// SplitMix64: a number that looks random, the same for the same seed on every machine.
        /// </summary>
        private static UInt64 Mix64(UInt64 Seed)
        {
            var x = Seed + 0x9E3779B97F4A7C15UL;
            x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
            x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
            return x ^ (x >> 31);
        }

        /// <summary>
        /// A number from 0 to 1, the same for the same seed on every machine.
        /// </summary>
        private static Double Unit(UInt64 Seed)
            => (Mix64(Seed) >> 11) * (1.0 / (1UL << 53));

        #endregion

    }

}
