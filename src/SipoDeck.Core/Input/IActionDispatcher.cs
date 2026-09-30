using SipoDeck.Core.Actions;

namespace SipoDeck.Core.Input;

/// <summary>
/// Aktif profilin ürettiği eylemleri çalıştırma sorumluluğu olan bileşene aktarır.
/// Input Engine eylemi kendisi çalıştırmaz; yalnızca bu sözleşme üzerinden teslim eder.
/// Çağrı, Input Engine'in iç kilidi dışında yapılır (uzun basma zamanlayıcısı dahil),
/// böylece eylem çalıştırması girdi değerlendirmesini bloklamaz.
/// </summary>
public interface IActionDispatcher
{
    void Dispatch(IAction action);
}
