using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server.Chat.Commands;

[AnyCommand]
public sealed class SubtleCommand : IConsoleCommand
{
    public string Command => "subtle";
    public string Description => "Send a subtle emote in whisper range. Does not go through walls and is blocked from non-admin ghosts.";
    public string Help => "subtle <text>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError("This command cannot be run from the server.");
            return;
        }

        if (player.Status != SessionStatus.InGame)
            return;

        if (player.AttachedEntity is not { } playerEntity)
        {
            shell.WriteError("You don't have an entity!");
            return;
        }

        if (args.Length < 1)
            return;

        var message = string.Join(" ", args).Trim();
        if (string.IsNullOrEmpty(message))
            return;

        var chat = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ChatSystem>();
        // NOTE: You must extend InGameICChatType with a Subtle value (or call a dedicated method).
        // The call below assumes you add InGameICChatType.Subtle.
        chat.TrySendInGameICMessage(playerEntity, message, InGameICChatType.Subtle,
            ChatTransmitRange.Normal, false, shell, player);
    }
}
