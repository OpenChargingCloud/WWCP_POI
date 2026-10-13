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
        InvalidResult

    }


    /// <summary>
    /// A conflict of two versions of a roaming network: what their common ancestor, the left and the right version
    /// have at one place. The owner resolves it with a change set on one of them, e.g. taking the value of the other.
    /// </summary>
    /// <param name="Kind">Why they conflict.</param>
    /// <param name="EntityType">The type of the entity.</param>
    /// <param name="EntityId">The identification of the entity.</param>
    /// <param name="PropertyName">The property, unless the entity conflicts as a whole.</param>
    /// <param name="Base">The value of the common ancestor; null when absent.</param>
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

        #region (private) MergeEntity

        /// <summary>
        /// An entity of a version, as the merge compares it.
        /// </summary>
        /// <param name="Type">Its entity type.</param>
        /// <param name="Id">Its identification.</param>
        /// <param name="Parent">The type and identification of the entity embedding it, unless it is the roaming network itself.</param>
        /// <param name="Own">Its properties, without its embedded children and without its last change.</param>
        /// <param name="Full">Its complete JSON representation, with its embedded children.</param>
        private sealed record MergeEntity(String                            Type,
                                          String                            Id,
                                          (String Type, String Id)?         Parent,
                                          JObject                           Own,
                                          JObject                           Full);

        #endregion

        #region Data

        /// <summary>
        /// The embedded children a change set addresses on their own: their JSON key and entity type, by the entity type embedding them.
        /// </summary>
        private static readonly Dictionary<String, (String Key, String Type)[]> MergeChildren = new () {
            { nameof(RoamingNetwork),           [ ("chargingStationOperators",          nameof(ChargingStationOperator)),
                                                  ("chargingStationManufacturers",      nameof(ChargingStationManufacturer)),
                                                  ("eMobilityProviders",                nameof(EMobilityProvider)),
                                                  ("gridOperators",                     nameof(GridOperator)),
                                                  ("parkingOperators",                  nameof(ParkingOperator)),
                                                  ("brands",                            nameof(Brand)),
                                                  ("energyMeterManufacturers",          nameof(EnergyMeterManufacturer)),
                                                  ("transparencySoftware",              nameof(TransparencySoftware)),
                                                  ("transparencySoftwareCertificates",  nameof(TransparencySoftwareCertificate)) ] },
            { nameof(ChargingStationOperator),  [ ("chargingTariffs",                   nameof(ChargingTariff)),
                                                  ("chargingPools",                     nameof(ChargingPool)),
                                                  ("EVSEGroups",                        nameof(EVSEGroup)),
                                                  ("chargingStationGroups",             nameof(ChargingStationGroup)),
                                                  ("chargingPoolGroups",                nameof(ChargingPoolGroup)),
                                                  ("chargingTariffGroups",              nameof(ChargingTariffGroup)) ] },
            { nameof(ChargingPool),             [ ("chargingStations",                  nameof(ChargingStation)) ] },
            { nameof(ChargingStation),          [ ("EVSEs",                             nameof(EVSE)) ] },
            { nameof(ParkingOperator),          [ ("parkingGarages",                    nameof(ParkingGarage)),
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
            "customData",
            POIPartyKeys.SigningKeysProperty,
            POIPartyKeys.IdentityKeysProperty
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
        {

            Merge      = null;
            ChangeSet  = null;

            var conflicts  = new List<RoamingNetworkMergeConflict>();
            var changes    = MergeChanges(Flatten(Base), Flatten(Left), Flatten(Right), conflicts);

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
                                                        Left.CalcETags(),
                                                        Left.CalcETags(),
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

            ChangeSet  = new RoamingNetworkChangeSet(pending.Id,
                                                     pending.RoamingNetworkId,
                                                     pending.CreatedAt,
                                                     changesWithOldValues,
                                                     pending.BeforeETags,
                                                     merge.CalcETags(),
                                                     pending.Description);
            Merge      = merge;
            Conflicts  = [];
            return true;

        }

        #endregion

        #region (internal) TryApplyMerge(ChangeSet, out RoamingNetwork, out ErrorResponse)

        /// <summary>
        /// Apply the change set of a merge, calculated by TryMerge, without signatures of owners:
        /// it must fit this version by its before ETags, and lead to its after ETags.
        /// </summary>
        internal Boolean TryApplyMerge(RoamingNetworkChangeSet                   ChangeSet,
                                       [NotNullWhen(true)]  out RoamingNetwork?  RoamingNetwork,
                                       [NotNullWhen(false)] out String?          ErrorResponse)
        {

            RoamingNetwork = null;

            if (!MatchETags(ChangeSet.BeforeETags, this, out var beforeError))
            {
                ErrorResponse = $"The merge does not fit this version: {beforeError}";
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
        private static Dictionary<(String Type, String Id), MergeEntity> Flatten(RoamingNetwork RoamingNetwork)
        {

            var entities = new Dictionary<(String, String), MergeEntity>();

            void Walk(String                     Type,
                      String                     Id,
                      (String Type, String Id)?  Parent,
                      JObject                    JSON)
            {

                var own       = (JObject) JSON.DeepClone();
                var children  = MergeChildren.TryGetValue(Type, out var childArrays) ? childArrays : [];

                foreach (var (key, _) in children)
                    own.Remove(key);

                own.Remove("lastChange");
                own.Remove("eTags");

                entities[(Type, Id)] = new MergeEntity(Type, Id, Parent, own, JSON);

                foreach (var (key, childType) in children)
                    foreach (var child in (JSON[key] as JArray ?? []).OfType<JObject>())
                        Walk(childType, child["@id"]!.Value<String>()!, (Type, Id), child);

            }

            Walk(nameof(RoamingNetwork), RoamingNetwork.Id.ToString(), null, RoamingNetwork.ToJSON(IncludeETags: false));

            return entities;

        }

        #endregion

        #region (private static) MergeChanges(Base, Left, Right, Conflicts)

        /// <summary>
        /// The changes leading from the left version to the merge, or the conflicts.
        /// </summary>
        private static List<RoamingNetworkChange> MergeChanges(Dictionary<(String Type, String Id), MergeEntity>  Base,
                                                               Dictionary<(String Type, String Id), MergeEntity>  Left,
                                                               Dictionary<(String Type, String Id), MergeEntity>  Right,
                                                               List<RoamingNetworkMergeConflict>                  Conflicts)
        {

            var adds      = new List<RoamingNetworkChange>();
            var updates   = new List<RoamingNetworkChange>();
            var replaces  = new List<RoamingNetworkChange>();
            var removes   = new List<RoamingNetworkChange>();

            var added     = new HashSet<(String, String)>();
            var removed   = new HashSet<(String, String)>();
            var replaced  = new HashSet<(String, String)>();

            void Conflict(RoamingNetworkMergeConflictKind Kind, MergeEntity Entity, String? PropertyName, JToken? BaseValue, JToken? LeftValue, JToken? RightValue, String Description)
                => Conflicts.Add(new (Kind, Entity.Type, Entity.Id, PropertyName, BaseValue, LeftValue, RightValue, Description));

            Boolean UnderAny(MergeEntity Entity, HashSet<(String, String)> Keys, Dictionary<(String Type, String Id), MergeEntity> Version)
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

                        var parentKept = r!.Parent is not (String, String) parent ||
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
                            adds.Add(RoamingNetworkChange.Add(r.Type, r.Id, (JObject) r.Full.DeepClone(), ParentType(r), r.Parent?.Id));

                        continue;

                    }

                    // Removed by the left one: fine, unless the right one changed it.
                    if (r!.Parent != b.Parent || !JToken.DeepEquals(r.Own, b.Own))
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

                    if (l.Parent != b.Parent || !JToken.DeepEquals(l.Own, b.Own))
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
                        replaces.Add(RoamingNetworkChange.Add(r.Type, r.Id, (JObject) r.Full.DeepClone(), ParentType(r), r.Parent?.Id));
                        continue;
                    }

                    // Moved by the left one, and the right one did not touch it: kept.
                    if (b is not null && r.Parent == b.Parent && JToken.DeepEquals(r.Own, b.Own))
                        continue;

                    Conflict(RoamingNetworkMergeConflictKind.Moved, l, null, b?.Full, l.Full, r.Full,
                             $"The {l.Type} '{l.Id}' is under different parents!");
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

                    conflicting = true;

                    Conflict(b is null ? RoamingNetworkMergeConflictKind.AddAdd : RoamingNetworkMergeConflictKind.DifferentValues,
                             l, propertyName, bv, lv, rv,
                             b is null
                                 ? $"Both versions added the {l.Type} '{l.Id}', with different values of '{propertyName}'!"
                                 : $"Both versions changed '{propertyName}' of the {l.Type} '{l.Id}' differently!");

                }

                if (conflicting || JToken.DeepEquals(target, l.Own))
                    continue;

                var differing     = target.Properties().Select(property => property.Name).
                                        Union(l.Own.Properties().Select(property => property.Name)).
                                        Where(propertyName => !Same(target[propertyName], l.Own[propertyName])).
                                        ToArray();

                var changeable    = ChangeableProperties(l.Type);

                if (differing.All(changeable.Contains))
                {
                    foreach (var propertyName in differing)
                        updates.Add(target[propertyName] is JToken newValue
                                        ? RoamingNetworkChange.UpdateProperty(l.Type, l.Id, propertyName, null, newValue.DeepClone())
                                        : RoamingNetworkChange.RemoveProperty(l.Type, l.Id, propertyName));
                    continue;
                }

                // E.g. removed and added again by the right one: replaced as the right one has it, unless the left one changed it.
                if (b is not null && l.Type != nameof(RoamingNetwork) && JToken.DeepEquals(target, r.Own) && SameSubtree(l, b, Left, Base))
                {
                    replaced.Add(key);
                    replaces.Add(RoamingNetworkChange.Remove(l.Type, l.Id));
                    replaces.Add(RoamingNetworkChange.Add(r.Type, r.Id, (JObject) r.Full.DeepClone(), ParentType(r), r.Parent?.Id));
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

        #region (private static) Same(Value1, Value2)

        /// <summary>
        /// Whether the given values are equal; null means absent.
        /// </summary>
        private static Boolean Same(JToken? Value1, JToken? Value2)

            => Value1 is null
                   ? Value2 is null
                   : Value2 is not null && JToken.DeepEquals(Value1, Value2);

        #endregion

        #region (private static) SameSubtree(Entity, BaseEntity, Version, BaseVersion)

        /// <summary>
        /// Whether the given entity and everything it embeds is as in the base version, but for its last changes.
        /// </summary>
        private static Boolean SameSubtree(MergeEntity                                         Entity,
                                           MergeEntity                                         BaseEntity,
                                           Dictionary<(String Type, String Id), MergeEntity>  Version,
                                           Dictionary<(String Type, String Id), MergeEntity>  BaseVersion)
        {

            static IEnumerable<MergeEntity> Subtree(MergeEntity Root, Dictionary<(String Type, String Id), MergeEntity> Version)

                => Version.Values.Where(entity => {
                       var parent = entity.Parent;
                       while (parent.HasValue)
                       {
                           if (parent.Value == (Root.Type, Root.Id))
                               return true;
                           parent = Version.TryGetValue(parent.Value, out var parentEntity) ? parentEntity.Parent : null;
                       }
                       return false;
                   });

            if (!JToken.DeepEquals(Entity.Own, BaseEntity.Own))
                return false;

            var subtree      = Subtree(Entity,     Version).    ToDictionary(entity => (entity.Type, entity.Id));
            var baseSubtree  = Subtree(BaseEntity, BaseVersion).ToDictionary(entity => (entity.Type, entity.Id));

            return subtree.Count == baseSubtree.Count &&
                   subtree.All(entry => baseSubtree.TryGetValue(entry.Key, out var baseEntity) &&
                                        entry.Value.Parent == baseEntity.Parent &&
                                        JToken.DeepEquals(entry.Value.Own, baseEntity.Own));

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
                   nameof(ChargingStationOperator)          => ChangeableOperatorProperties,
                   nameof(ChargingPool)                     => ChangeableChargingPoolProperties,
                   nameof(ChargingStation)                  => ChangeableChargingStationProperties,
                   nameof(EVSE)                             => ChangeableEVSEProperties,
                   nameof(ChargingTariff)                   => ChangeableChargingTariffProperties,
                   nameof(ChargingStationManufacturer)      => ManufacturerRegistry.                   ChangeableProperties,
                   nameof(EMobilityProvider)                => EMobilityProviderRegistry.              ChangeableProperties,
                   nameof(GridOperator)                     => GridOperatorRegistry.                   ChangeableProperties,
                   nameof(ParkingOperator)                  => ParkingOperatorRegistry.                ChangeableProperties,
                   nameof(Brand)                            => BrandRegistry.                          ChangeableProperties,
                   nameof(EnergyMeterManufacturer)          => EnergyMeterManufacturerRegistry.        ChangeableProperties,
                   nameof(TransparencySoftware)             => TransparencySoftwareRegistry.           ChangeableProperties,
                   nameof(TransparencySoftwareCertificate)  => TransparencySoftwareCertificateRegistry.ChangeableProperties,
                   nameof(ParkingGarage)                    => ChangeableParkingGarageProperties,
                   nameof(ParkingSpace)                     => ChangeableParkingSpaceProperties,
                   nameof(ParkingSensor)                    => ChangeableParkingSensorProperties,
                   nameof(ParkingSpaceGroup)                => ChangeableParkingSpaceGroupProperties,
                   nameof(ParkingProduct)                   => ChangeableParkingProductProperties,
                   _ when ChangeableGroupProperties.TryGetValue(EntityType, out var groupProperties)
                                                            => groupProperties,
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
            foreach (var chargingStationOperator in chargingStationOperators.Values)
            {

                Left.chargingStationOperators.TryGetValue(chargingStationOperator.Id, out var left);

                if (ReferenceEquals(chargingStationOperator, left))
                    continue;

                if (Right.chargingStationOperators.TryGetValue(chargingStationOperator.Id, out var right))
                    chargingStationOperator.ShareRuntimeOfAll(right);

                if (left is not null)
                    chargingStationOperator.ShareRuntimeOfAll(left);

            }

            foreach (var parkingOperator in parkingOperators.Values)
            {

                Left.parkingOperators.TryGetValue(parkingOperator.Id, out var left);

                if (ReferenceEquals(parkingOperator, left))
                    continue;

                if (Right.parkingOperators.TryGetValue(parkingOperator.Id, out var right))
                    parkingOperator.ShareRuntimeOfAll(right);

                if (left is not null)
                    parkingOperator.ShareRuntimeOfAll(left);

            }

            foreach (var eMobilityProvider in eMobilityProviders.Values)
                if (!Left.eMobilityProviders.ContainsKey(eMobilityProvider.Id) && Right.eMobilityProviders.TryGetValue(eMobilityProvider.Id, out var right))
                    eMobilityProvider.ShareRuntimeOf(right);

            foreach (var gridOperator in gridOperators.Values)
                if (!Left.gridOperators.ContainsKey(gridOperator.Id) && Right.gridOperators.TryGetValue(gridOperator.Id, out var right))
                    gridOperator.ShareRuntimeOf(right);

            foreach (var manufacturer in chargingStationManufacturers.Values)
                if (!Left.chargingStationManufacturers.ContainsKey(manufacturer.Id) && Right.chargingStationManufacturers.TryGetValue(manufacturer.Id, out var right))
                    manufacturer.ShareRuntimeOf(right);

        }

        #endregion

    }

}
