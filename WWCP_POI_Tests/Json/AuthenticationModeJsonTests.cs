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


using Newtonsoft.Json.Linq;

using NUnit.Framework;

using cloud.charging.open.protocols.WWCP.POI;

#endregion

namespace WWCP_POI_Tests.Json
{
    [TestFixture]
    public sealed class AuthenticationModeJsonTests
    {
        private static IEnumerable<AuthenticationModes> SimpleModes()
        {

            yield return new AuthenticationModes.FreeCharging();

            yield return new AuthenticationModes.PINPAD();

            yield return new AuthenticationModes.ISO15118_PLC();

            yield return new AuthenticationModes.ISO15118_Air();

            yield return new AuthenticationModes.REMOTE();

            yield return new AuthenticationModes.CreditCard();

            yield return new AuthenticationModes.DebitCard();

            yield return new AuthenticationModes.PrepaidCard();

            yield return new AuthenticationModes.NFC();

            yield return new AuthenticationModes.Bluetooth();

            yield return new AuthenticationModes.WLAN();

            yield return new AuthenticationModes.NoAuthenticationRequired();

            yield return new AuthenticationModes("VendorDefinedMode");

        }

        [TestCaseSource(nameof(SimpleModes))]
        public void Simple_modes_roundtrip_to_the_original_variant(AuthenticationModes source)
        {

            var json = JObject.Parse(source.ToJSON().ToString());
            var parsed = AuthenticationModes.Parse(json);

            Assert.That(parsed.GetType(), Is.EqualTo(source.GetType()));
            Assert.That(parsed.Type, Is.EqualTo(source.Type));
            Assert.That(JToken.DeepEquals(parsed.ToJSON(), json), Is.True);

        }

        [Test]
        public void Rfid_roundtrip_preserves_all_card_types_and_brand_ids()
        {

            var source = new AuthenticationModes.RFID([RFIDCardTypes.MifareClassic, RFIDCardTypes.MifareDESFire],
                                                                                      [Brand_Id.Parse("brand-a"), Brand_Id.Parse("brand-b")]);
            var parsed = (AuthenticationModes.RFID)AuthenticationModes.Parse(JObject.Parse(source.ToJSON().ToString()));

            Assert.That(parsed.CardTypes, Is.EqualTo(source.CardTypes));
            Assert.That(parsed.BrandIds, Is.EqualTo(source.BrandIds));

        }

        [Test]
        public void Rfid_snapshots_input_collections_before_serialization()
        {

            var cards = new List<RFIDCardTypes> { RFIDCardTypes.Calypso };
            var brands = new List<Brand_Id> { Brand_Id.Parse("brand-a") };
            var source = new AuthenticationModes.RFID(cards, brands);

            cards.Clear();
            brands.Clear();

            var parsed = (AuthenticationModes.RFID)AuthenticationModes.Parse(source.ToJSON());

            Assert.That(parsed.CardTypes, Is.EqualTo(new[] { RFIDCardTypes.Calypso }));
            Assert.That(parsed.BrandIds.Single(), Is.EqualTo(Brand_Id.Parse("brand-a")));

        }

        [TestCase(null)]
        [TestCase("station-123")]
        public void Sms_roundtrip_preserves_phone_number_and_station_code(string? stationCode)
        {

            var source = new AuthenticationModes.SMS("+49123456789", stationCode);
            var parsed = (AuthenticationModes.SMS)AuthenticationModes.Parse(source.ToJSON());

            Assert.That(parsed.Number, Is.EqualTo(source.Number));
            Assert.That(parsed.StationCode, Is.EqualTo(stationCode));

        }

        [Test]
        public void Phone_call_roundtrip_preserves_phone_number()
        {

            var source = new AuthenticationModes.PhoneCall("+49123456789");
            var parsed = (AuthenticationModes.PhoneCall)AuthenticationModes.Parse(source.ToJSON());

            Assert.That(parsed.Number, Is.EqualTo(source.Number));

        }

        [TestCase("{}")]
        [TestCase("{\"type\":null}")]
        [TestCase("{\"type\":42}")]
        [TestCase("{\"type\":\" \"}")]
        [TestCase("{\"type\":\"RFID\",\"cardTypes\":\"MifareClassic\"}")]
        [TestCase("{\"type\":\"RFID\",\"cardTypes\":[\"UnknownCard\"]}")]
        [TestCase("{\"type\":\"RFID\",\"cardTypes\":[0]}")]
        [TestCase("{\"type\":\"RFID\",\"brandIds\":[\"\"]}")]
        [TestCase("{\"type\":\"SMS\"}")]
        [TestCase("{\"type\":\"SMS\",\"Number\":123}")]
        [TestCase("{\"type\":\"SMS\",\"Number\":\"123\",\"StationCode\":{}}")]
        [TestCase("{\"type\":\"PhoneCall\",\"Number\":\" \"}")]
        public void Invalid_authentication_json_is_rejected(string text)
        {

            var json = JObject.Parse(text);

            Assert.That(AuthenticationModes.TryParse(json, out var parsed, out var error), Is.False);
            Assert.That(parsed, Is.Null);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(() => AuthenticationModes.Parse(json), Throws.ArgumentException);

        }

        [Test]
        public void Null_json_is_rejected_without_throwing()
        {

            Assert.That(AuthenticationModes.TryParse(null!, out var parsed, out var error), Is.False);
            Assert.That(parsed, Is.Null);
            Assert.That(error, Is.Not.Null.And.Not.Empty);

        }
    }
}
