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

using System.Reflection;
using System.Runtime.CompilerServices;

using NUnit.Framework;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.StatusTypes
{

    /// <summary>
    /// All (admin) status types are readonly structs: no enums are left, every predefined
    /// value is distinct, writes its text and parses back from it in any case.
    /// </summary>
    [TestFixture]
    public class StatusTypeConsistencyTests
    {

        private static IEnumerable<Type> StatusTypes()

            => typeof(EVSE_Id).Assembly.GetTypes().
                   Where(type => type.IsPublic && (type.Name.EndsWith("StatusType") || type.Name.EndsWith("StatusTypes"))).
                   OrderBy(type => type.Name);


        #region No_status_type_is_an_enum()

        [Test]
        public void No_status_type_is_an_enum()
        {

            var types = StatusTypes().ToArray();

            Assert.That(types, Has.Length.GreaterThanOrEqualTo(35));

            foreach (var type in types)
            {
                Assert.That(type.Name,                                     Does.EndWith("StatusType"), type.Name);
                Assert.That(type.IsValueType && !type.IsEnum,              Is.True,                    type.Name);
                Assert.That(type.IsDefined(typeof(IsReadOnlyAttribute), false), Is.True,               type.Name);
            }

        }

        #endregion

        #region Predefined_values_roundtrip_and_ignore_case()

        [TestCaseSource(nameof(StatusTypes))]
        public void Predefined_values_roundtrip_and_ignore_case(Type Type)
        {

            // Predefined values are static fields in some types and static properties in others.
            var values = Type.GetFields    (BindingFlags.Public | BindingFlags.Static).
                              Where (field    => field.FieldType    == Type).
                              Select(field    => field.GetValue(null)!).
                         Concat(Type.GetProperties(BindingFlags.Public | BindingFlags.Static).
                              Where (property => property.PropertyType == Type).
                              Select(property => property.GetValue(null)!)).
                         ToArray();

            var parse  = Type.GetMethod("Parse", [typeof(String)])!;

            Assert.That(values,                                         Is.Not.Empty);
            Assert.That(values.Select(value => value.ToString()),       Is.Unique);
            Assert.That(values.Select(value => value.ToString()),       Has.Some.EqualTo("unknown").Or.Some.EqualTo("unspecified"));

            foreach (var value in values)
            {
                Assert.That(parse.Invoke(null, [value.ToString()]),                      Is.EqualTo(value));
                Assert.That(parse.Invoke(null, [value.ToString()!.ToUpperInvariant()]),  Is.EqualTo(value));
            }

        }

        #endregion

    }

}
