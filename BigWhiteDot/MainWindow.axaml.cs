using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Launcher;
using System;
using System.Collections.Generic;

namespace BigWhiteDot
{
    public partial class MainWindow : Window
    {
        #region Construction
        public MainWindow()
        {
            InitializeComponent();
            InitializeTrayIcon();

            Topmost = true;

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
                ToolTipText = "Show Big White Dot",
            };

            // Build a native menu
            NativeMenu menu = [];

            // "Recent" submenu
            NativeMenuItem recentSub = new("Recent") { Menu = [] };
            // Example: populate with a couple of dummy entries
            foreach (string? name in new[] { "Foo", "Bar" })
            {
                NativeMenuItem item = new(name);
                item.Click += (s, e) =>
                {
                    // your LaunchAndRecord(name) logic here
                    Console.WriteLine($"Launch {name}");
                };
                recentSub.Menu.Items.Add(item);
            }
            menu.Items.Add(recentSub);

            // "Exit"
            NativeMenuItem exitItem = new("Exit");
            exitItem.Click += (s, e) =>
            {
                // allow the window to really close, then shut down
                _reallyClosing = true;
                Close();
                if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d)
                    d.Shutdown();
            };
            menu.Items.Add(exitItem);

            // Wire it up
            _trayIcon.Menu = menu;
            TrayIcon.SetIcons(Application.Current, [_trayIcon]);

            // Click‑to‑toggle visibility
            _trayIcon.Clicked += (s, e) =>
            {
                if (IsVisible) Hide();
                else
                {
                    Show();
                    Activate();
                }
            };
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
            global::Launcher.Launcher.ConfigurationPath.OpenWithDefaultProgram(null);
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
        public void UpdateMenuItems()
        {
            // RECENT
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

            // FAVORITES (all configured shortcuts)
            FavoritesMenu.Items.Clear();
            Dictionary<string, Shortcut> configs = global::Launcher.Launcher.ReadConfigurations();
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

            // FILE SYSTEM (hard‑coded common folders + icons)
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
                // Load a folder icon from your Assets (or system) here if you have one:
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
                if (global::Launcher.Launcher.ReadConfigurations().ContainsKey(nameOrPath))
                    // Treat as shortcut name
                    global::Launcher.Launcher.Launch(nameOrPath, args ?? Array.Empty<string>(), useDefaultProgram);
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