using DBPF;
using Sc4Network;
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace ConsoleApplication1
{

    class Program
    {
        static void x(string f, Readable o)
        {
            using (var m = MemoryMappedFile.CreateNew(Guid.NewGuid().ToString(), 10000))
            using (var w = m.CreateViewAccessor(0, 10000, MemoryMappedFileAccess.ReadWrite))
            {
                Console.WriteLine("{0}: {1}", f, o.Read(w, 0));
            }
        }
        static void Main(string[] args)
        {
            x("NetworkIndexHeader", new NetworkIndexHeader());
            x("NetworkIndexTileHeader", new NetworkIndexTileHeader());
            x("NetworkIndexTileSubBlockHeader", new NetworkIndexTileSubBlockHeader());
            x("NetworkIndexTileDetail", new NetworkIndexTileDetail());
            x("NetworkIndexTileUnknown2F", new NetworkIndexTileUnknown2F());
            x("NetworkIndexTileUnknownN", new NetworkIndexTileUnknownN());
            x("NetworkIndexTileUnknown4", new NetworkIndexTileUnknown4());
            x("NetworkIndexBlock", new NetworkIndexBlock());
            x("NetworkIndexBlockV7", new NetworkIndexBlockV7());
            x("NetworkIndexBlockUnknown4", new NetworkIndexBlockUnknown4());
            x("NetworkIndexTrailer", new NetworkIndexTrailer());
            x("NetworkIndexUnknownBlock4I", new NetworkIndexUnknownBlock4I());
            x("NetworkIndexUnknownBlock", new NetworkIndexUnknownBlock());

            Console.WriteLine("Press enter...");
            Console.ReadLine();

            //new NetworkFileAnalysis(@"Z:\Sc4Decode\network.bin", @"Z:\Sc4Decode\network.txt").runAnalysis();
            string fn;
            fn = @"C:\Users\szeleung\Documents\SimCity 4\Regions\Max Pop Project\City - Template Large.sc4";
            fn = @"C:\Users\szeleung\Documents\SimCity 4\Regions\London\City - Woolwich.sc4";

            using (StreamWriter sw = new StreamWriter(@"Z:\Sc4Decode\PostBlocks.txt"))
                mainDir(@"C:\Users\szeleung\Documents\SimCity 4\Regions", sw);

            Console.WriteLine("Press enter...");
            Console.ReadLine();
        }

        private static void mainDir(string dirpath, StreamWriter sw)
        {
            foreach (var elem in Directory.GetFiles(dirpath))
                if (elem.EndsWith(".sc4", StringComparison.OrdinalIgnoreCase))
                    main(elem, sw);
            foreach (var elem in Directory.GetDirectories(dirpath))
                mainDir(elem, sw);
        }

        static void Mains(string[] args)
        {
            string fn = @"C:\Program Files (x86)\Steam\steamapps\common\SimCity 4 Deluxe\SimCity_{0}.dat";
            for (int i = 0; i++ < 5;)
                main(string.Format(fn, i), null);
            Console.WriteLine("Press enter...");
            Console.ReadLine();
        }

        static void main(string fn, StreamWriter sw)
        {
            Console.WriteLine("Opening: {0}", fn);
            string target = @"Z:\Sc4Decode\" + Path.GetFileName(Path.GetDirectoryName(fn)) + "\\" + Path.GetFileNameWithoutExtension(fn) + ".network.bin";
            string target2 = @"Z:\Sc4Decode\" + Path.GetFileName(Path.GetDirectoryName(fn)) + "\\" + Path.GetFileNameWithoutExtension(fn) + ".network.txt";

            var mmf = MemoryMappedFile.CreateFromFile(fn, System.IO.FileMode.Open);
            var f = Mapper.map(mmf);

            Console.WriteLine(f.header.identifier);

            DBDirectoryEntry dbDirEntry;
            foreach (var en in f.indexTable)
                foreach (var en1 in en.Value)
                    foreach (var en2 in en1.Value)
                    {
                        var n = string.Format("{0},actualSize={1}", en2.Value, f.dbDirEntries.TryGetValue(en2.Value.tgi, out dbDirEntry) ? dbDirEntry.size : en2.Value.size);
                        DBDirectoryEntry e;
                        bool compressed = f.dbDirEntries.TryGetValue(en2.Value.tgi, out e);

                        if ((en1.Key == 0x1 || en1.Key == 0x1ABE787D || en1.Key == 0x22DEC92D || en1.Key == 0x46A006B0 || en1.Key == 0x4C06F888 || en1.Key == 0x6A1EED2C ||
                            en1.Key == 0x8b6b7857 || en1.Key == 0xA9179251 || en1.Key == 0xAB7E5421 || en1.Key == 0xebdd10a4 || en1.Key == 0x6A386D26
                            ) && compressed && false)
                            using (var r = mmf.CreateViewAccessor(en2.Value.fileOffset, en2.Value.size, MemoryMappedFileAccess.Read))
                            using (var m = MemoryMappedFile.CreateNew(Guid.NewGuid().ToString(), e.size))
                            using (var w = m.CreateViewAccessor(0, e.size, MemoryMappedFileAccess.ReadWrite))
                            {
                                byte[] b;

                                b = new byte[en2.Value.size];
                                r.ReadArray(0, b, 0, b.Length);
                                using (var wf = MemoryMappedFile.CreateFromFile(string.Format(@"Z:\Sc4Decode\{0}.bin", n), System.IO.FileMode.Create, Guid.NewGuid().ToString(), b.Length))
                                using (var ww = wf.CreateViewAccessor(0, b.Length, MemoryMappedFileAccess.Write))
                                    ww.WriteArray(0, b, 0, b.Length);

                                f.decompress(r, w);
                                b = new byte[e.size];
                                w.ReadArray(0, b, 0, b.Length);
                                using (var wf = MemoryMappedFile.CreateFromFile(string.Format(@"Z:\Sc4Decode\{0}.png", n), System.IO.FileMode.Create, Guid.NewGuid().ToString(), b.Length))
                                using (var ww = wf.CreateViewAccessor(0, b.Length, MemoryMappedFileAccess.Write))
                                    ww.WriteArray(0, b, 0, b.Length);
                            }

                        else if (en.Key == 0x6A0F82B2)
                        {
                            NetworkIndexSubFile n1 = null;
                            long pos = 0;
                            Console.WriteLine(en2.Value);
                            using (var r = mmf.CreateViewAccessor(en2.Value.fileOffset, en2.Value.size, MemoryMappedFileAccess.Read))
                                if (compressed)
                                {
                                    using (var m = MemoryMappedFile.CreateNew(Guid.NewGuid().ToString(), e.size))
                                    using (var w = m.CreateViewAccessor(0, e.size, MemoryMappedFileAccess.ReadWrite))
                                    {
                                        f.decompress(r, w);
                                        n1 = makeNetworkIndexSubFile(w, out pos, target, target2, e.size, sw);
                                    }
                                }
                                else {
                                    n1 = makeNetworkIndexSubFile(r, out pos, target, target2, en2.Value.size, sw);
                                }

                            if (n1 != null)
                            {
                                Console.WriteLine("filesize={1}, read ended at {0:X8}({0})", pos, n1.header.fileSz);
                                Console.WriteLine("tilesCount={0}, tilesEnd={1:X8}({1}), blocksCount={2}, blocksEnd={3:X8}({3}), trailerEnd={6:X8}({6}), unknownBlocksCount={4}, unknownBlocksEnd={5:X8}({5})",
                                    n1.tiles.Length, n1.tilesEnd, n1.blocksV7 == null ? n1.blocks.Length : n1.blocksV7.Length, n1.blocksEnd, n1.unknownBlocks.Length, n1.unknownBlocksEnd,
                                    n1.trailerEnd
                                    );
                            }

                            var nodes = n1.treeNodes(delegate (string text, RecursiveTreeNode[] c) { return new RecursiveTreeNode(text, c); });
                            continue;

                            foreach (var node in nodes)
                            {
                                //   node.outConsole(0);
                            }
                        }
                    }
            Console.WriteLine("Dir entry: {0}", f.dbDirEntries);

        }

        public static NetworkIndexSubFile makeNetworkIndexSubFile(MemoryMappedViewAccessor a, out long pos, string binTarget, string txtTarget, long sz, StreamWriter w)
        {
            pos = 0;

            Console.Write("Saving bin: {0}", binTarget);
            save(a, binTarget, sz);
            Console.WriteLine(" ...done!");

            NetworkIndexSubFile f = null;
            //try
            //{
            //NetworkFileAnalysis f = new NetworkFileAnalysis(null, txtTarget, w).testNetworkFileAnalysis2(null, a, (uint)sz);

            f = NetworkIndexSubFile.instantiate(a, out pos);
            if (pos != sz)
            {
                throw new ArgumentException(string.Format("size check {0}: expect {1}, got pos={2}", binTarget, sz, pos));
            }
            //}
            //catch (Exception e)
            //{
            //    NetworkIndexHeader h = new NetworkIndexHeader();
            //    h.Read(a, 0);
            //    w.WriteLine("{0}:{1}: {2}", txtTarget.PadRight(70), string.Format("v{0}", h.verMajor).PadRight(90), e.Message);
            //    throw;
            //}

            return f;
        }

        private static void save(MemoryMappedViewAccessor w, string target, long sz)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            Console.WriteLine("write to: {0}", target);
            byte[] bytes = new byte[sz];
            w.ReadArray(0, bytes, 0, bytes.Length);
            File.WriteAllBytes(target, bytes);
        }
    }

    internal class RecursiveTreeNode
    {
        public readonly string SP = "                                                                         ";
        public readonly string text;
        public readonly RecursiveTreeNode[] children;

        public RecursiveTreeNode(string text, RecursiveTreeNode[] children)
        {
            this.text = text;
            this.children = children;
        }

        internal void outConsole(int lvl)
        {

            Console.WriteLine("{0}{1}{2}", SP.Substring(0, lvl * 2), text, children == null ? "" : " *");
            lvl++;
            if (children != null)
                foreach (var c in children)
                    c.outConsole(lvl);
        }
    }
}

