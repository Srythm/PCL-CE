using PCL.Core.App.Region;

namespace PCL;

/// <summary>
///     区域限制功能判定。
///     本分支已移除下载相关功能（下载、补全、整合包安装、启动补齐等）的地区限制：
///     <see cref="Check" /> 与 <see cref="ThrowIfRestricted" /> 恒为放行，仅保留 <see cref="Assess" />
///     的判定结果用于诊断日志。
/// </summary>
internal static class RestrictedFeaturePolicy
{
    internal static RegionAssessment Assess() => RegionPolicy.Evaluate(RegionPolicy.ReadWindowsSignals());

    /// <summary>
    ///     是否处于受限地区。地区限制已移除，故恒为 <see langword="false" />。
    /// </summary>
    internal static bool IsRestricted => false;

    internal static bool Check(string feature, bool showHint = true)
    {
        // 地区限制已移除：功能一律放行。仅在原本会被拦截时留下诊断日志，便于排查网络问题。
        var assessment = Assess();
        if (assessment.IsRestricted)
            LauncherCommon.Log(
                $"[RegionPolicy] 功能 {feature} 的地区限制已被移除，已放行。原判定原因：{assessment.Reason}");
        return true;
    }

    internal static void ThrowIfRestricted(string feature)
    {
        // 地区限制已移除，不再中断流程。
    }
}
