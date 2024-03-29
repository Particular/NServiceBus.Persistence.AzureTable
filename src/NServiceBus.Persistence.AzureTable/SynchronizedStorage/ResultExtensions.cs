﻿namespace NServiceBus.Persistence.AzureTable
{
    using Azure;

    static class ResultExtensions
    {
        public static bool IsSuccessStatusCode(this Response result)
            => result.Status is >= 200 and <= 299;
    }
}