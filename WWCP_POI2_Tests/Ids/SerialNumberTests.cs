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

using NUnit.Framework;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.Ids
{

    /// <summary>
    /// Serial numbers are built like identifications, a readonly struct implementing IId&lt;T&gt;, but taken exactly as
    /// they are given, as hardware revisions and firmware versions are: neither trimmed nor compared case-insensitively.
    /// </summary>
    [TestFixture]
    public class SerialNumberTests
    {

        #region Serial_numbers_are_taken_exactly_as_they_are_given()

        [Test]
        public void Serial_numbers_are_taken_exactly_as_they_are_given()
        {

            var serialNumber1 = SerialNumber.Parse("1ESY1161229886");
            var serialNumber2 = SerialNumber.Parse("1ESY1161229886");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(serialNumber1,                                               Is.EqualTo(serialNumber2));
                Assert.That(serialNumber1.GetHashCode(),                                 Is.EqualTo(serialNumber2.GetHashCode()));
                Assert.That(SerialNumber.Parse("M500-000815"),                           Is.Not.EqualTo(SerialNumber.Parse("m500-000815")));
                Assert.That(SerialNumber.Parse("M500-000815").CompareTo(SerialNumber.Parse("m500-000815")), Is.Not.Zero);
                Assert.That(SerialNumber.Parse("M-1") < SerialNumber.Parse("M-2"),       Is.True);
                Assert.That(new HashSet<SerialNumber> { SerialNumber.Parse("M-1"), SerialNumber.Parse("M-1") }, Has.Count.EqualTo(1));
                Assert.That(SerialNumber.Parse("M-1").ToString(),                        Is.EqualTo("M-1"));
                Assert.That(SerialNumber.Parse("M-1").Length,                            Is.EqualTo(3));

                Assert.That(SerialNumber.TryParse(" M-1", out _),                        Is.False);
                Assert.That(SerialNumber.TryParse("M-1 ", out _),                        Is.False);
                Assert.That(SerialNumber.TryParse("",     out _),                        Is.False);
                Assert.That(SerialNumber.TryParse("M 1",  out _),                        Is.True);
                Assert.That(SerialNumber.TryParse("  "),                                 Is.Null);
                Assert.That(() => SerialNumber.Parse(" M-1"),                            Throws.ArgumentException.With.Message.Contains("Invalid text representation of a serial number: ' M-1'"));

                Assert.That(default(SerialNumber).IsNullOrEmpty,                         Is.True);
                Assert.That(((SerialNumber?) null).IsNullOrEmpty(),                      Is.True);
                Assert.That(((SerialNumber?) SerialNumber.Parse("M-1")).IsNotNullOrEmpty(), Is.True);

            }

        }

        #endregion

    }

}
