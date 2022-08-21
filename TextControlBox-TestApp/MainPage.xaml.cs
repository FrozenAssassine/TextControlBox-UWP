using System;
using System.Text;
using TextControlBox_TestApp.TextControlBox.Helper;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace TextControlBox_TestApp
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;

            //Set the Syntaxhighlighting-language to Csharp
            TextControlBox.CodeLanguage = CodeLanguages.Csharp;
            //Open a test file
            //OpenFile();
            TextControlBox.SetText(GenerateContent());
            TextControlBox.FontSize = 42;
        }

        private string GenerateContent()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 1; i < 40; i++)
            {
                sb.Append("Line" + i + (i == 39 ? "" : "\n"));
                //sb.Append("Line" + i + " Line" + (i+9) + (i == 9 ? "" : "\n"));
            }
            return sb.ToString();
        }

        private async void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            bool ControlKey = Window.Current.CoreWindow.GetKeyState(Windows.System.VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
            if (ControlKey && args.VirtualKey == Windows.System.VirtualKey.R)
            {
                TextControlBox.SetText(GenerateContent());
            }
        }

        private async void OpenFile()
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("csharp.txt", CreationCollisionOption.OpenIfExists);
            string text = await FileIO.ReadTextAsync(file);
            TextControlBox.SetText(text);
        }
    }
}
