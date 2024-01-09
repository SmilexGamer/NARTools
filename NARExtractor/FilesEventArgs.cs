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

namespace Nexon.Extractor
{
    public class FilesEventArgs : EventArgs
    {
        #region Fields

        private string path;
        private IList<NexonArchiveFileEntry> files;

        #endregion

        #region Constructors

        public FilesEventArgs()
            : this(string.Empty, null)
        {
        }

        public FilesEventArgs(string path, IList<NexonArchiveFileEntry> files)
        {
            this.path = path;
            this.files = files;
        }

        #endregion

        #region Properties

        public string Path
        {
            get { return this.path; }
        }

        public IList<NexonArchiveFileEntry> Files
        {
            get { return this.files; }
        }

        #endregion
    }
}
