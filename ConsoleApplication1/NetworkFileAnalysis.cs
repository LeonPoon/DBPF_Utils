﻿/**************************************************************************
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

using Sc4Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using GenUtils;

namespace ConsoleApplication1
{
    internal class NetworkFileAnalysis : AnalysisTools
    {
        private string fn;
        private StreamWriter outAll;
        private string outfile;
        private Dictionary<Tuple<int, uint>, List<long>> offsetOccurrences = new Dictionary<Tuple<int, uint>, List<long>>();
        private Dictionary<uint, List<long>> blockCountOccurrences = new Dictionary<uint, List<long>>();

        public NetworkFileAnalysis(string fn, string outfile, StreamWriter outAll)
        {
            this.fn = fn;
            this.outfile = outfile;
            this.outAll = outAll;
        }

        public long testNetworkFileAnalysis(MemoryMappedViewAccessor accessor, UInt32 sz)
        {

            long pos = 0;
            NetworkIndexSubFile f = NetworkIndexSubFile.instantiate(accessor, 0, out pos);
            if (pos != sz)
                throw new ArgumentException(string.Format("size check {0}: expect whole file size={1}, actual read={2}", outfile, sz, pos));
            if (pos != f.sz)
                throw new ArgumentException(string.Format("size check {0}: expect instantiation record size={1}, actual read={2}", outfile, f.Sz, pos));
            pos = checkSize(f.ReadResultComponents, 0);
            if (pos != sz)
                throw new ArgumentException(string.Format("size check {0}: expect {1}, got pos={2}", outfile, sz, pos));
            using (var sw = new StreamWriter(outfile))
                writeOut(0, sw, f.ReadResultComponents, findMaxLen(0, f.ReadResultComponents), accessor);
            return pos;
        }

        public const int INDENT_SPACES = 2;

        public int findMaxLen(int lvl, ReadResult[] readResultComponents)
        {
            int len = 0;
            foreach (var c in readResultComponents)
            {
                int nexLen = c.getName().Length + lvl * INDENT_SPACES;
                if (nexLen > len) len = nexLen;
                readResultComponents = c.ReadResultComponents;
                if (readResultComponents != null)
                {
                    nexLen = findMaxLen(lvl + 1, readResultComponents);
                    if (nexLen > len) len = nexLen;
                }
            }
            return len;
        }

        public void writeOut(int level, StreamWriter sw, ReadResult[] readResultComponents, int maxLen, MemoryMappedViewAccessor accessor)
        {
            foreach (var c in readResultComponents)
            {
                var name = c.getName();
                name = string.Format("{0}{1}{2}:",
                    "                                                         ".Substring(0, level * INDENT_SPACES),
                    name,
                    ".........................................................".Substring(0, maxLen - level * INDENT_SPACES - name.Length)
                    );
                sw.Write(name);
                printBytes(sw, accessor, c.Pos, (int)c.Bytes);

                readResultComponents = c.ReadResultComponents;
                if (readResultComponents != null)
                {
                    writeOut(level + 1, sw, readResultComponents, maxLen, accessor);
                }
            }
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

        private void printStats<T, U>(Dictionary<T, List<U>> d, string desc)
        {
            foreach (var x in d)
            {
                Console.WriteLine("{2}={0}, occurrences={1}", x.Key, x.Value.Count, desc);
                if (x.Value.Count == 1)
                    Console.WriteLine("At {0:X8}", x.Value[0]);
            }
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
