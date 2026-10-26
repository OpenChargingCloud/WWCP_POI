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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Changes of the certificates embedded in the conformity assessment bodies issuing them.
    /// </summary>
    public sealed partial class RoamingNetwork
    {

        #region Data

        /// <summary>
        /// The properties of a certificate a change set can update or remove.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableConformityCertificateProperties = [
            ConformityCertificate.PropertyKeys.Legislation,
            ConformityCertificate.PropertyKeys.ChargingStationModel,
            ConformityCertificate.PropertyKeys.HardwareRevisions,
            ConformityCertificate.PropertyKeys.FirmwareVersions,
            ConformityCertificate.PropertyKeys.ChargingStationManufacturerId,
            ConformityCertificate.PropertyKeys.EnergyMeterModel,
            ConformityCertificate.PropertyKeys.EnergyMeterManufacturerId,
            ConformityCertificate.PropertyKeys.EnergyMeters,
            ConformityCertificate.PropertyKeys.DocumentNumber,
            ConformityCertificate.PropertyKeys.DocumentURL,
            ConformityCertificate.PropertyKeys.VerifiedTransparencySoftwareIds,
            ConformityCertificate.PropertyKeys.CompatibleTransparencySoftwareIds,
            ConformityCertificate.PropertyKeys.NotBefore,
            ConformityCertificate.PropertyKeys.NotAfter,
            ConformityCertificate.PropertyKeys.StatedBy
        ];

        #endregion


        #region (private) CertificateStatedByError(ChangeSet, Bodies)

        /// <summary>
        /// Why a certificate the given change set adds or changes does not say who states it, if one does not
        /// (SPECIFICATION.md, section 4.1): the roaming network operator, signing it in place of a conformity assessment body
        /// without signing keys in this version, names itself as its statedBy; a body signing it itself, with its keys, names
        /// none. So a change of a certificate of a body that got its keys since changes its statedBy as well, and the other way
        /// round.
        /// </summary>
        private String? CertificateStatedByError(RoamingNetworkChangeSet                                                     ChangeSet,
                                                 IReadOnlyDictionary<ConformityAssessmentBody_Id, ConformityAssessmentBody>  Bodies)
        {

            foreach (var change in ChangeSet.Changes)
            {

                if (change.EntityType != nameof(ConformityCertificate) || change.Kind == RoamingNetworkChangeKind.Remove ||
                    !ConformityCertificate_Id.TryParse(change.EntityId, out var certificateId) ||
                    ConformityAssessmentBodyOf(Bodies.Values, certificateId) is not ConformityAssessmentBody body ||
                    !body.TryGetConformityCertificate(certificateId, out var certificate))
                {
                    continue;
                }

                var byOperator = conformityAssessmentBodies.TryGet(body.Id, out var before) && before.SigningKeys.IsDefaultOrEmpty;

                if (certificate.StatedByRoamingNetworkOperator != byOperator)
                    return byOperator
                               ? $"The certificate '{certificate.Id}' of the conformity assessment body '{body.Id}' without signing keys is stated by the roaming network operator in its place: its statedBy is '{ConformityCertificate.RoamingNetworkOperator}'!"
                               : $"The certificate '{certificate.Id}' of the conformity assessment body '{body.Id}' is stated by the body itself, with its keys: it names no statedBy!";

            }

            return null;

        }

        #endregion


        #region (private static) TryApplyConformityCertificateChange(Change, Index, CreatedAt, AsHeld, Bodies, out CurrentValue, out HeldValue, out ErrorResponse)

        /// <summary>
        /// Apply a change of a certificate to the given conformity assessment bodies: an added one to the body
        /// its parent names, every other to the one having it.
        /// </summary>
        private static Boolean TryApplyConformityCertificateChange(RoamingNetworkChange                                                               Change,
                                                                  Int32                                                                              Index,
                                                                  DateTimeOffset                                                                     CreatedAt,
                                                                  Boolean                                                                            AsHeld,
                                                                  Dictionary<ConformityAssessmentBody_Id, ConformityAssessmentBody>                  Bodies,
                                                                  out JToken?                                                                        CurrentValue,
                                                                  out JToken?                                                                        HeldValue,
                                                                  [NotNullWhen(false)] out String?                                                   ErrorResponse)
        {

            CurrentValue = null;
            HeldValue    = null;

            var given    = Change;

            #region The conformity assessment body: the one having it, or the parent of a new one

            if (!ConformityCertificate_Id.TryParse(Change.EntityId, out var certificateId))
            {
                ErrorResponse = $"Change {Index}: invalid certificate identification '{Change.EntityId}'!";
                return false;
            }

            var body = ConformityAssessmentBodyOf(Bodies.Values, certificateId);

            if (Change.Kind == RoamingNetworkChangeKind.Add)
            {

                if (body is not null)
                {
                    ErrorResponse = $"Change {Index}: the certificate '{certificateId}' already exists, at the conformity assessment body '{body.Id}'!";
                    return false;
                }

                if (Change.ParentEntityType != nameof(ConformityAssessmentBody) ||
                    !ConformityAssessmentBody_Id.TryParse(Change.ParentEntityId ?? "", out var parentId) ||
                    !Bodies.TryGetValue(parentId, out body))
                {
                    ErrorResponse = $"Change {Index}: the certificate '{certificateId}' to add needs its ConformityAssessmentBody, an existing one!";
                    return false;
                }

            }

            else
            {

                if (body is null)
                {
                    ErrorResponse = $"Change {Index}: the certificate '{certificateId}' does not exist!";
                    return false;
                }

                if (Change.ParentEntityType is not null &&
                   (Change.ParentEntityType != nameof(ConformityAssessmentBody)                              ||
                    !ConformityAssessmentBody_Id.TryParse(Change.ParentEntityId ?? "", out var namedParentId) ||
                    namedParentId != body.Id))
                {
                    ErrorResponse = $"Change {Index}: the certificate '{certificateId}' belongs to ConformityAssessmentBody '{body.Id}', not to {Change.ParentEntityType} '{Change.ParentEntityId}'!";
                    return false;
                }

            }

            var bodyJSON          = body.ToJSON(Embedded: true);
            var certificates      = bodyJSON[ConformityAssessmentBody.PropertyKeys.Certificates] as JArray;
            // Found under the identification it is kept with: the change may name it in another spelling.
            var storedId          = body.TryGetConformityCertificate(certificateId, out var stored) ? stored.Id.ToString() : null;
            var certificateJSON       = Change.Kind == RoamingNetworkChangeKind.Add
                                        ? null
                                        : certificates?.OfType<JObject>().FirstOrDefault(certificate => certificate[ConformityCertificate.PropertyKeys.Id]?.Value<String>() == storedId);

            #endregion

            // An element change is applied as the change of the property it amounts to.
            var isElementChange = Change.IsElementChange;

            if (!TryResolveElementChange(ref Change, certificateJSON, "certificate", out var elementValue, out ErrorResponse))
            {
                ErrorResponse = $"Change {Index}: {ErrorResponse}";
                return false;
            }

            var isProperty = Change.Kind is RoamingNetworkChangeKind.UpdateProperty or RoamingNetworkChangeKind.RemoveProperty;

            // A property change compares the property, every other change the complete certificate.
            CurrentValue = isProperty
                               ? certificateJSON![Change.PropertyName!]?.DeepClone()
                               : certificateJSON?.DeepClone();

            if (Change.OldValue is JToken expected &&
                !OldValueMatches(expected, CurrentValue))
            {
                ErrorResponse = isProperty
                                    ? $"Change {Index}: the property '{Change.PropertyName}' of the certificate '{certificateId}' is no longer the expected old value, but '{POIJSON.ContentText(CurrentValue)}'!"
                                    : $"Change {Index}: the certificate '{certificateId}' is no longer the expected old value{(CurrentValue is null ? ": it does not exist" : "")}!";
                return false;
            }

            switch (Change.Kind)
            {

                #region UpdateProperty, RemoveProperty

                case RoamingNetworkChangeKind.UpdateProperty:
                case RoamingNetworkChangeKind.RemoveProperty:

                    var propertyName = Change.PropertyName!;

                    if (!ChangeableConformityCertificateProperties.Contains(propertyName))
                    {
                        ErrorResponse = $"Change {Index}: the property '{propertyName}' of a certificate can not be changed!";
                        return false;
                    }

                    if (Change.Kind == RoamingNetworkChangeKind.RemoveProperty)
                    {

                        if (CurrentValue is null)
                        {
                            ErrorResponse = $"Change {Index}: the property '{propertyName}' of the certificate '{certificateId}' does not exist!";
                            return false;
                        }

                        certificateJSON!.Remove(propertyName);

                    }
                    else
                        certificateJSON![propertyName] = Change.NewValue;

                    break;

                #endregion

                #region Add

                case RoamingNetworkChangeKind.Add:

                    if (Change.NewValue is not JObject newCertificate ||
                        newCertificate[ConformityCertificate.PropertyKeys.Id]?.Type != JTokenType.String ||
                        !ConformityCertificate_Id.TryParse(newCertificate[ConformityCertificate.PropertyKeys.Id]!.Value<String>()!, out var newId) ||
                        newId != certificateId)
                    {
                        ErrorResponse = $"Change {Index}: the certificate to add must be a JSON object with the identification '{certificateId}'!";
                        return false;
                    }

                    if (certificates is null)
                    {
                        certificates = [];
                        bodyJSON[ConformityAssessmentBody.PropertyKeys.Certificates] = certificates;
                    }

                    certificates.Add(newCertificate.DeepClone());
                    break;

                #endregion

                #region Remove

                case RoamingNetworkChangeKind.Remove:

                    certificates!.Remove(certificateJSON!);

                    if (certificates.Count == 0)
                        bodyJSON.Remove(ConformityAssessmentBody.PropertyKeys.Certificates);

                    break;

                #endregion

                default:
                    ErrorResponse = $"Change {Index}: {Change.Kind} of a certificate is not supported yet!";
                    return false;

            }

            // The conformity assessment body embedding it changed with it, at the time of the change set.
            bodyJSON[ConformityAssessmentBody.BasePropertyKeys.LastChange] = CreatedAt.ToISO8601();

            #region The changed conformity assessment body must still be a valid one

            if (!ConformityAssessmentBody.TryParse(bodyJSON, out var changedBody, out var changedBodyError))
            {
                ErrorResponse = $"Change {Index}: {changedBodyError}";
                return false;
            }

            // What is signed is what is held: the certificate as the body read again writes it (SPECIFICATION.md, section 4).
            if (!TryHeldAsGiven(given, () => FindHeld(changedBody.ToJSON(Embedded: true), nameof(ConformityAssessmentBody), given), "certificate", AsHeld, out HeldValue, out ErrorResponse))
            {
                ErrorResponse = $"Change {Index}: {ErrorResponse}";
                return false;
            }

            // Admin status, status and internal data are not content: they go along.
            changedBody.ShareRuntimeOf(body);

            Bodies[body.Id] = changedBody;

            #endregion

            // The old value of an element change is the value of what it addresses.
            if (isElementChange)
                CurrentValue = elementValue;

            ErrorResponse = null;
            return true;

        }

        #endregion

    }

}
