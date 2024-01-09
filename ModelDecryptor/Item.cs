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
using System.IO;

namespace Nexon.CSO.ModelDecryptor
{
    internal sealed class Item
    {
        #region Fields

        private string inputPath;

        #endregion

        #region Constructors

        public Item(string inputPath)
        {
            // If the path is empty, convert it to the current directory.
            if (string.IsNullOrEmpty(inputPath))
                inputPath = Environment.CurrentDirectory;

            if (!Directory.Exists(inputPath) && !File.Exists(inputPath))
                throw new ArgumentException("Input path must be an existing directory or file.", "inputPath");

            this.InputPath = inputPath;
        }

        #endregion

        #region Properties

        public string InputPath
        {
            get { return this.inputPath; }
            set
            {
                if (!Directory.Exists(value) && !File.Exists(value))
                    throw new ArgumentException("Input path must be an existing directory or file.", "value");
                this.inputPath = value;
            }
        }

        #endregion

        #region Methods

        public void Decrypt(bool backup)
        {
            System.Diagnostics.Debug.Assert(this.InputPath != null);

            DirectoryInfo directory = new DirectoryInfo(this.InputPath);
            if (directory.Exists)
                Decrypt(backup, directory);
            else
            {
                FileInfo file = new FileInfo(this.InputPath);
                if (file.Exists)
                    Decrypt(backup, file);
            }
        }

        private void Decrypt(bool backup, DirectoryInfo directory)
        {
            System.Diagnostics.Debug.Assert(directory != null);
            System.Diagnostics.Debug.Assert(directory.Exists);

            foreach (FileInfo file in directory.GetFiles("*.mdl", SearchOption.AllDirectories))
            {
                Decrypt(backup, file);
            }
        }

        private void Decrypt(bool backup, FileInfo file)
        {
            System.Diagnostics.Debug.Assert(file != null);
            System.Diagnostics.Debug.Assert(file.Exists);

            // Create a temp file (a copy of the real file).
            FileInfo tempFile = null;
            do
            {
                tempFile = new FileInfo(Path.Combine(Path.GetDirectoryName(file.DirectoryName), Path.ChangeExtension(Path.GetTempFileName(), ".mdl")));
            } while (tempFile.Exists);
            file.CopyTo(tempFile.FullName);
            tempFile.Attributes = FileAttributes.Hidden | FileAttributes.Temporary;

            try
            {
                // Try to decrypt the temp file.
                if (ModelHelper.DecryptModel(tempFile.FullName) == ModelResult.Success)
                {
                    // Backup the real file.
                    if (backup)
                    {
                        FileInfo backupFile = new FileInfo(GetBackupFileName(file.FullName));
                        file.CopyTo(backupFile.FullName);
                    }

                    // Replace the real file's data with the temp file's data.
                    tempFile.CopyTo(file.FullName, true);
                }
            }
            finally
            {
                // Delete the temp file.
                tempFile.Refresh();
                if (tempFile.Exists)
                    tempFile.Delete();
            }
        }

        private string GetBackupFileName(string fileName)
        {
            FileInfo backupFile;
            int backupFileIndex = 0;
            do
            {
                string bakExtension = ".bak" + (backupFileIndex == 0 ? string.Empty : backupFileIndex.ToString());
                backupFile = new FileInfo(Path.ChangeExtension(fileName, Path.GetExtension(fileName) + bakExtension));
                ++backupFileIndex;
            } while (backupFile.Exists);
            return backupFile.FullName;
        }

        public override string ToString()
        {
            return this.InputPath;
        }

        #endregion
    }
}
