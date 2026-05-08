using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;

namespace UnifiedExplorer
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private double _iconSize = 80;
        public double IconSize
        {
            get => _iconSize;
            set { _iconSize = value; OnPropertyChanged(nameof(IconSize)); }
        }
        private ObservableCollection<FileSystemItem> _currentFiles = new ObservableCollection<FileSystemItem>();
        private ObservableCollection<TreeNode> _favorites = new ObservableCollection<TreeNode>();
        private ObservableCollection<TreeNode> _thisPC = new ObservableCollection<TreeNode>();
        
        private string _currentPath = "";
        private string _clipboardPath = "";
        private bool _isCutOperation = false;
        
        private GridViewColumnHeader _lastHeaderClicked = null;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;
        private bool _previewVisible = false;
        private char _lastSearchChar = '\0';

        private GridView _detailsView;
        private GridViewColumn _colName, _colDateMod, _colType, _colSize, _colCreation, _colDimensions;
        
        // Context Menu Items
        private MenuItem _menuColSizeFit;
        private MenuItem _menuColName, _menuColDateMod, _menuColType, _menuColSize, _menuColCreation, _menuColDimensions;

        public MainWindow()
        {
            InitializeComponent();
            FileListView.ItemsSource = _currentFiles;
            FavoritesList.ItemsSource = _favorites;
            ThisPCTree.ItemsSource = _thisPC;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetupContextMenu();
            ApplyLocalization();
            SetupDetailsView();
            LoadSidebar();
            ViewLargeIcons_Click(null, null); // Default to Large Icons view
        }

        private void SetupContextMenu()
        {
            _menuColSizeFit = new MenuItem();
            _menuColSizeFit.Click += MenuColSizeFit_Click;

            _menuColName = new MenuItem { IsCheckable = true, IsChecked = true };
            _menuColName.Checked += ColToggle_Changed; _menuColName.Unchecked += ColToggle_Changed;

            _menuColDateMod = new MenuItem { IsCheckable = true, IsChecked = true };
            _menuColDateMod.Checked += ColToggle_Changed; _menuColDateMod.Unchecked += ColToggle_Changed;

            _menuColType = new MenuItem { IsCheckable = true, IsChecked = true };
            _menuColType.Checked += ColToggle_Changed; _menuColType.Unchecked += ColToggle_Changed;

            _menuColSize = new MenuItem { IsCheckable = true, IsChecked = true };
            _menuColSize.Checked += ColToggle_Changed; _menuColSize.Unchecked += ColToggle_Changed;

            _menuColCreation = new MenuItem { IsCheckable = true, IsChecked = false };
            _menuColCreation.Checked += ColToggle_Changed; _menuColCreation.Unchecked += ColToggle_Changed;

            _menuColDimensions = new MenuItem { IsCheckable = true, IsChecked = false };
            _menuColDimensions.Checked += ColToggle_Changed; _menuColDimensions.Unchecked += ColToggle_Changed;

            var ctxMenu = new ContextMenu();
            ctxMenu.Opened += ColumnContextMenu_Opened;
            ctxMenu.Items.Add(_menuColSizeFit);
            ctxMenu.Items.Add(new Separator());
            ctxMenu.Items.Add(_menuColName);
            ctxMenu.Items.Add(_menuColDateMod);
            ctxMenu.Items.Add(_menuColType);
            ctxMenu.Items.Add(_menuColSize);
            ctxMenu.Items.Add(_menuColCreation);
            ctxMenu.Items.Add(_menuColDimensions);

            var style = new Style(typeof(GridViewColumnHeader));
            style.Setters.Add(new Setter(GridViewColumnHeader.ContextMenuProperty, ctxMenu));
            FileListView.Resources.Add(typeof(GridViewColumnHeader), style);
        }

        private void ApplyLocalization()
        {
            SearchTextBox.Text = LocalizationManager.GetString("Search");
            PreviewTypeLbl.Text = LocalizationManager.GetString("Type") + ":";
            PreviewSizeLbl.Text = LocalizationManager.GetString("Size") + ":";
            PreviewDateLbl.Text = LocalizationManager.GetString("DateModified") + ":";
            
            _menuColSizeFit.Header = LocalizationManager.GetString("SizeAllColumns");
            _menuColName.Header = LocalizationManager.GetString("Name");
            _menuColDateMod.Header = LocalizationManager.GetString("DateModified");
            _menuColType.Header = LocalizationManager.GetString("Type");
            _menuColSize.Header = LocalizationManager.GetString("Size");
            _menuColCreation.Header = LocalizationManager.GetString("CreationDate");
            _menuColDimensions.Header = LocalizationManager.GetString("Dimensions");
        }

        private void SetupDetailsView()
        {
            _detailsView = new GridView();
            
            var nameTemplate = new DataTemplate();
            var stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
            stackPanelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            var iconFactory = new FrameworkElementFactory(typeof(TextBlock));
            iconFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Icon"));
            iconFactory.SetBinding(TextBlock.ForegroundProperty, new System.Windows.Data.Binding("IconColor"));
            iconFactory.SetValue(TextBlock.FontFamilyProperty, FindResource("FluentIcons"));
            iconFactory.SetValue(TextBlock.MarginProperty, new Thickness(0,0,8,0));
            iconFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            var textFactory = new FrameworkElementFactory(typeof(TextBlock));
            textFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Name"));
            textFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            stackPanelFactory.AppendChild(iconFactory);
            stackPanelFactory.AppendChild(textFactory);
            nameTemplate.VisualTree = stackPanelFactory;

            _colName = new GridViewColumn { Header = LocalizationManager.GetString("Name"), CellTemplate = nameTemplate, Width = 300 };
            _colDateMod = new GridViewColumn { Header = LocalizationManager.GetString("DateModified"), DisplayMemberBinding = new System.Windows.Data.Binding("DateModifiedStr"), Width = 150 };
            _colType = new GridViewColumn { Header = LocalizationManager.GetString("Type"), DisplayMemberBinding = new System.Windows.Data.Binding("Type"), Width = 120 };
            _colSize = new GridViewColumn { Header = LocalizationManager.GetString("Size"), DisplayMemberBinding = new System.Windows.Data.Binding("Size"), Width = 100 };
            _colCreation = new GridViewColumn { Header = LocalizationManager.GetString("CreationDate"), DisplayMemberBinding = new System.Windows.Data.Binding("CreationDateStr"), Width = 0 }; 
            _colDimensions = new GridViewColumn { Header = LocalizationManager.GetString("Dimensions"), DisplayMemberBinding = new System.Windows.Data.Binding("Dimensions"), Width = 0 }; 

            _detailsView.Columns.Add(_colName);
            _detailsView.Columns.Add(_colDateMod);
            _detailsView.Columns.Add(_colType);
            _detailsView.Columns.Add(_colSize);
            _detailsView.Columns.Add(_colCreation);
            _detailsView.Columns.Add(_colDimensions);

            FileListView.View = _detailsView;
        }

        private void LoadSidebar()
        {
            _favorites.Clear();
            _thisPC.Clear();

            AddSpecialFolder(_favorites, Environment.SpecialFolder.Desktop, LocalizationManager.GetString("Desktop"), "\xE869");
            AddSpecialFolder(_favorites, Environment.SpecialFolder.UserProfile, LocalizationManager.GetString("Downloads"), "\xE896", "Downloads");
            AddSpecialFolder(_favorites, Environment.SpecialFolder.MyDocuments, LocalizationManager.GetString("Documents"), "\xE8A5");
            AddSpecialFolder(_favorites, Environment.SpecialFolder.MyPictures, LocalizationManager.GetString("Pictures"), "\xE8B9");
            AddSpecialFolder(_favorites, Environment.SpecialFolder.MyMusic, LocalizationManager.GetString("Music"), "\xE8D6");
            AddSpecialFolder(_favorites, Environment.SpecialFolder.MyVideos, LocalizationManager.GetString("Videos"), "\xE714");

            var thisPCNode = new TreeNode("This PC", LocalizationManager.GetString("ThisPC"), "\xE8A6", "#808080");
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    string icon = drive.DriveType == DriveType.CDRom ? "\xE958" : drive.DriveType == DriveType.Network ? "\xE839" : "\xEDA2";
                    var node = new TreeNode(drive.RootDirectory.FullName, drive.Name, icon, "#808080");
                    node.Children.Add(new TreeNode("", LocalizationManager.GetString("Loading"), ""));
                    thisPCNode.Children.Add(node);
                }
            }
            _thisPC.Add(thisPCNode);
        }

        private void AddSpecialFolder(ObservableCollection<TreeNode> collection, Environment.SpecialFolder folder, string name, string icon, string subfolder = "")
        {
            string path = Environment.GetFolderPath(folder);
            if (!string.IsNullOrEmpty(subfolder)) path = Path.Combine(path, subfolder);
            if (Directory.Exists(path)) collection.Add(new TreeNode(path, name, icon, "#E8A11C"));
        }

        private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem item && item.DataContext is TreeNode node)
            {
                if (node.Children.Count > 0 && node.Children[0].Name == LocalizationManager.GetString("Loading"))
                {
                    node.Children.Clear();
                    try
                    {
                        foreach (var dir in Directory.GetDirectories(node.Path))
                        {
                            var info = new DirectoryInfo(dir);
                            if (!info.Attributes.HasFlag(FileAttributes.Hidden))
                            {
                                var subNode = new TreeNode(dir, info.Name, "\xE8B7", "#E8A11C");
                                subNode.Children.Add(new TreeNode("", LocalizationManager.GetString("Loading"), ""));
                                node.Children.Add(subNode);
                            }
                        }
                    } catch { }
                }
            }
        }

        private void FavoritesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FavoritesList.SelectedItem is TreeNode node && !string.IsNullOrEmpty(node.Path))
            {
                ThisPCTree.SelectedItemChanged -= ThisPCTree_SelectedItemChanged;
                if (ThisPCTree.ItemContainerGenerator.ContainerFromIndex(0) is TreeViewItem tvi) tvi.IsSelected = false;
                ThisPCTree.SelectedItemChanged += ThisPCTree_SelectedItemChanged;
                LoadDirectory(node.Path);
            }
        }

        private void ThisPCTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeNode node && !string.IsNullOrEmpty(node.Path) && node.Path != "This PC")
            {
                FavoritesList.SelectionChanged -= FavoritesList_SelectionChanged;
                FavoritesList.SelectedIndex = -1;
                FavoritesList.SelectionChanged += FavoritesList_SelectionChanged;
                LoadDirectory(node.Path);
            }
        }

        private void LoadDirectory(string path)
        {
            if (string.IsNullOrEmpty(path) || path == "This PC") return;

            try
            {
                _currentFiles.Clear();
                _currentPath = path;
                PathTextBox.Text = path;
                _lastSearchChar = '\0';

                var dirInfo = new DirectoryInfo(path);

                foreach (var dir in dirInfo.GetDirectories())
                {
                    if (!dir.Attributes.HasFlag(FileAttributes.Hidden))
                    {
                        _currentFiles.Add(new FileSystemItem
                        {
                            Name = dir.Name,
                            Path = dir.FullName,
                            IsDirectory = true,
                            DateModified = dir.LastWriteTime,
                            CreationDate = dir.CreationTime,
                            Type = LocalizationManager.GetString("FileFolder"),
                            Size = "",
                            SizeBytes = 0,
                            Icon = "\xE8B7",
                            IconColor = "#E8A11C",
                            Dimensions = ""
                        });
                    }
                }

                foreach (var file in dirInfo.GetFiles())
                {
                    if (!file.Attributes.HasFlag(FileAttributes.Hidden))
                    {
                        string ext = file.Extension.ToLower();
                        string dimensions = "";
                        
                        if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".gif" || ext == ".bmp")
                        {
                            try
                            {
                                using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                                {
                                    var decoder = BitmapDecoder.Create(fs, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                                    if (decoder.Frames.Count > 0) dimensions = $"{decoder.Frames[0].PixelWidth} x {decoder.Frames[0].PixelHeight}";
                                }
                            } catch { }
                        }

                        var newFileItem = new FileSystemItem
                        {
                            Name = file.Name,
                            Path = file.FullName,
                            IsDirectory = false,
                            DateModified = file.LastWriteTime,
                            CreationDate = file.CreationTime,
                            Type = file.Extension + " File",
                            Size = FormatSize(file.Length),
                            SizeBytes = file.Length,
                            Icon = GetIconForFile(ext),
                            IconColor = "#8A8A8A",
                            Dimensions = dimensions
                        };
                        _currentFiles.Add(newFileItem);

                        if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".gif" || ext == ".bmp")
                        {
                            _ = Task.Run(() =>
                            {
                                try
                                {
                                    using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                                    {
                                        var bitmap = new BitmapImage();
                                        bitmap.BeginInit();
                                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                        bitmap.DecodePixelWidth = 300;
                                        bitmap.StreamSource = fs;
                                        bitmap.EndInit();
                                        bitmap.Freeze();
                                        Dispatcher.InvokeAsync(() => { newFileItem.Thumbnail = bitmap; });
                                    }
                                } catch { }
                            });
                        }
                    }
                }
                
                Sort("Name", ListSortDirection.Ascending);
                ItemCountLbl.Content = $"{_currentFiles.Count} items";
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error"); }
        }

        private string GetIconForFile(string ext)
        {
            switch (ext)
            {
                case ".txt": return "\xE8D5";
                case ".jpg": case ".png": case ".jpeg": case ".gif": return "\xE8B9";
                case ".mp4": case ".avi": case ".mkv": return "\xE714";
                case ".mp3": case ".wav": return "\xE8D6";
                case ".zip": case ".rar": case ".7z": return "\xE8A5";
                case ".exe": return "\xE8A5";
                default: return "\xE8A5";
            }
        }

        private string FormatSize(long bytes)
        {
            if (bytes == 0) return "0 KB";
            double kb = bytes / 1024.0;
            return $"{Math.Ceiling(kb)} KB";
        }

        // View Toggles
        private void ViewDetails_Click(object sender, RoutedEventArgs e)
        {
            FileListView.View = _detailsView;
            FileListView.ItemTemplate = null;
            
            var factory = new FrameworkElementFactory(typeof(VirtualizingStackPanel));
            var template = new ItemsPanelTemplate(factory);
            FileListView.ItemsPanel = template;
            
            ScrollViewer.SetHorizontalScrollBarVisibility(FileListView, ScrollBarVisibility.Auto);
        }

        private void ViewLargeIcons_Click(object sender, RoutedEventArgs e)
        {
            FileListView.View = null; 
            
            var dataTemplate = new DataTemplate();
            var stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
            stackPanelFactory.SetBinding(StackPanel.WidthProperty, new System.Windows.Data.Binding("IconSize") { Source = this, Converter = new SizeAdditionConverter(), ConverterParameter = 20.0 });
            stackPanelFactory.SetValue(StackPanel.MarginProperty, new Thickness(5));
            stackPanelFactory.SetValue(StackPanel.BackgroundProperty, Brushes.Transparent);
            
            var gridFactory = new FrameworkElementFactory(typeof(Grid));
            gridFactory.SetBinding(Grid.WidthProperty, new System.Windows.Data.Binding("IconSize") { Source = this });
            gridFactory.SetBinding(Grid.HeightProperty, new System.Windows.Data.Binding("IconSize") { Source = this });
            gridFactory.SetValue(Grid.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            gridFactory.SetValue(Grid.MarginProperty, new Thickness(0, 0, 0, 5));

            var iconFactory = new FrameworkElementFactory(typeof(TextBlock));
            iconFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Icon"));
            iconFactory.SetBinding(TextBlock.ForegroundProperty, new System.Windows.Data.Binding("IconColor"));
            iconFactory.SetValue(TextBlock.FontFamilyProperty, FindResource("FluentIcons"));
            iconFactory.SetBinding(TextBlock.FontSizeProperty, new System.Windows.Data.Binding("IconSize") { Source = this, Converter = new SizeMultiplierConverter(), ConverterParameter = 0.6 });
            iconFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            iconFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            iconFactory.SetBinding(TextBlock.VisibilityProperty, new System.Windows.Data.Binding("IconVisibility"));
            
            var imgFactory = new FrameworkElementFactory(typeof(Image));
            imgFactory.SetBinding(Image.SourceProperty, new System.Windows.Data.Binding("Thumbnail"));
            imgFactory.SetValue(Image.StretchProperty, Stretch.Uniform);
            imgFactory.SetValue(Image.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            imgFactory.SetValue(Image.VerticalAlignmentProperty, VerticalAlignment.Center);
            imgFactory.SetBinding(Image.VisibilityProperty, new System.Windows.Data.Binding("ThumbnailVisibility"));

            gridFactory.AppendChild(iconFactory);
            gridFactory.AppendChild(imgFactory);
            
            var textFactory = new FrameworkElementFactory(typeof(TextBlock));
            textFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Name"));
            textFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            textFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
            textFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
            textFactory.SetValue(TextBlock.MaxHeightProperty, 40.0);
            textFactory.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);

            stackPanelFactory.AppendChild(gridFactory);
            stackPanelFactory.AppendChild(textFactory);
            dataTemplate.VisualTree = stackPanelFactory;
            FileListView.ItemTemplate = dataTemplate;

            var panelFactory = new FrameworkElementFactory(typeof(WrapPanel));
            var itemsTemplate = new ItemsPanelTemplate(panelFactory);
            FileListView.ItemsPanel = itemsTemplate;

            ScrollViewer.SetHorizontalScrollBarVisibility(FileListView, ScrollBarVisibility.Disabled);
        }

        private void FileListView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                double newSize = IconSize + (e.Delta > 0 ? 20 : -20);
                if (newSize < 80) newSize = 80;
                if (newSize > 300) newSize = 300;
                IconSize = newSize;
            }
        }

        private void FileListView_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Text)) return;
            
            char pressedChar = char.ToLower(e.Text[0]);
            if (!char.IsLetterOrDigit(pressedChar) && !char.IsPunctuation(pressedChar) && !char.IsSymbol(pressedChar)) return;

            if (_currentFiles == null || _currentFiles.Count == 0) return;

            int startIndex = 0;
            int currentSelectedIndex = FileListView.SelectedIndex;

            if (currentSelectedIndex >= 0)
            {
                startIndex = currentSelectedIndex + 1;
            }

            _lastSearchChar = pressedChar;

            for (int i = 0; i < _currentFiles.Count; i++)
            {
                int index = (startIndex + i) % _currentFiles.Count;
                var item = _currentFiles[index];
                if (item.Name.StartsWith(pressedChar.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    FileListView.SelectedItem = item;
                    FileListView.ScrollIntoView(item);
                    if (FileListView.ItemContainerGenerator.ContainerFromIndex(index) is ListViewItem lvi)
                    {
                        lvi.Focus();
                    }
                    e.Handled = true;
                    return;
                }
            }
        }

        // Column Context Menu Logic
        private void ColumnContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (_colName != null) _menuColName.IsChecked = _colName.Width > 0;
            if (_colDateMod != null) _menuColDateMod.IsChecked = _colDateMod.Width > 0;
            if (_colType != null) _menuColType.IsChecked = _colType.Width > 0;
            if (_colSize != null) _menuColSize.IsChecked = _colSize.Width > 0;
            if (_colCreation != null) _menuColCreation.IsChecked = _colCreation.Width > 0;
            if (_colDimensions != null) _menuColDimensions.IsChecked = _colDimensions.Width > 0;
        }

        private void ColToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item)
            {
                if (item == _menuColName && _colName != null) _colName.Width = item.IsChecked ? 300 : 0;
                else if (item == _menuColDateMod && _colDateMod != null) _colDateMod.Width = item.IsChecked ? 150 : 0;
                else if (item == _menuColType && _colType != null) _colType.Width = item.IsChecked ? 120 : 0;
                else if (item == _menuColSize && _colSize != null) _colSize.Width = item.IsChecked ? 100 : 0;
                else if (item == _menuColCreation && _colCreation != null) _colCreation.Width = item.IsChecked ? 150 : 0;
                else if (item == _menuColDimensions && _colDimensions != null) _colDimensions.Width = item.IsChecked ? 100 : 0;
            }
        }

        private void MenuColSizeFit_Click(object sender, RoutedEventArgs e)
        {
            if (_detailsView == null) return;
            foreach (var col in _detailsView.Columns)
            {
                if (col.Width > 0)
                {
                    col.Width = double.NaN;
                }
            }
        }

        // Toolbar / Nav Bar Events
        private void Up_Click(object sender, RoutedEventArgs e) { if (!string.IsNullOrEmpty(_currentPath)) { DirectoryInfo parent = Directory.GetParent(_currentPath); if (parent != null) LoadDirectory(parent.FullName); } }
        private void Refresh_Click(object sender, RoutedEventArgs e) { LoadDirectory(_currentPath); }
        private void PathTextBox_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) LoadDirectory(PathTextBox.Text); }
        
        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            string searchStr = LocalizationManager.GetString("Search");
            if (SearchTextBox.Text == searchStr) { SearchTextBox.Text = ""; SearchTextBox.Foreground = new SolidColorBrush(Colors.Black); }
        }
        
        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text)) { SearchTextBox.Text = LocalizationManager.GetString("Search"); SearchTextBox.Foreground = new SolidColorBrush(Colors.Gray); }
        }

        private void TogglePreview_Click(object sender, RoutedEventArgs e)
        {
            _previewVisible = !_previewVisible;
            if (_previewVisible) { PreviewColumn.Width = new GridLength(250); PreviewSplitter.Visibility = Visibility.Visible; PreviewPanelBorder.Visibility = Visibility.Visible; }
            else { PreviewColumn.Width = new GridLength(0); PreviewSplitter.Visibility = Visibility.Collapsed; PreviewPanelBorder.Visibility = Visibility.Collapsed; }
        }

        private void FileListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FileListView.SelectedItem is FileSystemItem item) { if (item.IsDirectory) LoadDirectory(item.Path); else Process.Start(new ProcessStartInfo(item.Path) { UseShellExecute = true }); }
        }

        private void FileListView_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is GridViewColumnHeader headerClicked && headerClicked.Role != GridViewColumnHeaderRole.Padding && headerClicked.Column != null)
            {
                ListSortDirection direction = ListSortDirection.Ascending;
                if (headerClicked == _lastHeaderClicked) direction = _lastDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;

                string sortBy = headerClicked.Column.Header as string;
                Sort(sortBy, direction);
                _lastHeaderClicked = headerClicked;
                _lastDirection = direction;
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            var sortedList = _currentFiles.ToList();
            int sign = direction == ListSortDirection.Ascending ? 1 : -1;

            sortedList.Sort((x, y) =>
            {
                if (sortBy == LocalizationManager.GetString("Name")) return sign * string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
                if (sortBy == LocalizationManager.GetString("DateModified")) return sign * DateTime.Compare(x.DateModified, y.DateModified);
                if (sortBy == LocalizationManager.GetString("CreationDate")) return sign * DateTime.Compare(x.CreationDate, y.CreationDate);
                if (sortBy == LocalizationManager.GetString("Type")) return sign * string.Compare(x.Type, y.Type, StringComparison.OrdinalIgnoreCase);
                if (sortBy == LocalizationManager.GetString("Size")) return sign * x.SizeBytes.CompareTo(y.SizeBytes);
                return 0;
            });

            _currentFiles.Clear();
            foreach (var item in sortedList) _currentFiles.Add(item);
        }

        // Drag Drop & Context Logic
        private void FileListView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && FileListView.SelectedItem is FileSystemItem item && item.IsDirectory)
                DragDrop.DoDragDrop(FileListView, item.Path, DragDropEffects.Link);
        }

        private void FavoritesList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                string path = (string)e.Data.GetData(DataFormats.StringFormat);
                if (Directory.Exists(path) && !_favorites.Any(f => f.Path == path))
                    _favorites.Add(new TreeNode(path, new DirectoryInfo(path).Name, "\xE8B7", "#E8A11C"));
            }
        }

        private void Unpin_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is TreeNode node)
            {
                if (MessageBox.Show($"Remove '{node.Name}' from Quick access?", "Unpin", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    _favorites.Remove(node);
            }
        }

        private void MenuOpen_Click(object sender, RoutedEventArgs e) { FileListView_MouseDoubleClick(null, null); }
        private void MenuCut_Click(object sender, RoutedEventArgs e) { if (FileListView.SelectedItem is FileSystemItem item) { _clipboardPath = item.Path; _isCutOperation = true; } }
        private void MenuCopy_Click(object sender, RoutedEventArgs e) { if (FileListView.SelectedItem is FileSystemItem item) { _clipboardPath = item.Path; _isCutOperation = false; } }
        private void MenuPaste_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_clipboardPath) || string.IsNullOrEmpty(_currentPath)) return;
            try
            {
                string destPath = Path.Combine(_currentPath, Path.GetFileName(_clipboardPath));
                if (File.Exists(_clipboardPath)) { if (_isCutOperation) File.Move(_clipboardPath, destPath); else File.Copy(_clipboardPath, destPath, false); }
                else if (Directory.Exists(_clipboardPath)) { if (_isCutOperation) Directory.Move(_clipboardPath, destPath); else MessageBox.Show("Deep folder copy not fully implemented."); }
                LoadDirectory(_currentPath);
            } catch (Exception ex) { MessageBox.Show(ex.Message, "Error"); }
        }
        private void MenuRename_Click(object sender, RoutedEventArgs e)
        {
            if (FileListView.SelectedItem is FileSystemItem item)
            {
                var input = InputBox.Show("Enter new name:", "Rename", item.Name);
                if (!string.IsNullOrWhiteSpace(input) && input != item.Name)
                {
                    try { string newPath = Path.Combine(_currentPath, input); if (item.IsDirectory) Directory.Move(item.Path, newPath); else File.Move(item.Path, newPath); LoadDirectory(_currentPath); }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error"); }
                }
            }
        }
        private void MenuDelete_Click(object sender, RoutedEventArgs e)
        {
            if (FileListView.SelectedItem is FileSystemItem item)
            {
                if (MessageBox.Show($"Delete {item.Name}?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try { if (item.IsDirectory) Directory.Delete(item.Path, true); else File.Delete(item.Path); LoadDirectory(_currentPath); }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error"); }
                }
            }
        }
        private void MenuNewFolder_Click(object sender, RoutedEventArgs e)
        {
            var input = InputBox.Show("Enter folder name:", "New Folder", "New folder");
            if (!string.IsNullOrWhiteSpace(input))
            {
                try { Directory.CreateDirectory(Path.Combine(_currentPath, input)); LoadDirectory(_currentPath); }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Error"); }
            }
        }
        private void MenuNewFile_Click(object sender, RoutedEventArgs e)
        {
            var input = InputBox.Show("Enter file name:", "New File", "New Document.txt");
            if (!string.IsNullOrWhiteSpace(input))
            {
                try { File.Create(Path.Combine(_currentPath, input)).Close(); LoadDirectory(_currentPath); }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Error"); }
            }
        }
    }

    public static class InputBox
    {
        public static string Show(string prompt, string title, string defaultValue = "")
        {
            Window window = new Window() { Title = title, Width = 300, Height = 150, WindowStartupLocation = WindowStartupLocation.CenterScreen, ResizeMode = ResizeMode.NoResize };
            StackPanel stack = new StackPanel() { Margin = new Thickness(10) };
            TextBlock label = new TextBlock() { Text = prompt, Margin = new Thickness(0, 0, 0, 5) };
            TextBox textBox = new TextBox() { Text = defaultValue };
            Button okButton = new Button() { Content = "OK", Width = 70, Margin = new Thickness(0, 15, 0, 0), HorizontalAlignment = HorizontalAlignment.Right };
            okButton.Click += (sender, e) => { window.DialogResult = true; window.Close(); };
            window.Loaded += (sender, e) => { textBox.Focus(); textBox.SelectAll(); };
            stack.Children.Add(label); stack.Children.Add(textBox); stack.Children.Add(okButton);
            window.Content = stack;
            return window.ShowDialog() == true ? textBox.Text : string.Empty;
        }
    }

    public class TreeNode
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string IconColor { get; set; }
        public ObservableCollection<TreeNode> Children { get; set; }
        public TreeNode(string path, string name, string icon = "\xE8B7", string iconColor = "#E8A11C")
        {
            Path = path; Name = name; Icon = icon; IconColor = iconColor; Children = new ObservableCollection<TreeNode>();
        }
    }

    public class FileSystemItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsDirectory { get; set; }
        public DateTime DateModified { get; set; }
        public string DateModifiedStr => DateModified.ToString("g");
        public DateTime CreationDate { get; set; }
        public string CreationDateStr => CreationDate.ToString("g");
        public string Type { get; set; }
        public string Size { get; set; }
        public long SizeBytes { get; set; }
        public string Dimensions { get; set; }
        public string Icon { get; set; }
        public string IconColor { get; set; }

        private ImageSource _thumbnail;
        public ImageSource Thumbnail
        {
            get => _thumbnail;
            set
            {
                _thumbnail = value;
                OnPropertyChanged(nameof(Thumbnail));
                OnPropertyChanged(nameof(ThumbnailVisibility));
                OnPropertyChanged(nameof(IconVisibility));
            }
        }
        
        public Visibility ThumbnailVisibility => Thumbnail != null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IconVisibility => Thumbnail == null ? Visibility.Visible : Visibility.Collapsed;
    }

    public class SizeMultiplierConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double val && double.TryParse(parameter?.ToString(), out double mult)) return val * mult;
            return 48.0;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }

    public class SizeAdditionConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double v && double.TryParse(parameter?.ToString(), out double add)) return v + add;
            return 100.0;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }
}
