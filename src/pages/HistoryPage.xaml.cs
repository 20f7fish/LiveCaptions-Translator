﻿using System.Windows.Controls;
using System.Windows;
using Microsoft.Win32;
using Wpf.Ui.Controls;

using LiveCaptionsTranslator.utils;
using LiveCaptionsTranslator.models;

namespace LiveCaptionsTranslator
{
    public partial class HistoryPage : Page
    {
        int currentPage = 1;
        int maxPage = 1;
        int maxRow = 20;
        int searchPage = 1;
        string searching = string.Empty;

        public HistoryPage()
        {
            InitializeComponent();

            Loaded += async (s, e) => await LoadHistory();
            Unloaded += HistoryPage_Unloaded;

            HistoryMaxRow.SelectedIndex = App.Settings.HistoryMaxRow;
            Translator.TranslationLogged += OnTranslationLogged;
        }

        private async void OnTranslationLogged()
        {
            await LoadHistory();
        }

        private void HistoryPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Translator.TranslationLogged -= OnTranslationLogged;
            HistoryDataGrid.ItemsSource = null;
        }

        private async Task LoadHistory()
        {
            var data = await SQLiteHistoryLogger.LoadHistoryAsync(currentPage, maxRow, searching);
            List<TranslationHistoryEntry> history = data.Item1;

            maxPage = (data.Item2 > 0) ? data.Item2 : 1;

            await Dispatcher.InvokeAsync(() =>
            {
                HistoryDataGrid.ItemsSource = history;
                PageNumber.Text = currentPage.ToString() + "/" + maxPage.ToString();
            });
        }

        private async void PageDown(object sender, RoutedEventArgs e)
        {
            if (currentPage - 1 >= 1)
                currentPage--;
            await LoadHistory();
        }

        private async void PageUp(object sender, RoutedEventArgs e)
        {
            if (currentPage < maxPage)
                currentPage++;
            await LoadHistory();
        }

        private async void DeleteHistory(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Do you want to delete all history?",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
                DefaultButton = ContentDialogButton.Close,
                DialogHost = (Application.Current.MainWindow as MainWindow)?.DialogHostContainer,
                Padding = new Thickness(8, 4, 8, 8),
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                currentPage = 1;
                await SQLiteHistoryLogger.ClearHistory();
                await LoadHistory();
            }
        }

        private async void maxRow_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string text = (e.AddedItems[0] as ComboBoxItem).Tag as string;
            maxRow = Convert.ToInt32(text);
            App.Settings.HistoryMaxRow = HistoryMaxRow.SelectedIndex;

            await LoadHistory();

            if (currentPage > maxPage)
            {
                currentPage = maxPage;
                await LoadHistory();
            }
        }

        private async void ReloadLogs(object sender, RoutedEventArgs e)
        {
            await LoadHistory();
        }

        private async void ExportHistory(object ain, RoutedEventArgs e)
        {

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv|All file (*.*)|*.*",
                DefaultExt = ".csv",
                FileName = $"exported_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.csv",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    await SQLiteHistoryLogger.ExportToCsv(saveFileDialog.FileName);
                    ShowSnackbar("Saved Success", $"File saved to: {saveFileDialog.FileName}");
                }
                catch (Exception ex)
                {
                    ShowSnackbar("Save Failed", $"File saved faild:{ex.Message}");
                }
            }
        }

        private void ShowSnackbar(string title, string message, bool isError = false)
        {
            var snackbar = new Snackbar(SnackbarHost)
            {
                Title = title,
                Content = message,
                Appearance = isError ? ControlAppearance.Danger : ControlAppearance.Light,
                Timeout = TimeSpan.FromSeconds(2)
            };

            snackbar.Show();
        }

        private async void HistorySearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            string searchText = (sender as AutoSuggestBox)?.Text ?? "";

            // Clear search by Ctrl+A and Delete and Enter
            if (string.IsNullOrEmpty(searchText))
            {
                searching = string.Empty;
                currentPage = searchPage;
            }
            else // Submit search
            {
                if (string.IsNullOrEmpty(searching))
                {
                    searchPage = currentPage;
                }
                searching = (sender as AutoSuggestBox)?.Text;
                currentPage = 1;
            }
            await LoadHistory();
        }

        private async void HistorySearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // Press X to clear search box
            if (args.Reason == AutoSuggestionBoxTextChangeReason.ProgrammaticChange)
            {
                if (!string.IsNullOrEmpty(searching))
                {
                    searching = null;
                    currentPage = searchPage;
                    await LoadHistory();
                }
            }
        }
    }
}