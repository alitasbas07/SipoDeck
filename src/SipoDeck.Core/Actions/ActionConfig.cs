using System.Text.Json.Serialization;

namespace SipoDeck.Core.Actions;

/// <summary>
/// Bir eylemin saklanabilir (serileştirilebilir) tanımıdır. Çalışma zamanında
/// <see cref="ToAction"/> ile ilgili <see cref="IAction"/> nesnesine dönüştürülür.
/// Yeni eylem türleri yeni bir alt sınıf ve JsonDerivedType kaydıyla eklenir.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(RunProgramActionConfig), "runProgram")]
[JsonDerivedType(typeof(OpenUrlActionConfig), "openUrl")]
[JsonDerivedType(typeof(WaitActionConfig), "wait")]
[JsonDerivedType(typeof(NotificationActionConfig), "notification")]
[JsonDerivedType(typeof(VolumeActionConfig), "volume")]
[JsonDerivedType(typeof(MediaActionConfig), "media")]
[JsonDerivedType(typeof(ActionChainConfig), "chain")]
public abstract class ActionConfig
{
    public abstract IAction ToAction();
}

public sealed class RunProgramActionConfig : ActionConfig
{
    public string FilePath { get; set; } = string.Empty;
    public string? Arguments { get; set; }
    public string? WorkingDirectory { get; set; }

    public override IAction ToAction() => new RunProgramAction(FilePath, Arguments, WorkingDirectory);
}

public sealed class OpenUrlActionConfig : ActionConfig
{
    public string Url { get; set; } = string.Empty;

    public override IAction ToAction() => new OpenUrlAction(Url);
}

public sealed class WaitActionConfig : ActionConfig
{
    public int Milliseconds { get; set; }

    public override IAction ToAction() => new WaitAction(TimeSpan.FromMilliseconds(Milliseconds));
}

public sealed class NotificationActionConfig : ActionConfig
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public override IAction ToAction() => new NotificationAction(Title, Message);
}

public sealed class VolumeActionConfig : ActionConfig
{
    public VolumeCommand Command { get; set; }

    public override IAction ToAction() => new VolumeAction(Command);
}

public sealed class MediaActionConfig : ActionConfig
{
    public MediaCommand Command { get; set; }

    public override IAction ToAction() => new MediaAction(Command);
}

public sealed class ActionChainConfig : ActionConfig
{
    public List<ActionConfig> Actions { get; set; } = new();

    public override IAction ToAction() => new ActionChain(Actions.Select(a => a.ToAction()));
}
