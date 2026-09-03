namespace SipoDeck.Core.Transport;

/// <summary>
/// Cihazlarla veri alışverişini sağlayan iletişim kanalını temsil eder.
/// </summary>
public interface ITransport
{
    string Name { get; }

    bool IsConnected { get; }
}
