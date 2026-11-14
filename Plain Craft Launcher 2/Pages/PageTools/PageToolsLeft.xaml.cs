using System.Windows;
using System.Windows.Controls;

using PCL.Core.App.Localization;
namespace PCL;

public partial class PageToolsLeft : IPageView
{
    private readonly PageToolsLeftViewModel _vm = new();

    private bool isLoad;
    private bool isPageSwitched; // 如果在 Loaded 前切换到其他页面，会导致触发 Loaded 时再次切换一次

    public PageToolsLeft()
    {
        InitializeComponent();
        ApplyLobbyAvailability();
        AnimatedControl = PanItem;
        Loaded += PageLinkLeft_Loaded;
        Unloaded += PageOtherLeft_Unloaded;
    }

    /// <summary>
    ///     未配置大厅服务器地址时移除联机入口。
    ///     该地址由编译期注入的密钥提供，缺失时进入该页面只会报错，故不展示入口。
    /// </summary>
    private void ApplyLobbyAvailability()
    {
        if (PageToolsLeftViewModel.IsLobbyAvailable) return;
        // 初始子页面直接落到「百宝箱」：FormMain 会在切换时先读 pageID 再 PageGet，
        // 若此处仍为默认值 1，联机页会被提前构造并触发公告请求。
        pageID = FormMain.PageSubType.ToolsTest;
        ItemGameLink.IsEnabled = false;
        // 仅设置 Visibility 会被后续的页面动画与控件内部逻辑覆盖，故直接从视觉树移除。
        var hadCategory = PanItem.Children.Contains(TextGameLinkCategory);
        var hadItem = PanItem.Children.Contains(ItemGameLink);
        PanItem.Children.Remove(TextGameLinkCategory);
        PanItem.Children.Remove(ItemGameLink);
        Common.Log($"[Tools] 未配置大厅服务器，已移除联机入口（标题 {hadCategory}，条目 {hadItem}）",
            Common.LogLevel.Normal);
    }

    private void PageLinkLeft_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshNavigation();
    }

    public void OnNavigatedTo(object? parameter)
    {
        RefreshNavigation();
    }

    public void OnNavigatedFrom()
    {
    }

    private void RefreshNavigation()
    {
        var isHiddenPage = _vm.IsHiddenPage(ItemGameLink.Checked, ItemTest.Checked, PageSetupUI.HiddenForceShow);

        // 若页面错误，或尚未加载，则继续
        if (isLoad && !isHiddenPage)
            return;
        isLoad = true;
        // 刷新子页面隐藏情况
        PageSetupUI.HiddenRefresh();
        // 选择第一个未被禁用的子页面
        if (isPageSwitched)
            return;
        switch (_vm.ResolveInitialPageId())
        {
            case FormMain.PageSubType.ToolsTest:
                ItemTest.SetChecked(true, false, false);
                break;
            default:
                ItemGameLink.SetChecked(true, false, false);
                break;
        }
    }

    private void PageOtherLeft_Unloaded(object sender, RoutedEventArgs e)
    {
        isPageSwitched = false;
    }

    public void Refresh(object sender, EventArgs e)
    {
        var button = (MyIconButton)sender;
        if (button.Tag is null)
            return;
        double id = Common.Val(button.Tag);
        switch (id)
        {
            case (double)FormMain.PageSubType.ToolsGameLink:
            {
                if (Launcher.frmToolsGameLink is null)
                    Launcher.frmToolsGameLink = new PageToolsGameLink();
                Launcher.frmToolsGameLink.Reload();
                ItemGameLink.Checked = true;
                break;
            }
        }
    }

    #region 页面切换

    /// <summary>
    ///     当前页面的编号。
    /// </summary>
    public FormMain.PageSubType pageID = FormMain.PageSubType.ToolsGameLink;

    /// <summary>
    ///     勾选事件改变页面。
    /// </summary>
    private void PageCheck(object senderRaw, Common.RouteEventArgs e)
    {
        var sender = (MyListItem)senderRaw;
        // 尚未初始化控件属性时，sender.Tag 为 Nothing，会导致切换到页面 0
        // 若使用 IsLoaded，则会导致模拟点击不被执行（模拟点击切换页面时，控件的 IsLoaded 为 False）
        if (sender.Tag is not null)
            PageChange((FormMain.PageSubType)Common.Val(sender.Tag));
    }

    public object PageGet(FormMain.PageSubType? id = null)
    {
        var targetID = id ?? pageID;
        // 联机大厅不可用时，任何指向该页面的请求都回落到「百宝箱」，避免构造必然报错的页面
        if (!PageToolsLeftViewModel.IsLobbyAvailable && targetID == FormMain.PageSubType.ToolsGameLink)
            targetID = FormMain.PageSubType.ToolsTest;
        switch (targetID)
        {
            case FormMain.PageSubType.ToolsGameLink:
            {
                if (Launcher.frmToolsGameLink is null)
                    Launcher.frmToolsGameLink = new PageToolsGameLink();
                return Launcher.frmToolsGameLink;
            }
            case FormMain.PageSubType.ToolsTest:
            {
                if (Launcher.frmToolsTest is null)
                    Launcher.frmToolsTest = new PageToolsTest();
                return Launcher.frmToolsTest;
            }
            default:
            {
                throw new Exception("未知的更多子页面种类：" + (int)id);
            }
        }
    }

    /// <summary>
    ///     切换现有页面。
    /// </summary>
    public void PageChange(FormMain.PageSubType id)
    {
        if (!PageToolsLeftViewModel.IsLobbyAvailable && id == FormMain.PageSubType.ToolsGameLink)
            id = FormMain.PageSubType.ToolsTest;
        if (pageID == id)
            return;
        Animations.AniBeginControl();
        isPageSwitched = true;
        try
        {
            PageChangeRun((MyPageRight)PageGet(id));
            pageID = id;
        }
        catch (Exception ex)
        {
            Common.Log(
                ex,
                $"切换分页面失败（ID {(int)id}）",
                Common.LogLevel.Feedback,
                userSummary: Lang.Text("Tools.Error.OperationFailed"));
        }
        finally
        {
            Animations.AniEndControl();
        }
    }

    private static void PageChangeRun(MyPageRight target)
    {
        Animations.AniStop("FrmMain PageChangeRight"); // 停止主页面的右页面切换动画，防止它与本动画一起触发多次 PageOnEnter
        if (target.Parent is not null)
            target.SetValue(ContentPresenter.ContentProperty, null);
        Launcher.frmMain.pageRight = target;
        ((MyPageRight)Launcher.frmMain.PanMainRight.Child).PageOnExit();
        Animations.AniStart(new[]
        {
            Animations.AaCode(() =>
            {
                ((MyPageRight)Launcher.frmMain.PanMainRight.Child).PageOnForceExit();
                Launcher.frmMain.PanMainRight.Child = Launcher.frmMain.pageRight;
                Launcher.frmMain.pageRight.Opacity = 0d;
            }, 130),
            Animations.AaCode(() =>
            {
                // 延迟触发页面通用动画，以使得在 Loaded 事件中加载的控件得以处理
                Launcher.frmMain.pageRight.Opacity = 1d;
                Launcher.frmMain.pageRight.PageOnEnter();
            }, 30, true)
        }, "PageLeft PageChange");
    }

    #endregion
}
