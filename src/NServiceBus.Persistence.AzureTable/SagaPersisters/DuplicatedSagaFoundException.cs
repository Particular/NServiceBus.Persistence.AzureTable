namespace NServiceBus.Persistence.AzureTable
{
    using System;

    /// <summary></summary>
    public class DuplicatedSagaFoundException : Exception
    {
        /// <summary></summary>
        public Type SagaType { get; }

        /// <summary></summary>
        public Guid[] Identifiers { get; }

        /// <summary></summary>
        public string PropertyName { get; }

        /// <summary></summary>
        public DuplicatedSagaFoundException(Type sagaType, string propertyName, params Guid[] identifiers)
            : base($"Sagas of type {sagaType.Name} with the following identifiers '{string.Join("', '", identifiers)}' are considered duplicates because of the violation of the Unique property {propertyName}.")
        {
            SagaType = sagaType;
            PropertyName = propertyName;
            Identifiers = identifiers;
        }
    }
}