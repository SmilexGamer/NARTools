namespace Nexon.Extractor
{
    partial class FilesListView
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.colName = new System.Windows.Forms.ColumnHeader();
            this.colLastModified = new System.Windows.Forms.ColumnHeader();
            this.colSize = new System.Windows.Forms.ColumnHeader();
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.extractToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.verifyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // colName
            // 
            this.colName.Text = "Name";
            this.colName.Width = 150;
            // 
            // colLastModified
            // 
            this.colLastModified.Text = "Last Modified";
            this.colLastModified.Width = 150;
            // 
            // colSize
            // 
            this.colSize.Text = "Size";
            this.colSize.Width = 80;
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.extractToolStripMenuItem,
            this.verifyToolStripMenuItem});
            this.contextMenuStrip.Name = "contextMenuStrip";
            this.contextMenuStrip.Size = new System.Drawing.Size(119, 48);
            this.contextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip_Opening);
            // 
            // extractToolStripMenuItem
            // 
            this.extractToolStripMenuItem.Name = "extractToolStripMenuItem";
            this.extractToolStripMenuItem.Size = new System.Drawing.Size(118, 22);
            this.extractToolStripMenuItem.Text = "E&xtract...";
            this.extractToolStripMenuItem.Click += new System.EventHandler(this.extractToolStripMenuItem_Click);
            // 
            // verifyToolStripMenuItem
            // 
            this.verifyToolStripMenuItem.Name = "verifyToolStripMenuItem";
            this.verifyToolStripMenuItem.Size = new System.Drawing.Size(118, 22);
            this.verifyToolStripMenuItem.Text = "&Verify";
            this.verifyToolStripMenuItem.Click += new System.EventHandler(this.verifyToolStripMenuItem_Click);
            // 
            // FilesListView
            // 
            this.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colName,
            this.colLastModified,
            this.colSize});
            this.ContextMenuStrip = this.contextMenuStrip;
            this.FullRowSelect = true;
            this.HideSelection = false;
            this.View = System.Windows.Forms.View.Details;
            this.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.FilesListView_ColumnClick);
            this.contextMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ColumnHeader colLastModified;
        private System.Windows.Forms.ColumnHeader colSize;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem extractToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem verifyToolStripMenuItem;
    }
}
