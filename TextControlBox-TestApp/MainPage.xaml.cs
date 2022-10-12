using System;
using System.Diagnostics;
using System.Text;
using TextControlBox.Helper;
using Windows.Storage;
using Windows.Storage.Pickers;
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
            TextControlBox.LoadText(GenerateContent());
            TextControlBox.FontSize = 42;
            //GC.Collect();
        }
        
        private string GenerateContent()
        {
            int Limit = 10;
            StringBuilder sb = new StringBuilder();
            for (int i = 1; i < Limit; i++)
            {
                sb.Append("Line" + i + (i == Limit - 1 ? "" : "\n"));
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
            if(ControlKey && args.VirtualKey == Windows.System.VirtualKey.D)
            {
                TextControlBox.DuplicateLine(TextControlBox.CurrentLineIndex);
            }
            if (ControlKey && args.VirtualKey == Windows.System.VirtualKey.O)
            {
                FileOpenPicker openPicker = new FileOpenPicker();
                openPicker.ViewMode = PickerViewMode.Thumbnail;
                openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
                openPicker.FileTypeFilter.Add("*");

                var file = await openPicker.PickSingleFileAsync();
                if(file != null)
                {
                    string text = await FileIO.ReadTextAsync(file);
                    TextControlBox.LoadText(text);
                }
            }
        }
    }
}
