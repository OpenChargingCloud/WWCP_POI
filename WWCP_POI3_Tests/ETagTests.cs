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

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.Complex
{

    [TestFixture]
    public class ETagTests
    {

        #region RoamingNetwork_JSON_RoundtripTest()

        [Test]
        public void RoamingNetwork_JSON_RoundtripTest()
        {

            var rn1 = new RoamingNetwork(
                          Id:           RoamingNetwork_Id.Parse("rn1"),
                          Name:         I18NString.Create(Languages.en, "Roaming Network 1"),
                          Description:  I18NString.Create(Languages.en, "My first roaming network!").
                                                      Set(Languages.de, "Mein erstes Roaming-Netzwerk!"),
                          DataSource:   "test"
                      );

            var j1  = rn1.ToJSON();
            var e1  = rn1.CalcChangeSetETags();
            var t1  = j1.ToString(Newtonsoft.Json.Formatting.None);

            var rn2 = RoamingNetwork.Parse(JObject.Parse(t1));
            var j2  = rn2.ToJSON();
            var e2  = rn2.CalcChangeSetETags();
            var t2  = j2.ToString(Newtonsoft.Json.Formatting.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rn2.Id,              Is.EqualTo(rn1.Id));
                Assert.That(rn2.Name,            Is.EqualTo(rn1.Name));
                Assert.That(rn2.Description,     Is.EqualTo(rn1.Description));
                Assert.That(rn2.DataSource,      Is.EqualTo(rn1.DataSource));
                Assert.That(e1.First().Digest,   Is.EqualTo(e2.First().Digest));
                Assert.That(t2,                  Is.EqualTo(t1));
            }

        }

        #endregion

        #region RoamingNetwork_CBOR_RoundtripTest()

        [Test]
        public void RoamingNetwork_CBOR_RoundtripTest()
        {

            var rn1 = new RoamingNetwork(
                          Id:           RoamingNetwork_Id.Parse("rn1"),
                          Name:         I18NString.Create(Languages.en, "Roaming Network 1"),
                          Description:  I18NString.Create(Languages.en, "My first roaming network!").
                                                      Set(Languages.de, "Mein erstes Roaming-Netzwerk!"),
                          DataSource:   "test"
                      );

            var c1  = rn1.ToCBOR();
            var e1  = new[] { rn1.ContentETag(ETagFormat.CBOR) };
            var b1  = c1.ToByteArray(CBORWriterOptions.Canonical);

            var rn2 = RoamingNetwork.Parse(CBORValue.Parse(b1));
            var c2  = rn2.ToCBOR();
            var e2  = new[] { rn2.ContentETag(ETagFormat.CBOR) };
            var b2  = c2.ToByteArray(CBORWriterOptions.Canonical);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rn2.Id,              Is.EqualTo(rn1.Id));
                Assert.That(rn2.Name,            Is.EqualTo(rn1.Name));
                Assert.That(rn2.Description,     Is.EqualTo(rn1.Description));
                Assert.That(rn2.DataSource,      Is.EqualTo(rn1.DataSource));
                Assert.That(e1.First().Digest,   Is.EqualTo(e2.First().Digest));
                Assert.That(b2,                  Is.EqualTo(b1));
            }

        }

        #endregion

        #region RoamingNetwork_serializes_its_ETags()

        [Test]
        public void RoamingNetwork_serializes_its_ETags()
        {

            var rn1       = new RoamingNetwork(
                                Id:           RoamingNetwork_Id.Parse("rn1"),
                                Name:         I18NString.Create(Languages.en, "Roaming Network 1"),
                                Description:  I18NString.Create(Languages.en, "My first roaming network!").
                                                            Set(Languages.de, "Mein erstes Roaming-Netzwerk!"),
                                DataSource:   "test"
                            );

            var expected  = new[] {
                                ETag.Compute(ETagFormat.JSON, CanonicalJSON.ToUTF8Bytes(rn1.ToJSON(IncludeETags: false))),
                                ETag.Compute(ETagFormat.CBOR, rn1.ToCBOR(IncludeETags: false).ToByteArray(CBORWriterOptions.Canonical)),
                                rn1.EffectiveTreeETag(ETagHashAlgorithm.SHA256)
                            };

            // ToJSON() and ToCBOR() write only stored ETags!
            // So they have to be calculated first!
            using (Assert.EnterMultipleScope())
            {
                Assert.That(rn1.ToJSON().ContainsKey("eTags"),                              Is.False);
                Assert.That(rn1.ToCBOR().TryGetValue(CBORValue.FromText("eTags"), out _),   Is.False);
                Assert.That(rn1.CalcETags(),                                                Is.EqualTo(expected));
            }

            var json      = rn1.ToJSON();
            var embedded  = rn1.ToJSON(Embedded: true);
            var cbor      = rn1.ToCBOR();

            Assert.That(cbor.TryGetValue(CBORValue.FromText("eTags"), out var cborETags), Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(json    ["eTags"]!.Select(ETag.Parse),    Is.EqualTo(expected));
                Assert.That(embedded["eTags"]!.Select(ETag.Parse),    Is.EqualTo(expected));
                Assert.That(cborETags.AsArray().Select(ETag.Parse),   Is.EqualTo(expected));
                Assert.That(rn1.ETags,                                Is.EqualTo(expected));
            }

        }

        #endregion

        #region Entities_have_the_ETags_of_their_content_as_their_parent_embeds_it()

        /// <summary>
        /// The ETags of every entity are those of its content as its parent embeds it: the ones a precondition names.
        /// </summary>
        [Test]
        public void Entities_have_the_ETags_of_their_content_as_their_parent_embeds_it()
        {

            var network   = Proofs.StationProofTests.NewNetwork();
            var gef       = network.GetChargingStationOperatorById(ChargingStationOperator_Id.Parse("DE*GEF"))!;
            var entities  = new List<(String Type, String Id, IHasContentETags Entity)> {
                                (nameof(ChargingStationOperator), gef.Id.ToString(), gef)
                            };

            foreach (var chargingPool in gef.ChargingPools)
            {

                entities.Add((nameof(ChargingPool), chargingPool.Id.ToString(), chargingPool));

                foreach (var chargingStation in chargingPool.ChargingStations)
                {

                    entities.Add((nameof(ChargingStation), chargingStation.Id.ToString(), chargingStation));

                    foreach (var evse in chargingStation.EVSEs)
                        entities.Add((nameof(EVSE), evse.Id.ToString(), evse));

                }

            }

            entities.AddRange(gef.    ChargingTariffs.             Select(entity => (nameof(ChargingTariff),              entity.Id.ToString(), (IHasContentETags) entity)));
            entities.AddRange(network.GridOperators.               Select(entity => (nameof(GridOperator),                entity.Id.ToString(), (IHasContentETags) entity)));
            entities.AddRange(network.ParkingOperators.            Select(entity => (nameof(ParkingOperator),             entity.Id.ToString(), (IHasContentETags) entity)));
            entities.AddRange(network.ChargingStationManufacturers.Select(entity => (nameof(ChargingStationManufacturer), entity.Id.ToString(), (IHasContentETags) entity)));

            using (Assert.EnterMultipleScope())
            {
                foreach (var (type, id, entity) in entities)
                {
                    foreach (var format in new[] { ETagFormat.JSON, ETagFormat.CBOR })
                    {
                        Assert.That(network.TryGetEntityETag(type, id, out var eTag, format),  Is.True,                         $"{type} '{id}'");
                        Assert.That(entity.ContentETag(format),                               Is.EqualTo(eTag),                $"{type} '{id}', {format}");
                    }
                    Assert.That(entity.ETags,  Is.EqualTo(new[] { entity.ContentETag(ETagFormat.JSON), entity.ContentETag(ETagFormat.CBOR) }),  $"{type} '{id}'");
                }
            }

        }

        #endregion

        #region An_entity_writes_and_checks_its_ETags()

        /// <summary>
        /// A representation of an entity of its own includes its ETags when asked to, in Base64 unless told otherwise; an
        /// embedded one never by default. Reading it checks them: stale ones are refused.
        /// </summary>
        [Test]
        public void An_entity_writes_and_checks_its_ETags()
        {

            var evse     = new EVSE(EVSE_Id.Parse("DE*GEF*E1*1"),
                                    MaxPower:  Watt.FromW(22000),
                                    Created:   DateTimeOffset.Parse("2026-01-01T00:00:00Z"));

            var json     = evse.ToJSON(IncludeETags: true);
            var hex      = evse.ToJSON(IncludeETags: true, ETagDigestEncoding: ETagDigestEncoding.HEX);

            var changed  = (JObject) json.DeepClone();
            changed["maxPower"] = 11000;

            var content  = evse.ToJSON(Embedded: true);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(evse.ToJSON().             ContainsKey("eTags"),  Is.False);
                Assert.That(evse.ToJSON(Embedded: true).ContainsKey("eTags"),  Is.False);

                Assert.That(evse.ETags,  Is.EqualTo(new[] {
                                                        ETag.Compute(ETagFormat.JSON, CanonicalJSON.ToUTF8Bytes(content)),
                                                        ETag.Compute(ETagFormat.CBOR, CBORJSON.ToCBOR(content).ToByteArray(CBORWriterOptions.Canonical))
                                                    }));

                Assert.That(json["eTags"]!.Select(ETag.Parse),          Is.EqualTo(evse.ETags));
                Assert.That(json["eTags"]![0]![2]!.Value<String>(),     Is.EqualTo("base64"));
                Assert.That(hex ["eTags"]![0]![2]!.Value<String>(),     Is.EqualTo("hex"));

                Assert.That(EVSE.TryParse(json,    out var parsed, out var error1),  Is.True,  error1);
                Assert.That(parsed!.ETags,                                          Is.EqualTo(evse.ETags));
                Assert.That(EVSE.TryParse(hex,     out _,          out var error2),  Is.True,  error2);
                Assert.That(EVSE.TryParse(changed, out _,          out var error3),  Is.False);
                Assert.That(error3,                                                 Does.Contain("does not match its content"));

            }

        }

        #endregion

        #region An_operator_checks_its_ETags_in_CBOR()

        /// <summary>
        /// The CBOR representation of a charging station operator includes its ETags when asked to, and reading it checks them.
        /// </summary>
        [Test]
        public void An_operator_checks_its_ETags_in_CBOR()
        {

            var gef      = Proofs.StationProofTests.NewNetwork().GetChargingStationOperatorById(ChargingStationOperator_Id.Parse("DE*GEF"))!;
            var cbor     = gef.ToCBOR(IncludeETags: true);
            var changed  = CBORValue.FromMap(cbor.AsMap().Append(new KeyValuePair<CBORValue, CBORValue>(CBORValue.FromText("dataSource"), CBORValue.FromText("changed"))));

            Assert.That(cbor.TryGetValue(CBORValue.FromText("eTags"), out var eTags),  Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(eTags.AsArray().Select(ETag.Parse),                                       Is.EqualTo(gef.ETags));
                Assert.That(gef.ToCBOR().TryGetValue(CBORValue.FromText("eTags"), out _),             Is.False);
                Assert.That(ChargingStationOperator.TryParse(cbor,    out var parsed, out var error1),  Is.True,  error1);
                Assert.That(parsed!.ETags,                                                            Is.EqualTo(gef.ETags));
                Assert.That(ChargingStationOperator.TryParse(changed, out _,          out var error2),  Is.False);
                Assert.That(error2,                                                                   Does.Contain("does not match its content"));
            }

        }

        #endregion

        #region RoamingNetwork_writes_its_ETags_in_Base64()

        [Test]
        public void RoamingNetwork_writes_its_ETags_in_Base64()
        {

            var rn1 = new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                                         Name: I18NString.Create(Languages.en, "Roaming Network 1"));

            rn1.CalcETags();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rn1.ToJSON()["eTags"]!.Select(eTag => eTag[2]!.Value<String>()),                                           Is.All.EqualTo("base64"));
                Assert.That(rn1.ToJSON(ETagDigestEncoding: ETagDigestEncoding.HEX)["eTags"]!.Select(eTag => eTag[2]!.Value<String>()),  Is.All.EqualTo("hex"));
                Assert.That(RoamingNetwork.Parse(rn1.ToJSON()).CalcETags(),                                                          Is.EqualTo(rn1.ETags));
            }

        }

        #endregion


        #region RoamingNetwork_ValidateETags()

        [Test]
        public void RoamingNetwork_ValidateETags()
        {

            var rn1   = new RoamingNetwork(
                            Id:           RoamingNetwork_Id.Parse("rn1"),
                            Name:         I18NString.Create(Languages.en, "Roaming Network 1"),
                            DataSource:   "test"
                        );

            var noETags    = rn1.ToJSON();
            rn1.CalcETags();

            var json       = rn1.ToJSON();
            var cbor       = rn1.ToCBOR();

            var changed    = (JObject) json.DeepClone();
            changed["dataSource"] = "changed";

            var wrongCBOR  = (JObject) json.DeepClone();
            ((JArray) wrongCBOR["eTags"]!)[1] = ETag.Compute(ETagFormat.CBOR, [ 1, 2, 3 ]).ToJSON();

            var twice      = (JObject) json.DeepClone();
            ((JArray) twice["eTags"]!).Add(((JArray) twice["eTags"]!)[0].DeepClone());

            var changedCBOR = CBORValue.FromMap(cbor.AsMap().Select(entry => entry.Key.TryGetText(out var key) && key == "dataSource"
                                                                                 ? new KeyValuePair<CBORValue, CBORValue>(entry.Key, CBORValue.FromText("changed"))
                                                                                 : entry));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(rn1.ValidateETags(json,        out var error1),   Is.True,  error1);
                Assert.That(rn1.ValidateETags(cbor,        out var error2),   Is.True,  error2);

                Assert.That(rn1.ValidateETags(noETags,     out var error3),   Is.False);
                Assert.That(error3,                                           Does.Contain("no ETags"));

                Assert.That(rn1.ValidateETags(changed,     out var error4),   Is.False);
                Assert.That(error4,                                           Does.Contain("JSON ETag does not match the content"));

                Assert.That(rn1.ValidateETags(changedCBOR, out var error5),   Is.False);
                Assert.That(error5,                                           Does.Contain("CBOR ETag does not match the content"));

                Assert.That(rn1.ValidateETags(wrongCBOR,   out var error6),   Is.False);
                Assert.That(error6,                                           Does.Contain("do not match its content: The CBOR ETag"));

                Assert.That(rn1.ValidateETags(twice,       out var error7),   Is.False);
                Assert.That(error7,                                           Does.Contain("must not be declared twice"));

            }

        }

        #endregion

        #region RoamingNetwork_TryParse_checks_its_ETags_and_its_context()

        /// <summary>
        /// Declared ETags must be those of the content read, a declared JSON-LD context the own one;
        /// both may be left out.
        /// </summary>
        [Test]
        public void RoamingNetwork_TryParse_checks_its_ETags_and_its_context()
        {

            var rn1          = new RoamingNetwork(
                                   Id:          RoamingNetwork_Id.Parse("rn1"),
                                   Name:        I18NString.Create(Languages.en, "Roaming Network 1"),
                                   DataSource:  "test"
                               );

            var noETags      = rn1.ToJSON();
            rn1.CalcETags();

            var json         = rn1.ToJSON();
            var cbor         = rn1.ToCBOR();

            var changed      = (JObject) json.DeepClone();
            changed["dataSource"] = "changed";

            var wrongCBOR    = (JObject) json.DeepClone();
            ((JArray) wrongCBOR["eTags"]!)[1] = ETag.Compute(ETagFormat.CBOR, [ 1, 2, 3 ]).ToJSON();

            var noArray      = (JObject) json.DeepClone();
            noArray["eTags"] = "none";

            var foreign      = (JObject) json.DeepClone();
            foreign["@context"] = "https://example.org/contexts/somethingElse";

            var noContext    = (JObject) noETags.DeepClone();
            noContext.Remove("@context");

            var changedCBOR  = CBORValue.FromMap(cbor.AsMap().Select(entry => entry.Key.TryGetText(out var key) && key == "dataSource"
                                                                                  ? new KeyValuePair<CBORValue, CBORValue>(entry.Key, CBORValue.FromText("changed"))
                                                                                  : entry));

            var foreignCBOR  = CBORValue.FromMap(cbor.AsMap().Select(entry => entry.Key.TryGetText(out var key) && key == "@context"
                                                                                  ? new KeyValuePair<CBORValue, CBORValue>(entry.Key, CBORValue.FromText("https://example.org/contexts/somethingElse"))
                                                                                  : entry));

            var operatorJSON = new ChargingStationOperator(ChargingStationOperator_Id.Parse("DE*GEF")).ToJSON();
            operatorJSON["@context"] = "https://example.org/contexts/somethingElse";

            using (Assert.EnterMultipleScope())
            {

                Assert.That(RoamingNetwork.TryParse(json,        out _, out var error1),   Is.True,  error1);
                Assert.That(RoamingNetwork.TryParse(cbor,        out _, out var error2),   Is.True,  error2);
                Assert.That(RoamingNetwork.TryParse(noETags,     out _, out var error3),   Is.True,  error3);
                Assert.That(RoamingNetwork.TryParse(noContext,   out _, out var error4),   Is.True,  error4);

                Assert.That(RoamingNetwork.TryParse(changed,     out _, out var error5),   Is.False);
                Assert.That(error5,                                                       Does.Contain("The ETags of the roaming network 'rn1' do not match its content: The JSON ETag"));

                Assert.That(RoamingNetwork.TryParse(changedCBOR, out _, out var error6),   Is.False);
                Assert.That(error6,                                                       Does.Contain("The ETags of the roaming network 'rn1' do not match its content"));

                Assert.That(RoamingNetwork.TryParse(wrongCBOR,   out _, out var error7),   Is.False);
                Assert.That(error7,                                                       Does.Contain("do not match its content: The CBOR ETag"));

                Assert.That(RoamingNetwork.TryParse(noArray,     out _, out var error8),   Is.False);
                Assert.That(error8,                                                       Does.Contain("No ETags were declared"));

                Assert.That(RoamingNetwork.TryParse(foreign,     out _, out var error9),   Is.False);
                Assert.That(error9,                                                       Does.Contain($"must be '{RoamingNetwork.JSONLDContext}'"));

                Assert.That(RoamingNetwork.TryParse(foreignCBOR, out _, out var error10),  Is.False);
                Assert.That(error10,                                                      Does.Contain($"must be '{RoamingNetwork.JSONLDContext}'"));

                Assert.That(ChargingStationOperator.TryParse(operatorJSON, out _, out var error11),  Is.False);
                Assert.That(error11,                                                                Does.Contain($"must be '{ChargingStationOperator.JSONLDContext}'"));

            }

        }

        #endregion

        #region RoamingNetwork_CBOR_rejects_an_unknown_language()

        [Test]
        public void RoamingNetwork_CBOR_rejects_an_unknown_language()
        {

            var cbor = CBORValue.FromMap([
                           new (CBORValue.FromText("@id"),   CBORValue.FromText("rn1")),
                           new (CBORValue.FromText("name"),  CBORValue.FromMap([
                                                                 new (CBORValue.FromText("xx-unknown"), CBORValue.FromText("?"))
                                                             ]))
                       ]);

            Assert.That(RoamingNetwork.TryParse(cbor, out _, out var error),  Is.False);
            Assert.That(error,                                                Does.Contain("xx-unknown"));

        }

        #endregion

    }

}
