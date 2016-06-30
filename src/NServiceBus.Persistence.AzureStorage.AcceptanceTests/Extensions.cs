namespace NServiceBus.Persistence.AzureStorage.AcceptanceTests
{
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage.Table;

    public static class Extensions
    {
        public static async Task DeleteAllEntities(this CloudTable table)
        {
            TableQuerySegment<DynamicTableEntity> segment = null;

            while (segment == null || segment.ContinuationToken != null)
            {
                segment = await table.ExecuteQuerySegmentedAsync(new TableQuery().Take(100), segment?.ContinuationToken);
                foreach (var entity in segment.Results)
                {
                    await table.ExecuteAsync(TableOperation.Delete(entity));
                }
            }
        }

        public static async Task<int> CountAllEntities(this CloudTable table)
        {
            TableQuerySegment<DynamicTableEntity> segment = null;

            var count = 0;
            while (segment == null || segment.ContinuationToken != null)
            {
                segment = await table.ExecuteQuerySegmentedAsync(new TableQuery().Take(100), segment?.ContinuationToken);
                if (segment != null)
                {
                    count += segment.Results.Count;
                }
            }

            return count;
        }
    }
}