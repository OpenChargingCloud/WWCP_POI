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

using Org.BouncyCastle.Crypto.Parameters;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{


    public sealed partial class RootCAInfo
    {

        private readonly I18NString immutableName;
        public I18NString Name => immutableName.Clone();
        public RootCAProtocol         Protocol     { get; }
        public ECPublicKeyParameters  PublicKey    { get; }
        public DateTime               NotBefore    { get; }
        public DateTime               NotAfter     { get; }
        public String                 Algorithm    { get; }
        private readonly I18NString immutableComment;
        public I18NString Comment => immutableComment.Clone();

        public RootCAInfo(I18NString             Name,
                          RootCAProtocol         Protocol,
                          ECPublicKeyParameters  PublicKey,
                          DateTime               NotBefore,
                          DateTime               NotAfter,
                          String                 Algorithm   = "P-256",
                          I18NString?            Comment     = null)
        {

            this.immutableName       = (Name).Clone();
            this.Protocol   = Protocol;
            this.PublicKey  = PublicKey;
            this.NotBefore  = NotBefore.ToUniversalTime();
            this.NotAfter   = NotAfter.ToUniversalTime();
            this.Algorithm  = Algorithm ?? "P-256";
            this.immutableComment    = (Comment   ?? I18NString.Empty).Clone();

        }


        #region Clone()

        /// <summary>
        /// Clone this root CA information.
        /// </summary>
        public RootCAInfo Clone()

            => new (
                   Name.     Clone(),
                   Protocol. Clone(),
                   PublicKey,
                   NotBefore,
                   NotAfter,
                   Algorithm.CloneString(),
                   Comment.  Clone()
               );

        #endregion

    }

}
