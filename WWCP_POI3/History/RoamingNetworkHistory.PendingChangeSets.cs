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
    /// What became of a change set given to be signed by the holders of a quorum, or of its withdrawal.
    /// </summary>
    public enum RoamingNetworkCoSigningStatus
    {

        /// <summary>
        /// It waits for the signatures still missing.
        /// </summary>
        Pending,

        /// <summary>
        /// Its owners signed it, and it was committed now.
        /// </summary>
        Committed,

        /// <summary>
        /// It was committed before: nothing changed.
        /// </summary>
        AlreadyCommitted,

        /// <summary>
        /// It was withdrawn by an owner of one of its changes, and waits no longer.
        /// </summary>
        Withdrawn,

        /// <summary>
        /// It was refused, e.g. as a signature is invalid, no owner signed it, or it fits no version; a change set waiting
        /// for signatures was dropped, when it can never be committed.
        /// </summary>
        Refused,

        /// <summary>
        /// No change set of this identification waits for signatures.
        /// </summary>
        NotFound

    }


    /// <summary>
    /// The result of giving a change set to be signed by the holders of a quorum, or of withdrawing one.
    /// </summary>
    /// <param name="Status">The outcome.</param>
    /// <param name="ChangeSet">The change set with the signatures it has, unless refused or not found.</param>
    /// <param name="Missing">The signatures still missing, by change, while it waits.</param>
    /// <param name="Commit">Its commit, when committed now or before.</param>
    /// <param name="ErrorResponse">Why it was refused.</param>
    public sealed record RoamingNetworkCoSigningResult(RoamingNetworkCoSigningStatus  Status,
                                                       RoamingNetworkChangeSet?       ChangeSet       = null,
                                                       IReadOnlyList<String>?         Missing         = null,
                                                       RoamingNetworkCommit?          Commit          = null,
                                                       String?                        ErrorResponse   = null)
    {

        /// <summary>
        /// Whether the change set is part of the history now.
        /// </summary>
        public Boolean IsCommitted
            => Status is RoamingNetworkCoSigningStatus.Committed or RoamingNetworkCoSigningStatus.AlreadyCommitted;

        /// <summary>
        /// The JSON representation of this result: its status, the change set, the signatures missing, the identification
        /// of its commit and why it was refused, as far as they are known.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty("status", Status switch {
                                                       RoamingNetworkCoSigningStatus.Pending           => "pending",
                                                       RoamingNetworkCoSigningStatus.Committed         => "committed",
                                                       RoamingNetworkCoSigningStatus.AlreadyCommitted  => "alreadyCommitted",
                                                       RoamingNetworkCoSigningStatus.Withdrawn         => "withdrawn",
                                                       RoamingNetworkCoSigningStatus.NotFound          => "notFound",
                                                       _                                               => "refused"
                                                   })
                       );

            if (ChangeSet is not null)
                json.Add("changeSet",     ChangeSet.ToJSON(Embedded: true));

            if (Missing is not null && Missing.Count > 0)
                json.Add("missing",       new JArray(Missing));

            if (Commit is not null)
                json.Add("commitId",      Commit.Id.ToString());

            if (ErrorResponse is not null)
                json.Add("description",   ErrorResponse);

            return json;

        }

    }


    /// <summary>
    /// Called for every change set that starts or keeps waiting for signatures, or waits no longer, while the history is
    /// locked: return quickly and do not change the history from here.
    /// </summary>
    /// <param name="Result">The change set with the signatures it has and those still missing, or why it waits no longer.</param>
    public delegate void OnRoamingNetworkPendingChangeSetDelegate(RoamingNetworkCoSigningResult Result);


    public sealed partial class RoamingNetworkHistory
    {

        #region Data

        /// <summary>
        /// The change sets waiting for the signatures of their owners, in the order they came, by their identifications.
        /// </summary>
        private readonly OrderedDictionary<ChangeSet_Id, RoamingNetworkChangeSet> pendingChangeSets = new ();

        #endregion

        #region Properties

        /// <summary>
        /// How many change sets may wait for signatures at once; 1,000 by default.
        /// </summary>
        public UInt32 MaxPendingChangeSets { get; set; } = 1000;

        /// <summary>
        /// The change sets waiting for the signatures of their owners (SPECIFICATION.md, section 4.2), with those they
        /// have, in the order they came.
        /// </summary>
        public IReadOnlyList<RoamingNetworkChangeSet> PendingChangeSets
        {
            get
            {
                lock (historyLock)
                {
                    return [ .. pendingChangeSets.Values ];
                }
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Called for every change set that starts or keeps waiting for signatures, or waits no longer, while the history
        /// is locked: return quickly and do not change the history from here.
        /// </summary>
        public event OnRoamingNetworkPendingChangeSetDelegate?  OnPendingChangeSet;

        #endregion


        #region TryGetPendingChangeSet(ChangeSetId, out Result)

        /// <summary>
        /// The change set of the given identification waiting for signatures, with those it has and those still missing.
        /// </summary>
        /// <param name="ChangeSetId">The identification of the change set.</param>
        /// <param name="Result">The change set, its signatures and those still missing.</param>
        public Boolean TryGetPendingChangeSet(ChangeSet_Id                                             ChangeSetId,
                                              [NotNullWhen(true)] out RoamingNetworkCoSigningResult?   Result)
        {

            lock (historyLock)
            {

                if (!pendingChangeSets.TryGetValue(ChangeSetId, out var changeSet))
                {
                    Result = null;
                    return false;
                }

                Result = FindVersionFor(changeSet) is (_, RoamingNetwork version) &&
                         version.TryCheckCoSigning(changeSet, out var missing, out _, out _)
                             ? new (RoamingNetworkCoSigningStatus.Pending, changeSet, missing)
                             : new (RoamingNetworkCoSigningStatus.Pending, changeSet);

                return true;

            }

        }

        #endregion

        #region CoSign(ChangeSet, VerifySignature = null)

        /// <summary>
        /// Give the given change set to be signed by the holders of a quorum, or add the signatures it has to the change
        /// set of its identification waiting already (SPECIFICATION.md, section 4.2). It must fit a version, every signature
        /// must be valid, and an owner of one of its changes must have signed it. Once its owners signed it, by as many
        /// holders as their quorums need, it is committed; until then it waits, here, and is kept by the store.
        /// </summary>
        /// <param name="ChangeSet">The change set, signed by an owner of one of its changes at least.</param>
        /// <param name="VerifySignature">An optional verifier of its signatures, in addition to those of its owners.</param>
        public RoamingNetworkCoSigningResult CoSign(RoamingNetworkChangeSet            ChangeSet,
                                                    VerifyChangeSetSignatureDelegate?  VerifySignature   = null)
        {

            lock (historyLock)
            {

                if (ChangeSet is null)
                    return Refused("The given change set must not be null!");

                if (ChangeSet.RoamingNetworkId != RoamingNetworkId)
                    return Refused($"The change set is for the roaming network '{ChangeSet.RoamingNetworkId}', not for '{RoamingNetworkId}'!");

                if (ClosedError() is String closed)
                    return Refused(closed);

                foreach (var signature in ChangeSet.Signatures)
                    if (SignatureAlgorithmError(signature.Algorithm, "a signature of the change set") is String algorithmError)
                        return Refused(algorithmError);

                if (FindCommitted(ChangeSet) is RoamingNetworkCommit committed)
                    return new (RoamingNetworkCoSigningStatus.AlreadyCommitted, ChangeSet, Commit: committed);

                #region The signatures of the change set waiting already, and the new ones

                var waiting    = pendingChangeSets.TryGetValue(ChangeSet.Id, out var pending) ? pending : null;
                var changeSet  = ChangeSet;

                if (waiting is not null)
                {

                    if (!JToken.DeepEquals(waiting.  ToJSON(Embedded: true, IncludeSignatures: false),
                                           ChangeSet.ToJSON(Embedded: true, IncludeSignatures: false)))
                    {
                        return Refused($"The change set '{ChangeSet.Id}' is another one than the change set of this identification waiting for signatures!");
                    }

                    changeSet = waiting.WithSignatures([ .. waiting.Signatures,
                                                         .. ChangeSet.Signatures.Where(signature => !waiting.Signatures.Contains(signature)) ]);

                }

                #endregion

                if (FindVersionFor(changeSet) is not (_, RoamingNetwork version))
                    return Dropped(waiting, "The change set fits no version of this history: prepare it again for the head!");

                if (!version.TryCheckCoSigning(changeSet, out var missing, out var signedByAnOwner, out var signatureError))
                    return Refused(signatureError);

                if (!signedByAnOwner)
                    return Refused("A change set waits for the signatures of its owners only once an owner of one of its changes signed it!");

                if (VerifySignature is not null)
                    for (var index = 0; index < changeSet.Signatures.Length; index++)
                        if (!VerifySignature(changeSet, changeSet.Signatures[index]))
                            return Refused($"Signature {index} ('{changeSet.Signatures[index].KeyId}') is invalid!");

                #region Signed by its owners: committed

                if (missing.Count == 0)
                {

                    var commitResult = Commit(changeSet, VerifySignature);

                    if (commitResult.IsCommitted)
                    {

                        // Committed: Remember(…) let it go already, and told the subscribers.
                        return new (commitResult.Status == RoamingNetworkCommitStatus.Committed
                                        ? RoamingNetworkCoSigningStatus.Committed
                                        : RoamingNetworkCoSigningStatus.AlreadyCommitted,
                                    changeSet,
                                    Commit: commitResult.Commit);

                    }

                    return Dropped(waiting, commitResult.ErrorResponse ?? "The change set was not committed!");

                }

                #endregion

                #region Not yet: it waits

                if (waiting is null)
                {

                    if (pendingChangeSets.Count >= MaxPendingChangeSets)
                        return Refused($"There are {pendingChangeSets.Count} change sets waiting for signatures, as many as may wait!");

                    // Whether it will be committed once its owners signed it.
                    if (!version.TryApplyPendingChangeSet(changeSet, out _, out var applyError))
                        return Refused(applyError);

                }

                else if (changeSet.Signatures.Length == waiting.Signatures.Length)
                    return new (RoamingNetworkCoSigningStatus.Pending, waiting, missing);

                // Kept before it is shown: a store failing keeps it from waiting.
                try
                {
                    StoreAppend(RoamingNetworkHistoryLogEntry.ForPendingChangeSet(changeSet));
                }
                catch (Exception e)
                {
                    return Refused("The change set can not be stored: " + e.Message);
                }

                pendingChangeSets[changeSet.Id] = changeSet;

                var pendingResult = new RoamingNetworkCoSigningResult(RoamingNetworkCoSigningStatus.Pending, changeSet, missing);

                Announce(pendingResult);

                return pendingResult;

                #endregion

            }

        }

        #endregion

        #region AddPendingSignatures(ChangeSetId, Signatures, VerifySignature = null)

        /// <summary>
        /// Add the given signatures to the change set of the given identification waiting for signatures; as CoSign(…) of
        /// it with them.
        /// </summary>
        /// <param name="ChangeSetId">The identification of the change set.</param>
        /// <param name="Signatures">Its new signatures.</param>
        /// <param name="VerifySignature">An optional verifier of its signatures, in addition to those of its owners.</param>
        public RoamingNetworkCoSigningResult AddPendingSignatures(ChangeSet_Id                                    ChangeSetId,
                                                                  IEnumerable<RoamingNetworkChangeSetSignature>   Signatures,
                                                                  VerifyChangeSetSignatureDelegate?               VerifySignature   = null)
        {

            lock (historyLock)
            {

                if (!pendingChangeSets.TryGetValue(ChangeSetId, out var waiting))
                    return new (RoamingNetworkCoSigningStatus.NotFound, ErrorResponse: $"No change set '{ChangeSetId}' waits for signatures!");

                return CoSign(waiting.WithSignatures([ .. Signatures ]), VerifySignature);

            }

        }

        #endregion

        #region Withdraw(Withdrawal)

        /// <summary>
        /// Withdraw the change set waiting for signatures that the given withdrawal names: signed by a key of an owner of
        /// one of its changes, of the version it fits. One fitting no version waits no longer anyway.
        /// </summary>
        /// <param name="Withdrawal">The withdrawal.</param>
        public RoamingNetworkCoSigningResult Withdraw(RoamingNetworkChangeSetWithdrawal Withdrawal)
        {

            lock (historyLock)
            {

                if (Withdrawal is null)
                    return Refused("The given withdrawal must not be null!");

                if (Withdrawal.RoamingNetworkId != RoamingNetworkId)
                    return Refused($"The withdrawal is for the roaming network '{Withdrawal.RoamingNetworkId}', not for '{RoamingNetworkId}'!");

                if (!pendingChangeSets.TryGetValue(Withdrawal.ChangeSetId, out var waiting))
                    return new (RoamingNetworkCoSigningStatus.NotFound, ErrorResponse: $"No change set '{Withdrawal.ChangeSetId}' waits for signatures!");

                if (SignatureAlgorithmError(Withdrawal.Algorithm, "the withdrawal") is String algorithmError)
                    return Refused(algorithmError);

                if (FindVersionFor(waiting) is not (_, RoamingNetwork version))
                    return Dropped(waiting, "The change set fits no version of this history: prepare it again for the head!");

                if (!version.TryCheckWithdrawal(Withdrawal, waiting, out var withdrawalError))
                    return Refused(withdrawalError);

                try
                {
                    Drop(waiting);
                }
                catch (Exception e)
                {
                    return Refused("The withdrawal can not be stored: " + e.Message);
                }

                var result = new RoamingNetworkCoSigningResult(RoamingNetworkCoSigningStatus.Withdrawn, waiting);

                Announce(result);

                return result;

            }

        }

        #endregion


        #region (private) DropUnfittingPendingChangeSets()

        /// <summary>
        /// Let go of the change sets waiting for signatures that fit no version any longer, e.g. after a compaction,
        /// while the history is locked.
        /// </summary>
        private void DropUnfittingPendingChangeSets()
        {

            foreach (var changeSet in pendingChangeSets.Values.ToArray())
                if (FindVersionFor(changeSet) is null)
                    Dropped(changeSet, "The change set fits no version of this history any longer!");

        }

        #endregion

        #region (private) PendingChangeSetLogEntries()

        /// <summary>
        /// The log entries of the change sets waiting for signatures, for a compacted store.
        /// </summary>
        private IEnumerable<RoamingNetworkHistoryLogEntry> PendingChangeSetLogEntries()

            => [ .. pendingChangeSets.Values.Select(RoamingNetworkHistoryLogEntry.ForPendingChangeSet) ];

        #endregion

        #region (private) TakeStoredPendingChangeSet(Entry)

        /// <summary>
        /// Take the given log entry of a change set waiting for signatures, or no longer, while the history is loaded.
        /// </summary>
        private void TakeStoredPendingChangeSet(RoamingNetworkHistoryLogEntry Entry)
        {

            if (Entry.Type == RoamingNetworkHistoryLogEntryType.PendingChangeSetDropped)
                pendingChangeSets.Remove(Entry.ChangeSetId!.Value);

            else if (Entry.ChangeSet is RoamingNetworkChangeSet changeSet && FindCommitted(changeSet) is null)
                pendingChangeSets[changeSet.Id] = changeSet;

        }

        #endregion

        #region (private) ForgetPendingChangeSet(Commit, ChangeSet)

        /// <summary>
        /// Let go of the change set waiting for signatures of the given commit of a change set, or of those it was rebased
        /// from, while the history is locked: its commit tells the store it waits no longer.
        /// </summary>
        private void ForgetPendingChangeSet(RoamingNetworkCommit     Commit,
                                            RoamingNetworkChangeSet  ChangeSet)
        {
            foreach (var changeSetId in ChangeSetIds(ChangeSet))
                if (pendingChangeSets.Remove(changeSetId, out var waiting) && !loading)
                    Announce(new (RoamingNetworkCoSigningStatus.Committed, waiting, Commit: Commit));
        }

        #endregion

        #region (private) Drop(ChangeSet) / Dropped(ChangeSet, ErrorResponse) / Refused(ErrorResponse) / Announce(Result)

        /// <summary>
        /// Let go of the given change set waiting for signatures, telling the store first.
        /// </summary>
        private void Drop(RoamingNetworkChangeSet ChangeSet)
        {
            StoreAppend(RoamingNetworkHistoryLogEntry.ForPendingChangeSetDropped(ChangeSet.Id));
            pendingChangeSets.Remove(ChangeSet.Id);
        }

        /// <summary>
        /// Refused for the given reason; a change set waiting for signatures, when given, waits no longer, as it can
        /// never be committed.
        /// </summary>
        private RoamingNetworkCoSigningResult Dropped(RoamingNetworkChangeSet?  ChangeSet,
                                                      String                    ErrorResponse)
        {

            var result = Refused(ErrorResponse);

            if (ChangeSet is not null && pendingChangeSets.ContainsKey(ChangeSet.Id))
            {

                try
                {
                    Drop(ChangeSet);
                }
                catch (Exception e)
                {
                    return Refused(ErrorResponse + " Letting it go can not be stored: " + e.Message);
                }

                Announce(result with { ChangeSet = ChangeSet });

            }

            return result;

        }

        private static RoamingNetworkCoSigningResult Refused(String ErrorResponse)

            => new (RoamingNetworkCoSigningStatus.Refused, ErrorResponse: ErrorResponse);

        /// <summary>
        /// Tell the subscribers about the given change set waiting, or no longer: one failing must not keep it from the others.
        /// </summary>
        private void Announce(RoamingNetworkCoSigningResult Result)
        {
            foreach (var subscriber in OnPendingChangeSet?.GetInvocationList().Cast<OnRoamingNetworkPendingChangeSetDelegate>() ?? [])
            {
                try
                {
                    subscriber(Result);
                }
                catch
                { }
            }
        }

        #endregion

    }

}
