using Avalonia.Controls;
using Launcher;

namespace BigWhiteDot
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Topmost = true;
        }

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
        #endregion

        #region Routines
        public void UpdateMenuItems()
        {
        }
        #endregion
    }
}