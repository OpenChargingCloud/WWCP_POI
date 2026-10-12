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

using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A charging connector of an EVSE: a plug or a socket. Content of its EVSE, without a status of its own.
    /// </summary>
    /// <param name="Id">The identification of the connector, unique within its EVSE.</param>
    /// <param name="Type">The type of the connector, e.g. IEC_62196_T2.</param>
    /// <param name="Lockable">Whether the connector can be locked, if known.</param>
    public sealed record ChargingConnector(ChargingConnector_Id   Id,
                                           ChargingConnectorType  Type,
                                           Boolean?               Lockable   = null)
    {

        #region ToJSON()

        /// <summary>
        /// Return a JSON representation of this charging connector.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty("@id",   Id.  ToString()),
                           new JProperty("type",  Type.ToString())
                       );

            if (Lockable.HasValue)
                json.Add("lockable", Lockable.Value);

            return json;

        }

        #endregion

        #region (static) TryParse(JSON, out ChargingConnector, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a charging connector.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ChargingConnector">The parsed charging connector.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                      JSON,
                                       [NotNullWhen(true)]  out ChargingConnector?  ChargingConnector,
                                       [NotNullWhen(false)] out String?             ErrorResponse)
        {

            ChargingConnector = null;

            if (!JSON.ParseMandatory("@id",
                                     "charging connector identification",
                                     ChargingConnector_Id.TryParse,
                                     out ChargingConnector_Id id,
                                     out ErrorResponse))
            {
                return false;
            }

            if (!JSON.ParseMandatory("type",
                                     "charging connector type",
                                     ChargingConnectorType.TryParse,
                                     out ChargingConnectorType type,
                                     out ErrorResponse))
            {
                return false;
            }

            if (JSON.ParseOptional("lockable",
                                   "lockable",
                                   out Boolean? lockable,
                                   out ErrorResponse))
            {
                if (ErrorResponse is not null)
                    return false;
            }

            ChargingConnector  = new ChargingConnector(id, type, lockable);
            ErrorResponse      = null;
            return true;

        }

        #endregion

    }

}
