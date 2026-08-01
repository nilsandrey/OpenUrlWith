using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace OpenWithTool.Services;

public interface IWindowingService
{
    void ConfigureMainWindow(Window window);
    void ConfigureDialog(Window window, Window owner, int widthDip, int heightDip, bool isResizable);
}

public sealed class WindowingService : IWindowingService
{
    private const int GwlExStyle = -20;
    private const int GwlpHwndParent = -8;
    private const long WsExToolWindow = 0x00000080L;

    public void ConfigureMainWindow(Window window)
    {
        ConfigureWindow(window, owner: null, widthDip: 760, heightDip: 640, isResizable: true, isModal: false);

        if (window.AppWindow.Presenter is OverlappedPresenter presenter)
            presenter.IsAlwaysOnTop = true;
    }

    public void ConfigureDialog(Window window, Window owner, int widthDip, int heightDip, bool isResizable)
    {
        ConfigureWindow(window, owner, widthDip, heightDip, isResizable, isModal: true);
    }

    private static void ConfigureWindow(
        Window window,
        Window? owner,
        int widthDip,
        int heightDip,
        bool isResizable,
        bool isModal)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var scale = GetDpiForWindow(hwnd) / 96.0;
        var width = (int)Math.Round(widthDip * scale);
        var height = (int)Math.Round(heightDip * scale);

        var presenter = isModal
            ? OverlappedPresenter.CreateForDialog()
            : OverlappedPresenter.Create();

        presenter.IsResizable = isResizable;
        presenter.IsMaximizable = isResizable;
        presenter.IsMinimizable = !isModal;
        presenter.IsModal = isModal;

        if (owner != null)
        {
            var ownerHwnd = WinRT.Interop.WindowNative.GetWindowHandle(owner);
            SetWindowLongPtr(hwnd, GwlpHwndParent, ownerHwnd);
        }

        window.AppWindow.SetPresenter(presenter);
        window.AppWindow.SetIcon("Assets/AppIcon.ico");

        HideFromTaskbar(hwnd);
        CenterWindow(window, owner, width, height);
    }

    private static void CenterWindow(Window window, Window? owner, int width, int height)
    {
        int left;
        int top;

        if (owner != null)
        {
            left = owner.AppWindow.Position.X + Math.Max(0, (owner.AppWindow.Size.Width - width) / 2);
            top = owner.AppWindow.Position.Y + Math.Max(0, (owner.AppWindow.Size.Height - height) / 2);
        }
        else
        {
            var displayArea = DisplayArea.GetFromWindowId(window.AppWindow.Id, DisplayAreaFallback.Primary);
            var workArea = displayArea.WorkArea;
            left = workArea.X + Math.Max(0, (workArea.Width - width) / 2);
            top = workArea.Y + Math.Max(0, (workArea.Height - height) / 2);
        }

        window.AppWindow.MoveAndResize(new RectInt32(left, top, width, height));
    }

    private static void HideFromTaskbar(nint hwnd)
    {
        var extendedStyle = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
        SetWindowLongPtr(hwnd, GwlExStyle, new nint(extendedStyle | WsExToolWindow));
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint hwnd);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(nint hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr(nint hwnd, int index, nint newLong);
}
