namespace NServiceBus.Timeout.TimeoutLogic
{
    using NServiceBus.Timeout.Core;
    using System.Collections.Generic;

    public class TimoutChunkComparer : IEqualityComparer<TimeoutsChunk.Timeout>
    {
        public bool Equals(TimeoutsChunk.Timeout x, TimeoutsChunk.Timeout y)
        {
            return x.Id == y.Id && x.DueTime == y.DueTime;
        }

        public int GetHashCode(TimeoutsChunk.Timeout obj)
        {
            return (obj.DueTime.GetHashCode() * 397) ^ (obj.Id?.GetHashCode() ?? 0);
        }
    }
}