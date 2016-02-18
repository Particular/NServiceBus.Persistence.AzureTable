namespace NServiceBus.AzureStoragePersistence.ComponentTests.Timeouts
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    [Category("AzureStoragePersistence")]
    public class When_checking_if_a_timeout_has_a_lease
    {
        [SetUp]
        public void Perform_storage_cleanup()
        {
            TestHelper.PerformStorageCleanup();
        }

        [Test]
        public void if_the_timeout_doesnt_exist_there_will_be_no_lease()
        {
            var timeoutPersister = TestHelper.CreateTimeoutPersister();

            var timeoutData = TestHelper.GenerateTimeoutWithHeaders();
            timeoutData.Id = Guid.NewGuid().ToString();
            var hasLease = timeoutPersister.CanSend(timeoutData);

            Assert.That(hasLease, Is.False);
        }

        [Test]
        public void if_the_timeout_exists_it_should_never_not_get_a_lease()
        {
            var timeoutPersister = TestHelper.CreateTimeoutPersister();

            var timeoutData = TestHelper.GenerateTimeoutWithHeaders();

            timeoutPersister.Add(timeoutData);

            var hasLease = timeoutPersister.CanSend(timeoutData);

            Assert.That(hasLease, Is.Not.False);
        }

        [Test]
        public void if_the_timeout_exists_it_should_get_a_lease()
        {
            var timeoutPersister = TestHelper.CreateTimeoutPersister();

            var timeoutData = TestHelper.GenerateTimeoutWithHeaders();

            timeoutPersister.Add(timeoutData);

            var hasLease = timeoutPersister.CanSend(timeoutData);

            Assert.That(hasLease, Is.True);
        }

        [Test]
        public void if_the_timeout_exists_and_already_has_a_lease()
        {
            var timeoutPersister = TestHelper.CreateTimeoutPersister();

            var timeoutData = TestHelper.GenerateTimeoutWithHeaders();

            timeoutPersister.Add(timeoutData);

            timeoutPersister.CanSend(timeoutData);

            var secondLease = timeoutPersister.CanSend(timeoutData);
            Assert.That(secondLease, Is.True);
        }
    }
}