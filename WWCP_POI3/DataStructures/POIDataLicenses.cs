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

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Data licenses within POI data: written completely and in a fixed order,
    /// so that they are part of the content and of its ETags,
    /// and read from complete JSON/CBOR or from the identifications of well-known licenses.
    /// </summary>
    public static class POIDataLicenses
    {

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization of a data license within POI data.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its identification.
            /// </summary>
            public const String  Id            = "@id";

            /// <summary>
            /// The property of its multilingual description.
            /// </summary>
            public const String  Description   = "description";

            /// <summary>
            /// The property of its URLs.
            /// </summary>
            public const String  URLs          = "URLs";

        }

        #endregion

        #region Known

        /// <summary>
        /// The well-known data licenses, which can be given by their identification alone.
        /// </summary>
        public static ImmutableArray<DataLicense> Known { get; } = [
            DataLicense.None,
            DataLicense.PublicDomainDedicationAndLicense,
            DataLicense.AttributionLicense,
            DataLicense.OpenDatabaseLicense,
            DataLicense.DatenlizenzDeutschland_BY_2,
            DataLicense.DatenlizenzDeutschland_Zero_2,
            DataLicense.GeoLizenz_OpenData_1_3_1,
            DataLicense.CreativeCommons_BY_4,
            DataLicense.CreativeCommons_BY_SA_4,
            DataLicense.CreativeCommons_BY_ND_4,
            DataLicense.CreativeCommons_BY_NC_4,
            DataLicense.CreativeCommons_BY_NC_SA_4,
            DataLicense.CreativeCommons_BY_NC_ND_4
        ];

        #endregion


        #region Ordered    (this DataLicenses)

        /// <summary>
        /// The given data licenses, ordered by their identification (ordinal).
        /// </summary>
        public static IEnumerable<DataLicense> Ordered(this IEnumerable<DataLicense> DataLicenses)

            => DataLicenses.OrderBy(dataLicense => dataLicense.Id.ToString(), StringComparer.Ordinal);

        #endregion

        #region ToPOIJSON  (this DataLicense)

        /// <summary>
        /// The complete JSON representation of the given data license within POI data:
        /// embedded, with its URLs in ordinal order.
        /// </summary>
        public static JObject ToPOIJSON(this DataLicense DataLicense)

            => JSONObject.Create(

                         new JProperty(PropertyKeys.Id,            DataLicense.Id.ToString()),

                   DataLicense.Description.IsNotNullOrEmpty()
                       ? new JProperty(PropertyKeys.Description,   DataLicense.Description.ToJSON())
                       : null,

                   DataLicense.URLs.Any()
                       ? new JProperty(PropertyKeys.URLs,          new JArray(DataLicense.URLs.
                                                                       Select (url => url.ToString()).
                                                                       Order  (StringComparer.Ordinal)))
                       : null

               );

        #endregion

        #region ToPOIJSON  (this DataLicenses)

        /// <summary>
        /// The complete JSON representations of the given data licenses, ordered by their identification.
        /// </summary>
        public static JArray ToPOIJSON(this IEnumerable<DataLicense> DataLicenses)

            => new (DataLicenses.Ordered().Select(dataLicense => dataLicense.ToPOIJSON()));

        #endregion

        #region ToPOICBOR  (this DataLicense)

        /// <summary>
        /// The complete CBOR representation of the given data license within POI data: the keys and values of its JSON
        /// representation, built directly.
        /// </summary>
        public static CBORValue ToPOICBOR(this DataLicense DataLicense)
        {

            var cbor = new CBORMap {
                           { CBORValue.FromText(PropertyKeys.Id),            CBORValue.FromText(DataLicense.Id.ToString()) }
                       };

            if (DataLicense.Description.IsNotNullOrEmpty())
                cbor.Add(CBORValue.FromText(PropertyKeys.Description),   DataLicense.Description.ToCBOR());

            if (DataLicense.URLs.Any())
                cbor.Add(CBORValue.FromText(PropertyKeys.URLs),          CBORValue.FromArray(DataLicense.URLs.
                                                                                     Select (url => url.ToString()).
                                                                                     Order  (StringComparer.Ordinal).
                                                                                     Select (CBORValue.FromText)));

            return cbor;

        }

        #endregion

        #region ToPOICBOR  (this DataLicenses)

        /// <summary>
        /// The complete CBOR representations of the given data licenses, ordered by their identification.
        /// </summary>
        public static CBORValue ToPOICBOR(this IEnumerable<DataLicense> DataLicenses)

            => CBORValue.FromArray(DataLicenses.Ordered().Select(dataLicense => dataLicense.ToPOICBOR()));

        #endregion

        #region ToPOIIdsJSON(this DataLicenses)

        /// <summary>
        /// The identifications of the given data licenses, in ordinal order.
        /// </summary>
        public static JArray ToPOIIdsJSON(this IEnumerable<DataLicense> DataLicenses)

            => new (DataLicenses.Ordered().Select(dataLicense => dataLicense.Id.ToString()));

        #endregion


        #region TryParse   (DataLicensesJSON, DataLicenseIdsJSON, out DataLicenses, out ErrorResponse)

        /// <summary>
        /// Try to read data licenses from complete JSON representations ("dataLicenses")
        /// and/or from the identifications of well-known licenses ("dataLicenseIds").
        /// An unknown identification is an error, as it can not be turned into a license.
        /// </summary>
        /// <param name="DataLicensesJSON">An optional JSON array of complete data licenses.</param>
        /// <param name="DataLicenseIdsJSON">An optional JSON array of identifications of well-known data licenses.</param>
        /// <param name="DataLicenses">The data licenses read.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JToken?                           DataLicensesJSON,
                                       JToken?                           DataLicenseIdsJSON,
                                       out ImmutableArray<DataLicense>   DataLicenses,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            DataLicenses = [];

            var dataLicenses = new Dictionary<DataLicense_Id, DataLicense>();

            if (DataLicensesJSON is not null)
            {

                if (DataLicensesJSON is not JArray dataLicensesArray)
                {
                    ErrorResponse = "The data licenses must be a JSON array!";
                    return false;
                }

                foreach (var dataLicenseJSON in dataLicensesArray)
                {

                    if (dataLicenseJSON is not JObject dataLicenseObject)
                    {
                        ErrorResponse = "A data license must be a JSON object!";
                        return false;
                    }

                    if (!DataLicense.TryParse(dataLicenseObject, out var dataLicense, out var dataLicenseError))
                    {
                        ErrorResponse = "Invalid data license: " + dataLicenseError;
                        return false;
                    }

                    if (!dataLicenses.TryAdd(dataLicense!.Id, dataLicense))
                    {
                        ErrorResponse = $"The data license '{dataLicense.Id}' must not be given twice!";
                        return false;
                    }

                }

            }

            if (DataLicenseIdsJSON is not null)
            {

                if (DataLicenseIdsJSON is not JArray dataLicenseIdsArray)
                {
                    ErrorResponse = "The data license identifications must be a JSON array!";
                    return false;
                }

                foreach (var dataLicenseIdJSON in dataLicenseIdsArray)
                {

                    if (dataLicenseIdJSON.Type != JTokenType.String ||
                        !DataLicense_Id.TryParse(dataLicenseIdJSON.Value<String>() ?? "", out var dataLicenseId))
                    {
                        ErrorResponse = $"Invalid data license identification '{dataLicenseIdJSON}'!";
                        return false;
                    }

                    var known = Known.FirstOrDefault(dataLicense => dataLicense.Id == dataLicenseId);

                    if (known is null)
                    {
                        ErrorResponse = $"The data license '{dataLicenseId}' is not well-known, so it must be given completely!";
                        return false;
                    }

                    if (!dataLicenses.TryAdd(dataLicenseId, known))
                    {
                        ErrorResponse = $"The data license '{dataLicenseId}' must not be given twice!";
                        return false;
                    }

                }

            }

            DataLicenses   = [.. dataLicenses.Values.Ordered()];
            ErrorResponse  = null;
            return true;

        }

        #endregion

    }

}
