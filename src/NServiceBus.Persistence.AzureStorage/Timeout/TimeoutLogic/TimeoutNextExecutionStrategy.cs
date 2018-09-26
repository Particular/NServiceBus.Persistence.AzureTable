namespace NServiceBus.Persistence.AzureStorage
{
    using System;

    class TimeoutNextExecutionStrategy
    {
        readonly TimeSpan backoffIncrease;
        readonly TimeSpan maximumBackoff;

        TimeSpan currentBackoff;

        /// <summary>
        /// </summary>
        /// <param name="backoffIncrease">The amount of time to increase back-off each time.</param>
        /// <param name="maximumBackoff">The maximum amount of time to back-off.</param>
        public TimeoutNextExecutionStrategy(TimeSpan backoffIncrease, TimeSpan maximumBackoff)
        {
            this.backoffIncrease = backoffIncrease;
            this.maximumBackoff = maximumBackoff;
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

            return DateTime.UtcNow.Add(currentBackoff);
        }

        public DateTime GetNextRun(DateTime? lastSuccessfulRead, TimeoutDataEntity future)
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