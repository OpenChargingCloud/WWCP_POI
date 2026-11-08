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

using System.Security.Cryptography;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// A refusal says what it refuses as WWCP_POI.TS says it: with the article it is spoken with, its numbers in no
    /// culture of this machine, and who may do what it refuses.
    /// </summary>
    [TestFixture]
    public class RefusalWordsTests
    {

        #region Data

        private static readonly ChargingStation_Id  S1  = ChargingStation_Id.Parse("DE*GEF*S1");
        private static readonly EVSE_Id             E1  = EVSE_Id.           Parse("DE*GEF*E1*1");

        #endregion


        #region A_refusal_says_its_article()

        /// <summary>
        /// "An" before A, E, I and O, "A" before any other letter: not "An public key", "A EU vehicle category",
        /// "A accepted e-mobility provider" or "A ENERGY price component".
        /// </summary>
        [Test]
        public void A_refusal_says_its_article()
        {

            using var key       = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var publicKey       = new MeasurementPublicKey(key.ExportSubjectPublicKeyInfo());
            var provider        = EMobilityProvider_Id.Parse("DE-ABC");

            var energy          = new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.39m, StepSize: 1).ToJSON();
            energy.Remove("stepSize");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(() => new ChargingStation(S1, PublicKeys: [ publicKey, publicKey ]),
                            Throws.ArgumentException.With.Message.Contains("A public key of the charging station 'DE*GEF*S1' must not be given twice!"));

                Assert.That(() => new ChargingStation(S1, EUVehicleCategories: [ EUVehicleCategory.M1, EUVehicleCategory.M1 ]),
                            Throws.ArgumentException.With.Message.Contains("An EU vehicle category of the charging station 'DE*GEF*S1' must not be given twice!"));

                Assert.That(() => new EVSE(E1, AcceptedEMobilityProviderIds: [ provider, provider ]),
                            Throws.ArgumentException.With.Message.Contains("An accepted e-mobility provider of the EVSE 'DE*GEF*E1*1' must not be given twice!"));

                Assert.That(ChargingPriceComponent.TryParse(energy, out _, out var energyError),  Is.False);
                Assert.That(energyError,  Is.EqualTo("An ENERGY price component needs a positive step size!"));

            }

        }

        #endregion

        #region A_refusal_says_its_numbers_in_no_culture()

        /// <summary>
        /// On a German machine as on any other: "22000.5 W", not "22000,5 W", and no trailing zeros, as WWCP_POI.TS says it.
        /// </summary>
        [Test]
        [SetCulture("de-DE")]
        public void A_refusal_says_its_numbers_in_no_culture()
        {

            using (Assert.EnterMultipleScope())
            {

                Assert.That(() => new ChargingStation(S1,
                                                      EVSEs:     [ new EVSE(E1, MaxPower: Watt.FromW(11000.25m)) ],
                                                      MaxPower:  Watt.FromW(22000.50m)),
                            Throws.ArgumentException.With.Message.Contains("The maximum power of the charging station 'DE*GEF*S1', 22000.5 W, must not exceed the 11000.25 W of all its EVSEs together!"));

                Assert.That(() => new ChargingTariffDiscount(100.50m),
                            Throws.ArgumentException.With.Message.Contains("must be above 0 and at most 100, not 100.5!"));

                Assert.That(() => new EVTalerCovers(2.50m, 1.0m, EVTalerCovers.KWh),
                            Throws.ArgumentException.With.Message.Contains("A step covers from 2.5 to 1: from not negative and less than to!"));

            }

        }

        #endregion

        #region A_grid_connection_point_names_its_maintainers()

        /// <summary>
        /// A grid connection point is maintained by its grid operator, or by the charging station operator or parking
        /// operator its maintainedBy names: a maintainedBy naming its grid operator is refused, not as if it could.
        /// </summary>
        [Test]
        public void A_grid_connection_point_names_its_maintainers()
        {

            Assert.That(() => new GridConnectionPoint(GridConnectionPoint_Id.Parse("connection-1"), MaintainedBy: TestParties.Party(GridOperator_Id.Parse("DE*NET"))),
                        Throws.ArgumentException.With.Message.StartsWith("Besides its grid operator, the grid connection point 'connection-1' can only be maintained by a charging station operator or a parking operator, not by "));

        }

        #endregion

    }

}
