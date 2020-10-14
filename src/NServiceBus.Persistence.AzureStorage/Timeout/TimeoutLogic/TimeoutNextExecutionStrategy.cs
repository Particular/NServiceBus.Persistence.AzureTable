namespace NServiceBus.Persistence.AzureStorage
{
    using System;

    class TimeoutNextExecutionStrategy
    {
        readonly TimeSpan backoffIncrease;
        readonly TimeSpan maximumBackoff;
        readonly Func<DateTime> currentDateTimeInUtc;

        TimeSpan currentBackoff;

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