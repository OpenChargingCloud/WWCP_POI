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

using System.Collections;
using System.Collections.Immutable;
using Newtonsoft.Json.Linq;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// An immutable multilingual value, detached from Illias' mutable I18NString.
/// </summary>
public sealed partial class ImmutableI18NString : IEnumerable<I18NPair>, IEquatable<ImmutableI18NString>
{
    private readonly ImmutableArray<I18NPair> texts;
    public ImmutableI18NString(I18NString text) => texts = text.ToImmutableArray();
    public static ImmutableI18NString Empty { get; } = new(I18NString.Empty);
    public UInt32 Count => (UInt32) texts.Length;
    public String this[Languages language] => ToMutable()[language];
    public Boolean Has(Languages language) => texts.Any(text => text.Language == language);
    public Boolean IsNullOrEmpty() => texts.IsEmpty;
    public Boolean IsNotNullOrEmpty() => !texts.IsEmpty;
    public String FirstText() => texts.IsEmpty ? "" : texts[0].Text;
    public ImmutableI18NString Clone() => this;
    public ImmutableI18NString With(Languages language, String text)
        => new(ToMutable().Set(language, text));
    public JObject ToJSON() => ToMutable().ToJSON();
    public I18NString ToMutable() => new(texts);
    public static implicit operator ImmutableI18NString(I18NString value) => new(value);
    public static implicit operator I18NString(ImmutableI18NString value) => value.ToMutable();
    public IEnumerator<I18NPair> GetEnumerator() => ((IEnumerable<I18NPair>) texts).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public Boolean Equals(ImmutableI18NString? other) => other is not null && ToMutable().Equals(other.ToMutable());
    public override Boolean Equals(Object? other) => other is ImmutableI18NString text && Equals(text);
    public override Int32 GetHashCode() => ToMutable().GetHashCode();
    public override String ToString() => ToMutable().ToString();
}
