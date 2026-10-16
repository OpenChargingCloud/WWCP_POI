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
    /// A delegate verifying one peer signature of a change set with a trusted key.
    /// </summary>
    /// <param name="ChangeSet">The signed change set.</param>
    /// <param name="Signature">One of its signatures.</param>
    public delegate Boolean VerifyChangeSetSignatureDelegate(RoamingNetworkChangeSet           ChangeSet,
                                                             RoamingNetworkChangeSetSignature  Signature);


    public sealed partial class RoamingNetwork
    {

        #region Properties

        /// <summary>
        /// The number of change sets applied to reach this version, starting at 0.
        /// A local counter only: neither part of the JSON/CBOR representation nor of the ETags.
        /// A change set fits a version by its before ETags.
        /// </summary>
        public UInt64 Revision { get; private init; }

        #endregion


        #region (internal) WithRevision(Revision)

        /// <summary>
        /// This version with the given revision: itself, when it has it already,
        /// otherwise a copy sharing its entities and its runtime status, e.g. for the head of a snapshot.
        /// </summary>
        /// <param name="Revision">The revision.</param>
        internal RoamingNetwork WithRevision(UInt64 Revision)
        {

            if (Revision == this.Revision)
                return this;

            var roamingNetwork = new RoamingNetwork(
                                     Id,
                                     Name,
                                     Description,
                                     AdminStatus.Value,
                                     Status.Value,
                                     adminStatusSchedule.MaxStatusHistorySize,
                                     statusSchedule.     MaxStatusHistorySize,
                                     DataSource,
                                     Created,
                                     LastChangeDate,
                                     CustomData,
                                     InternalData,
                                     DataLicenses,
                                     chargingStationOperators,
                                     chargingStationManufacturers,
                                     eMobilityProviders,
                                     gridOperators,
                                     parkingOperators,
                                     transparencySoftware,
                                     transparencySoftwareCertificates,
                                     energyMeterManufacturers,
                                     SigningKeys,
                                     IdentityKeys
                                 ) {
                                     Revision = Revision
                                 };

            roamingNetwork.ShareRuntimeOf(this);

            return roamingNetwork;

        }

        #endregion


        #region CreateChangeSet  (Changes, Description = null, Metadata = null, Id = null, CreatedAt = null, Preconditions = null)

        /// <summary>
        /// Prepare an unsigned change set against this version: its before ETags are the ETags of this version,
        /// its after ETags the ETags of the result. A change without an old value (except Add and AddElement) gets the value
        /// it finds after the changes before it, so a change set prepared concurrently will not silently overwrite it.
        /// This version stays unchanged.
        /// </summary>
        /// <param name="Changes">The changes, applied in their order.</param>
        /// <param name="Description">An optional multi-language description.</param>
        /// <param name="Metadata">Optional application metadata.</param>
        /// <param name="Id">An optional change set identification, a random one by default.</param>
        /// <param name="CreatedAt">An optional commit timestamp, now by default.</param>
        /// <param name="Preconditions">Optional ETags entities must still have, e.g. Precondition(entityType, entityId) of the version a client read; they must hold in this version.</param>
        public RoamingNetworkChangeSet CreateChangeSet(IEnumerable<RoamingNetworkChange>         Changes,
                                                       ImmutableI18NString?                      Description     = null,
                                                       JObject?                                  Metadata        = null,
                                                       ChangeSet_Id?                             Id              = null,
                                                       DateTimeOffset?                           CreatedAt       = null,
                                                       IEnumerable<RoamingNetworkPrecondition>?  Preconditions   = null)
        {

            ImmutableArray<RoamingNetworkPrecondition> preconditions = [ .. Preconditions ?? [] ];

            if (!TryCheckPreconditions(preconditions, out var preconditionError))
                throw new ArgumentException(preconditionError, nameof(Preconditions));

            var beforeETags  = CalcETags();

            var pending      = new RoamingNetworkChangeSet(
                                   Id ?? ChangeSet_Id.NewRandom(),
                                   this.Id,
                                   CreatedAt ?? Timestamp.Now,
                                   [.. Changes],
                                   beforeETags,
                                   beforeETags,
                                   Description,
                                   Metadata,
                                   Preconditions: preconditions
                               );

            if (!TryApplyChanges(pending, out var result, out var changesWithOldValues, out var errorResponse))
                throw new ArgumentException(errorResponse, nameof(Changes));

            return new RoamingNetworkChangeSet(
                       pending.Id,
                       pending.RoamingNetworkId,
                       pending.CreatedAt,
                       changesWithOldValues,
                       pending.BeforeETags,
                       result.CalcETags(),
                       pending.Description,
                       pending.Metadata,
                       Preconditions: preconditions
                   );

        }

        #endregion

        #region Fits             (ChangeSet)

        /// <summary>
        /// Whether the given change set was prepared against this version:
        /// for this roaming network, with before ETags matching this version.
        /// </summary>
        /// <param name="ChangeSet">A change set.</param>
        public Boolean Fits(RoamingNetworkChangeSet ChangeSet)

            => ChangeSet is not null              &&
               ChangeSet.RoamingNetworkId == Id   &&
               MatchETags(ChangeSet.BeforeETags, this, out _);

        #endregion

        #region MatchesETags     (ETags)

        /// <summary>
        /// Whether at least one ETag is given and every given ETag is one of this version,
        /// calculated with its own format and hash algorithm.
        /// </summary>
        /// <param name="ETags">ETags.</param>
        public Boolean MatchesETags(ImmutableArray<ETag> ETags)

            => MatchETags(ETags, this, out _);

        #endregion

        #region ApplyChangeSet   (ChangeSet, VerifySignature = null)

        /// <summary>
        /// Apply all changes of the given change set to a new version of this roaming network.
        /// This version stays unchanged; nothing is changed when anything fails.
        /// The change set must fit this version by its before ETags,
        /// and the new version must have its after ETags.
        /// A signed change set requires a verifier accepting every signature.
        /// </summary>
        /// <param name="ChangeSet">The change set.</param>
        /// <param name="VerifySignature">A verifier of signatures using trusted keys, required for signed change sets.</param>
        public RoamingNetwork ApplyChangeSet(RoamingNetworkChangeSet            ChangeSet,
                                             VerifyChangeSetSignatureDelegate?  VerifySignature   = null)
        {

            if (TryApplyChangeSet(ChangeSet,
                                  out var roamingNetwork,
                                  out var errorResponse,
                                  VerifySignature))
            {
                return roamingNetwork;
            }

            throw new ArgumentException(errorResponse, nameof(ChangeSet));

        }

        #endregion

        #region TryApplyChangeSet(ChangeSet, out RoamingNetwork, out ErrorResponse, VerifySignature = null)

        /// <summary>
        /// Try to apply all changes of the given change set to a new version of this roaming network.
        /// This version stays unchanged; nothing is changed when anything fails.
        /// The change set must fit this version by its before ETags,
        /// and the new version must have its after ETags.
        /// A signed change set requires a verifier accepting every signature.
        /// </summary>
        /// <param name="ChangeSet">The change set.</param>
        /// <param name="RoamingNetwork">The new version of this roaming network.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="VerifySignature">A verifier of signatures using trusted keys, required for signed change sets.</param>
        public Boolean TryApplyChangeSet(RoamingNetworkChangeSet                   ChangeSet,
                                         [NotNullWhen(true)]  out RoamingNetwork?  RoamingNetwork,
                                         [NotNullWhen(false)] out String?          ErrorResponse,
                                         VerifyChangeSetSignatureDelegate?         VerifySignature   = null)
        {

            RoamingNetwork = null;

            try
            {

                if (ChangeSet is null)
                {
                    ErrorResponse = "The given change set must not be null!";
                    return false;
                }

                if (ChangeSet.RoamingNetworkId != Id)
                {
                    ErrorResponse = $"The change set is for the roaming network '{ChangeSet.RoamingNetworkId}', not for '{Id}'!";
                    return false;
                }

                #region Signatures: by the owners of everything it changes, and of the optional verifier

                if (!TryCheckOwnership(ChangeSet, out var ownershipError))
                {
                    ErrorResponse = ownershipError;
                    return false;
                }

                if (VerifySignature is not null)
                    for (var index = 0; index < ChangeSet.Signatures.Length; index++)
                        if (!VerifySignature(ChangeSet, ChangeSet.Signatures[index]))
                        {
                            ErrorResponse = $"Signature {index} ('{ChangeSet.Signatures[index].KeyId}') is invalid!";
                            return false;
                        }

                #endregion

                #region Before ETags

                if (!MatchETags(ChangeSet.BeforeETags, this,   out var beforeError))
                {
                    ErrorResponse = $"The change set does not fit this version of the roaming network: {beforeError}";
                    return false;
                }

                #endregion

                #region Preconditions

                if (!TryCheckPreconditions(ChangeSet.Preconditions, out var preconditionError))
                {
                    ErrorResponse = $"The change set does not fit this version of the roaming network: {preconditionError}";
                    return false;
                }

                #endregion

                if (!TryApplyChanges(ChangeSet, out var result, out _, out ErrorResponse))
                    return false;

                #region After ETags

                result.CalcETags();

                if (!MatchETags(ChangeSet.AfterETags,  result, out var afterError))
                {
                    ErrorResponse = $"The result does not match the change set: {afterError}";
                    return false;
                }

                #endregion

                RoamingNetwork = result;
                ErrorResponse  = null;
                return true;

            }
            catch (Exception e)
            {
                RoamingNetwork  = null;
                ErrorResponse   = e.Message;
                return false;
            }

        }

        #endregion


        #region Rebase           (ChangeSet, VerifySignature = null)

        /// <summary>
        /// Prepare the given change set, which was prepared against an older version, again against this version.
        /// See TryRebase.
        /// </summary>
        /// <param name="ChangeSet">The change set.</param>
        /// <param name="VerifySignature">A verifier of signatures using trusted keys, required for signed change sets.</param>
        public RoamingNetworkChangeSet Rebase(RoamingNetworkChangeSet            ChangeSet,
                                              VerifyChangeSetSignatureDelegate?  VerifySignature   = null)
        {

            if (TryRebase(ChangeSet,
                          out var rebasedChangeSet,
                          out var errorResponse,
                          VerifySignature))
            {
                return rebasedChangeSet;
            }

            throw new ArgumentException(errorResponse, nameof(ChangeSet));

        }

        #endregion

        #region TryRebase        (ChangeSet, out RebasedChangeSet, out ErrorResponse, VerifySignature = null)

        /// <summary>
        /// Try to prepare the given change set, which was prepared against an older version, again against this version.
        /// A change set that still fits this version is returned as it is.
        /// Otherwise every change must still find the old value it expects: a change set prepared concurrently
        /// may have changed other values, but not these. That is why every change except Add needs an old value;
        /// CreateChangeSet gives every change one.
        /// The rebased change set is a new, unsigned one: its signatures would no longer cover its ETags.
        /// It keeps the changes, description, metadata and timestamp, and names the original in its metadata
        /// as "rebasedFrom". A signed change set requires a verifier accepting every signature.
        /// </summary>
        /// <param name="ChangeSet">The change set.</param>
        /// <param name="RebasedChangeSet">The change set, fitting this version.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="VerifySignature">A verifier of signatures using trusted keys, required for signed change sets.</param>
        public Boolean TryRebase(RoamingNetworkChangeSet                            ChangeSet,
                                 [NotNullWhen(true)]  out RoamingNetworkChangeSet?  RebasedChangeSet,
                                 [NotNullWhen(false)] out String?                   ErrorResponse,
                                 VerifyChangeSetSignatureDelegate?                  VerifySignature   = null)
        {

            RebasedChangeSet = null;

            try
            {

                if (ChangeSet is null)
                {
                    ErrorResponse = "The given change set must not be null!";
                    return false;
                }

                if (ChangeSet.RoamingNetworkId != Id)
                {
                    ErrorResponse = $"The change set is for the roaming network '{ChangeSet.RoamingNetworkId}', not for '{Id}'!";
                    return false;
                }

                #region Signatures of the optional verifier: the rebased change set is unsigned and must be signed again

                if (VerifySignature is not null)
                    for (var index = 0; index < ChangeSet.Signatures.Length; index++)
                        if (!VerifySignature(ChangeSet, ChangeSet.Signatures[index]))
                        {
                            ErrorResponse = $"Signature {index} ('{ChangeSet.Signatures[index].KeyId}') is invalid!";
                            return false;
                        }

                #endregion

                // The entities it names must still be as it names them.
                if (!TryCheckPreconditions(ChangeSet.Preconditions, out var preconditionError))
                {
                    ErrorResponse = "The change set can not be rebased: " + preconditionError;
                    return false;
                }

                // Still fitting: nothing to do.
                if (MatchETags(ChangeSet.BeforeETags, this, out _))
                {
                    RebasedChangeSet  = ChangeSet;
                    ErrorResponse     = null;
                    return true;
                }

                for (var index = 0; index < ChangeSet.Changes.Length; index++)
                {
                    if (ChangeSet.Changes[index].OldValue is null &&
                        ChangeSet.Changes[index].Kind is not (RoamingNetworkChangeKind.Add or RoamingNetworkChangeKind.AddElement))
                    {
                        ErrorResponse = $"Change {index} has no old value, so a rebase could silently overwrite a concurrent change!";
                        return false;
                    }
                }

                var metadata     = ChangeSet.Metadata;
                var rebasedFrom  = metadata["rebasedFrom"] as JArray ?? [];

                rebasedFrom.Add(new JObject(
                                    new JProperty("@id",          ChangeSet.Id.ToString()),
                                    new JProperty("beforeETags",  new JArray(ChangeSet.BeforeETags.Select(eTag => eTag.ToJSON())))
                                ));

                metadata["rebasedFrom"] = rebasedFrom;

                var beforeETags  = CalcETags();

                var pending      = new RoamingNetworkChangeSet(
                                       ChangeSet_Id.NewRandom(),
                                       Id,
                                       ChangeSet.CreatedAt,
                                       ChangeSet.Changes,
                                       beforeETags,
                                       beforeETags,
                                       ChangeSet.Description,
                                       metadata,
                                       Preconditions: ChangeSet.Preconditions
                                   );

                if (!TryApplyChanges(pending, out var result, out _, out var applyError))
                {
                    ErrorResponse = "The change set can not be rebased: " + applyError;
                    return false;
                }

                RebasedChangeSet = new RoamingNetworkChangeSet(
                                       pending.Id,
                                       pending.RoamingNetworkId,
                                       pending.CreatedAt,
                                       pending.Changes,
                                       pending.BeforeETags,
                                       result.CalcETags(),
                                       pending.Description,
                                       pending.Metadata,
                                       Preconditions: pending.Preconditions
                                   );

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                RebasedChangeSet  = null;
                ErrorResponse     = e.Message;
                return false;
            }

        }

        #endregion


        #region (private) TryApplyChanges(ChangeSet, out RoamingNetwork, out ChangesWithOldValues, out ErrorResponse)

        /// <summary>
        /// Apply the changes to a new version, without checking signatures or ETags.
        /// Every change is checked against the state left by the changes before it.
        /// The changes are returned with their old values: a change without one
        /// gets the value it found, so the change set can detect a concurrent change later.
        /// </summary>
        private Boolean TryApplyChanges(RoamingNetworkChangeSet                                    ChangeSet,
                                        [NotNullWhen(true)]  out RoamingNetwork?                   RoamingNetwork,
                                        out ImmutableArray<RoamingNetworkChange>                   ChangesWithOldValues,
                                        [NotNullWhen(false)] out String?                           ErrorResponse)
        {

            RoamingNetwork        = null;
            ChangesWithOldValues  = [];

            if (ChangeSet.RoamingNetworkId != Id)
            {
                ErrorResponse = $"The change set is for the roaming network '{ChangeSet.RoamingNetworkId}', not for '{Id}'!";
                return false;
            }

            var name                  = Name;
            var description           = Description;
            var dataSource            = DataSource;
            var licenses              = DataLicenses.ToImmutableArray();
            var customData            = CustomData;
            var signingKeys           = SigningKeys;
            var identityKeys          = IdentityKeys;
            var operators             = new Dictionary<ChargingStationOperator_Id, ChargingStationOperator>(chargingStationOperators.Index);
            var manufacturers         = new Dictionary<ChargingStationManufacturer_Id, ChargingStationManufacturer>(chargingStationManufacturers.Index);
            var providers             = new Dictionary<EMobilityProvider_Id, EMobilityProvider>(eMobilityProviders.Index);
            var gridOps               = new Dictionary<GridOperator_Id, GridOperator>(gridOperators.Index);
            var parkingOps            = new Dictionary<ParkingOperator_Id, ParkingOperator>(parkingOperators.Index);
            var softwares             = new Dictionary<TransparencySoftware_Id, TransparencySoftware>(transparencySoftware.Index);
            var certificates          = new Dictionary<TransparencySoftwareCertificate_Id, TransparencySoftwareCertificate>(transparencySoftwareCertificates.Index);
            var meterManufacturers    = new Dictionary<EnergyMeterManufacturer_Id, EnergyMeterManufacturer>(energyMeterManufacturers.Index);
            var changesWithOldValues  = ImmutableArray.CreateBuilder<RoamingNetworkChange>(ChangeSet.Changes.Length);

            for (var index = 0; index < ChangeSet.Changes.Length; index++)
            {

                var      change        = ChangeSet.Changes[index];
                JToken?  currentValue;

                #region Charging station operators

                if (change.EntityType == nameof(ChargingStationOperator))
                {

                    if (change.ParentEntityType is not null &&
                       (change.ParentEntityType != nameof(POI.RoamingNetwork) || !IsThisRoamingNetwork(change.ParentEntityId)))
                    {
                        ErrorResponse = $"Change {index}: a charging station operator of this roaming network belongs to RoamingNetwork '{Id}', not to {change.ParentEntityType} '{change.ParentEntityId}'!";
                        return false;
                    }

                    if (!ChargingStationOperator_Id.TryParse(change.EntityId, out var operatorId))
                    {
                        ErrorResponse = $"Change {index}: invalid charging station operator identification '{change.EntityId}'!";
                        return false;
                    }

                    operators.TryGetValue(operatorId, out var currentOperator);

                    var currentOperatorJSON  = currentOperator?.ToEmbeddedJSON();

                    // An element change is applied as the change of the property it amounts to.
                    var applied              = change;

                    if (!TryResolveElementChange(ref applied, currentOperatorJSON, "charging station operator", out var operatorElementValue, out var operatorElementError))
                    {
                        ErrorResponse = $"Change {index}: {operatorElementError}";
                        return false;
                    }

                    var operatorProperty     = applied.Kind is RoamingNetworkChangeKind.UpdateProperty or RoamingNetworkChangeKind.RemoveProperty;

                    if (operatorProperty && currentOperatorJSON is null)
                    {
                        ErrorResponse = $"Change {index}: the charging station operator '{operatorId}' does not exist!";
                        return false;
                    }

                    // A property change compares the property, every other change the complete operator.
                    currentValue = operatorProperty
                                       ? currentOperatorJSON![applied.PropertyName!]?.DeepClone()
                                       : currentOperatorJSON;

                    if (applied.OldValue is JToken expectedOperator &&
                        !OldValueMatches(expectedOperator, currentValue))
                    {
                        ErrorResponse = operatorProperty
                                            ? $"Change {index}: the property '{applied.PropertyName}' of the charging station operator '{operatorId}' is no longer the expected old value, but '{currentValue?.ToString(Newtonsoft.Json.Formatting.None) ?? "absent"}'!"
                                            : $"Change {index}: the charging station operator '{operatorId}' is no longer the expected old value{(currentValue is null ? ": it does not exist" : "")}!";
                        return false;
                    }

                    switch (applied.Kind)
                    {

                        #region UpdateProperty, RemoveProperty

                        case RoamingNetworkChangeKind.UpdateProperty:
                        case RoamingNetworkChangeKind.RemoveProperty:

                            var propertyName = applied.PropertyName!;

                            if (!ChangeableOperatorProperties.Contains(propertyName))
                            {
                                ErrorResponse = $"Change {index}: the property '{propertyName}' of a charging station operator can not be changed!";
                                return false;
                            }

                            if (applied.Kind == RoamingNetworkChangeKind.RemoveProperty)
                            {

                                if (propertyName == "name")
                                {
                                    ErrorResponse = $"Change {index}: the name of a charging station operator can not be removed, only changed!";
                                    return false;
                                }

                                if (currentValue is null)
                                {
                                    ErrorResponse = $"Change {index}: the property '{propertyName}' of the charging station operator '{operatorId}' does not exist!";
                                    return false;
                                }

                                currentOperatorJSON!.Remove(propertyName);

                            }
                            else
                                currentOperatorJSON![propertyName] = applied.NewValue;

                            // The change set's timestamp is the last change, so that its after ETags are deterministic.
                            currentOperatorJSON!["lastChange"] = ChangeSet.CreatedAt.ToISO8601();

                            // The changed operator must still be a valid one.
                            if (!ChargingStationOperator.TryParse(currentOperatorJSON, out var changedOperator, out var changedOperatorError))
                            {
                                ErrorResponse = $"Change {index}: {changedOperatorError}";
                                return false;
                            }

                            // Its admin status, status and internal data are not content: they go along,
                            // as those of its charging pools, charging stations and EVSEs.
                            changedOperator.ShareRuntimeOfAll(currentOperator!);

                            operators[operatorId] = changedOperator;
                            break;

                        #endregion


                        case RoamingNetworkChangeKind.Add:

                            if (currentOperator is not null)
                            {
                                ErrorResponse = $"Change {index}: the charging station operator '{operatorId}' already exists!";
                                return false;
                            }

                            // The change set's timestamp is the last change and, unless given, the creation,
                            // so that its after ETags are deterministic.
                            var newOperatorJSON = (JObject) applied.NewValue!;
                            newOperatorJSON["created"]    ??= ChangeSet.CreatedAt.ToISO8601();
                            newOperatorJSON["lastChange"]   = ChangeSet.CreatedAt.ToISO8601();

                            if (!ChargingStationOperator.TryParse(newOperatorJSON, out var newOperator, out var newOperatorError))
                            {
                                ErrorResponse = $"Change {index}: {newOperatorError}";
                                return false;
                            }

                            if (newOperator.Id != operatorId)
                            {
                                ErrorResponse = $"Change {index}: the charging station operator '{newOperator.Id}' is not the one to add, '{operatorId}'!";
                                return false;
                            }

                            operators.Add(operatorId, newOperator);
                            break;

                        case RoamingNetworkChangeKind.Remove:

                            if (currentOperator is null)
                            {
                                ErrorResponse = $"Change {index}: the charging station operator '{operatorId}' does not exist!";
                                return false;
                            }

                            operators.Remove(operatorId);
                            break;

                        default:
                            ErrorResponse = $"Change {index}: {applied.Kind} of a charging station operator is not supported yet!";
                            return false;

                    }

                    // The old value of an element change is the value of what it addresses.
                    if (change.IsElementChange)
                        currentValue = operatorElementValue;

                }

                #endregion

                #region The roaming network itself

                else if (change.EntityType == nameof(POI.RoamingNetwork) &&
                         IsThisRoamingNetwork(change.EntityId)           &&
                         change.ParentEntityType is null)
                {

                    // An element change is applied as the change of the property it amounts to.
                    var applied       = change;
                    JToken? networkElementValue = null;

                    if (change.IsElementChange)
                    {

                        var networkJSON = new JObject();

                        if (CurrentValue(change.EntityPropertyName!, name, description, dataSource, licenses, customData, signingKeys, identityKeys) is JToken networkPropertyValue)
                            networkJSON[change.EntityPropertyName!] = networkPropertyValue;

                        if (!TryResolveElementChange(ref applied, networkJSON, "roaming network", out networkElementValue, out var networkElementError))
                        {
                            ErrorResponse = $"Change {index}: {networkElementError}";
                            return false;
                        }

                    }

                    var propertyName  = applied.PropertyName ?? "";
                    currentValue      = CurrentValue(propertyName, name, description, dataSource, licenses, customData, signingKeys, identityKeys);

                    if (applied.OldValue is JToken oldValue &&
                        !OldValueMatches(oldValue, currentValue))
                    {
                        ErrorResponse = $"Change {index}: the property '{propertyName}' is no longer the expected old value, but '{currentValue?.ToString(Newtonsoft.Json.Formatting.None) ?? "absent"}'!";
                        return false;
                    }

                    switch (applied.Kind)
                    {

                        #region UpdateProperty

                        case RoamingNetworkChangeKind.UpdateProperty:

                            var newValue = applied.NewValue!;

                            switch (propertyName)
                            {

                                case "name":
                                    if (!TryParseI18N(newValue, out var newName, out var nameError))
                                    {
                                        ErrorResponse = $"Change {index}: invalid name: {nameError}";
                                        return false;
                                    }
                                    name = newName;
                                    break;

                                case "description":
                                    if (!TryParseI18N(newValue, out var newDescription, out var descriptionError))
                                    {
                                        ErrorResponse = $"Change {index}: invalid description: {descriptionError}";
                                        return false;
                                    }
                                    description = newDescription;
                                    break;

                                case "dataSource":
                                    if (newValue.Type != JTokenType.String || String.IsNullOrWhiteSpace(newValue.Value<String>()))
                                    {
                                        ErrorResponse = $"Change {index}: the data source must be a nonempty text!";
                                        return false;
                                    }
                                    dataSource = newValue.Value<String>();
                                    break;

                                case "dataLicenses":
                                    if (!POIDataLicenses.TryParse(newValue, null, out var newLicenses, out var licensesError))
                                    {
                                        ErrorResponse = $"Change {index}: {licensesError}";
                                        return false;
                                    }
                                    licenses = newLicenses;
                                    break;

                                case "customData":
                                    if (newValue is not JObject newCustomData)
                                    {
                                        ErrorResponse = $"Change {index}: the custom data must be a JSON object!";
                                        return false;
                                    }
                                    customData = CustomDataNew.ParseJObject(newCustomData);
                                    break;

                                case POIPartyKeys.SigningKeysProperty:
                                case POIPartyKeys.IdentityKeysProperty:
                                    if (!POIPartyKeys.TryParse(new JObject(new JProperty(propertyName, newValue)), out var newSigningKeys, out var newIdentityKeys, out var keysError))
                                    {
                                        ErrorResponse = $"Change {index}: {keysError}";
                                        return false;
                                    }
                                    if (propertyName == POIPartyKeys.SigningKeysProperty)
                                        signingKeys  = POIPublicKey.Ordered(newSigningKeys,  $"signing key of the roaming network '{Id}'");
                                    else
                                        identityKeys = POIPublicKey.Ordered(newIdentityKeys, $"identity key of the roaming network '{Id}'");
                                    break;

                                default:
                                    ErrorResponse = $"Change {index}: the property '{propertyName}' of a roaming network can not be changed!";
                                    return false;

                            }

                            break;

                        #endregion

                        #region RemoveProperty

                        case RoamingNetworkChangeKind.RemoveProperty:

                            if (currentValue is null)
                            {
                                ErrorResponse = $"Change {index}: the property '{propertyName}' does not exist!";
                                return false;
                            }

                            switch (propertyName)
                            {

                                case "name":
                                    name         = ImmutableI18NString.Empty;
                                    break;

                                case "description":
                                    description  = ImmutableI18NString.Empty;
                                    break;

                                case "dataSource":
                                    dataSource   = null;
                                    break;

                                case "dataLicenses":
                                    licenses     = [];
                                    break;

                                case "customData":
                                    customData   = CustomDataNew.Empty;
                                    break;

                                default:
                                    ErrorResponse = $"Change {index}: the property '{propertyName}' of a roaming network can not be removed!";
                                    return false;

                            }

                            break;

                        #endregion

                        default:
                            ErrorResponse = $"Change {index}: {applied.Kind} of the roaming network itself is not supported yet!";
                            return false;

                    }

                    // The old value of an element change is the value of what it addresses.
                    if (change.IsElementChange)
                        currentValue = networkElementValue;

                }

                #endregion

                #region Charging station manufacturers, e-mobility providers, grid operators

                else if (change.EntityType == nameof(ChargingStationManufacturer))
                {
                    if (!TryApplyRegistryChange(ManufacturerRegistry,      change, index, ChangeSet.CreatedAt, manufacturers, out currentValue, out ErrorResponse))
                        return false;
                }

                else if (change.EntityType == nameof(EMobilityProvider))
                {
                    if (!TryApplyRegistryChange(EMobilityProviderRegistry, change, index, ChangeSet.CreatedAt, providers,     out currentValue, out ErrorResponse))
                        return false;
                }

                else if (change.EntityType == nameof(GridOperator))
                {
                    if (!TryApplyRegistryChange(GridOperatorRegistry,      change, index, ChangeSet.CreatedAt, gridOps,       out currentValue, out ErrorResponse))
                        return false;
                }

                else if (change.EntityType == nameof(GridConnectionPoint))
                {
                    if (!TryApplyGridConnectionPointChange(change, index, ChangeSet.CreatedAt, gridOps, out currentValue, out ErrorResponse))
                        return false;
                }

                #endregion

                #region Parking operators, parking garages, parking spaces and parking sensors

                else if (change.EntityType == nameof(ParkingOperator))
                {
                    if (!TryApplyRegistryChange(ParkingOperatorRegistry,   change, index, ChangeSet.CreatedAt, parkingOps,    out currentValue, out ErrorResponse))
                        return false;
                }

                else if (IsParking(change.EntityType))
                {
                    if (!TryApplyParkingChange(change, index, ChangeSet.CreatedAt, parkingOps, operators, out currentValue, out ErrorResponse))
                        return false;
                }

                #endregion

                #region Energy meter manufacturers

                else if (change.EntityType == nameof(EnergyMeterManufacturer))
                {
                    if (!TryApplyRegistryChange(EnergyMeterManufacturerRegistry, change, index, ChangeSet.CreatedAt, meterManufacturers, out currentValue, out ErrorResponse))
                        return false;
                }

                #endregion

                #region Transparency software releases and certificates

                else if (change.EntityType == nameof(TransparencySoftware))
                {
                    if (!TryApplyRegistryChange(TransparencySoftwareRegistry,            change, index, ChangeSet.CreatedAt, softwares,    out currentValue, out ErrorResponse))
                        return false;
                }

                else if (change.EntityType == nameof(TransparencySoftwareCertificate))
                {
                    if (!TryApplyRegistryChange(TransparencySoftwareCertificateRegistry, change, index, ChangeSet.CreatedAt, certificates, out currentValue, out ErrorResponse))
                        return false;
                }

                #endregion

                #region Charging pools, charging stations and EVSEs

                else if (IsInfrastructure(change.EntityType))
                {
                    if (!TryApplyInfrastructureChange(change, index, ChangeSet.CreatedAt, operators, out currentValue, out ErrorResponse))
                        return false;
                }

                #endregion

                else
                {
                    ErrorResponse = $"Change {index}: changes of {change.EntityType} '{change.EntityId}' are not supported yet!";
                    return false;
                }

                #region Remember the old value

                // Add and AddElement have no old value; every other change without one gets the value it found,
                // or JSON null when it found none: then the value is expected to be absent.
                changesWithOldValues.Add(change.OldValue is null && change.Kind is not (RoamingNetworkChangeKind.Add or RoamingNetworkChangeKind.AddElement)

                                             ? new RoamingNetworkChange(
                                                   change.Kind,
                                                   change.EntityType,
                                                   change.EntityId,
                                                   change.PropertyName,
                                                   currentValue ?? JValue.CreateNull(),
                                                   change.NewValue,
                                                   change.ParentEntityType,
                                                   change.ParentEntityId,
                                                   change.ElementPath
                                               )

                                             : change);

                #endregion

            }

            // Every manufacturer a charging station names, every grid connection point a charging pool names,
            // and every maintainer of a grid connection point must still be one of the network;
            // grid connection points and energy meters must be unique within the network.
            var allEnergyMeters = AllEnergyMeters(operators.Values, gridOps.Values).ToArray();

            if ((UnknownManufacturer (operators.Values, manufacturers) ??
                 DuplicateGridConnectionPoint(gridOps.Values)          ??
                 UnknownGridConnectionPoint(operators.Values, gridOps.Values) ??
                 UnknownMaintainer(gridOps.Values, operators, parkingOps) ??
                 DuplicateEnergyMeter(allEnergyMeters)                 ??
                 DuplicateParking    (parkingOps.Values, operators.Values) ??
                 UnknownChargingStation(parkingOps.Values, operators)  ??
                 UnknownParkingSpace(operators.Values, parkingOps.Values) ??
                 UnknownTransparencyReference(allEnergyMeters, softwares, certificates, manufacturers) ??
                 UnknownEnergyMeterManufacturer(allEnergyMeters, meterManufacturers)) is String referenceError)
            {
                ErrorResponse = referenceError;
                return false;
            }

            RoamingNetwork = new RoamingNetwork(
                                 Id,
                                 name,
                                 description,
                                 AdminStatus.Value,
                                 Status.Value,
                                 adminStatusSchedule.MaxStatusHistorySize,
                                 statusSchedule.     MaxStatusHistorySize,
                                 dataSource,
                                 Created,
                                 ChangeSet.CreatedAt,
                                 customData,
                                 InternalData,
                                 licenses,
                                 operators.Values,
                                 manufacturers.Values,
                                 providers.Values,
                                 gridOps.Values,
                                 parkingOps.Values,
                                 softwares.Values,
                                 certificates.Values,
                                 meterManufacturers.Values,
                                 signingKeys,
                                 identityKeys
                             ) {
                                 Revision = Revision + 1
                             };

            // Its admin status, status and internal data are not content: they go along.
            RoamingNetwork.ShareRuntimeOf(this);

            ChangesWithOldValues  = changesWithOldValues.MoveToImmutable();
            ErrorResponse         = null;
            return true;

        }

        #endregion

        #region (private static) OldValueMatches(OldValue, CurrentValue)

        /// <summary>
        /// Whether the current value is the expected old value:
        /// a JSON null expects the value to be absent.
        /// </summary>
        private static Boolean OldValueMatches(JToken   OldValue,
                                               JToken?  CurrentValue)

            => OldValue.Type == JTokenType.Null
                   ? CurrentValue is null
                   : JToken.DeepEquals(OldValue, CurrentValue);

        #endregion

        #region (private static) ChangeableOperatorProperties

        /// <summary>
        /// The properties of a charging station operator a change set can update or remove,
        /// as named in its embedded JSON representation. Its identification can not be changed.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableOperatorProperties = [
            "name",
            "description",
            "dataSource",
            "dataLicenses",
            "address",
            "geoLocation",
            "logos",
            "telephone",
            "eMailAddress",
            "homepage",
            "hotline",
            "termsAndConditions",
            "brands",
            "signingKeys",
            "identityKeys",
            "customData"
        ];

        #endregion

        #region (private static) CurrentValue (PropertyName, Name, Description, DataSource)

        /// <summary>
        /// The JSON value of the given property, as ToJSON() writes it, or null when it is absent.
        /// </summary>
        private static JToken? CurrentValue(String                     PropertyName,
                                            ImmutableI18NString        Name,
                                            ImmutableI18NString        Description,
                                            String?                    DataSource,
                                            ImmutableArray<DataLicense>  DataLicenses,
                                            CustomDataNew              CustomData,
                                            ImmutableArray<POIPublicKey>  SigningKeys,
                                            ImmutableArray<POIPublicKey>  IdentityKeys)

            => PropertyName switch {
                   "name"          => Name.       IsNotNullOrEmpty()       ? Name.       ToJSON()      : null,
                   "description"   => Description.IsNotNullOrEmpty()       ? Description.ToJSON()      : null,
                   "dataSource"    => DataSource.IsNeitherNullNorEmpty()   ? DataSource                : null,
                   "dataLicenses"  => DataLicenses.Length > 0              ? DataLicenses.ToPOIJSON()  : null,
                   "customData"    => CustomData.HasValues                 ? CustomData.ToJObject()    : null,
                   POIPartyKeys.SigningKeysProperty   => SigningKeys. Length > 0  ? new JArray(SigningKeys. Select(key => key.ToJSON())) : null,
                   POIPartyKeys.IdentityKeysProperty  => IdentityKeys.Length > 0  ? new JArray(IdentityKeys.Select(key => key.ToJSON())) : null,
                   _               => null
               };

        #endregion

        #region (private static) TryParseI18N (JSON, out I18NText, out ErrorResponse)

        private static Boolean TryParseI18N(JToken                                        JSON,
                                            [NotNullWhen(true)]  out ImmutableI18NString?  I18NText,
                                            [NotNullWhen(false)] out String?               ErrorResponse)
        {

            if (JSON is not JObject jsonObject)
            {
                I18NText       = null;
                ErrorResponse  = "A multi-language text must be a JSON object!";
                return false;
            }

            return ImmutableI18NString.TryParse(jsonObject,
                                                out I18NText,
                                                out ErrorResponse);

        }

        #endregion

        #region (private static) MatchETags   (Declared, RoamingNetwork, out ErrorResponse)

        /// <summary>
        /// At least one ETag must be declared, and every declared ETag must equal the ETag
        /// calculated with its own format and hash algorithm over the given roaming network.
        /// Formats not declared are not checked: JSON and CBOR carry the same content.
        /// </summary>
        private static Boolean MatchETags(ImmutableArray<ETag>              Declared,
                                          RoamingNetwork                    RoamingNetwork,
                                          [NotNullWhen(false)] out String?  ErrorResponse)
        {

            if (Declared.IsDefaultOrEmpty)
            {
                ErrorResponse = "No ETags were declared!";
                return false;
            }

            foreach (var declared in Declared)
            {

                var calculated = ETag.Compute(declared.Format,
                                              declared.HashAlgorithm,
                                              RoamingNetwork.CanonicalContent(declared.Format));

                if (!calculated.Equals(declared))
                {
                    ErrorResponse = $"The {declared.Format} ETag ({declared.HashAlgorithm}) differs!";
                    return false;
                }

            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region (private) CanonicalContent(Format)

        /// <summary>
        /// The canonical bytes the ETags of the given format are calculated over:
        /// canonical JSON or canonical CBOR, both without ETags.
        /// </summary>
        /// <param name="Format">The format.</param>
        private Byte[] CanonicalContent(ETagFormat Format)

            => Format switch {
                   ETagFormat.JSON  => CanonicalJSON.ToUTF8Bytes(ToJSON(IncludeETags: false)),
                   ETagFormat.CBOR  => ToCBOR(IncludeETags: false).ToByteArray(CBORWriterOptions.Canonical),
                   _                => throw new ArgumentOutOfRangeException(nameof(Format), "Unsupported ETag format!")
               };

        #endregion

    }

}
