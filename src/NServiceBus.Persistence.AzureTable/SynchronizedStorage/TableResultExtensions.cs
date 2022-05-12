namespace NServiceBus.Persistence.AzureTable
{
    using Microsoft.Azure.Cosmos.Table;

    static class TableResultExtensions
    {
        public static bool IsSuccessStatusCode(this TableResult result)
        {
            return result.HttpStatusCode is >= 200 and <= 299;
        }
    }
}