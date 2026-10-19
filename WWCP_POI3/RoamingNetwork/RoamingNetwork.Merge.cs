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
    /// Why two versions of a roaming network can not be merged.
    /// </summary>
    public enum RoamingNetworkMergeConflictKind
    {

        /// <summary>
        /// Both changed a property differently.
        /// </summary>
        DifferentValues,

        /// <summary>
        /// Both added an entity with different properties.
        /// </summary>
        AddAdd,

        /// <summary>
        /// One removed an entity the other changed, or added something to.
        /// </summary>
        DeleteModify,

        /// <summary>
        /// One moved an entity to another parent, the other changed it or moved it elsewhere.
        /// </summary>
        Moved,

        /// <summary>
        /// The merge is invalid as a whole, e.g. it names an entity one of them removed.
        /// </summary>
        InvalidResult,

        /// <summary>
        /// A change set of one names an entity as it must still be, by a precondition, and the other changed or removed it.
        /// </summary>
        Precondition

    }


    /// <summary>
    /// A conflict of two versions of a roaming network: what their common ancestor, the left and the right version
    /// have at one place. The owner resolves it with a change set on one of them, e.g. taking the value of the other.
    /// </summary>
    /// <param name="Kind">Why they conflict.</param>
    /// <param name="EntityType">The type of the entity.</param>
    /// <param name="EntityId">The identification of the entity.</param>
    /// <param name="PropertyName">The property, unless the entity conflicts as a whole.</param>
    /// <param name="Base">The value of the common ancestor; null when absent. Where several best common ancestors conflict,
    /// a conflict marker, { "@conflict": { base?, left?, right? } }, with what they have.</param>
    /// <param name="Left">The value of the left version; null when absent.</param>
    /// <param name="Right">The value of the right version; null when absent.</param>
    /// <param name="Description">A description.</param>
    public sealed record RoamingNetworkMergeConflict(RoamingNetworkMergeConflictKind  Kind,
                                                     String                           EntityType,
                                                     String                           EntityId,
                                                     String?                          PropertyName,
                                                     JToken?                          Base,
                                                     JToken?                          Left,
                                                     JToken?                          Right,
                                                     String                           Description)
    {

        /// <summary>
        /// Return the JSON representation of this conflict.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty("kind",        Kind.ToString()),
                           new JProperty("entityType",  EntityType),
                           new JProperty("entityId",    EntityId),
                           new JProperty("description", Description)
                       );

            if (PropertyName is not null)
                json.Add("propertyName", PropertyName);

            if (Base  is not null)
                json.Add("base",   Base. DeepClone());

            if (Left  is not null)
                json.Add("left",   Left. DeepClone());

            if (Right is not null)
                json.Add("right",  Right.DeepClone());

            return json;

        }

    }


    /// <summary>
    /// The deterministic three-way merge of two versions of a roaming network with their common ancestor.
    /// </summary>
    /// <remarks>
    /// Every entity a change set can address is compared on its own, by its type and identification, property by property,
    /// without its embedded children and without its last change: a property only one of them changed takes that change,
    /// a property both changed alike takes it once, and a property both changed differently conflicts.
    /// An entity only one of them added or removed is added or removed, unless the other one changed it.
    /// The merge is written as a change set leading from the left version to the merge, applied without signatures:
    /// whoever has the three versions calculates the very same change set, and so the very same merge commit.
    /// </remarks>
    public sealed partial class RoamingNetwork
    {

        #region (internal) MergeEntity

        /// <summary>
        /// The key of an entity, as the merge finds it in every version: its entity type and its identification,
        /// compared as its identification type compares them, e.g. an e-mobility provider 'DE-ABC' as 'DE*ABC',
        /// most without case; a text its type does not read, ordinally.
        /// </summary>
        /// <param name="Type">The entity type.</param>
        /// <param name="Id">The typed identification, or the text its type does not read.</param>
        internal readonly record struct MergeKey(String  Type,
                                                 Object  Id)
        {

            /// <summary>
            /// The key of the entity of the given type with the given identification.
            /// </summary>
            /// <param name="Type">An entity type.</param>
            /// <param name="Id">The text of its identification.</param>
            internal static MergeKey Of(String  Type,
                                        String  Id)

                => new (Type, TypedId(Type, Id) ?? Id);

        }

        /// <summary>
        /// An entity of a version, as the merge compares it.
        /// </summary>
        /// <param name="Type">Its entity type.</param>
        /// <param name="Id">Its identification, as this version writes it.</param>
        /// <param name="Parent">The key of the entity embedding it, unless it is the roaming network itself.</param>
        /// <param name="Own">Its properties, without its identification, its embedded children and its last change.</param>
        /// <param name="Full">Its complete JSON representation, with its embedded children.</param>
        internal sealed record MergeEntity(String                            Type,
                                           String                            Id,
                                           MergeKey?                         Parent,
                                           JObject                           Own,
                                           JObject                           Full)
        {

            /// <summary>
            /// Its key, as the merge finds it in every version.
            /// </summary>
            internal MergeKey Key { get; } = MergeKey.Of(Type, Id);

        }

        #endregion

        #region (internal) MergeBase

        /// <summary>
        /// The merge base of two versions, as the merge compares it: the version of their best common ancestor,
        /// or a virtual one, several best common ancestors merged one after the other.
        /// </summary>
        /// <remarks>
        /// A virtual merge base never conflicts: where the common ancestors conflict, it has a conflict marker
        /// instead, { "@conflict": { base?, left?, right? } }, as a value or as the only property of an entity.
        /// It equals no value of a version, so that the merge conflicts there, unless both versions have the same.
        /// </remarks>
        internal sealed class MergeBase
        {

            /// <summary>
            /// Its entities, by their type and identification.
            /// </summary>
            internal Dictionary<MergeKey, MergeEntity>  Entities  { get; }

            private MergeBase(Dictionary<MergeKey, MergeEntity> Entities)
            {
                this.Entities = Entities;
            }

            /// <summary>
            /// The given version as a merge base.
            /// </summary>
            /// <param name="Version">The version of the best common ancestor.</param>
            internal static MergeBase Of(RoamingNetwork Version)

                => new (Flatten(Version));

            /// <summary>
            /// The virtual merge base of the left and the right one, merged with their own merge base, without conflicts.
            /// </summary>
            /// <param name="Base">The merge base of the left and the right one.</param>
            /// <param name="Left">A common ancestor, or the virtual merge base of the ones before.</param>
            /// <param name="Right">The next common ancestor.</param>
            internal static MergeBase Virtual(MergeBase  Base,
                                              MergeBase  Left,
                                              MergeBase  Right)

                => new (MergeVirtual(Base.Entities, Left.Entities, Right.Entities));

        }

        #endregion

        #region Data

        /// <summary>
        /// The property of a conflict marker of a virtual merge base: where its common ancestors conflict.
        /// </summary>
        public const String ConflictMarkerProperty = "@conflict";

        /// <summary>
        /// The embedded children a change set addresses on their own: their JSON key and entity type, by the entity type embedding them.
        /// A key holds an array of them, or, as the roaming network operator, one.
        /// </summary>
        private static readonly Dictionary<String, (String Key, String Type)[]> MergeChildren = new () {
            { nameof(RoamingNetwork),           [ (RoamingNetworkOperator.JSONProperty, nameof(RoamingNetworkOperator)),
                                                  ("chargingStationOperators",          nameof(ChargingStationOperator)),
                                                  ("chargingStationManufacturers",      nameof(ChargingStationManufacturer)),
                                                  ("eMobilityProviders",                nameof(EMobilityProvider)),
                                                  ("gridOperators",                     nameof(GridOperator)),
                                                  ("parkingOperators",                  nameof(ParkingOperator)),
                                                  ("energyMeterManufacturers",          nameof(EnergyMeterManufacturer)),
                                                  ("transparencySoftware",              nameof(TransparencySoftware)),
                                                  ("transparencySoftwareCertificates",  nameof(TransparencySoftwareCertificate)) ] },
            { nameof(ChargingStationOperator),  [ ("chargingTariffTemplates",           nameof(ChargingTariffTemplate)),
                                                  ("chargingTariffs",                   nameof(ChargingTariff)),
                                                  ("EVSETemplates",                     nameof(EVSETemplate)),
                                                  ("chargingStationTemplates",          nameof(ChargingStationTemplate)),
                                                  ("chargingPoolTemplates",             nameof(ChargingPoolTemplate)),
                                                  ("energyMeterTemplates",              nameof(EnergyMeterTemplate)),
                                                  ("chargingPools",                     nameof(ChargingPool)),
                                                  ("EVSEGroups",                        nameof(EVSEGroup)),
                                                  ("chargingStationGroups",             nameof(ChargingStationGroup)),
                                                  ("chargingPoolGroups",                nameof(ChargingPoolGroup)),
                                                  ("chargingTariffGroups",              nameof(ChargingTariffGroup)),
                                                  ("energyMeterGroups",                 nameof(EnergyMeterGroup)),
                                                  ("chargingTariffOffers",              nameof(ChargingTariffOffer)),
                                                  ("parkingSpaces",                     nameof(ParkingSpace)) ] },
            { nameof(ChargingPool),             [ ("chargingStations",                  nameof(ChargingStation)) ] },
            { nameof(ChargingStation),          [ ("EVSEs",                             nameof(EVSE)) ] },
            { nameof(EMobilityProvider),        [ ("EVSEGroups",                        nameof(EVSEGroup)),
                                                  ("chargingStationGroups",             nameof(ChargingStationGroup)),
                                                  ("chargingPoolGroups",                nameof(ChargingPoolGroup)),
                                                  ("chargingTariffOffers",              nameof(ChargingTariffOffer)) ] },
            { nameof(GridOperator),             [ ("energyMeterTemplates",              nameof(EnergyMeterTemplate)),
                                                  ("gridConnectionPoints",              nameof(GridConnectionPoint)),
                                                  ("energyMeterGroups",                 nameof(EnergyMeterGroup)) ] },
            { nameof(ParkingOperator),          [ ("parkingGarages",                    nameof(ParkingGarage)),
                                                  ("parkingSpaceTemplates",             nameof(ParkingSpaceTemplate)),
                                                  ("parkingSpaces",                     nameof(ParkingSpace)),
                                                  ("parkingSensors",                    nameof(ParkingSensor)),
                                                  ("parkingSpaceGroups",                nameof(ParkingSpaceGroup)),
                                                  ("parkingProducts",                   nameof(ParkingProduct)) ] }
        };

        /// <summary>
        /// The properties of the roaming network itself a change set can update or remove.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableNetworkProperties = [
            "name",
            "description",
            "dataSource",
            "dataLicenses",
            POITreeHashAlgorithms.JSONProperty,
            "customData"
        ];

        #endregion


        #region (static) TryMerge(Base, Left, Right, ChangeSetId, out Merge, out ChangeSet, out Conflicts)

        /// <summary>
        /// Try to merge the right version into the left one, with their common ancestor as the base.
        /// The change set leads from the left version to the merge; its creation is the latest last change of both,
        /// so that everyone merging the same versions calculates the same change set.
        /// </summary>
        /// <param name="Base">The common ancestor.</param>
        /// <param name="Left">The left version, the one the change set is applied to.</param>
        /// <param name="Right">The right version.</param>
        /// <param name="ChangeSetId">The identification of the change set of the merge.</param>
        /// <param name="Merge">The merged version.</param>
        /// <param name="ChangeSet">The change set leading from the left version to the merge.</param>
        /// <param name="Conflicts">The conflicts, when they can not be merged.</param>
        public static Boolean TryMerge(RoamingNetwork                                                  Base,
                                       RoamingNetwork                                                  Left,
                                       RoamingNetwork                                                  Right,
                                       ChangeSet_Id                                                    ChangeSetId,
                                       [NotNullWhen(true)]  out RoamingNetwork?                        Merge,
                                       [NotNullWhen(true)]  out RoamingNetworkChangeSet?               ChangeSet,
                                       out IReadOnlyList<RoamingNetworkMergeConflict>                  Conflicts)

            => TryMerge(MergeBase.Of(Base),
                        Left,
                        Right,
                        ChangeSetId,
                        out Merge,
                        out ChangeSet,
                        out Conflicts);


        /// <summary>
        /// Try to merge the right version into the left one, with the given merge base: the version of their
        /// best common ancestor, or a virtual one of several.
        /// </summary>
        /// <param name="Base">The merge base.</param>
        /// <param name="Left">The left version, the one the change set is applied to.</param>
        /// <param name="Right">The right version.</param>
        /// <param name="ChangeSetId">The identification of the change set of the merge.</param>
        /// <param name="Merge">The merged version.</param>
        /// <param name="ChangeSet">The change set leading from the left version to the merge.</param>
        /// <param name="Conflicts">The conflicts, when they can not be merged.</param>
        internal static Boolean TryMerge(MergeBase                                                       Base,
                                         RoamingNetwork                                                  Left,
                                         RoamingNetwork                                                  Right,
                                         ChangeSet_Id                                                    ChangeSetId,
                                         [NotNullWhen(true)]  out RoamingNetwork?                        Merge,
                                         [NotNullWhen(true)]  out RoamingNetworkChangeSet?               ChangeSet,
                                         out IReadOnlyList<RoamingNetworkMergeConflict>                  Conflicts)
        {

            Merge      = null;
            ChangeSet  = null;

            var conflicts  = new List<RoamingNetworkMergeConflict>();
            var changes    = MergeChanges(Base.Entities, Flatten(Left), Flatten(Right), conflicts);

            if (conflicts.Count > 0)
            {
                Conflicts = conflicts;
                return false;
            }

            var createdAt = new[] { Left.LastChangeDate, Right.LastChangeDate }.Max();

            var pending   = new RoamingNetworkChangeSet(ChangeSetId,
                                                        Left.Id,
                                                        createdAt,
                                                        [ .. changes ],
                                                        Left.CalcChangeSetETags(),
                                                        Left.CalcChangeSetETags(),
                                                        new ImmutableI18NString(I18NString.Create(Languages.en, "Merge")));

            if (!Left.TryApplyChanges(pending, out var merge, out var changesWithOldValues, out var applyError))
            {
                Conflicts = [ new RoamingNetworkMergeConflict(RoamingNetworkMergeConflictKind.InvalidResult,
                                                              nameof(RoamingNetwork),
                                                              Left.Id.ToString(),
                                                              null,
                                                              null,
                                                              null,
                                                              null,
                                                              applyError) ];
                return false;
            }

            merge.ShareRuntimeOfMerged(Left, Right);

            // The ETags of the merged version, its effectiveTree ETags as well; its change set names those of its representations.
            var mergeETags = merge.CalcETags();

            ChangeSet  = new RoamingNetworkChangeSet(pending.Id,
                                                     pending.RoamingNetworkId,
                                                     pending.CreatedAt,
                                                     changesWithOldValues,
                                                     pending.BeforeETags,
                                                     [ .. mergeETags.Where(eTag => eTag.Format != ETagFormat.EffectiveTree) ],
                                                     pending.Description);
            Merge      = merge;
            Conflicts  = [];
            return true;

        }

        #endregion

        #region (internal) TryApplyKnown(ChangeSet, out RoamingNetwork, out ErrorResponse)

        /// <summary>
        /// Apply a change set checked before, e.g. one of a commit of a history whose version is calculated again,
        /// or the change set of a merge calculated by TryMerge, without checking the signatures of its owners:
        /// it must fit this version by its before ETags, and lead to its after ETags.
        /// </summary>
        internal Boolean TryApplyKnown(RoamingNetworkChangeSet                   ChangeSet,
                                       [NotNullWhen(true)]  out RoamingNetwork?  RoamingNetwork,
                                       [NotNullWhen(false)] out String?          ErrorResponse)
        {

            RoamingNetwork = null;

            if (!MatchETags(ChangeSet.BeforeETags, this, out var beforeError))
            {
                ErrorResponse = $"The merge does not fit this version: {beforeError}";
                return false;
            }

            if (!TryCheckPreconditions(ChangeSet.Preconditions, out var preconditionError))
            {
                ErrorResponse = $"The change set does not fit this version: {preconditionError}";
                return false;
            }

            if (!TryApplyChanges(ChangeSet, out var result, out _, out ErrorResponse))
                return false;

            result.CalcETags();

            if (!MatchETags(ChangeSet.AfterETags, result, out var afterError))
            {
                ErrorResponse = $"The result does not match the merge: {afterError}";
                return false;
            }

            RoamingNetwork = result;
            return true;

        }

        #endregion


        #region (private static) Flatten(RoamingNetwork)

        /// <summary>
        /// The entities of the given version, in the order of its JSON representation: parents before their children.
        /// </summary>
        private static Dictionary<MergeKey, MergeEntity> Flatten(RoamingNetwork RoamingNetwork)
        {

            var entities = new Dictionary<MergeKey, MergeEntity>();

            void Walk(String     Type,
                      String     Id,
                      MergeKey?  Parent,
                      JObject    JSON)
            {

                var own       = (JObject) JSON.DeepClone();
                var children  = MergeChildren.TryGetValue(Type, out var childArrays) ? childArrays : [];

                foreach (var (key, _) in children)
                    own.Remove(key);

                // Its identification is its key: another spelling of it is the same entity.
                own.Remove("@id");
                own.Remove("lastChange");
                own.Remove("eTags");

                var entity = new MergeEntity(Type, Id, Parent, own, JSON);

                entities[entity.Key] = entity;

                foreach (var (key, childType) in children)
                {

                    IEnumerable<JObject> childObjects = JSON[key] is JObject single
                                                            ? [ single ]
                                                            : (JSON[key] as JArray ?? []).OfType<JObject>();

                    foreach (var child in childObjects)
                        Walk(childType, child["@id"]!.Value<String>()!, entity.Key, child);

                }

            }

            Walk(nameof(RoamingNetwork), RoamingNetwork.Id.ToString(), null, RoamingNetwork.ToJSON(IncludeETags: false));

            return entities;

        }

        #endregion

        #region (private static) MergeChanges(Base, Left, Right, Conflicts)

        /// <summary>
        /// The changes leading from the left version to the merge, or the conflicts.
        /// </summary>
        private static List<RoamingNetworkChange> MergeChanges(Dictionary<MergeKey, MergeEntity>  Base,
                                                               Dictionary<MergeKey, MergeEntity>  Left,
                                                               Dictionary<MergeKey, MergeEntity>  Right,
                                                               List<RoamingNetworkMergeConflict>                  Conflicts)
        {

            var adds      = new List<RoamingNetworkChange>();
            var updates   = new List<RoamingNetworkChange>();
            var replaces  = new List<RoamingNetworkChange>();
            var removes   = new List<RoamingNetworkChange>();

            var added     = new HashSet<MergeKey>();
            var removed   = new HashSet<MergeKey>();
            var replaced  = new HashSet<MergeKey>();

            void Conflict(RoamingNetworkMergeConflictKind Kind, MergeEntity Entity, String? PropertyName, JToken? BaseValue, JToken? LeftValue, JToken? RightValue, String Description)
                => Conflicts.Add(new (Kind, Entity.Type, Entity.Id, PropertyName, BaseValue, LeftValue, RightValue, Description));

            Boolean UnderAny(MergeEntity Entity, HashSet<MergeKey> Keys, Dictionary<MergeKey, MergeEntity> Version)
            {
                var parent = Entity.Parent;
                while (parent.HasValue)
                {
                    if (Keys.Contains(parent.Value))
                        return true;
                    parent = Version.TryGetValue(parent.Value, out var parentEntity) ? parentEntity.Parent : null;
                }
                return false;
            }

            // The left entities in their order, then those only the right one has, then those only the base had.
            var keys = Left.Keys.
                           Concat(Right.Keys.Where(key => !Left.ContainsKey(key))).
                           Concat(Base. Keys.Where(key => !Left.ContainsKey(key) && !Right.ContainsKey(key))).
                           ToArray();

            foreach (var key in keys)
            {

                Base. TryGetValue(key, out var b);
                Left. TryGetValue(key, out var l);
                Right.TryGetValue(key, out var r);

                #region Removed by both, or only by the left one

                if (l is null && r is null)
                    continue;

                if (l is null)
                {

                    // Added by the right one only.
                    if (b is null)
                    {

                        var parentKept = r!.Parent is not MergeKey parent ||
                                         (Left.ContainsKey(parent) && !removed.Contains(parent)) ||
                                         added.Contains(parent);

                        if (!parentKept)
                        {
                            Conflict(RoamingNetworkMergeConflictKind.DeleteModify, r, null, null, null, r.Full,
                                     $"The right version adds the {r.Type} '{r.Id}' to the {r.Parent!.Value.Type} '{r.Parent!.Value.Id}' the left version removed!");
                            continue;
                        }

                        added.Add(key);

                        if (!UnderAny(r, added, Right))
                            adds.Add(RoamingNetworkChange.Add(r.Type, r.Id, (JObject) r.Full.DeepClone(), ParentType(r), ParentId(r, Right)));

                        continue;

                    }

                    // Removed by the left one: fine, unless the right one changed it.
                    if (r!.Parent != b.Parent || !POIJSON.SameValue(r.Own, b.Own))
                        Conflict(RoamingNetworkMergeConflictKind.DeleteModify, r, null, b.Full, null, r.Full,
                                 $"The left version removed the {r.Type} '{r.Id}' the right version changed!");

                    continue;

                }

                #endregion

                #region Removed by the right one

                if (r is null)
                {

                    // Added by the left one only: kept, unless the right one removed what it was added to.
                    if (b is null)
                    {
                        if (UnderAny(l, removed, Left))
                            Conflict(RoamingNetworkMergeConflictKind.DeleteModify, l, null, null, l.Full, null,
                                     $"The left version adds the {l.Type} '{l.Id}' to the {l.Parent!.Value.Type} '{l.Parent!.Value.Id}' the right version removed!");
                        continue;
                    }

                    if (l.Parent != b.Parent || !POIJSON.SameValue(l.Own, b.Own))
                    {
                        Conflict(RoamingNetworkMergeConflictKind.DeleteModify, l, null, b.Full, l.Full, null,
                                 $"The right version removed the {l.Type} '{l.Id}' the left version changed!");
                        continue;
                    }

                    removed.Add(key);

                    if (!UnderAny(l, removed, Left))
                        removes.Add(RoamingNetworkChange.Remove(l.Type, l.Id));

                    continue;

                }

                #endregion

                #region In both: under a replaced, added or removed parent, nothing to do on its own

                if (UnderAny(l, replaced, Left) || UnderAny(r, replaced, Right))
                    continue;

                #endregion

                #region Moved to another parent

                if (l.Parent != r.Parent)
                {

                    // Moved by the right one, and the left one did not touch it: replaced as the right one has it.
                    if (b is not null && l.Parent == b.Parent && SameSubtree(l, b, Left, Base))
                    {
                        replaced.Add(key);
                        replaces.Add(RoamingNetworkChange.Remove(l.Type, l.Id));
                        replaces.Add(RoamingNetworkChange.Add(r.Type, r.Id, (JObject) r.Full.DeepClone(), ParentType(r), ParentId(r, Right)));
                        continue;
                    }

                    // Moved by the left one, and the right one did not touch it: kept.
                    if (b is not null && r.Parent == b.Parent && POIJSON.SameValue(r.Own, b.Own))
                        continue;

                    Conflict(RoamingNetworkMergeConflictKind.Moved, l, null, b?.Full, l.Full, r.Full,
                             $"The {l.Type} '{l.Id}' is under different parents!");
                    continue;

                }

                #endregion

                #region Conflicted in the virtual merge base: as a whole

                // Its common ancestors conflict at it: the versions conflict, unless both have it the same; neither
                // property by property nor element by element, as an absent property of the marker is no value of theirs.
                if (IsConflicted(b))
                {

                    if (!POIJSON.SameValue(l.Own, r.Own))
                        Conflict(RoamingNetworkMergeConflictKind.DifferentValues, l, null, b!.Own, l.Own, r.Own,
                                 $"The common ancestors of both versions conflict at the {l.Type} '{l.Id}', and the versions have it differently!");

                    continue;

                }

                #endregion

                #region Property by property

                var target         = (JObject) l.Own.DeepClone();
                var conflicting    = false;
                var propertyNames  = l.Own.Properties().Select(property => property.Name).
                                         Union(r.Own.Properties().Select(property => property.Name)).
                                         Union(b?.Own.Properties().Select(property => property.Name) ?? []).
                                         ToArray();

                foreach (var propertyName in propertyNames)
                {

                    var bv = b?.Own[propertyName];
                    var lv = l. Own[propertyName];
                    var rv = r. Own[propertyName];

                    if (Same(lv, rv) || Same(rv, bv))
                        continue;

                    if (Same(lv, bv))
                    {
                        if (rv is null)
                            target.Remove(propertyName);
                        else
                            target[propertyName] = rv.DeepClone();
                        continue;
                    }

                    // Both changed an array of identified elements: element by element.
                    if (b is not null && TryMergeElements(propertyName, bv, lv, rv, out var mergedValue))
                    {
                        if (mergedValue is null)
                            target.Remove(propertyName);
                        else
                            target[propertyName] = mergedValue;
                        continue;
                    }

                    conflicting = true;

                    Conflict(b is null ? RoamingNetworkMergeConflictKind.AddAdd : RoamingNetworkMergeConflictKind.DifferentValues,
                             l, propertyName, bv, lv, rv,
                             b is null
                                 ? $"Both versions added the {l.Type} '{l.Id}', with different values of '{propertyName}'!"
                                 : $"Both versions changed '{propertyName}' of the {l.Type} '{l.Id}' differently!");

                }

                if (conflicting || POIJSON.SameValue(target, l.Own))
                    continue;

                var differing     = target.Properties().Select(property => property.Name).
                                        Union(l.Own.Properties().Select(property => property.Name)).
                                        Where(propertyName => !Same(target[propertyName], l.Own[propertyName])).
                                        ToArray();

                var changeable    = ChangeableProperties(l.Type);

                if (differing.All(changeable.Contains))
                {
                    foreach (var propertyName in differing)
                        updates.AddRange(PropertyChanges(l.Type, l.Id, propertyName, l.Own[propertyName], target[propertyName]));
                    continue;
                }

                // E.g. removed and added again by the right one: replaced as the right one has it, unless the left one changed it.
                if (b is not null && l.Type is not (nameof(RoamingNetwork) or nameof(RoamingNetworkOperator)) && POIJSON.SameValue(target, r.Own) && SameSubtree(l, b, Left, Base))
                {
                    replaced.Add(key);
                    replaces.Add(RoamingNetworkChange.Remove(l.Type, l.Id));
                    replaces.Add(RoamingNetworkChange.Add(r.Type, r.Id, (JObject) r.Full.DeepClone(), ParentType(r), ParentId(r, Right)));
                    continue;
                }

                Conflict(RoamingNetworkMergeConflictKind.DifferentValues, l, differing.First(propertyName => !changeable.Contains(propertyName)),
                         b?.Own, l.Own, r.Own,
                         $"The {l.Type} '{l.Id}' can not be merged property by property!");

                #endregion

            }

            // Added first, so that what is changed or kept can name it; removed last.
            return [ .. adds, .. updates, .. replaces, .. removes ];

        }

        #endregion

        #region (private static) MergeVirtual(Base, Left, Right)

        /// <summary>
        /// The entities of the virtual merge base of the left and the right ones, merged with their merge base
        /// as a three-way merge does, but with a conflict marker where they conflict:
        /// a property both changed differently has it as its value; an entity one removed and the other changed,
        /// or one both moved, or moved by one and changed by the other, has it as its only property.
        /// </summary>
        private static Dictionary<MergeKey, MergeEntity> MergeVirtual(Dictionary<MergeKey, MergeEntity>  Base,
                                                                                      Dictionary<MergeKey, MergeEntity>  Left,
                                                                                      Dictionary<MergeKey, MergeEntity>  Right)
        {

            static Boolean Unchanged(MergeEntity Entity, MergeEntity BaseEntity)
                => Entity.Parent == BaseEntity.Parent && POIJSON.SameValue(Entity.Own, BaseEntity.Own);

            static MergeEntity Conflicted(MergeEntity Entity, MergeEntity? BaseEntity, MergeEntity? LeftEntity, MergeEntity? RightEntity)
            {
                var own = ConflictMarker(BaseEntity?.Own, LeftEntity?.Own, RightEntity?.Own);
                return new MergeEntity(Entity.Type, Entity.Id, Entity.Parent, own, own);
            }

            var entities  = new Dictionary<MergeKey, MergeEntity>();

            var keys      = Left.Keys.
                                Concat(Right.Keys.Where(key => !Left.ContainsKey(key))).
                                ToArray();

            foreach (var key in keys)
            {

                Base. TryGetValue(key, out var b);
                Left. TryGetValue(key, out var l);
                Right.TryGetValue(key, out var r);

                // Only one of them has it: added by it, removed by the other one, or removed by one and changed by the other.
                if (l is null || r is null)
                {

                    var one = (l ?? r)!;

                    if (b is null)
                        entities[key] = one;

                    else if (!Unchanged(one, b))
                        entities[key] = Conflicted(one, b, l, r);

                    continue;

                }

                if (l.Parent != r.Parent)
                {

                    entities[key] = b is not null && Unchanged(l, b) ? r
                                  : b is not null && Unchanged(r, b) ? l
                                  : Conflicted(l, b, l, r);

                    continue;

                }

                // Conflicted in their own merge base: as a whole, unless both have it the same.
                if (IsConflicted(b))
                {

                    entities[key] = POIJSON.SameValue(l.Own, r.Own)
                                        ? l
                                        : Conflicted(l, b, l, r);

                    continue;

                }

                var own            = new JObject();
                var propertyNames  = l.Own.Properties().Select(property => property.Name).
                                         Union(r.Own.Properties().Select(property => property.Name)).
                                         Union(b?.Own.Properties().Select(property => property.Name) ?? []).
                                         ToArray();

                foreach (var propertyName in propertyNames)
                {

                    var bv     = b?.Own[propertyName];
                    var lv     = l. Own[propertyName];
                    var rv     = r. Own[propertyName];

                    var value  = Same(lv, rv) || Same(rv, bv)                                            ? lv
                               : Same(lv, bv)                                                            ? rv
                               : b is not null && TryMergeElements(propertyName, bv, lv, rv, out var merged)  ? merged
                               : ConflictMarker(bv, lv, rv);

                    if (value is not null)
                        own.Add(propertyName, value.DeepClone());

                }

                entities[key] = new MergeEntity(l.Type, l.Id, l.Parent, own, own);

            }

            return entities;

        }

        #endregion

        #region (private static) IsConflicted(Entity)

        /// <summary>
        /// Whether the given entity of a virtual merge base is a conflict marker as a whole: its common ancestors conflict at it.
        /// </summary>
        private static Boolean IsConflicted(MergeEntity? Entity)

            => Entity?.Own[ConflictMarkerProperty] is not null;

        #endregion

        #region (private static) ConflictMarker(Base, Left, Right)

        /// <summary>
        /// The conflict marker of a virtual merge base: { "@conflict": { base?, left?, right? } }, absent values left out.
        /// </summary>
        private static JObject ConflictMarker(JToken?  Base,
                                              JToken?  Left,
                                              JToken?  Right)
        {

            var conflict = new JObject();

            if (Base  is not null)
                conflict.Add("base",   Base. DeepClone());

            if (Left  is not null)
                conflict.Add("left",   Left. DeepClone());

            if (Right is not null)
                conflict.Add("right",  Right.DeepClone());

            return new JObject(new JProperty(ConflictMarkerProperty, conflict));

        }

        #endregion

        #region (private static) TryMergeElements(PropertyName, Base, Left, Right, out Merged)

        /// <summary>
        /// Merge a property both versions changed differently element by element, when it is an array of
        /// identified elements (see POIElements.TryMerge). The keys of a party, the templates of a member and the tree hash
        /// algorithms of a roaming network, whose order is content, are merged as a whole.
        /// </summary>
        private static Boolean TryMergeElements(String       PropertyName,
                                                JToken?      Base,
                                                JToken?      Left,
                                                JToken?      Right,
                                                out JToken?  Merged)
        {

            Merged = null;

            return !POIPartyKeys.IsKeyProperty(PropertyName) &&
                   PropertyName != POIElements.TemplateIdsProperty &&
                   PropertyName != POITreeHashAlgorithms.JSONProperty &&
                   POIElements.TryMerge(PropertyName, Base, Left, Right, out Merged);

        }

        #endregion

        #region (private static) PropertyChanges(EntityType, EntityId, PropertyName, Value, NewValue)

        /// <summary>
        /// The changes of a property leading from the value of the left version to the merged one: element by element,
        /// where they can be (see POIElements.Diff), the keys of a party, the templates of a member and the tree hash
        /// algorithms as a whole; without old values,
        /// so that they get the values they find.
        /// </summary>
        private static IEnumerable<RoamingNetworkChange> PropertyChanges(String   EntityType,
                                                                         String   EntityId,
                                                                         String   PropertyName,
                                                                         JToken?  Value,
                                                                         JToken?  NewValue)
        {

            if (POIPartyKeys.IsKeyProperty(PropertyName) || PropertyName == POIElements.TemplateIdsProperty || PropertyName == POITreeHashAlgorithms.JSONProperty)
                return [ NewValue is not null
                             ? RoamingNetworkChange.UpdateProperty(EntityType, EntityId, PropertyName, null, NewValue.DeepClone())
                             : RoamingNetworkChange.RemoveProperty(EntityType, EntityId, PropertyName) ];

            return POIElements.Diff(EntityType, EntityId, PropertyName, Value, NewValue).
                               Select(change => change.WithoutOldValue());

        }

        #endregion

        #region (private static) Same(Value1, Value2)

        /// <summary>
        /// Whether the given values are equal; null means absent.
        /// </summary>
        private static Boolean Same(JToken? Value1, JToken? Value2)

            => Value1 is null
                   ? Value2 is null
                   : Value2 is not null && POIJSON.SameValue(Value1, Value2);

        #endregion

        #region (private static) SameSubtree(Entity, BaseEntity, Version, BaseVersion)

        /// <summary>
        /// Whether the given entity and everything it embeds is as in the base version, but for its last changes.
        /// </summary>
        private static Boolean SameSubtree(MergeEntity                                         Entity,
                                           MergeEntity                                         BaseEntity,
                                           Dictionary<MergeKey, MergeEntity>  Version,
                                           Dictionary<MergeKey, MergeEntity>  BaseVersion)
        {

            static IEnumerable<MergeEntity> Subtree(MergeEntity Root, Dictionary<MergeKey, MergeEntity> Version)

                => Version.Values.Where(entity => {
                       var parent = entity.Parent;
                       while (parent.HasValue)
                       {
                           if (parent.Value == Root.Key)
                               return true;
                           parent = Version.TryGetValue(parent.Value, out var parentEntity) ? parentEntity.Parent : null;
                       }
                       return false;
                   });

            if (!POIJSON.SameValue(Entity.Own, BaseEntity.Own))
                return false;

            var subtree      = Subtree(Entity,     Version).    ToDictionary(entity => entity.Key);
            var baseSubtree  = Subtree(BaseEntity, BaseVersion).ToDictionary(entity => entity.Key);

            return subtree.Count == baseSubtree.Count &&
                   subtree.All(entry => baseSubtree.TryGetValue(entry.Key, out var baseEntity) &&
                                        entry.Value.Parent == baseEntity.Parent &&
                                        POIJSON.SameValue(entry.Value.Own, baseEntity.Own));

        }

        #endregion

        #region (private static) ParentId(Entity, Version)

        /// <summary>
        /// The identification of the entity embedding the given one, as the given version writes it.
        /// </summary>
        private static String? ParentId(MergeEntity                         Entity,
                                        Dictionary<MergeKey, MergeEntity>  Version)

            => Entity.Parent is MergeKey parent
                   ? Version.TryGetValue(parent, out var parentEntity)
                         ? parentEntity.Id
                         : parent.Id.ToString()
                   : null;

        #endregion

        #region (private static) TypedId(EntityType, EntityId)

        /// <summary>
        /// The identification of the given entity type the given text names, compared as its type compares it;
        /// null for a text its type does not read.
        /// </summary>
        private static Object? TypedId(String  EntityType,
                                       String  EntityId)
        {

            Object? id = EntityType switch {
                nameof(RoamingNetwork)                   => RoamingNetwork_Id.                 TryParse(EntityId, out var roamingNetworkId)  ? roamingNetworkId  : null,
                nameof(RoamingNetworkOperator)           => RoamingNetworkOperator_Id.         TryParse(EntityId, out var networkOperatorId) ? networkOperatorId : null,
                nameof(ChargingStationOperator)          => ChargingStationOperator_Id.        TryParse(EntityId, out var operatorId)        ? operatorId        : null,
                nameof(ChargingStationManufacturer)      => ChargingStationManufacturer_Id.    TryParse(EntityId, out var manufacturerId)    ? manufacturerId    : null,
                nameof(EMobilityProvider)                => EMobilityProvider_Id.              TryParse(EntityId, out var providerId)        ? providerId        : null,
                nameof(GridOperator)                     => GridOperator_Id.                   TryParse(EntityId, out var gridOperatorId)    ? gridOperatorId    : null,
                nameof(ParkingOperator)                  => ParkingOperator_Id.                TryParse(EntityId, out var parkingOperatorId) ? parkingOperatorId : null,
                nameof(EnergyMeterManufacturer)          => EnergyMeterManufacturer_Id.        TryParse(EntityId, out var meterMakerId)      ? meterMakerId      : null,
                nameof(TransparencySoftware)             => TransparencySoftware_Id.           TryParse(EntityId, out var softwareId)        ? softwareId        : null,
                nameof(TransparencySoftwareCertificate)  => TransparencySoftwareCertificate_Id.TryParse(EntityId, out var certificateId)     ? certificateId     : null,
                nameof(ChargingTariff)                   => ChargingTariff_Id.                 TryParse(EntityId, out var tariffId)          ? tariffId          : null,
                nameof(ChargingPool)                     => ChargingPool_Id.                   TryParse(EntityId, out var poolId)            ? poolId            : null,
                nameof(ChargingStation)                  => ChargingStation_Id.                TryParse(EntityId, out var stationId)         ? stationId         : null,
                nameof(EVSE)                             => EVSE_Id.                           TryParse(EntityId, out var evseId)            ? evseId            : null,
                nameof(EVSETemplate)                     => EVSETemplate_Id.                   TryParse(EntityId, out var evseTemplateId)    ? evseTemplateId    : null,
                nameof(ChargingStationTemplate)          => ChargingStationTemplate_Id.        TryParse(EntityId, out var stationTemplateId) ? stationTemplateId : null,
                nameof(ChargingPoolTemplate)             => ChargingPoolTemplate_Id.           TryParse(EntityId, out var poolTemplateId)    ? poolTemplateId    : null,
                nameof(ChargingTariffTemplate)           => ChargingTariffTemplate_Id.         TryParse(EntityId, out var tariffTemplateId)  ? tariffTemplateId  : null,
                nameof(EnergyMeterTemplate)              => EnergyMeterTemplate_Id.            TryParse(EntityId, out var meterTemplateId)   ? meterTemplateId   : null,
                nameof(EVSEGroup)                        => EVSEGroup_Id.                      TryParse(EntityId, out var evseGroupId)       ? evseGroupId       : null,
                nameof(ChargingStationGroup)             => ChargingStationGroup_Id.           TryParse(EntityId, out var stationGroupId)    ? stationGroupId    : null,
                nameof(ChargingPoolGroup)                => ChargingPoolGroup_Id.              TryParse(EntityId, out var poolGroupId)       ? poolGroupId       : null,
                nameof(ChargingTariffGroup)              => ChargingTariffGroup_Id.            TryParse(EntityId, out var tariffGroupId)     ? tariffGroupId     : null,
                nameof(GridConnectionPoint)              => GridConnectionPoint_Id.            TryParse(EntityId, out var pointId)           ? pointId           : null,
                nameof(EnergyMeterGroup)                 => EnergyMeterGroup_Id.               TryParse(EntityId, out var meterGroupId)      ? meterGroupId      : null,
                nameof(ChargingTariffOffer)              => ChargingTariffOffer_Id.            TryParse(EntityId, out var offerId)           ? offerId           : null,
                _                                        => ParkingId(EntityType, EntityId)
            };

            return id;

        }

        #endregion

        #region (private static) ParentType(Entity)

        /// <summary>
        /// The parent type of an Add change: none for the parties and registries of the roaming network itself.
        /// </summary>
        private static String? ParentType(MergeEntity Entity)

            => Entity.Parent?.Type == nameof(RoamingNetwork)
                   ? null
                   : Entity.Parent?.Type;

        #endregion

        #region (private static) ChangeableProperties(EntityType)

        /// <summary>
        /// The properties a change set can update or remove, by the entity type.
        /// </summary>
        private static ImmutableHashSet<String> ChangeableProperties(String EntityType)

            => EntityType switch {
                   nameof(RoamingNetwork)                   => ChangeableNetworkProperties,
                   nameof(RoamingNetworkOperator)           => RoamingNetworkOperatorRegistry.         ChangeableProperties,
                   nameof(ChargingStationOperator)          => ChangeableOperatorProperties,
                   nameof(ChargingPool)                     => ChangeableChargingPoolProperties,
                   nameof(ChargingStation)                  => ChangeableChargingStationProperties,
                   nameof(EVSE)                             => ChangeableEVSEProperties,
                   nameof(ChargingTariff)                   => ChangeableChargingTariffProperties,
                   nameof(ChargingStationManufacturer)      => ManufacturerRegistry.                   ChangeableProperties,
                   nameof(EMobilityProvider)                => EMobilityProviderRegistry.              ChangeableProperties,
                   nameof(GridOperator)                     => GridOperatorRegistry.                   ChangeableProperties,
                   nameof(ParkingOperator)                  => ParkingOperatorRegistry.                ChangeableProperties,
                   nameof(EnergyMeterManufacturer)          => EnergyMeterManufacturerRegistry.        ChangeableProperties,
                   nameof(TransparencySoftware)             => TransparencySoftwareRegistry.           ChangeableProperties,
                   nameof(TransparencySoftwareCertificate)  => TransparencySoftwareCertificateRegistry.ChangeableProperties,
                   nameof(ParkingGarage)                    => ChangeableParkingGarageProperties,
                   nameof(ParkingSpace)                     => ChangeableParkingSpaceProperties,
                   nameof(ParkingSensor)                    => ChangeableParkingSensorProperties,
                   nameof(ParkingSpaceTemplate)             => ChangeableParkingSpaceTemplateProperties,
                   nameof(ParkingSpaceGroup)                => ChangeableParkingSpaceGroupProperties,
                   nameof(ParkingProduct)                   => ChangeableParkingProductProperties,
                   nameof(GridConnectionPoint)              => ChangeableGridConnectionPointProperties,
                   nameof(EnergyMeterTemplate)              => ChangeableEnergyMeterTemplateProperties,
                   nameof(EnergyMeterGroup)                 => ChangeableEnergyMeterGroupProperties,
                   nameof(ChargingTariffOffer)              => ChangeableChargingTariffOfferProperties,
                   _ when ChangeableGroupProperties.TryGetValue(EntityType, out var groupProperties)
                                                            => groupProperties,
                   _ when ChangeableTemplateProperties.TryGetValue(EntityType, out var templateProperties)
                                                            => templateProperties,
                   _                                        => []
               };

        #endregion

        #region (private) ShareRuntimeOfMerged(Left, Right)

        /// <summary>
        /// Take the runtime status of the entities of the merge: of the right version for what only it has,
        /// of the left version for everything else.
        /// </summary>
        private void ShareRuntimeOfMerged(RoamingNetwork  Left,
                                          RoamingNetwork  Right)
        {

            // Only what is new in the merge: what it took from the left version unchanged is that version's, and may be listened to.
            foreach (var chargingStationOperator in chargingStationOperators)
            {

                Left.chargingStationOperators.TryGet(chargingStationOperator.Id, out var left);

                if (ReferenceEquals(chargingStationOperator, left))
                    continue;

                if (Right.chargingStationOperators.TryGet(chargingStationOperator.Id, out var right))
                    chargingStationOperator.ShareRuntimeOfAll(right);

                if (left is not null)
                    chargingStationOperator.ShareRuntimeOfAll(left);

            }

            foreach (var parkingOperator in parkingOperators)
            {

                Left.parkingOperators.TryGet(parkingOperator.Id, out var left);

                if (ReferenceEquals(parkingOperator, left))
                    continue;

                if (Right.parkingOperators.TryGet(parkingOperator.Id, out var right))
                    parkingOperator.ShareRuntimeOfAll(right);

                if (left is not null)
                    parkingOperator.ShareRuntimeOfAll(left);

            }

            foreach (var eMobilityProvider in eMobilityProviders)
            {

                Left.eMobilityProviders.TryGet(eMobilityProvider.Id, out var left);

                if (ReferenceEquals(eMobilityProvider, left))
                    continue;

                if (Right.eMobilityProviders.TryGet(eMobilityProvider.Id, out var right))
                    eMobilityProvider.ShareRuntimeOfAll(right);

                if (left is not null)
                    eMobilityProvider.ShareRuntimeOfAll(left);

            }

            foreach (var gridOperator in gridOperators)
            {

                Left.gridOperators.TryGet(gridOperator.Id, out var left);

                if (ReferenceEquals(gridOperator, left))
                    continue;

                if (Right.gridOperators.TryGet(gridOperator.Id, out var right))
                    gridOperator.ShareRuntimeOfAll(right);

                if (left is not null)
                    gridOperator.ShareRuntimeOfAll(left);

            }

            foreach (var manufacturer in chargingStationManufacturers)
                if (!Left.chargingStationManufacturers.Contains(manufacturer.Id) && Right.chargingStationManufacturers.TryGet(manufacturer.Id, out var right))
                    manufacturer.ShareRuntimeOf(right);

        }

        #endregion

    }

}
