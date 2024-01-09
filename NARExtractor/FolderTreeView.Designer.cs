namespace Nexon.Extractor
{
    partial class FolderTreeView
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
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.extractToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.verifyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip.SuspendLayout();
            this.SuspendLayout();
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
            // FolderTreeView
            // 
            this.ContextMenuStrip = this.contextMenuStrip;
            this.HideSelection = false;
            this.LineColor = System.Drawing.Color.Black;
            this.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.FolderTreeView_AfterSelect);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.FolderTreeView_MouseDown);
            this.contextMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem extractToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem verifyToolStripMenuItem;
    }
}
