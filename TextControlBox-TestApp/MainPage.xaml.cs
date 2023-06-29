using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using TextControlBox;
using TextControlBox.Helper;
using TextControlBox.Text;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace TextControlBox_TestApp
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;

            textbox.FontSize = 20;
            Load();

            /*TextControlBox.Design = new TextControlBoxDesign
            {
                Background = new SolidColorBrush(Color.FromArgb(255, 59, 46, 69)),
                CursorColor = Color.FromArgb(255, 255, 100, 255),
                LineHighlighterColor = Color.FromArgb(255, 79, 66, 89),
                LineNumberBackground = Color.FromArgb(255, 59, 46, 69),
                LineNumberColor = Color.FromArgb(255, 93, 2, 163),
                TextColor = Color.FromArgb(255, 144, 0, 255),
                SelectionColor = Color.FromArgb(100, 144, 0, 255)
            };*/
        }
        private void Load()
        {
            textbox.CodeLanguage = TextControlBox.TextControlBox.GetCodeLanguageFromId("C#");
            textbox.SyntaxHighlighting = true;
        }
        private IEnumerable<string> GenerateContent()
        {
            for (int i = 1000; i > 0; i--)
            {
                yield return "Line: " + i;
            }
        }

        private async void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            bool ControlKey = Window.Current.CoreWindow.GetKeyState(Windows.System.VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
            if (ControlKey && args.VirtualKey == Windows.System.VirtualKey.R)
            {
                textbox.LoadLines(GenerateContent());
            }
            if (ControlKey && args.VirtualKey == Windows.System.VirtualKey.E)
            {
                Load();
            }
            if (ControlKey && args.VirtualKey == Windows.System.VirtualKey.D)
            {
                textbox.RequestedTheme = textbox.RequestedTheme == ElementTheme.Dark ? 
                    ElementTheme.Light : textbox.RequestedTheme == ElementTheme.Default ? ElementTheme.Light : ElementTheme.Dark;
                
                //TextControlBox.DuplicateLine(TextControlBox.CurrentLineIndex);
            }
            if (ControlKey && args.VirtualKey == Windows.System.VirtualKey.L)
            {
                var text = await FileIO.ReadTextAsync(await ApplicationData.Current.LocalFolder.GetFileAsync("css.json"));
                Debug.WriteLine(text);
                var res = TextControlBox.TextControlBox.GetCodeLanguageFromJson(text);
                Debug.WriteLine("RESULT: " + res.Succeed);
                textbox.CodeLanguage = res.CodeLanguage;
                //textbox.ShowLineNumbers = !textbox.ShowLineNumbers;
                //TextControlBox.DuplicateLine(TextControlBox.CurrentLineIndex);
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
                    textbox.LoadLines(await FileIO.ReadLinesAsync(file));
                }
            }
            if(ControlKey && args.VirtualKey == Windows.System.VirtualKey.S)
            {
                FileSavePicker savepicker = new FileSavePicker();
                savepicker.SuggestedStartLocation = PickerLocationId.Desktop;
                savepicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt" });

                var file = await savepicker.PickSaveFileAsync();
                if (file != null)
                {
                    //await FileIO.WriteLinesAsync(file, GenerateContent());
                    await FileIO.WriteLinesAsync(file, textbox.Lines);
                }
            }
        }

        private void searchInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            textbox.BeginSearch(searchInput.Text, false, false);
        }

        private void SearchUp_Click(object sender, RoutedEventArgs e)
        {
            textbox.FindPrevious();
        }

        private void SearchDown_Click(object sender, RoutedEventArgs e)
        {
            textbox.FindNext();
        }
    }
}
