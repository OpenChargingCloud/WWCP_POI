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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The manufacturer a certificate of conformity approves the quality system of: a charging station manufacturer or an
    /// energy meter manufacturer, by its identification, and the scope of its quality system, the categories of products
    /// it makes under it, e.g. MI-003, the active electrical energy meters of the MID. The holder of a certificate of a
    /// module of quality assurance (Decision 768/2008/EC, Annex II, modules D, D1, E, E1, H and H1): the approval belongs
    /// to the manufacturer, not to one of its models.
    /// </summary>
    public sealed class QIDigitalManufacturer
    {

        #region Data

        /// <summary>
        /// The type of a charging station manufacturer.
        /// </summary>
        public const String ChargingStationManufacturerType  = nameof(ChargingStationManufacturer);

        /// <summary>
        /// The type of an energy meter manufacturer.
        /// </summary>
        public const String EnergyMeterManufacturerType      = nameof(EnergyMeterManufacturer);

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its type.
            /// </summary>
            public const String  Type            = "type";

            /// <summary>
            /// The property of its manufacturer.
            /// </summary>
            public const String  ManufacturerId  = "manufacturerId";

            /// <summary>
            /// The property of the scope of its quality system.
            /// </summary>
            public const String  Scope           = "scope";

        }

        #endregion

        #region (static) class Categories

        /// <summary>
        /// Well-known categories of the scope of a quality system, each of the kind of products it is for; any other
        /// nonempty text names another one, of a kind not known here.
        /// </summary>
        public static class Categories
        {

            /// <summary>
            /// Active electrical energy meters, the instrument specific annex MI-003 of the MID (Annex V): energy meters.
            /// </summary>
            public const String  MI003  = "MI-003";

            /// <summary>
            /// The kind of products the given category is for, ChargingStation or EnergyMeter, or null when it is not known.
            /// </summary>
            /// <param name="Category">A category of a scope.</param>
            public static String? KindOf(String Category)

                => Category switch {
                       MI003  => QIDigitalItem.EnergyMeterType,
                       _      => null
                   };

        }

        #endregion


        /// <summary>
        /// ChargingStationManufacturer or EnergyMeterManufacturer.
        /// </summary>
        public String                           Type                 { get; }

        /// <summary>
        /// The manufacturer, a charging station or an energy meter manufacturer by its type: the holder of the certificate.
        /// </summary>
        public String                           ManufacturerId       { get; }

        /// <summary>
        /// The scope of its quality system: the categories of products it makes under it, at least one, each once, in
        /// their given order, e.g. MI-003.
        /// </summary>
        public ImmutableArray<String>           Scope                { get; }

        /// <summary>
        /// Itself as a charging station manufacturer, when it is one.
        /// </summary>
        public ChargingStationManufacturer_Id?  ChargingStationManufacturerId
            => Type == ChargingStationManufacturerType ? ChargingStationManufacturer_Id.Parse(ManufacturerId) : null;

        /// <summary>
        /// Itself as an energy meter manufacturer, when it is one.
        /// </summary>
        public EnergyMeterManufacturer_Id?      EnergyMeterManufacturerId
            => Type == EnergyMeterManufacturerType ? EnergyMeterManufacturer_Id.Parse(ManufacturerId) : null;

        /// <summary>
        /// Itself in words, e.g. "the energy meter manufacturer 'SAALE'".
        /// </summary>
        public String                           Text
            => $"the {(Type == ChargingStationManufacturerType ? "charging station" : "energy meter")} manufacturer '{ManufacturerId}'";

        #endregion

        #region Constructor(s)

        private QIDigitalManufacturer(String                Type,
                                      String                ManufacturerId,
                                      IEnumerable<String>?  Scope)
        {

            var text   = $"the {(Type == ChargingStationManufacturerType ? "charging station" : "energy meter")} manufacturer '{ManufacturerId}'";
            var scope  = (Scope ?? []).ToImmutableArray();

            if (scope.IsEmpty)
                throw new ArgumentException($"A certificate of the quality system of {text} names at least one category of its scope!", nameof(Scope));

            if (scope.Any(category => String.IsNullOrWhiteSpace(category) || category != category.Trim()))
                throw new ArgumentException($"A category of the scope of {text} must neither be empty nor begin or end with whitespace!", nameof(Scope));

            if (scope.Distinct(StringComparer.Ordinal).Count() != scope.Length)
                throw new ArgumentException($"A category of the scope of {text} must not be given twice!", nameof(Scope));

            this.Type            = Type;
            this.ManufacturerId  = ManufacturerId;
            this.Scope           = scope;

        }

        #endregion


        #region (static) Of(ManufacturerId, Scope)

        /// <summary>
        /// The given charging station manufacturer, with the given scope of its quality system.
        /// </summary>
        public static QIDigitalManufacturer Of(ChargingStationManufacturer_Id  ManufacturerId,
                                               IEnumerable<String>             Scope)

            => new (ChargingStationManufacturerType, ManufacturerId.ToString(), Scope);


        /// <summary>
        /// The given energy meter manufacturer, with the given scope of its quality system.
        /// </summary>
        public static QIDigitalManufacturer Of(EnergyMeterManufacturer_Id  ManufacturerId,
                                               IEnumerable<String>         Scope)

            => new (EnergyMeterManufacturerType, ManufacturerId.ToString(), Scope);

        #endregion


        #region InScope(ItemType)

        /// <summary>
        /// Whether its scope takes the given kind of products, ChargingStation or EnergyMeter: as far as it can be told, a
        /// scope of well-known categories only takes the kinds they are for, any other scope every kind.
        /// </summary>
        /// <param name="ItemType">ChargingStation or EnergyMeter.</param>
        public Boolean InScope(String ItemType)

            => Scope.Any(category => Categories.KindOf(category) is not String kind || kind == ItemType);

        #endregion

        #region Makes(Model)

        /// <summary>
        /// Whether the given model is one of it: of the kind it is a manufacturer of, a charging station model of a
        /// charging station manufacturer, an energy meter model of an energy meter manufacturer, and its manufacturer it.
        /// </summary>
        /// <param name="Model">The model of a declaration or of another certificate of conformity.</param>
        public Boolean Makes(QIDigitalModel Model)

            => Model.Type == (Type == ChargingStationManufacturerType ? QIDigitalModel.ChargingStationModelType : QIDigitalModel.EnergyMeterModelType) &&
               QIDigitalModel.SameId(Model.ManufacturerId, ManufacturerId);

        #endregion

        #region TryCover(Item, out ErrorResponse)

        /// <summary>
        /// Whether its quality system covers the given charging station or energy meter, e.g. the item of a digital
        /// calibration certificate: one of it, of the kind it is a manufacturer of, and of a kind in its scope, as far as
        /// it can be told. Its model is the one of its declaration and of its type examination.
        /// </summary>
        /// <param name="Item">A charging station or an energy meter.</param>
        /// <param name="ErrorResponse">Why not.</param>
        public Boolean TryCover(QIDigitalItem                     Item,
                                [NotNullWhen(false)] out String?  ErrorResponse)
        {

            var itemType = Type == ChargingStationManufacturerType ? QIDigitalItem.ChargingStationType : QIDigitalItem.EnergyMeterType;

            if (Item.Type != itemType)
                ErrorResponse = $"The {Item.Text} is no {(itemType == QIDigitalItem.ChargingStationType ? "charging station" : "energy meter")}!";

            else if (!QIDigitalModel.SameId(Item.ManufacturerId, ManufacturerId))
                ErrorResponse = $"The {Item.Text} is none of '{ManufacturerId}'!";

            else if (!InScope(itemType))
                ErrorResponse = $"The {Item.Text} is of no category of the scope {String.Join(", ", Scope)} of the quality system of {Text}!";

            else
                ErrorResponse = null;

            return ErrorResponse is null;

        }

        #endregion

        #region TryVerify(RoamingNetwork, out ErrorResponse)

        /// <summary>
        /// Whether it is a manufacturer of the given roaming network, of the kind it names.
        /// </summary>
        public Boolean TryVerify(RoamingNetwork                    RoamingNetwork,
                                 [NotNullWhen(false)] out String?  ErrorResponse)
        {

            if (ChargingStationManufacturerId is ChargingStationManufacturer_Id chargingStationManufacturerId
                    ? !RoamingNetwork.TryGetChargingStationManufacturerById(chargingStationManufacturerId,    out _)
                    : !RoamingNetwork.TryGetEnergyMeterManufacturerById    (EnergyMeterManufacturerId!.Value, out _))
            {
                ErrorResponse = $"The {(Type == ChargingStationManufacturerType ? "charging station" : "energy meter")} manufacturer '{ManufacturerId}' is none of the roaming network '{RoamingNetwork.Id}'!";
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion


        #region (static) TryParse(JSON, out Manufacturer, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of the manufacturer of a certificate of conformity.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="Manufacturer">The parsed manufacturer.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                          JSON,
                                       [NotNullWhen(true)]  out QIDigitalManufacturer?  Manufacturer,
                                       [NotNullWhen(false)] out String?                 ErrorResponse)
        {

            Manufacturer = null;

            try
            {

                if (!QIDigitalJSON.TryOnly(JSON, "a manufacturer", out ErrorResponse,
                                           PropertyKeys.Type, PropertyKeys.ManufacturerId, PropertyKeys.Scope) ||
                    !POIJSON.TryParseStrings(JSON, PropertyKeys.Scope, "scope of a manufacturer", out var scope, out ErrorResponse))
                {
                    return false;
                }

                switch (QIDigitalJSON.Text(JSON, PropertyKeys.Type))
                {

                    case ChargingStationManufacturerType:

                        if (!QIDigitalJSON.TryParseRequiredText(JSON, PropertyKeys.ManufacturerId, "charging station manufacturer identification", ChargingStationManufacturer_Id.TryParse, out ChargingStationManufacturer_Id chargingStationManufacturerId, out ErrorResponse))
                            return false;

                        Manufacturer = Of(chargingStationManufacturerId, scope);
                        break;

                    case EnergyMeterManufacturerType:

                        if (!QIDigitalJSON.TryParseRequiredText(JSON, PropertyKeys.ManufacturerId, "energy meter manufacturer identification",     EnergyMeterManufacturer_Id.    TryParse, out EnergyMeterManufacturer_Id     energyMeterManufacturerId,     out ErrorResponse))
                            return false;

                        Manufacturer = Of(energyMeterManufacturerId, scope);
                        break;

                    default:
                        ErrorResponse = $"The type of a manufacturer must be '{ChargingStationManufacturerType}' or '{EnergyMeterManufacturerType}'!";
                        return false;

                }

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                Manufacturer   = null;
                ErrorResponse  = e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this manufacturer.
        /// </summary>
        public JObject ToJSON()

            => new (
                   new JProperty(PropertyKeys.Type,            Type),
                   new JProperty(PropertyKeys.ManufacturerId,  ManufacturerId),
                   new JProperty(PropertyKeys.Scope,           new JArray(Scope))
               );

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"{ManufacturerId} (scope {String.Join(", ", Scope)})";

        #endregion

    }

}
