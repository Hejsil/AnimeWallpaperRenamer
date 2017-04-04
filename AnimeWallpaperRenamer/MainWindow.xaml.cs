using System;
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
using System.Windows.Input;
using AnimeWallpaperRenamer.Annotations;
using WinForms = System.Windows.Forms;

namespace AnimeWallpaperRenamer
{
    public partial class MainWindow : INotifyPropertyChanged
    {
        private readonly ObservableCollection<string> _categories = new ObservableCollection<string>();
        private readonly WinForms.FolderBrowserDialog _fromFolderBrowserDialog = new WinForms.FolderBrowserDialog();
        private readonly WinForms.FolderBrowserDialog _toFolderBrowserDialog = new WinForms.FolderBrowserDialog();
        private ICollectionView _categoriesView;
        private Stack<string> _imagesToMove;
        private string _fromPath;
        private string _toPath;
        private string _imagePath;
        private string _newCategoryName;

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

        public string NewCategoryName
        {
            get { return _newCategoryName; }
            set
            {
                _newCategoryName = value;
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
            
            if (result != WinForms.DialogResult.OK || !Directory.Exists(_toFolderBrowserDialog.SelectedPath))
                return;

            ToPath = _toFolderBrowserDialog.SelectedPath;
            _categories.Clear();

            // The loop for finding all categories in a wallpaper folder
            foreach (var file in Directory.GetFiles(ToPath))
            {
                // Use regex to filter out the category in the file name
                var regexResult = Regex.Match(Path.GetFileNameWithoutExtension(file) ?? "", "^(.*) [0-9]*$");

                if (!regexResult.Success)
                    continue;

                var name = regexResult.Groups[1].Value;

                // Only add category, if it doesn't exist
                if (!_categories.Contains(name))
                    _categories.Add(name);
            }
        }

        private void OpenFromPath_OnClick(object sender, RoutedEventArgs e)
        {
            var result = _fromFolderBrowserDialog.ShowDialog();

            if (result != WinForms.DialogResult.OK || !Directory.Exists(_fromFolderBrowserDialog.SelectedPath))
                return;

            FromPath = _fromFolderBrowserDialog.SelectedPath;
            _imagesToMove = new Stack<string>(Directory.GetFiles(FromPath));

            // Get next image only if the stack is not null, and contains items
            ImagePath = _imagesToMove != null && _imagesToMove.Count != 0 ? _imagesToMove.Pop() : null;
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

            // TODO: Figure out a faster way of getting a unique filename, but which still tries two follow the sequence 1, 2, 3...
            string moveToPath;
            var id = 0;
            do
            {
                id++;

                // Construct path that the image should be renamed to
                moveToPath = $@"{ToPath}\{category} {id}{Path.GetExtension(ImagePath)}";
            } while (File.Exists(moveToPath));

            File.Move(ImagePath, moveToPath);

            // Get next image only if the stack is not null, and contains items
            ImagePath = _imagesToMove != null && _imagesToMove.Count != 0 ? _imagesToMove.Pop() : null;
        }

        private void AddButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewCategoryName))
                return;

            // Ensure that no char in the string is an invalid filename char
            if (NewCategoryName.Any(chr => Path.GetInvalidFileNameChars().Contains(chr)))
                return;

            if (!_categories.Contains(NewCategoryName))
                _categories.Add(NewCategoryName);
        }

        private void FilterTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var text = ((TextBox)sender).Text;

            if (string.IsNullOrEmpty(text))
                CategoriesView.Filter = null;
            else
                CategoriesView.Filter = o =>
                {
                    var category = o as string;
                    return category != null && Regex.IsMatch(category, text, RegexOptions.IgnoreCase);
                };
        }
    }
}