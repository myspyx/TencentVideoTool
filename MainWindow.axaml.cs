using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using MessageBox.Avalonia;
using TencentCloud.Vod.V20180717.Models;

namespace TencentVideoTool
{
    public class TencentMediaInfo
    {
        public string MediaName { get; set; }
        public string SrtFilePath { get; set; }
        public bool HasSubtitle { get; set; }
        public string Message { get; set; }

        public MediaInfo MediaInfo { get; set; }
    }

    public partial class MainWindow : Window
    {
        private StackPanel _signInPanel;
        private TextBox _secretIdTextBox;
        private TextBox _secretKeyTextBox;
        private TextBox _subAppIdTextBox;

        private DockPanel _mainPanel;
        private ComboBox _categoryComboBox;
        private TextBox _srtPathTextBox;
        private StackPanel _mediaGrid;


        private TencentVodService _tencentVodService;
        private List<MediaClassInfo> _classInfos;
        private long? _currentClassId;
        private List<TencentMediaInfo> _tencentMediaInfos;


        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            _signInPanel = this.Find<StackPanel>("SignInPanel");
            _secretIdTextBox = this.Find<TextBox>("SecretIdTextBox");
            _secretKeyTextBox = this.Find<TextBox>("SecretKeyTextBox");
            _subAppIdTextBox = this.Find<TextBox>("SubAppIdTextBox");

            _mainPanel = this.Find<DockPanel>("MainPanel");
            _categoryComboBox = this.Find<ComboBox>("CategoryComboBox");
            _srtPathTextBox = this.Find<TextBox>("SrtPathTextBox");
            _mediaGrid = this.Find<StackPanel>("MediaGrid");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnSignInClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                _tencentVodService = new TencentVodService(_secretIdTextBox.Text.Trim(), _secretKeyTextBox.Text.Trim(), Convert.ToUInt64(_subAppIdTextBox.Text.Trim()));
                _classInfos = _tencentVodService.GetAllMediaClassInfos();
                var avaloniaList = new AvaloniaList<TextBlock>();
                foreach (var mediaClassInfo in _classInfos)
                {
                    avaloniaList.Add(new TextBlock() {Text = mediaClassInfo.ClassName});
                }

                _categoryComboBox.Items = avaloniaList;

                _signInPanel.IsEnabled = false;
                _mainPanel.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBoxManager.GetMessageBoxStandardWindow("Error", ex.Message).Show();
            }
        }

        private void OnLoadClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentClassId = _classInfos[_categoryComboBox.SelectedIndex].ClassId;

                _tencentMediaInfos = _tencentVodService.GetMediaInfos(_currentClassId).Select(mediaInfo =>
                {
                    var tencentMediaInfo = new TencentMediaInfo();
                    // name
                    tencentMediaInfo.MediaName = mediaInfo.BasicInfo.Name;
                    // srt name
                    var srtFileName = Path.Combine(_srtPathTextBox.Text ?? "", Path.ChangeExtension(tencentMediaInfo.MediaName, ".srt"));
                    if (File.Exists(srtFileName))
                    {
                        tencentMediaInfo.SrtFilePath = srtFileName;
                    }

                    // has subtitle
                    tencentMediaInfo.HasSubtitle = mediaInfo.SubtitleInfo?.SubtitleSet?.Any() ?? false;

                    tencentMediaInfo.MediaInfo = mediaInfo;
                    return tencentMediaInfo;
                }).ToList();
                
                this.RenderVideoGrid();
            }
            catch (Exception ex)
            {
                MessageBoxManager.GetMessageBoxStandardWindow("Error", ex.Message).Show();
            }
        }

        private void OnAddAllSubtitlesClicked(object sender, RoutedEventArgs e)
        {
            foreach (var tencentMediaInfo in _tencentMediaInfos)
            {
                if (string.IsNullOrWhiteSpace(tencentMediaInfo.SrtFilePath) || tencentMediaInfo.HasSubtitle) continue;

                try
                {
                    var srtSubTitle = File.ReadAllText(tencentMediaInfo.SrtFilePath, Encoding.UTF8);
                    var vttSubTitle = SubtitleConverter.ConvertSrtToVtt(srtSubTitle);

                    _tencentVodService.AddSubtitle(tencentMediaInfo.MediaInfo, vttSubTitle);
                    tencentMediaInfo.HasSubtitle = true;
                    tencentMediaInfo.Message = "";
                }
                catch (Exception ex)
                {
                    tencentMediaInfo.Message = ex.Message;
                }

                RenderVideoGrid();
            }
        }

        private void RenderVideoGrid()
        {
            // clear table
            _mediaGrid.Children.Clear();

            var i = 0;
            foreach (var tencentMediaInfo in _tencentMediaInfos)
            {
                i++;

                var rowWrapper = new Border()
                {
                    Classes = new Classes("media-info"),
                };

                var row = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                };

                // number
                row.Children.Add(new TextBlock() {Text = i.ToString(), TextWrapping = TextWrapping.WrapWithOverflow, Classes = new Classes("td", "xs")});

                // video name
                row.Children.Add(new TextBlock() {Text = tencentMediaInfo.MediaName, TextWrapping = TextWrapping.WrapWithOverflow, Classes = new Classes("td", "lg")});
                // srt name
                if (string.IsNullOrWhiteSpace(tencentMediaInfo.SrtFilePath))
                {
                    row.Children.Add(new TextBlock() {Text = "Not found", Classes = new Classes("td", "lg", "error")});
                }
                else
                {
                    row.Children.Add(new TextBlock() {Text = tencentMediaInfo.SrtFilePath, TextWrapping = TextWrapping.WrapWithOverflow, Classes = new Classes("td", "lg")});
                }

                // has subtitle
                if (tencentMediaInfo.HasSubtitle)
                {
                    row.Children.Add(new TextBlock() {Text = "Yes", Classes = new Classes("td", "sm", "pass")});
                }
                else
                {
                    row.Children.Add(new TextBlock() {Text = "No", Classes = new Classes("td", "sm")});
                }

                // actions
                if (tencentMediaInfo.HasSubtitle)
                {
                    var clearButton = new Button() {Content = "Clear subtitle", Classes = new Classes("sm")};
                    clearButton.Click += (sender, args) =>
                    {
                        try
                        {
                            _tencentVodService.ClearSubtitles(tencentMediaInfo.MediaInfo);
                            tencentMediaInfo.HasSubtitle = false;
                            tencentMediaInfo.Message = "";
                        }
                        catch (Exception ex)
                        {
                            tencentMediaInfo.Message = ex.Message;
                        }

                        RenderVideoGrid();
                    };
                    row.Children.Add(clearButton);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(tencentMediaInfo.SrtFilePath))
                    {
                        var addButton = new Button() {Content = "Add subtitle", Classes = new Classes("sm")};
                        addButton.Click += (sender, args) =>
                        {
                            try
                            {
                                var srtSubTitle = File.ReadAllText(tencentMediaInfo.SrtFilePath, Encoding.UTF8);
                                var vttSubTitle = SubtitleConverter.ConvertSrtToVtt(srtSubTitle);

                                _tencentVodService.AddSubtitle(tencentMediaInfo.MediaInfo, vttSubTitle);
                                tencentMediaInfo.HasSubtitle = true;
                                tencentMediaInfo.Message = "";
                            }
                            catch (Exception ex)
                            {
                                tencentMediaInfo.Message = ex.Message;
                            }

                            RenderVideoGrid();
                        };
                        row.Children.Add(addButton);
                    }
                }

                // message
                row.Children.Add(new TextBlock()
                {
                    Text = tencentMediaInfo.Message,
                    TextWrapping = TextWrapping.WrapWithOverflow,
                    Classes = new Classes("td", "lg", string.IsNullOrWhiteSpace(tencentMediaInfo.Message) ? "" : "error")
                });
                
                rowWrapper.Child = row;

                _mediaGrid.Children.Add(rowWrapper);
            }
        }
    }
}