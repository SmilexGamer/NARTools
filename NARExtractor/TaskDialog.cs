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
using System.IO;
using System.Windows.Forms;

namespace Nexon.Extractor
{
    public partial class TaskDialog : Form
    {
        #region Nested Classes

        private sealed class ProgressUpdate
        {
            public long CurrentFileSize;
            public long TotalFileSize;

            //public int CurrentIndex;
            //public int Count;
        }

        #endregion

        #region Fields

        private Predicate<NexonArchiveFileEntry> task;
        private ICollection<NexonArchiveFileEntry> entries;

        private string destinationPath;

        #endregion

        #region Constructors

        public TaskDialog(Predicate<NexonArchiveFileEntry> task, ICollection<NexonArchiveFileEntry> entries)
        {
            if (task == null)
                throw new ArgumentNullException("task");
            if (entries == null)
                throw new ArgumentNullException("entries");

            this.task = task;
            this.entries = entries;

            InitializeComponent();
        }

        #endregion

        #region Properties

        public string DestinationPath
        {
            get { return this.destinationPath; }
            set { this.destinationPath = value; }
        }

        #endregion

        #region Methods

        private void NexusArchiveTaskDialog_Load(object sender, EventArgs e)
        {
            this.backgroundWorker.RunWorkerAsync();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            if (this.backgroundWorker.IsBusy)
            {
                this.backgroundWorker.CancelAsync();
                this.progressBar.Style = ProgressBarStyle.Marquee;
            }
            else
            {
                this.DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        // State parameter is unused; it's necessary to use the Action delegate.
        // (This is a .NET 2.0 project, so there is no parameterless action.)
        private void InitializeProgressWork(object state)
        {
            this.progressBar.Style = ProgressBarStyle.Continuous;
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // Lock the entries so they do not get modified while the background
            // worker is working.
            lock (this.entries)
            {
                ProgressUpdate progress = new ProgressUpdate();

                // Calculate total size of all the entries.
                //progress.Count = this.entries.Count;
                foreach (NexonArchiveFileEntry entry in this.entries)
                    progress.TotalFileSize += entry.Size;

                // Check for cancellation.
                if (this.backgroundWorker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                // Initialize the progress bar to continuous mode.
                this.Invoke(new Action<object>(InitializeProgressWork), (object)null);

                // Check for cancellation.
                if (this.backgroundWorker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                // Process entries.
                foreach (NexonArchiveFileEntry entry in this.entries)
                {
                    // Report the file name as the progress.
                    this.backgroundWorker.ReportProgress(0, entry.Path);

                    // Run the task. If it fails, throw an exception (which will
                    // get caught by the BackgroundWorker or debugger).
                    if (!this.task(entry))
                    {
                        throw new ApplicationException("Task sent an abort code.");
                    }

                    // Check for cancellation.
                    if (this.backgroundWorker.CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }

                    // Report the current size total as the progress.
                    progress.CurrentFileSize += entry.Size;
                    this.backgroundWorker.ReportProgress(0, progress);

                    //++progress.CurrentIndex;
                }
            }
        }

        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Check if there is a size-based progress update.
            ProgressUpdate progress = e.UserState as ProgressUpdate;
            if (progress != null)
            {
                // Since theoretically the maximum size of an archive (in this
                // implementation) is [2 * 0xFFFFFFFF], the value cannot be
                // converted to an integer without overflowing. Therefore, scale
                // the progress value to the progress bar's current width. The
                // scaling should never overflow, since the width should never
                // be large enough to cause an overflow of a long value. (To
                // create an overflow, a width greater than 0x40000000 is
                // required.)
                int width = this.progressBar.ClientSize.Width;
                this.progressBar.Maximum = width;
                this.progressBar.Value = Convert.ToInt32((progress.CurrentFileSize * width) / progress.TotalFileSize);
            }
            else
            {
                // Check if there is a file name-based progress update.
                string path = e.UserState as string;
                if (!string.IsNullOrEmpty(path))
                {
                    // Convert alternate slashes to regular slashes.
                    path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

                    // Strip initial slashes from path.
                    int slashIndex = 0;
                    while (path[slashIndex] == Path.DirectorySeparatorChar)
                        ++slashIndex;
                    path = path.Substring(slashIndex);

                    // Set path labels.
                    this.sourcePathLabel.Text = path;
                    if (this.destinationPath != null)
                        this.destinationPathLabel.Text = Path.Combine(this.destinationPath, path);
                }
            }
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
                this.DialogResult = DialogResult.Abort;
            else if (e.Cancelled)
                this.DialogResult = DialogResult.Cancel;
            else
            {
                this.progressBar.Value = this.progressBar.Maximum;
                this.DialogResult = DialogResult.OK;
            }
            Close();
        }

        #endregion
    }
}
