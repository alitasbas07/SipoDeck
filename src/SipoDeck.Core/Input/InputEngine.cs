using System.Threading;
using SipoDeck.Core.Actions;
using SipoDeck.Core.Devices;
using SipoDeck.Core.Profiles;
using SipoDeck.Core.Protocol;

namespace SipoDeck.Core.Input;

/// <summary>
/// Cihaz tuş olaylarını aktif profil üzerinden yorumlar ve ilgili eylemleri çalıştırır.
/// Değerlendirme sırası: FN ile profil değiştirme → kombinasyonlar → tekli tuşlar.
/// Uzun basma, yapılandırılabilir eşik süre üzerinden zamanlayıcı ile tetiklenir.
/// </summary>
public sealed class InputEngine : IInputEngine, IDisposable
{
    private readonly ProfileManager _profiles;
    private readonly IActionDispatcher _dispatcher;
    private readonly TimeSpan _longPressThreshold;
    private readonly int? _fnKey;
    private readonly IReadOnlyDictionary<int, string> _fnProfileSwitchMap;
    private readonly TimeProvider _timeProvider;

    private readonly object _gate = new();
    private readonly HashSet<int> _pressedKeys = new();
    private readonly HashSet<int> _consumedByCombination = new();
    private readonly HashSet<int> _longPressFired = new();
    private readonly Dictionary<int, ITimer> _longPressTimers = new();

    /// <param name="profiles">Aktif profili sağlayan profil yöneticisi.</param>
    /// <param name="dispatcher">Üretilen eylemlerin teslim edileceği bileşen (Input Engine eylemi kendisi çalıştırmaz).</param>
    /// <param name="longPressThreshold">Uzun basma eşiği. Varsayılan koda gömülmez; çağıran tarafından verilir.</param>
    /// <param name="fnKey">FN olarak kullanılacak tuş numarası (isteğe bağlı, yapılandırılabilir).</param>
    /// <param name="fnProfileSwitchMap">FN + tuş → profil kimliği eşleştirmesi.</param>
    /// <param name="timeProvider">Zaman kaynağı; verilmezse sistem sağlayıcısı kullanılır.</param>
    public InputEngine(
        ProfileManager profiles,
        IActionDispatcher dispatcher,
        TimeSpan longPressThreshold,
        int? fnKey = null,
        IReadOnlyDictionary<int, string>? fnProfileSwitchMap = null,
        TimeProvider? timeProvider = null)
    {
        _profiles = profiles;
        _dispatcher = dispatcher;
        _longPressThreshold = longPressThreshold;
        _fnKey = fnKey;
        _fnProfileSwitchMap = fnProfileSwitchMap ?? new Dictionary<int, string>();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public void Process(IDeviceEvent deviceEvent)
    {
        if (deviceEvent is not KeyEvent keyEvent)
            return;

        if (keyEvent.State == ButtonState.Pressed)
            HandlePress(keyEvent.Button);
        else
            HandleRelease(keyEvent.Button);
    }

    private void HandlePress(int key)
    {
        IAction? toExecute = null;

        lock (_gate)
        {
            _pressedKeys.Add(key);

            // 1) FN + tuş → profil değiştirme (global, en yüksek öncelik).
            if (_fnKey is int fn && key != fn && _pressedKeys.Contains(fn)
                && _fnProfileSwitchMap.TryGetValue(key, out var targetProfileId))
            {
                _consumedByCombination.Add(key);
                _profiles.SwitchTo(targetProfileId);
                return;
            }

            var profile = _profiles.ActiveProfile;
            if (profile is null || !profile.IsEnabled)
                return;

            // FN tuşu tek başına eylem üretmez; yalnızca kombinasyon için kullanılır.
            if (_fnKey is int fnKey && key == fnKey)
                return;

            // 2) Aktif profil kombinasyonları (tekli tuşlardan önce değerlendirilir).
            foreach (var combination in profile.Combinations)
            {
                if (combination.Key.Contains(key) && combination.Key.IsSatisfiedBy(_pressedKeys))
                {
                    foreach (var combinationKey in combination.Key.Keys)
                        _consumedByCombination.Add(combinationKey);
                    toExecute = combination.Value;
                    break;
                }
            }

            // 3) Tekli tuş: normal eylem tuş bırakıldığında çalışır (kombinasyonlara
            //    öncelik verilebilmesi için). Uzun basma tanımlıysa eşik zamanlayıcısı kurulur.
            if (toExecute is null
                && profile.Keys.TryGetValue(key, out var binding)
                && binding.HasLongPress)
            {
                var timer = _timeProvider.CreateTimer(
                    _ => OnLongPressElapsed(key), null, _longPressThreshold, Timeout.InfiniteTimeSpan);
                _longPressTimers[key] = timer;
            }
        }

        if (toExecute is not null)
            _dispatcher.Dispatch(toExecute);
    }

    private void OnLongPressElapsed(int key)
    {
        IAction? longPressAction = null;

        lock (_gate)
        {
            DisposeTimer(key);

            if (!_pressedKeys.Contains(key) || _consumedByCombination.Contains(key))
                return;

            var profile = _profiles.ActiveProfile;
            if (profile is not null && profile.Keys.TryGetValue(key, out var binding) && binding.HasLongPress)
            {
                longPressAction = binding.LongPressAction;
                _longPressFired.Add(key);
            }
        }

        if (longPressAction is not null)
            _dispatcher.Dispatch(longPressAction);
    }

    private void HandleRelease(int key)
    {
        IAction? toExecute = null;

        lock (_gate)
        {
            DisposeTimer(key);

            var consumed = _consumedByCombination.Remove(key);
            var longPressAlreadyFired = _longPressFired.Remove(key);
            _pressedKeys.Remove(key);

            if (consumed || longPressAlreadyFired)
                return;

            var profile = _profiles.ActiveProfile;
            if (profile is null || !profile.IsEnabled)
                return;

            if (_fnKey is int fnKey && key == fnKey)
                return;

            // Normal eylem tuş bırakıldığında çalışır (uzun basma tetiklenmediyse).
            if (profile.Keys.TryGetValue(key, out var binding))
                toExecute = binding.Action;
        }

        if (toExecute is not null)
            _dispatcher.Dispatch(toExecute);
    }

    private void DisposeTimer(int key)
    {
        if (_longPressTimers.Remove(key, out var timer))
            timer.Dispose();
    }

    public void Dispose()
    {
        lock (_gate)
        {
            foreach (var timer in _longPressTimers.Values)
                timer.Dispose();
            _longPressTimers.Clear();
        }
    }
}
