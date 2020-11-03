namespace NServiceBus.Persistence.AzureTable
{
    using Microsoft.Azure.Cosmos.Table;

    static class TableResultExtensions
    {
        public static bool IsSuccessStatusCode(this TableResult result)
        {
            return result.HttpStatusCode >= 200 && result.HttpStatusCode <= 299;
        }
    }
}