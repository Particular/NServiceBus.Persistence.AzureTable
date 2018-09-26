namespace NServiceBus.Persistence.AzureStorage
{
    using System;

    class TimeoutNextExecutionStrategy
    {
        readonly TimeSpan backoffIncrease;
        readonly TimeSpan maximumBackoff;
        readonly Func<DateTime> currentDateTimeInUtc;

        TimeSpan currentBackoff;

        /// <summary>
        /// </summary>
        /// <param name="backoffIncrease">The amount of time to increase back-off each time.</param>
        /// <param name="maximumBackoff">The maximum amount of time to back-off.</param>
        /// <param name="currentDateTimeInUtc">Current DateTime.NowUtc generator.</param>
        public TimeoutNextExecutionStrategy(TimeSpan backoffIncrease, TimeSpan maximumBackoff, Func<DateTime> currentDateTimeInUtc)
        {
            this.backoffIncrease = backoffIncrease;
            this.maximumBackoff = maximumBackoff;
            this.currentDateTimeInUtc = currentDateTimeInUtc;
        }

        DateTime BackOff()
        {
            if (currentBackoff + backoffIncrease < maximumBackoff)
            {
                currentBackoff += backoffIncrease;
            }
            else
            {
                currentBackoff = maximumBackoff;
            }

            return currentDateTimeInUtc().Add(currentBackoff);
        }

        public DateTime GetNextRun(DateTime? lastSuccessfulRead = null, TimeoutDataEntity future = null)
        {
            if (lastSuccessfulRead == null && future == null)
            {
                return BackOff();
            }

            currentBackoff = TimeSpan.Zero;
            return lastSuccessfulRead ?? future.Time;

        }

    }
}