using DBotDDRefuge.Common.Audio;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System.Text;
using Victoria;
using Victoria.Enums;
using Victoria.Responses.Search;

namespace DBotDDRefuge.Modules;

/// <summary>
/// AudioModule
/// <para>來源：https://github.com/Yucked/Victoria/tree/examples </para>
/// </summary>
public sealed class AudioModule : InteractionModuleBase<ShardedInteractionContext>
{
    private readonly LavaNode<XLavaPlayer> _lavaNode;
    private readonly AudioService _audioService;
    private readonly ILogger<AudioModule> _logger;

    private static readonly IEnumerable<int> Range = Enumerable.Range(1900, 2000);

    /// <summary>
    /// 手動控制斜線命令的權限
    /// </summary>
    private const GuildPermission _permission = GuildPermission.UseApplicationCommands;

    public AudioModule(
        LavaNode<XLavaPlayer> lavaNode,
        AudioService audioService,
        ILogger<AudioModule> logger)
    {
        _lavaNode = lavaNode;
        _audioService = audioService;
        _logger = logger;
    }

    [SlashCommand("join", "加入語音頻道。")]
    [DefaultMemberPermissions(_permission)]
    public async Task JoinAsync()
    {
        if (_lavaNode.HasPlayer(Context.Guild))
        {
            await RespondAsync("我已經在一個語音頻道內了！");

            return;
        }

        IVoiceState? voiceState = Context.User as IVoiceState;

        if (voiceState?.VoiceChannel == null)
        {
            await RespondAsync("您必須先連接至一個語音頻道！");

            return;
        }

        try
        {
            await _lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel);
            await RespondAsync($"已加入 {voiceState.VoiceChannel.Name}！");
        }
        catch (Exception ex)
        {
            _logger.LogError("{Exception}", ex);

            await RespondAsync(ex.Message);
        }
    }

    [SlashCommand("leave", "離開語音頻道。")]
    [DefaultMemberPermissions(_permission)]
    public async Task LeaveAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await RespondAsync("我並未連接至任一語音頻道！");

            return;
        }

        IVoiceChannel voiceChannel = (Context.User as IVoiceState)?.VoiceChannel ?? player.VoiceChannel;

        if (voiceChannel == null)
        {
            await RespondAsync("不確定您要我與哪個語音頻道斷線。");

            return;
        }

        try
        {
            await _lavaNode.LeaveAsync(voiceChannel);
            await RespondAsync($"我已離開 {voiceChannel.Name}！");
        }
        catch (Exception ex)
        {
            _logger.LogError("{Exception}", ex);

            await RespondAsync(ex.Message);
        }
    }

    [SlashCommand("play", "播放歌曲。")]
    [DefaultMemberPermissions(_permission)]
    public async Task PlayAsync([Remainder] string searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            await RespondAsync("請輸入搜尋詞。");

            return;
        }

        if (!_lavaNode.HasPlayer(Context.Guild))
        {
            await RespondAsync("我並未連接至任一語音頻道！");

            return;
        }

        SearchResponse searchResponse = await _lavaNode.SearchAsync(SearchType.Direct, searchQuery);

        if (searchResponse.Status is SearchStatus.LoadFailed or SearchStatus.NoMatches)
        {
            await RespondAsync($"我無法找到與 `{searchQuery}` 有關的歌曲。");

            return;
        }

        XLavaPlayer player = _lavaNode.GetPlayer(Context.Guild);

        if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
        {
            player.Queue.Enqueue(searchResponse.Tracks);

            await RespondAsync($"已佇列 {searchResponse.Tracks.Count} 首歌曲。");
        }
        else
        {
            var track = searchResponse.Tracks.FirstOrDefault();

            player.Queue.Enqueue(track);

            await RespondAsync($"已佇列 {track?.Title}");
        }

        if (player.PlayerState is PlayerState.Playing or PlayerState.Paused)
        {
            return;
        }

        player.Queue.TryDequeue(out var lavaTrack);

        await player.PlayAsync(x =>
        {
            x.Track = lavaTrack;
            x.ShouldPause = false;
        });
    }

    [SlashCommand("pause", "暫停播放。")]
    [DefaultMemberPermissions(_permission)]
    public async Task PauseAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out XLavaPlayer player))
        {
            await RespondAsync("我並未連接至任一語音頻道！");

            return;
        }

        if (player.PlayerState != PlayerState.Playing)
        {
            await RespondAsync("我沒有辦法在未播放任何歌曲的時候暫停播放！");

            return;
        }

        try
        {
            await player.PauseAsync();
            await RespondAsync($"已暫停播放：{player.Track.Title}");
        }
        catch (Exception ex)
        {
            _logger.LogError("{Exception}", ex);

            await RespondAsync(ex.Message);
        }
    }

    [SlashCommand("resume", "恢復播放。")]
    [DefaultMemberPermissions(_permission)]
    public async Task ResumeAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out XLavaPlayer player))
        {
            await RespondAsync("我並未連接至任一語音頻道！");

            return;
        }

        if (player.PlayerState != PlayerState.Paused)
        {
            await RespondAsync("我沒有辦法在未播放任何歌曲的時候恢復播放！");

            return;
        }

        try
        {
            await player.ResumeAsync();
            await RespondAsync($"已恢復播放：{player.Track.Title}");
        }
        catch (Exception ex)
        {
            _logger.LogError("{Exception}", ex);

            await RespondAsync(ex.Message);
        }
    }

    [SlashCommand("stop", "停止播放。")]
    [DefaultMemberPermissions(_permission)]
    public async Task StopAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out XLavaPlayer player))
        {
            await RespondAsync("我並未連接至任一語音頻道！");

            return;
        }

        if (player.PlayerState == PlayerState.Stopped)
        {
            await RespondAsync("哇，我無法停止已停止的播放。");

            return;
        }

        try
        {
            await player.StopAsync();
            await RespondAsync("不再播放任何東西。");
        }
        catch (Exception ex)
        {
            _logger.LogError("{Exception}", ex);

            await RespondAsync(ex.Message);
        }
    }

    [SlashCommand("skip", "跳過目前歌曲。")]
    [DefaultMemberPermissions(_permission)]
    public async Task SkipAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out XLavaPlayer player))
        {
            await RespondAsync("我並未連接至任一語音頻道！");

            return;
        }

        if (player.PlayerState != PlayerState.Playing)
        {
            await RespondAsync("哇，我無法在未播放任何東西的時候跳過目前歌曲。");

            return;
        }

        var voiceChannelUsers = (player.VoiceChannel as SocketVoiceChannel)?.Users
            .Where(x => !x.IsBot)
            .ToArray();

        if (_audioService.VoteQueue.Contains(Context.User.Id))
        {
            await RespondAsync("您無法重複投票。");

            return;
        }

        _audioService.VoteQueue.Add(Context.User.Id);

        if (voiceChannelUsers != null)
        {
            var percentage = _audioService.VoteQueue.Count / voiceChannelUsers.Length * 100;

            if (percentage < 85)
            {
                await RespondAsync("您需要 85%（含）以上的投票率以跳過目前歌曲。");

                return;
            }
        }

        try
        {
            var (oldTrack, currenTrack) = await player.SkipAsync();

            await RespondAsync($"已跳過：{oldTrack.Title}\n現正播放：{player.Track.Title}");
        }
        catch (Exception ex)
        {
            _logger.LogError("{Exception}", ex);

            await RespondAsync(ex.Message);
        }

        _audioService.VoteQueue.Clear();
    }

    [SlashCommand("seek", "更改目前歌曲的播放時間位置。")]
    [DefaultMemberPermissions(_permission)]
    public async Task SeekAsync(TimeSpan timeSpan)
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out XLavaPlayer player))
        {
            await RespondAsync("我並未連接至任一語音頻道！");

            return;
        }

        if (player.PlayerState != PlayerState.Playing)
        {
            await RespondAsync("哇，我無法在未播放任何東西的時候更改目前歌曲的播放時間位置。");

            return;
        }

        try
        {
            await player.SeekAsync(timeSpan);
            await RespondAsync($"我已快轉 `{player.Track.Title}` 至 {timeSpan}。");
        }
        catch (Exception ex)
        {
            _logger.LogError("{Exception}", ex);

            await RespondAsync(ex.Message);
        }
    }

    [SlashCommand("volume", "調整音量。")]
    [DefaultMemberPermissions(_permission)]
    public async Task VolumeAsync(ushort volume)
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out XLavaPlayer player))
        {
            await RespondAsync("我並未連接至任一語音頻道！");

            return;
        }

        try
        {
            await player.UpdateVolumeAsync(volume);
            await RespondAsync($"我已將播放器的音量調整至 {volume}。");
        }
        catch (Exception ex)
        {
            _logger.LogError("{Exception}", ex);

            await RespondAsync(ex.Message);
        }
    }

    [SlashCommand("now-playing", "目前播放中的歌曲。")]
    [DefaultMemberPermissions(_permission)]
    public async Task NowPlayingAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out XLavaPlayer player))
        {
            await RespondAsync("我並未連接至任一語音頻道！");

            return;
        }

        if (player.PlayerState != PlayerState.Playing)
        {
            await RespondAsync("哇，我目前並沒有播放任何的音軌。");

            return;
        }

        var track = player.Track;
        var artwork = await track.FetchArtworkAsync();

        var embed = new EmbedBuilder()
            .WithAuthor(track.Author, Context.Client.CurrentUser.GetAvatarUrl(), track.Url)
            .WithTitle($"現正播放：{track.Title}")
            .WithImageUrl(artwork)
            .WithFooter($"{track.Position}／{track.Duration}");

        await RespondAsync(embed: embed.Build());
    }

    [SlashCommand("genius", "從 Genuis 取得歌詞。", runMode: Discord.Interactions.RunMode.Async)]
    [DefaultMemberPermissions(_permission)]
    public async Task ShowGeniusLyrics()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out XLavaPlayer player))
        {
            await RespondAsync("我並未連接至任一語音頻道！");

            return;
        }

        try
        {
            if (player.PlayerState != PlayerState.Playing)
            {
                await RespondAsync("哇，我目前並沒有播放任何的音軌。");

                return;
            }

            // TODO: 2021-12-30 有機會會報錯。
            var lyrics = await player.Track.FetchLyricsFromGeniusAsync();

            if (string.IsNullOrWhiteSpace(lyrics))
            {
                await RespondAsync($"找不到 {player.Track.Title}　的歌詞。");

                return;
            }

            await SendLyricsAsync(lyrics);
        }
        catch (Exception ex)
        {
            _logger.LogError("{Exception}", ex);

            await RespondAsync($"發生錯誤，在 Genius 找不到 {player.Track.Title} 的歌詞。");
        }
    }

    [SlashCommand("ovh", "從 OVH 取得歌詞。", runMode: Discord.Interactions.RunMode.Async)]
    [DefaultMemberPermissions(_permission)]
    public async Task ShowOvhLyrics()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out XLavaPlayer player))
        {
            await RespondAsync("我並未連接至任一語音頻道！");

            return;
        }

        try
        {
            if (player.PlayerState != PlayerState.Playing)
            {
                await RespondAsync("哇，我目前並沒有播放任何的音軌。");

                return;
            }

            // 2021-12-30 有機會報錯。
            var lyrics = await player.Track.FetchLyricsFromOvhAsync();

            if (string.IsNullOrWhiteSpace(lyrics))
            {
                await RespondAsync($"找不到 {player.Track.Title} 的歌詞。");

                return;
            }

            await SendLyricsAsync(lyrics);
        }
        catch (Exception ex)
        {
            _logger.LogError("{Exception}", ex);

            await RespondAsync($"發生錯誤，在 OVH 找不到 {player.Track.Title}　的歌詞。");
        }
    }

    [SlashCommand("queue", "佇列清單。")]
    [DefaultMemberPermissions(_permission)]
    public Task QueueAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out XLavaPlayer player))
        {
            return RespondAsync("我並未連接至任一語音頻道！");
        }

        if (player.Queue.Count > 0)
        {
            // 2021-12-30 當只有一首歌時，會出錯。
            return RespondAsync(player.PlayerState != PlayerState.Playing
                ? "哇，我目前並沒有播放任何的音軌。"
                : string.Join(Environment.NewLine, player.Queue.Select(x => x.Title)));
        }
        else
        {
            return RespondAsync("佇列清單是空的。");
        }
    }

    /// <summary>
    /// 傳送歌詞
    /// </summary>
    /// <param name="lyrics">字串，歌詞。</param>
    /// <returns>Task</returns>
    private async Task SendLyricsAsync(string lyrics)
    {
        string[] splitLyrics = lyrics.Split(Environment.NewLine);

        StringBuilder stringBuilder = new();

        foreach (string rawLine in splitLyrics)
        {
            if (rawLine.Contains('['))
            {
                stringBuilder.Append(Environment.NewLine);
            }

            if (Range.Contains(stringBuilder.Length))
            {
                await RespondAsync($"```{stringBuilder}```");

                stringBuilder.Clear();
            }
            else
            {
                stringBuilder.AppendLine(rawLine);
            }
        }

        await RespondAsync($"```{stringBuilder}```");
    }
}