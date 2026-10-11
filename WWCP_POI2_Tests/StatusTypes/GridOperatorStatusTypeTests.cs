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

namespace cloud.charging.open.protocols.WWCP.POI.tests.StatusTypes
{

    /// <summary>
    /// The grid operator's status types are readonly structs like the other status types:
    /// they parse any nonempty text, compare without case sensitivity and write camel case.
    /// </summary>
    [TestFixture]
    public class GridOperatorStatusTypeTests
    {

        #region Status_types_parse_and_compare_without_case_sensitivity()

        [Test]
        public void Status_types_parse_and_compare_without_case_sensitivity()
        {

            Assert.That(GridOperatorStatusType.     Parse("Offline"),        Is.EqualTo(GridOperatorStatusType.Offline));
            Assert.That(GridOperatorStatusType.     Parse("AVAILABLE"),      Is.EqualTo(GridOperatorStatusType.Available));
            Assert.That(GridOperatorAdminStatusType.Parse("operational"),    Is.EqualTo(GridOperatorAdminStatusType.Operational));
            Assert.That(GridOperatorAdminStatusType.Parse("OutOfService"),   Is.EqualTo(GridOperatorAdminStatusType.OutOfService));

            Assert.That(GridOperatorStatusType.Offline.GetHashCode(),        Is.EqualTo(GridOperatorStatusType.Parse("OFFLINE").GetHashCode()));
            Assert.That(GridOperatorStatusType.Offline,                      Is.Not.EqualTo(GridOperatorStatusType.Available));

        }

        #endregion

        #region Status_types_write_their_camel_case_text()

        [Test]
        public void Status_types_write_their_camel_case_text()
        {

            Assert.That(GridOperatorStatusType.     InDeployment.ToString(), Is.EqualTo("inDeployment"));
            Assert.That(GridOperatorAdminStatusType.InternalUse. ToString(), Is.EqualTo("internalUse"));

        }

        #endregion

        #region Empty_text_is_no_status_type()

        [TestCase("")]
        [TestCase("   ")]
        public void Empty_text_is_no_status_type(String Text)
        {

            Assert.That(GridOperatorStatusType.     TryParse(Text, out _),  Is.False);
            Assert.That(GridOperatorAdminStatusType.TryParse(Text, out _),  Is.False);
            Assert.That(() => GridOperatorStatusType.Parse(Text),           Throws.ArgumentException);

        }

        #endregion

    }

}
