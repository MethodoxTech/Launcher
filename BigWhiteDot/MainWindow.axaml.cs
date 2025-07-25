using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.VisualTree;
using Launcher.Shared;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BigWhiteDot
{
    public partial class MainWindow : Window
    {
        #region Windows Specific
        // Win32 constants
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW = 0x00040000;

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private void HideFromAltTab()
        {
            // Get the Avalonia TopLevel (our native host)
            if (this.GetVisualRoot() is TopLevel top 
                // and ask it for its platform handle
                && top.TryGetPlatformHandle() is IPlatformHandle handle
                && handle.Handle != IntPtr.Zero)
            {
                nint hWnd = handle.Handle;
                // read current ex‑style
                int ex = GetWindowLong(hWnd, GWL_EXSTYLE);
                // add WS_EX_TOOLWINDOW, remove WS_EX_APPWINDOW
                ex |= WS_EX_TOOLWINDOW;
                ex &= ~WS_EX_APPWINDOW;
                SetWindowLong(hWnd, GWL_EXSTYLE, ex);
            }
        }
        #endregion

        #region Construction
        public MainWindow()
        {
            InitializeComponent();
            InitializeTrayIcon();

            Topmost = true;

            // Wait until native window exists
            Opened += (_, __) => HideFromAltTab();
            Closing += (_, e) =>
            {
                if (!_reallyClosing)
                {
                    e.Cancel = true;
                    Hide();
                }
            };
        }
        private void InitializeTrayIcon()
        {
            _trayIcon = new TrayIcon
            {
                Icon = new WindowIcon(new Bitmap(AssetLoader.Open(new Uri("avares://BigWhiteDot/Assets/Icon.ico")))),
                ToolTipText = "Big White Dot"
            };

            // Build it once at startup
            _trayIcon.Menu = BuildTrayMenu();

            // Register toggle‐window handler
            _trayIcon.Clicked += (s, e) =>
            {
                if (IsVisible) Hide();
                else
                {
                    Show();
                    Activate();
                }
            };

            // Actually attach it to the app
            TrayIcon.SetIcons(Application.Current, [_trayIcon]);
        }
        #endregion

        #region Properties
        private TrayIcon _trayIcon;
        private bool _reallyClosing;
        /// <summary>
        /// Keep up most‑recent shortcuts
        /// </summary>
        private readonly LinkedList<string> _recent = new();
        private const int MaxRecent = 8;
        #endregion

        #region Events
        private void Window_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                BeginMoveDrag(e);
        }
        private void Border_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
        {
            // Not working, doesn't do anything
            if (sender is Border border)
            {
                Avalonia.Input.PointerPoint pp = e.GetCurrentPoint(border);
                if (pp.Properties.IsLeftButtonPressed)
                {
                    UpdateMenuItems();
                    // Open the ContextMenu at the border’s position
                    border.ContextMenu?.Open(border);
                    e.Handled = true;
                }
            }
        }
        private void Border_ContextRequested(object? sender, ContextRequestedEventArgs e)
        {
            if (sender is Border b)
            {
                UpdateMenuItems();
                b.ContextMenu?.Open(b);
                e.Handled = true;
            }
        }
        private void Border_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            // Launch edit
            LauncherCore.ConfigurationPath.OpenWithDefaultProgram(null);
        }
        private void HideMenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
            => Hide();
        private void ExitMenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // Allow the window to actually close, then shut down:
            _reallyClosing = true;
            this.Close();
            (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
                ?.Shutdown();
        }
        private void Window_Closed(object? sender, System.EventArgs e)
            => _trayIcon?.Dispose();
        #endregion

        #region Routines
        /// <summary>
        /// Construct a fresh NativeMenu from recent, favorites, etc.
        /// </summary>
        private NativeMenu BuildTrayMenu()
        {
            NativeMenu menu = [];

            // Recent submenu
            NativeMenuItem recentSub = new("Recent")
            {
                Menu = []
            };
            if (_recent.Count > 0)
            {
                foreach (string name in _recent)
                {
                    NativeMenuItem item = new(name);
                    item.Click += (_, __) => LaunchAndRecord(name);
                    recentSub.Menu.Items.Add(item);
                }
            }
            else
            {
                NativeMenuItem empty = new("(no recents yet)")
                {
                    IsEnabled = false
                };
                recentSub.Menu.Items.Add(empty);
            }
            menu.Items.Add(recentSub);

            // Favorites submenu
            NativeMenuItem favSub = new("Favorites")
            {
                Menu = []
            };
            Dictionary<string, Shortcut> configs = LauncherCore.ReadConfigurations();
            if (configs.Count > 0)
            {
                foreach (KeyValuePair<string, Shortcut> kv in configs)
                {
                    string name = kv.Key;
                    NativeMenuItem item = new(name);
                    item.Click += (_, __) => LaunchAndRecord(name);
                    favSub.Menu.Items.Add(item);
                }
            }
            else
            {
                NativeMenuItem emptyFav = new("(no favorites)")
                {
                    IsEnabled = false
                };
                favSub.Menu.Items.Add(emptyFav);
            }
            menu.Items.Add(favSub);

            // File System submenu
            NativeMenuItem fsSub = new("File System")
            {
                Menu = []
            };
            Dictionary<string, string> folders = new()
            {
                ["Desktop"] = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                ["Documents"] = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                ["Downloads"] = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
                ["Pictures"] = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            };
            foreach (KeyValuePair<string, string> kv in folders)
            {
                Bitmap icon = new(AssetLoader.Open(new Uri("avares://BigWhiteDot/Assets/Folder.png")));
                NativeMenuItem mi = new(kv.Key)
                {
                    Icon = icon
                };
                mi.Click += (_, __) => LaunchAndRecord(kv.Value, useDefaultProgram: true);
                fsSub.Menu.Items.Add(mi);
            }
            menu.Items.Add(fsSub);

            // EXIT
            NativeMenuItem exit = new("Exit");
            exit.Click += (_, __) =>
            {
                _reallyClosing = true;
                Close();
                if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d)
                    d.Shutdown();
            };
            menu.Items.Add(exit);

            return menu;
        }
        public void UpdateMenuItems()
        {
            // Recent menu
            RecentMenu.Items.Clear();
            foreach (string name in _recent)
            {
                RecentMenu.Items.Add(
                    MakeItem(name, () => LaunchAndRecord(name))
                );
            }
            if (_recent.Count == 0)
                RecentMenu.Items.Add(
                    MakeItem("(no recents yet)", () => { })
                );

            // Favorites menu (all configured shortcuts)
            FavoritesMenu.Items.Clear();
            Dictionary<string, Shortcut> configs = LauncherCore.ReadConfigurations();
            foreach (KeyValuePair<string, Shortcut> kv in configs)
            {
                string name = kv.Key;
                FavoritesMenu.Items.Add(
                    MakeItem(name, () => LaunchAndRecord(name))
                );
            }
            if (configs.Count == 0)
                FavoritesMenu.Items.Add(
                    MakeItem("(no favorites)", () => { })
                );

            // File System menu (hard‑coded common folders + icons)
            FileSystemMenu.Items.Clear();
            Dictionary<string, string> folders = new()
            {
                ["Desktop"] = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                ["Documents"] = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                ["Downloads"] = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
                ["Pictures"] = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };
            foreach (KeyValuePair<string, string> kv in folders)
            {
                // Load a folder icon
                Image icon = new() { Source = new Bitmap(AssetLoader.Open(new Uri("avares://BigWhiteDot/Assets/Folder.png"))), Width=16, Height=16 };
                MenuItem mi = MakeItem(
                    kv.Key,
                    () => LaunchAndRecord(kv.Value, useDefaultProgram: true),
                    icon: icon
                );
                FileSystemMenu.Items.Add(mi);
            }

            // EVERYTHING (leave empty for now)
            EverythingMenu.Items.Clear();
            EverythingMenu.IsVisible = false;
        }
        #endregion

        #region Callback
        private void LaunchAndRecord(string nameOrPath, bool useDefaultProgram = false, string[]? args = null)
        {
            // Launch
            try
            {
                if (LauncherCore.ReadConfigurations().ContainsKey(nameOrPath))
                    // Treat as shortcut name
                    LauncherCore.Launch(nameOrPath, args ?? Array.Empty<string>(), useDefaultProgram);
                else
                    // Treat as literal path/url
                    nameOrPath.Launch(args ?? Array.Empty<string>(), useDefaultProgram);
            }
            catch (Exception ex)
            {
                // you can show a Toast or dialog here if you want
                Console.WriteLine(ex.Message);
                return;
            }

            // Record in recents
            _recent.Remove(nameOrPath);
            _recent.AddFirst(nameOrPath);
            if (_recent.Count > MaxRecent)
                _recent.RemoveLast();

            // Refresh the tray menu
            _trayIcon.Menu = BuildTrayMenu();
        }
        #endregion

        #region Helpers
        private static MenuItem MakeItem(string header, Action onClick, Control? icon = null)
        {
            MenuItem mi = new() { Header = header };
            if (icon != null)
                mi.Icon = icon;
            mi.Click += (_, _) => onClick();
            return mi;
        }
        #endregion
    }
}