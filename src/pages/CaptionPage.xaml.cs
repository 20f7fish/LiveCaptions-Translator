﻿using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace LiveCaptionsTranslator
{
    public partial class CaptionPage : Page
    {
        public static CaptionPage Current;
        public CaptionPage()
        {
            InitializeComponent();
            DataContext = App.Captions;
            Current = this;

            Loaded += (s, e) => App.Captions.PropertyChanged += TranslatedChanged;
            Unloaded += (s, e) => App.Captions.PropertyChanged -= TranslatedChanged;

            CollapseTranslatedCaption(App.Settings.CaptionLogEnable);
        }

        private async void TextBlock_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock textBlock)
            {
                try
                {
                    Clipboard.SetText(textBlock.Text);
                    textBlock.ToolTip = "Copied!";
                }
                catch
                {
                    textBlock.ToolTip = "Error to Copy";
                }
                await Task.Delay(500);
                textBlock.ToolTip = "Click to Copy";
            }
        }

        private void TranslatedChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(App.Captions.DisplayTranslatedCaption))
            {
                if (Encoding.UTF8.GetByteCount(App.Captions.DisplayTranslatedCaption) >= 160)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.TranslatedCaption.FontSize = 15;
                    }), DispatcherPriority.Background);
                }
                else
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        this.TranslatedCaption.FontSize = 18;
                    }), DispatcherPriority.Background);
                }
            }
        }

        public void CollapseTranslatedCaption(bool collapse)
        {
            var converter = new GridLengthConverter();

            if (collapse)
            {
                TranslatedCaption_Row.Height = (GridLength)converter.ConvertFromString("Auto");
                CaptionLogCard.Visibility = Visibility.Visible;
            }
            else
            {
                TranslatedCaption_Row.Height = (GridLength)converter.ConvertFromString("*");
                CaptionLogCard.Visibility = Visibility.Collapsed;
            }
        }
    }
}
