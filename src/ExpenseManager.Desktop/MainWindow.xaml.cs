using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ExpenseManager.Desktop.Services;
using ExpenseManager.Desktop.Services.Branding;
using ExpenseManager.Desktop.ViewModels;
using ExpenseManager.Desktop.Views.Dialogs;
using WpfApplication = System.Windows.Application;

namespace ExpenseManager.Desktop;

public partial class MainWindow : Window
{
    private const int WmGetminmaxinfo = 0x0024;
    private const uint MonitorDefaulttonearest = 2;

    private readonly MainWindowViewModel _viewModel;
    private readonly IUserSessionService _sessionService;
    private readonly Func<LoginWindow> _loginWindowFactory;
    private readonly IBrandingService _brandingService;
    private readonly ILocalizationService _localizationService;
    private readonly Button? _windowStateButton;
    private readonly ImageSource? _defaultIcon;
    private HwndSource? _hwndSource;

    public MainWindow(MainWindowViewModel viewModel, IUserSessionService sessionService, Func<LoginWindow> loginWindowFactory, IBrandingService brandingService, ILocalizationService localizationService)
    {
        var resourceLocator = new Uri("/ExpenseManager.Desktop;component/MainWindow.xaml", UriKind.Relative);
        WpfApplication.LoadComponent(this, resourceLocator);
        _windowStateButton = (Button?)FindName("WindowStateButton");
        _viewModel = viewModel;
        _sessionService = sessionService;
        _loginWindowFactory = loginWindowFactory;
        _brandingService = brandingService;
        _localizationService = localizationService;
        _defaultIcon = Icon;
        DataContext = _viewModel;
        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoadedAsync;
        StateChanged += OnWindowStateChanged;
        UpdateWindowStateButtonIcon();
        Closed += OnClosed;
        _brandingService.BrandingChanged += OnBrandingChanged;
        ApplyBrandingIcon();
    }

    public MainWindowViewModel ViewModel => _viewModel;

    private async void OnLoadedAsync(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnLoadedAsync;
        if (!_sessionService.IsAuthenticated)
        {
            var loginWindow = _loginWindowFactory();
            loginWindow.Owner = this;
            var result = loginWindow.ShowDialog();
            if (result != true)
            {
                Close();
                return;
            }

            ApplySessionCultureIfAvailable();
            _viewModel.OnUserAuthenticated();
        }
        else
        {
            ApplySessionCultureIfAvailable();
        }

        await _viewModel.InitializeAsync();
        ApplySessionCultureIfAvailable();
    }

    private void OnHeaderMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void OnMinimizeClicked(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnToggleWindowStateClicked(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        UpdateWindowStateButtonIcon();
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        UpdateWindowStateButtonIcon();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        SourceInitialized -= OnSourceInitialized;
        _hwndSource = (HwndSource?)PresentationSource.FromVisual(this);
        if (_hwndSource is not null)
        {
            _hwndSource.AddHook(WndProc);
        }
    }

    private void UpdateWindowStateButtonIcon()
    {
        if (_windowStateButton is null)
        {
            return;
        }

        _windowStateButton.Content = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
    }

    private void OnBrandingChanged(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(ApplyBrandingIcon);
    }

    private void ApplyBrandingIcon()
    {
        var iconPath = _brandingService.Current.IconPath;

        if (!string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(iconPath);
                bitmap.EndInit();
                Icon = bitmap;
                return;
            }
            catch
            {
                Icon = _defaultIcon;
                return;
            }
        }

        Icon = _defaultIcon;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_hwndSource is not null)
        {
            _hwndSource.RemoveHook(WndProc);
            _hwndSource = null;
        }

        _brandingService.BrandingChanged -= OnBrandingChanged;
    }

    private void ApplySessionCultureIfAvailable()
    {
        if (!_sessionService.IsAuthenticated)
        {
            return;
        }

        var culture = _sessionService.PreferredLanguage;
        if (string.IsNullOrWhiteSpace(culture))
        {
            return;
        }

        _localizationService.TryApplyCulture(culture, out _);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmGetminmaxinfo)
        {
            var info = Marshal.PtrToStructure<MinMaxInfo>(lParam);
            var monitor = MonitorFromWindow(hwnd, MonitorDefaulttonearest);
            if (monitor != IntPtr.Zero)
            {
                var monitorInfo = new MonitorInfo { cbSize = Marshal.SizeOf<MonitorInfo>() };
                if (GetMonitorInfo(monitor, ref monitorInfo))
                {
                    var workArea = monitorInfo.rcWork;
                    var monitorArea = monitorInfo.rcMonitor;

                    info.ptMaxPosition.X = Math.Abs(workArea.Left - monitorArea.Left);
                    info.ptMaxPosition.Y = Math.Abs(workArea.Top - monitorArea.Top);
                    info.ptMaxSize.X = Math.Abs(workArea.Right - workArea.Left);
                    info.ptMaxSize.Y = Math.Abs(workArea.Bottom - workArea.Top);
                    info.ptMaxTrackSize = info.ptMaxSize;

                    Marshal.StructureToPtr(info, lParam, false);
                    handled = true;
                }
            }
        }

        return IntPtr.Zero;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int cbSize;
        public Rect rcMonitor;
        public Rect rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PointStruct
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public PointStruct ptReserved;
        public PointStruct ptMaxSize;
        public PointStruct ptMaxPosition;
        public PointStruct ptMinTrackSize;
        public PointStruct ptMaxTrackSize;
    }
}