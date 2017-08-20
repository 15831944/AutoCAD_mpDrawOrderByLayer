using System.Collections.Generic;
using ModPlusAPI.Interfaces;

namespace mpDrawOrderByLayer
{
    public class Interface : IModPlusFunctionInterface
    {
        public SupportedProduct SupportedProduct => SupportedProduct.AutoCAD;
        public string Name => "mpDrawOrderByLayer";
        public string AvailProductExternalVersion => "2013";
        public string ClassName => string.Empty;
        public string LName => "Порядок по слою";
        public string Description => "Функция служит для изменения порядка прорисовки согласно слоям";
        public string Author => "Пекшев Александр aka Modis";
        public string Price => "0";
        public bool CanAddToRibbon => true;
        public string FullDescription => "Функция обрабатывает слои, отмеченные в окне галочками, изменяя порядок прорисовки согласно порядку отмеченных слоев. Присутствует режим «Авто», который автоматически меняет порядок прорисовки объектов на одном из двух указанных слоев при создании или редактировании";
        public string ToolTipHelpImage => string.Empty;
        public List<string> SubFunctionsNames => new List<string>();
        public List<string> SubFunctionsLames => new List<string>();
        public List<string> SubDescriptions => new List<string>();
        public List<string> SubFullDescriptions => new List<string>();
        public List<string> SubHelpImages => new List<string>();
        public List<string> SubClassNames => new List<string>();
    }
}
