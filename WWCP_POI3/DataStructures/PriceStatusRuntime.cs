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

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The price status schedule of one version of a charging pool, a charging station or an EVSE.
    /// Like the admin status and the status, it belongs to the entity, not to one of its versions:
    /// a new version takes the schedule of the one before, and raises its events only while somebody listens.
    /// Without a reported price status, there is none.
    /// </summary>
    internal sealed class PriceStatusRuntime
    {

        #region Data

        /// <summary>
        /// The default number of price statuses kept: they may be reported often.
        /// </summary>
        public const UInt16 DefaultMaxPriceStatusScheduleSize = 10;

        private readonly Lock                                                                                       runtimeLock = new ();
        private readonly Func<DateTimeOffset, EventTracking_Id, Timestamped<ChargingPrices>, Timestamped<ChargingPrices>, Context?, Task>  raise;
        private          Boolean                                                             listens;

        #endregion

        #region Properties

        /// <summary>
        /// The price status schedule, shared by all versions of the entity.
        /// </summary>
        public RuntimeStatusSchedule<ChargingPrices>  Schedule  { get; private set; }

        /// <summary>
        /// The current price status, if there is one already.
        /// </summary>
        public Timestamped<ChargingPrices>?  Current
            => Schedule.Any(priceStatus => priceStatus.Timestamp <= Timestamp.Now)
                   ? Schedule.CurrentStatus
                   // Not a bare null: it would become a Timestamped<ChargingPrices> of no prices, since now.
                   : (Timestamped<ChargingPrices>?) null;

        /// <summary>
        /// The next planned price status.
        /// </summary>
        public Timestamped<ChargingPrices>?  Next
            => Schedule.NextStatus;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create the price status schedule of a new entity.
        /// </summary>
        /// <param name="Raise">Raises the price status events of the entity: timestamp, event tracking identification, new status, old status, data source.</param>
        /// <param name="MaxPriceStatusScheduleSize">The number of price statuses kept.</param>
        public PriceStatusRuntime(Func<DateTimeOffset, EventTracking_Id, Timestamped<ChargingPrices>, Timestamped<ChargingPrices>, Context?, Task>  Raise,
                                   UInt16?                                                                                              MaxPriceStatusScheduleSize   = null)
        {

            this.raise     = Raise;
            this.Schedule  = new RuntimeStatusSchedule<ChargingPrices>(MaxPriceStatusScheduleSize ?? DefaultMaxPriceStatusScheduleSize);

        }

        #endregion


        #region ShareOf(Previous)

        /// <summary>
        /// Take the price status schedule of the previous version of the entity.
        /// Only for a new version nobody listens to yet.
        /// </summary>
        public void ShareOf(PriceStatusRuntime Previous)
        {
            lock (runtimeLock)
            {

                if (listens)
                    throw new InvalidOperationException("Only a new version nobody listens to yet can take the price status of the one before!");

                Schedule = Previous.Schedule;

            }
        }

        #endregion

        #region Listen(Listen)

        /// <summary>
        /// Raise the price status events of this version while Listen is true.
        /// </summary>
        public void Listen(Boolean Listen)
        {
            lock (runtimeLock)
            {

                if (Listen == listens)
                    return;

                if (Listen)
                    Schedule.OnStatusChanged += ScheduleChanged;
                else
                    Schedule.OnStatusChanged -= ScheduleChanged;

                listens = Listen;

            }
        }

        #endregion

        #region Set(PriceStatus, Timestamp, DataSource)

        /// <summary>
        /// Insert the given price status into the schedule, which ignores one repeating the price status in effect before it.
        /// </summary>
        public void Set(ChargingPrices      PriceStatus,
                        DateTimeOffset  Timestamp,
                        Context?        DataSource)

            => Schedule.Insert(PriceStatus, Timestamp, DataSource);

        #endregion

        #region Take(PriceStatus, Timestamp, DataSource)

        /// <summary>
        /// Take the given price status as it is, also when it repeats the one in effect before it.
        /// </summary>
        public void Take(ChargingPrices      PriceStatus,
                         DateTimeOffset  Timestamp,
                         Context?        DataSource)

            => Schedule.Set([ new Timestamped<ChargingPrices>(Timestamp, PriceStatus) ], ChangeMethods.Insert, DataSource);

        #endregion


        #region (private) ScheduleChanged(...)

        private Task ScheduleChanged(DateTimeOffset                     Timestamp,
                                     EventTracking_Id                   EventTrackingId,
                                     RuntimeStatusSchedule<ChargingPrices>  Schedule,
                                     Timestamped<ChargingPrices>            NewStatus,
                                     Timestamped<ChargingPrices>            OldStatus,
                                     Context?                           DataSource)

            => raise(Timestamp, EventTrackingId, NewStatus, OldStatus, DataSource);

        #endregion

    }

}
