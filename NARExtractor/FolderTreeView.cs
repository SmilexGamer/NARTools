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
using System.IO;
using System.Windows.Forms;

namespace Nexon.Extractor
{
    public partial class FolderTreeView : TreeView
    {
        #region Events

        public event EventHandler<FilesEventArgs> ShowFolder;
        public event EventHandler<FilesEventArgs> ExtractFolder;
        public event EventHandler<FilesEventArgs> VerifyFolder;

        #endregion

        #region Constructors

        public FolderTreeView()
        {
            InitializeComponent();
            this.TreeViewNodeSorter = new TreeSort(this);
        }

        #endregion

        #region Methods

        #region Event Callers

        protected void OnShowFolder(FilesEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            if (ShowFolder != null)
                ShowFolder(this, e);
        }

        protected void OnExtractFolder(FilesEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            if (ExtractFolder != null)
                ExtractFolder(this, e);
        }

        protected void OnVerifyFolder(FilesEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            if (VerifyFolder != null)
                VerifyFolder(this, e);
        }

        #endregion

        #region Event Handlers

        private void FolderTreeView_MouseDown(object sender, MouseEventArgs e)
        {
            // Make it so when the left or right mouse buttons go down, the item
            // at that location is selected. (Rather than waiting for mouse up;
            // and even then the right click won't apply the selection.)
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                this.SelectedNode = GetNodeAt(e.Location);
                // Handle deselection here since BeforeSelect/AfterSelect does
                // not trigger if there is no node.
                if (this.SelectedNode == null)
                {
                    OnShowFolder(new FilesEventArgs());
                }
            }
        }

        private void FolderTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            OnShowFolder(new FilesEventArgs(GetFullPath(e.Node), e.Node.Tag as List<NexonArchiveFileEntry>));
        }

        private void contextMenuStrip_Opening(object sender, CancelEventArgs e)
        {
            if (this.SelectedNode == null)
                e.Cancel = true;
        }

        private void extractToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.SelectedNode != null)
            {
                List<NexonArchiveFileEntry> files = new List<NexonArchiveFileEntry>();
                GetFilesRecursive(this.SelectedNode, files);

                OnExtractFolder(new FilesEventArgs(GetFullPath(this.SelectedNode), files));
            }
        }

        private void verifyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.SelectedNode != null)
            {
                List<NexonArchiveFileEntry> files = new List<NexonArchiveFileEntry>();
                GetFilesRecursive(this.SelectedNode, files);

                OnVerifyFolder(new FilesEventArgs(GetFullPath(this.SelectedNode), files));
            }
        }

        #endregion

        private static TreeNode FindOrCreateNodePath(TreeNode rootNode, string path)
        {
            System.Diagnostics.Debug.Assert(rootNode != null);
            System.Diagnostics.Debug.Assert(path != null);

            // If the path is empty return the root node.
            if (path.Length == 0)
                return rootNode;

            // Skip any slashes in the beginning of the path.
            int startIndex = 0;
            int separatorIndex;
            while (path[startIndex] == '/' || path[startIndex] == '\\')
                ++startIndex;

            // Extract the first component from the path.
            string name;
            separatorIndex = path.IndexOfAny(new char[] { '/', '\\' }, startIndex);
            if (separatorIndex >= 0)
                name = path.Substring(startIndex, separatorIndex - startIndex);
            else
                name = path.Substring(startIndex);

            TreeNode finalNode = null;

            // Search for a node with the same name.
            foreach (TreeNode node in rootNode.Nodes)
            {
                if (string.Compare(node.Name, name, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    finalNode = node;
                    break;
                }
            }

            // If no node with the given name was found, then create a new one
            // and append it to the root node.
            if (finalNode == null)
            {
                finalNode = new TreeNode(name);
                finalNode.Name = name;
                rootNode.Nodes.Add(finalNode);
            }

            // If there are more path components, then process the them.
            if (separatorIndex >= 0)
                return FindOrCreateNodePath(finalNode, path.Substring(separatorIndex + 1));

            return finalNode;
        }

        public void LoadArchive(NexonArchive archive)
        {
            this.Nodes.Clear();
            if (archive == null)
                return;

            TreeNode rootNode = new TreeNode("(root)");
            foreach (NexonArchiveFileEntry entry in archive.FileEntries)
            {
                TreeNode node = FindOrCreateNodePath(rootNode, Path.GetDirectoryName(entry.Path));
                IList<NexonArchiveFileEntry> nodeList = node.Tag as IList<NexonArchiveFileEntry>;
                if (nodeList == null)
                {
                    nodeList = new List<NexonArchiveFileEntry>();
                    node.Tag = nodeList;
                }
                nodeList.Add(entry);
            }
            rootNode.Expand();
            this.Nodes.Add(rootNode);
            this.SelectedNode = rootNode;
        }

        public static IList<NexonArchiveFileEntry> GetFilesRecursive(TreeNode node, IList<NexonArchiveFileEntry> files)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            if (files == null)
                throw new ArgumentNullException("files");

            IList<NexonArchiveFileEntry> directoryFiles = node.Tag as IList<NexonArchiveFileEntry>;
            if (directoryFiles != null)
            {
                foreach (NexonArchiveFileEntry file in directoryFiles)
                {
                    System.Diagnostics.Debug.Assert(file != null);
                    files.Add(file);
                }
            }
            foreach (TreeNode childNode in node.Nodes)
            {
                GetFilesRecursive(childNode, files);
            }
            return files;
        }

        public static string GetFullPath(TreeNode node)
        {
            if (node == null || node.Parent == null)
                return string.Empty;
            //if (node.Parent == null)
            //    return "/";

            if (node.Parent != null && node.Parent.Parent != null)
                return GetFullPath(node.Parent) + "/" + node.Text;
            return node.Text;
        }

        #endregion

        #region Nested Classes

        private sealed class TreeSort : IComparer
        {
            #region Fields

            private FolderTreeView treeView;

            #endregion

            #region Constructors

            public TreeSort(FolderTreeView treeView)
            {
                System.Diagnostics.Debug.Assert(treeView != null);
                this.treeView = treeView;
            }

            #endregion

            #region IComparer Members

            public int Compare(object x, object y)
            {
                TreeNode xNode = x as TreeNode;
                TreeNode yNode = y as TreeNode;

                // If x and/or y nodes are null.
                if (xNode == null)
                {
                    if (yNode == null)
                        return 0;
                    return -1;
                }
                if (yNode == null)
                    return 1;

                // TODO: Use a natural string comparison algorithm.
                return string.Compare(xNode.Text, yNode.Text,
                    StringComparison.CurrentCultureIgnoreCase);
            }

            #endregion
        }

        #endregion
    }
}
