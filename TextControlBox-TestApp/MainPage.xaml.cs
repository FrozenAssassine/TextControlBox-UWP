using System;
using System.Diagnostics;
using System.Text;
using TextControlBox.Helper;
//using TextControlBox.Helper;
//using TextControlBox.Text;
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
            TextControlBox.SelectionChanged += TextControlBox_SelectionChanged;
        }
        
        private void TextControlBox_SelectionChanged(TextControlBox.TextControlBox sender, TextControlBox.Text.SelectionChangedEventHandler args)
        {
            Debug.WriteLine("Selection: " + args.SelectionStartIndex + "::" + args.SelectionLength + "::" + TextControlBox.CharacterCount);
        }

        private string GenerateContent()
        {
            int Limit = 140;
            StringBuilder sb = new StringBuilder();
            for (int i = 1; i < Limit; i++)
            {
                sb.Append("Line" + i + (i == Limit - 1 ? "" : "\n"));
            }
            return sb.ToString();
        }

        private void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
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
            if (ControlKey && args.VirtualKey == Windows.System.VirtualKey.Q)
            {
                TextControlBox.SetSelection(100, 1500);
            }
        }

        private async void OpenFile()
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("csharp.txt", CreationCollisionOption.OpenIfExists);
            string text = await FileIO.ReadTextAsync(file);
            //TextControlBox.SetText(text);
        }
    }
}
