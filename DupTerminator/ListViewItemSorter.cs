using System;
//using System.Text;
using System.Windows.Forms; //SortOrder
//using System.Collections; // IComparer
using System.Collections.Generic; //IComparer<ListViewGroup>

namespace DupTerminator
{
    /// <summary>
	/// This class is an implementation of the 'IComparer' interface.
	/// </summary>
    public class ListViewItemSorter : IComparer<ListViewItem>
    //public class ListViewItemSorter : IComparer
	{
		/// <summary>
		/// Specifies the column to be sorted
		/// </summary>
		private int ColumnToSort;
		/// <summary>
		/// Specifies the order in which to sort (i.e. 'Ascending').
		/// </summary>
		private SortOrder OrderOfSort;

		/// <summary>
		/// Class constructor.  Initializes various elements
		/// </summary>
		public ListViewItemSorter()
		{
			// Initialize the column to '0'
			ColumnToSort = 0;

			// Initialize the sort order to 'none'
			//OrderOfSort = SortOrder.None;
			OrderOfSort = SortOrder.Ascending;
		}

		/// <summary>
		/// This method is inherited from the IComparer interface.  It compares the two objects passed using a case insensitive comparison.
		/// </summary>
		/// <param name="x">First object to be compared</param>
		/// <param name="y">Second object to be compared</param>
		/// <returns>The result of the comparison. "0" if equal, negative if 'x' is less than 'y' and positive if 'x' is greater than 'y'</returns>
		//public int Compare(object x, object y)
        public int Compare(ListViewItem listviewX, ListViewItem listviewY)
        //public int Compare(object X, object Y)
		{
            int compareResult;

            /*ListViewItem listviewX = X as ListViewItem;
            ListViewItem listviewY = Y as ListViewItem;
		    compareResult = 0;
            int coun = Math.Min(listviewX.Group.Items.Count, listviewY.Group.Items.Count);
		    if (coun > 0)
		    {
		        for (int i = 0; i < coun; i++)
		        {
		            System.Windows.Forms.ListViewItem listItemX = listviewX.Group.Items[i];
		            System.Windows.Forms.ListViewItem listItemY = listviewY.Group.Items[i];

		            compareResult = String.Compare(listItemX.SubItems[ColumnToSort].Text,
		                                           listItemY.SubItems[ColumnToSort].Text);
		            if (compareResult != 0)
		            {
		                break;
		            }
		        }
		    }*/

		    // Compare the two items
            compareResult = String.Compare(listviewX.SubItems[ColumnToSort].Text, listviewY.SubItems[ColumnToSort].Text);

            // Calculate correct return value based on object comparison
            if (OrderOfSort == SortOrder.Ascending)
            {
                // Ascending sort is selected, return normal result of compare operation
                return compareResult;
            }
            else if (OrderOfSort == SortOrder.Descending)
            {
                // Descending sort is selected, return negative result of compare operation
                return (-compareResult);
            }
            else
            {
                // Return '0' to indicate they are equal
                return 0;
            }

		}
    
		/// <summary>
		/// Gets or sets the number of the column to which to apply the sorting operation (Defaults to '0').
		/// </summary>
		public int SortColumn
		{
			set
			{
				ColumnToSort = value;
			}
			get
			{
				return ColumnToSort;
			}
		}

		/// <summary>
		/// Gets or sets the order of sorting to apply (for example, 'Ascending' or 'Descending').
		/// </summary>
		public SortOrder Order
		{
			set
			{
				OrderOfSort = value;
			}
			get
			{
				return OrderOfSort;
			}
		}
        
	}
}
