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

using GenUtils;
using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace Sc4Network
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NetworkIndexHeaderStruct : Readable
    {
        public UInt32 fileSz; // DWORD Subfile Size in bytes
        public UInt32 crc; // DWORD   CRC
        public UInt32 ptr1; // DWORD   Memory address
        public UInt16 verMajor; // WORD Major Version(0x0007 for built cities, but 0x0003 for Maxis' own, unbuilt cities)
        public UInt32 tilesInCity; //  City Tile Count  (4096, 16384 or 65536 depending on city size)
        // UInt32 tilesInNetwork; // DWORD Count of Network Tiles(0 for a new city)

        public long Read(MemoryMappedViewAccessor accessor, long pos)
        {
            fileSz = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            crc = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            ptr1 = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            verMajor = accessor.ReadUInt16(pos); pos += Marshal.SizeOf(typeof(UInt16));
            tilesInCity = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            return pos;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NetworkIndexTileHeaderStruct : Readable
    {
        public UInt32 tileNumber; // Tile Number(See Appendix 1.1 for more information)
        public UInt32 linkPtr; // DWORD   Memory address
        public UInt32 subfileTypeId; //  DWORD Link: Subfile Type ID (C9C05C6E or CA16374F or 49c1a034)
        //public UInt32 blockCount; //  DWORD    Count of blocks(either 0 or 10)

        public long Read(MemoryMappedViewAccessor accessor, long pos)
        {
            tileNumber = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            linkPtr = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            subfileTypeId = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            //blockCount = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            return pos;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NetworkIndexTileSubBlockHeaderStruct : Readable
    {
        public UInt32 blockNumber; // (first one is 0, last one is 9)
        //public UInt32 byte8Count; // (anything between 0-16. Number of byte blocks)
        // BYTE×8   	Unknown block of bytes.Small numbers< 10

        public long Read(MemoryMappedViewAccessor accessor, long pos)
        {
            blockNumber = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            //byte8Count = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            return pos;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NetworkIndexTileDetailStruct : Readable
    {
        public byte unknownByte; //   (always 0x00)
        //public UInt32 unknown1; // (always 0x00000000)
        public UInt32 unknown2; // (Seen 0 and 2)
        public UInt32 unknown3; // (Only seen 2)
        public UInt32 unknown4; // (Seen 0 and 4)
        public NetworkIndexTileUnknown4Struct unknownBlock1;
        public NetworkIndexTileUnknown4Struct unknownBlock2;
        public NetworkIndexTileUnknown4Struct unknownBlock3;
        public NetworkIndexTileUnknown4Struct unknownBlock4;
        public UInt16 unknownShort1; // (always 0)
        //public UInt32 blocksCount; // (always 2)

        public long Read(MemoryMappedViewAccessor accessor, long pos)
        {
            unknownByte = accessor.ReadByte(pos++);
            //unknown1 = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            unknown2 = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            unknown3 = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            unknown4 = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            pos = unknownBlock1.Read(accessor, pos);
            pos = unknownBlock2.Read(accessor, pos);
            pos = unknownBlock3.Read(accessor, pos);
            pos = unknownBlock4.Read(accessor, pos);
            unknownShort1 = accessor.ReadUInt16(pos); pos += Marshal.SizeOf(typeof(UInt16));
            //blocksCount = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            return pos;
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NetworkIndexTileUnknown2FStruct : TreeNodeProvider, Readable
    {
        public float unknownFloat1;
        public float unknownFloat2;

        public T[] treeNodes<T>(TreeNodeMaker<T> maker)
        {
            return new T[] {
                maker(string.Format("unknownFloat1: {0}", unknownFloat1), null),
                maker(string.Format("unknownFloat2: {0}", unknownFloat2), null),
            };
        }

        public long Read(MemoryMappedViewAccessor accessor, long pos)
        {
            unknownFloat1 = accessor.ReadSingle(pos); pos += Marshal.SizeOf(typeof(float));
            unknownFloat2 = accessor.ReadSingle(pos); pos += Marshal.SizeOf(typeof(float));
            return pos;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NetworkIndexTileUnknownNStruct : TreeNodeProvider, Readable
    {
        public UInt32 blockNumber;
        public float unknownFloat;
        public UInt16 unknownShort;

        public T[] treeNodes<T>(TreeNodeMaker<T> maker)
        {
            return new T[] {
                maker(string.Format("blockNumber: {0}", blockNumber), null),
                maker(string.Format("unknownFloat: {0}", unknownFloat), null),
                maker(string.Format("unknownShort: {0}", unknownShort), null),
            };
        }

        public long Read(MemoryMappedViewAccessor accessor, long pos)
        {
            blockNumber = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            unknownFloat = accessor.ReadSingle(pos); pos += Marshal.SizeOf(typeof(float));
            unknownShort = accessor.ReadUInt16(pos); pos += Marshal.SizeOf(typeof(UInt16));
            return pos;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NetworkIndexTileUnknown4Struct : TreeNodeProvider, Readable
    {
        public float unknownFloat; // (Small values, e.g. 0.0, 2.0, 5.0, 6.0, 8.0)
        public UInt16 unknownShort1; // (0 or 1)
        public UInt16 unknownShort2; // (0 or 1)
        public UInt16 unknownShort3; // (0 or 1)
        public UInt16 unknownShort4; // (0 or 1)

        public long Read(MemoryMappedViewAccessor accessor, long pos)
        {
            unknownFloat = accessor.ReadSingle(pos); pos += Marshal.SizeOf(typeof(float));
            unknownShort1 = accessor.ReadUInt16(pos); pos += Marshal.SizeOf(typeof(UInt16));
            unknownShort2 = accessor.ReadUInt16(pos); pos += Marshal.SizeOf(typeof(UInt16));
            unknownShort3 = accessor.ReadUInt16(pos); pos += Marshal.SizeOf(typeof(UInt16));
            unknownShort4 = accessor.ReadUInt16(pos); pos += Marshal.SizeOf(typeof(UInt16));
            return pos;
        }

        public T[] treeNodes<T>(TreeNodeMaker<T> maker)
        {
            return new T[] {
                maker(string.Format("unknown: {0}", unknownFloat), null),
                maker(string.Format("unknown: {0}", unknownShort1), null),
                maker(string.Format("unknown: {0}", unknownShort2), null),
                maker(string.Format("unknown: {0}", unknownShort3), null),
                maker(string.Format("unknown: {0}", unknownShort4), null),
            };
        }
    }

    public interface INetworkIndexBlockStruct : TreeNodeProvider, Readable
    {
        UInt32 linkPtr { get; } // DWORD   Memory address
        UInt32 subfileTypeId { get; } //  DWORD Link: Subfile Type ID
        byte unknownByte { get; } // (only seen 0x00)
        UInt32 unknown { get; } //(only seen 0x00000000)
        NetworkIndexBlockUnknown4Struct unknown1 { get; }
        NetworkIndexBlockUnknown4Struct unknown2 { get; }
        NetworkIndexBlockUnknown4Struct unknown3 { get; }
        NetworkIndexBlockUnknown4Struct unknown4 { get; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NetworkIndexBlockPre7Struct : INetworkIndexBlockStruct
    {
        private UInt32 _linkPtr; // DWORD   Memory address
        private UInt32 _subfileTypeId; //  DWORD Link: Subfile Type ID
        private NetworkIndexBlockUnknown4Struct _unknown1;
        private NetworkIndexBlockUnknown4Struct _unknown2;
        private NetworkIndexBlockUnknown4Struct _unknown3;
        private NetworkIndexBlockUnknown4Struct _unknown4;

        public uint linkPtr { get { return _linkPtr; } }
        public uint subfileTypeId { get { return _subfileTypeId; } }
        public byte unknownByte { get { return 0; } }
        public uint unknown { get { return 0; } }
        public NetworkIndexBlockUnknown4Struct unknown1 { get { return _unknown1; } }
        public NetworkIndexBlockUnknown4Struct unknown2 { get { return _unknown2; } }
        public NetworkIndexBlockUnknown4Struct unknown3 { get { return _unknown3; } }
        public NetworkIndexBlockUnknown4Struct unknown4 { get { return _unknown4; } }

        public long Read(MemoryMappedViewAccessor accessor, long pos)
        {
            _linkPtr = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            _subfileTypeId = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            pos = _unknown1.Read(accessor, pos);
            pos = _unknown2.Read(accessor, pos);
            pos = _unknown3.Read(accessor, pos);
            pos = _unknown4.Read(accessor, pos);
            return pos;
        }

        public T[] treeNodes<T>(TreeNodeMaker<T> maker)
        {
            return new T[] {
                maker(string.Format("unknown: {0:X8}", _linkPtr), null),
                maker(string.Format("unknown: {0:X8}", _subfileTypeId), null),
                maker(string.Format("unknown: {0}", 0), _unknown1.treeNodes(maker)),
                maker(string.Format("unknown: {0}", 1), _unknown2.treeNodes(maker)),
                maker(string.Format("unknown: {0}", 2), _unknown3.treeNodes(maker)),
                maker(string.Format("unknown: {0}", 3), _unknown4.treeNodes(maker)),
            };
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NetworkIndexBlockV7Struct : INetworkIndexBlockStruct
    {
        private UInt32 _linkPtr; // DWORD   Memory address
        private UInt32 _subfileTypeId; //  DWORD Link: Subfile Type ID
        private byte _unknownByte; // (only seen 0x00)
        private UInt32 _unknown; //(only seen 0x00000000)
        private NetworkIndexBlockUnknown4Struct _unknown1;
        private NetworkIndexBlockUnknown4Struct _unknown2;
        private NetworkIndexBlockUnknown4Struct _unknown3;
        private NetworkIndexBlockUnknown4Struct _unknown4;

        public uint linkPtr { get { return _linkPtr; } }
        public uint subfileTypeId { get { return _subfileTypeId; } }
        public byte unknownByte { get { return _unknownByte; } }
        public uint unknown { get { return _unknown; } }
        public NetworkIndexBlockUnknown4Struct unknown1 { get { return _unknown1; } }
        public NetworkIndexBlockUnknown4Struct unknown2 { get { return _unknown2; } }
        public NetworkIndexBlockUnknown4Struct unknown3 { get { return _unknown3; } }
        public NetworkIndexBlockUnknown4Struct unknown4 { get { return _unknown4; } }

        public long Read(MemoryMappedViewAccessor accessor, long pos)
        {
            _linkPtr = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            _subfileTypeId = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            _unknownByte = accessor.ReadByte(pos++);
            _unknown = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            pos = _unknown1.Read(accessor, pos);
            pos = _unknown2.Read(accessor, pos);
            pos = _unknown3.Read(accessor, pos);
            pos = _unknown4.Read(accessor, pos);
            return pos;
        }

        public T[] treeNodes<T>(TreeNodeMaker<T> maker)
        {
            return new T[] {
                maker(string.Format("unknown: {0:X8}", _linkPtr), null),
                maker(string.Format("unknown: {0:X8}", _subfileTypeId), null),
                maker(string.Format("unknown: {0:X}", _unknownByte), null),
                maker(string.Format("unknown: {0:X8}", _unknown), null),
                maker(string.Format("unknown: {0}", 0), _unknown1.treeNodes(maker)),
                maker(string.Format("unknown: {0}", 1), _unknown2.treeNodes(maker)),
                maker(string.Format("unknown: {0}", 2), _unknown3.treeNodes(maker)),
                maker(string.Format("unknown: {0}", 3), _unknown4.treeNodes(maker)),
            };
        }
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NetworkIndexBlockUnknown4Struct : TreeNodeProvider, Readable
    {
        public UInt32 unknown32; //(Either 0, 1, 2 or 3)
        public bool unknownBool; // (Either 0 or 1, false or true)
        public float unknownF1; //(small value)
        public float unknownF2; //(small value)
        public float unknownF3; //(small value)

        public T[] treeNodes<T>(TreeNodeMaker<T> maker)
        {
            return new T[] {
                maker(string.Format("unknown: {0}", unknown32), null),
                maker(string.Format("unknown: {0}", unknownBool), null),
                maker(string.Format("unknown: {0}", unknownF1), null),
                maker(string.Format("unknown: {0}", unknownF2), null),
                maker(string.Format("unknown: {0}", unknownF3), null),
            };
        }

        public long Read(MemoryMappedViewAccessor accessor, long pos)
        {
            unknown32 = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            unknownBool = accessor.ReadByte(pos++) != 0;
            unknownF1 = accessor.ReadSingle(pos); pos += Marshal.SizeOf(typeof(float));
            unknownF2 = accessor.ReadSingle(pos); pos += Marshal.SizeOf(typeof(float));
            unknownF3 = accessor.ReadSingle(pos); pos += Marshal.SizeOf(typeof(float));
            return pos;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NetworkIndexTrailerStruct : Readable
    {
        //public UInt32 unknown1; //(only seen 0x00000000)
        public UInt32 unknown2; //(only seen 0x00000000)
        public UInt32 unknown3; //(only seen 0x00000000)
        public UInt32 tileX; // (63, 127 or 255 for a new city, smaller for built up cities)
        public UInt32 tileZ; //(63, 127 or 255 for a new city, smaller for built up cities)
        public float unknown4; //(Small value. 0.00 for a new city)
        public bool Unknown5; // (always 1, true)
        public float unknown6; //(Small value. 0.00 for a new city)
        public bool Unknown7; // (always 1, true)
        // public UInt32 unknown; //(Does not appear if version = 0x0003.Always 0x00000000 if version = 0x0007)

        public long Read(MemoryMappedViewAccessor accessor, long pos)
        {
            //unknown1 = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            unknown2 = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            unknown3 = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            tileX = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            tileZ = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            unknown4 = accessor.ReadSingle(pos); pos += Marshal.SizeOf(typeof(float));
            Unknown5 = accessor.ReadByte(pos++) != 0;
            unknown6 = accessor.ReadSingle(pos); pos += Marshal.SizeOf(typeof(float));
            Unknown7 = accessor.ReadByte(pos++) != 0;
            return pos;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NetworkIndexUnknownBlock4IStruct : TreeNodeProvider, Readable
    {
        public UInt32 unknown1;
        public UInt32 unknown2;
        public UInt32 ptr;
        public UInt32 subfileTypeId;

        public long Read(MemoryMappedViewAccessor accessor, long pos)
        {
            unknown1 = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            unknown2 = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            ptr = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            subfileTypeId = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            return pos;
        }

        public T[] treeNodes<T>(TreeNodeMaker<T> maker)
        {
            return new T[] {
                maker(string.Format("unknown1: {0:X8}", unknown1), null),
                maker(string.Format("unknown2: {0:X8}", unknown2), null),
                maker(string.Format("ptr: {0:X8}", ptr), null),
                maker(string.Format("subfileTypeId: {0:X8}", subfileTypeId), null),
            };
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NetworkIndexUnknownBlockStruct : TreeNodeProvider, Readable
    {
        public UInt32 ptr;
        public UInt32 subfileTypeId;
        public UInt32 unknown1;
        public UInt32 unknown2;
        public UInt32 unknown3;
        public UInt32 unknown4;
        public UInt32 unknown5;
        public UInt32 unknown6;
        public UInt32 unknown7;
        public UInt32 unknown8;

        public long Read(MemoryMappedViewAccessor accessor, long pos)
        {
            ptr = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            subfileTypeId = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            unknown1 = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            unknown2 = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            unknown3 = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            unknown4 = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            unknown5 = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            unknown6 = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            unknown7 = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            unknown8 = accessor.ReadUInt32(pos); pos += Marshal.SizeOf(typeof(UInt32));
            return pos;
        }

        public T[] treeNodes<T>(TreeNodeMaker<T> maker)
        {
            return new T[] {
                maker(string.Format("ptr: {0:X8}", ptr), null),
                maker(string.Format("subfileTypeId: {0:X8}", subfileTypeId), null),
                maker("32 unknown bytes", null),
            };
        }
    }
}
