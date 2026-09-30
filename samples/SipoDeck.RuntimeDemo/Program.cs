using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SipoDeck.Core.Actions;
using SipoDeck.Core.Transport;
using SipoDeck.Runtime;

// Task 012 — Runtime donanımsız doğrulama demosu.
// Gerçek ESP32 / Wi-Fi / USB KULLANILMAZ. Gerçek kullanıcı ayar/profil dosyalarına da
// dokunulmaz: Runtime, geçici ve boş bir klasördeki dosya yollarıyla başlatılır, bu da
// varsayılan ayarlara (bağlantı ayarı yok → cihazsız çalışma) karşılık gelir.

int fails = 0;
void Check(bool ok, string name) { Console.WriteLine($"{(ok ? "PASS" : "FAIL")} {name}"); if (!ok) fails++; }

var tempDir = Path.Combine(Path.GetTempPath(), "SipoDeckRuntimeDemo_" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(tempDir);
var settingsPath = Path.Combine(tempDir, "settings.json");
var profilesPath = Path.Combine(tempDir, "profiles.json");

Console.WriteLine("=== SipoDeck Runtime Doğrulama Demosu (Task 012) ===\n");
Console.WriteLine($"Geçici ayar/profil dosyaları (gerçek kullanıcı verisine dokunulmaz): {tempDir}\n");

var runtime = new AppRuntime(settingsPath, profilesPath);

var failedActions = new List<(string Type, string Message)>();
var failedGate = new object();
runtime.ActionFailed += (_, e) => { lock (failedGate) failedActions.Add((e.ActionType, e.Exception.Message)); };

// 1) Başlangıç durumu ve StartAsync
Check(runtime.State == RuntimeState.Stopped, "başlangıç durumu Stopped");

await runtime.StartAsync();
Check(runtime.State == RuntimeState.Running, "StartAsync sonrası Running");

// 2) Bağlantı ayarı yok (boş/geçici dosyalar) -> Running ama cihaz Disconnected
Check(runtime.DeviceConnectionState == ConnectionState.Disconnected, "bağlantı ayarı yokken cihaz bağlantısı Disconnected");

// 3) Tekrarlı StartAsync sistemi bozmuyor
await runtime.StartAsync();
Check(runtime.State == RuntimeState.Running, "tekrarlı StartAsync sonrası hâlâ Running");

// 4) Eylem sırası: kuyruğa giriş sırasına göre çalışmalı
var order = new List<int>();
var orderGate = new object();
void RecordAction(int n) { lock (orderGate) order.Add(n); }

runtime.Dispatch(new RecordingAction(1, RecordAction));
runtime.Dispatch(new RecordingAction(2, RecordAction));
runtime.Dispatch(new RecordingAction(3, RecordAction));
await WaitUntilAsync(() => { lock (orderGate) return order.Count == 3; }, TimeSpan.FromSeconds(2));
lock (orderGate)
    Check(order.Count == 3 && order[0] == 1 && order[1] == 2 && order[2] == 3, $"eylemler kuyruğa giriş sırasına göre çalıştı ({string.Join(",", order)})");

// 5) Hatalı eylem sonraki eylemi engellemiyor; hata olayı üretiliyor
runtime.Dispatch(new FailingAction("test-hata"));
runtime.Dispatch(new RecordingAction(4, RecordAction));
await WaitUntilAsync(() => { lock (orderGate) return order.Count == 4; }, TimeSpan.FromSeconds(2));
lock (orderGate)
    Check(order.Count == 4 && order[3] == 4, "hatalı eylemden sonraki eylem yine de çalıştı");

await WaitUntilAsync(() => { lock (failedGate) return failedActions.Count == 1; }, TimeSpan.FromSeconds(2));
lock (failedGate)
    Check(failedActions.Count == 1 && failedActions[0].Type == nameof(FailingAction) && failedActions[0].Message == "test-hata",
        $"hata olayı üretildi ({(failedActions.Count > 0 ? failedActions[0].Message : "-")})");

// 5b) ActionChain: bir adım başarısız olsa da sıradaki adım çalışır; zincir hatayı Runtime'a bildirir
var chainOrder = new List<int>();
var chainGate = new object();
void RecordChain(int n) { lock (chainGate) chainOrder.Add(n); }

runtime.Dispatch(new ActionChain(
    new RecordingAction(10, RecordChain),
    new FailingAction("zincir-hatası"),
    new RecordingAction(11, RecordChain)));
await WaitUntilAsync(() => { lock (chainGate) return chainOrder.Count == 2; }, TimeSpan.FromSeconds(2));
lock (chainGate)
    Check(chainOrder.Count == 2 && chainOrder[0] == 10 && chainOrder[1] == 11, $"ActionChain: hatalı adımdan sonraki adım da çalıştı ({string.Join(",", chainOrder)})");

await WaitUntilAsync(() => { lock (failedGate) return failedActions.Count == 2; }, TimeSpan.FromSeconds(2));
lock (failedGate)
    Check(failedActions.Count == 2 && failedActions[1].Type == nameof(ActionChain), $"ActionChain: zincir hatası Runtime'a bildirildi (tür: {(failedActions.Count > 1 ? failedActions[1].Type : "-")})");

// 6) Kapanışta çalışan bir bekleme eylemi iptal edilir; kapanış onu beklemez
var probe = new CancelableWaitProbe();
runtime.Dispatch(probe.Action);
await WaitUntilAsync(() => probe.Started, TimeSpan.FromSeconds(2));

var sw = Stopwatch.StartNew();
await runtime.StopAsync();
sw.Stop();

Check(runtime.State == RuntimeState.Stopped, "StopAsync sonrası Stopped");
Check(probe.Canceled, "kapanışta çalışan bekleme eylemi iptal edildi");
Check(sw.Elapsed < TimeSpan.FromSeconds(5), $"kapanış 30 saniyelik beklemeyi beklemeden hızlı tamamlandı ({sw.ElapsedMilliseconds} ms)");

// 7) Tekrarlı StopAsync sistemi bozmuyor
await runtime.StopAsync();
Check(runtime.State == RuntimeState.Stopped, "tekrarlı StopAsync sonrası hâlâ Stopped");

// 8) Durdurulduktan sonra yeniden başlatılabiliyor
await runtime.StartAsync();
Check(runtime.State == RuntimeState.Running, "durdurulduktan sonra yeniden StartAsync ile Running");
await runtime.StopAsync();

try { Directory.Delete(tempDir, recursive: true); } catch { /* geçici klasör; temizlenemezse önemli değil */ }

Console.WriteLine(fails == 0 ? "\nTÜM DOĞRULAMALAR GEÇTİ" : $"\n{fails} DOĞRULAMA BAŞARISIZ");
return fails;

static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
{
    var sw = Stopwatch.StartNew();
    while (!condition() && sw.Elapsed < timeout)
        await Task.Delay(20);
}

sealed class RecordingAction : IAction
{
    private readonly int _n;
    private readonly Action<int> _record;
    public RecordingAction(int n, Action<int> record) { _n = n; _record = record; }

    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _record(_n);
        return Task.CompletedTask;
    }
}

sealed class FailingAction : IAction
{
    private readonly string _message;
    public FailingAction(string message) => _message = message;

    public Task ExecuteAsync(CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(_message);
}

/// <summary>30 saniyelik iptal edilebilir bir bekleme yapan test eylemi; kapanışta iptal edildiğini gösterir.</summary>
sealed class CancelableWaitProbe
{
    public bool Started { get; private set; }
    public bool Canceled { get; private set; }
    public IAction Action { get; }

    public CancelableWaitProbe() => Action = new ProbeAction(this);

    private sealed class ProbeAction : IAction
    {
        private readonly CancelableWaitProbe _owner;
        public ProbeAction(CancelableWaitProbe owner) => _owner = owner;

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            _owner.Started = true;
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _owner.Canceled = true;
                throw;
            }
        }
    }
}
