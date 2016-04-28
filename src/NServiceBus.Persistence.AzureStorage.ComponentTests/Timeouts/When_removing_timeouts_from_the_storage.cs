namespace NServiceBus.Persistence.AzureStorage.ComponentTests.Timeouts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    [Category("AzureStoragePersistence")]
    public class When_removing_timeouts_from_the_storage
    {
        [SetUp]
        public void Perform_storage_cleanup()
        {
            TestHelper.PerformStorageCleanup();
        }

        [Test]
        public async Task Should_return_true_when_timeout_is_TryRemoved_successfully()
        {
            var timeoutPersister = TestHelper.CreateTimeoutPersister();

            var timeout = TestHelper.GenerateTimeoutWithHeaders();
            await timeoutPersister.Add(timeout, null);

            var timeouts = await TestHelper.GetAllTimeoutsUsingGetNextChunk(timeoutPersister);

            Assert.True(timeouts.Count == 1);

            var wasRemoved = await timeoutPersister.TryRemove(timeouts.First().Item1, null);
            Assert.True(wasRemoved);
        }

        [Test]
        public async Task Should_return_correct_headers_when_timeout_is_Peeked()
        {
            var timeoutPersister = TestHelper.CreateTimeoutPersister();

            var timeout = TestHelper.GenerateTimeoutWithHeaders();
            await timeoutPersister.Add(timeout, null);

            var timeouts = await TestHelper.GetAllTimeoutsUsingGetNextChunk(timeoutPersister);

            Assert.AreEqual(timeouts.Count, 1);

            var timeoutId = timeouts.First().Item1;
            var timeoutData = await timeoutPersister.Peek(timeoutId, null);

            CollectionAssert.AreEqual(new Dictionary<string, string>
            {
                {"Prop1", "1234"},
                {"Prop2", "text"}
            }, timeoutData.Headers);
        }

        [Test]
        public async Task Peek_should_return_null_for_non_existing_timeout()
        {
            var timeoutPersister = TestHelper.CreateTimeoutPersister();

            var timeoutData = await timeoutPersister.Peek("A2B34534324F3435A324234C", null);

            Assert.IsNull(timeoutData);
        }

        [Test]
        public async Task Should_remove_timeouts_by_id_and_return_true_using_new_interface()
        {
            var timeoutPersister = TestHelper.CreateTimeoutPersister();
            var timeout1 = TestHelper.GenerateTimeoutWithHeaders();
            var timeout2 = TestHelper.GenerateTimeoutWithHeaders();
            await timeoutPersister.Add(timeout1, null);
            await timeoutPersister.Add(timeout2, null);

            var timeouts = await TestHelper.GetAllTimeoutsUsingGetNextChunk(timeoutPersister);
            Assert.IsTrue(timeouts.Count == 2);

            var itemRemoved = true;
            foreach (var timeout in timeouts)
            {
                itemRemoved &= await timeoutPersister.TryRemove(timeout.Item1, null);
            }

            Assert.IsTrue(itemRemoved, "Expected 2 invocations to return true, but one or both of them returned false");

            await TestHelper.AssertAllTimeoutsThatHaveBeenRemoved(timeoutPersister);
        }

        [Test]
        public async Task Should_return_false_if_timeout_already_deleted_for_TryRemove_invocation()
        {
            var timeoutPersister = TestHelper.CreateTimeoutPersister();

            var timeout = TestHelper.GenerateTimeoutWithHeaders();
            await timeoutPersister.Add(timeout, null);

            var timeouts = await TestHelper.GetAllTimeoutsUsingGetNextChunk(timeoutPersister);
            Assert.IsTrue(timeouts.Count == 1);

            var timeoutId = timeouts.First().Item1;

            Assert.IsTrue(await timeoutPersister.TryRemove(timeoutId, null));
            Assert.IsFalse(await timeoutPersister.TryRemove(timeoutId, null));
        }

        [Test]
        public async Task Should_remove_timeouts_by_sagaid()
        {
            var timeoutPersister = TestHelper.CreateTimeoutPersister();
            var sagaId1 = Guid.NewGuid();
            var sagaId2 = Guid.NewGuid();
            var timeout1 = TestHelper.GenerateTimeoutWithSagaId(sagaId1);
            var timeout2 = TestHelper.GenerateTimeoutWithSagaId(sagaId2);
            await timeoutPersister.Add(timeout1, null);
            await timeoutPersister.Add(timeout2, null);

            var timeouts = await TestHelper.GetAllTimeoutsUsingGetNextChunk(timeoutPersister);
            Assert.IsTrue(timeouts.Count == 2);

            await timeoutPersister.RemoveTimeoutBy(sagaId1, null);
            await timeoutPersister.RemoveTimeoutBy(sagaId2, null);

            await TestHelper.AssertAllTimeoutsThatHaveBeenRemoved(timeoutPersister);
        }

        [Test]
        public async Task TryRemove_should_work_with_concurrent_operations()
        {
            var timeoutPersister = TestHelper.CreateTimeoutPersister();
            var timeout = TestHelper.GenerateTimeoutWithHeaders();

            await timeoutPersister.Add(timeout, null);

            var task1 = timeoutPersister.TryRemove(timeout.Id, null);
            var task2 = timeoutPersister.TryRemove(timeout.Id, null);

            await Task.WhenAll(task1, task2).ConfigureAwait(false);

            Assert.IsTrue(task1.Result || task2.Result);
        }
    }
}