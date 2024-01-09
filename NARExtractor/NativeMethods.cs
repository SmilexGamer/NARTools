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
using System.Runtime.InteropServices;

namespace Nexon.Extractor
{
    internal static class NativeMethods
    {
        #region Nested Structures

        [StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct LVCOLUMN
        {
            public Int32 mask;
            public Int32 cx;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pszText;
            public IntPtr hbm;
            public Int32 cchTextMax;
            public Int32 fmt;
            public Int32 iSubItem;
            public Int32 iImage;
            public Int32 iOrder;
        }

        #endregion

        #region Fields

        public const Int32 HDI_FORMAT = 0x4;
        public const Int32 HDF_SORTUP = 0x400;
        public const Int32 HDF_SORTDOWN = 0x200;
        public const Int32 LVM_GETHEADER = 0x101f;
        public const Int32 HDM_GETITEM = 0x120b;
        public const Int32 HDM_SETITEM = 0x120c;

        #endregion

        #region Constants

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, ref LVCOLUMN lPLVCOLUMN);

        #endregion
    }
}
