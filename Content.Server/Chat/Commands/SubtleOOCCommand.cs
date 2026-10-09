using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server.Chat.Commands;

[AnyCommand]
public sealed class SubtleOOCCommand : IConsoleCommand
{
    public string Command => "sooc";
    public string Description => "Send an out-of-character message in whisper range. Does not go through walls and is blocked from non-admin ghosts.";
    public string Help => "sooc <text>";

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
        chat.TrySendInGameOOCMessage(playerEntity, message, InGameOOCChatType.Looc, false, shell, player);
    }
}
