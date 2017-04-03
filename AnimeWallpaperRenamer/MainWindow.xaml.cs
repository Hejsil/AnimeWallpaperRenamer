using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AnimeWallpaperRenamer.Annotations;
using Microsoft.Win32;
using static System.Windows.Forms.DialogResult;
using ListViewItem = System.Windows.Controls.ListViewItem;
using Path = System.IO.Path;

namespace AnimeWallpaperRenamer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly FolderBrowserDialog _toFolderBrowserDialog = new FolderBrowserDialog();
        private readonly FolderBrowserDialog _fromFolderBrowserDialog = new FolderBrowserDialog();

        private string _toPath;
        private string _fromPath;
        private string _newCategory;
        private ObservableCollection<string> _categories;
        private string _imagePath;
        private Stack<string> _imagesToMove;
        private ICollectionView _categoriesView;

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

        public string NewCategory
        {
            get { return _newCategory; }
            set
            {
                _newCategory = value;
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

        public ObservableCollection<string> Categories
        {
            get { return _categories; }
            set
            {
                _categories = value;
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

        public MainWindow()
        {
            InitializeComponent();
            Categories = new ObservableCollection<string>();
            CategoriesView = new ListCollectionView(Categories);
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

                var regex = new Regex("^(.*) ([0-9]*)$");
                Categories.Clear();

                foreach (var file in Directory.GetFiles(ToPath))
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    var regexResult = regex.Match(Path.GetFileNameWithoutExtension(file));

                    if (!regexResult.Success)
                        continue;

                    var name = regexResult.Groups[1].Value;
                    
                    if (!Categories.Contains(name))
                        Categories.Add(name);
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

            var from = ImagePath;
            ImagePath = _imagesToMove != null && _imagesToMove.Count != 0 ? _imagesToMove.Pop() : null;
            File.Move(from, moveToPath);
        }

        private void AddButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewCategory))
                return;

            foreach (var chr in Path.GetInvalidFileNameChars())
            {
                if (NewCategory.Contains(chr))
                    return;
            }

            if (Categories.Contains(NewCategory))
                Categories.Add(NewCategory);
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
