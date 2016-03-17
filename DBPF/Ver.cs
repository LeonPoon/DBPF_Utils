using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DBPF
{

    internal interface VersionedStructAccessor<I>
    {
        int MarshalSize { get; }
        I read(MemoryMappedViewAccessor accessor, int pos);
    }

    internal class VersionedStructAccessor<T, I> : VersionedStructAccessor<I>, IDisposable
        where T : struct, I
    {
        public int MarshalSize { get { return Marshal.SizeOf(typeof(T)); } }

        public void Dispose()
        {
            // nothing actually
        }

        public I read(MemoryMappedViewAccessor accessor, int pos)
        {
            T val;
            accessor.Read(pos * MarshalSize, out val);
            return val;
        }
    }

    internal abstract class Ver
    {
        private readonly VersionedStructAccessor<IndexTableEntry> IndexTableEntryAccessor;
        private readonly VersionedStructAccessor<DBDirectoryEntry> DBDirectoryEntryAccessor;

        protected Ver(
            VersionedStructAccessor<IndexTableEntry> IndexTableEntryAccessor,
            VersionedStructAccessor<DBDirectoryEntry> DBDirectoryEntryAccessor)
        {
            this.IndexTableEntryAccessor = IndexTableEntryAccessor;
            this.DBDirectoryEntryAccessor = DBDirectoryEntryAccessor;
        }

        public VersionedStructAccessor<IndexTableEntry> getIndexTableEntryAccessor() { return IndexTableEntryAccessor; }
        public VersionedStructAccessor<DBDirectoryEntry> getDBDirectoryEntryAccessor() { return DBDirectoryEntryAccessor; }
    }

    internal class Ver70 : Ver
    {

        public Ver70() : base(
            new VersionedStructAccessor<IndexTableEntry70, IndexTableEntry>(),
            new VersionedStructAccessor<DBDirectoryEntry70, DBDirectoryEntry>())
        {
        }
    }

    internal class Ver71 : Ver
    {

        public Ver71() : base(
            new VersionedStructAccessor<IndexTableEntry71, IndexTableEntry>(),
            new VersionedStructAccessor<DBDirectoryEntry71, DBDirectoryEntry>())
        {
        }
    }
}
