using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DBPF
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public unsafe struct Header
    {
        public fixed byte _identifier[4];

        public UInt32 majorVer;
        public UInt32 minorVer;

        public UInt32 unknown1;
        public UInt32 unknown2;
        public UInt32 unknown3;

        public UInt32 dateCreated;
        public UInt32 dateModified;

        public UInt32 indexMajorVer;
        public UInt32 indexEntryCount;
        public UInt32 indexFirstEntryOffset;
        public UInt32 indexSize;

        public UInt32 holeEntryCount;
        public UInt32 holeOffset;
        public UInt32 holeSize;

        public UInt32 indexMinorVer;
        public UInt32 indexOffset;

        public UInt32 unknown4;

        public fixed byte reserved[24];

        public string identifier
        {
            get
            {
                byte[] bytes = new byte[4];
                fixed (byte* charPtr = _identifier)
                {
                    for (int i = 0; i < bytes.Length; i++)
                        bytes[i] = charPtr[i];
                }
                return Encoding.ASCII.GetString(bytes);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct TypeGroupInstance
    {
        public UInt32 _typeId;
        public UInt32 _groupId;
        public UInt32 _instanceId;
        UInt32 typeId { get { return _typeId; } }
        UInt32 groupId { get { return _groupId; } }
        UInt32 instanceId { get { return _instanceId; } }

        public TypeGroupInstance(UInt32 typeId, UInt32 groupId, UInt32 instanceId)
        {
            _typeId = typeId;
            _groupId = groupId;
            _instanceId = instanceId;
        }

        public override string ToString()
        {
            return String.Format("typeId={0:X8},groupId={1:X8},instanceId={2:X8}", _typeId, _groupId, _instanceId);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TypeGroupInstance))
                return false;
            TypeGroupInstance other = (TypeGroupInstance)obj;
            return other.typeId == typeId && other.groupId == groupId && other.instanceId == instanceId;
        }

        public override int GetHashCode()
        {
            return _typeId.GetHashCode() | _groupId.GetHashCode() | _instanceId.GetHashCode();
        }
    }

    public interface HasTGI
    {
        TypeGroupInstance tgi { get; }
    }

    public interface IndexTableEntry : HasTGI
    {
        UInt32 typeId { get; }
        UInt32 groupId { get; }
        UInt32 instanceId { get; }
        UInt32 instanceId2 { get; }
        UInt32 fileOffset { get; }
        UInt32 size { get; }
        bool hasInstanceId2 { get; }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct IndexTableEntry70 : IndexTableEntry
    {
        public TypeGroupInstance _tgi;
        public UInt32 _fileOffset;
        public UInt32 _size;

        TypeGroupInstance HasTGI.tgi { get { return _tgi; } }
        UInt32 IndexTableEntry.typeId { get { return _tgi._typeId; } }
        UInt32 IndexTableEntry.groupId { get { return _tgi._groupId; } }
        UInt32 IndexTableEntry.instanceId { get { return _tgi._instanceId; } }
        UInt32 IndexTableEntry.instanceId2 { get { return UInt32.MaxValue; } }
        UInt32 IndexTableEntry.fileOffset { get { return _fileOffset; } }
        UInt32 IndexTableEntry.size { get { return _size; } }
        bool IndexTableEntry.hasInstanceId2 { get { return false; } }

        public override string ToString()
        {
            return String.Format("{0},offset={1},size={2}", _tgi, _fileOffset, _size);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct IndexTableEntry71 : IndexTableEntry
    {
        public TypeGroupInstance _tgi;
        public UInt32 _instanceId2;
        public UInt32 _fileOffset;
        public UInt32 _size;

        TypeGroupInstance HasTGI.tgi { get { return _tgi; } }
        UInt32 IndexTableEntry.typeId { get { return _tgi._typeId; } }
        UInt32 IndexTableEntry.groupId { get { return _tgi._groupId; } }
        UInt32 IndexTableEntry.instanceId { get { return _tgi._instanceId; } }
        UInt32 IndexTableEntry.instanceId2 { get { return _instanceId2; } }
        UInt32 IndexTableEntry.fileOffset { get { return _fileOffset; } }
        UInt32 IndexTableEntry.size { get { return _size; } }
        bool IndexTableEntry.hasInstanceId2 { get { return true; } }

        public override string ToString()
        {
            return String.Format("{0},instanceId2={1:X8},offset={2},size={3}", _tgi, _instanceId2, _fileOffset, _size);
        }
    }
    public interface DBDirectoryEntry : HasTGI
    {
        UInt32 typeId { get; }
        UInt32 groupId { get; }
        UInt32 instanceId { get; }
        UInt32 instanceId2 { get; }
        UInt32 size { get; }
        bool hasInstanceId2 { get; }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DBDirectoryEntry70 : DBDirectoryEntry
    {
        public TypeGroupInstance _tgi;
        public UInt32 _size;

        TypeGroupInstance HasTGI.tgi { get { return _tgi; } }
        UInt32 DBDirectoryEntry.typeId { get { return _tgi._typeId; } }
        UInt32 DBDirectoryEntry.groupId { get { return _tgi._groupId; } }
        UInt32 DBDirectoryEntry.instanceId { get { return _tgi._instanceId; } }
        UInt32 DBDirectoryEntry.instanceId2 { get { return UInt32.MaxValue; } }
        UInt32 DBDirectoryEntry.size { get { return _size; } }
        bool DBDirectoryEntry.hasInstanceId2 { get { return false; } }

        public override string ToString()
        {
            return String.Format("{0},size={2}", _tgi, _size);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DBDirectoryEntry71 : DBDirectoryEntry
    {
        public TypeGroupInstance _tgi;
        public UInt32 _instanceId2;
        public UInt32 _size;

        TypeGroupInstance HasTGI.tgi { get { return _tgi; } }
        UInt32 DBDirectoryEntry.typeId { get { return _tgi._typeId; } }
        UInt32 DBDirectoryEntry.groupId { get { return _tgi._groupId; } }
        UInt32 DBDirectoryEntry.instanceId { get { return _tgi._instanceId; } }
        UInt32 DBDirectoryEntry.instanceId2 { get { return _instanceId2; } }
        UInt32 DBDirectoryEntry.size { get { return _size; } }
        bool DBDirectoryEntry.hasInstanceId2 { get { return true; } }

        public override string ToString()
        {
            return String.Format("{0},instanceId2={1:X8},size={3}", _tgi, _instanceId2, _size);
        }
    }
}
