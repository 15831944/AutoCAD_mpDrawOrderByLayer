using mpPInterface;

namespace mpDrawOrderByLayer
{
    public class Interface : IPluginInterface
    {
        public string Name => "mpDrawOrderByLayer";
        public string AvailCad => "2018";
        public string LName => "Порядок по слою";
        public string Description => "Функция служит для изменения порядка прорисовки согласно слоям";
        public string Author => "Пекшев Александр aka Modis";
        public string Price => "0";
    }
}
