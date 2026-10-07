using SipoDeck.Core.Actions;
using SipoDeck.Core.Profiles;
using SipoDeck.Core.Validation;

namespace SipoDeck.Core.Tests;

public class ChainValidationTests
{
    private static ValidationResult ValidateKey(int key, ActionConfig action)
        => ProfilesValidator.Validate(new ProfilesData
        {
            ActiveProfileId = "p",
            Profiles =
            {
                new ProfileData { Id = "p", Name = "P", Keys = { [key] = new KeyConfig { Action = action } } }
            }
        });

    private static ActionConfig Step() => new WaitActionConfig { Milliseconds = 1 };

    private static ActionChainConfig FlatChain(int steps)
    {
        var chain = new ActionChainConfig();
        for (var i = 0; i < steps; i++)
            chain.Actions.Add(Step());
        return chain;
    }

    // Verilen sayıda iç içe zincir; en içteki zincir bir bekleme adımı içerir.
    private static ActionChainConfig Nested(int depth)
    {
        var inner = new ActionChainConfig { Actions = { Step() } };
        for (var i = 1; i < depth; i++)
            inner = new ActionChainConfig { Actions = { inner } };
        return inner;
    }

    [Fact]
    public void MaxChainConstants_AreAsDocumented()
    {
        Assert.Equal(50, ActionCatalog.MaxChainSteps);
        Assert.Equal(5, ActionCatalog.MaxChainDepth);
    }

    [Fact]
    public void Chain_AtStepLimit_IsValid()
    {
        // Toplam adım sayısına zincirin kendisi de dahildir (zincir + 49 adım = 50).
        Assert.True(ValidateKey(0, FlatChain(ActionCatalog.MaxChainSteps - 1)).IsValid);
    }

    [Fact]
    public void Chain_OverStepLimit_IsInvalid_OnActionPath()
    {
        var result = ValidateKey(2, FlatChain(ActionCatalog.MaxChainSteps + 1));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == "Profiles[0].Keys[2].Action");
    }

    [Fact]
    public void Chain_StepLimit_CountsNestedStepsInTotal()
    {
        var outer = new ActionChainConfig();
        for (var i = 0; i < 3; i++)
            outer.Actions.Add(FlatChain(20));

        Assert.False(ValidateKey(0, outer).IsValid);
    }

    [Fact]
    public void Chain_AtDepthLimit_IsValid()
        => Assert.True(ValidateKey(0, Nested(ActionCatalog.MaxChainDepth)).IsValid);

    [Fact]
    public void Chain_OverDepthLimit_IsInvalid()
    {
        var result = ValidateKey(0, Nested(ActionCatalog.MaxChainDepth + 1));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field.StartsWith("Profiles[0].Keys[0].Action.Actions[0]"));
    }

    [Fact]
    public void Chain_ContainingItself_IsInvalid_WithoutStackOverflow()
    {
        var chain = new ActionChainConfig { Actions = { Step() } };
        chain.Actions.Add(chain);

        var result = ValidateKey(1, chain);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == "Profiles[0].Keys[1].Action.Actions[1]");
    }

    [Fact]
    public void Chain_IndirectCycle_IsInvalid_WithoutStackOverflow()
    {
        var a = new ActionChainConfig();
        var b = new ActionChainConfig { Actions = { a } };
        a.Actions.Add(b);

        Assert.False(ValidateKey(0, a).IsValid);
    }

    [Fact]
    public void Chain_SameInstanceReusedAsSiblings_IsNotACycle()
    {
        var shared = new ActionChainConfig { Actions = { Step() } };
        var outer = new ActionChainConfig { Actions = { shared, shared } };

        Assert.True(ValidateKey(0, outer).IsValid);
    }

    [Fact]
    public void Chain_NestedValid_IsValid()
    {
        var chain = new ActionChainConfig
        {
            Actions =
            {
                new KeyboardShortcutActionConfig { Shortcut = "Ctrl+C" },
                new ActionChainConfig { Actions = { new TypeTextActionConfig { Text = "x" }, Step() } },
                new SwitchProfileActionConfig { ProfileId = "p" }
            }
        };

        Assert.True(ValidateKey(0, chain).IsValid);
    }

    [Fact]
    public void Chain_ErrorPath_PointsToInvalidStep()
    {
        var chain = new ActionChainConfig
        {
            Actions = { Step(), new OpenUrlActionConfig { Url = "" } }
        };

        var result = ValidateKey(3, chain);

        Assert.Contains(result.Errors, e => e.Field == "Profiles[0].Keys[3].Action.Actions[1].Url");
    }

    [Fact]
    public void Chain_NullStep_IsReported()
    {
        var chain = new ActionChainConfig();
        chain.Actions.Add(null!);

        var result = ValidateKey(0, chain);

        Assert.Contains(result.Errors, e => e.Field == "Profiles[0].Keys[0].Action.Actions[0]");
    }

    [Fact]
    public void LongPressAction_Chain_IsValidatedToo()
    {
        var data = new ProfilesData
        {
            Profiles =
            {
                new ProfileData
                {
                    Id = "p", Name = "P",
                    Keys = { [0] = new KeyConfig { LongPressAction = new ActionChainConfig { Actions = { new OpenUrlActionConfig() } } } }
                }
            }
        };

        var result = ProfilesValidator.Validate(data);

        Assert.Contains(result.Errors, e => e.Field == "Profiles[0].Keys[0].LongPressAction.Actions[0].Url");
    }
}
