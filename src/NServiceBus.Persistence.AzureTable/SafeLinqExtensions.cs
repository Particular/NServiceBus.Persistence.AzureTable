namespace NServiceBus.Persistence.AzureTable
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using Azure;

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
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
            {
                return default;
            }
        }
    }
}