namespace NServiceBus.Persistence.AzureStorage
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Azure.Cosmos.Table;

    static class SafeLinqExtensions
    {
        public static TSource SafeFirstOrDefault<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                return default;
            }

            try
            {
                return source.FirstOrDefault();
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == 404)
                {
                    return default;
                }

                throw;
            }
        }
    }
}