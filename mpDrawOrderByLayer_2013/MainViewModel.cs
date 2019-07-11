using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using ModPlusAPI.Windows;
using Autodesk.AutoCAD.Runtime;
using ModPlusAPI;

namespace mpDrawOrderByLayer
{
    using System.Diagnostics;
    using ModPlusAPI.Mvvm;
    using Exception = System.Exception;

    public class MainViewModel : VmBase
    {
        private readonly string _dictionaryName = "MP_DOBLAuto";
        private readonly string _mpLayersposition = "MP_LayersPosition";

        private bool _autoMove;

        private LayerItem _downLayerName;

        private bool _downLayerWork;

        private bool _enableElements = true;

        private bool _isEnableLoadLayersPosition;

        private double _progressMaximum = 1;

        private double _progressValue;

        private LayerItem _upLayerName;

        private bool _upLayerWork;
        public DrawOrderByLayer ParentWindow;

        public MainViewModel()
        {
            Layers = new ObservableCollection<LayerItem>();
            FillLayers();
            CheckXData();
            CheckIsEnableLoadLayersPosition();
            AcApp.DocumentManager.MdiActiveDocument.Database.ObjectAppended += Database_ObjectAppended;
            AcApp.DocumentManager.MdiActiveDocument.Database.ObjectErased += Database_ObjectErased;
            AcApp.DocumentManager.MdiActiveDocument.Database.ObjectModified += Database_ObjectModified;
            AcApp.DocumentManager.DocumentCreated += DocumentManager_DocumentCreated;
            AcApp.DocumentManager.DocumentActivated += DocumentManager_DocumentActivated;
            // commands
            ReverseListCommand = new RelayCommand(ReverseList);
            SelectAllCommand = new RelayCommand(SelectAll);
            DeSelectAllCommand = new RelayCommand(DeSelectAll);
            InverseListCommand = new RelayCommand(InverseList);
            AcceptCommand = new RelayCommand(Accept);
            SaveLayersPositionCommand = new RelayCommand(SaveLayersPosition);
            LoadLayersPositionCommand = new RelayCommand(LoadLayersPosition);
        }

        public ObservableCollection<LayerItem> Layers { get; set; }

        /// <summary>Режим "Авто"</summary>
        public bool AutoMove
        {
            get => _autoMove;
            set
            {
                _autoMove = value;
                OnPropertyChanged();
                ModPlus.Helpers.XDataHelpers.SetStringXData(_dictionaryName, value ? "ON" : "OFF");
            }
        }

        public bool UpLayerWork
        {
            get => _upLayerWork;
            set
            {
                _upLayerWork = value;
                OnPropertyChanged();
                ModPlus.Helpers.XDataHelpers.SetStringXData("MP_DOBLAuto_up", value ? "ON" : "OFF");
            }
        }

        public bool DownLayerWork
        {
            get => _downLayerWork;
            set
            {
                _downLayerWork = value;
                OnPropertyChanged();
                ModPlus.Helpers.XDataHelpers.SetStringXData("MP_DOBLAuto_down", value ? "ON" : "OFF");
            }
        }

        public LayerItem UpLayerName
        {
            get => _upLayerName;
            set
            {
                _upLayerName = value;
                OnPropertyChanged();
                ModPlus.Helpers.XDataHelpers.SetStringXData("MP_DOBLAuto_up_layer", value.Name);
            }
        }

        public LayerItem DownLayerName
        {
            get => _downLayerName;
            set
            {
                _downLayerName = value;
                OnPropertyChanged();
                ModPlus.Helpers.XDataHelpers.SetStringXData("MP_DOBLAuto_down_layer", value.Name);
            }
        }

        public double ProgressValue
        {
            get => _progressValue;
            set
            {
                _progressValue = value;
                OnPropertyChanged();
            }
        }

        public double ProgressMaximum
        {
            get => _progressMaximum;
            set
            {
                _progressMaximum = value;
                OnPropertyChanged();
            }
        }

        public bool EnableElements
        {
            get => _enableElements;
            set
            {
                _enableElements = value;
                OnPropertyChanged();
            }
        }

        /// <summary>Доступность кнопки загрузки позиций слоев</summary>
        public bool IsEnableLoadLayersPosition
        {
            get => _isEnableLoadLayersPosition;
            set
            {
                if (Equals(value, _isEnableLoadLayersPosition)) return;
                _isEnableLoadLayersPosition = value;
                OnPropertyChanged();
            }
        }

        /// <summary>Заполнение списков слоев</summary>
        private void FillLayers()
        {
            Layers.Clear();
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
                            Layers.Add(new LayerItem { Name = acLyrTblRec.Name, Selected = false });
                    }
                }
            }
        }

        private void AddLayerToLists(SymbolTableRecord ltr)
        {
            try
            {
                var hasLayer = false;
                foreach (var item in Layers)
                {
                    if (item.Name != ltr.Name) continue;
                    hasLayer = true;
                    break;
                }

                if (!hasLayer)
                {
                    Layers.Add(new LayerItem { Name = ltr.Name });
                }
            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        // Проверка расширенных данных и установка начальных значений для режима "Авто"
        private void CheckXData()
        {
            // Проверяем запись о состоянии режима "Авто"
            if (ModPlus.Helpers.XDataHelpers.HasXDataDictionary(_dictionaryName))
            {
                // Если такая запись существует, то проверяем состояние вкл/выкл
                var doblaStatus = ModPlus.Helpers.XDataHelpers.GetStringXData(_dictionaryName);
                // Если состояние вкл
                AutoMove = doblaStatus.Equals("ON");
                // Независимо от состояния проверяем и выставляем слои
                // Слой "Вверх"
                if (ModPlus.Helpers.XDataHelpers.HasXDataDictionary("MP_DOBLAuto_up"))
                {
                    var doblaUp = ModPlus.Helpers.XDataHelpers.GetStringXData("MP_DOBLAuto_up");
                    UpLayerWork = doblaUp.Equals("ON");
                    var doblaUpLayer = Layers.FirstOrDefault(
                        l => l.Name == ModPlus.Helpers.XDataHelpers.GetStringXData("MP_DOBLAuto_up_layer"));
                    UpLayerName = doblaUpLayer ?? Layers[0];
                }
                else
                {
                    UpLayerWork = false;
                    UpLayerName = Layers[0];
                }

                // Слой "Вниз"
                if (ModPlus.Helpers.XDataHelpers.HasXDataDictionary("MP_DOBLAuto_down"))
                {
                    var doblaDown = ModPlus.Helpers.XDataHelpers.GetStringXData("MP_DOBLAuto_down");
                    DownLayerWork = doblaDown.Equals("ON");
                    var doblaDownLayer = Layers.FirstOrDefault(
                        l => l.Name == ModPlus.Helpers.XDataHelpers.GetStringXData("MP_DOBLAuto_down_layer"));
                    DownLayerName = doblaDownLayer ?? Layers[0];
                }
                else
                {
                    DownLayerWork = false;
                    DownLayerName = Layers[0];
                }
            }
            else
            {
                // Если такой записи нет, убираем галочку с режима "Авто"
                AutoMove = false;
                // а в списках слоев выбираем первый слой (Слой по умолчанию)
                UpLayerWork = false;
                UpLayerName = Layers[0];
                DownLayerWork = false;
                DownLayerName = Layers[0];
            }
        }

        private void CheckIsEnableLoadLayersPosition()
        {
            IsEnableLoadLayersPosition = ModPlus.Helpers.XDataHelpers.HasXDataDictionary(_mpLayersposition);
        }

        #region Commands

        public ICommand ReverseListCommand { get; set; }

        private void ReverseList(object o)
        {
            for (var i = 0; i < Layers.Count; i++)
                Layers.Move(Layers.Count - 1, i);
        }

        public ICommand SelectAllCommand { get; set; }

        private void SelectAll(object o)
        {
            foreach (var layerItem in Layers)
                layerItem.Selected = true;
        }

        public ICommand DeSelectAllCommand { get; set; }

        private void DeSelectAll(object o)
        {
            foreach (var layerItem in Layers)
                layerItem.Selected = false;
        }

        public ICommand InverseListCommand { get; set; }

        private void InverseList(object o)
        {
            foreach (var layerItem in Layers)
                layerItem.Selected = !layerItem.Selected;
        }

        public ICommand AcceptCommand { get; set; }

        private void Accept(object o)
        {
            // Сначала обрабатываем перемещение слоев из списка
            // Получаем список имен слоев, которые отмечены
            var layers = Layers.Where(item => item.Selected).ToList();
            ProgressMaximum = layers.Count;
            // Если список слоев не пуст
            if (layers.Count > 0)
            {
                try
                {
                    // Переворачиваем список
                    layers.Reverse();
                    var doc = AcApp.DocumentManager.MdiActiveDocument;
                    var db = doc.Database;
                    var ed = doc.Editor;
                    using (doc.LockDocument())
                    {
                        using (var tr = db.TransactionManager.StartTransaction())
                        {
                            var btr = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                            if (btr != null)
                            {
                                var dot = tr.GetObject(btr.DrawOrderTableId, OpenMode.ForWrite) as DrawOrderTable;
                                try
                                {
                                    EnableElements = false;
                                    System.Windows.Forms.Application.DoEvents();
                                    var index = 0;
                                    foreach (var lay in layers)
                                    {
                                        index++;
                                        ProgressValue = index;
                                        System.Windows.Forms.Application.DoEvents();

                                        var tvs = new[]
                                        {
                                            new TypedValue((int) DxfCode.LayerName, lay.Name),
                                        };
                                        var sf = new SelectionFilter(tvs);
                                        var psr = ed.SelectAll(sf);
                                        if (psr.Value != null)
                                        {
                                            var objectIdCollection = new ObjectIdCollection();
                                            foreach (var objectId in psr.Value.GetObjectIds())
                                            {
                                                var obj = tr.GetObject(objectId, OpenMode.ForRead);
                                                if (obj.OwnerId == db.CurrentSpaceId)
                                                    objectIdCollection.Add(objectId);
                                            }

                                            if (objectIdCollection.Count > 0)
                                                dot?.MoveToTop(objectIdCollection);
                                        }
                                    }
                                }
                                finally
                                {
                                    EnableElements = true;
                                    tr.Commit();
                                }
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    ExceptionBox.Show(exception);
                }
            }

            // Сохраняем значение вкл/выкл режима "Авто"
            if (AutoMove)
            {
                // Включаем обработчики событий
                var doev = new DrawOrderByLayerEvents();
                doev.On();
            }
            else
            {
                // Выключаем обработчики событий
                var doev = new DrawOrderByLayerEvents();
                doev.Off();
            }

            // Закрываем окно
            ParentWindow.Close();
        }

        public ICommand SaveLayersPositionCommand { get; set; }

        private void SaveLayersPosition(object o)
        {
            // Каждый слой буду хранить как отдельную запись, чтобы не превысить лимит

            for (var i = 0; i < Layers.Count; i++)
            {
                var layerItem = Layers[i];
                ModPlus.Helpers.XDataHelpers.SetStringXData($"{_mpLayersposition}_{layerItem.Name}", $"{i}_{layerItem.Selected}");
            }

            ModPlus.Helpers.XDataHelpers.SetStringXData(_mpLayersposition, "no matter");
            CheckIsEnableLoadLayersPosition();
        }

        public ICommand LoadLayersPositionCommand { get; set; }

        private void LoadLayersPosition(object o)
        {
            try
            {
                Dictionary<int, LayerItem> savedLayers = new Dictionary<int, LayerItem>();
                List<LayerItem> notSavedLayers = new List<LayerItem>();
                foreach (var layerItem in Layers)
                {
                    var key = $"{_mpLayersposition}_{layerItem.Name}";
                    var value = ModPlus.Helpers.XDataHelpers.GetStringXData(key);
                    bool hasSaved = false;
                    if (!string.IsNullOrEmpty(value))
                    {
                        var splitted = value.Split('_');
                        if (splitted.Length == 2)
                        {
                            if (int.TryParse(splitted[0], out int position) &&
                                bool.TryParse(splitted[1], out bool isChecked))
                            {
                                hasSaved = true;
                                layerItem.Selected = isChecked;
                                savedLayers.Add(position, layerItem);
                            }
                        }
                    }

                    if (!hasSaved)
                        notSavedLayers.Add(layerItem);
                }

                Layers = new ObservableCollection<LayerItem>();
                var orderedSavedLayers = savedLayers.OrderBy(i => i.Key).ToList();
                foreach (var keyValuePair in orderedSavedLayers)
                {
                    Layers.Add(keyValuePair.Value);
                }

                foreach (var notSavedLayer in notSavedLayers)
                {
                    Layers.Add(notSavedLayer);
                }

                OnPropertyChanged(nameof(Layers));
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        #endregion

        #region Acad events

        private void DocumentManager_DocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            FillLayers();
        }

        private void DocumentManager_DocumentCreated(object sender, DocumentCollectionEventArgs e)
        {
            FillLayers();
        }

        private void Database_ObjectAppended(object sender, ObjectEventArgs e)
        {
            if (e.DBObject is LayerTableRecord ltr)
            {
                AddLayerToLists(ltr);
            }
        }

        private void Database_ObjectErased(object sender, ObjectErasedEventArgs e)
        {
            try
            {
                if (e.DBObject is LayerTableRecord ltr)
                {
                    foreach (var item in Layers)
                    {
                        if (item.Name == ltr.Name)
                        {
                            Layers.Remove(item);
                            break;
                        }
                    }
                }
            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        private void Database_ObjectModified(object sender, ObjectEventArgs e)
        {
            try
            {
                if (e.DBObject is LayerTableRecord ltr)
                {
                    var doc = AcApp.DocumentManager.MdiActiveDocument;
                    var db = doc.Database;
                    var layersNames = new List<string>();
                    using (var tr = db.TransactionManager.StartOpenCloseTransaction())
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
                                    layersNames.Add(acLyrTblRec.Name);
                                }
                            }
                        }
                    }

                    for (var i = Layers.Count - 1; i >= 0; i--)
                    {
                        if (!layersNames.Contains(Layers[i].Name))
                            Layers.RemoveAt(i);
                    }

                    // add
                    AddLayerToLists(ltr);
                }
            }
            catch (System.Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        public void OnClosed()
        {
            try
            {
                AcApp.DocumentManager.MdiActiveDocument.Database.ObjectAppended -= Database_ObjectAppended;
                AcApp.DocumentManager.MdiActiveDocument.Database.ObjectErased -= Database_ObjectErased;
                AcApp.DocumentManager.MdiActiveDocument.Database.ObjectModified -= Database_ObjectModified;
                AcApp.DocumentManager.DocumentCreated -= DocumentManager_DocumentCreated;
                AcApp.DocumentManager.DocumentActivated -= DocumentManager_DocumentActivated;
            }
            catch
            {
                // ignored
            }
        }

        #endregion
    }

    // Оработчики событий
    public class DrawOrderByLayerEvents : IExtensionApplication
    {
        private const string LangItem = "mpDrawOrderByLayer";

        // Переменная показывающая включена функция или нет
        public static bool DoblaIsEventOn;
        // Будем работать по принципу функции Автослои
        // При добавлении объекта запоминать его
        // При завершении команды перемещать слой

        // Глобальная переменная с наборами
        public ObjectIdCollection ObjCol; // = new ObjectIdCollection();

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
            if (ModPlus.Helpers.XDataHelpers.HasXDataDictionary("MP_DOBLAuto"))
            {
                // Если такая запись существует, то проверяем состояние вкл/выкл
                var doblaStatus = ModPlus.Helpers.XDataHelpers.GetStringXData("MP_DOBLAuto");
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
            DoblaIsEventOn = ModPlus.Helpers.XDataHelpers.GetStringXData("MP_DOBLAuto").Equals("ON");
        }

        // Обработка события добавления объекта в базу чертежа
        private void CallBack_ObjectAppended(object sender, ObjectEventArgs e)
        {
            if (DoblaIsEventOn)
            {
                var doc = AcApp.DocumentManager.MdiActiveDocument;
                var db = doc.Database;
                if (e.DBObject.OwnerId == db.CurrentSpaceId &&
                    !e.DBObject.IsErased &&
                    e.DBObject.ObjectId != ObjectId.Null)
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
                        e.GlobalCommandName.ToUpper() != "EXPORTLAYOUT" && // Экспорт листа в модель
                        e.GlobalCommandName.ToUpper() != "EATTEDIT" && //редактирование атрибутов
                        e.GlobalCommandName.ToUpper() != "BEDIT" // редактирование блока
                    )
                    {
                        if (ObjCol != null && ObjCol.Count > 0)
                        {
                            using (doc.LockDocument())
                            {
                                using (var tr = db.TransactionManager.StartTransaction())
                                {
                                    foreach (ObjectId objId in ObjCol)
                                    {
                                        try
                                        {
                                            var ent = tr.GetObject(objId, OpenMode.ForWrite) as Entity;
                                            if (ent != null && ent.OwnerId == db.CurrentSpaceId && !ent.IsErased && ent.ObjectId != ObjectId.Null)
                                            {
                                                var btr = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                                                if (btr != null)
                                                {
                                                    var dot = tr.GetObject(btr.DrawOrderTableId, OpenMode.ForWrite) as DrawOrderTable;
                                                    var curLay = ent.Layer;
                                                    if (ModPlus.Helpers.XDataHelpers.GetStringXData("MP_DOBLAuto_up").Equals("ON"))
                                                    {
                                                        if (ModPlus.Helpers.XDataHelpers.GetStringXData("MP_DOBLAuto_up_layer").Equals(curLay))
                                                        {
                                                            dot?.MoveToTop(new ObjectIdCollection(new[] { ent.ObjectId }));
                                                            ed.WriteMessage("\n" + Language.GetItem(LangItem, "h10") +
                                                                            " " + "\"" + curLay + "\" " + Language.GetItem(LangItem, "h11"));
                                                        }
                                                    }

                                                    if (ModPlus.Helpers.XDataHelpers.GetStringXData("MP_DOBLAuto_down").Equals("ON"))
                                                    {
                                                        if (ModPlus.Helpers.XDataHelpers.GetStringXData("MP_DOBLAuto_down_layer").Equals(curLay))
                                                        {
                                                            dot?.MoveToBottom(new ObjectIdCollection(new[] { ent.ObjectId }));
                                                            ed.WriteMessage("\n" + Language.GetItem(LangItem, "h10") +
                                                                            " " + "\"" + curLay + "\" " + Language.GetItem(LangItem, "h12"));
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

                        ObjCol?.Clear();
                    }
                    else
                    {
                        ObjCol?.Clear();
                    }
                }
                catch (System.Exception ex)
                {
                    ExceptionBox.Show(ex);
                }
            }
        }
    }

    public class LayerItem : INotifyPropertyChanged
    {
        private bool _selected;
        public string Name { get; set; }

        public bool Selected
        {
            get => _selected;
            set
            {
                _selected = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}