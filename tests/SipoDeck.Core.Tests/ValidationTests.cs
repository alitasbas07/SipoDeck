using SipoDeck.Core.Actions;
using SipoDeck.Core.Profiles;
using SipoDeck.Core.Settings;
using SipoDeck.Core.Transport;
using SipoDeck.Core.Validation;

namespace SipoDeck.Core.Tests;

public class SettingsValidatorTests
{
    private static readonly IReadOnlySet<string> Known = new HashSet<string> { "p1", "p2" };

    private static ValidationResult Validate(AppSettings s) => SettingsValidator.Validate(s, Known);

    private static void AssertError(ValidationResult result, string field)
    {
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == field);
    }

    [Fact]
    public void DefaultSettings_AreValid()
        => Assert.True(Validate(new AppSettings()).IsValid);

    [Fact]
    public void WiFi_Configured_IsValid()
    {
        var s = new AppSettings();
        s.Connection.Host = "192.168.1.5";
        s.Connection.Port = 8080;
        Assert.True(Validate(s).IsValid);
    }

    [Fact]
    public void WiFi_HostWithoutPort_IsInvalid()
    {
        var s = new AppSettings();
        s.Connection.Host = "192.168.1.5";
        AssertError(Validate(s), "Connection.Port");
    }

    [Fact]
    public void WiFi_PortWithoutHost_IsInvalid()
    {
        var s = new AppSettings();
        s.Connection.Port = 80;
        AssertError(Validate(s), "Connection.Host");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(65536)]
    public void WiFi_PortOutOfRange_IsInvalid(int port)
    {
        var s = new AppSettings();
        s.Connection.Host = "h";
        s.Connection.Port = port;
        AssertError(Validate(s), "Connection.Port");
    }

    [Fact]
    public void Serial_Unconfigured_IsValid()
    {
        var s = new AppSettings();
        s.Connection.ConnectionType = ConnectionType.Serial;
        Assert.True(Validate(s).IsValid);
    }

    [Fact]
    public void Serial_Configured_IsValid()
    {
        var s = new AppSettings();
        s.Connection.ConnectionType = ConnectionType.Serial;
        s.Connection.SerialPortName = "COM3";
        Assert.True(Validate(s).IsValid);
    }

    [Fact]
    public void Serial_BaudWithoutPortName_IsInvalid()
    {
        var s = new AppSettings();
        s.Connection.ConnectionType = ConnectionType.Serial;
        s.Connection.BaudRate = 9600;
        AssertError(Validate(s), "Connection.SerialPortName");
    }

    [Fact]
    public void Serial_NonPositiveBaud_IsInvalid()
    {
        var s = new AppSettings();
        s.Connection.ConnectionType = ConnectionType.Serial;
        s.Connection.SerialPortName = "COM3";
        s.Connection.BaudRate = 0;
        AssertError(Validate(s), "Connection.BaudRate");
    }

    [Fact]
    public void UnselectedConnectionType_IsNotValidated()
    {
        var s = new AppSettings();
        s.Connection.ConnectionType = ConnectionType.Serial;
        s.Connection.Host = "";
        s.Connection.Port = 99999;
        Assert.True(Validate(s).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void LongPress_NonPositive_IsInvalid(int ms)
    {
        var s = new AppSettings();
        s.Input.LongPressThresholdMilliseconds = ms;
        AssertError(Validate(s), "Input.LongPressThresholdMilliseconds");
    }

    [Fact]
    public void ProfileSwitchMap_UnknownProfile_IsInvalid()
    {
        var s = new AppSettings();
        s.Input.ProfileSwitchMap[1] = "p1";
        s.Input.ProfileSwitchMap[2] = "missing";
        var result = Validate(s);
        Assert.Single(result.Errors);
        AssertError(result, "Input.ProfileSwitchMap[2]");
    }

    [Fact]
    public void ProfileSwitchMap_KnownProfiles_IsValid()
    {
        var s = new AppSettings();
        s.Input.ProfileSwitchMap[1] = "p1";
        s.Input.ProfileSwitchMap[2] = "p2";
        Assert.True(Validate(s).IsValid);
    }

    [Fact]
    public void NegativeFnKey_IsInvalid()
    {
        var s = new AppSettings();
        s.Input.FnKey = -1;
        AssertError(Validate(s), "Input.FnKey");
    }
}

public class ProfilesValidatorTests
{
    private static ProfileData Profile(string id = "p1", string name = "A") => new() { Id = id, Name = name };

    private static ProfilesData Data(params ProfileData[] profiles)
    {
        var data = new ProfilesData();
        data.Profiles.AddRange(profiles);
        return data;
    }

    private static void AssertError(ValidationResult result, string field)
    {
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == field);
    }

    private static ValidationResult ValidateAction(ActionConfig action)
    {
        var profile = Profile();
        profile.Keys[3] = new KeyConfig { Action = action };
        return ProfilesValidator.Validate(Data(profile));
    }

    [Fact]
    public void Default_IsValid()
        => Assert.True(ProfilesValidator.Validate(ProfilesData.CreateDefault()).IsValid);

    [Fact]
    public void NoProfiles_IsInvalid()
        => AssertError(ProfilesValidator.Validate(new ProfilesData()), "Profiles");

    [Fact]
    public void EmptyName_IsInvalid()
        => AssertError(ProfilesValidator.Validate(Data(Profile(), Profile("p2", " "))), "Profiles[1].Name");

    [Fact]
    public void EmptyId_IsInvalid()
        => AssertError(ProfilesValidator.Validate(Data(Profile(""))), "Profiles[0].Id");

    [Fact]
    public void DuplicateId_IsInvalid()
        => AssertError(ProfilesValidator.Validate(Data(Profile("x"), Profile("x", "B"))), "Profiles[1].Id");

    [Fact]
    public void NegativeKey_IsInvalid()
    {
        var profile = Profile();
        profile.Keys[-1] = new KeyConfig();
        AssertError(ProfilesValidator.Validate(Data(profile)), "Profiles[0].Keys[-1]");
    }

    [Fact]
    public void Combination_Valid()
    {
        var profile = Profile();
        profile.Combinations.Add(new CombinationConfig { Keys = { 1, 2 }, Action = new OpenUrlActionConfig { Url = "u" } });
        Assert.True(ProfilesValidator.Validate(Data(profile)).IsValid);
    }

    [Theory]
    [InlineData(new[] { 1 })]
    [InlineData(new[] { 1, 1 })]
    [InlineData(new int[0])]
    public void Combination_LessThanTwoDistinctKeys_IsInvalid(int[] keys)
    {
        var profile = Profile();
        profile.Combinations.Add(new CombinationConfig { Keys = keys.ToList(), Action = new OpenUrlActionConfig { Url = "u" } });
        AssertError(ProfilesValidator.Validate(Data(profile)), "Profiles[0].Combinations[0].Keys");
    }

    [Fact]
    public void Combination_NegativeKeyOrMissingAction_IsInvalid()
    {
        var profile = Profile();
        profile.Combinations.Add(new CombinationConfig { Keys = { -1, 2 } });
        var result = ProfilesValidator.Validate(Data(profile));
        AssertError(result, "Profiles[0].Combinations[0].Keys");
        AssertError(result, "Profiles[0].Combinations[0].Action");
    }

    [Fact]
    public void Key_WithNoAction_IsValid()
    {
        var profile = Profile();
        profile.Keys[1] = new KeyConfig();
        Assert.True(ProfilesValidator.Validate(Data(profile)).IsValid);
    }

    [Fact]
    public void EmptyUrl_IsInvalid()
        => AssertError(ValidateAction(new OpenUrlActionConfig()), "Profiles[0].Keys[3].Action.Url");

    [Fact]
    public void EmptyProgramPath_IsInvalid()
        => AssertError(ValidateAction(new RunProgramActionConfig()), "Profiles[0].Keys[3].Action.FilePath");

    [Fact]
    public void NegativeWait_IsInvalid()
        => AssertError(ValidateAction(new WaitActionConfig { Milliseconds = -1 }), "Profiles[0].Keys[3].Action.Milliseconds");

    [Fact]
    public void ZeroWait_IsValid()
        => Assert.True(ValidateAction(new WaitActionConfig { Milliseconds = 0 }).IsValid);

    [Fact]
    public void EmptyNotification_IsInvalid()
        => AssertError(ValidateAction(new NotificationActionConfig()), "Profiles[0].Keys[3].Action.Message");

    [Fact]
    public void UndefinedEnumCommands_AreInvalid()
    {
        AssertError(ValidateAction(new VolumeActionConfig { Command = (VolumeCommand)99 }), "Profiles[0].Keys[3].Action.Command");
        AssertError(ValidateAction(new MediaActionConfig { Command = (MediaCommand)99 }), "Profiles[0].Keys[3].Action.Command");
    }

    [Fact]
    public void ValidActions_AreValid()
    {
        Assert.True(ValidateAction(new RunProgramActionConfig { FilePath = "a.exe" }).IsValid);
        Assert.True(ValidateAction(new NotificationActionConfig { Title = "t" }).IsValid);
        Assert.True(ValidateAction(new VolumeActionConfig { Command = VolumeCommand.Up }).IsValid);
        Assert.True(ValidateAction(new MediaActionConfig { Command = MediaCommand.Next }).IsValid);
    }

    [Fact]
    public void LongPressAction_IsValidated()
    {
        var profile = Profile();
        profile.Keys[2] = new KeyConfig { LongPressAction = new OpenUrlActionConfig() };
        AssertError(ProfilesValidator.Validate(Data(profile)), "Profiles[0].Keys[2].LongPressAction.Url");
    }

    [Fact]
    public void ActionChain_ValidatedRecursively()
    {
        var chain = new ActionChainConfig
        {
            Actions =
            {
                new OpenUrlActionConfig { Url = "ok" },
                new ActionChainConfig { Actions = { new WaitActionConfig { Milliseconds = -5 } } }
            }
        };
        AssertError(ValidateAction(chain), "Profiles[0].Keys[3].Action.Actions[1].Actions[0].Milliseconds");
    }

    [Fact]
    public void ActiveProfileId_MustReferenceExistingProfile()
    {
        var data = Data(Profile());
        data.ActiveProfileId = "ghost";
        AssertError(ProfilesValidator.Validate(data), "ActiveProfileId");

        data.ActiveProfileId = "p1";
        Assert.True(ProfilesValidator.Validate(data).IsValid);

        data.ActiveProfileId = null;
        Assert.True(ProfilesValidator.Validate(data).IsValid);
    }

    [Fact]
    public void ValidationResult_SuccessAndFailure()
    {
        Assert.True(ValidationResult.Success.IsValid);
        Assert.Empty(ValidationResult.Success.Errors);
        Assert.True(ValidationResult.Failure(Array.Empty<ValidationError>()).IsValid);
        var failure = ValidationResult.Failure(new[] { new ValidationError("F", "M") });
        Assert.False(failure.IsValid);
        Assert.Equal("F", failure.Errors[0].Field);
    }
}
