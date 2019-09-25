namespace mpDrawOrderByLayer
{
    using System;
    using System.Windows;
    using System.Windows.Input;
    using Autodesk.AutoCAD.Internal;

    public partial class DrawOrderByLayer
    {
        public DrawOrderByLayer()
        {
            InitializeComponent();
            Title = ModPlusAPI.Language.GetItem("mpDrawOrderByLayer", "h1");
        }
        // Окно загрузилось
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SizeToContent = SizeToContent.Manual;
        }

        private void DrawOrderByLayer_OnClosed(object sender, EventArgs e)
        {
            ((MainViewModel)DataContext)?.OnClosed();
        }

        private void DrawOrderByLayer_OnMouseEnter(object sender, MouseEventArgs e)
        {
            Focus();
        }

        private void DrawOrderByLayer_OnMouseLeave(object sender, MouseEventArgs e)
        {
            Utils.SetFocusToDwgView();
        }
        
        private void DrawOrderByLayer_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }
    }
}
