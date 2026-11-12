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
using System.Text;

using org.GraphDefined.Vanaheimr.Hermod;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Charging at the operator's charging station management system, as the backend of EV Taler sessions needs it
    /// (concepts/EVTaler.md, section 5.6): started and limited to the credit paid, the limit raised with every step paid,
    /// stopped; with OCPP 2.1 RequestStartTransaction with a transactionLimit, the limit raised in the answer to the next
    /// TransactionEvent (after a TriggerMessage), RequestStopTransaction. The system tells the backend what happens through
    /// IEVTalerChargingEvents: the start with its signed meter value (Transaction.Begin), what was delivered, and the end
    /// with its signed meter value (Transaction.End). It knows the web payments of its EVSEs, as OCPP 2.1's WebPaymentsCtrlr
    /// of an EVSE or of its station is set: every session names the one-time password of its EVSE (decision D8).
    /// </summary>
    public interface IEVTalerChargingControl
    {

        /// <summary>
        /// The web payments of the given EVSE, as OCPP 2.1's WebPaymentsCtrlr of the EVSE, or else of its station, is set:
        /// what the one-time password of its secure dynamic QR code is made of; null where it gives none.
        /// </summary>
        Task<EVTalerWebPayments?> WebPaymentsOf(EVSE_Id            EVSEId,
                                                CancellationToken  CancellationToken);

        /// <summary>
        /// Why the given EVSE takes no session now, e.g. no vehicle plugged in or one charging already; null when it does.
        /// </summary>
        Task<String?> WhyNotStart(EVSE_Id            EVSEId,
                                  CancellationToken  CancellationToken);

        /// <summary>
        /// Start charging at the given EVSE for the given session, limited to the given credit; what happens is told to
        /// the given events. Null when it started, else why not.
        /// </summary>
        Task<String?> Start(EVSE_Id                  EVSEId,
                            String                   SessionId,
                            EVTalerChargingLimit     Limit,
                            IEVTalerChargingEvents   Events,
                            CancellationToken        CancellationToken);

        /// <summary>
        /// Raise the limit of the given session to the given credit.
        /// </summary>
        Task RaiseLimit(String                SessionId,
                        EVTalerChargingLimit  Limit,
                        CancellationToken     CancellationToken);

        /// <summary>
        /// Stop charging of the given session; its end is told as every other end.
        /// </summary>
        Task Stop(String             SessionId,
                  CancellationToken  CancellationToken);

    }


    /// <summary>
    /// What the charging station management system tells the backend of an EV Taler session.
    /// </summary>
    public interface IEVTalerChargingEvents
    {

        /// <summary>
        /// Charging started, with the signed meter value at the start (OCPP 2.1 Transaction.Begin), if the station gave one.
        /// </summary>
        Task Started(String                    SessionId,
                     EVTalerSignedMeterValue?  StartValue);

        /// <summary>
        /// What was delivered since the start, in the unit of the steps: kWh, or minutes.
        /// </summary>
        Task Delivered(String   SessionId,
                       Decimal  Quantity);

        /// <summary>
        /// Charging ended (EVTalerStopReasons), with the signed meter value at the end (OCPP 2.1 Transaction.End), if the
        /// station gave one.
        /// </summary>
        Task Ended(String                    SessionId,
                   EVTalerSignedMeterValue?  EndValue,
                   String                    Reason);

    }


    /// <summary>
    /// The limit of charging: the credit paid, counted from the start of the session, in the unit of its steps; OCPP 2.1's
    /// transactionLimit maxEnergy (in Wh) for kWh, maxTime (in seconds) for minutes.
    /// </summary>
    /// <param name="Quantity">The credit.</param>
    /// <param name="Unit">EVTalerCovers.KWh or EVTalerCovers.Minutes.</param>
    public sealed record EVTalerChargingLimit(Decimal  Quantity,
                                              String   Unit)
    {

        /// <summary>
        /// OCPP 2.1's maxEnergy, in Wh, for a limit in kWh.
        /// </summary>
        public Decimal? MaxEnergyWh
            => Unit == EVTalerCovers.KWh ? Quantity * 1000m : null;

        /// <summary>
        /// OCPP 2.1's maxTime, in seconds, for a limit in minutes.
        /// </summary>
        public Decimal? MaxTimeSeconds
            => Unit == EVTalerCovers.Minutes ? Quantity * 60m : null;

        public override String ToString()
            => $"{EVTalerAmounts.WriteDecimal(Quantity)} {Unit}";

    }


    /// <summary>
    /// The web payments of an EVSE, as OCPP 2.1's WebPaymentsCtrlr of the EVSE or of its station is set: what the time-based
    /// one-time password (TOTP) in the URL of its secure dynamic QR code is made of (concepts/EVTaler.md, 5.6), as Hermod's
    /// TOTPGenerator makes it. The check keeps no state: a password is current in its time slot and in the one before and
    /// after it, however often it is named, as the clocks of the station and of the system differ and a driver takes a
    /// moment. A short validity time keeps a password photographed or passed on from being worth much.
    /// </summary>
    /// <param name="SharedSecret">The shared secret of the station and the charging station management system (SharedSecret).</param>
    /// <param name="ValidityTime">How long a password is valid (ValidityTime): whole seconds, 1 s at least.</param>
    /// <param name="Length">The length of a password (Length), 12 by default.</param>
    /// <param name="HashAlgorithm">The hash algorithm of its HMAC, SHA-256 by default.</param>
    public sealed record EVTalerWebPayments(String             SharedSecret,
                                            TimeSpan           ValidityTime,
                                            Byte               Length          = 12,
                                            TOTPHashAlgorithm  HashAlgorithm   = TOTPHashAlgorithm.SHA256)
    {

        /// <summary>
        /// Whether the given password is the one the EVSE gives at the given time, or in the time slot before or after it.
        /// Settings TOTPGenerator does not take, e.g. a shared secret of less than 16 characters, throw: the system is set
        /// wrongly, which is no fault of the driver.
        /// </summary>
        /// <param name="TOTP">The password, as the URL of the code carries it.</param>
        /// <param name="Now">The time.</param>
        public Boolean IsCurrent(String          TOTP,
                                 DateTimeOffset  Now)
        {

            var (previous, current, next, _, _) = TOTPGenerator.GenerateTOTPs(SharedSecret,
                                                                              ValidityTime,
                                                                              Length,
                                                                              TOTPTimestamp:  Now,
                                                                              HashAlgorithm:  HashAlgorithm);

            var given = Encoding.UTF8.GetBytes(TOTP);

            return Same(given, previous) |
                   Same(given, current)  |
                   Same(given, next);

        }

        private static Boolean Same(Byte[]  Given,
                                    String  Expected)

            => CryptographicOperations.FixedTimeEquals(Given, Encoding.UTF8.GetBytes(Expected));

        /// <summary>
        /// Without the shared secret.
        /// </summary>
        public override String ToString()
            => $"{Length} characters, valid {ValidityTime.TotalSeconds} s, HMAC-{HashAlgorithm}";

    }

}
