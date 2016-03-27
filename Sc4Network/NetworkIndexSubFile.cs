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
    public class NetworkIndexTileSubBlock : TreeNodeProvider, ReadResultN
    {
        public const int BLOCK_SZ = 8;
        public readonly uint index;
        public readonly long headerStart, headerSz;
        public readonly NetworkIndexTileSubBlockHeaderStruct header;
        public readonly byte[,] byteBlocks;
        private readonly long sz;

        public long Pos { get { return headerStart; } }
        public long Bytes { get { return 0; } }
        public long Sz { get { return sz; } }
        public uint Index { get { return index; } }

        public NetworkIndexTileSubBlock(uint index, NetworkIndexTileSubBlockHeaderStruct header, long headerStart, long headerSz, byte[,] byteBlocks, long sz)
        {
            this.index = index;
            this.headerStart = headerStart;
            this.headerSz = headerSz;
            this.header = header;
            this.byteBlocks = byteBlocks ?? new byte[0, BLOCK_SZ];
            this.sz = sz;
        }

        public T[] treeNodes<T>(TreeNodeMaker<T> maker)
        {
            var bt = new T[byteBlocks.GetLength(0)];
            for (int k = 0; k < bt.Length; k++)
            {
                var bbt = new T[BLOCK_SZ];
                for (int i = 0; i < BLOCK_SZ; i++)
                    bbt[i] = maker(string.Format("{0}: {1}", i, byteBlocks[k, i]), null);
                bt[k] = maker(k.ToString(), bbt);
            }

            return new T[] {
                maker(string.Format("blockNumber: {0}", header.blockNumber), null),
                maker(string.Format("byte8Count: {0}", byteBlocks.GetLength(0)), bt),
            };
        }

        internal static NetworkIndexTileSubBlock instantiate(MemoryMappedViewAccessor accessor, uint index, long start, out long pos)
        {
            NetworkIndexTileSubBlockHeaderStruct __header = new NetworkIndexTileSubBlockHeaderStruct();
            byte[,] byteBlocks;

            pos = __header.Read(accessor, start); long headerSz = pos - start;
            byteBlocks = new byte[accessor.ReadUInt32(pos), BLOCK_SZ]; pos += Marshal.SizeOf(typeof(UInt32));

            for (int k = 0; k < byteBlocks.GetLength(0); k++)
                for (int i = 0; i < BLOCK_SZ; i++)
                    byteBlocks[k, i] = accessor.ReadByte(pos++);
            long sz = pos - start;

            return new NetworkIndexTileSubBlock(index, __header, start, headerSz, byteBlocks, sz);
        }

        public ReadResult[] ReadResultComponents
        {
            get
            {
                var r = new ReadResult[byteBlocks.GetLength(0)];
                for (int i = 0; i < r.Length; i++)
                    r[i] = new ReadResultWrapper(headerStart + headerSz + Marshal.SizeOf(typeof(UInt32)) + i * byteBlocks.GetLength(1), byteBlocks.GetLength(1), string.Format("NetworkIndexTileSubBlock8Bytes[{0}]", i));
                return new ReadResult[] {
                    new ReadResultWrapper(headerStart, headerSz, "NetworkIndexTileSubBlockHeader"),
                    new ReadResultWrapper(headerStart+ headerSz, Marshal.SizeOf(typeof(UInt32)), Marshal.SizeOf(typeof(UInt32)) + r.Length * byteBlocks.GetLength(1), "byteBlocks", r),
                };
            }
        }

        public string getName()
        {
            return string.Format("NetworkIndexTileSubBlock[{0}]", Index);
        }
    }

    public class NetworkIndexTileUnknown2F : StructTreeNodeProviderReadResult<NetworkIndexTileUnknown2FStruct>
    {
        public NetworkIndexTileUnknown2F(uint index) : base(string.Format("NetworkIndexTileUnknown2F[{0}]", index)) { }
    }

    public class NetworkIndexTileUnknownN : StructTreeNodeProviderReadResult<NetworkIndexTileUnknownNStruct>
    {
        public NetworkIndexTileUnknownN(uint index) : base(string.Format("NetworkIndexTileUnknownN[{0}]", index)) { }
    }

    public class NetworkIndexTile : TreeNodeProvider, ReadResultN
    {
        private readonly uint index;
        public readonly long tileStart, headerSz;
        public readonly NetworkIndexTileHeaderStruct header;
        public readonly NetworkIndexTileSubBlock[] blocks;
        public readonly long detailStart, detailSz;
        public readonly NetworkIndexTileDetailStruct detail;
        public readonly NetworkIndexTileUnknown2F[] unknown2FloatBlocks;
        public readonly NetworkIndexTileUnknownN[] unknownBlocks;
        private readonly long sz;

        public long Bytes { get { return 0; } }
        public uint Index { get { return index; } }
        public long Pos { get { return tileStart; } }
        public long Sz { get { return sz; } }

        public NetworkIndexTile(uint index,
            long tileStart, long headerSz,
            NetworkIndexTileHeaderStruct header,
            NetworkIndexTileSubBlock[] blocks,
            NetworkIndexTileUnknown2F[] unknown2FloatBlocks,
            long detailStart, long detailSz,
            NetworkIndexTileDetailStruct detail,
            NetworkIndexTileUnknownN[] unknownBlocks,
            long end)
        {
            this.index = index;

            this.tileStart = tileStart;
            this.header = header;
            this.headerSz = headerSz;

            this.blocks = blocks ?? new NetworkIndexTileSubBlock[0];
            this.unknown2FloatBlocks = unknown2FloatBlocks ?? new NetworkIndexTileUnknown2F[0];

            this.detailStart = detailStart;
            this.detail = detail;
            this.detailSz = detailSz;

            this.unknownBlocks = unknownBlocks ?? new NetworkIndexTileUnknownN[0];

            this.sz = end - tileStart;
        }

        public ReadResult[] ReadResultComponents
        {
            get
            {
                ReadResult r;
                return new ReadResult[]{
                    r = new ReadResultWrapper(tileStart, headerSz, "NetworkIndexTileHeader"),
                    r = ReadResultWrapper.wrap(r, blocks, "NetworkIndexTileSubBlocks"),
                    r = ReadResultWrapper.wrap(r, unknown2FloatBlocks, "NetworkIndexTileUnknowns2F"),
                    r = new ReadResultWrapper(detailStart, detailSz, "NetworkIndexTileDetail"),
                    r = ReadResultWrapper.wrap(r, unknownBlocks, "NetworkIndexTileUnknownsN"),
                };
            }
        }

        public T[] treeNodes<T>(TreeNodeMaker<T> maker)
        {
            return new T[] {
                maker(string.Format("tileNumber: {0:X8}", header.tileNumber), null),
                maker(string.Format("linkPtr: {0:X8}", header.linkPtr), null),
                maker(string.Format("subfileTypeId: {0:X8}", header.subfileTypeId), null),
                maker(string.Format("blocks: {0}", blocks.Length), RecursiveTreeNode.recurse(maker, blocks)),
                maker(string.Format("unknown2FloatBlocks: {0:X8}", unknown2FloatBlocks.Length), RecursiveTreeNode.recurse(maker, unknown2FloatBlocks)),
                maker(string.Format("unknownByte: {0:X}", detail.unknownByte), null),
                maker(string.Format("unknown: {0}", detail.unknown2), null),
                maker(string.Format("unknown: {0}", detail.unknown3), null),
                maker(string.Format("unknown: {0}", detail.unknown4), null),
                maker(string.Format("unknownBlock: {0}", 0), detail.unknownBlock1.treeNodes(maker)),
                maker(string.Format("unknownBlock: {0}", 1), detail.unknownBlock2.treeNodes(maker)),
                maker(string.Format("unknownBlock: {0}", 2), detail.unknownBlock3.treeNodes(maker)),
                maker(string.Format("unknownBlock: {0}", 3), detail.unknownBlock4.treeNodes(maker)),
                maker(string.Format("unknown: {0}", detail.unknownShort1), null),
                maker(string.Format("blocksCount: {0}", unknownBlocks.Length), RecursiveTreeNode.recurse(maker, unknownBlocks)),
            };
        }

        public static NetworkIndexTile instantiate(MemoryMappedViewAccessor accessor, uint i, long tileStart, out long pos)
        {
            NetworkIndexTileHeaderStruct _header = new NetworkIndexTileHeaderStruct();
            NetworkIndexTileSubBlock[] _blocks;
            NetworkIndexTileDetailStruct _detail = new NetworkIndexTileDetailStruct();
            NetworkIndexTileUnknown2F[] _blocks2F;
            NetworkIndexTileUnknownN[] _blocksN;

            pos = _header.Read(accessor, tileStart); long headerSz = pos - tileStart;

            _blocks = new NetworkIndexTileSubBlock[accessor.ReadUInt32(pos)]; pos += Marshal.SizeOf(typeof(UInt32));
            for (uint j = 0; j < _blocks.Length; j++)
                _blocks[j] = NetworkIndexTileSubBlock.instantiate(accessor, j, pos, out pos);

            _blocks2F = new NetworkIndexTileUnknown2F[accessor.ReadUInt32(pos)]; pos += Marshal.SizeOf(typeof(UInt32));
            for (int j = 0; j < _blocks2F.Length; j++)
                pos = (_blocks2F[j] = new NetworkIndexTileUnknown2F(i)).Read(accessor, pos);

            long detailStart = pos; pos = _detail.Read(accessor, pos); long detailSz = pos - detailStart;

            _blocksN = new NetworkIndexTileUnknownN[accessor.ReadUInt32(pos)]; pos += Marshal.SizeOf(typeof(UInt32));
            for (int j = 0; j < _blocksN.Length; j++)
                pos = (_blocksN[j] = new NetworkIndexTileUnknownN(i)).Read(accessor, pos);

            return new NetworkIndexTile(i, tileStart, headerSz, _header, _blocks, _blocks2F, detailStart, detailSz, _detail, _blocksN, pos);
        }

        public string getName()
        {
            return string.Format("NetworkIndexTile[{0}]", Index);
        }
    }

    public class NetworkIndexBlock : StructTreeNodeProviderReadResult<INetworkIndexBlockStruct>, INetworkIndexBlockStruct
    {
        public uint linkPtr { get { return value.linkPtr; } }
        public uint subfileTypeId { get { return value.subfileTypeId; } }
        public byte unknownByte { get { return value.unknownByte; } }
        public uint unknown { get { return value.unknown; } }
        public NetworkIndexBlockUnknown4Struct unknown1 { get { return value.unknown1; } }
        public NetworkIndexBlockUnknown4Struct unknown2 { get { return value.unknown2; } }
        public NetworkIndexBlockUnknown4Struct unknown3 { get { return value.unknown3; } }
        public NetworkIndexBlockUnknown4Struct unknown4 { get { return value.unknown4; } }

        public NetworkIndexBlock(uint i, INetworkIndexBlockStruct value) : base(string.Format("NetworkIndexBlock[{0}]", i), value) { }
    }

    public class NetworkIndexUnknownBlock4I : StructTreeNodeProviderReadResult<NetworkIndexUnknownBlock4IStruct>
    {
        public NetworkIndexUnknownBlock4I(uint index) : base(string.Format("NetworkIndexTileUnknown2F[{0}]", index)) { }
    }

    public class NetworkIndexUnknownBlock : StructTreeNodeProviderReadResult<NetworkIndexUnknownBlockStruct>
    {
        public NetworkIndexUnknownBlock(uint index) : base(string.Format("NetworkIndexUnknownBlock[{0}]", index)) { }
    }


    public class NetworkIndexSubFile : TreeNodeProvider, ReadResult
    {
        public readonly long headerStart, headerSz;
        public readonly NetworkIndexHeaderStruct header;

        public readonly NetworkIndexTile[] tiles;
        public readonly NetworkIndexBlock[] blocks;
        public readonly NetworkIndexUnknownBlock4I[] unknown4IBlocks;

        public readonly long trailerStart, trailerSz;
        public readonly NetworkIndexTrailerStruct trailer;

        public readonly NetworkIndexUnknownBlock[] unknownBlocks;

        public readonly long sz;

        public long Bytes { get { return 0; } }
        public long Pos { get { return headerStart; } }
        public long Sz { get { return sz; } }

        public NetworkIndexSubFile(long headerStart, NetworkIndexHeaderStruct header, long headerSz,
            NetworkIndexTile[] tiles,
            NetworkIndexBlock[] blocks,
            NetworkIndexUnknownBlock4I[] unknown4IBlocks,
            NetworkIndexTrailerStruct trailer, long trailerStart, long trailerSz,
            NetworkIndexUnknownBlock[] unknownBlocks,
            long sz)
        {
            this.headerStart = headerStart;
            this.header = header;
            this.headerSz = headerSz;
            this.tiles = tiles ?? new NetworkIndexTile[0];
            this.blocks = blocks ?? new NetworkIndexBlock[0];
            this.unknown4IBlocks = unknown4IBlocks ?? new NetworkIndexUnknownBlock4I[0];
            this.trailer = trailer;
            this.trailerStart = trailerStart;
            this.trailerSz = trailerSz;
            this.unknownBlocks = unknownBlocks;
            this.sz = sz;
        }

        public static NetworkIndexSubFile instantiate(MemoryMappedViewAccessor accessor, long start, out long pos)
        {
            NetworkIndexHeaderStruct header = new NetworkIndexHeaderStruct();
            NetworkIndexTile[] tiles;
            NetworkIndexBlock[] blocks;
            NetworkIndexUnknownBlock4I[] unknown4IBlocks;
            NetworkIndexTrailerStruct trailer = new NetworkIndexTrailerStruct();
            NetworkIndexUnknownBlock[] unknownBlocks;

            pos = header.Read(accessor, start); long headerSz = pos - start;

            switch (header.verMajor)
            {
                case 3:
                case 4:
                case 6:
                case 7:
                    break;
                default:
                    throw new ArgumentException(string.Format("unknown version: {0}", header.verMajor));
            }

            tiles = new NetworkIndexTile[accessor.ReadUInt32(pos)]; pos += Marshal.SizeOf(typeof(UInt32));
            for (uint i = 0; i < tiles.Length; i++)
                tiles[i] = NetworkIndexTile.instantiate(accessor, i, pos, out pos);
            long tilesEnd = pos;

            blocks = new NetworkIndexBlock[accessor.ReadUInt32(pos)]; pos += Marshal.SizeOf(typeof(UInt32));

            switch (header.verMajor)
            {
                case 3:
                case 4:
                case 6:
                    for (uint i = 0; i < blocks.Length; i++)
                        pos = (blocks[i] = new NetworkIndexBlock(i, new NetworkIndexBlockPre7Struct())).Read(accessor, pos, i);
                    break;
                default:
                    for (uint i = 0; i < blocks.Length; i++)
                        pos = (blocks[i] = new NetworkIndexBlock(i, new NetworkIndexBlockV7Struct())).Read(accessor, pos, i);
                    break;
            }

            unknown4IBlocks = new NetworkIndexUnknownBlock4I[accessor.ReadUInt32(pos)]; pos += Marshal.SizeOf(typeof(UInt32));
            for (uint i = 0; i < unknown4IBlocks.Length; i++)
                pos = (unknown4IBlocks[i] = new NetworkIndexUnknownBlock4I(i)).Read(accessor, pos);

            long trailerStart = pos;
            pos = trailer.Read(accessor, pos);
            long trailerSz = pos - trailerStart;

            switch (header.verMajor)
            {
                case 6:
                case 7:
                    unknownBlocks = new NetworkIndexUnknownBlock[accessor.ReadUInt32(pos)]; pos += Marshal.SizeOf(typeof(UInt32));
                    for (uint i = 0; i < unknownBlocks.Length; i++)
                        pos = (unknownBlocks[i] = new NetworkIndexUnknownBlock(i)).Read(accessor, pos);
                    break;
                default:
                    unknownBlocks = null;
                    break;
            }
            long sz = pos - start;

            NetworkIndexSubFile f =
                new NetworkIndexSubFile(start, header, headerSz, tiles, blocks, unknown4IBlocks, trailer, trailerStart, trailerSz, unknownBlocks, sz);
            return f;
        }

        public T[] treeNodes<T>(TreeNodeMaker<T> maker)
        {
            return new T[] {
                maker(string.Format("filesize: {0}", header.fileSz), null),
                maker(string.Format("crc: {0:X8}", header.crc), null),
                maker(string.Format("ptr1: {0:X8}", header.ptr1), null),
                maker(string.Format("verMajor: {0:X4}", header.verMajor), null),
                maker(string.Format("tiles: {0}", tiles.Length), RecursiveTreeNode.recurse(maker, tiles)),
                maker(string.Format("blocksV{0}: {1}",header.verMajor), RecursiveTreeNode.recurse(maker, blocks)),
                maker(string.Format("unknown4IBlocks: {0}", unknown4IBlocks.Length), RecursiveTreeNode.recurse(maker, unknown4IBlocks)),
                maker(string.Format("unknown: {0:X8}", trailer.unknown2), null),
                maker(string.Format("unknown: {0:X8}", trailer.unknown3), null),
                maker(string.Format("tileX: {0}", trailer.tileX), null),
                maker(string.Format("tileZ: {0}", trailer.tileZ), null),
                maker(string.Format("unknown: {0}", trailer.unknown4), null),
                maker(string.Format("unknown: {0}", trailer.Unknown5), null),
                maker(string.Format("unknown: {0}", trailer.unknown6), null),
                maker(string.Format("unknown: {0}", trailer.Unknown7), null),
                unknownBlocks == null? maker("unknownBlocks: null", null): maker(string.Format("unknownBlocks: {0}", unknownBlocks.Length), RecursiveTreeNode.recurse(maker, unknownBlocks)),
            };
        }

        public ReadResult[] ReadResultComponents
        {
            get
            {
                ReadResult r;
                return new ReadResult[] {
                    r = new ReadResultWrapper(headerStart, headerSz, "NetworkIndexHeader"),
                    r = ReadResultWrapper.wrap(r,tiles, "NetworkIndexTiles"),
                    r = ReadResultWrapper.wrap(r,blocks, "NetworkIndexBlocks"),
                    r = ReadResultWrapper.wrap(r,unknown4IBlocks, "NetworkIndexUnknownBlocks4I"),
                    r = new ReadResultWrapper(trailerStart, trailerSz, "NetworkIndexTrailer"),
                    r = ReadResultWrapper.wrap(r,unknownBlocks, "NetworkIndexUnknownBlocks"),
                };
            }
        }

        public string getName()
        {
            return null;
        }
    }
}
