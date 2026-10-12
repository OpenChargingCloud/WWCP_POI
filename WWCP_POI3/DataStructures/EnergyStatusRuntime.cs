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
    /// The energy status schedule of one version of an EVSE or a charging station.
    /// Like the admin status and the status, it belongs to the entity, not to one of its versions:
    /// a new version takes the schedule of the one before, and raises its events only while somebody listens.
    /// Without a reported energy status, there is none.
    /// </summary>
    internal sealed class EnergyStatusRuntime
    {

        #region Data

        /// <summary>
        /// The default number of energy statuses kept: they may be reported often.
        /// </summary>
        public const UInt16 DefaultMaxEnergyStatusScheduleSize = 10;

        private readonly Lock                                                                                       runtimeLock = new ();
        private readonly Func<DateTimeOffset, EventTracking_Id, Timestamped<EnergyInfo>, Timestamped<EnergyInfo>, Context?, Task>  raise;
        private          Boolean                                                             listens;

        #endregion

        #region Properties

        /// <summary>
        /// The energy status schedule, shared by all versions of the entity.
        /// </summary>
        public RuntimeStatusSchedule<EnergyInfo>  Schedule  { get; private set; }

        /// <summary>
        /// The current energy status, if there is one already.
        /// </summary>
        public Timestamped<EnergyInfo>?  Current
            => Schedule.Any(energyStatus => energyStatus.Timestamp <= Timestamp.Now)
                   ? Schedule.CurrentStatus
                   : null;

        /// <summary>
        /// The next planned energy status.
        /// </summary>
        public Timestamped<EnergyInfo>?  Next
            => Schedule.NextStatus;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create the energy status schedule of a new entity.
        /// </summary>
        /// <param name="Raise">Raises the energy status events of the entity: timestamp, event tracking identification, new status, old status, data source.</param>
        /// <param name="MaxEnergyStatusScheduleSize">The number of energy statuses kept.</param>
        public EnergyStatusRuntime(Func<DateTimeOffset, EventTracking_Id, Timestamped<EnergyInfo>, Timestamped<EnergyInfo>, Context?, Task>  Raise,
                                   UInt16?                                                                                              MaxEnergyStatusScheduleSize   = null)
        {

            this.raise     = Raise;
            this.Schedule  = new RuntimeStatusSchedule<EnergyInfo>(MaxEnergyStatusScheduleSize ?? DefaultMaxEnergyStatusScheduleSize);

        }

        #endregion


        #region ShareOf(Previous)

        /// <summary>
        /// Take the energy status schedule of the previous version of the entity.
        /// Only for a new version nobody listens to yet.
        /// </summary>
        public void ShareOf(EnergyStatusRuntime Previous)
        {
            lock (runtimeLock)
            {

                if (listens)
                    throw new InvalidOperationException("Only a new version nobody listens to yet can take the energy status of the one before!");

                Schedule = Previous.Schedule;

            }
        }

        #endregion

        #region Listen(Listen)

        /// <summary>
        /// Raise the energy status events of this version while Listen is true.
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

        #region Set(EnergyStatus, Timestamp, DataSource)

        /// <summary>
        /// Insert the given energy status into the schedule, which ignores one repeating the energy status in effect before it.
        /// </summary>
        public void Set(EnergyInfo      EnergyStatus,
                        DateTimeOffset  Timestamp,
                        Context?        DataSource)

            => Schedule.Insert(EnergyStatus, Timestamp, DataSource);

        #endregion

        #region Take(EnergyStatus, Timestamp, DataSource)

        /// <summary>
        /// Take the given energy status as it is, also when it repeats the one in effect before it.
        /// </summary>
        public void Take(EnergyInfo      EnergyStatus,
                         DateTimeOffset  Timestamp,
                         Context?        DataSource)

            => Schedule.Set([ new Timestamped<EnergyInfo>(Timestamp, EnergyStatus) ], ChangeMethods.Insert, DataSource);

        #endregion


        #region (private) ScheduleChanged(...)

        private Task ScheduleChanged(DateTimeOffset                     Timestamp,
                                     EventTracking_Id                   EventTrackingId,
                                     RuntimeStatusSchedule<EnergyInfo>  Schedule,
                                     Timestamped<EnergyInfo>            NewStatus,
                                     Timestamped<EnergyInfo>            OldStatus,
                                     Context?                           DataSource)

            => raise(Timestamp, EventTrackingId, NewStatus, OldStatus, DataSource);

        #endregion

    }

}
