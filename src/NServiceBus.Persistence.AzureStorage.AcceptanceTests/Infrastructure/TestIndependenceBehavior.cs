using System;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NServiceBus.Pipeline;

class TestIndependenceSkipBehavior : IBehavior<ITransportReceiveContext, ITransportReceiveContext>
{
    string testRunId;

    public TestIndependenceSkipBehavior(ScenarioContext scenarioContext)
    {
        testRunId = scenarioContext.TestRunId.ToString();
    }

    public Task Invoke(ITransportReceiveContext context, Func<ITransportReceiveContext, Task> next)
    {
        string runId;
        if (!context.Message.Headers.TryGetValue("$AcceptanceTesting.TestRunId", out runId) || runId != testRunId)
        {
            Console.WriteLine($"Skipping message {context.Message.MessageId} from previous test run");
            return Task.FromResult(0);
        }

        return next(context);
    }
}
