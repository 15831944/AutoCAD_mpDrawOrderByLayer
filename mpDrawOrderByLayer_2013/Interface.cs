using System.Diagnostics.CodeAnalysis;
using mpPInterface;

namespace mpDrawOrderByLayer
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Interface : IPluginInterface
    {
        private const string _Name = "mpDrawOrderByLayer";
        private const string _AvailCad = "2013";
        private const string _LName = "Порядок по слою";
        private const string _Description = "Функция служит для изменения порядка прорисовки согласно слоям";
        private const string _Author = "Пекшев Александр aka Modis";
        private const string _Price = "0";
        public string Name => _Name;
        public string AvailCad => _AvailCad;
        public string LName => _LName;
        public string Description => _Description;
        public string Author => _Author;
        public string Price => _Price;
    }
}
