#if ac2010
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
#elif ac2013
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
#endif
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using System.Windows;
using System.Linq;
using System.Windows.Input;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Internal;
using mpMsg;
using mpSettings;
using ModPlus;
using Exception = System.Exception;
using Visibility = System.Windows.Visibility;

// ModPlus

namespace mpDrawOrderByLayer
{
    /// <summary>
    /// Логика взаимодействия для DrawOrderByLayer.xaml
    /// </summary>
    public partial class DrawOrderByLayer
    {

        public DrawOrderByLayer()
        {
            InitializeComponent();
            MpWindowHelpers.OnWindowStartUp(
                this,
                MpSettings.GetValue("Settings", "MainSet", "Theme"),
                MpSettings.GetValue("Settings", "MainSet", "AccentColor"),
                MpSettings.GetValue("Settings", "MainSet", "BordersType")
                );
        }
        // Окно загрузилось
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Заполняем списки слоев
            FillLayers();
            // Проверка расширенных данных и установка начальных значений для режима "Авто"
            CheckXData();
        }
        private void DrawOrderByLayer_OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Focus();
        }

        private void DrawOrderByLayer_OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Utils.SetFocusToDwgView();
        }
        private void DrawOrderByLayer_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
        // Проверка расширенных данных и установка начальных значений для режима "Авто"
        private void CheckXData()
        {
            // Проверяем запись о состоянии режима "Авто"
            if (MpCadHelpers.HasXDataDictionary("MP_DOBLAuto"))
            {
                // Если такая запись существует, то проверяем состояние вкл/выкл
                var doblaStatus = MpCadHelpers.GetStringXData("MP_DOBLAuto");
                // Если состояние вкл
                if (doblaStatus.Equals("ON"))
                {
                    // Ставим галочку
                    ChkAutoMove.IsChecked = true;
                    GridAutoMove.Visibility = Visibility.Visible;
                }
                else
                {
                    ChkAutoMove.IsChecked = false;
                    GridAutoMove.Visibility = Visibility.Collapsed;
                }
                // Независимо от состояния проверяем и выставляем слои
                // Слой "Вверх"
                if (MpCadHelpers.HasXDataDictionary("MP_DOBLAuto_up"))
                {
                    var doblaUp = MpCadHelpers.GetStringXData("MP_DOBLAuto_up");
                    if (doblaUp.Equals("ON"))
                    {
                        ChkUpLayer.IsChecked = true;
                        TbUpLayer.IsEnabled = true;
                        CbUpLayers.IsEnabled = true;
                    }
                    else
                    {
                        ChkUpLayer.IsChecked = false;
                        TbUpLayer.IsEnabled = false;
                        CbUpLayers.IsEnabled = false;
                    }
                    var doblaUpLayer = MpCadHelpers.GetStringXData("MP_DOBLAuto_up_layer");
                    if (CbUpLayers.Items.Contains(doblaUpLayer))
                        CbUpLayers.SelectedItem = doblaUpLayer;
                    else CbUpLayers.SelectedIndex = 0;
                }
                else
                {
                    ChkUpLayer.IsChecked = true;
                    TbUpLayer.IsEnabled = true;
                    CbUpLayers.IsEnabled = true;
                    CbUpLayers.SelectedIndex = 0;
                }
                // Слой "Вниз"
                if (MpCadHelpers.HasXDataDictionary("MP_DOBLAuto_down"))
                {
                    var doblaDown = MpCadHelpers.GetStringXData("MP_DOBLAuto_down");
                    if (doblaDown.Equals("ON"))
                    {
                        ChkDownLayer.IsChecked = true;
                        TbDownLayer.IsEnabled = true;
                        CbDownLayers.IsEnabled = true;
                    }
                    else
                    {
                        ChkDownLayer.IsChecked = false;
                        TbDownLayer.IsEnabled = false;
                        CbDownLayers.IsEnabled = false;
                    }
                    var doblaDownLayer = MpCadHelpers.GetStringXData("MP_DOBLAuto_down_layer");
                    if (CbDownLayers.Items.Contains(doblaDownLayer))
                        CbDownLayers.SelectedItem = doblaDownLayer;
                    else CbDownLayers.SelectedIndex = 0;
                }
                else
                {
                    ChkDownLayer.IsChecked = true;
                    TbDownLayer.IsEnabled = true;
                    CbDownLayers.IsEnabled = true;
                    CbDownLayers.SelectedIndex = 0;
                }
            }
            else
            {
                // Если такой записи нет, убираем галочку с режима "Авто"
                ChkAutoMove.IsChecked = false;
                GridAutoMove.Visibility = Visibility.Collapsed;
                // а в списках слоев выбираем первый слой (Слой по умолчанию)
                ChkUpLayer.IsChecked = true;
                CbDownLayers.SelectedIndex = 0;
                ChkDownLayer.IsChecked = true;
                CbUpLayers.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Заполнение списков слоев
        /// </summary>
        private void FillLayers()
        {
            var cbs = new List<ComboBox> { CbDownLayers, CbUpLayers };
            // Сначала очищаем списки
            foreach (var cb in cbs)
                cb.Items.Clear();
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                // Open the Layer table for read
                var acLyrTbl = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
                if (acLyrTbl != null)
                {
                    foreach (var acObjId in acLyrTbl)
                    {
                        var acLyrTblRec = tr.GetObject(acObjId, OpenMode.ForRead) as LayerTableRecord;

                        if (acLyrTblRec != null & acLyrTblRec?.IsDependent == false)
                        {
                            foreach (var cb in cbs)
                                cb.Items.Add(acLyrTblRec.Name);
                            var chk = new CheckBox { Content = acLyrTblRec.Name };
                            if (!LbLayers.Items.Contains(chk))
                                LbLayers.Items.Add(chk);
                        }
                    }
                }
            }
        }
        #region Настройка режима "Авто"
        // Поставили галочку "Авто"
        private void ChkAutoMove_Checked(object sender, RoutedEventArgs e)
        {
            // Отоброжаем настройки
            GridAutoMove.Visibility = Visibility.Visible;
        }
        // Убрали галочку "Авто"
        private void ChkAutoMove_Unchecked(object sender, RoutedEventArgs e)
        {
            // Скрываем настройки
            GridAutoMove.Visibility = Visibility.Collapsed;
        }
        private void ChkUpLayer_Checked(object sender, RoutedEventArgs e)
        {
            TbUpLayer.IsEnabled = true;
            CbUpLayers.IsEnabled = true;
        }
        private void ChkUpLayer_Unchecked(object sender, RoutedEventArgs e)
        {
            TbUpLayer.IsEnabled = false;
            CbUpLayers.IsEnabled = false;
        }
        private void ChkDownLayer_Checked(object sender, RoutedEventArgs e)
        {
            TbDownLayer.IsEnabled = true;
            CbDownLayers.IsEnabled = true;
        }

        private void ChkDownLayer_Unchecked(object sender, RoutedEventArgs e)
        {
            TbDownLayer.IsEnabled = false;
            CbDownLayers.IsEnabled = false;
        }
        #endregion

        // Применить
        private void BtAccept_Click(object sender, RoutedEventArgs e)
        {
            // Сначала обрабатываем перемещение слоев из списка
            // Получаем список имен слоев, которые отмечены
            var layers = (
                from CheckBox chk in LbLayers.Items
                where chk.IsChecked != null && chk.IsChecked.Value
                select chk.Content.ToString()).ToList();
            // Если список слоев не пуст
            if (layers.Count > 0)
            {
                // Переворачиваем список
                layers.Reverse();

                var doc = AcApp.DocumentManager.MdiActiveDocument;
                var db = doc.Database;
                var ed = doc.Editor;
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var btr = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                    if (btr != null)
                    {
                        var dot = tr.GetObject(btr.DrawOrderTableId, OpenMode.ForWrite) as DrawOrderTable;
                        try
                        {
                            PrWait.Visibility = Visibility.Visible;
                            foreach (var lay in layers)
                            {
                                var tvs = new[]
                                {
                                    new TypedValue((int)DxfCode.LayerName,lay),
                                };
                                var sf = new SelectionFilter(tvs);
                                var psr = ed.SelectAll(sf);
                                if (psr.Value != null)
                                {
                                    var objs = new ObjectIdCollection();
                                    foreach (var objid in psr.Value.GetObjectIds())
                                    {
                                        var obj = tr.GetObject(objid, OpenMode.ForRead);
                                        if (obj.OwnerId == db.CurrentSpaceId)
                                            objs.Add(objid);
                                    }
                                    dot?.MoveToTop(objs);
                                }
                            }
                        }
                        finally
                        {
                            PrWait.Visibility = Visibility.Hidden;
                            tr.Commit();
                        }
                    }
                }
            }
            // Сохраняем значение вкл/выкл режима "Авто"
            var isChecked = ChkAutoMove.IsChecked;
            if (isChecked != null && (bool)isChecked)
            {
                // Сохраняем запись о включенном режиме
                MpCadHelpers.SetStringXData("MP_DOBLAuto", "ON");
                // Сохраняем настройки слоев
                var b = ChkUpLayer.IsChecked;
                if (b != null && (bool)b)
                    MpCadHelpers.SetStringXData("MP_DOBLAuto_up", "ON");
                else MpCadHelpers.SetStringXData("MP_DOBLAuto_up", "OFF");
                var @checked = ChkDownLayer.IsChecked;
                if (@checked != null && (bool)@checked)
                    MpCadHelpers.SetStringXData("MP_DOBLAuto_down", "ON");
                else MpCadHelpers.SetStringXData("MP_DOBLAuto_down", "OFF");
                // Сохраняем выбранные слои в файл
                MpCadHelpers.SetStringXData("MP_DOBLAuto_up_layer", CbUpLayers.SelectedItem.ToString());
                MpCadHelpers.SetStringXData("MP_DOBLAuto_down_layer", CbDownLayers.SelectedItem.ToString());
                // Включаем обработчики событий
                var doev = new DrawOrderByLayerEvents();
                doev.On();
            }
            else
            {
                MpCadHelpers.SetStringXData("MP_DOBLAuto", "OFF");
                // Выключаем обработчики событий
                var doev = new DrawOrderByLayerEvents();
                doev.Off();
            }
            // Закрываем окно
            Close();
        }
        #region Управление списком слоев
        // Переместить слой вверх
        private void BtLayerUp_Click(object sender, RoutedEventArgs e)
        {
            if (LbLayers.SelectedIndex == -1) return;
            var item = LbLayers.SelectedItem;
            var index = LbLayers.Items.IndexOf(item);
            if (index > 0)
            {
                LbLayers.Items.Remove(item);
                LbLayers.Items.Insert(index - 1, item);
                LbLayers.SelectedItem = item;
            }
        }
        // Переместить слой вниз
        private void BtLayerDown_Click(object sender, RoutedEventArgs e)
        {
            if (LbLayers.SelectedIndex == -1) return;
            var item = LbLayers.SelectedItem;
            var index = LbLayers.Items.IndexOf(item);
            if (index < LbLayers.Items.Count - 1)
            {
                LbLayers.Items.Remove(item);
                LbLayers.Items.Insert(index + 1, item);
                LbLayers.SelectedItem = item;
            }
        }
        // Перевернуть список
        private void BtReverse_Click(object sender, RoutedEventArgs e)
        {
            var items = LbLayers.Items;
            for (int i = 0, j = items.Count - 1; i < j; i++, j--)
            {
                var tmpi = items[i];
                var tmpj = items[j];
                items.RemoveAt(j);
                items.RemoveAt(i);
                items.Insert(i, tmpj);
                items.Insert(j, tmpi);
            }
        }
        // Выбрать все
        private void BtSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox chk in LbLayers.Items)
                chk.IsChecked = true;
        }
        // Убрать выделение
        private void BtDeSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox chk in LbLayers.Items)
                chk.IsChecked = false;
        }
        // Инверсия
        private void BtInverse_Click(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox chk in LbLayers.Items)
                chk.IsChecked = !chk.IsChecked;
        }
        #endregion

    }
    // Оработчики событий
    public class DrawOrderByLayerEvents : IExtensionApplication
    {
        // Будем работать по принципу функции Автослои
        // При добавлении объекта запоминать его
        // При завершении команды перемещать слой

        // Глобальная переменная с наборами
        public ObjectIdCollection ObjCol;// = new ObjectIdCollection();
        // Переменная показывающая включена функция или нет
        public static bool DoblaIsEventOn;
        // Включение
        public void On()
        {
            ObjCol = new ObjectIdCollection();
            DoblaIsEventOn = true;
            AcApp.DocumentManager.MdiActiveDocument.Database.ObjectAppended += CallBack_ObjectAppended;
            AcApp.DocumentManager.MdiActiveDocument.CommandEnded += CallBack_CommandEnded;
            AcApp.DocumentManager.MdiActiveDocument.CommandCancelled += CallBack_CommandEnded;
            AcApp.DocumentManager.MdiActiveDocument.CommandFailed += CallBack_CommandEnded;
            AcApp.DocumentManager.DocumentActivated += DocumentManager_DocumentActivated;
        }
        // Отключение функции
        public void Off()
        {
            ObjCol = null;
            DoblaIsEventOn = false;
            AcApp.DocumentManager.MdiActiveDocument.Database.ObjectAppended -= CallBack_ObjectAppended;
            AcApp.DocumentManager.MdiActiveDocument.CommandEnded -= CallBack_CommandEnded;
            AcApp.DocumentManager.MdiActiveDocument.CommandCancelled -= CallBack_CommandEnded;
            AcApp.DocumentManager.MdiActiveDocument.CommandFailed -= CallBack_CommandEnded;
        }
        private void DocumentManager_DocumentCreated(object sender, DocumentCollectionEventArgs e)
        {
            AcApp.DocumentManager.MdiActiveDocument = e.Document;
            // Проверяем запись о состоянии режима "Авто"
            if (MpCadHelpers.HasXDataDictionary("MP_DOBLAuto"))
            {
                // Если такая запись существует, то проверяем состояние вкл/выкл
                var doblaStatus = MpCadHelpers.GetStringXData("MP_DOBLAuto");
                // Если состояние вкл
                if (doblaStatus.Equals("ON"))
                    On();
                else Off();
            }
        }
        // Установка переменной в нужное значение в случае смены активного чертежа
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void DocumentManager_DocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            DoblaIsEventOn = MpCadHelpers.GetStringXData("MP_DOBLAuto").Equals("ON");
        }

        // Обработка события добавления объекта в базу чертежа
        private void CallBack_ObjectAppended(object sender, ObjectEventArgs e)
        {
            if (DoblaIsEventOn)
            {
                var doc = AcApp.DocumentManager.MdiActiveDocument;
                var db = doc.Database;
                if (
                    e.DBObject.OwnerId == db.CurrentSpaceId &&
                    !e.DBObject.IsErased &&
                    e.DBObject.ObjectId != ObjectId.Null
                    )
                {
                    try
                    {
                        if (ObjCol == null) ObjCol = new ObjectIdCollection();
                        ObjCol.Add(e.DBObject.ObjectId);
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }
        // Обработка события завершения команды автокада
        public void CallBack_CommandEnded(object sender, CommandEventArgs e)
        {
            if (DoblaIsEventOn)
            {
                var doc = AcApp.DocumentManager.MdiActiveDocument;
                var db = doc.Database;
                var ed = doc.Editor;
                try
                {
                    // Исключаем из обработки команды:
                    if (e.GlobalCommandName.ToUpper() != "COPY" && // Копировать
                        e.GlobalCommandName.ToUpper() != "UNDO" && // Отменить
                        e.GlobalCommandName.ToUpper() != "ERASE" && // Стереть
                        e.GlobalCommandName.ToUpper() != "LAYOUT" && // Переход на лист
                        e.GlobalCommandName.ToUpper() != "MODEL" && // Переход на модель
                        e.GlobalCommandName.ToUpper() != "PASTECLIP" && // Вставить 
                        e.GlobalCommandName.ToUpper() != "PASTEBLOCK" && // Вставить как блок
                        e.GlobalCommandName.ToUpper() != "CUTCLIP" && // Вырезать
                        e.GlobalCommandName.ToUpper() != "MPMULTICOPY" && // Мультикопирование
                        e.GlobalCommandName.ToUpper() != "MPTXTNUMCOPY" && // Копирование с нумирацией
                        e.GlobalCommandName.ToUpper() != "EXPORTLAYOUT" // Экспорт листа в модель
                        )
                    {
                        if (ObjCol.Count > 0 & ObjCol != null)
                        {
                            using (doc.LockDocument())
                            {
                                using (var tr = db.TransactionManager.StartTransaction())
                                {
                                    foreach (ObjectId objId in ObjCol)
                                    {
                                        try
                                        {
                                            var ent = (Entity)tr.GetObject(objId, OpenMode.ForWrite);
                                            if (
                                                ent.OwnerId == db.CurrentSpaceId &&
                                                !ent.IsErased &&
                                                ent.ObjectId != ObjectId.Null
                                                )
                                            {
                                                var btr = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                                if (btr != null)
                                                {
                                                    var dot = tr.GetObject(btr.DrawOrderTableId, OpenMode.ForWrite) as DrawOrderTable;
                                                    var curLay = ent.Layer;
                                                    if (MpCadHelpers.GetStringXData("MP_DOBLAuto_up").Equals("ON"))
                                                    {
                                                        if (MpCadHelpers.GetStringXData("MP_DOBLAuto_up_layer").Equals(curLay))
                                                        {
                                                            dot?.MoveToTop(new ObjectIdCollection(new[] { ent.ObjectId }));
                                                            ed.WriteMessage("\nModPlus: Объект на слое: " + "\"" + curLay + "\" перемещен на передний план");
                                                        }
                                                    }
                                                    if (MpCadHelpers.GetStringXData("MP_DOBLAuto_down").Equals("ON"))
                                                    {
                                                        if (MpCadHelpers.GetStringXData("MP_DOBLAuto_down_layer").Equals(curLay))
                                                        {
                                                            dot?.MoveToBottom(new ObjectIdCollection(new[] { ent.ObjectId }));
                                                            ed.WriteMessage("\nModPlus: Объект на слое: " + "\"" + curLay + "\" перемещен на задний план");
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        catch
                                        {
                                            // ignored
                                        }
                                    } // foreach
                                    tr.Commit();
                                }
                            }
                        } // if
                        ObjCol.Clear();
                    }
                    else
                    {
                        ObjCol.Clear();
                    }
                }
                catch (Exception ex)
                {
                    MpExWin.Show(ex);
                }
            }
        }
        // Загрузка функции в автокад
        public void Initialize()
        {
            // Подписываемся на событие создания чертежа (оно же - открытие)
            AcApp.DocumentManager.DocumentCreated += DocumentManager_DocumentCreated;
        }

        public void Terminate()
        {
            // Ничего не нужно
        }
    }

    public class DrawOrderByLayerFunction
    {
        DrawOrderByLayer _drawOrderByLayer;

        [CommandMethod("ModPlus", "MpDrawOrderByLayer", CommandFlags.Modal)]
        public void StartFunction()
        {
            if (_drawOrderByLayer == null)
            {
                _drawOrderByLayer = new DrawOrderByLayer();
                _drawOrderByLayer.Closed += window_Closed;
            }
            if (_drawOrderByLayer.IsLoaded)
                _drawOrderByLayer.Activate();
            else
                AcApp.ShowModalWindow(
                    AcApp.MainWindow.Handle, _drawOrderByLayer);
        }
        void window_Closed(object sender, EventArgs e)
        {
            _drawOrderByLayer = null;
        }
    }
}
