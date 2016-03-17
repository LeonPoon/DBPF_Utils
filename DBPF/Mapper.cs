using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DBPF
{

    public class Mapper
    {
        public static readonly TypeGroupInstance TGI_DIR_FILE = new TypeGroupInstance();
        static Mapper()
        {
            TGI_DIR_FILE._typeId = 0xe86b1eef;
            TGI_DIR_FILE._groupId = 0xe86b1eef;
            TGI_DIR_FILE._instanceId = 0x286b1f03;
        }

        public static DBPFile map(MemoryMappedFile mmf)
        {
            Header header;
            using (var accessor = mmf.CreateViewAccessor(0, Marshal.SizeOf(typeof(Header))))
                accessor.Read(0, out header);

            Console.WriteLine("dbpfVer: {0}.{1}, indexFirstEntryOffset: {2}, indexOffset: {3}", header.majorVer, header.minorVer, header.indexFirstEntryOffset, header.indexOffset);
            Console.WriteLine("indexMajorVer: {0}, indexMinorVer: {1} ({3}), indexSize: {4}, indexEntryCount: {2}", header.indexMajorVer, header.indexMinorVer, header.indexEntryCount,
                header.indexMinorVer == 1 ? "7.0" : header.indexMinorVer == 2 ? "7.1" : "?",
                header.indexSize
                );

            Ver ver = header.indexMinorVer == 1 ? (Ver)new Ver71() : new Ver70();

            IndexTableEntry[] indexTableEntries = new IndexTableEntry[header.indexEntryCount];

            using (var accessor = mmf.CreateViewAccessor(header.indexFirstEntryOffset, header.indexSize))
            {
                var entryAccessor = ver.getIndexTableEntryAccessor();
                for (int i = 0; i < indexTableEntries.Length; i++)
                    indexTableEntries[i] = entryAccessor.read(accessor, i);
            }

            TGIObjects<IndexTableEntry> indexTable = new TGIObjects<IndexTableEntry>(indexTableEntries);

            IndexTableEntry dirEntry;
            try
            {
                dirEntry = indexTable[TGI_DIR_FILE];
            }
            catch (KeyNotFoundException)
            {
                dirEntry = null;
            }

            TGIObjects<DBDirectoryEntry> dbDirEntries;

            if (dirEntry == null)
                dbDirEntries = new TGIObjects<DBDirectoryEntry>();
            else
                using (var accessor = mmf.CreateViewAccessor(dirEntry.fileOffset, dirEntry.size))
                {
                    var entryAccessor = ver.getDBDirectoryEntryAccessor();
                    int len = (int)(dirEntry.size / entryAccessor.MarshalSize);
                    DBDirectoryEntry[] entries = new DBDirectoryEntry[len];
                    for (int i = 0; i < len; i++)
                        entries[i] = entryAccessor.read(accessor, i);
                    dbDirEntries = new TGIObjects<DBDirectoryEntry>(entries);
                }

            return new DBPFile(header, indexTable, dbDirEntries);
        }
    }
}
