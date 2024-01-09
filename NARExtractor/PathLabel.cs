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

using System.Drawing;
using System.Windows.Forms;

namespace Nexon.Extractor
{
    public class PathLabel : Label
    {
        #region Fields

        private StringFormat stringFormat = new StringFormat(StringFormatFlags.NoWrap);
        private SolidBrush foreBrush;

        #endregion

        #region Constructors

        public PathLabel()
        {
            this.stringFormat.Trimming = StringTrimming.EllipsisPath;
            this.foreBrush = new SolidBrush(this.ForeColor);
        }

        #endregion

        #region Properties

        public override Color ForeColor
        {
            get { return base.ForeColor; }
            set
            {
                base.ForeColor = value;
                this.foreBrush.Dispose();
                this.foreBrush = new SolidBrush(value);
            }
        }

        #endregion

        #region Methods

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.stringFormat.Dispose();
                this.foreBrush.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.DrawString(this.Text, this.Font, this.foreBrush, this.ClientRectangle, this.stringFormat);
        }

        #endregion
    }
}
