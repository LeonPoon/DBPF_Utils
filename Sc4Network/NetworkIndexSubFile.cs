using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace Sc4Network
{
    public delegate T TreeNodeMaker<T>(string text, T[] children);

    public class NetworkIndexTileSubBlock : TreeNodeProvider
    {
        public const int BLOCK_SZ = 8;
        public readonly NetworkIndexTileSubBlockHeader header;
        public readonly byte[,] byteBlocks;

        public NetworkIndexTileSubBlock(NetworkIndexTileSubBlockHeader header, byte[,] byteBlocks)
        {
            this.header = header;
            this.byteBlocks = byteBlocks ?? new byte[0, BLOCK_SZ];
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
                maker(string.Format("byte8Count: {0}", header.byte8Count), bt),
            };
        }

        internal static NetworkIndexTileSubBlock instantiate(MemoryMappedViewAccessor accessor, long start, out long pos)
        {
            NetworkIndexTileSubBlockHeader __header = new NetworkIndexTileSubBlockHeader();
            byte[,] byteBlocks;

            pos = __header.Read(accessor, start);
            byteBlocks = new byte[__header.byte8Count, BLOCK_SZ];

            for (int k = 0; k < __header.byte8Count; k++)
                for (int i = 0; i < BLOCK_SZ; i++)
                    byteBlocks[k, i] = accessor.ReadByte(pos++);

            return new NetworkIndexTileSubBlock(__header, byteBlocks);
        }
    }
    public class NetworkIndexTile : TreeNodeProvider
    {
        public readonly long tileStart;
        public readonly NetworkIndexTileHeader header;
        public readonly NetworkIndexTileSubBlock[] blocks;
        public readonly NetworkIndexTileDetail detail;
        public readonly NetworkIndexTileUnknown2F[] unknown2FloatBlocks;
        public readonly NetworkIndexTileUnknownN[] unknownBlocks;

        public NetworkIndexTile(long tileStart,
            NetworkIndexTileHeader header,
            NetworkIndexTileSubBlock[] blocks,
            NetworkIndexTileUnknown2F[] unknown2FloatBlocks,
            NetworkIndexTileDetail detail,
            NetworkIndexTileUnknownN[] unknownBlocks)
        {
            this.tileStart = tileStart;
            this.header = header;
            this.blocks = blocks ?? new NetworkIndexTileSubBlock[0];
            this.unknown2FloatBlocks = unknown2FloatBlocks ?? new NetworkIndexTileUnknown2F[0];
            this.detail = detail;
            this.unknownBlocks = unknownBlocks ?? new NetworkIndexTileUnknownN[0];
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
                maker(string.Format("blocksCount: {0}", detail.blocksCount), RecursiveTreeNode.recurse(maker, unknownBlocks)),
        };
        }

        public static NetworkIndexTile instantiate(MemoryMappedViewAccessor accessor, long tileStart, out long pos)
        {
            NetworkIndexTileHeader _header = new NetworkIndexTileHeader();
            NetworkIndexTileSubBlock[] _blocks;
            NetworkIndexTileDetail _detail = new NetworkIndexTileDetail();
            NetworkIndexTileUnknown2F[] _blocks2F;
            NetworkIndexTileUnknownN[] _blocksN;

            pos = _header.Read(accessor, tileStart);

            _blocks = new NetworkIndexTileSubBlock[_header.blockCount];
            for (int j = 0; j < _blocks.Length; j++)
                _blocks[j] = NetworkIndexTileSubBlock.instantiate(accessor, pos, out pos);

            _blocks2F = new NetworkIndexTileUnknown2F[accessor.ReadUInt32(pos)]; pos += Marshal.SizeOf(typeof(UInt32));
            for (int j = 0; j < _blocks2F.Length; j++)
                pos = _blocks2F[j].Read(accessor, pos);

            pos = _detail.Read(accessor, pos);

            _blocksN = new NetworkIndexTileUnknownN[_detail.blocksCount];
            for (int j = 0; j < _blocksN.Length; j++)
                pos = _blocksN[j].Read(accessor, pos);

            return new NetworkIndexTile(tileStart, _header, _blocks, _blocks2F, _detail, _blocksN);
        }
    }

    public class NetworkIndexSubFile : TreeNodeProvider
    {
        public readonly NetworkIndexHeader header;
        public readonly NetworkIndexTile[] tiles;
        public readonly NetworkIndexBlockV7[] blocksV7;
        public readonly NetworkIndexUnknownBlock[] unknownBlocks;
        public readonly NetworkIndexTrailer trailer;
        public readonly long tilesEnd;
        public readonly long blocksEnd;
        public readonly long unknownBlocksEnd;
        public readonly long trailerEnd;
        public readonly NetworkIndexUnknownBlock4I[] unknown4IBlocks;
        public readonly long unknown4IBlocksEnd;
        public readonly NetworkIndexBlock[] blocks;

        public NetworkIndexSubFile(NetworkIndexHeader header,
            NetworkIndexTile[] tiles, long tilesEnd,
            NetworkIndexBlockV7[] blocksV7, long blocksEnd,
            NetworkIndexUnknownBlock4I[] unknown4IBlocks, long unknown4IBlocksEnd,
            NetworkIndexTrailer trailer, long trailerEnd,
            NetworkIndexUnknownBlock[] unknownBlocks, long unknownBlocksEnd)
        {
            this.header = header;
            this.tiles = tiles ?? new NetworkIndexTile[0];
            this.tilesEnd = tilesEnd;
            this.blocksV7 = blocksV7 ?? new NetworkIndexBlockV7[0];
            this.blocksEnd = blocksEnd;
            this.unknown4IBlocks = unknown4IBlocks ?? new NetworkIndexUnknownBlock4I[0];
            this.unknown4IBlocksEnd = unknown4IBlocksEnd;
            this.trailer = trailer;
            this.trailerEnd = trailerEnd;
            this.unknownBlocks = unknownBlocks ?? new NetworkIndexUnknownBlock[0];
            this.unknownBlocksEnd = unknownBlocksEnd;
        }

        public NetworkIndexSubFile(NetworkIndexHeader header,
            NetworkIndexTile[] tiles, long tilesEnd,
            NetworkIndexBlock[] blocks, long blocksEnd,
            NetworkIndexUnknownBlock4I[] unknown4IBlocks, long unknown4IBlocksEnd,
            NetworkIndexTrailer trailer, long trailerEnd,
            NetworkIndexUnknownBlock[] unknownBlocks, long unknownBlocksEnd)
        {
            this.header = header;
            this.tiles = tiles ?? new NetworkIndexTile[0];
            this.tilesEnd = tilesEnd;
            this.blocks = blocks ?? new NetworkIndexBlock[0];
            this.blocksEnd = blocksEnd;
            this.unknown4IBlocks = unknown4IBlocks ?? new NetworkIndexUnknownBlock4I[0];
            this.unknown4IBlocksEnd = unknown4IBlocksEnd;
            this.trailer = trailer;
            this.trailerEnd = trailerEnd;
            this.unknownBlocks = unknownBlocks ?? new NetworkIndexUnknownBlock[0];
            this.unknownBlocksEnd = unknownBlocksEnd;
        }

        public static NetworkIndexSubFile instantiate(MemoryMappedViewAccessor accessor, out long pos)
        {
            NetworkIndexHeader header = new NetworkIndexHeader();
            NetworkIndexTile[] tiles;
            NetworkIndexBlockV7[] blocks7;
            NetworkIndexBlock[] blocks;
            NetworkIndexUnknownBlock4I[] unknown4IBlocks;
            NetworkIndexTrailer trailer = new NetworkIndexTrailer();
            NetworkIndexUnknownBlock[] unknownBlocks;

            pos = header.Read(accessor, 0);

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
            for (int i = 0; i < tiles.Length; i++)
                tiles[i] = NetworkIndexTile.instantiate(accessor, pos, out pos);
            long tilesEnd = pos;

            // 3 = 011
            // 4 = 100
            // 6 = 110
            // 7 = 111

            long blocksEnd, trailerEnd, unknownBlocksEnd;

            switch (header.verMajor)
            {
                case 3:
                case 4:
                case 6:
                    blocks = new NetworkIndexBlock[accessor.ReadUInt32(pos)]; pos += Marshal.SizeOf(typeof(UInt32));
                    for (int i = 0; i < blocks.Length; i++)
                        pos = blocks[i].Read(accessor, pos);
                    blocks7 = null;
                    break;
                default:
                    blocks = null;
                    blocks7 = new NetworkIndexBlockV7[accessor.ReadUInt32(pos)]; pos += Marshal.SizeOf(typeof(UInt32));
                    for (int i = 0; i < blocks7.Length; i++)
                        pos = blocks7[i].Read(accessor, pos);
                    break;
            }
            blocksEnd = pos;

            unknown4IBlocks = new NetworkIndexUnknownBlock4I[accessor.ReadUInt32(pos)]; pos += Marshal.SizeOf(typeof(UInt32));
            for (int i = 0; i < unknown4IBlocks.Length; i++)
                pos = unknown4IBlocks[i].Read(accessor, pos);
            long unknown4IBlocksEnd = pos;

            pos = trailer.Read(accessor, pos);
            trailerEnd = pos;

            switch (header.verMajor)
            {
                case 6:
                case 7:
                    unknownBlocks = new NetworkIndexUnknownBlock[accessor.ReadUInt32(pos)]; pos += Marshal.SizeOf(typeof(UInt32));
                    for (int i = 0; i < unknownBlocks.Length; i++)
                        pos = (unknownBlocks[i] = new NetworkIndexUnknownBlock()).Read(accessor, pos);
                    break;
                default:
                    unknownBlocks = null;
                    break;
            }
            unknownBlocksEnd = pos;

            NetworkIndexSubFile f =
                blocks7 == null ?
                new NetworkIndexSubFile(header, tiles, tilesEnd, blocks, blocksEnd, unknown4IBlocks, unknown4IBlocksEnd,
                    trailer, trailerEnd, unknownBlocks, unknownBlocksEnd) :
                new NetworkIndexSubFile(header, tiles, tilesEnd, blocks7, blocksEnd, unknown4IBlocks, unknown4IBlocksEnd,
                    trailer, trailerEnd, unknownBlocks, unknownBlocksEnd);
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
                maker(string.Format("blocks{0}: {1}",
                        blocks == null? "V7": "", blocks == null? blocksV7.Length: blocks.Length),
                        blocks == null? RecursiveTreeNode.recurse(maker, blocksV7): RecursiveTreeNode.recurse(maker, blocks)),
                maker(string.Format("unknown4IBlocks: {0}", unknown4IBlocks.Length), RecursiveTreeNode.recurse(maker, unknown4IBlocks)),
                maker(string.Format("unknown: {0:X8}", trailer.unknown2), null),
                maker(string.Format("unknown: {0:X8}", trailer.unknown3), null),
                maker(string.Format("tileX: {0}", trailer.tileX), null),
                maker(string.Format("tileZ: {0}", trailer.tileZ), null),
                maker(string.Format("unknown: {0}", trailer.unknown4), null),
                maker(string.Format("unknown: {0}", trailer.Unknown5), null),
                maker(string.Format("unknown: {0}", trailer.unknown6), null),
                maker(string.Format("unknown: {0}", trailer.Unknown7), null),
                maker(string.Format("unknownBlocks: {0}", unknownBlocks.Length), RecursiveTreeNode.recurse(maker, unknownBlocks)),
            };
        }
    }

    internal interface TreeNodeProvider
    {
        T[] treeNodes<T>(TreeNodeMaker<T> maker);
    }

    internal class RecursiveTreeNode
    {
        public readonly string text;
        public readonly RecursiveTreeNode[] children;

        public RecursiveTreeNode(string text, RecursiveTreeNode[] children)
        {
            this.text = text;
            this.children = children;
        }

        internal static T[] recurse<T, U>(TreeNodeMaker<T> maker, U[] source) where U : TreeNodeProvider
        {
            T[] children = new T[source.Length];
            for (int i = 0; i < children.Length; i++)
                children[i] = maker(i.ToString(), source[i].treeNodes(maker));
            return children;
        }
    }
}
