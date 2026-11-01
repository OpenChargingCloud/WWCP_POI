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
    /// The local authorization of an app a verifier checks historical documents under: a trust context of its current
    /// decisions (RoamingNetworkTrustContext), or a historical one (HistoricalTrustContext), as WWCP_POI.TS takes
    /// "TrustContext | HistoricalTrustContext".
    /// </summary>
    public interface IRoamingNetworkTrustAuthority
    {

        /// <summary>
        /// The trust anchor a verifier checks against: of the holders of the imported one, those trusted locally.
        /// </summary>
        RoamingNetworkTrustAnchor           AuthorizedTrustAnchor       { get; }

        /// <summary>
        /// The roaming network.
        /// </summary>
        RoamingNetwork_Id                   RoamingNetworkId            { get; }

        /// <summary>
        /// The roaming network operator.
        /// </summary>
        RoamingNetworkOperator_Id           RoamingNetworkOperatorId    { get; }

        /// <summary>
        /// How many holders must sign.
        /// </summary>
        Byte                                RequiredConsensus           { get; }

        /// <summary>
        /// The SHA-256 of the content of the key statement it was evaluated of, as lowercase hex.
        /// </summary>
        String                              KeyStatementDigest          { get; }

        /// <summary>
        /// The time it was evaluated at: no document of a later time is checked under it.
        /// </summary>
        DateTimeOffset                      EvaluatedAt                 { get; }

        /// <summary>
        /// Until when it may be used.
        /// </summary>
        DateTimeOffset                      ValidUntil                  { get; }

        /// <summary>
        /// The epoch it is in.
        /// </summary>
        RoamingNetworkEpoch                 Epoch                       { get; }

        /// <summary>
        /// When the epoch began, where a verified trust transition says so.
        /// </summary>
        DateTimeOffset?                     EpochStartedAt              { get; }

        /// <summary>
        /// The epochs verified so far.
        /// </summary>
        IReadOnlyList<RoamingNetworkEpoch>  Epochs                      { get; }

        /// <summary>
        /// The trust context of the authority covering the given time, of verified trust transitions; none where it has no
        /// timeline, false where none covers it unambiguously.
        /// </summary>
        /// <param name="At">The historical time.</param>
        /// <param name="Authority">The trust context covering it, if any.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        Boolean TryGetAuthorityAt(DateTimeOffset                    At,
                                  out RoamingNetworkTrustContext?   Authority,
                                  [NotNullWhen(false)] out String?  ErrorResponse);

    }


    /// <summary>
    /// The fresh local permission of an app to check historical documents (WWCP_POI.TS HistoricalTrustContext): of a saved
    /// key statement of reference, verified at the time it was signed, so that it ending since needs neither a server nor a
    /// newly issued statement; or of the timeline of authorities verified trust transitions give. Its validity of one minute
    /// is a deadline of the operation, not of the historical documents: recreate it for every check, never store it.
    /// </summary>
    public sealed class HistoricalTrustContext : IRoamingNetworkTrustAuthority
    {

        #region Data

        /// <summary>
        /// How long a historical trust context may be used: one operation.
        /// </summary>
        public static readonly TimeSpan OperationDeadline = TimeSpan.FromMinutes(1);

        private readonly ImmutableArray<(RoamingNetworkTrustContext Context, DateTimeOffset From, DateTimeOffset Until)> authorities;

        #endregion

        #region Properties

        /// <inheritdoc/>
        public RoamingNetworkTrustAnchor             AuthorizedTrustAnchor       { get; }

        /// <inheritdoc/>
        public RoamingNetwork_Id                     RoamingNetworkId            { get; }

        /// <inheritdoc/>
        public RoamingNetworkOperator_Id             RoamingNetworkOperatorId    { get; }

        /// <inheritdoc/>
        public Byte                                  RequiredConsensus           { get; }

        /// <inheritdoc/>
        public String                                KeyStatementDigest          { get; }

        /// <inheritdoc/>
        public DateTimeOffset                        EvaluatedAt                 { get; }

        /// <summary>
        /// The deadline of the operation it was created for, not the validity of historical documents.
        /// </summary>
        public DateTimeOffset                        ValidUntil                  { get; }

        /// <summary>
        /// The @ids of the identity keys trusted locally per holder.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<String>>  TrustedHolders              { get; }

        /// <inheritdoc/>
        public RoamingNetworkEpoch                   Epoch                       { get; }

        /// <inheritdoc/>
        public DateTimeOffset?                       EpochStartedAt              { get; }

        /// <inheritdoc/>
        public IReadOnlyList<RoamingNetworkEpoch>    Epochs                      { get; }

        #endregion

        #region (private) Constructor(s)

        private HistoricalTrustContext(RoamingNetworkTrustContext                                                         Reference,
                                       DateTimeOffset                                                                     EvaluatedAt,
                                       IEnumerable<(RoamingNetworkTrustContext Context, DateTimeOffset From, DateTimeOffset Until)>?  Authorities   = null)
        {

            this.AuthorizedTrustAnchor     = Reference.AuthorizedTrustAnchor;
            this.RoamingNetworkId          = Reference.RoamingNetworkId;
            this.RoamingNetworkOperatorId  = Reference.RoamingNetworkOperatorId;
            this.RequiredConsensus         = Reference.RequiredConsensus;
            this.KeyStatementDigest        = Reference.KeyStatementDigest;
            this.TrustedHolders            = Reference.TrustedHolders;
            this.Epoch                     = Reference.Epoch;
            this.EpochStartedAt            = Reference.EpochStartedAt;
            this.Epochs                    = Reference.Epochs;
            this.EvaluatedAt               = EvaluatedAt;
            this.ValidUntil                = EvaluatedAt + OperationDeadline;
            this.authorities               = [.. Authorities ?? []];

        }

        #endregion


        #region (static) TryCreate(TrustAnchor, Reference, TrustedIdentityKeyIds, out Context, out ErrorResponse, OtherIdentityKeys = null, Now = null)

        /// <summary>
        /// Authorize historical checks of the fresh local decisions of an app: the given saved key statement of reference,
        /// verified against the given trust anchor at the time it was signed, of the identity keys the app trusts locally.
        /// </summary>
        /// <param name="TrustAnchor">The trust anchor, as imported.</param>
        /// <param name="Reference">The saved key statement of reference.</param>
        /// <param name="TrustedIdentityKeyIds">The @ids of the identity keys the app trusts locally.</param>
        /// <param name="Context">The historical trust context.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="OtherIdentityKeys">Other identity keys, e.g. those of the operator the statement was saved with.</param>
        /// <param name="Now">The time of the check, now by default.</param>
        public static Boolean TryCreate(RoamingNetworkTrustAnchor                          TrustAnchor,
                                        RoamingNetworkKeyStatement                         Reference,
                                        IEnumerable<String>                                TrustedIdentityKeyIds,
                                        [NotNullWhen(true)]  out HistoricalTrustContext?   Context,
                                        [NotNullWhen(false)] out String?                   ErrorResponse,
                                        IEnumerable<POIPublicKey>?                         OtherIdentityKeys   = null,
                                        DateTimeOffset?                                    Now                 = null)
        {

            Context = null;

            var now = Now ?? Timestamp.Now;

            if (Reference.SignedAt > now)
            {
                ErrorResponse = "The key statement of reference was signed after the time of the check!";
                return false;
            }

            if (!RoamingNetworkTrustContext.TryCreate(TrustAnchor, Reference, TrustedIdentityKeyIds, out var verified, out ErrorResponse, OtherIdentityKeys, Reference.SignedAt))
                return false;

            Context = new HistoricalTrustContext(verified, now);
            return true;

        }

        #endregion

        #region (static) TryCreate(Reference, Authorities, Now, out Context, out ErrorResponse)

        /// <summary>
        /// Authorize historical checks of the given timeline of authorities, each a trust context of a verified trust
        /// transition (RoamingNetworkVerifiedTrustTransition) and the times it covers, from inclusive, until exclusive.
        /// Reconstructed of the current local roots and verified transitions, never of stored results.
        /// </summary>
        /// <param name="Reference">The trust context of the current local decisions.</param>
        /// <param name="Authorities">The authorities and the times they cover.</param>
        /// <param name="Now">The time of the check.</param>
        /// <param name="Context">The historical trust context.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryCreate(RoamingNetworkTrustContext                                                         Reference,
                                        IEnumerable<(RoamingNetworkTrustContext Context, DateTimeOffset From, DateTimeOffset Until)>  Authorities,
                                        DateTimeOffset                                                                     Now,
                                        [NotNullWhen(true)]  out HistoricalTrustContext?                                   Context,
                                        [NotNullWhen(false)] out String?                                                   ErrorResponse)
        {

            Context = null;

            var authorities = Authorities.ToArray();

            if (authorities.Length == 0 ||
                authorities.Any(authority => authority.Context.RoamingNetworkId         != Reference.RoamingNetworkId         ||
                                             authority.Context.RoamingNetworkOperatorId != Reference.RoamingNetworkOperatorId ||
                                             authority.Until                            <= authority.From))
            {
                ErrorResponse = "Invalid historical authority lineage!";
                return false;
            }

            Context        = new HistoricalTrustContext(Reference, Now, authorities);
            ErrorResponse  = null;
            return true;

        }

        #endregion


        #region TryGetAuthorityAt(At, out Authority, out ErrorResponse)

        /// <inheritdoc/>
        public Boolean TryGetAuthorityAt(DateTimeOffset                    At,
                                         out RoamingNetworkTrustContext?   Authority,
                                         [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Authority      = null;
            ErrorResponse  = null;

            if (authorities.IsEmpty)
                return true;

            var matches = authorities.Where(authority => At >= authority.From && At < authority.Until).ToArray();

            if (matches.Length != 1)
            {
                ErrorResponse = "No unambiguous authorized policy covers this historical time!";
                return false;
            }

            Authority = matches[0].Context;
            return true;

        }

        #endregion


        #region (static) TryGetOperatorContext(Current, Operator, At, out Context, out ErrorResponse)

        /// <summary>
        /// The trust context of the given roaming network operator of a historical document, e.g. as an old proof gives it,
        /// at the given time of that document: its key statement, of the network and operator of the current authorization,
        /// verified then against the authorized trust anchor, or the one of the authority covering that time; in the epoch
        /// of either. Without an authority it must not require fewer holders than the current authorization.
        /// </summary>
        /// <param name="Current">The current local authorization.</param>
        /// <param name="Operator">The roaming network operator, as the historical document gives it.</param>
        /// <param name="At">The time of the historical document.</param>
        /// <param name="Context">The trust context of that operator at that time.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryGetOperatorContext(IRoamingNetworkTrustAuthority                      Current,
                                                    JToken?                                            Operator,
                                                    DateTimeOffset                                     At,
                                                    [NotNullWhen(true)]  out RoamingNetworkTrustContext?  Context,
                                                    [NotNullWhen(false)] out String?                   ErrorResponse)
        {

            Context = null;

            if (Operator is not JObject operatorJSON ||
                operatorJSON[RoamingNetworkOperator.PropertyKeys.KeyStatement] is not JObject statementJSON)
            {
                ErrorResponse = "A historical roaming network operator must give its key statement!";
                return false;
            }

            if (!RoamingNetworkKeyStatement.TryParse(statementJSON, out var statement, out ErrorResponse))
                return false;

            if (statement.RoamingNetworkId                != Current.RoamingNetworkId         ||
                statement.RoamingNetworkOperatorId        != Current.RoamingNetworkOperatorId ||
                VerifiedChargingStation.IdOf(operatorJSON) != Current.RoamingNetworkOperatorId.ToString())
            {
                ErrorResponse = "The historical statement belongs to another authorized network/operator!";
                return false;
            }

            if (!Current.TryGetAuthorityAt(At, out var authority, out ErrorResponse))
                return false;

            if (!POIPartyKeys.TryParse(operatorJSON, out _, out var identityKeys, out ErrorResponse))
                return false;

            var anchor = authority?.AuthorizedTrustAnchor ?? Current.AuthorizedTrustAnchor;

            if (!RoamingNetworkTrustContext.TryCreate(anchor, statement, anchor.Keys, out var created, out ErrorResponse, identityKeys, At))
                return false;

            var context = RoamingNetworkTrustContext.WithEpoch(created,
                                                               authority?.Epoch          ?? Current.Epoch,
                                                               authority is not null ? authority.EpochStartedAt : Current.EpochStartedAt,
                                                               Current.Epochs);

            if (authority is null && context.RequiredConsensus < Current.RequiredConsensus)
            {
                ErrorResponse = "Historical verification must not weaken the current local quorum!";
                return false;
            }

            Context = context;
            return true;

        }

        #endregion

    }

}
