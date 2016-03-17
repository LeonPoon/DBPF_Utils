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
        const string fn = @"C:\Users\szeleung\Documents\SimCity 4\Regions\Max Pop Project\City - Template Large.sc4";
        static void Main(string[] args)
        {

            var mmf = MemoryMappedFile.CreateFromFile(fn, System.IO.FileMode.Open);
            var f = Mapper.map(mmf);

            Console.WriteLine(f.header.identifier);

            DBDirectoryEntry dbDirEntry;
            foreach (var en in f.indexTable)
                foreach (var en1 in en.Value)
                    foreach (var en2 in en1.Value)
                        Console.WriteLine("{0},actualSize={1}", en2.Value, f.dbDirEntries.TryGetValue(en2.Value.tgi, out dbDirEntry) ? dbDirEntry.size : en2.Value.size);
            Console.WriteLine("Dir entry: {0}", f.dbDirEntries);

            Console.WriteLine("Press enter...");
            Console.ReadLine();
        }
    }
}
