using SipoDeck.Plugin;

namespace SipoDeck.Plugin.Abstractions.Tests;

public sealed class PluginManifestTests
{
    private const string ValidJson = """
        {
          "id": "ornek.plugin",
          "name": "Örnek Plugin",
          "version": "1.0.0",
          "author": "YOUR_NAME",
          "description": "Kısa açıklama",
          "apiVersion": 1,
          "entry": "Ornek.Plugin.dll",
          "permissions": ["notifications"]
        }
        """;

    private static PluginManifest Valid() => new()
    {
        Id = "ornek.plugin",
        Name = "Örnek",
        Version = "1.0.0",
        ApiVersion = PluginApi.Current,
        Entry = "Ornek.Plugin.dll"
    };

    private static void AssertError(IReadOnlyList<PluginManifestError> errors, string field)
        => Assert.Contains(errors, e => e.Field == field);

    [Fact]
    public void ValidManifest_Parses()
    {
        var result = PluginManifestParser.Parse(ValidJson);

        Assert.True(result.IsValid);
        Assert.Equal("ornek.plugin", result.Manifest!.Id);
        Assert.Equal(new[] { "notifications" }, result.Manifest.Permissions);
    }

    [Fact]
    public void MalformedJson_ReturnsError_WithoutThrowing()
    {
        var result = PluginManifestParser.Parse("{ not json");

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void NullJson_IsInvalid() => Assert.False(PluginManifestParser.Parse("null").IsValid);

    [Theory]
    [InlineData("")]
    [InlineData("Ornek.Plugin")]
    [InlineData("ornek plugin")]
    [InlineData(".ornek")]
    [InlineData("ornek.")]
    [InlineData("ornek..plugin")]
    [InlineData("../ornek")]
    public void InvalidId_IsRejected(string id)
    {
        var manifest = Valid();
        manifest.Id = id;

        AssertError(PluginManifestParser.Validate(manifest), "id");
    }

    [Fact]
    public void TooLongId_IsRejected()
    {
        var manifest = Valid();
        manifest.Id = new string('a', PluginManifestParser.MaxIdLength + 1);

        AssertError(PluginManifestParser.Validate(manifest), "id");
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("v1.0.0")]
    [InlineData("a.b.c")]
    [InlineData("")]
    public void InvalidVersion_IsRejected(string version)
    {
        var manifest = Valid();
        manifest.Version = version;

        AssertError(PluginManifestParser.Validate(manifest), "version");
    }

    [Theory]
    [InlineData("1.2.3")]
    [InlineData("1.0.0-beta.1")]
    public void SemanticVersions_AreAccepted(string version)
    {
        var manifest = Valid();
        manifest.Version = version;

        Assert.Empty(PluginManifestParser.Validate(manifest));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void UnsupportedApiVersion_IsRejected(int apiVersion)
    {
        var manifest = Valid();
        manifest.ApiVersion = apiVersion;

        AssertError(PluginManifestParser.Validate(manifest), "apiVersion");
    }

    [Theory]
    [InlineData("")]
    [InlineData("plugin.exe")]
    [InlineData("..\\evil.dll")]
    [InlineData("sub/../../evil.dll")]
    [InlineData("C:\\evil.dll")]
    [InlineData("/evil.dll")]
    public void InvalidEntry_IsRejected(string entry)
    {
        var manifest = Valid();
        manifest.Entry = entry;

        AssertError(PluginManifestParser.Validate(manifest), "entry");
    }

    [Fact]
    public void RelativeEntryInSubfolder_IsAccepted()
    {
        var manifest = Valid();
        manifest.Entry = "bin/Ornek.Plugin.dll";

        Assert.Empty(PluginManifestParser.Validate(manifest));
    }

    [Fact]
    public void UnknownOrDuplicatePermission_IsRejected()
    {
        var manifest = Valid();
        manifest.Permissions = new() { "notifications", "notifications", "root.access" };

        var errors = PluginManifestParser.Validate(manifest);

        Assert.Equal(2, errors.Count(e => e.Field == "permissions"));
    }

    [Fact]
    public void AllKnownPermissions_AreAccepted()
    {
        var manifest = Valid();
        manifest.Permissions = PluginPermissions.All.ToList();

        Assert.Empty(PluginManifestParser.Validate(manifest));
    }

    [Fact]
    public void MissingPermissionsProperty_MeansNoPermissions()
    {
        var result = PluginManifestParser.Parse(
            """{"id":"a.b","name":"A","version":"1.0.0","apiVersion":1,"entry":"a.dll"}""");

        Assert.True(result.IsValid);
        Assert.Empty(result.Manifest!.Permissions);
    }

    [Fact]
    public void Reapproval_RequiredOnlyForNewPermissions()
    {
        var updated = Valid();
        updated.Permissions = new() { "notifications", "input.send" };

        Assert.True(PluginManifestParser.RequiresReapproval(new[] { "notifications" }, updated));
        Assert.False(PluginManifestParser.RequiresReapproval(new[] { "notifications", "input.send", "network" }, updated));
    }
}
