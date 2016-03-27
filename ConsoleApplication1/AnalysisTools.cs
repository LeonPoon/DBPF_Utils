using GenUtils;
using System;
using System.Runtime.InteropServices;

namespace ConsoleApplication1
{
    internal class AnalysisTools
    {
        public long checkSize(ReadResult[] f, long pos)
        {
            foreach (var c in f)
            {
                if (c.Pos != pos)
                    throw new ArgumentException(string.Format("start of {2}: expect {0}, got actual={1}", pos, c.Pos, c.getName()));
                pos += c.Bytes;
                var cs = c.ReadResultComponents;
                if (cs != null)
                    pos = checkSize(cs, pos);
                if (pos != c.Pos + c.Sz)
                    throw new ArgumentException(string.Format("endpos of {2}: expect {0}, got actual={1}", pos, c.Pos + c.Sz, c.getName()));
            }
            return pos;
        }
    }
}


