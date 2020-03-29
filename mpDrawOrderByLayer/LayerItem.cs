namespace mpDrawOrderByLayer
{
    using ModPlusAPI.Mvvm;

    public class LayerItem : VmBase
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
    }
}