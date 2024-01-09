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
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace Nexon.CSO.ModelDecryptor
{
    public partial class MainForm : Form
    {
        #region Fields

        private bool exitOnFinish;
        private bool backupFiles;

        #endregion

        #region Constructors

        public MainForm()
        {
            InitializeComponent();
        }

        #endregion

        #region Methods

        public void AddInputItem(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            Item item = new Item(path);
            this.listBox.Items.Add(item);
        }

        #region Event Handlers

        #region Form

        private void ModelDecryptorForm_Load(object sender, EventArgs e)
        {
            ListBox_SelectedIndexChanged(this, EventArgs.Empty);
        }

        #endregion

        #region Drag & Drop

        private void File_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void TextBox_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null && files.Length > 0)
                {
                    ((TextBox)sender).Text = files[0];
                }
            }
        }

        private void ListBox_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null && files.Length > 0)
                {
                    foreach (string file in files)
                    {
                        AddInputItem(file);
                    }
                }
            }
        }

        #endregion

        #region Buttons

        private void addButton_Click(object sender, EventArgs e)
        {
            try
            {
                AddInputItem(this.inputTextBox.Text);
            }
            catch (ArgumentException)
            {
                MessageBox.Show(this, "Input path must be a real file or directory.", "Error");
            }
        }

        private void modifyButton_Click(object sender, EventArgs e)
        {
            if (this.listBox.SelectedIndex < 0)
                return;

            Item item;
            try
            {
                item = new Item(this.inputTextBox.Text);
            }
            catch (ArgumentException)
            {
                MessageBox.Show("Input path must be a real file or directory.");
                return;
            }
            this.listBox.Items[this.listBox.SelectedIndex] = item;
        }

        private void removeButton_Click(object sender, EventArgs e)
        {
            if (this.listBox.SelectedIndex < 0)
                return;

            this.listBox.Items.RemoveAt(this.listBox.SelectedIndex);
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            if (this.backgroundWorker.IsBusy)
                this.backgroundWorker.CancelAsync();
            else
                Close();
        }

        private void decryptButton_Click(object sender, EventArgs e)
        {
            // Deselect any items that are selected in the list.
            this.listBox.SelectedItems.Clear();

            // Disable controls.
            this.decryptButton.Enabled = false;
            this.inputTextBox.Enabled = false;
            this.addButton.Enabled = false;

            this.backupFiles = this.backupCheckBox.Checked;

            List<Item> items = new List<Item>(this.listBox.Items.Count);
            foreach (Item item in this.listBox.Items)
            {
                System.Diagnostics.Debug.Assert(item != null);
                items.Add(item);
            }
            this.backgroundWorker.RunWorkerAsync(items);
        }

        #endregion

        #region ListBox

        private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool editEnabled = (this.listBox.SelectedIndex >= 0);
            this.modifyButton.Enabled = editEnabled;
            this.removeButton.Enabled = editEnabled;
            if (editEnabled)
            {
                Item item = this.listBox.SelectedItem as Item;
                System.Diagnostics.Debug.Assert(item != null);

                this.inputTextBox.Text = item.InputPath;
            }
        }

        #endregion

        #region TextBox

        private void TextBox_Enter(object sender, EventArgs e)
        {
            this.AcceptButton = this.addButton;
        }

        private void TextBox_Leave(object sender, EventArgs e)
        {
            this.AcceptButton = null;
        }

        #endregion

        #region BackgroundWorker

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            List<Item> items = e.Argument as List<Item>;
            if (items == null)
                return;

            foreach (Item item in items)
            {
                System.Diagnostics.Debug.Assert(item != null);

                if (this.backgroundWorker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                item.Decrypt(this.backupFiles);
            }
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (this.exitOnFinish)
                Close();

            // Enable controls.
            this.decryptButton.Enabled = true;
            this.inputTextBox.Enabled = true;
            this.addButton.Enabled = true;
        }

        #endregion

        #endregion

        #endregion
    }
}
