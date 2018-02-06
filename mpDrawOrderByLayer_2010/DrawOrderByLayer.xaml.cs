#if ac2010
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
#elif ac2013
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
#endif
using System;
using Autodesk.AutoCAD.Runtime;
using System.Windows;
using System.Windows.Input;
using Autodesk.AutoCAD.Internal;
using ModPlusAPI;
using ModPlusAPI.Windows.Helpers;

namespace mpDrawOrderByLayer
{
    public partial class DrawOrderByLayer
    {
        public DrawOrderByLayer()
        {
            InitializeComponent();
            this.OnWindowStartUp();
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

        private void DrawOrderByLayer_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void DrawOrderByLayer_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }
    }

    public class DrawOrderByLayerFunction
    {
        DrawOrderByLayer _drawOrderByLayer;

        [CommandMethod("ModPlus", "MpDrawOrderByLayer", CommandFlags.Modal)]
        public void StartFunction()
        {
            Statistic.SendCommandStarting(new Interface());

            if (_drawOrderByLayer == null)
            {
                _drawOrderByLayer = new DrawOrderByLayer();
                var mainViewModel = new MainViewModel { ParentWindow = _drawOrderByLayer };
                _drawOrderByLayer.DataContext = mainViewModel;
                _drawOrderByLayer.Closed += window_Closed;
            }
            if (_drawOrderByLayer.IsLoaded)
                _drawOrderByLayer.Activate();
            else
                AcApp.ShowModelessWindow(
                    AcApp.MainWindow.Handle, _drawOrderByLayer);
        }

        private void window_Closed(object sender, EventArgs e)
        {
            _drawOrderByLayer = null;
        }
    }
}
