using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Victoria;
using Victoria.Enums;
using Victoria.EventArgs;

namespace DBotDDRefuge.Common.Audio;

/// <summary>
/// AudioService
/// <para>來源：https://github.com/Yucked/Victoria/tree/examples </para>
/// </summary>
public sealed class AudioService
{
    public readonly HashSet<ulong> VoteQueue;

    private readonly LavaNode<XLavaPlayer> _lavaNode;
    private readonly ILogger<AudioService> _logger;
    private readonly ConcurrentDictionary<ulong, CancellationTokenSource> _disconnectTokens;

    public AudioService(LavaNode<XLavaPlayer> lavaNode, ILogger<AudioService> logger)
    {
        _lavaNode = lavaNode;
        _logger = logger;

        _disconnectTokens = new ConcurrentDictionary<ulong, CancellationTokenSource>();

        _lavaNode.OnLog += arg =>
        {
            CustomFunction.WriteLog(_logger, arg);

            return Task.CompletedTask;
        };

        _lavaNode.OnPlayerUpdated += OnPlayerUpdated;
        _lavaNode.OnStatsReceived += OnStatsReceived;
        _lavaNode.OnTrackEnded += OnTrackEnded;
        _lavaNode.OnTrackStarted += OnTrackStarted;
        _lavaNode.OnTrackException += OnTrackException;
        _lavaNode.OnTrackStuck += OnTrackStuck;
        _lavaNode.OnWebSocketClosed += OnWebSocketClosed;

        VoteQueue = new HashSet<ulong>();
    }

    private Task OnPlayerUpdated(PlayerUpdateEventArgs arg)
    {
        _logger.LogInformation(
            "已接收音軌 {TrackTitle} 的更新，進度：{Position}",
            arg.Track.Title,
            arg.Position);

        return Task.CompletedTask;
    }

    private Task OnStatsReceived(StatsEventArgs arg)
    {
        _logger.LogInformation("Lavalink 已經執行 {UpTime}。", arg.Uptime);

        return Task.CompletedTask;
    }

    private async Task OnTrackStarted(TrackStartEventArgs arg)
    {
        await arg.Player.TextChannel.SendMessageAsync($"現正播放：{arg.Track.Title}");

        if (!_disconnectTokens.TryGetValue(arg.Player.VoiceChannel.Id, out var value))
        {
            return;
        }

        if (value.IsCancellationRequested)
        {
            return;
        }

        value.Cancel(true);

        await arg.Player.TextChannel.SendMessageAsync("已取消自動斷開連線！");
    }

    private async Task OnTrackEnded(TrackEndedEventArgs args)
    {
        if (args.Reason != TrackEndReason.Finished)
        {
            return;
        }

        var player = args.Player;

        if (!player.Queue.TryDequeue(out var lavaTrack))
        {
            await player.TextChannel.SendMessageAsync("佇列內的音軌已播放完畢！請加入更多的音軌一同來搖滾吧！");

            _ = InitiateDisconnectAsync(args.Player, TimeSpan.FromSeconds(10));

            return;
        }

        if (lavaTrack is null)
        {
            await player.TextChannel.SendMessageAsync("佇列中的下一個項目不是音軌。");

            return;
        }

        await args.Player.PlayAsync(lavaTrack);
        await args.Player.TextChannel.SendMessageAsync(
            $"{args.Reason}: {args.Track.Title}\n現正播放：{lavaTrack.Title}");
    }

    private async Task InitiateDisconnectAsync(LavaPlayer player, TimeSpan timeSpan)
    {
        if (!_disconnectTokens.TryGetValue(player.VoiceChannel.Id, out var value))
        {
            value = new CancellationTokenSource();

            _disconnectTokens.TryAdd(player.VoiceChannel.Id, value);
        }
        else if (value.IsCancellationRequested)
        {
            _disconnectTokens.TryUpdate(player.VoiceChannel.Id, new CancellationTokenSource(), value);

            value = _disconnectTokens[player.VoiceChannel.Id];
        }

        await player.TextChannel.SendMessageAsync($"已啟動自動斷線機制！在 {timeSpan} 秒後自動斷線……");

        var isCancelled = SpinWait.SpinUntil(() => value.IsCancellationRequested, timeSpan);

        if (isCancelled)
        {
            return;
        }

        await _lavaNode.LeaveAsync(player.VoiceChannel);
        await player.TextChannel.SendMessageAsync("甜心，等妳有空時再邀請我吧。");
    }

    private async Task OnTrackException(TrackExceptionEventArgs arg)
    {
        _logger.LogError("音軌 {Title} 拋出一個例外。請確認 Lavalink 的控制臺／日誌紀錄。", arg.Track.Title);

        arg.Player.Queue.Enqueue(arg.Track);

        await arg.Player.TextChannel.SendMessageAsync(
            $"{arg.Track.Title} 在拋出一個例外後，已重新加入至佇列清單內。");
    }

    private async Task OnTrackStuck(TrackStuckEventArgs arg)
    {
        _logger.LogError(
            "音軌 {Title} 被卡住 {Threshold}ms。請確認 Lavalink 的控制臺／日誌紀錄。", arg.Track.Title);

        arg.Player.Queue.Enqueue(arg.Track);

        await arg.Player.TextChannel.SendMessageAsync(
            $"{arg.Track.Title} 在被卡住後，已重新加入至佇列清單內。");
    }

    private Task OnWebSocketClosed(WebSocketClosedEventArgs arg)
    {
        _logger.LogCritical("Discord WebSocket 已因下列的原因斷線：{Reason}", arg.Reason);

        return Task.CompletedTask;
    }
}