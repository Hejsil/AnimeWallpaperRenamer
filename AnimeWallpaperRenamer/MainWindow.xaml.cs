using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using AnimeWallpaperRenamer.Annotations;
using static System.Windows.Forms.DialogResult;
using ListViewItem = System.Windows.Controls.ListViewItem;

namespace AnimeWallpaperRenamer
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private readonly ObservableCollection<string> _categories = new ObservableCollection<string>();
        private readonly FolderBrowserDialog _fromFolderBrowserDialog = new FolderBrowserDialog();
        private readonly FolderBrowserDialog _toFolderBrowserDialog = new FolderBrowserDialog();
        private ICollectionView _categoriesView;
        private Stack<string> _imagesToMove;
        private string _fromPath;
        private string _imagePath;

        private string _toPath;

        public MainWindow()
        {
            InitializeComponent();
            CategoriesView = new ListCollectionView(_categories);
        }

        public string ToPath
        {
            get { return _toPath; }
            set
            {
                _toPath = value;
                OnPropertyChanged();
            }
        }

        public string FromPath
        {
            get { return _fromPath; }
            set
            {
                _fromPath = value;
                OnPropertyChanged();
            }
        }

        public string ImagePath
        {
            get { return _imagePath; }
            set
            {
                _imagePath = value;
                OnPropertyChanged();
            }
        }

        public ICollectionView CategoriesView
        {
            get { return _categoriesView; }
            set
            {
                _categoriesView = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OpenToPath_OnClick(object sender, RoutedEventArgs e)
        {
            var result = _toFolderBrowserDialog.ShowDialog();

            if (result == OK && !string.IsNullOrWhiteSpace(_toFolderBrowserDialog.SelectedPath))
            {
                ToPath = _toFolderBrowserDialog.SelectedPath;
                _categories.Clear();

                foreach (var file in Directory.GetFiles(ToPath))
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    var regexResult = Regex.Match(Path.GetFileNameWithoutExtension(file), "^(.*) [0-9]*$");

                    if (!regexResult.Success)
                        continue;

                    var name = regexResult.Groups[1].Value;

                    if (!_categories.Contains(name))
                        _categories.Add(name);
                }
            }
        }

        private void OpenFromPath_OnClick(object sender, RoutedEventArgs e)
        {
            var result = _fromFolderBrowserDialog.ShowDialog();

            if (result == OK && !string.IsNullOrWhiteSpace(_fromFolderBrowserDialog.SelectedPath))
            {
                FromPath = _fromFolderBrowserDialog.SelectedPath;
                _imagesToMove = new Stack<string>(Directory.GetFiles(FromPath));
                ImagePath = _imagesToMove != null && _imagesToMove.Count != 0 ? _imagesToMove.Pop() : null;
            }
        }

        private void Category_OnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!Directory.Exists(ToPath))
                return;
            if (!Directory.Exists(FromPath))
                return;
            if (!File.Exists(ImagePath))
                return;

            var category = ((ListViewItem)sender).Content as string;
            if (category == null)
                return;

            string moveToPath;
            var id = 0;
            do
            {
                id++;
                moveToPath = $@"{ToPath}\{category} {id}{Path.GetExtension(ImagePath)}";
            } while (File.Exists(moveToPath));
            
            ImagePath = _imagesToMove != null && _imagesToMove.Count != 0 ? _imagesToMove.Pop() : null;
            File.Move(ImagePath, moveToPath);
        }

        private void AddButton_OnClick(object sender, RoutedEventArgs e)
        {
            var text = NewCategoryTextBox.Text;
            if (string.IsNullOrWhiteSpace(text))
                return;

            foreach (var chr in Path.GetInvalidFileNameChars())
                if (text.Contains(chr))
                    return;

            if (!_categories.Contains(text))
                _categories.Add(text);
        }

        private void TextBoxBase_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(FilterTextBox.Text))
                CategoriesView.Filter = null;
            else
                CategoriesView.Filter = o =>
                {
                    var category = o as string;
                    return category != null && Regex.IsMatch(category, FilterTextBox.Text);
                };
        }
    }
}