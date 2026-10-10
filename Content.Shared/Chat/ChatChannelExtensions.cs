namespace Content.Shared.Chat;

public static class ChatChannelExtensions
{
    public static Color TextColor(this ChatChannel channel)
    {
        return channel switch
        {
            ChatChannel.Server => Color.Orange,
            ChatChannel.Radio => Color.LimeGreen,
            ChatChannel.LOOC => Color.MediumTurquoise,
            ChatChannel.OOC => Color.LightSkyBlue,
            ChatChannel.Dead => Color.MediumPurple,
            ChatChannel.Admin => Color.Red,
            ChatChannel.AdminAlert => Color.Red,
            ChatChannel.AdminChat => Color.HotPink,
            ChatChannel.Whisper => Color.DarkGray,
            ChatChannel.Subtle => new Color(230, 230, 255),   // light blue-ish
            ChatChannel.SubtleOOC => new Color(255, 119, 130), // soft pink/red
            _ => Color.LightGray
        };
    }

    // Starlight
    public static bool IsExemptFromLanguages(this ChatChannel channel) =>
    channel is ChatChannel.LOOC or ChatChannel.Emotes or ChatChannel.Notifications or ChatChannel.Visual;
}
