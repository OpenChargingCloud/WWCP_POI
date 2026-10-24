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

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The canonical JSON and the deterministic CBOR content of the entities having child arrays, kept with the entities:
    /// they are immutable and shared by the versions that did not change them, so the JSON and CBOR ETags of a version
    /// write what it changed and the paths to the root, and hash the rest as it is kept (SPECIFICATION.md, section 8.7).
    /// </summary>
    /// <remarks>
    /// Canonical JSON and deterministic CBOR are made of the representations of the children: an entity with child arrays
    /// writes its JSON without them, a marker in the place of each child array having children, and its content is the
    /// pieces between the markers and, in their places, the arrays of the contents of its children, each as its parent
    /// embeds it (ToJSON(Embedded: true)). The CBOR content is the keys and values of the JSON content (CBORJSON).
    /// </remarks>
    internal static class POIContentRepresentations
    {

        #region Data

        /// <summary>
        /// The content of an entity in one format: its own bytes, and the contents of its children in their places.
        /// </summary>
        private sealed class Representation(Object[] Pieces)
        {
            /// <summary>
            /// Byte arrays and representations of children, in their order.
            /// </summary>
            public readonly Object[] Pieces = Pieces;
        }

        /// <summary>
        /// What is kept with an entity: its JSON and its CBOR content.
        /// </summary>
        private sealed class Entry
        {
            public Representation? JSON;
            public Representation? CBOR;
        }

        /// <summary>
        /// A child array of an entity: its name and the contents of its children.
        /// </summary>
        private readonly record struct ChildArray(String                 Name,
                                                  Func<Representation>[]  Children);

        private static readonly ConditionalWeakTable<Object, Entry> entries = new ();

        /// <summary>
        /// A random number of this process, in the markers of the child arrays.
        /// </summary>
        private static readonly String markerNonce     = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));

        private static readonly Byte[] jsonArrayStart  = "["u8.ToArray();
        private static readonly Byte[] jsonArrayEnd    = "]"u8.ToArray();
        private static readonly Byte[] jsonComma       = ","u8.ToArray();

        #endregion


        #region TryDigest(Entity, Format, HashAlgorithm, out Digest)

        /// <summary>
        /// The digest of the canonical JSON or the deterministic CBOR content of the given entity, if it is one having child
        /// arrays: of a roaming network, its parties, charging pools and charging stations.
        /// </summary>
        /// <param name="Entity">An entity.</param>
        /// <param name="Format">The format, JSON or CBOR.</param>
        /// <param name="HashAlgorithm">The hash algorithm.</param>
        /// <param name="Digest">The digest of its content.</param>
        public static Boolean TryDigest(Object                           Entity,
                                        ETagFormat                       Format,
                                        ETagHashAlgorithm                HashAlgorithm,
                                        [NotNullWhen(true)] out Byte[]?  Digest)
        {

            if (Format is not (ETagFormat.JSON or ETagFormat.CBOR) ||
                RepresentationOf(Entity, Format) is not Representation representation)
            {
                Digest = null;
                return false;
            }

            using var hash = IncrementalHash.CreateHash(HashAlgorithm switch {
                                                            ETagHashAlgorithm.SHA256  => HashAlgorithmName.SHA256,
                                                            ETagHashAlgorithm.SHA384  => HashAlgorithmName.SHA384,
                                                            ETagHashAlgorithm.SHA512  => HashAlgorithmName.SHA512,
                                                            _                         => throw new ArgumentOutOfRangeException(nameof(HashAlgorithm), "Unsupported ETag hash algorithm!")
                                                        });

            Append(hash, representation);

            Digest = hash.GetHashAndReset();
            return true;

        }

        #endregion

        #region ToBytes(Entity, Format)

        /// <summary>
        /// The canonical JSON or the deterministic CBOR content of the given entity as one byte array, if it is one having
        /// child arrays.
        /// </summary>
        /// <param name="Entity">An entity.</param>
        /// <param name="Format">The format, JSON or CBOR.</param>
        public static Byte[]? ToBytes(Object      Entity,
                                      ETagFormat  Format)
        {

            if (RepresentationOf(Entity, Format) is not Representation representation)
                return null;

            var bytes = new MemoryStream();
            Write(bytes, representation);
            return bytes.ToArray();

        }

        #endregion


        #region (private) RepresentationOf(Entity, Format)

        private static Representation? RepresentationOf(Object      Entity,
                                                        ETagFormat  Format)

            => Entity switch {
                   RoamingNetwork           roamingNetwork           => Of(roamingNetwork,           Format),
                   ChargingStationOperator  chargingStationOperator  => Of(chargingStationOperator,  Format),
                   EMobilityProvider        eMobilityProvider        => Of(eMobilityProvider,        Format),
                   GridOperator             gridOperator             => Of(gridOperator,             Format),
                   ParkingOperator          parkingOperator          => Of(parkingOperator,          Format),
                   TransparencySoftwareManufacturer  transparencySoftwareManufacturer  => Of(transparencySoftwareManufacturer, Format),
                   ConformityAssessmentBody          conformityAssessmentBody          => Of(conformityAssessmentBody,         Format),
                   ChargingPool             chargingPool             => Of(chargingPool,             Format),
                   ChargingStation          chargingStation          => Of(chargingStation,          Format),
                   _                                                 => null
               };

        #endregion

        #region (private) Of(RoamingNetwork,          Format)

        private static Representation Of(RoamingNetwork  RoamingNetwork,
                                         ETagFormat      Format)

            => Kept(RoamingNetwork, Format, () => Compose(

                   // As its content has it: ToJSON(IncludeETags: false), without its child arrays.
                   RoamingNetwork.ToJSON(Embedded:                                 false,
                                         ExpandRoamingNetworkIds:                  InfoStatus.ShowIdOnly,
                                         ExpandChargingStationOperatorIds:         InfoStatus.Expanded,
                                         ExpandChargingPoolIds:                    InfoStatus.ShowIdOnly,
                                         ExpandChargingStationIds:                 InfoStatus.ShowIdOnly,
                                         ExpandEVSEIds:                            InfoStatus.ShowIdOnly,
                                         ExpandBrandIds:                           InfoStatus.ShowIdOnly,
                                         ExpandDataLicenses:                       InfoStatus.Expanded,
                                         ExpandEMobilityProviderId:                InfoStatus.ShowIdOnly,
                                         CustomRoamingNetworkSerializer:           null,
                                         CustomChargingStationOperatorSerializer:  null,
                                         IncludeETags:                             false,
                                         ETagDigestEncoding:                       null,
                                         IncludeChildArrays:                       false),

                   [ Array(RoamingNetwork.PropertyKeys.ChargingStationOperators,          RoamingNetwork.ChargingStationOperators,         party => () => Of(party, Format)),
                     Array(RoamingNetwork.PropertyKeys.ChargingStationManufacturers,      RoamingNetwork.ChargingStationManufacturers,     value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)),
                     Array(RoamingNetwork.PropertyKeys.EMobilityProviders,                RoamingNetwork.EMobilityProviders,               party => () => Of(party, Format)),
                     Array(RoamingNetwork.PropertyKeys.GridOperators,                     RoamingNetwork.GridOperators,                    party => () => Of(party, Format)),
                     Array(RoamingNetwork.PropertyKeys.ParkingOperators,                  RoamingNetwork.ParkingOperators,                 party => () => Of(party, Format)),
                     Array(RoamingNetwork.PropertyKeys.EnergyMeterManufacturers,          RoamingNetwork.EnergyMeterManufacturers,         value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)),
                     Array(RoamingNetwork.PropertyKeys.TransparencySoftwareManufacturers, RoamingNetwork.TransparencySoftwareManufacturers, party => () => Of(party, Format)),
                     Array(RoamingNetwork.PropertyKeys.ConformityAssessmentBodies,        RoamingNetwork.ConformityAssessmentBodies,       party => () => Of(party, Format)) ],

                   Format));

        #endregion

        #region (private) Of(ChargingStationOperator, Format)

        private static Representation Of(ChargingStationOperator  ChargingStationOperator,
                                         ETagFormat               Format)

            => Kept(ChargingStationOperator, Format, () => Compose(

                   // As its roaming network embeds it, without its child arrays.
                   ChargingStationOperator.ToJSON(Embedded:                                 true,
                                                  ExpandChargingPoolIds:                    InfoStatus.ShowIdOnly,
                                                  ExpandChargingStationIds:                 InfoStatus.ShowIdOnly,
                                                  ExpandEVSEIds:                            InfoStatus.ShowIdOnly,
                                                  ExpandBrandIds:                           InfoStatus.ShowIdOnly,
                                                  ExpandDataLicenses:                       InfoStatus.Expanded,
                                                  CustomChargingStationOperatorSerializer:  null,
                                                  IncludeChildArrays:                       false),

                   [ Array(ChargingStationOperator.PropertyKeys.ChargingTariffs,           ChargingStationOperator.ChargingTariffs,           value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)),
                     Array(ChargingStationOperator.PropertyKeys.ChargingPools,             ChargingStationOperator.ChargingPools,             value => () => Of(value, Format)),
                     Array(ChargingStationOperator.PropertyKeys.EVSETemplates,             ChargingStationOperator.EVSETemplates,             value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)),
                     Array(ChargingStationOperator.PropertyKeys.ChargingStationTemplates,  ChargingStationOperator.ChargingStationTemplates,  value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)),
                     Array(ChargingStationOperator.PropertyKeys.ChargingPoolTemplates,     ChargingStationOperator.ChargingPoolTemplates,     value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)),
                     Array(ChargingStationOperator.PropertyKeys.ChargingTariffTemplates,   ChargingStationOperator.ChargingTariffTemplates,   value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)),
                     Array(ChargingStationOperator.PropertyKeys.EnergyMeterTemplates,      ChargingStationOperator.EnergyMeterTemplates,      value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)),
                     Array(ChargingStationOperator.PropertyKeys.EVSEGroups,                ChargingStationOperator.EVSEGroups,                value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)),
                     Array(ChargingStationOperator.PropertyKeys.ChargingStationGroups,     ChargingStationOperator.ChargingStationGroups,     value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)),
                     Array(ChargingStationOperator.PropertyKeys.ChargingPoolGroups,        ChargingStationOperator.ChargingPoolGroups,        value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)),
                     Array(ChargingStationOperator.PropertyKeys.ChargingTariffGroups,      ChargingStationOperator.ChargingTariffGroups,      value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)),
                     Array(ChargingStationOperator.PropertyKeys.EnergyMeterGroups,         ChargingStationOperator.EnergyMeterGroups,         value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)),
                     Array(ChargingStationOperator.PropertyKeys.ChargingTariffOffers,      ChargingStationOperator.ChargingTariffOffers,      value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)),
                     Array(ChargingStationOperator.PropertyKeys.ParkingSpaces,             ChargingStationOperator.ParkingSpaces,             value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)) ],

                   Format));

        #endregion

        #region (private) Of(EMobilityProvider,       Format)

        private static Representation Of(EMobilityProvider  EMobilityProvider,
                                         ETagFormat         Format)

            => Kept(EMobilityProvider, Format, () => Compose(

                   EMobilityProvider.ToJSON(Embedded: true, IncludeETags: false, ETagDigestEncoding: null, CustomEMobilityProviderSerializer: null, IncludeChildArrays: false),

                   [ Array(EMobilityProvider.PropertyKeys.EVSEGroups,             EMobilityProvider.EVSEGroups,             value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)),
                     Array(EMobilityProvider.PropertyKeys.ChargingStationGroups,  EMobilityProvider.ChargingStationGroups,  value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)),
                     Array(EMobilityProvider.PropertyKeys.ChargingPoolGroups,     EMobilityProvider.ChargingPoolGroups,     value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)),
                     Array(EMobilityProvider.PropertyKeys.ChargingTariffOffers,   EMobilityProvider.ChargingTariffOffers,   value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)) ],

                   Format));

        #endregion

        #region (private) Of(GridOperator,            Format)

        private static Representation Of(GridOperator  GridOperator,
                                         ETagFormat    Format)

            => Kept(GridOperator, Format, () => Compose(

                   GridOperator.ToJSON(IncludeChildArrays: false),

                   [ Array(GridOperator.PropertyKeys.GridConnectionPoints,  GridOperator.GridConnectionPoints,  value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)),
                     Array(GridOperator.PropertyKeys.EnergyMeterTemplates,  GridOperator.EnergyMeterTemplates,  value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)),
                     Array(GridOperator.PropertyKeys.EnergyMeterGroups,     GridOperator.EnergyMeterGroups,     value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)) ],

                   Format));

        #endregion

        #region (private) Of(TransparencySoftwareManufacturer, Format)

        private static Representation Of(TransparencySoftwareManufacturer  TransparencySoftwareManufacturer,
                                         ETagFormat                        Format)

            => Kept(TransparencySoftwareManufacturer, Format, () => Compose(

                   TransparencySoftwareManufacturer.ToJSON(Embedded: true, IncludeChildArrays: false),

                   [ Array(TransparencySoftwareManufacturer.PropertyKeys.TransparencySoftware,  TransparencySoftwareManufacturer.TransparencySoftware,  value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)) ],

                   Format));

        #endregion

        #region (private) Of(ConformityAssessmentBody, Format)

        private static Representation Of(ConformityAssessmentBody  ConformityAssessmentBody,
                                         ETagFormat                Format)

            => Kept(ConformityAssessmentBody, Format, () => Compose(

                   ConformityAssessmentBody.ToJSON(Embedded: true, IncludeChildArrays: false),

                   [ Array(ConformityAssessmentBody.PropertyKeys.TransparencySoftwareCertificates,  ConformityAssessmentBody.TransparencySoftwareCertificates,  value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)) ],

                   Format));

        #endregion

        #region (private) Of(ParkingOperator,         Format)

        private static Representation Of(ParkingOperator  ParkingOperator,
                                         ETagFormat       Format)

            => Kept(ParkingOperator, Format, () => Compose(

                   ParkingOperator.ToJSON(IncludeChildArrays: false),

                   [ Array(ParkingOperator.PropertyKeys.ParkingGarages,         ParkingOperator.ParkingGarages,         value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)),
                     Array(ParkingOperator.PropertyKeys.ParkingSpaces,          ParkingOperator.ParkingSpaces,          value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)),
                     Array(ParkingOperator.PropertyKeys.ParkingSensors,         ParkingOperator.ParkingSensors,         value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)),
                     Array(ParkingOperator.PropertyKeys.ParkingSpaceTemplates,  ParkingOperator.ParkingSpaceTemplates,  value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)),
                     Array(ParkingOperator.PropertyKeys.ParkingSpaceGroups,     ParkingOperator.ParkingSpaceGroups,     value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)),
                     Array(ParkingOperator.PropertyKeys.ParkingProducts,        ParkingOperator.ParkingProducts,        value => () => Leaf(value, () => value.ToJSON(Embedded: true), Format)) ],

                   Format));

        #endregion

        #region (private) Of(ChargingPool,            Format)

        private static Representation Of(ChargingPool  ChargingPool,
                                         ETagFormat    Format)

            => Kept(ChargingPool, Format, () => Compose(

                   ChargingPool.ToJSON(Embedded: true, IncludeChildArrays: false),

                   [ Array(ChargingPool.PropertyKeys.ChargingStations, ChargingPool.ChargingStations, value => () => Of(value, Format)) ],

                   Format));

        #endregion

        #region (private) Of(ChargingStation,         Format)

        /// <summary>
        /// A charging station is kept as one piece, with its EVSEs: a change of an EVSE writes the few of its station.
        /// </summary>
        private static Representation Of(ChargingStation  ChargingStation,
                                         ETagFormat       Format)

            => Leaf(ChargingStation, () => ChargingStation.ToJSON(Embedded: true), Format);

        #endregion


        #region (private) Array(Name, Children, RepresentationOf)

        private static ChildArray Array<T>(String                               Name,
                                           IEnumerable<T>                       Children,
                                           Func<T, Func<Representation>>        RepresentationOf)

            => new (Name, [ .. Children.Select(RepresentationOf) ]);

        #endregion

        #region (private) Leaf(Child, JSON, Format)

        /// <summary>
        /// The content of a child without child arrays: its JSON as its parent embeds it, kept with it.
        /// </summary>
        private static Representation Leaf(Object         Child,
                                           Func<JObject>  JSON,
                                           ETagFormat     Format)

            => Kept(Child, Format, () => new Representation([ Serialize(JSON(), Format) ]));

        #endregion

        #region (private) Compose(JSON, ChildArrays, Format)

        /// <summary>
        /// The content of an entity of the given JSON without its child arrays: those having children take their places,
        /// as the entity writes only those having children.
        /// </summary>
        private static Representation Compose(JObject                  JSON,
                                              IReadOnlyList<ChildArray>  ChildArrays,
                                              ETagFormat               Format)
        {

            var arrays   = ChildArrays.Where(childArray => childArray.Children.Length > 0).ToArray();
            var markers  = new Byte[arrays.Length][];

            for (var i = 0; i < arrays.Length; i++)
            {
                var marker  = $"\u0001wwcp-poi-child-array-{markerNonce}-{i}\u0001";
                JSON[arrays[i].Name] = marker;
                markers[i]  = Serialize(new JValue(marker), Format);
            }

            var bytes      = Serialize(JSON, Format);
            var positions  = new (Int32 Position, Int32 Array)[arrays.Length];

            for (var i = 0; i < arrays.Length; i++)
            {

                var position = bytes.AsSpan().IndexOf(markers[i]);

                // A marker is a text no content has: it names a random number of this process.
                if (position < 0 || bytes.AsSpan(position + 1).IndexOf(markers[i]) >= 0)
                    throw new InvalidOperationException($"The marker of the child array '{arrays[i].Name}' is not found exactly once in the content!");

                positions[i] = (position, i);

            }

            System.Array.Sort(positions);

            var pieces  = new List<Object>();
            var start   = 0;

            foreach (var (position, index) in positions)
            {

                pieces.Add(bytes[start..position]);

                var children = arrays[index].Children;

                if (Format == ETagFormat.JSON)
                {

                    pieces.Add(jsonArrayStart);

                    for (var i = 0; i < children.Length; i++)
                    {
                        if (i > 0)
                            pieces.Add(jsonComma);
                        pieces.Add(children[i]());
                    }

                    pieces.Add(jsonArrayEnd);

                }

                else
                {

                    pieces.Add(CBORArrayHead(children.Length));

                    foreach (var child in children)
                        pieces.Add(child());

                }

                start = position + markers[index].Length;

            }

            pieces.Add(bytes[start..]);

            return new Representation([ .. pieces ]);

        }

        #endregion

        #region (private) Serialize(JSON, Format)

        /// <summary>
        /// The canonical JSON, or the deterministic CBOR of the keys and values, of the given JSON.
        /// </summary>
        private static Byte[] Serialize(JToken      JSON,
                                        ETagFormat  Format)

            => Format == ETagFormat.JSON
                   ? CanonicalJSON.ToUTF8Bytes(JSON)
                   : CBORJSON.ToCBOR(JSON).ToByteArray(CBORWriterOptions.Canonical);

        #endregion

        #region (private) CBORArrayHead(Length)

        /// <summary>
        /// The head of a CBOR array of the given length, in its shortest form (RFC 8949, section 4.2.1).
        /// </summary>
        private static Byte[] CBORArrayHead(Int32 Length)
        {

            if (Length < 24)
                return [ (Byte) (0x80 | Length) ];

            if (Length <= Byte.MaxValue)
                return [ 0x98, (Byte) Length ];

            if (Length <= UInt16.MaxValue)
            {
                var head = new Byte[3];
                head[0] = 0x99;
                BinaryPrimitives.WriteUInt16BigEndian(head.AsSpan(1), (UInt16) Length);
                return head;
            }

            var longHead = new Byte[5];
            longHead[0] = 0x9A;
            BinaryPrimitives.WriteUInt32BigEndian(longHead.AsSpan(1), (UInt32) Length);
            return longHead;

        }

        #endregion

        #region (private) Append(Hash, Representation) / Write(Stream, Representation)

        private static void Append(IncrementalHash  Hash,
                                   Representation   Representation)
        {
            foreach (var piece in Representation.Pieces)
            {
                if (piece is Byte[] bytes)
                    Hash.AppendData(bytes);
                else
                    Append(Hash, (Representation) piece);
            }
        }

        private static void Write(Stream          Stream,
                                  Representation  Representation)
        {
            foreach (var piece in Representation.Pieces)
            {
                if (piece is Byte[] bytes)
                    Stream.Write(bytes);
                else
                    Write(Stream, (Representation) piece);
            }
        }

        #endregion

        #region (private) Kept(Entity, Format, Calculate)

        /// <summary>
        /// The content of the given entity, calculated once: concurrent callers may calculate it both, with the same result.
        /// </summary>
        private static Representation Kept(Object                Entity,
                                           ETagFormat            Format,
                                           Func<Representation>  Calculate)
        {

            var entry = entries.GetValue(Entity, _ => new Entry());

            if (Format == ETagFormat.JSON)
            {
                if (Volatile.Read(ref entry.JSON) is Representation json)
                    return json;
                var calculated = Calculate();
                Volatile.Write(ref entry.JSON, calculated);
                return calculated;
            }

            if (Volatile.Read(ref entry.CBOR) is Representation cbor)
                return cbor;

            var calculatedCBOR = Calculate();
            Volatile.Write(ref entry.CBOR, calculatedCBOR);
            return calculatedCBOR;

        }

        #endregion

    }

}
