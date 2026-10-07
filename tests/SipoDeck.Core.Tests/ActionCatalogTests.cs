using System.Text.Json;
using System.Text.Json.Serialization;
using SipoDeck.Core.Actions;

namespace SipoDeck.Core.Tests;

public class ActionCatalogTests
{
    private static readonly string[] BuiltInTypeIds =
    {
        "runProgram", "openUrl", "openPath", "wait", "notification", "volume",
        "media", "keyboardShortcut", "typeText", "switchProfile", "chain"
    };

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private static ActionCatalog Catalog => ActionCatalog.Default;

    [Fact]
    public void Default_ContainsAllBuiltInTypes()
    {
        foreach (var id in BuiltInTypeIds)
            Assert.NotNull(Catalog.Find(id));

        Assert.Equal(BuiltInTypeIds.Length, Catalog.Definitions.Count);
    }

    [Fact]
    public void TypeIds_MatchJsonTypeDiscriminator()
    {
        foreach (var definition in Catalog.Definitions)
        {
            var config = definition.Create();
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize<ActionConfig>(config, Options));

            Assert.Equal(definition.TypeId, doc.RootElement.GetProperty("type").GetString());
            Assert.Equal(definition.ConfigType, config.GetType());
        }
    }

    [Fact]
    public void Register_DuplicateTypeId_Throws()
    {
        var catalog = new ActionCatalog();
        ActionDefinition Make() => new("x", "X", "d", "c", typeof(WaitActionConfig), () => new WaitActionConfig());

        catalog.Register(Make());

        Assert.Throws<InvalidOperationException>(() => catalog.Register(Make()));
        Assert.Single(catalog.Definitions);
    }

    [Fact]
    public void Find_ByConfig_ReturnsMatchingDefinition()
    {
        Assert.Equal("keyboardShortcut", Catalog.Find(new KeyboardShortcutActionConfig())!.TypeId);
        Assert.Equal("chain", Catalog.Find(new ActionChainConfig())!.TypeId);
        Assert.Null(Catalog.Find("nope"));
    }

    private static IReadOnlyList<(string Property, string Message)> Validate(ActionConfig config)
        => Catalog.ValidateParameters(config);

    [Fact]
    public void ValidateParameters_DefaultCreatedConfigs_RequiredFieldsReported()
    {
        Assert.Contains(Validate(new RunProgramActionConfig()), e => e.Property == "FilePath");
        Assert.Contains(Validate(new OpenUrlActionConfig()), e => e.Property == "Url");
        Assert.Contains(Validate(new OpenPathActionConfig()), e => e.Property == "Path");
        Assert.Contains(Validate(new TypeTextActionConfig()), e => e.Property == "Text");
        Assert.Contains(Validate(new SwitchProfileActionConfig()), e => e.Property == "ProfileId");
        Assert.Contains(Validate(new KeyboardShortcutActionConfig()), e => e.Property == "Shortcut");
    }

    [Fact]
    public void ValidateParameters_WhitespaceOnlyRequired_IsReported()
        => Assert.Contains(Validate(new OpenUrlActionConfig { Url = "   " }), e => e.Property == "Url");

    [Fact]
    public void ValidateParameters_ValidConfigs_HaveNoErrors()
    {
        Assert.Empty(Validate(new RunProgramActionConfig { FilePath = "notepad.exe" }));
        Assert.Empty(Validate(new OpenUrlActionConfig { Url = "https://example.com" }));
        Assert.Empty(Validate(new OpenPathActionConfig { Path = @"C:\Temp" }));
        Assert.Empty(Validate(new WaitActionConfig { Milliseconds = 0 }));
        Assert.Empty(Validate(new NotificationActionConfig { Title = "t" }));
        Assert.Empty(Validate(new VolumeActionConfig { Command = VolumeCommand.Mute }));
        Assert.Empty(Validate(new MediaActionConfig { Command = MediaCommand.Next }));
        Assert.Empty(Validate(new KeyboardShortcutActionConfig { Shortcut = "Ctrl+Shift+M" }));
        Assert.Empty(Validate(new TypeTextActionConfig { Text = "merhaba" }));
        Assert.Empty(Validate(new SwitchProfileActionConfig { ProfileId = "p1" }));
        Assert.Empty(Validate(new ActionChainConfig()));
    }

    [Fact]
    public void ValidateParameters_WaitNegative_IsReported()
        => Assert.Contains(Validate(new WaitActionConfig { Milliseconds = -1 }), e => e.Property == "Milliseconds");

    [Theory]
    [InlineData("Ctrl+")]
    [InlineData("A+B")]
    [InlineData("Ctrl")]
    [InlineData("Ctrl+Foo")]
    public void ValidateParameters_InvalidShortcut_IsReported(string shortcut)
        => Assert.Contains(Validate(new KeyboardShortcutActionConfig { Shortcut = shortcut }), e => e.Property == "Shortcut");

    [Fact]
    public void ValidateParameters_NotificationWithoutTitleAndMessage_IsReported()
    {
        Assert.Contains(Validate(new NotificationActionConfig { Title = "", Message = "" }), e => e.Property == "Message");
        Assert.Contains(Validate(new NotificationActionConfig { Title = " ", Message = " " }), e => e.Property == "Message");
        Assert.Empty(Validate(new NotificationActionConfig { Title = "", Message = "m" }));
    }

    [Fact]
    public void ValidateParameters_InvalidEnumValues_AreReported()
    {
        Assert.Contains(Validate(new VolumeActionConfig { Command = (VolumeCommand)99 }), e => e.Property == "Command");
        Assert.Contains(Validate(new MediaActionConfig { Command = (MediaCommand)99 }), e => e.Property == "Command");
    }

    [Fact]
    public void ChainDefinition_IsContainer_OthersAreNot()
    {
        Assert.True(Catalog.Find("chain")!.IsContainer);
        Assert.All(Catalog.Definitions.Where(d => d.TypeId != "chain"), d => Assert.False(d.IsContainer));
    }
}
