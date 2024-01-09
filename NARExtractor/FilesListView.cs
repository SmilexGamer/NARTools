// Copyright © 2010 "Da_FileServer"
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace Nexon.Extractor
{
    public partial class FilesListView : ListView
    {
        #region Events

        public event EventHandler<FilesEventArgs> ExtractFiles;
        public event EventHandler<FilesEventArgs> VerifyFiles;

        #endregion

        #region Constructors

        public FilesListView()
        {
            InitializeComponent();

            ColumnSort columnSort = new ColumnSort(this);
            columnSort.Column = -1;
            columnSort.Order = SortOrder.None;
            this.ListViewItemSorter = columnSort;
        }

        #endregion

        #region Properties

        public string FullPath
        {
            get;
            set;
        }

        public int SortColumn
        {
            get
            {
                ColumnSort columnSort = this.ListViewItemSorter as ColumnSort;
                if (columnSort != null && columnSort.Column >= 0 && columnSort.Column < this.Columns.Count)
                    return columnSort.Column;
                return -1;
            }
            set
            {
                ColumnSort columnSort = this.ListViewItemSorter as ColumnSort;
                if (columnSort != null)
                {
                    columnSort.Column = value;
                    Sort();
                }
            }
        }

        public SortOrder SortColumnOrder
        {
            get
            {
                ColumnSort columnSort = this.ListViewItemSorter as ColumnSort;
                if (columnSort != null && columnSort.Column >= 0 && columnSort.Column < this.Columns.Count)
                    return columnSort.Order;
                return SortOrder.None;
            }
            set
            {
                ColumnSort columnSort = this.ListViewItemSorter as ColumnSort;
                if (columnSort != null)
                {
                    columnSort.Order = value;
                    Sort();
                }
            }
        }

        #endregion

        #region Methods

        #region Event Callers

        protected void OnExtractFiles(FilesEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            if (ExtractFiles != null)
                ExtractFiles(this, e);
        }

        protected void OnVerifyFiles(FilesEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            if (VerifyFiles != null)
                VerifyFiles(this, e);
        }

        #endregion

        #region Event Handlers

        private void FilesListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            System.Diagnostics.Debug.Assert(e.Column >= 0);
            ColumnSort columnSorter = this.ListViewItemSorter as ColumnSort;
            if (columnSorter != null)
            {
                // Change the sorting column and/or order.
                if (columnSorter.Column != e.Column)
                {
                    columnSorter.Column = e.Column;
                    columnSorter.Order = SortOrder.Ascending;
                }
                else
                {
                    switch (columnSorter.Order)
                    {
                        case SortOrder.Ascending:
                            columnSorter.Order = SortOrder.Descending;
                            break;
                        case SortOrder.None:
                        case SortOrder.Descending:
                            columnSorter.Order = SortOrder.Ascending;
                            break;
                    }
                }

                // Set the sort icon for the column.
                SetSortIcon(columnSorter.Column, columnSorter.Order);

                // Resort the items.
                Sort();
            }
            else
            {
                // Set the sort icon for no columns.
                SetSortIcon(-1, SortOrder.None);
            }
        }

        private void contextMenuStrip_Opening(object sender, CancelEventArgs e)
        {
            if (this.SelectedIndices.Count <= 0)
                e.Cancel = true;
        }

        private void extractToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.SelectedIndices.Count > 0)
            {
                List<NexonArchiveFileEntry> files = new List<NexonArchiveFileEntry>();
                foreach (ListViewItem item in this.SelectedItems)
                {
                    NexonArchiveFileEntry file = item.Tag as NexonArchiveFileEntry;
                    if (file != null)
                        files.Add(file);
                }
                OnExtractFiles(new FilesEventArgs(this.FullPath, files));
            }
        }

        private void verifyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.SelectedIndices.Count > 0)
            {
                List<NexonArchiveFileEntry> files = new List<NexonArchiveFileEntry>();
                foreach (ListViewItem item in this.SelectedItems)
                {
                    NexonArchiveFileEntry file = item.Tag as NexonArchiveFileEntry;
                    if (file != null)
                        files.Add(file);
                }
                OnVerifyFiles(new FilesEventArgs(this.FullPath, files));
            }
        }

        #endregion

        protected override void CreateHandle()
        {
            base.CreateHandle();

            ColumnSort columnSorter = this.ListViewItemSorter as ColumnSort;
            if (columnSorter != null)
            {
                // Set the sort icon for the column.
                SetSortIcon(columnSorter.Column, columnSorter.Order);
            }
        }

        private static string GetHumanSize(long size)
        {
            if (size < 0)
                return string.Empty;

            // This is ugly, but it works...
            double temp = size;
            if (temp > 1024)
            {
                temp /= 1024;
                if (temp > 1024)
                {
                    temp /= 1024;
                    if (temp > 1024)
                    {
                        temp /= 1024;
                        return temp.ToString("n2", NumberFormatInfo.CurrentInfo) + " GB";
                    }
                    return temp.ToString("n2", NumberFormatInfo.CurrentInfo) + " MB";
                }
                return temp.ToString("n0", NumberFormatInfo.CurrentInfo) + " KB";
            }
            return temp.ToString("n0", NumberFormatInfo.CurrentInfo) + ((temp == 1) ? " byte" : " bytes");
        }

        public ListViewItem AddFile(NexonArchiveFileEntry file)
        {
            if (file == null)
                throw new ArgumentNullException("file");

            ListViewItem item = new ListViewItem(Path.GetFileName(file.Path));
            item.Tag = file;

            item.SubItems.Add(file.LastModifiedTime.ToString(DateTimeFormatInfo.CurrentInfo));
            item.SubItems.Add(GetHumanSize(file.Size));

            this.Items.Add(item);
            return item;
        }

        private void SetSortIcon(int columnIndex, SortOrder order)
        {
            IntPtr columnHeader = NativeMethods.SendMessage(this.Handle, NativeMethods.LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);

            for (int columnNumber = 0; columnNumber <= this.Columns.Count - 1; columnNumber++)
            {
                IntPtr columnPtr = new IntPtr(columnNumber);
                NativeMethods.LVCOLUMN lvColumn = new NativeMethods.LVCOLUMN();
                lvColumn.mask = NativeMethods.HDI_FORMAT;
                NativeMethods.SendMessage(columnHeader, NativeMethods.HDM_GETITEM, columnPtr, ref lvColumn);

                if (!(order == SortOrder.None) && columnNumber == columnIndex)
                {
                    switch (order)
                    {
                        case SortOrder.Ascending:
                            lvColumn.fmt &= ~NativeMethods.HDF_SORTDOWN;
                            lvColumn.fmt |= NativeMethods.HDF_SORTUP;
                            break;
                        case SortOrder.Descending:
                            lvColumn.fmt &= ~NativeMethods.HDF_SORTUP;
                            lvColumn.fmt |= NativeMethods.HDF_SORTDOWN;
                            break;
                    }
                }
                else
                {
                    // Either way works, but the second one looks easier to understand.
                    //lvColumn.fmt &= ~NativeMethods.HDF_SORTDOWN & ~NativeMethods.HDF_SORTUP;
                    lvColumn.fmt &= ~(NativeMethods.HDF_SORTDOWN | NativeMethods.HDF_SORTUP);
                }

                NativeMethods.SendMessage(columnHeader, NativeMethods.HDM_SETITEM, columnPtr, ref lvColumn);
            }
        }

        #endregion

        #region Nested Classes

        private sealed class ColumnSort : IComparer
        {
            #region Fields

            private FilesListView listView;
            public int Column = -1;
            public SortOrder Order;

            #endregion

            #region Constructors

            public ColumnSort(FilesListView listView)
            {
                System.Diagnostics.Debug.Assert(listView != null);
                this.listView = listView;
            }

            #endregion

            #region IComparer Members

            public int Compare(object x, object y)
            {
                ListViewItem xItem = x as ListViewItem;
                ListViewItem yItem = y as ListViewItem;

                // If x and/or y items are null.
                if (xItem == null)
                {
                    if (yItem == null)
                        return 0;
                    return -1;
                }
                if (yItem == null)
                    return 1;

                // If no columns are being sorted.
                if (this.Column < 0 || this.Order == SortOrder.None)
                    return 0;

                // Make sure the sort order is valid.
                if (this.Order != SortOrder.Ascending && this.Order != SortOrder.Descending)
                    return 0;

                int value;
                NexonArchiveFileEntry xFile = xItem.Tag as NexonArchiveFileEntry;
                NexonArchiveFileEntry yFile = yItem.Tag as NexonArchiveFileEntry;

                // Make sure the column exists.
                if (this.Column >= xItem.SubItems.Count &&
                    this.Column >= yItem.SubItems.Count)
                {
                    return 0;
                }

                // Do specialized sort operations for special data columns.
                switch (this.Column)
                {
                    case 1: // LastModifiedTime
                        if (xFile == null || yFile == null)
                            goto default;
                        value = DateTime.Compare(xFile.LastModifiedTime, yFile.LastModifiedTime);
                        break;
                    case 2: // Size
                        if (xFile == null || yFile == null)
                            goto default;
                        value = xFile.Size.CompareTo(yFile.Size);
                        break;
                    default:
                        // TODO: Use a natural string comparison algorithm.
                        value = string.Compare(
                            xItem.SubItems[this.Column].Text,
                            yItem.SubItems[this.Column].Text,
                            StringComparison.CurrentCultureIgnoreCase);
                        break;
                }

                // If the order is descending, negate the value.
                if (this.Order == SortOrder.Descending)
                    return -value;
                return value;
            }

            #endregion
        }

        #endregion
    }
}
