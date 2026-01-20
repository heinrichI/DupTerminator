using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DupTerminator.WPF.Controls
{
    /// <summary>
    /// Interaction logic for FileExtensionsControl.xaml
    /// </summary>
    public partial class FileExtensionsControl : UserControl
    {
        // Dependency Property for the extensions collection
        public static readonly DependencyProperty ExtensionsProperty =
            DependencyProperty.Register("Extensions", typeof(ObservableCollection<string>),
                typeof(FileExtensionsControl), new PropertyMetadata(new ObservableCollection<string>()));

        public ObservableCollection<string> Extensions
        {
            get { return (ObservableCollection<string>)GetValue(ExtensionsProperty); }
            set { SetValue(ExtensionsProperty, value); }
        }

        public FileExtensionsControl()
        {
            InitializeComponent();
            //DataContext = this;
            AllowDrop = true;
            Drop += FileExtensionListUserControl_Drop;
            DragEnter += FileExtensionListUserControl_DragEnter;
        }

        private void AddExtension_Click(object sender, RoutedEventArgs e)
        {
            var extension = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter file extension (e.g., .txt):",
                "Add Extension",
                ".ext");

            if (IsValidExtension(extension) && !Extensions.Contains(extension))
            {
                Extensions.Add(extension);
            }
            else if (!string.IsNullOrWhiteSpace(extension))
            {
                MessageBox.Show("Please enter a valid file extension (e.g., .txt, .jpg)");
            }
        }

        private void EditExtension_Click(object sender, RoutedEventArgs e)
        {
            if (ExtensionsListBox.SelectedItem == null) return;

            var current = ExtensionsListBox.SelectedItem.ToString();
            var newExtension = Microsoft.VisualBasic.Interaction.InputBox(
                "Edit file extension:",
                "Edit Extension",
                current);

            if (!string.IsNullOrWhiteSpace(newExtension) && newExtension != current)
            {
                int index = Extensions.IndexOf(current);
                Extensions[index] = newExtension;
            }
        }

        private void RemoveExtension_Click(object sender, RoutedEventArgs e)
        {
            if (ExtensionsListBox.SelectedItem != null)
            {
                Extensions.Remove(ExtensionsListBox.SelectedItem.ToString());
            }
        }

        private bool IsValidExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension)) return false;
            if (!extension.StartsWith(".")) return false;
            if (extension.Length < 2) return false;

            // Check for valid characters (letters, numbers, dots)
            foreach (char c in extension)
            {
                if (!char.IsLetterOrDigit(c) && c != '.') return false;
            }

            return true;
        }

        private void FileExtensionListUserControl_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
        }

        private void FileExtensionListUserControl_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (string file in files)
                {
                    string extension = System.IO.Path.GetExtension(file);
                    if (!string.IsNullOrEmpty(extension))
                    {
                        // Add to the collection if not already present
                        if (!Extensions.Contains(extension))
                        {
                            Extensions.Add(extension);
                        }
                    }
                }
            }
        }
    }
}
