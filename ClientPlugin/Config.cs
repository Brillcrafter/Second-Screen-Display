using ClientPlugin.Settings;
using ClientPlugin.Settings.Elements;
using Sandbox.Graphics.GUI;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using VRage.Input;
using VRageMath;


namespace ClientPlugin
{
    public enum ExampleEnum
    {
        FirstAlpha,
        SecondBeta,
        ThirdGamma,
        AndTheDelta,
        Epsilon
    }

    public class Config : INotifyPropertyChanged
    {
        #region Options

        // TODO: Define your configuration options and their default values
        private bool toggle = true;
        private int integer = 2;
        private float number = 0.1f;
        private string baseFontSize = "10";
        private string secondWindowWidth = "1920";
        private string secondWindowHeight = "1080";
        private ExampleEnum dropdown = ExampleEnum.FirstAlpha;
        private Color color = Color.Cyan;
        private Color colorWithAlpha = new Color(0.8f, 0.6f, 0.2f, 0.5f);
        private Binding keybind = new Binding(MyKeys.None);

        #endregion

        #region User interface

        // TODO: Settings dialog title
        public readonly string Title = "Second Screen Display";

        // TODO: Settings dialog controls, one property for each configuration option

        [Checkbox(description: "Checkbox Tooltip")]
        public bool Toggle
        {
            get => toggle;
            set => SetField(ref toggle, value);
        }
        
        [Textbox(description: "Base Font Size")]
        public string BaseFontSize
        {
            get => baseFontSize;
            set => SetField(ref baseFontSize, value);
        }
        
        [Textbox(description: "Second Window Width")]
        public string SecondWindowWidth
        {
            get => secondWindowWidth;
            set => SetField(ref secondWindowWidth, value);
        }
        
        [Textbox(description: "Second Window Height")]
        public string SecondWindowHeight
        {
            get => secondWindowHeight;
            set => SetField(ref secondWindowHeight, value);
        }
        

        [Color(description: "RGB color")]
        public Color Color
        {
            get => color;
            set => SetField(ref color, value);
        }
        
        #endregion

        #region Property change notification bilerplate

        public static readonly Config Default = new Config();
        public static readonly Config Current = ConfigStorage.Load();

        public event PropertyChangedEventHandler PropertyChanged;

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