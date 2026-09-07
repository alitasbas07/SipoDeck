using System;
using SipoDeck.Core.Actions;
using SipoDeck.Core.Devices;
using SipoDeck.Core.Input;
using SipoDeck.Core.Profiles;
using SipoDeck.Core.Protocol;

// Task 011 — İlk Olay Akışı
// Amaç: mimarinin uçtan uca çalıştığını doğrulamak.
// Gerçek ESP32 / Wi-Fi / USB KULLANILMAZ; sahte bir olayla akış test edilir.
// Bu demo Core'a bağımlıdır; Core bu demoya bağımlı DEĞİLDİR (kalıcı bağımlılık yok).

Console.WriteLine("=== SipoDeck İlk Olay Akışı Demosu (Task 011) ===\n");

// 1) Aktif profil: Button 1 -> iki adımlı test eylem zinciri
var profile = new Profile("demo", "Demo Profil");
profile.Keys[1] = new KeyBinding(new ActionChain(
    new TestAction("adım 1"),
    new TestAction("adım 2")));

var profiles = new ProfileManager();
profiles.Add(profile); // ilk eklenen etkin profil aktif olur
Console.WriteLine($"Aktif profil: {profiles.ActiveProfile?.Name} (Button 1 -> zincir: adım 1, adım 2)\n");

var engine = new InputEngine(profiles, TimeSpan.FromMilliseconds(500));

// 2) Sahte test olayları oluştur
static KeyEvent Pressed(int button) => new("test-device", button, ButtonState.Pressed, DateTimeOffset.UtcNow);
static KeyEvent Released(int button) => new("test-device", button, ButtonState.Released, DateTimeOffset.UtcNow);

// 3) pressed -> Input Engine (tekli tuşta eylem bırakılınca çalışır, basılınca değil)
Console.WriteLine("[olay] Button 1 pressed  -> Input Engine");
engine.Process(Pressed(1));

// 4) released -> Input Engine -> aktif profil -> tuş eşleşmesi -> eylem zinciri
Console.WriteLine("[olay] Button 1 released -> Input Engine");
engine.Process(Released(1));

Console.WriteLine("\nTanımsız bir tuş (Button 5) hiçbir eylem tetiklememeli:");
engine.Process(Pressed(5));
engine.Process(Released(5));

Console.WriteLine("\n=== Akış doğrulandı ===");

sealed class TestAction : IAction
{
    private readonly string _name;
    public TestAction(string name) => _name = name;
    public void Execute() => Console.WriteLine($"   >>> Test eylemi çalıştı: {_name}");
}
