using ClientPlugin.Settings;
using ClientPlugin.Settings.Elements;
using Sandbox.Graphics.GUI;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace ClientPlugin
{
    public class Config : INotifyPropertyChanged
    {
        #region Options
        
        private string baseFontSize = "10";
        private string secondWindowWidth = "1920";
        private string secondWindowHeight = "1080";
        #endregion

        #region User interface
        
        public readonly string Title = "Second Screen Display";
        
        
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