using Avalonia.Controls;

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
        private void Border_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {

        }
        #endregion
    }
}