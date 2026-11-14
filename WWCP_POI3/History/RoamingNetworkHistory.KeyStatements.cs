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

using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The key statements of the operator of the roaming network of a history, one after the other (SPECIFICATION.md,
    /// section 8.6, Succession): who holds its identity keys, and how many of them decide, changes with its change sets,
    /// a holder joining or leaving. A verifier that pinned a trust anchor some time ago follows them from the keys it
    /// trusts to those of the head (RoamingNetworkKeyStatement.TryVerifyChain), without replaying the history.
    /// </summary>
    public sealed partial class RoamingNetworkHistory
    {

        #region Data

        /// <summary>
        /// The most key statements given at once: whoever asks is not known, and what it is given grows with every
        /// change of the holders.
        /// </summary>
        public const Int32 DefaultMaxKeyStatements = 100;

        #endregion

        #region KeyStatements(MaxCount = DefaultMaxKeyStatements)

        /// <summary>
        /// The key statements of the operator of the roaming network this history knows of, the oldest first, the one of
        /// its head last, each once: the one its first commit changing it found, where it holds such a commit, those the
        /// commits the head follows stated since, and the one of the head. None for an operator stating no keys. A
        /// history started from a snapshot, or one that dropped older commits (10.6), begins with the oldest it still
        /// knows of, and of more than the given number the latest are given: a verifier trusting only older keys needs
        /// a newer trust anchor then. They say who held the keys one after the other, not that the last of them is
        /// the one of today: that, the attestations of the head say (8.5).
        /// </summary>
        /// <param name="MaxCount">The maximum number of key statements to return, the latest ones.</param>
        public IReadOnlyList<RoamingNetworkKeyStatement> KeyStatements(Int32 MaxCount = DefaultMaxKeyStatements)
        {

            var view        = View;
            var head        = Head;
            var followed    = view.Ancestors(head.Commit.Id);
            var statements  = new List<RoamingNetworkKeyStatement>();

            void Add(JToken? JSON)
            {

                if (JSON is JObject json &&
                    RoamingNetworkKeyStatement.TryParse(json, out var keyStatement, out _) &&
                    (statements.Count == 0 || !Same(statements[^1], keyStatement)))
                {
                    statements.Add(keyStatement);
                }

            }

            foreach (var commit in view.Commits)
            {

                if (commit.ChangeSet is null || !followed.Contains(commit.Id))
                    continue;

                foreach (var change in commit.ChangeSet.Changes)
                {

                    if (change.EntityType   != nameof(RoamingNetworkOperator)                    ||
                        change.PropertyName != RoamingNetworkOperator.PropertyKeys.KeyStatement  ||
                        change.IsElementChange)
                    {
                        continue;
                    }

                    // The statement before the first change this history holds, as that change found it.
                    if (statements.Count == 0)
                        Add(change.OldValue);

                    Add(change.NewValue);

                }

            }

            // Always the last: the one of the head, as its version states it.
            if (head.RoamingNetwork.Operator?.KeyStatement is RoamingNetworkKeyStatement current &&
                (statements.Count == 0 || !Same(statements[^1], current)))
            {
                statements.Add(current);
            }

            return MaxCount > 0 && statements.Count > MaxCount
                       ? statements.GetRange(statements.Count - MaxCount, MaxCount)
                       : statements;

        }

        private static Boolean Same(RoamingNetworkKeyStatement  Left,
                                    RoamingNetworkKeyStatement  Right)

            => JToken.DeepEquals(Left.ToJSON(Embedded: true), Right.ToJSON(Embedded: true));

        #endregion

    }

}
