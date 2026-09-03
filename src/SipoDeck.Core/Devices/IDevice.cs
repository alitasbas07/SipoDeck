namespace SipoDeck.Core.Devices;

/// <summary>
/// Bir transport üzerinden bağlanan fiziksel cihazı temsil eder.
/// </summary>
public interface IDevice
{
    string Id { get; }

    string Name { get; }
}
