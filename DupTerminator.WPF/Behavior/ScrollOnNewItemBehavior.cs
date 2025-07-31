using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace DupTerminator.WPF.Behavior
{
    public class ScrollOnNewItemBehavior : Behavior<DataGrid>
    {
        protected override void OnAttached()
        {
            AssociatedObject.Loaded += OnLoaded;
            AssociatedObject.Unloaded += OnUnLoaded;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Loaded -= OnLoaded;
            AssociatedObject.Unloaded -= OnUnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var incc = AssociatedObject.ItemsSource as INotifyCollectionChanged;
            if (incc == null) return;

            incc.CollectionChanged += OnCollectionChanged;
        }

        private void OnUnLoaded(object sender, RoutedEventArgs e)
        {
            var incc = AssociatedObject.ItemsSource as INotifyCollectionChanged;
            if (incc == null) return;

            incc.CollectionChanged -= OnCollectionChanged;
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                int count = AssociatedObject.Items.Count;
                if (count == 0)
                    return;

                var item = AssociatedObject.Items[count - 1];

                if (AssociatedObject is DataGrid)
                {
                    DataGrid grid = (DataGrid)AssociatedObject;
                    grid.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        grid.UpdateLayout();
                        grid.ScrollIntoView(item, null);
                    }));
                }
            }
        }
    }
}
