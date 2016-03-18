using DBPF;
using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class Program
    {
        const string fn = @"C:\Program Files (x86)\Steam\steamapps\common\SimCity 4 Deluxe\SimCity_{0}.dat";
        static void Main(string[] args)
        {
            for (int i = 0; i++ < 5;)
                main(string.Format(fn, i));
            Console.WriteLine("Press enter...");
            Console.ReadLine();
        }

        static void main(string fn)
        {

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
                        if ((en1.Key == 0x1 || en1.Key == 0x1ABE787D || en1.Key == 0x22DEC92D || en1.Key == 0x46A006B0 || en1.Key == 0x4C06F888 || en1.Key == 0x6A1EED2C ||
                            en1.Key == 0x8b6b7857 || en1.Key == 0xA9179251 || en1.Key == 0xAB7E5421 || en1.Key == 0xebdd10a4 || en1.Key == 0x6A386D26
                            ) && f.dbDirEntries.TryGetValue(en2.Value.tgi, out e) && false)
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
                    }
            Console.WriteLine("Dir entry: {0}", f.dbDirEntries);

        }
    }
}
