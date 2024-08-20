namespace NServiceBus.Persistence.AzureTable.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class LRUCacheTests
    {
        public class When_cache_is_empty
        {
            LRUCache<int, int> Empty = new LRUCache<int, int>(0);

            [Test]
            public void Should_not_throw_on_remove()
            {
                Empty.Remove(1);
            }

            [Test]
            public void Should_not_find_any_value()
            {
                AssertNoValue(Empty, 1);
            }
        }

        public class When_cache_is_full
        {
            LRUCache<int, int> cache;
            const int Key1 = 1;
            const int Key2 = 2;
            const int Key3 = 3;
            const int Value1 = 11;
            const int Value11 = 111;
            const int Value2 = 22;
            const int Value3 = 32;

            [SetUp]
            public void SetUp()
            {
                cache = new LRUCache<int, int>(2);
                cache.Put(Key1, Value1);
                cache.Put(Key2, Value2);
            }

            [Test]
            public void Should_preserve_values()
            {
                AssertValue(cache, Key1, Value1);
                AssertValue(cache, Key2, Value2);
            }

            [Test]
            public void Should_add_new_value_removing_the_oldest()
            {
                cache.Put(Key3, Value3);

                AssertNoValue(cache, Key1);
                AssertValue(cache, Key2, Value2);
                AssertValue(cache, Key3, Value3);
            }

            [Test]
            public void Should_update_existing_value_reordering_lru_properly()
            {
                cache.Put(Key1, Value11);
                cache.Put(Key3, Value3);

                AssertNoValue(cache, Key2);
                AssertValue(cache, Key1, Value11);
                AssertValue(cache, Key3, Value3);
            }

            [Test]
            public void Should_create_a_slot_when_removing()
            {
                cache.Remove(Key2);
                cache.Put(Key3, Value3);

                AssertNoValue(cache, Key2);
                AssertValue(cache, Key1, Value1);
                AssertValue(cache, Key3, Value3);
            }
        }

        static void AssertValue(LRUCache<int, int> lruCache, int key, int expectedValue)
        {
            Assert.That(lruCache.TryGet(key, out var value), Is.True);
            Assert.AreEqual(expectedValue, value);
        }

        static void AssertNoValue(LRUCache<int, int> lruCache, int key)
        {
            var tryGet = lruCache.TryGet(key, out var value);
            Assert.AreEqual(false, tryGet);
            Assert.AreEqual(default(int), value);
        }
    }
}