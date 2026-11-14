using PCL.Core.App;
using PCL.Core.ViewModel;

namespace PCL;

/// <summary>工具页面左侧导航栏的视图模型。</summary>
public class PageToolsLeftViewModel : ViewModelBase
{
    /// <summary>
    ///     联机大厅是否可用。
    ///     大厅服务器地址由编译期注入的密钥提供，未配置时该入口无法工作，应予以隐藏。
    /// </summary>
    public static bool IsLobbyAvailable =>
        Secrets.LinkServers.Length > 0 && !string.IsNullOrWhiteSpace(Secrets.LinkServers[0]);

    public bool IsHiddenPage(bool isGameLinkChecked, bool isTestChecked, bool isForceShow)
    {
        var hide = Config.Preference.Hide;
        var isHiddenPage = isGameLinkChecked && hide.ToolsGameLink || isTestChecked && hide.ToolsTest;
        return isForceShow ? false : isHiddenPage;
    }

    public FormMain.PageSubType ResolveInitialPageId()
    {
        // 联机大厅不可用时，不把首页定位到该页面
        if (!IsLobbyAvailable) return FormMain.PageSubType.ToolsTest;
        var hide = Config.Preference.Hide;
        if (!hide.ToolsGameLink) return FormMain.PageSubType.ToolsGameLink;
        if (!hide.ToolsTest) return FormMain.PageSubType.ToolsTest;
        return FormMain.PageSubType.ToolsGameLink;
    }
}
