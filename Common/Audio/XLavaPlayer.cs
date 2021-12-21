using Discord;
using Victoria;

namespace DBotDDRefuge.Common.Audio;

/// <summary>
/// XLavaPlayer
/// <para>來源：https://github.com/Yucked/Victoria/tree/examples </para>
/// </summary>
public class XLavaPlayer : LavaPlayer
{
    public string ChannelName { get; }

    public XLavaPlayer(
        LavaSocket lavaSocket,
        IVoiceChannel voiceChannel,
        ITextChannel textChannel) : base(lavaSocket, voiceChannel, textChannel)
    {
        ChannelName = textChannel.Name;
    }
}