using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBPF
{
    public class DBPFile
    {
        public readonly Header header;
        public readonly TGIObjects<IndexTableEntry> indexTable;
        public readonly TGIObjects<DBDirectoryEntry> dbDirEntries;

        public DBPFile(Header header, TGIObjects<IndexTableEntry> indexTable, TGIObjects<DBDirectoryEntry> dbDirEntries)
        {
            this.header = header;
            this.indexTable = indexTable;
            this.dbDirEntries = dbDirEntries;
        }

        /// <summary>
        /// From http://www.wiki.sc4devotion.com/index.php?title=DBPF_Source_Code (php), which got it
        /// from http://hullabaloo.simshost.com/forum/viewtopic.php?t=6578&postdays=0&postorder=asc (perl).
        /// </summary>
        /// <param name="r"></param>
        /// <param name="w"></param>
        public long decompress(MemoryMappedViewAccessor r, MemoryMappedViewAccessor w)
        {
            long answerlen = 0;

            for (UInt32 handle = 9, numplain, numcopy, offset, len = r.ReadUInt32(0) - handle; len > 0;)
            {

                byte cc = r.ReadByte(handle++);
                len -= 1;

                if (cc >= 252)
                { // 0xFC
                    numplain = (UInt32)(cc & 0x03);
                    if (numplain > len) { numplain = len; }
                    numcopy = 0;
                    offset = 0;
                }
                else if (cc >= 224)
                { // 0xE0
                    numplain = ((UInt32)(cc - 0xdf)) << 2;
                    numcopy = 0;
                    offset = 0;
                }
                else if (cc >= 192)
                { // 0xC0
                    len -= 3;

                    byte byte1 = r.ReadByte(handle++);
                    byte byte2 = r.ReadByte(handle++);
                    byte byte3 = r.ReadByte(handle++);

                    numplain = (UInt32)(cc & 0x03);
                    numcopy = (((UInt32)(cc & 0x0c)) << 6) + 5 + byte3;
                    offset = (((UInt32)(cc & 0x10)) << 12) + (((UInt32)byte1) << 8) + byte2;
                }
                else if (cc >= 128)
                { // 0x80
                    len -= 2;

                    byte byte1 = r.ReadByte(handle++);
                    byte byte2 = r.ReadByte(handle++);

                    numplain = ((UInt32)(byte1 & 0xc0)) >> 6;
                    numcopy = ((UInt32)(cc & 0x3f)) + 4;
                    offset = (((UInt32)(byte1 & 0x3f)) << 8) + byte2;
                }
                else {
                    len -= 1;

                    byte byte1 = r.ReadByte(handle++);

                    numplain = (UInt32)(cc & 0x03);
                    numcopy = (((UInt32)(cc & 0x1c)) >> 2) + 3;
                    offset = (((UInt32)(cc & 0x60)) << 3) + byte1;
                }

                if (numplain > 0)
                {
                    byte[] buf = new byte[numplain];
                    r.ReadArray(handle, buf, 0, buf.Length);
                    handle += numplain;
                    len -= numplain;
                    w.WriteArray(answerlen, buf, 0, buf.Length);
                    answerlen += numplain;
                }

                if (numcopy > 0)
                {
                    long fromoffset = answerlen - (offset + 1);  // 0 == last char
                    for (int i = 0; i < numcopy; i++)
                        w.Write(answerlen++, w.ReadByte(fromoffset++));
                }
            }

            return answerlen;
        }
    }

}
