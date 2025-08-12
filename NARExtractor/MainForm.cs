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
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace Nexon.Extractor
{
    public partial class MainForm : Form
    {
        #region Nested Classes

        private sealed class ExtractHelper
        {
            public bool DecryptModels;
            public string ExtractPath;

            public bool Extract(NexonArchiveFileEntry file)
            {
                System.Diagnostics.Debug.Assert(file != null);
                System.Diagnostics.Debug.Assert(this.ExtractPath != null);

                string path = file.Path;

                // Strip initial slashes from path.
                int slashIndex = 0;
                while (path[slashIndex] == Path.DirectorySeparatorChar || path[slashIndex] == Path.AltDirectorySeparatorChar)
                    ++slashIndex;
                path = path.Substring(slashIndex);

                // Combine extraction path and file path.
                path = Path.Combine(this.ExtractPath, path);

                // Get the path's directory and make sure it exists.
                DirectoryInfo pathDirectory = new DirectoryInfo(Path.GetDirectoryName(path));
                if (!pathDirectory.Exists)
                    pathDirectory.Create();

                // Create file and extract to it.
                using (FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
                {
                    file.Extract(fileStream);

                    // If the extension is MDL and model decryption is enabled,
                    // attempt to decrypt the model file.
                    if (this.DecryptModels && string.Compare(Path.GetExtension(path), ".mdl", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        ModelHelper.DecryptModel(fileStream);
                    }
                }

                File.SetCreationTime(path, file.LastModifiedTime);
                File.SetLastWriteTime(path, file.LastModifiedTime);
                File.SetLastAccessTime(path, file.LastModifiedTime);

                return true;
            }
        }

        #endregion

        #region Fields

        private NexonArchive archive;

        #endregion

        #region Constructors

        public MainForm()
        {
            InitializeComponent();
        }

        #endregion

        #region Properties

        public string InitialLoadFile
        {
            get;
            set;
        }

        #endregion

        #region Methods

        #region Form

        private void NARExtractorForm_Load(object sender, EventArgs e)
        {
            // Load settings.
            Properties.Settings settings = Properties.Settings.Default;
            if (!settings.FormSize.IsEmpty)
                this.Size = settings.FormSize;
            if (settings.FormWindowState != FormWindowState.Minimized)
                this.WindowState = settings.FormWindowState;
            if (settings.SplitterDistance > 0)
                this.splitContainer1.SplitterDistance = settings.SplitterDistance;
            if (settings.ColumnNameWidth >= 0)
                this.listView.Columns[0].Width = settings.ColumnNameWidth;
            if (settings.ColumnNamePosition >= 0)
                this.listView.Columns[0].DisplayIndex = settings.ColumnNamePosition;
            if (settings.ColumnLastModifiedWidth >= 0)
                this.listView.Columns[1].Width = settings.ColumnLastModifiedWidth;
            if (settings.ColumnLastModifiedPosition >= 0)
                this.listView.Columns[1].DisplayIndex = settings.ColumnLastModifiedPosition;
            if (settings.ColumnSizeWidth >= 0)
                this.listView.Columns[2].Width = settings.ColumnSizeWidth;
            if (settings.ColumnSizePosition >= 0)
                this.listView.Columns[2].DisplayIndex = settings.ColumnSizePosition;
            if (settings.ColumnStoredTypeWidth >= 0)
                this.listView.Columns[2].Width = settings.ColumnStoredTypeWidth;
            if (settings.ColumnStoredTypePosition >= 0)
                this.listView.Columns[2].DisplayIndex = settings.ColumnStoredTypePosition;
            if (settings.SortColumn >= 0)
            {
                this.listView.SortColumn = settings.SortColumn;
                this.listView.SortColumnOrder = SortOrder.None;
            }
            if (settings.SortColumnOrder != SortOrder.None)
                this.listView.SortColumnOrder = settings.SortColumnOrder;
            this.autoDecryptModelsToolStripMenuItem.Checked = settings.AutoDecryptModels;

            this.pathToolStripStatusLabel.Text = string.Empty;
            this.fileToolStripStatusLabel.Text = string.Empty;
        }

        private void NARExtractorForm_Shown(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(this.InitialLoadFile))
            {
                Open(this.InitialLoadFile);
            }
        }

        private void NARExtractorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            CloseArchive();
        }

        private void NARExtractorForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Save settings.
            Properties.Settings settings = Properties.Settings.Default;
            settings.FormSize = this.WindowState == FormWindowState.Minimized ||
                this.WindowState == FormWindowState.Maximized
                ? this.RestoreBounds.Size : this.Size;
            settings.FormWindowState = this.WindowState;
            settings.SplitterDistance = this.splitContainer1.SplitterDistance;
            settings.ColumnNameWidth = this.listView.Columns[0].Width;
            settings.ColumnNamePosition = this.listView.Columns[0].DisplayIndex;
            settings.ColumnLastModifiedWidth = this.listView.Columns[1].Width;
            settings.ColumnLastModifiedPosition = this.listView.Columns[1].DisplayIndex;
            settings.ColumnSizeWidth = this.listView.Columns[2].Width;
            settings.ColumnSizePosition = this.listView.Columns[2].DisplayIndex;
            settings.ColumnStoredTypeWidth = this.listView.Columns[2].Width;
            settings.ColumnStoredTypePosition = this.listView.Columns[2].DisplayIndex;
            settings.SortColumn = this.listView.SortColumn;
            settings.SortColumnOrder = this.listView.SortColumnOrder;
            settings.AutoDecryptModels = this.autoDecryptModelsToolStripMenuItem.Checked;
            settings.Save();
        }

        #endregion

        #region Menu Strip

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenDialog();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.listView.Focus();
            //this.listView.BeginUpdate();
            foreach (ListViewItem item in this.listView.Items)
            {
                item.Selected = true;
            }
            //this.listView.EndUpdate();
        }

        private void extractAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.archive != null && this.treeView.TopNode != null)
            {
                List<NexonArchiveFileEntry> files = new List<NexonArchiveFileEntry>();
                FolderTreeView.GetFilesRecursive(this.treeView.TopNode, files);
                ExtractFiles(this, new FilesEventArgs(FolderTreeView.GetFullPath(this.treeView.TopNode), files));
            }
        }

        private void verifyAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.archive != null && this.treeView.TopNode != null)
            {
                List<NexonArchiveFileEntry> files = new List<NexonArchiveFileEntry>();
                FolderTreeView.GetFilesRecursive(this.treeView.TopNode, files);
                VerifyFiles(this, new FilesEventArgs(FolderTreeView.GetFullPath(this.treeView.TopNode), files));
            }
        }

        private void autoDecryptModelsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.AutoDecryptModels = this.autoDecryptModelsToolStripMenuItem.Checked;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (AboutBox aboutBox = new AboutBox())
            {
                aboutBox.ShowDialog();
            }
        }

        #endregion

        #region Drag & Drop

        private void NARExtractorForm_DragEnter(object sender, DragEventArgs e)
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

        private void NARExtractorForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null && files.Length > 0)
                {
                    // Only load the first file, ignore all the rest.
                    Open(files[0]);
                }
            }
        }

        #endregion

        #region Tree View

        private void treeView_ShowFolder(object sender, FilesEventArgs e)
        {
            this.pathToolStripStatusLabel.Text = e.Path;
            this.listView.FullPath = e.Path;

            this.listView.BeginUpdate();
            this.listView.Items.Clear();
            listView_SelectedIndexChanged(this, EventArgs.Empty);
            if (e.Files != null)
            {
                foreach (NexonArchiveFileEntry entry in e.Files)
                {
                    System.Diagnostics.Debug.Assert(entry != null);
                    this.listView.AddFile(entry);
                }
            }
            this.listView.EndUpdate();
            listView_SelectedIndexChanged(this, EventArgs.Empty);
        }

        #endregion

        #region List View

        private void listView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.treeView.SelectedNode != null)
            {
                if (this.listView.SelectedIndices.Count > 0)
                {
                    if (this.listView.SelectedIndices.Count == 1)
                        this.fileToolStripStatusLabel.Text = string.Format(NumberFormatInfo.CurrentInfo, "{0} item selected", this.listView.SelectedIndices.Count);
                    else
                        this.fileToolStripStatusLabel.Text = string.Format(NumberFormatInfo.CurrentInfo, "{0} items selected", this.listView.SelectedIndices.Count);
                }
                else
                {
                    if (this.listView.Items.Count == 1)
                        this.fileToolStripStatusLabel.Text = string.Format(NumberFormatInfo.CurrentInfo, "{0} item", this.listView.Items.Count);
                    else
                        this.fileToolStripStatusLabel.Text = string.Format(NumberFormatInfo.CurrentInfo, "{0} items", this.listView.Items.Count);
                }
            }
            else
            {
                this.fileToolStripStatusLabel.Text = string.Empty;
            }
        }

        #endregion

        private void SetTitle(string fileName)
        {
            SetTitle(fileName, false);
        }

        private void SetTitle(string fileName, bool modified)
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                if (modified)
                    this.Text = string.Format(CultureInfo.CurrentUICulture, "NAR Extractor [{0}]*", fileName);
                else
                    this.Text = string.Format(CultureInfo.CurrentUICulture, "NAR Extractor [{0}]", fileName);
            }
            else
                this.Text = string.Format(CultureInfo.CurrentUICulture, "NAR Extractor");
        }

        private void CloseArchive()
        {
            if (this.archive != null)
            {
                this.treeView.Nodes.Clear();
                this.listView.Items.Clear();
                this.archive.Close();
                SetTitle(null);
            }
        }

        private void OpenDialog()
        {
            if (this.narOpenFileDialog.ShowDialog() == DialogResult.OK)
            {
                Open(this.narOpenFileDialog.FileName);
            }
        }

        public void Open(string fileName)
        {
            CloseArchive();

            try
            {
                this.archive = new NexonArchive();
                this.archive.Load(fileName, false);
            }
            catch (Exception)
            {
                MessageBox.Show(this, "Could not open file: " + fileName, "Error");
                return;
            }

            SetTitle(fileName);
            this.treeView.LoadArchive(archive);
        }

        private void ExtractFiles(object sender, FilesEventArgs e)
        {
            if (this.extractFolderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                ExtractHelper extractHelper = new ExtractHelper();
                extractHelper.DecryptModels = Properties.Settings.Default.AutoDecryptModels;
                extractHelper.ExtractPath = this.extractFolderBrowserDialog.SelectedPath;
                using (TaskDialog taskDialog = new TaskDialog(extractHelper.Extract, e.Files))
                {
                    taskDialog.Text = "Extracting files...";
                    taskDialog.DestinationPath = extractHelper.ExtractPath;
                    DateTime startTime = DateTime.Now;
                    DialogResult result = taskDialog.ShowDialog(this);
                    DateTime endTime = DateTime.Now;
                    if (result == DialogResult.Abort)
                        MessageBox.Show("An error occured while extracting the files.", "Error");
                    else if (result == DialogResult.OK)
                        MessageBox.Show("All the selected files have been extracted successfully.\nExtraction time: " + (endTime - startTime), "Extraction Complete");
                }
            }
        }

        private static bool VerifyFilesHelper(NexonArchiveFileEntry file)
        {
            System.Diagnostics.Debug.Assert(file != null);
            return file.Verify();
        }

        private void VerifyFiles(object sender, FilesEventArgs e)
        {
            using (TaskDialog taskDialog = new TaskDialog(VerifyFilesHelper, e.Files))
            {
                taskDialog.Text = "Verifying files...";
                DialogResult result = taskDialog.ShowDialog(this);
                if (result == DialogResult.Abort)
                    MessageBox.Show("An error occured while verifying the files.", "Error");
                else if (result == DialogResult.OK)
                    MessageBox.Show("All the selected files have been verified successfully.", "Verification Complete");
            }
        }

        #endregion
    }
}
