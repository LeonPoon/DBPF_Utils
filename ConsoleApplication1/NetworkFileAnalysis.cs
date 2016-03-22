using Sc4Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace ConsoleApplication1
{

    class FoundRecord
    {
        public int i, blocksCount2;
        public long pos, headerEndPos, blocksEndPos, endPos, sz, szAfterBlocks;
        public NetworkIndexTileHeader header;

        internal void populate(MemoryMappedViewAccessor accessor)
        {
            int blocks = (int)header.blockCount;
            NetworkIndexTileSubBlock[] subblocks = new NetworkIndexTileSubBlock[blocks];

            blocksEndPos = headerEndPos;

            for (int i = 0; i < blocks; i++)
            {
                NetworkIndexTileSubBlockHeader tileHeader = new NetworkIndexTileSubBlockHeader();
                blocksEndPos = tileHeader.Read(accessor, blocksEndPos);

                if (tileHeader.blockNumber != i)
                    throw new ArgumentException(string.Format("expect block {0} but got {1}", i, tileHeader.blockNumber));

                byte[,] bytes = new byte[tileHeader.byte8Count, NetworkIndexTileSubBlock.BLOCK_SZ];
                NetworkIndexTileSubBlock subblock = subblocks[i] = new NetworkIndexTileSubBlock(tileHeader, bytes);

                blocksEndPos += tileHeader.byte8Count * NetworkIndexTileSubBlock.BLOCK_SZ;
            }

            blocksCount2 = (int)accessor.ReadUInt32(blocksEndPos); blocksEndPos += 4;
            blocksEndPos += 4 * 2 * blocksCount2;

            szAfterBlocks = endPos - blocksEndPos;


        }
    }

    internal class NetworkFileAnalysis
    {
        private string fn;
        private StreamWriter outAll;
        private Dictionary<Tuple<int, uint>, List<long>> offsetOccurrences = new Dictionary<Tuple<int, uint>, List<long>>();
        private Dictionary<uint, List<long>> blockCountOccurrences = new Dictionary<uint, List<long>>();

        public NetworkFileAnalysis(string fn, string outfile, StreamWriter outAll)
        {
            this.fn = fn;
            this.outfile = outfile;
            this.outAll = outAll;
        }

        internal void runAnalysis()
        {
            using (var mmf = MemoryMappedFile.CreateFromFile(fn, System.IO.FileMode.Open, Guid.NewGuid().ToString(), 0, MemoryMappedFileAccess.Read))
            {
                UInt32 sz;
                using (var accessor = mmf.CreateViewAccessor(0, 4, MemoryMappedFileAccess.Read))
                    sz = accessor.ReadUInt32(0);
                using (var accessor = mmf.CreateViewAccessor(0, sz, MemoryMappedFileAccess.Read))
                    testNetworkFileAnalysis2(mmf, accessor, sz);
            }
        }

        public NetworkIndexSubFile testNetworkFileAnalysis2(MemoryMappedFile ____, MemoryMappedViewAccessor accessor, UInt32 sz)
        {
            long pos;
            var f = NetworkIndexSubFile.instantiate(accessor, out pos);
            long finishRead = pos;

            var h = f.header;
            //pos = h.Read(accessor, 0);

            uint tiles = (uint)f.tiles.Length; // accessor.ReadUInt32(pos); pos += 4;
            ////using (var txt = new StreamWriter(outfile))
            //for (int i = 0; i < tiles; i++)
            //{
            //    NetworkIndexTile.instantiate(accessor, pos, out pos);
            //}

            long end = f.unknown4IBlocksEnd;
            long bsz = sz - end;
            bsz = bsz > 280 ? 280 : bsz;

            outAll.Write(string.Format("{0}:{1}:", outfile.PadRight(70), string.Format("v{0}@sz={1:X8}({1}),tiles={2},tilesEnd={3:X8}({3}),blocksCount={7},unknown4IBlocksEnd={6:X8}({6}),shown={4}/{5}:", h.verMajor, sz, tiles, f.tilesEnd, bsz, sz - end, f.unknown4IBlocksEnd, f.blocks == null ? f.blocksV7.Length : f.blocks.Length).PadRight(90)));

            for (int i = 0; i < bsz; i++)
            {
                outAll.Write("{0:X2}.", accessor.ReadByte(end + i));
            }
            outAll.WriteLine("  x={0:X2},z={1:X2},{2}", f.trailer.tileX, f.trailer.tileZ, f.trailer.tileX == f.trailer.tileZ);

            //outAll.WriteLine("finish={0:X8}({0})", finishRead);

            pos = f.tilesEnd + 4;
            using (var sw = new StreamWriter(outfile))
            {
                if (f.blocksV7 == null)
                {
                    for (int i = 0; i < f.blocks.Length; i++)
                    {
                        sw.Write("v6-{0:D5}:", i);
                        pos += printBytes(sw, accessor, pos, 76);
                    }
                }
                else
                {
                    for (int i = 0; i < f.blocksV7.Length; i++)
                    {
                        sw.Write("v7-{0:D5}:", i);
                        pos += printBytes(sw, accessor, pos, 81);
                    }
                }
            }

            return f;
        }

        private long printBytes(StreamWriter sw, MemoryMappedViewAccessor accessor, long pos, int len)
        {
            for (int i = 0; i < len; i++)
            {
                byte b = accessor.ReadByte(pos++);
                if (b == 0)
                    sw.Write("  .");
                else
                    sw.Write("{0:X2}.", b);
            }
            sw.WriteLine();
            return len;

        }

        public void testNetworkFileAnalysis3(MemoryMappedFile ____, MemoryMappedViewAccessor accessor, UInt32 sz)
        {
            var h = new NetworkIndexHeader();
            long pos = h.Read(accessor, 0);
            UInt32 count = accessor.ReadUInt32(pos); pos += 4;

            FoundRecord lastRecord = null;

            Console.WriteLine("gonna read {0} records", count);
            FoundRecord[] records = new FoundRecord[count];
            for (int i = 0; i < count; i++)
            {

                pos = findNextTypeId(accessor, sz, pos, i, count) - 8;

                FoundRecord record = records[i] = new FoundRecord()
                {
                    i = i,
                    pos = pos,
                };
                pos = record.headerEndPos = record.header.Read(accessor, pos);

                if (lastRecord == null)
                    Console.WriteLine("first record at {0:X8}", record.pos);
                else {
                    finishLastRecord(lastRecord, record.pos, accessor);
                    if (record.header.tileNumber < lastRecord.header.tileNumber)
                        Console.WriteLine("tile[{0}]: tile[{1}].tilenumber{1}->{2}", record.i, lastRecord.i, lastRecord.header.tileNumber, record.header.tileNumber);
                }

                lastRecord = record;
            }

            if (lastRecord != null)
            {
                pos = findNextTypeId(accessor, sz, pos, lastRecord.i, count) - 8;
                finishLastRecord(lastRecord, pos, accessor);
                long end = lastRecord.pos + lastRecord.sz;
                Console.WriteLine("last record at {0:X8} ends at {1:X8}", lastRecord.pos, end);

            }

            {
                long end;
                if (lastRecord == null)
                    end = pos;
                else
                    NetworkIndexTile.instantiate(accessor, lastRecord.pos, out end);
                long bsz = sz - end;
                bsz = bsz > 80 ? 80 : bsz;

                outAll.Write(string.Format("{0}:{1}:", outfile.PadRight(70), string.Format("v{0}@sz={1:X8}({1}),end={2:X8}({2}),shown={3}/{4}:", h.verMajor, sz, end, bsz, sz - end).PadRight(70)));

                for (int i = 0; i < bsz; i++)
                    outAll.Write("{0:X2}.", accessor.ReadByte(end + i));

                outAll.WriteLine();
            }

            Dictionary<Tuple<UInt32, long>, List<FoundRecord>> d = new Dictionary<Tuple<uint, long>, List<FoundRecord>>();
            Dictionary<Tuple<UInt32, long>, List<FoundRecord>> d2 = new Dictionary<Tuple<uint, long>, List<FoundRecord>>();

            Console.Write("Saving txt: {0}", outfile);
            using (var txt = new StreamWriter(outfile))
                foreach (FoundRecord record in records)
                {
                    addStats(d, new Tuple<UInt32, long>(record.header.subfileTypeId, record.sz), record);
                    addStats(d2, new Tuple<UInt32, long>(record.header.subfileTypeId, record.szAfterBlocks), record);
                    txt.Write("{0:D5}@0x{1:X8},type={2:X8},nBlk={3:D2},nBlk2F={4:D}:", record.i, record.pos, record.header.subfileTypeId, record.header.blockCount, record.blocksCount2);
                    for (int i = 0; i < record.szAfterBlocks; i++)
                    {
                        byte b = accessor.ReadByte(record.blocksEndPos + i);
                        if (b == 0)
                            txt.Write("  .");
                        else
                            txt.Write("{0:X2}.", b);
                    }
                    txt.WriteLine();
                }
            Console.WriteLine("... done!");

            printStats(d, "typeId+sz");
            printStats(d2, "typeId+szAfterBlocks");
        }

        private FoundRecord finishLastRecord(FoundRecord lastRecord, long nextPos, MemoryMappedViewAccessor accessor)
        {
            lastRecord.sz = (lastRecord.endPos = nextPos) - lastRecord.pos;
            lastRecord.populate(accessor);
            return lastRecord;
        }

        private long findNextTypeId(MemoryMappedViewAccessor accessor, uint sz, long pos, int forBlockNumber, uint numBlocks)
        {
            for (; pos < sz - 4; pos++)
            {
                UInt32 typeid = accessor.ReadUInt32(pos);
                switch (typeid)
                {
                    case 0xC9C05C6E:
                    case 0xCA16374F:
                    case 0x49c1a034:
                        return pos;
                }
            }
            throw new ArgumentException(string.Format("cannot find typeid for block[{0}] of {1}", forBlockNumber, numBlocks));
        }

        private void testNetworkFileAnalysis1(MemoryMappedFile mmf, MemoryMappedViewAccessor accessor, UInt32 sz)
        {
            var h = new NetworkIndexHeader();
            long pos, lastPos = 0;
            UInt32 count = accessor.ReadUInt32(pos = h.Read(accessor, 0));
            pos += 4;

            int found = 0;
            const UInt32 lookfor = 33152;

            int occurrences = 0;

            Console.WriteLine(count);
            for (uint i = count = 0; i <= h.fileSz - 4; i++)
            {
                UInt32 typeid;
                switch (typeid = accessor.ReadUInt32(i))
                {
                    case 0xC9C05C6E:
                    case 0xCA16374F:
                    case 0x49c1a034:
                        testHeader(typeid, i, accessor, lastPos, occurrences++);
                        lastPos = i;
                        count++;
                        break;
                    case lookfor:
                        Console.WriteLine("Found {1} at {0:X8}", i, lookfor);
                        found++;
                        break;
                }

            }

            printStats(offsetOccurrences, "offsetFromLast");
            printStats(blockCountOccurrences, "blockCount");

            Console.WriteLine(count);
            Console.WriteLine("Found: {0}", found);
        }

        private void printStats<T, U>(Dictionary<T, List<U>> d, string desc)
        {
            foreach (var x in d)
            {
                Console.WriteLine("{2}={0}, occurrences={1}", x.Key, x.Value.Count, desc);
                if (x.Value.Count == 1)
                    Console.WriteLine("At {0:X8}", x.Value[0]);
            }
        }

        UInt32 lastTile = 0;
        bool firstTileNumberChaos = true;
        private string outfile;

        private void testHeader(uint typeid, long pos, MemoryMappedViewAccessor accessor, long lastPos, int afterOccurrences)
        {
            int offsetFromLast = (int)(pos - lastPos);

            var h = new NetworkIndexTileHeader();
            h.Read(accessor, pos - 8);

            if (h.subfileTypeId != typeid)
                throw new ArgumentException();

            UInt32 tile = h.tileNumber;
            if (tile < lastTile)
            {
                Console.WriteLine("tile number chaos {2}->{3} near {0:X8} after {1} occurrences and {4} from last", pos, afterOccurrences, lastTile, tile, offsetFromLast);
                if (firstTileNumberChaos)
                {
                    firstTileNumberChaos = false;
                    printStats(offsetOccurrences, "offsetFromLast");
                    printStats(blockCountOccurrences, "blockCount");
                }
            }
            lastTile = tile;

            addStats(offsetOccurrences, new Tuple<int, uint>(offsetFromLast, h.blockCount), pos);
            //addStats(blockCountOccurrences, h.blockCount, pos);
            //Console.WriteLine("tile={0}, type={1:X8}, fromBefore={2}", h.tileNumber, typeid, offsetFromLast);
        }

        private void addStats<T, U>(Dictionary<T, List<U>> d, T k, U v)
        {
            List<U> vals;
            if (!d.TryGetValue(k, out vals))
                d.Add(k, vals = new List<U>());
            vals.Add(v);
        }
    }
}
