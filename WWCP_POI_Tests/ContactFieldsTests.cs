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

using System.Text.Json;
using NUnit.Framework;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Hermod.Mail;
using org.GraphDefined.Vanaheimr.Illias;
using cloud.charging.open.protocols.WWCP.POI;

namespace WWCP_POI_Tests;

/// <summary>
/// The contact fields of operators and providers are static POI data:
/// they are parsed, exported, captured and editable through ChangeSets.
/// </summary>
[TestFixture]
public sealed class ContactFieldsTests
{

    private const String Document = """
        {
          "@id": "network-a", "name": { "en": "Network" },
          "chargingStationOperators": [{
            "@id": "DE*ABC", "name": { "en": "Operator" },
            "geoLocation": { "lat": 52.125, "lng": 13.625 },
            "telephone": "+49 30 1234567",
            "eMailAddress": "office@example.com",
            "termsAndConditions": "https://example.com/terms"
          }],
          "eMobilityProviders": [{
            "@id": "DE-GHI", "name": { "en": "Provider" },
            "geoLocation": { "lat": 48.125, "lng": 11.625 },
            "telephone": "+49 89 7654321",
            "eMailAddress": "service@example.org"
          }]
        }
        """;

    [Test]
    public void Contact_fields_are_parsed_exported_and_captured()
    {
        var network  = RoamingNetwork.Parse(Document);
        var restored = RoamingNetwork.Parse(network.ToJSONSnapshot().ToString());

        foreach (var current in new[] { network, restored })
        {
            var op       = current.ChargingStationOperators.Single();
            var provider = current.EMobilityProviders.Single();

            Assert.That(op.GeoLocation?.Latitude.Value,  Is.EqualTo(52.125));
            Assert.That(op.Telephone,                    Is.EqualTo(PhoneNumber.Parse("+49 30 1234567")));
            Assert.That(op.EMailAddress,                 Is.EqualTo(SimpleEMailAddress.Parse("office@example.com")));
            Assert.That(op.TermsAndConditionsURL,        Is.EqualTo(URL.Parse("https://example.com/terms")));

            Assert.That(provider.GeoLocation?.Longitude.Value, Is.EqualTo(11.625));
            Assert.That(provider.Telephone,                    Is.EqualTo(PhoneNumber.Parse("+49 89 7654321")));
            Assert.That(provider.EMailAddress,                 Is.EqualTo(SimpleEMailAddress.Parse("service@example.org")));
        }

        Assert.That(network.DataSnapshot.GetEntity(InfrastructureEntityType.ChargingStationOperator, "DE*ABC").Properties.Keys,
                    Is.SupersetOf(new[] { "geoLocation", "telephone", "eMailAddress", "termsAndConditions" }));
        Assert.That(restored.DataSnapshot.Root, Is.EqualTo(network.DataSnapshot.Root));

        var cbor = RoamingNetworkDataSnapshot.ParseCBOR(network.DataSnapshot.ToCBOR(IncludeVersionMetadata: true));
        Assert.That(cbor.GetEntity(InfrastructureEntityType.EMobilityProvider, "DE-GHI").Properties.Keys,
                    Is.SupersetOf(new[] { "geoLocation", "telephone", "eMailAddress" }));
        Assert.That(cbor.ToJSON().ToString(), Is.EqualTo(network.DataSnapshot.ToJSON().ToString()));
    }

    [Test]
    public void Absent_contact_fields_stay_absent()
    {
        var network = RoamingNetwork.Parse("""
            { "@id": "network-a", "name": { "en": "Network" },
              "eMobilityProviders": [{ "@id": "DE-GHI", "name": { "en": "Provider" } }] }
            """);

        Assert.That(network.EMobilityProviders.Single().GeoLocation, Is.Null);
    }

    [Test]
    public void A_ChangeSet_updates_an_operator_telephone()
    {
        var network  = RoamingNetwork.Parse(Document);
        var snapshot = network.DataSnapshot;
        var old      = snapshot.GetEntity(InfrastructureEntityType.ChargingStationOperator, "DE*ABC").Properties["telephone"];

        var next = network.ApplyChangeSet(snapshot.CreateChangeSet(
                       id:        "telephone",
                       createdAt: DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                       changes:   [ RoamingNetworkChange.UpdateProperty("ChargingStationOperator", "DE*ABC", "telephone",
                                                                        old, JsonSerializer.SerializeToElement("+49 30 7654321")) ]));

        Assert.That(next.ChargingStationOperators.Single().Telephone, Is.EqualTo(PhoneNumber.Parse("+49 30 7654321")));
    }

    [Test]
    public void An_invalid_e_mail_address_is_rejected()
    {
        Assert.That(() => RoamingNetwork.Parse(Document.Replace("office@example.com", "no e-mail address")),
                    Throws.ArgumentException);
    }

}
