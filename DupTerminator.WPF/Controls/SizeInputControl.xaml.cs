using System;
using System.Collections.Generic;
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
    /// Interaction logic for SizeInputControl.xaml
    /// </summary>
    public partial class SizeInputControl : UserControl
    {
        public enum SizeUnit { KB, MB, GB }

        public ulong? SkipLessThan
        {
            get => (ulong?)GetValue(SkipLessThanProperty);
            set => SetValue(SkipLessThanProperty, value);
        }

        public static readonly DependencyProperty SkipLessThanProperty =
            DependencyProperty.Register(
                "SkipLessThan",
                typeof(ulong?),
                typeof(SizeInputControl),
                new FrameworkPropertyMetadata(
                    (ulong?)0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSkipLessThanChanged));

        private SizeUnit _currentUnit = SizeUnit.KB;
        private bool _isInternalUpdate;

        public SizeInputControl()
        {
            InitializeComponent();
        }

        private static void OnSkipLessThanChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as SizeInputControl;
            if (control == null || control._isInternalUpdate) return;

            control.UpdateDisplayFromBytes(control.SkipLessThan);
        }

        private void UpdateDisplayFromBytes(ulong? bytes)
        {
            _isInternalUpdate = true;

            if (bytes == null)
            {
                ValueTextBox.Text = string.Empty;
            }
            else
            {
                switch (_currentUnit)
                {
                    case SizeUnit.KB:
                        ValueTextBox.Text = Math.Round(bytes.Value / 1024.0, 2).ToString();
                        break;
                    case SizeUnit.MB:
                        ValueTextBox.Text = Math.Round(bytes.Value / (1024.0 * 1024), 2).ToString();
                        break;
                    case SizeUnit.GB:
                        ValueTextBox.Text = Math.Round(bytes.Value / (1024.0 * 1024 * 1024), 2).ToString();
                        break;
                }
            }

            _isInternalUpdate = false;
        }

        private void UpdateBytesFromInput()
        {
            if (_isInternalUpdate || string.IsNullOrWhiteSpace(ValueTextBox.Text))
            {
                SkipLessThan = null;
                return;
            }

            if (!double.TryParse(ValueTextBox.Text, out double value))
                return;

            try
            {
                _isInternalUpdate = true;
                SkipLessThan = _currentUnit switch
                {
                    SizeUnit.KB => (ulong?)Math.Round(value * 1024),
                    SizeUnit.MB => (ulong?)Math.Round(value * 1024 * 1024),
                    SizeUnit.GB => (ulong?)Math.Round(value * 1024 * 1024 * 1024),
                    _ => null
                };
            }
            finally
            {
                _isInternalUpdate = false;
            }
        }

        private void UnitRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == KbRadio) _currentUnit = SizeUnit.KB;
            else if (sender == MbRadio) _currentUnit = SizeUnit.MB;
            else if (sender == GbRadio) _currentUnit = SizeUnit.GB;

            UpdateDisplayFromBytes(SkipLessThan);
        }

        private void ValueTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateBytesFromInput();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            string newText = textBox.Text.Insert(textBox.SelectionStart, e.Text);
            e.Handled = !double.TryParse(newText, out _);
        }
    }
}
