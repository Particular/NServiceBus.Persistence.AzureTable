namespace NServiceBus.Azure
{
    using System;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;

    public static class LeaseBlobExtensions
    {
        public static string TryAcquireLease(this CloudBlockBlob blob)
        {
            try { return blob.AcquireLease(TimeSpan.FromSeconds(60), null); }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode != 409)
                {
                    throw ;
                }
                return null;
            }
        }

        public static bool TryRenewLease(this CloudBlockBlob blob, string leaseId)
        {
            try { blob.RenewLease(new AccessCondition
                {
                    LeaseId = leaseId
                }); return true; }
            catch { return false; }
        }

    }
}