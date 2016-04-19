namespace NServiceBus.AzureStoragePersistence.SagaDeduplicator.Tests.Index
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SagaDeduplicator.Index;
    using NUnit.Framework;

    public class IdHashBufferTests
    {
        static readonly Guid G1 = new Guid("11111111-F069-4FF0-8052-6872006DA7B2");
        static readonly Guid G2 = new Guid("22222222-F069-4FF0-8052-6872006DA7B2");
        static readonly Guid G3 = new Guid("33333333-F069-4FF0-8052-6872006DA7B2");
        static readonly Guid G4 = new Guid("44444444-F069-4FF0-8052-6872006DA7B2");
        static readonly Guid G5 = new Guid("55555555-F069-4FF0-8052-6872006DA7B2");
        static readonly Guid G6 = new Guid("66666666-F069-4FF0-8052-6872006DA7B2");
        static readonly Guid G7 = new Guid("77777777-F069-4FF0-8052-6872006DA7B2");
        static readonly Guid G8 = new Guid("88888888-F069-4FF0-8052-6872006DA7B2");

        [Test]
        public void When_buffer_is_full_Should_not_accept_writes()
        {
            var buffer = new IdHashBuffer(2);

            Assert.True(buffer.TryWrite(Guid.Empty, 1));
            Assert.True(buffer.TryWrite(Guid.Empty, 1));

            Assert.False(buffer.TryWrite(Guid.Empty, 1));
        }

        [Test]
        public void When_buffer_is_not_sealed_Should_not_accept_searches()
        {
            var buffer = new IdHashBuffer(2);

            Assert.Throws<InvalidOperationException>(() =>
            {
                buffer.FindHashCollisions(Enumerable.Empty<IdHashBuffer>(), (hash, guid) => { });
            });
        }

        [Test]
        public void When_buffer_is_sealed_Should_not_accept_writes()
        {
            var buffer = new IdHashBuffer(2);
            buffer.Seal();

            Assert.Throws<InvalidOperationException>(() =>
            {
                buffer.TryWrite(Guid.Empty, 0);
            });
        }

        [Test]
        public void When_buffer_searched_for_collisions_with_itself_Should_not_find_singular_occurrences()
        {
            var buffer = new IdHashBuffer(2);
            buffer.TryWrite(G1, 1);
            buffer.TryWrite(G2, 2);

            buffer.Seal();

            buffer.FindHashCollisions(new[] { buffer }, (hash, ids) => { Assert.Fail(); });
        }

        [Test]
        public void When_buffer_not_filled_Should_find_collision_properly()
        {
            var buffer = new IdHashBuffer(1024);
            buffer.TryWrite(G1, 1);
            const int expected = 100;
            buffer.TryWrite(G2, expected);
            buffer.TryWrite(G3, expected);
            buffer.TryWrite(G4, 20000);

            buffer.Seal();

            var assertionMet = false;
            buffer.FindHashCollisions(new[] { buffer }, (hash, ids) =>
            {
                Assert.AreEqual(expected, hash);
                CollectionAssert.AreEquivalent(new[] { G2, G3 }, ids);
                assertionMet = true;
            });

            Assert.True(assertionMet);
        }

        [Test]
        public void FinalTest()
        {
            var buffer1 = new IdHashBuffer(4);
            var buffer2 = new IdHashBuffer(4);

            buffer1.TryWrite(G1, 1);
            buffer1.TryWrite(G2, 2);
            buffer1.TryWrite(G3, 3);
            buffer1.TryWrite(G4, 3);

            buffer1.Seal();

            buffer2.TryWrite(G5, 5);
            buffer2.TryWrite(G6, 2);
            buffer2.TryWrite(G7, 3);
            buffer2.TryWrite(G8, 5);

            buffer2.Seal();

            var collisions = new List<Tuple<ulong, Guid[]>>();
            Action<ulong, ArraySegment<Guid>> onCollision = (hash, ids) =>
            {
                collisions.Add(new Tuple<ulong, Guid[]>(hash, ids.ToArray()));
            };

            buffer1.FindHashCollisions(new[] { buffer1, buffer2 }, onCollision);
            buffer2.FindHashCollisions(new[] { buffer2 }, onCollision);

            var collisionDictionary = collisions
                .GroupBy(t => t.Item1)
                .Select(g => Tuple.Create(g.Key, g.SelectMany(x => x.Item2).Distinct().ToArray()))
                .ToDictionary(t => t.Item1, t => t.Item2);

            Assert.AreEqual(3, collisionDictionary.Count);
            CollectionAssert.AreEquivalent(new[] { G2, G6 }, collisionDictionary[2]);
            CollectionAssert.AreEquivalent(new[] { G3, G4, G7 }, collisionDictionary[3]);
            CollectionAssert.AreEquivalent(new[] { G5, G8 }, collisionDictionary[5]);
        }
    }
}