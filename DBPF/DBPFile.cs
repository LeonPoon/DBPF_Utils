using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    }

}
