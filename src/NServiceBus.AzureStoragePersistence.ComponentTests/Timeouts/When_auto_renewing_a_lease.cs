namespace NServiceBus.AzureStoragePersistence.ComponentTests.Timeouts
{
    using System;
    using System.Threading;
    using Microsoft.WindowsAzure.Storage.Blob;
    using NServiceBus.Azure;
    using NUnit.Framework;

    [TestFixture]
    [Category("AzureStoragePersistence")]
    public class When_auto_renewing_a_lease
    {
        public void Setup()
        {
            TestHelper.PerformStorageCleanup();
        }
        
        [Test]
        public void the_lease_should_be_released_when_auto_renewal_ends()
        {
            var persister = TestHelper.CreateTimeoutPersister();
            var timeout = TestHelper.GenerateTimeoutWithHeaders();
            persister.Add(timeout);
            
            var cloudBlockBlob = TestHelper.CreateTimeoutCloudBlockBlob(timeout.Id);
            
            AutoRenewLease lease;
            using (lease = new AutoRenewLease(cloudBlockBlob))
            {
                Assert.That(lease.HasLease, Is.True);
            }
            Assert.That(cloudBlockBlob.Properties.LeaseStatus, Is.Not.EqualTo(LeaseStatus.Locked));
        }

        [Test]
        public void the_lease_should_be_held_for_longer_than_60_seconds()
        {
            var persister = TestHelper.CreateTimeoutPersister();
            var timeout = TestHelper.GenerateTimeoutWithHeaders();
            persister.Add(timeout);

            var cloudBlockBlob = TestHelper.CreateTimeoutCloudBlockBlob(timeout.Id);

            using (var lease = new AutoRenewLease(cloudBlockBlob))
            {
                Assert.That(lease.HasLease, Is.True);

                Thread.Sleep(TimeSpan.FromSeconds(65));

                Assert.That(lease.HasLease, Is.True);
            }
        }
    }
}