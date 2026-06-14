namespace PortfolioTracker.Api.Services.Messaging;

public sealed class MessageBrokerUnavailableException(string message, Exception innerException)
    : Exception(message, innerException);
