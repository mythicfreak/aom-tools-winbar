using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;

namespace Aom.Tools.WinBar.Ui
{
    internal class ListViewColumnSorter : IComparer<ListViewItem>, IComparer
    {
        private readonly CaseInsensitiveComparer _objectComparer;

        public int SortColumn { get; set; }
        public SortOrder Order { get; set; }

        public ListViewColumnSorter()
        {
            SortColumn = 0;
            Order = SortOrder.None;
            _objectComparer = new CaseInsensitiveComparer();
        }

        public int Compare(ListViewItem x, ListViewItem y)
        {
            var compareResult = CompareInternal(x, y);
            var multiplier = Order == SortOrder.None
                ? 0
                : Order == SortOrder.Ascending ? 1 : -1;

            return multiplier * compareResult;
        }

        private int CompareInternal(ListViewItem x, ListViewItem y)
        {
            if (x.Tag.Equals("Folder") && y.Tag.Equals("Folder"))
            {
                return _objectComparer.Compare(x.Text, y.Text);
            }
            if (x.Tag.Equals("Folder"))
            {
                return Order == SortOrder.Ascending ? -1 : 1;
            }
            if (y.Tag.Equals("Folder"))
            {
                return Order == SortOrder.Ascending ? 1 : -1;
            }
            switch (SortColumn)
            {
                case 0: //Name
                {
                    return _objectComparer.Compare(x.SubItems[0].Text, y.SubItems[0].Text);
                }
                case 1: //Size
                {
                    var sizeTextX = x.SubItems[1].Text;
                    var sizeTextY = y.SubItems[1].Text;

                    try
                    {
                        var sizeX = double.Parse(sizeTextX, NumberStyles.Any);
                        var sizeY = double.Parse(sizeTextY, NumberStyles.Any);
                        return _objectComparer.Compare(sizeX, sizeY);
                    }
                    catch (Exception)
                    {
                        return -1;
                    }
                }
                default: //Date
                {
                    DateTime dateX, dateY;
                    DateTime.TryParse(x.SubItems[2].Text, out dateX);
                    DateTime.TryParse(y.SubItems[2].Text, out dateY);
                    return _objectComparer.Compare(dateX, dateY);
                }
            }
        }

        public int Compare(object x, object y) => Compare((ListViewItem)x, (ListViewItem)y);
    }
}
