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

using DBPF;
using GenUtils;
using Sc4Network;
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace ConsoleApplication1
{

    class Program
    {
        static void x<T>(string f, T o) where T : struct, Readable
        {
            using (var m = MemoryMappedFile.CreateNew(Guid.NewGuid().ToString(), 10000))
            using (var w = m.CreateViewAccessor(0, 10000, MemoryMappedFileAccess.ReadWrite))
            {
                Console.WriteLine("{0}: {1} Marshal={2}", f, o.Read(w, 0), Marshal.SizeOf(o.GetType()));
            }
        }
        static void Main(string[] args)
        {
            x("NetworkIndexHeader", new NetworkIndexHeaderStruct());
            x("NetworkIndexTileHeader", new NetworkIndexTileHeaderStruct());
            x("NetworkIndexTileSubBlockHeader", new NetworkIndexTileSubBlockHeaderStruct());
            x("NetworkIndexTileDetail", new NetworkIndexTileDetailStruct());
            x("NetworkIndexTileUnknown2F", new NetworkIndexTileUnknown2FStruct());
            x("NetworkIndexTileUnknownN", new NetworkIndexTileUnknownNStruct());
            x("NetworkIndexTileUnknown4", new NetworkIndexTileUnknown4Struct());
            x("NetworkIndexBlock", new NetworkIndexBlockPre7Struct());
            x("NetworkIndexBlockV7", new NetworkIndexBlockV7Struct());
            x("NetworkIndexBlockUnknown4", new NetworkIndexBlockUnknown4Struct());
            x("NetworkIndexTrailer", new NetworkIndexTrailerStruct());
            x("NetworkIndexUnknownBlock4I", new NetworkIndexUnknownBlock4IStruct());
            x("NetworkIndexUnknownBlock", new NetworkIndexUnknownBlockStruct());

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
                            long pos = 0;
                            Console.WriteLine(en2.Value);
                            using (var r = mmf.CreateViewAccessor(en2.Value.fileOffset, en2.Value.size, MemoryMappedFileAccess.Read))
                                if (compressed)
                                {
                                    using (var m = MemoryMappedFile.CreateNew(Guid.NewGuid().ToString(), e.size))
                                    using (var w = m.CreateViewAccessor(0, e.size, MemoryMappedFileAccess.ReadWrite))
                                    {
                                        f.decompress(r, w);
                                        makeNetworkIndexSubFile(w, out pos, target, target2, e.size, sw);
                                    }
                                }
                                else {
                                    makeNetworkIndexSubFile(r, out pos, target, target2, en2.Value.size, sw);
                                }

                        }
                    }
            Console.WriteLine("Dir entry: {0}", f.dbDirEntries);

        }

        public static void makeNetworkIndexSubFile(MemoryMappedViewAccessor a, out long pos, string binTarget, string txtTarget, long sz, StreamWriter w)
        {
            Console.Write("Saving bin: {0}", binTarget);
            save(a, binTarget, sz);
            Console.WriteLine(" ...done!");

            pos = new NetworkFileAnalysis(null, txtTarget, w).testNetworkFileAnalysis(a, (uint)sz);
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

