using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using TextControlBox_TestApp.TextControlBox;
using TextControlBox_TestApp.TextControlBox.Helper;
using TextControlBox_TestApp.TextControlBox.Languages;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using muxc = Microsoft.UI.Xaml.Controls;

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
            TextControlBox.SetText(GenerateContent());
            TextControlBox.FontSize = 42;
            //OpenFile();
        }

        private string GenerateContent()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 1; i < 10; i++)
            {
                sb.Append("Line" + i + (i == 9 ? "" : "\n"));
                //sb.Append("Line" + i + " Line" + (i+9) + (i == 9 ? "" : "\n"));
            }
            return sb.ToString();
        }

        private async void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            bool ControlKey = Window.Current.CoreWindow.GetKeyState(Windows.System.VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
            if(ControlKey && args.VirtualKey == Windows.System.VirtualKey.R)
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
