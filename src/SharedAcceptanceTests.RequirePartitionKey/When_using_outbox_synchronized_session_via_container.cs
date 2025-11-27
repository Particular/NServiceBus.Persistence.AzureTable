namespace NServiceBus.AcceptanceTests;

using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using Persistence.AzureTable;
using NUnit.Framework;

[TestFixture]
public partial class When_using_outbox_synchronized_session_via_container : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_inject_synchronized_session_into_handler()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(b => b.When(s => s.SendLocal(new MyMessage())))
            .Done(c => c.Done)
            .Run();

        Assert.Multiple(() =>
        {
            Assert.That(context.RepositoryHasBatch, Is.True);
            Assert.That(context.RepositoryHasTable, Is.True);
        });
        AssertPartitionPart(context);
    }

    partial void AssertPartitionPart(Context scenarioContext);

    public class Context : ScenarioContext
    {
        public bool Done { get; set; }
        public bool RepositoryHasBatch { get; set; }
        public bool RepositoryHasTable { get; set; }
        public string PartitionKey { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() =>
            EndpointSetup<DefaultServer>(config =>
            {
                config.EnableOutbox();
                config.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                config.RegisterComponents(c =>
                {
                    c.AddScoped<MyRepository>();
                });
            });

        public class MyMessageHandler(MyRepository repository, Context context) : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext handlerContext)
            {
                repository.DoSomething();
                context.Done = true;
                return Task.CompletedTask;
            }
        }
    }

    public class MyRepository(IAzureTableStorageSession storageSession, Context context)
    {
        public void DoSomething()
        {
            context.RepositoryHasBatch = storageSession.Batch != null;
            context.RepositoryHasTable = storageSession.Table != null;
            context.PartitionKey = storageSession.PartitionKey;
        }
    }

    public class MyMessage : IMessage
    {
        public string Property { get; set; }
    }
}