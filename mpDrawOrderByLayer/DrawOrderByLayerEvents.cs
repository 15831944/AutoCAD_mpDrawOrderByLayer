namespace mpDrawOrderByLayer
{
    using Autodesk.AutoCAD.ApplicationServices;
    using Autodesk.AutoCAD.DatabaseServices;
    using Autodesk.AutoCAD.Runtime;
    using ModPlusAPI;
    using ModPlusAPI.Windows;
    using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

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
            Application.DocumentManager.DocumentCreated += DocumentManager_DocumentCreated;
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
            Application.DocumentManager.MdiActiveDocument.Database.ObjectAppended += CallBack_ObjectAppended;
            Application.DocumentManager.MdiActiveDocument.CommandEnded += CallBack_CommandEnded;
            Application.DocumentManager.MdiActiveDocument.CommandCancelled += CallBack_CommandEnded;
            Application.DocumentManager.MdiActiveDocument.CommandFailed += CallBack_CommandEnded;
            Application.DocumentManager.DocumentActivated += DocumentManager_DocumentActivated;
        }

        // Отключение функции
        public void Off()
        {
            ObjCol = null;
            DoblaIsEventOn = false;
            Application.DocumentManager.MdiActiveDocument.Database.ObjectAppended -= CallBack_ObjectAppended;
            Application.DocumentManager.MdiActiveDocument.CommandEnded -= CallBack_CommandEnded;
            Application.DocumentManager.MdiActiveDocument.CommandCancelled -= CallBack_CommandEnded;
            Application.DocumentManager.MdiActiveDocument.CommandFailed -= CallBack_CommandEnded;
        }

        private void DocumentManager_DocumentCreated(object sender, DocumentCollectionEventArgs e)
        {
            Application.DocumentManager.MdiActiveDocument = e.Document;

            // Проверяем запись о состоянии режима "Авто"
            if (ModPlus.Helpers.XDataHelpers.HasXDataDictionary("MP_DOBLAuto"))
            {
                // Если такая запись существует, то проверяем состояние вкл/выкл
                var doblaStatus = ModPlus.Helpers.XDataHelpers.GetStringXData("MP_DOBLAuto");

                // Если состояние вкл
                if (doblaStatus.Equals("ON"))
                    On();
                else
                    Off();
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
                var doc = Application.DocumentManager.MdiActiveDocument;
                var db = doc.Database;
                if (e.DBObject.OwnerId == db.CurrentSpaceId &&
                    !e.DBObject.IsErased &&
                    e.DBObject.ObjectId != ObjectId.Null)
                {
                    try
                    {
                        if (ObjCol == null)
                            ObjCol = new ObjectIdCollection();
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
                var doc = Application.DocumentManager.MdiActiveDocument;
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
                                    } 

                                    tr.Commit();
                                }
                            }
                        }

                        ObjCol?.Clear();
                    }
                    else
                    {
                        ObjCol?.Clear();
                    }
                }
                catch (Exception ex)
                {
                    ExceptionBox.Show(ex);
                }
            }
        }
    }
}