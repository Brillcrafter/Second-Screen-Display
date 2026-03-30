using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BrillcrafterSSD.Settings;
using BrillcrafterSSD.Settings.Elements;
using VRageMath;


namespace BrillcrafterSSD
{
    public class Config : INotifyPropertyChanged
    {
        #region Defaults
        
        private string _baseFontSize = "15";
        private string _secondWindowWidth = "1920";
        private string _secondWindowHeight = "1080";
        //private float _secondWindowTransparency;
        private Color _secondWindowBackgroundColor = Color.White;
        #endregion

        #region User interface
        
        public readonly string Title = "Second Screen Display";
        
        
        [Textbox(description: "Base Font Size")]
        public string BaseFontSize
        {
            get => _baseFontSize;
            set => SetField(ref _baseFontSize, value);
        }
        
        [Textbox(description: "Second Window Width")]
        public string SecondWindowWidth
        {
            get => _secondWindowWidth;
            set => SetField(ref _secondWindowWidth, value);
        }
        
        [Textbox(description: "Second Window Height")]
        public string SecondWindowHeight
        {
            get => _secondWindowHeight;
            set => SetField(ref _secondWindowHeight, value);
        }

        [Color(description: "Second Window Background Color")]
        public Color SecondWindowBackgroundColor
        {
            get => _secondWindowBackgroundColor;
            set => SetField(ref _secondWindowBackgroundColor, value);
        }
        
        /*[Slider(0f, 1f, 0.01f, SliderAttribute.SliderType.Float, description: "Second Window Transparency")]
        public float SecondWindowTransparency
        {
            get => _secondWindowTransparency;
            set => SetField(ref _secondWindowTransparency, value);
        }*/
        
        #endregion

        #region Property change notification bilerplate

        public static readonly Config Default = new Config();
        public static readonly Config Current = ConfigStorage.Load();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}
