﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using static System.Net.WebRequestMethods;
using Windows.UI.Core;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;
using Windows.Storage.Streams;
using Newtonsoft.Json.Linq;
using Windows.ApplicationModel.DataTransfer;

namespace TextControlBox_DemoApp.Views
{
    public sealed partial class MainPage : Page
    {
        private bool UnsavedChanges = false;
        private StorageFile OpenedFile = null;
        private CoreApplicationViewTitleBar coreTitleBar;
        private Encoding CurrentEncoding = Encoding.UTF8;

        public MainPage()
        {
            this.InitializeComponent();

            CustomTitleBar();
            UpdateTitle();

            Infobar_LineEnding.Text = textbox.LineEnding.ToString();
            textbox.Focus(FocusState.Programmatic);
        }

        private void ApplySettings()
        {
            textbox.FontFamily = new FontFamily(AppSettings.GetSettings("fontFamily") ?? "Consolas");
            textbox.FontSize = AppSettings.GetSettingsAsInt("fontSize", 18);

            if (Window.Current.Content is FrameworkElement rootElement)
            {
                textbox.RequestedTheme = rootElement.RequestedTheme = (ElementTheme)Enum.Parse(typeof(ElementTheme), AppSettings.GetSettingsAsInt("theme", 0).ToString());
            }
        }
        private void CustomTitleBar()
        {
            // Hide default title bar.
            coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

            // Set caption buttons background to transparent.
            ApplicationViewTitleBar titleBar =
                ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            // Set XAML element as a drag region.
            Window.Current.SetTitleBar(Titlebar);

            // Register a handler for when the size of the overlaid caption control changes.
            coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;

            // Register a handler for when the title bar visibility changes.
            // For example, when the title bar is invoked in full screen mode.
            coreTitleBar.IsVisibleChanged += CoreTitleBar_IsVisibleChanged;
        }
        private async Task<bool> CheckUnsavedChanges()
        {
            if (!UnsavedChanges)
                return false;

            var SaveDialog = new ContentDialog
            {
                Title = "Save file?",
                Content = "Would you like to save the file?",
                PrimaryButtonText = "Save",
                SecondaryButtonText = "Don't save",
                CloseButtonText = "Cancel",
            };
            var res = await SaveDialog.ShowAsync();
            if(res == ContentDialogResult.Primary)
                return !await SaveFile();
            else if (res == ContentDialogResult.None)
                return true;
            return false;
        }
        private void UpdateTitle()
        {
            if (OpenedFile == null)
                titleDisplay.Text = (UnsavedChanges ? "*" : "") + "Untitled.txt  - TCB Editor";
            else
                titleDisplay.Text = (UnsavedChanges ? "*" : "") + OpenedFile.Name + " - TCB Editor";
        }
        public async Task<(string Text, Encoding encoding, bool Succed)> ReadTextFromFileAsync(StorageFile file, Encoding encoding = null)
        {
            try
            {
                if (file == null)
                    return ("", Encoding.Default, false);

                using (var stream = (await file.OpenReadAsync()).AsStreamForRead())
                {
                    //Detect the encoding:
                    using (var reader = new StreamReader(stream, true))
                    {
                        byte[] buffer = new byte[stream.Length];
                        stream.Read(buffer, 0, buffer.Length);

                        reader.Read();
                        encoding = reader.CurrentEncoding;

                        Debug.WriteLine("TRY: " + encoding.EncodingName + " :" + encoding.GetString(buffer));

                        return (encoding.GetString(buffer, 0, buffer.Length), encoding, true);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return ("", Encoding.Default, false);
            }
        }
        public async Task<bool> WriteTextToFileAsync(StorageFile file, string text, Encoding encoding)
        {
            try
            {
                if (file == null)
                    return false;

                using (var stream = await file.OpenStreamForWriteAsync())
                {
                    using (var writer = new StreamWriter(stream, encoding))
                    {
                        await writer.WriteAsync(text);
                        return true;
                    }
                }
            }
            catch (IOException)
            {
                return await SaveFile(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }
        private async Task<bool> SaveFile(bool ForceSaveNew = false)
        {
            if (OpenedFile == null || ForceSaveNew)
            {
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt" });
                savePicker.SuggestedFileName = "Untitled.txt";
                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    CachedFileManager.DeferUpdates(file);
                    await WriteTextToFileAsync(file, textbox.GetText(), CurrentEncoding);
                    Windows.Storage.Provider.FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);

                    if (status == Windows.Storage.Provider.FileUpdateStatus.Complete)
                    {
                        OpenedFile = file;
                        UnsavedChanges = false;
                        UpdateTitle();
                        return true;
                    }
                }
            }
            else
            {
                var res = await WriteTextToFileAsync(OpenedFile, textbox.GetText(), CurrentEncoding);
                if(res == true)
                {
                    UnsavedChanges = false;
                    UpdateTitle();
                }
                return res;
            }
            return false;
        }
        private async Task OpenFile(StorageFile file)
        {
            if (file != null)
            {
                OpenedFile = file;
                UpdateTitle();
                var res = await ReadTextFromFileAsync(file);
                CurrentEncoding = res.encoding;
                Infobar_Encoding.Text = CurrentEncoding.EncodingName;

                textbox.LoadText(res.Text);
                Infobar_LineEnding.Text = textbox.LineEnding.ToString();
                UnsavedChanges = false;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ApplySettings();
            CustomTitleBar();
            base.OnNavigatedTo(e);
        }

        //Titlebar events:
        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            // Get the size of the caption controls and set padding.
            LeftPaddingColumn.Width = new GridLength(coreTitleBar.SystemOverlayLeftInset);
            RightPaddingColumn.Width = new GridLength(coreTitleBar.SystemOverlayRightInset);
        }
        private void CoreTitleBar_IsVisibleChanged(CoreApplicationViewTitleBar sender, object args)
        {
            Titlebar.Visibility = sender.IsVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void textbox_TextChanged(TextControlBox.TextControlBox sender, string Text)
        {
            UnsavedChanges = true;
            UpdateTitle();
        }

        private async void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (await CheckUnsavedChanges())
                return;

            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");

            await OpenFile(await picker.PickSingleFileAsync());
        }
        private async void NewFile_Click(object sender, RoutedEventArgs e)
        {
            if (await CheckUnsavedChanges())
                return;
            
            OpenedFile = null;
            UnsavedChanges = false;
            UpdateTitle();
            textbox.LoadText("");
        }
        private async void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            await SaveFile(false);
        }
        private async void SaveFileAs_Click(object sender, RoutedEventArgs e)
        {
            await SaveFile(true);
        }
        private async void ExitApp_Click(object sender, RoutedEventArgs e)
        {
            await ApplicationView.GetForCurrentView().TryConsolidateAsync();
        }
        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            textbox.Undo();
        }
        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            textbox.Redo();
        }
        private void Cut_Click(object sender, RoutedEventArgs e)
        {
            textbox.Cut();
        }
        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            textbox.Copy();
        }
        private void Paste_Click(object sender, RoutedEventArgs e)
        {
            textbox.Paste();
        }
        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            textbox.SelectAll();
        }
        private void Language_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item)
            {
                if (item.Tag.ToString() == "")
                    textbox.CodeLanguage = null;
                else 
                    textbox.SelectCodeLanguageById(item.Tag.ToString());
            }
        }
        private void DuplicateLine_Click(object sender, RoutedEventArgs e)
        {
            textbox.DuplicateLine(textbox.CurrentLineIndex);
        }
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SettingsPage));
        }

        private void textbox_ZoomChanged(TextControlBox.TextControlBox sender, int ZoomFactor)
        {
            Infobar_Zoom.Text = ZoomFactor + "%";
        }
        private void textbox_SelectionChanged(TextControlBox.TextControlBox sender, TextControlBox.Text.SelectionChangedEventHandler args)
        {
            Infobar_Cursor.Text = "Ln: " + (args.LineNumber + 1) + ", Col:" + args.CharacterPositionInLine;
        }

        private void Page_ActualThemeChanged(FrameworkElement sender, object args)
        {
            if (Window.Current.Content is FrameworkElement rootElement)
            {
                textbox.RequestedTheme = rootElement.RequestedTheme;
            }
        }

        private void TabMode_Click(object sender, RoutedEventArgs e)
        {
            if(sender is MenuFlyoutItem item)
            {
                textbox.UseSpacesInsteadTabs = item.Tag.ToString().Equals("0");
            }
        }

        private async void Page_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var files = await e.DataView.GetStorageItemsAsync();
                if (files.Count >= 1)
                {
                    if (await CheckUnsavedChanges())
                        return;

                    await OpenFile(files[0] as StorageFile);
                }
            }
        }

        private void Page_DragEnter(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }
    }
}
    