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

using System.Runtime.CompilerServices;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// Static metadata for infrastructure entities. Mutable IInternalData commands are
/// explicitly rejected, including through IEntity casts. InternalData is runtime context.
/// </summary>
public abstract class AImmutableInternalData : IInternalData
{
    private readonly CustomDataNew customData;
    public CustomDataNew CustomData => customData.Clone();
    public UserDefinedDictionary InternalData { get; }
    public DateTimeOffset Created { get; private set; }
    public DateTimeOffset LastChangeDate { get; private set; }
    public Boolean IsEmpty => InternalData.IsEmpty;
    public Boolean IsNotEmpty => InternalData.IsNotEmpty;
    public event OnPropertyChangedDelegate? OnPropertyChanged;

    protected AImmutableInternalData(CustomDataNew? customData, UserDefinedDictionary? internalData,
                                     DateTimeOffset? created, DateTimeOffset? lastChange)
    {
        var now = Timestamp.Now;
        Created = created ?? lastChange ?? now;
        LastChangeDate = lastChange ?? created ?? now;
        this.customData = (customData ?? CustomDataNew.Empty).Clone();
        InternalData = internalData ?? new UserDefinedDictionary();
    }

    protected void RestoreTimestamps(DateTimeOffset created, DateTimeOffset lastChange)
    {
        Created = created;
        LastChangeDate = lastChange;
    }

    // Used only by construction setters and dynamic real-time properties.
    // Dynamic updates do not change static timestamps.
    protected void SetProperty<T>(ref T field, T value, Context? DataSource = null,
                                  EventTracking_Id? EventTrackingId = null,
                                  [CallerMemberName] String PropertyName = "")
    {
        var oldValue = field;
        field = value;
        PropertyChanged(PropertyName, value, oldValue, DataSource, EventTrackingId);
    }
    protected void DeleteProperty<T>(ref T? field, [CallerMemberName] String PropertyName = "")
    {
        var oldValue = field;
        field = default;
        PropertyChanged(PropertyName, field, oldValue);
    }
    protected void PropertyChanged<T>(String PropertyName, T NewValue, T OldValue,
                                      Context? DataSource = null, EventTracking_Id? EventTrackingId = null)
        => OnPropertyChanged?.Invoke(Timestamp.Now, EventTrackingId ?? EventTracking_Id.New,
                                     this, PropertyName, NewValue, OldValue, DataSource);

    private static InvalidOperationException Immutable()
        => new("POI data is immutable. Apply a ChangeSet to produce a new network version.");
    DateTimeOffset IInternalData.Created { get => Created; set => throw Immutable(); }
    DateTimeOffset IInternalData.LastChangeDate { get => LastChangeDate; set => throw Immutable(); }
    void IInternalData.SetProperty<T>(ref T field, T value, Context? source, EventTracking_Id? id, String name) => throw Immutable();
    void IInternalData.DeleteProperty<T>(ref T? field, String name) where T : default => throw Immutable();
    void IInternalData.PropertyChanged<T>(String name, T oldValue, T newValue, Context? source, EventTracking_Id? id) => throw Immutable();
    public Boolean IsDefined(String key) => InternalData.ContainsKey(key);
    public Boolean IsDefined(String key, Object? value) => InternalData.Contains(key, value);
    public void IfDefined(String key, Action<Object> action) => InternalData.IfDefined(key, action);
    public void IfDefinedAs<T>(String key, Action<T> action) => InternalData.IfDefinedAs(key, action);
    public Object? GetInternalData(String key) => InternalData.Get(key);
    public T? GetInternalDataAs<T>(String key) => InternalData.GetAs<T>(key);
    public Boolean TryGetInternalData(String key, out Object? value) => InternalData.TryGet(key, out value);
    public Boolean TryGetInternalDataAs<T>(String key, out T? value) => InternalData.TryGetAs(key, out value);
    public SetPropertyResult SetInternalData(String key, Object? value, Object? oldValue = null,
                                             Context? source = null, EventTracking_Id? id = null)
        => InternalData.Set(key, value, oldValue, source, id);
}
