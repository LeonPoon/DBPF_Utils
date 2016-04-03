/**************************************************************************
 * Copyright 2016 Leon Poon
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 **************************************************************************/

using System;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Windows.Forms;

namespace DatabasePackedFileViewer
{
    public partial class TextDisplay : UserControl
    {
        private byte[] bytes;
        private const int bytesPerRow = 16;
        private const string byteTemplate = "  .";
        private static readonly int charsPerByte = byteTemplate.Length;
        private const string spacer = "   ";
        private const int addressWidth = 8;
        private static readonly string addressFormat = "{0:X" + addressWidth + "}";
        private static readonly string addressTemplate = string.Format(addressFormat, 0);
        private static readonly int addressChars = addressTemplate.Length;
        private static readonly int charsPerLine = addressChars + spacer.Length + bytesPerRow * charsPerByte + spacer.Length + bytesPerRow;
        private static readonly char[,] hexes = new char[256, 2];
        private static readonly string template;
        private static readonly int firstByteStartChar;
        private static readonly int firstHexStartChar;

        static TextDisplay()
        {
            hexes[0, 0] = hexes[0, 1] = ' ';
            for (int i = 1; i < hexes.GetLength(0); i++)
            {
                string s = string.Format("{0:X2}", i);
                for (int j = 0; j < s.Length || j < hexes.GetLength(1); j++)
                    hexes[i, j] = s[j];
            }
            char[] chars = new char[charsPerLine];
            int skip = 0;
            skip = copyChars(chars, addressTemplate, skip);
            skip = copyChars(chars, spacer, skip);
            firstHexStartChar = skip;
            for (int i = 0; i < bytesPerRow; i++)
                skip = copyChars(chars, byteTemplate, skip);
            skip = copyChars(chars, spacer, skip);
            firstByteStartChar = skip;
            for (int i = 0; i < bytesPerRow; i++)
                chars[skip++] = ' ';
            if (skip != chars.Length)
                throw new ArgumentException();
            template = new string(chars);
        }

        public byte[] Bytes
        {
            get { return bytes; }
            set
            {
                bytes = value;
                if (detectBinary(value))
                    textBox1.Lines = toHex(value);
                else
                    textBox1.Text = toString(value);
            }
        }

        public TextDisplay()
        {
            InitializeComponent();
        }

        public static string[] toHex(byte[] bytes)
        {
            int len = bytes.Length;
            int fullLines = len / bytesPerRow;
            int oddLineBytes = len % bytesPerRow;
            bool hasOdd = oddLineBytes != 0;

            string[] lines = new string[fullLines + (hasOdd ? 1 : 0)];
            char[] chars = new char[charsPerLine];
            copyChars(chars, template, 0);

            int start = 0;
            for (int i = 0; i < fullLines; i++, start += bytesPerRow)
            {
                copyChars(chars, string.Format("{0:X8}", start), 0);
                for (int j = start, max = j + bytesPerRow, hexChar = firstHexStartChar, byteChar = firstByteStartChar; j < max; j++, hexChar++)
                {
                    byte b = bytes[j];
                    char c1 = hexes[b, 0], c2 = hexes[b, 1];
                    chars[hexChar++] = c1;
                    chars[hexChar++] = c2;
                    chars[byteChar++] = isPrintable(b) ? Convert.ToChar(b) : '.';
                }
                lines[i] = new string(chars);
            }

            if (hasOdd)
            {
                copyChars(chars, string.Format("{0:X8}", start), 0);
                int hexChar = firstHexStartChar;
                int byteChar = firstByteStartChar;
                for (int j = start, max = j + oddLineBytes; j < max; j++, hexChar++)
                {
                    byte b = bytes[j];
                    chars[hexChar++] = hexes[b, 0];
                    chars[hexChar++] = hexes[b, 1];
                    chars[byteChar++] = isPrintable(b) ? Convert.ToChar(b) : '.';
                }
                for (int j = oddLineBytes; j < bytesPerRow; j++)
                {
                    chars[hexChar++] = ' ';
                    chars[hexChar++] = ' ';
                    chars[hexChar++] = ' ';
                }
                lines[fullLines] = new string(chars, 0, charsPerLine - (bytesPerRow - oddLineBytes));
            }

            return lines;
        }

        private static int copyChars(char[] dest, string from, int skipDest)
        {
            int len = from.Length;
            from.CopyTo(0, dest, skipDest, len);
            return skipDest + len;
        }

        public static bool detectBinary(byte[] bytes)
        {
            foreach (byte b in bytes)
                if (!isPrintable(b) && b != '\r' && b != '\n' && b != '\t')
                    return true;
            return false;
        }

        private static bool isPrintable(byte b)
        {
            return b >= 0x20 && b < 0x7f;
        }

        internal static string toString(byte[] bytes)
        {
            return Encoding.ASCII.GetString(bytes); // should detect encoding
        }
    }


    internal class TextDisplayInfo : DisplayInfo
    {
        public virtual Control createControl(ViewModel viewModel)
        {
            EntryModel model = viewModel.model;
            MemoryMappedViewAccessor accessor = viewModel.accessor;
            long sz = viewModel.sz;
            byte[] bytes = new byte[sz];
            accessor.ReadArray(0, bytes, 0, bytes.Length);
            TextDisplay disp = createControl();
            disp.Bytes = bytes;
            return disp;
        }

        protected virtual TextDisplay createControl()
        {
            return new TextDisplay();
        }

        public string getExtensionFilter(MemoryMappedViewAccessor accessor, long sz)
        {
            return "Binary file|*.bin";
        }

        public string getViewName(EntryModel model, MemoryMappedViewAccessor accessor, long sz)
        {
            return model.ToString();
        }
    }
}
