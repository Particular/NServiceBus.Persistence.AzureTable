namespace NServiceBus.Persistence.AzureStorage.Previous
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    /// This is a copy of the saga persister code 2.4.1
    /// </summary>
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