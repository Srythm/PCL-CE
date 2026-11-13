using System.Collections.Concurrent;
using PCL.Core.App;
using PCL.Core.App.Localization;

namespace PCL;

/// <summary>
///     实例游玩时长统计。
///     以实例为单位记录游戏进程的实际运行时长，并累加写入实例配置（<c>PCL/config.v1.yml</c>）。
/// </summary>
public static class InstancePlayTime
{
    /// <summary>
    ///     运行中会话的定期回写间隔。
    ///     启动器可能被设置为“启动游戏后立即退出”，此时进程退出事件不会被处理，故需要定期落盘。
    /// </summary>
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(30);

    private static readonly ConcurrentDictionary<string, Session> Sessions = new(StringComparer.OrdinalIgnoreCase);

    private static readonly object SyncRoot = new();

    /// <summary>
    ///     开始统计指定实例的游玩时长。同一实例重复调用不会重置已有会话。
    /// </summary>
    /// <param name="instancePath">实例文件夹路径</param>
    public static void Begin(string? instancePath)
    {
        if (string.IsNullOrWhiteSpace(instancePath)) return;

        lock (SyncRoot)
        {
            if (Sessions.ContainsKey(instancePath)) return;
            var session = new Session(instancePath);
            Sessions[instancePath] = session;
            session.FlushTimer = new System.Threading.Timer(
                _ => Flush(instancePath), null, FlushInterval, FlushInterval);
        }

        LauncherCommon.Log($"[PlayTime] 开始统计实例游玩时长：{instancePath}");
    }

    /// <summary>
    ///     把当前会话已经进行的时长写入配置，但不结束会话。
    /// </summary>
    /// <param name="instancePath">实例文件夹路径</param>
    public static void Flush(string? instancePath)
    {
        if (string.IsNullOrWhiteSpace(instancePath)) return;

        lock (SyncRoot)
        {
            if (!Sessions.TryGetValue(instancePath, out var session)) return;
            _PersistDelta(session);
        }
    }

    /// <summary>
    ///     结束统计，把剩余时长写入配置，并返回本次游玩的秒数。
    /// </summary>
    /// <param name="instancePath">实例文件夹路径</param>
    public static long End(string? instancePath)
    {
        if (string.IsNullOrWhiteSpace(instancePath)) return 0;

        long sessionSeconds;
        lock (SyncRoot)
        {
            if (!Sessions.TryRemove(instancePath, out var session)) return 0;
            sessionSeconds = Math.Max((long)(DateTime.Now - session.StartedAt).TotalSeconds, 0);
            _PersistDelta(session);
            session.FlushTimer?.Dispose();
            session.FlushTimer = null;
        }

        LauncherCommon.Log(
            $"[PlayTime] 实例游玩结束：{instancePath}，本次 {FormatDuration(sessionSeconds)}，"
            + $"累计 {FormatDuration(GetTotalSeconds(instancePath))}");
        return sessionSeconds;
    }

    /// <summary>
    ///     读取实例的累计游玩时长（秒），包含正在进行中的会话。
    /// </summary>
    /// <param name="instancePath">实例文件夹路径</param>
    public static long GetTotalSeconds(string? instancePath)
    {
        if (string.IsNullOrWhiteSpace(instancePath)) return 0;

        try
        {
            var total = (long)States.Instance.PlayTimeSeconds[instancePath];
            lock (SyncRoot)
            {
                if (Sessions.TryGetValue(instancePath, out var session))
                    total += (long)(DateTime.Now - session.StartedAt).TotalSeconds - session.PersistedSeconds;
            }

            return Math.Max(total, 0);
        }
        catch (Exception ex)
        {
            LauncherCommon.Log(ex, $"读取实例游玩时长失败（{instancePath}）");
            return 0;
        }
    }

    /// <summary>
    ///     把累计秒数格式化为界面展示文本。
    /// </summary>
    public static string Format(long totalSeconds)
    {
        if (totalSeconds <= 0) return Lang.Text("Instance.Overall.Info.PlayTime.Never");
        if (totalSeconds < 60) return Lang.Text("Instance.Overall.Info.PlayTime.LessThanMinute");

        var span = TimeSpan.FromSeconds(totalSeconds);
        if (span.TotalHours < 1)
            return Lang.Text("Instance.Overall.Info.PlayTime.Minutes", (long)span.TotalMinutes);
        if (span.TotalDays < 1)
            return Lang.Text("Instance.Overall.Info.PlayTime.HoursMinutes", (long)span.TotalHours, span.Minutes);
        return Lang.Text("Instance.Overall.Info.PlayTime.DaysHours", (long)span.TotalDays, span.Hours);
    }

    /// <summary>
    ///     把秒数格式化为日志文本，形如 <c>1h23m45s</c>。
    /// </summary>
    public static string FormatDuration(long totalSeconds)
    {
        var span = TimeSpan.FromSeconds(Math.Max(totalSeconds, 0));
        if (span.TotalHours >= 1)
            return $"{(long)span.TotalHours}h{span.Minutes:D2}m{span.Seconds:D2}s";
        return span.TotalMinutes >= 1 ? $"{span.Minutes}m{span.Seconds:D2}s" : $"{span.Seconds}s";
    }

    /// <summary>
    ///     把会话中尚未落盘的增量写入实例配置。
    /// </summary>
    /// <returns>本次写入的秒数</returns>
    private static long _PersistDelta(Session session)
    {
        var total = (long)(DateTime.Now - session.StartedAt).TotalSeconds;
        var delta = total - session.PersistedSeconds;
        if (delta <= 0) return 0;

        session.PersistedSeconds += delta;
        _AddSeconds(session.InstancePath, delta);
        return delta;
    }

    private static void _AddSeconds(string instancePath, long seconds)
    {
        try
        {
            var current = (long)States.Instance.PlayTimeSeconds[instancePath];
            var updated = Math.Min(current + seconds, int.MaxValue);
            States.Instance.PlayTimeSeconds[instancePath] = (int)updated;
        }
        catch (Exception ex)
        {
            LauncherCommon.Log(ex, $"写入实例游玩时长失败（{instancePath}）");
        }
    }

    /// <summary>
    ///     一次游戏进程的运行会话。
    /// </summary>
    private sealed class Session(string instancePath)
    {
        public string InstancePath { get; } = instancePath;

        public DateTime StartedAt { get; } = DateTime.Now;

        /// <summary>
        ///     已经写入配置的秒数，避免重复累加。
        /// </summary>
        public long PersistedSeconds { get; set; }

        /// <summary>
        ///     定期回写的定时器。
        /// </summary>
        public System.Threading.Timer? FlushTimer { get; set; }
    }
}
