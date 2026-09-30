using System.IO.Ports;
using SipoDeck.Core.Profiles;
using SipoDeck.Core.Settings;
using SipoDeck.Core.Validation;
using SipoDeck.Runtime.Management;

namespace SipoDeck.Runtime;

// Ayar/profil yönetim API'si. Akış: taslağı kopyala -> doğrula -> atomik yaz -> çalışan sisteme uygula -> olay.
// Tüm çağrılar yaşam döngüsü kilidiyle sıralanır; olaylar kilit serbest bırakıldıktan sonra tetiklenir.
public sealed partial class AppRuntime
{
    private const string UnexpectedFailureMessage = "İşlem tamamlanamadı.";

    /// <summary>Ayarlar başarıyla kaydedildiğinde tetiklenir. Bağlantı bilgisi taşımaz; ayarlar <see cref="GetSettingsSnapshot"/> ile okunur.</summary>
    public event EventHandler? SettingsChanged;

    /// <summary>Profil listesi/içeriği başarıyla değiştiğinde tetiklenir.</summary>
    public event EventHandler? ProfilesChanged;

    /// <summary>Aktif profil değiştiğinde tetiklenir (argüman: yeni aktif profil kimliği; kalmadıysa null).</summary>
    public event EventHandler<string?>? ActiveProfileChanged;

    /// <summary>Güncel ayarların bağımsız kopyası.</summary>
    public AppSettings GetSettingsSnapshot()
    {
        EnsureLoaded();
        lock (_dataLock)
            return _settings!.Clone();
    }

    /// <summary>Güncel profillerin bağımsız kopyası; <see cref="ProfilesData.ActiveProfileId"/> çalışan sistemdeki aktif profili yansıtır.</summary>
    public ProfilesData GetProfilesSnapshot()
    {
        EnsureLoaded();
        ProfilesData copy;
        lock (_dataLock)
            copy = _profilesData!.Clone();

        if (State == RuntimeState.Running)
            copy.ActiveProfileId = _profiles.ActiveProfile?.Id;

        return copy;
    }

    /// <summary>Sistemdeki seri port adları (sıralı). Listelenemezse boş liste döner.</summary>
    public IReadOnlyList<string> GetAvailablePortNames()
    {
        try
        {
            return SerialPort.GetPortNames()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9'), StringComparer.OrdinalIgnoreCase)
                .ThenBy(name => int.TryParse(name.AsSpan(name.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9').Length), out var n) ? n : 0)
                .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public Task<ManagementResult> UpdateSettingsAsync(AppSettings draft, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var next = draft.Clone();

        return RunManagementAsync(async events =>
        {
            var knownIds = _profilesData!.Profiles.Select(p => p.Id).ToHashSet();
            var validation = SettingsValidator.Validate(next, knownIds);
            if (!validation.IsValid)
                return ManagementResult.Invalid(validation.Errors);

            try
            {
                new SettingsStore(_settingsFilePath).Save(next);
            }
            catch
            {
                return ManagementResult.Failed("Ayarlar kaydedilemedi.");
            }

            var old = _settings!;
            lock (_dataLock)
                _settings = next;

            if (State == RuntimeState.Running)
            {
                if (ConnectionChanged(old, next))
                    await RestartConnectionAsync(next, events).ConfigureAwait(false);

                if (InputChanged(old.Input, next.Input))
                    ReplaceInputEngine(next.Input);
            }

            events.Add(() => SettingsChanged?.Invoke(this, EventArgs.Empty));
            return ManagementResult.Success;
        }, cancellationToken);
    }

    public Task<ManagementResult> CreateProfileAsync(ProfileData draft, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var profile = CloneProfile(draft);
        if (string.IsNullOrWhiteSpace(profile.Id))
            profile.Id = Guid.NewGuid().ToString("N");

        return RunManagementAsync(events =>
        {
            var next = _profilesData!.Clone();
            next.Profiles.Add(profile);
            return Task.FromResult(CommitProfiles(next, events, manager => manager.Add(profile.ToProfile())));
        }, cancellationToken);
    }

    public Task<ManagementResult> UpdateProfileAsync(ProfileData draft, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        var profile = CloneProfile(draft);

        return RunManagementAsync(events =>
        {
            var next = _profilesData!.Clone();
            var index = next.Profiles.FindIndex(p => p.Id == profile.Id);
            if (index < 0)
                return Task.FromResult(Invalid("Id", "Profil bulunamadı."));

            next.Profiles[index] = profile;
            return Task.FromResult(CommitProfiles(next, events, manager => manager.Replace(profile.ToProfile())));
        }, cancellationToken);
    }

    public Task<ManagementResult> DeleteProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        return RunManagementAsync(events =>
        {
            var next = _profilesData!.Clone();
            var index = next.Profiles.FindIndex(p => p.Id == profileId);
            if (index < 0)
                return Task.FromResult(Invalid("Id", "Profil bulunamadı."));

            if (next.Profiles.Count == 1)
                return Task.FromResult(Invalid("Profiles", "Son profil silinemez."));

            var references = _settings!.Input.ProfileSwitchMap
                .Where(pair => pair.Value == profileId)
                .Select(pair => new ValidationError(
                    $"Input.ProfileSwitchMap[{pair.Key}]",
                    "Profil bir FN eşleştirmesinde kullanılıyor; silmeden önce eşleştirmeyi değiştirin."))
                .ToArray();
            if (references.Length > 0)
                return Task.FromResult(ManagementResult.Invalid(references));

            next.Profiles.RemoveAt(index);
            return Task.FromResult(CommitProfiles(next, events, manager => manager.Remove(profileId)));
        }, cancellationToken);
    }

    public Task<ManagementResult> SetActiveProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        return RunManagementAsync(events =>
        {
            var profile = _profilesData!.Profiles.Find(p => p.Id == profileId);
            if (profile is null)
                return Task.FromResult(Invalid("ActiveProfileId", "Profil bulunamadı."));
            if (!profile.IsEnabled)
                return Task.FromResult(Invalid("ActiveProfileId", "Profil etkin değil."));

            if (_profilesData.ActiveProfileId == profileId)
                return Task.FromResult(ManagementResult.Success);

            var next = _profilesData.Clone();
            next.ActiveProfileId = profileId;
            return Task.FromResult(CommitProfiles(next, events, manager => manager.SwitchTo(profileId), profilesChanged: false));
        }, cancellationToken);
    }

    /// <summary>
    /// Cihaz bağlantısını kontrollü kapatıp mevcut ayarlarla yeniden kurar ve bağlanır. Runtime
    /// çalışmıyorsa veya bağlantı ayarı yoksa hiçbir şey yapmaz.
    /// </summary>
    public async Task ReconnectAsync(CancellationToken cancellationToken = default)
    {
        var events = new List<Action>();
        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (State != RuntimeState.Running || _transport is null || _settings is null)
                return;

            await RestartConnectionAsync(_settings, events).ConfigureAwait(false);
        }
        finally
        {
            _lifecycleLock.Release();
        }

        RaiseAll(events);
    }

    private async Task<ManagementResult> RunManagementAsync(
        Func<List<Action>, Task<ManagementResult>> operation,
        CancellationToken cancellationToken)
    {
        var events = new List<Action>();
        ManagementResult result;

        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureLoaded();
            FlushActiveProfile(events);
            result = await operation(events).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Ayrıntı (yol/adres) sızdırmamak için genel mesaj döndürülür.
            result = ManagementResult.Failed(UnexpectedFailureMessage);
        }
        finally
        {
            _lifecycleLock.Release();
        }

        RaiseAll(events);
        return result;
    }

    // Kilit altında çağrılır: profilleri doğrular, atomik yazar, bellekteki kopyayı günceller ve (Running ise) canlıya uygular.
    private ManagementResult CommitProfiles(
        ProfilesData next,
        List<Action> events,
        Action<ProfileManager> applyLive,
        bool profilesChanged = true)
    {
        next.ActiveProfileId = ResolveActiveProfileId(next);

        var validation = ProfilesValidator.Validate(next);
        if (!validation.IsValid)
            return ManagementResult.Invalid(validation.Errors);

        try
        {
            new ProfileStore(_profilesFilePath).Save(next);
        }
        catch
        {
            return ManagementResult.Failed("Profiller kaydedilemedi.");
        }

        var oldActive = _profilesData!.ActiveProfileId;
        lock (_dataLock)
            _profilesData = next;

        // Bellek güncellendikten sonra uygulanır: ProfileManager olayı yönetim kaynaklı değişimi "zaten kayıtlı" görür.
        if (State == RuntimeState.Running)
            applyLive(_profiles);

        if (profilesChanged)
            events.Add(() => ProfilesChanged?.Invoke(this, EventArgs.Empty));

        var newActive = next.ActiveProfileId;
        if (oldActive != newActive)
            events.Add(() => ActiveProfileChanged?.Invoke(this, newActive));

        return ManagementResult.Success;
    }

    // Kilit altında çağrılır: FN + tuş gibi yönetim dışı bir nedenle değişen aktif profili profiles.json'a yazar.
    // Yazma hatası yutulur; bellekteki durum yine de çalışan sistemle tutarlı kalır.
    private void FlushActiveProfile(List<Action> events)
    {
        if (_state is not (RuntimeState.Running or RuntimeState.Stopping))
            return;

        var current = _profilesData;
        if (current is null)
            return;

        var activeId = _profiles.ActiveProfile?.Id;
        if (activeId == current.ActiveProfileId)
            return;

        var updated = current.Clone();
        updated.ActiveProfileId = activeId;

        try
        {
            new ProfileStore(_profilesFilePath).Save(updated);
        }
        catch
        {
            // Kalıcılık en iyi çabayla yapılır; bir sonraki başarılı profil kaydı değeri zaten yazar.
        }

        lock (_dataLock)
            _profilesData = updated;

        events.Add(() => ActiveProfileChanged?.Invoke(this, activeId));
    }

    // ProfileManager.ActiveProfileChanged: yönetim çağrıları belleği önce güncellediği için yalnızca
    // yönetim dışı (FN + tuş) değişimler buradan arka plan yazmasını tetikler.
    private void OnProfileManagerActiveProfileChanged(object? sender, string? newId)
    {
        if (State != RuntimeState.Running)
            return;

        lock (_dataLock)
        {
            if (_profilesData is null || _profilesData.ActiveProfileId == newId)
                return;
        }

        _ = SyncActiveProfileAsync();
    }

    private async Task SyncActiveProfileAsync()
    {
        try
        {
            // Yönetim çağrılarının ortak girişi aktif profili yazar; burada yalnızca sıraya girilir.
            await RunManagementAsync(_ => Task.FromResult(ManagementResult.Success), CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch
        {
            // Arka plan kalıcılığı uygulamayı etkilememeli.
        }
    }

    // Kilit altında çağrılır: bağlantıyı kontrollü kapatır (eski transport/zamanlayıcı serbest kalır), sonra yenisini kurar.
    private async Task RestartConnectionAsync(AppSettings settings, List<Action> events)
    {
        var previousState = _deviceConnectionState;
        var previousType = _connectionType;

        await DisposeConnectionAsync().ConfigureAwait(false);

        // Eski bağlantının olayları bırakıldığı için "bağlantı koptu" bilgisi burada iletilir.
        if (previousState != Core.Transport.ConnectionState.Disconnected)
        {
            events.Add(() => DeviceConnectionStateChanged?.Invoke(
                this, new Events.DeviceConnectionStateChangedEventArgs(previousType, Core.Transport.ConnectionState.Disconnected)));
        }

        BuildConnection(settings);
    }

    private void ReplaceInputEngine(InputSettings input)
    {
        var fresh = CreateInputEngine(input);
        Interlocked.Exchange(ref _inputEngine, fresh)?.Dispose();
    }

    private static bool ConnectionChanged(AppSettings a, AppSettings b)
        => a.Connection.ConnectionType != b.Connection.ConnectionType
           || a.Connection.Host != b.Connection.Host
           || a.Connection.Port != b.Connection.Port
           || a.Connection.SerialPortName != b.Connection.SerialPortName
           || a.Connection.BaudRate != b.Connection.BaudRate
           || a.Application.AutoReconnect != b.Application.AutoReconnect;

    private static bool InputChanged(InputSettings a, InputSettings b)
        => a.FnKey != b.FnKey
           || a.LongPressThresholdMilliseconds != b.LongPressThresholdMilliseconds
           || a.ProfileSwitchMap.Count != b.ProfileSwitchMap.Count
           || a.ProfileSwitchMap.Any(pair => !b.ProfileSwitchMap.TryGetValue(pair.Key, out var id) || id != pair.Value);

    // ProfileManager ile aynı kural: aktif profil geçerli ve etkinse korunur, değilse ilk etkin profil, yoksa null.
    private static string? ResolveActiveProfileId(ProfilesData data)
    {
        if (data.ActiveProfileId is { } id && data.Profiles.Any(p => p.Id == id && p.IsEnabled))
            return id;

        return data.Profiles.FirstOrDefault(p => p.IsEnabled)?.Id;
    }

    private static ProfileData CloneProfile(ProfileData draft)
        => new ProfilesData { Profiles = { draft } }.Clone().Profiles[0];

    private static ManagementResult Invalid(string field, string message)
        => ManagementResult.Invalid(new[] { new ValidationError(field, message) });

    // Çalışma başlamadan/durmuşken yönetim çağrıları ve anlık görüntüler için diskteki veriyi bir kez yükler.
    private void EnsureLoaded()
    {
        lock (_dataLock)
        {
            if (_settings is not null && _profilesData is not null)
                return;

            var (settings, data) = LoadFromDisk();
            _settings = settings;
            _profilesData = data;
        }
    }

    private static void RaiseAll(List<Action> events)
    {
        foreach (var raise in events)
            raise();
    }
}
