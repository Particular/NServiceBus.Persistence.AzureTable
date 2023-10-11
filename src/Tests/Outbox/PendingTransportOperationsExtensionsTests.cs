namespace NServiceBus.Persistence.AzureTable.Tests
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using Routing;
    using Transport;

    [TestFixture]
    public class PendingTransportOperationsExtensionsTests
    {
        [Test]
        public void Should_clear_existing_operations()
        {
            var operations = new PendingTransportOperations();
            operations.Add(new TransportOperation(
                new OutgoingMessage("", [], ReadOnlyMemory<byte>.Empty),
                new UnicastAddressTag("someQueue")));

            operations.Clear();

            Assert.That(operations.Operations, Is.Empty);
        }

        [Test]
        public void Should_support_adding_after_clearing()
        {
            var operations = new PendingTransportOperations();
            operations.Add(new TransportOperation(
                new OutgoingMessage("1", [], ReadOnlyMemory<byte>.Empty),
                new UnicastAddressTag("someQueue")));

            operations.Clear();

            operations.Add(new TransportOperation(
                new OutgoingMessage("2", [], ReadOnlyMemory<byte>.Empty),
                new UnicastAddressTag("someQueue")));

            operations.Clear();

            operations.Add(new TransportOperation(
                new OutgoingMessage("3", [], ReadOnlyMemory<byte>.Empty),
                new UnicastAddressTag("someQueue")));

            Assert.That(operations.Operations, Has.Length.EqualTo(1));
        }
    }
}