namespace NServiceBus.AcceptanceTests.ScenarioDescriptors
{
    using NServiceBus.AcceptanceTesting.Support;

    public static class TransactionSettings
    {
        static TransactionSettings()
        {
            DistributedTransaction = new RunDescriptor("DistributedTransaction");

            LocalTransaction = new RunDescriptor("LocalTransaction");
            LocalTransaction.Settings.Set("Transactions.SuppressDistributedTransactions", bool.TrueString);

            NoTransaction = new RunDescriptor("NoTransaction");
            NoTransaction.Settings.Set("Transactions.Disable", bool.TrueString);
        }

        public static readonly RunDescriptor DistributedTransaction;
        public static readonly RunDescriptor LocalTransaction;
        public static readonly RunDescriptor NoTransaction;
    }
}
