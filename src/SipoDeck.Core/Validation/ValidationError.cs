namespace SipoDeck.Core.Validation;

/// <summary>
/// Tek bir alan hatası. <see cref="Field"/> arayüzün eşleyebileceği bir yoldur
/// (ör. "Connection.Host", "Profiles[2].Keys[3].Action.Url").
/// </summary>
public sealed record ValidationError(string Field, string Message);
