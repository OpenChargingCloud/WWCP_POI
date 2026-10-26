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
    /// Hardware revisions and firmware versions are built like identifications, readonly structs implementing IId&lt;T&gt;,
    /// but taken exactly as they are given: neither trimmed nor compared case-insensitively.
    /// </summary>
    [TestFixture]
    public class ModelVersionTests
    {

        #region Hardware_revisions_are_taken_exactly_as_they_are_given()

        [Test]
        public void Hardware_revisions_are_taken_exactly_as_they_are_given()
        {

            using (Assert.EnterMultipleScope())
            {

                Assert.That(HardwareRevision.Parse("R2"),                                Is.EqualTo(HardwareRevision.Parse("R2")));
                Assert.That(HardwareRevision.Parse("R2").GetHashCode(),                  Is.EqualTo(HardwareRevision.Parse("R2").GetHashCode()));
                Assert.That(HardwareRevision.Parse("R2"),                                Is.Not.EqualTo(HardwareRevision.Parse("r2")));
                Assert.That(HardwareRevision.Parse("R2").CompareTo(HardwareRevision.Parse("r2")), Is.Not.Zero);
                Assert.That(HardwareRevision.Parse("1.0") < HardwareRevision.Parse("1.1"), Is.True);
                Assert.That(HardwareRevision.Parse("R2").ToString(),                     Is.EqualTo("R2"));

                Assert.That(HardwareRevision.TryParse(" R2", out _),                     Is.False);
                Assert.That(HardwareRevision.TryParse("R2 ", out _),                     Is.False);
                Assert.That(HardwareRevision.TryParse("",    out _),                     Is.False);
                Assert.That(HardwareRevision.TryParse("R 2", out _),                     Is.True);
                Assert.That(HardwareRevision.TryParse("  "),                             Is.Null);

                Assert.That(default(HardwareRevision).IsNullOrEmpty,                     Is.True);
                Assert.That(((HardwareRevision?) null).IsNullOrEmpty(),                  Is.True);
                Assert.That(((HardwareRevision?) HardwareRevision.Parse("R2")).IsNotNullOrEmpty(), Is.True);

            }

        }

        #endregion

        #region Firmware_versions_are_taken_exactly_as_they_are_given()

        [Test]
        public void Firmware_versions_are_taken_exactly_as_they_are_given()
        {

            using (Assert.EnterMultipleScope())
            {

                Assert.That(FirmwareVersion.Parse("2.4.1-rc1"),                          Is.EqualTo(FirmwareVersion.Parse("2.4.1-rc1")));
                Assert.That(FirmwareVersion.Parse("2.4.1-rc1"),                          Is.Not.EqualTo(FirmwareVersion.Parse("2.4.1-RC1")));
                Assert.That(new HashSet<FirmwareVersion> { FirmwareVersion.Parse("1.7"), FirmwareVersion.Parse("1.7") }, Has.Count.EqualTo(1));
                Assert.That(FirmwareVersion.Parse("1.7").Length,                         Is.EqualTo(3));

                Assert.That(() => FirmwareVersion.Parse(" 1.7"),                         Throws.ArgumentException.With.Message.Contains("Invalid text representation of a firmware version: ' 1.7'"));

            }

        }

        #endregion

        #region Models_name_each_version_once()

        [Test]
        public void Models_name_each_version_once()
        {

            Assert.That(() => ModelVersions.Of([ FirmwareVersion.Parse("1.7"), FirmwareVersion.Parse("1.7") ], "firmware version", "the model 'M-500'"),
                        Throws.ArgumentException.With.Message.Contains("A firmware version of the model 'M-500' must not be given twice"));

            Assert.That(() => ModelVersions.Of([ default(HardwareRevision) ], "hardware revision", "the model 'M-500'"),
                        Throws.ArgumentException.With.Message.Contains("A hardware revision of the model 'M-500' must not be empty"));

        }

        #endregion

    }

}
