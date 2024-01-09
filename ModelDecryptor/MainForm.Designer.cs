namespace Nexon.CSO.ModelDecryptor
{
    partial class MainForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.Label inputLabel;
            System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
            this.addButton = new System.Windows.Forms.Button();
            this.removeButton = new System.Windows.Forms.Button();
            this.modifyButton = new System.Windows.Forms.Button();
            this.exitButton = new System.Windows.Forms.Button();
            this.decryptButton = new System.Windows.Forms.Button();
            this.listBox = new System.Windows.Forms.ListBox();
            this.inputTextBox = new System.Windows.Forms.TextBox();
            this.backgroundWorker = new System.ComponentModel.BackgroundWorker();
            this.backupCheckBox = new System.Windows.Forms.CheckBox();
            inputLabel = new System.Windows.Forms.Label();
            tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            tableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // inputLabel
            // 
            inputLabel.AutoSize = true;
            inputLabel.Location = new System.Drawing.Point(270, 9);
            inputLabel.Name = "inputLabel";
            inputLabel.Size = new System.Drawing.Size(87, 13);
            inputLabel.TabIndex = 2;
            inputLabel.Text = "&Input File/Folder:";
            // 
            // tableLayoutPanel
            // 
            tableLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            tableLayoutPanel.ColumnCount = 3;
            tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            tableLayoutPanel.Controls.Add(this.addButton, 0, 0);
            tableLayoutPanel.Controls.Add(this.removeButton, 2, 0);
            tableLayoutPanel.Controls.Add(this.modifyButton, 1, 0);
            tableLayoutPanel.Location = new System.Drawing.Point(270, 90);
            tableLayoutPanel.Name = "tableLayoutPanel";
            tableLayoutPanel.RowCount = 1;
            tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel.Size = new System.Drawing.Size(291, 29);
            tableLayoutPanel.TabIndex = 7;
            // 
            // addButton
            // 
            this.addButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.addButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.addButton.Location = new System.Drawing.Point(3, 3);
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size(91, 23);
            this.addButton.TabIndex = 6;
            this.addButton.Text = "&Add";
            this.addButton.UseVisualStyleBackColor = true;
            this.addButton.Click += new System.EventHandler(this.addButton_Click);
            // 
            // removeButton
            // 
            this.removeButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.removeButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.removeButton.Location = new System.Drawing.Point(197, 3);
            this.removeButton.Name = "removeButton";
            this.removeButton.Size = new System.Drawing.Size(91, 23);
            this.removeButton.TabIndex = 6;
            this.removeButton.Text = "&Remove";
            this.removeButton.UseVisualStyleBackColor = true;
            this.removeButton.Click += new System.EventHandler(this.removeButton_Click);
            // 
            // modifyButton
            // 
            this.modifyButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.modifyButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.modifyButton.Location = new System.Drawing.Point(100, 3);
            this.modifyButton.Name = "modifyButton";
            this.modifyButton.Size = new System.Drawing.Size(91, 23);
            this.modifyButton.TabIndex = 6;
            this.modifyButton.Text = "&Modify";
            this.modifyButton.UseVisualStyleBackColor = true;
            this.modifyButton.Click += new System.EventHandler(this.modifyButton_Click);
            // 
            // exitButton
            // 
            this.exitButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.exitButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.exitButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.exitButton.Location = new System.Drawing.Point(289, 241);
            this.exitButton.Name = "exitButton";
            this.exitButton.Size = new System.Drawing.Size(75, 23);
            this.exitButton.TabIndex = 0;
            this.exitButton.Text = "E&xit";
            this.exitButton.UseVisualStyleBackColor = true;
            this.exitButton.Click += new System.EventHandler(this.exitButton_Click);
            // 
            // decryptButton
            // 
            this.decryptButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.decryptButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.decryptButton.Location = new System.Drawing.Point(208, 241);
            this.decryptButton.Name = "decryptButton";
            this.decryptButton.Size = new System.Drawing.Size(75, 23);
            this.decryptButton.TabIndex = 0;
            this.decryptButton.Text = "&Decrypt";
            this.decryptButton.UseVisualStyleBackColor = true;
            this.decryptButton.Click += new System.EventHandler(this.decryptButton_Click);
            // 
            // listBox
            // 
            this.listBox.AllowDrop = true;
            this.listBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.listBox.FormattingEnabled = true;
            this.listBox.IntegralHeight = false;
            this.listBox.Location = new System.Drawing.Point(12, 12);
            this.listBox.Name = "listBox";
            this.listBox.Size = new System.Drawing.Size(252, 223);
            this.listBox.TabIndex = 1;
            this.listBox.SelectedIndexChanged += new System.EventHandler(this.ListBox_SelectedIndexChanged);
            this.listBox.DragDrop += new System.Windows.Forms.DragEventHandler(this.ListBox_DragDrop);
            this.listBox.DragEnter += new System.Windows.Forms.DragEventHandler(this.File_DragEnter);
            // 
            // inputTextBox
            // 
            this.inputTextBox.AllowDrop = true;
            this.inputTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.inputTextBox.Location = new System.Drawing.Point(270, 25);
            this.inputTextBox.Name = "inputTextBox";
            this.inputTextBox.Size = new System.Drawing.Size(291, 20);
            this.inputTextBox.TabIndex = 3;
            this.inputTextBox.DragDrop += new System.Windows.Forms.DragEventHandler(this.TextBox_DragDrop);
            this.inputTextBox.Leave += new System.EventHandler(this.TextBox_Leave);
            this.inputTextBox.Enter += new System.EventHandler(this.TextBox_Enter);
            this.inputTextBox.DragEnter += new System.Windows.Forms.DragEventHandler(this.File_DragEnter);
            // 
            // backgroundWorker
            // 
            this.backgroundWorker.WorkerSupportsCancellation = true;
            this.backgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker_DoWork);
            this.backgroundWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker_RunWorkerCompleted);
            // 
            // backupCheckBox
            // 
            this.backupCheckBox.AutoSize = true;
            this.backupCheckBox.Checked = true;
            this.backupCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.backupCheckBox.Location = new System.Drawing.Point(270, 125);
            this.backupCheckBox.Name = "backupCheckBox";
            this.backupCheckBox.Size = new System.Drawing.Size(138, 17);
            this.backupCheckBox.TabIndex = 8;
            this.backupCheckBox.Text = "&Backup Encrypted Files";
            this.backupCheckBox.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.exitButton;
            this.ClientSize = new System.Drawing.Size(573, 276);
            this.Controls.Add(this.backupCheckBox);
            this.Controls.Add(tableLayoutPanel);
            this.Controls.Add(this.inputTextBox);
            this.Controls.Add(inputLabel);
            this.Controls.Add(this.listBox);
            this.Controls.Add(this.decryptButton);
            this.Controls.Add(this.exitButton);
            this.MinimumSize = new System.Drawing.Size(495, 196);
            this.Name = "MainForm";
            this.Text = "Model Decryptor by Da_FileServer";
            this.Load += new System.EventHandler(this.ModelDecryptorForm_Load);
            tableLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button exitButton;
        private System.Windows.Forms.Button decryptButton;
        private System.Windows.Forms.ListBox listBox;
        private System.Windows.Forms.TextBox inputTextBox;
        private System.Windows.Forms.Button addButton;
        private System.Windows.Forms.Button modifyButton;
        private System.Windows.Forms.Button removeButton;
        private System.ComponentModel.BackgroundWorker backgroundWorker;
        private System.Windows.Forms.CheckBox backupCheckBox;
    }
}

