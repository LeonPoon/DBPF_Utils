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

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace GenUtils
{
    public class ReadUtils
    {
    }

    public interface ReadResult
    {
        long Pos { get; }
        long Bytes { get; }
        long Sz { get; }

        string getName();
        ReadResult[] ReadResultComponents { get; }
    }

    public class ReadResultWrapper : ReadResult
    {
        public const int INDENT_SPACES = 2;

        private readonly long pos;
        private readonly long sz;
        private readonly string name;
        private readonly ReadResult[] sub;
        private readonly long bytes;

        public long Pos { get { return pos; } }
        public long Bytes { get { return bytes; } }
        public long Sz { get { return sz; } }

        public ReadResultWrapper(long pos, long sz, string name) : this(pos, sz, sz, name) { }

        public ReadResultWrapper(long pos, long bytes, long sz, string name) : this(pos, bytes, sz, name, null)
        {
        }

        public ReadResultWrapper(long pos, long bytes, long sz, string name, ReadResult[] sub)
        {
            this.name = name;
            this.bytes = bytes;
            this.pos = pos;
            this.sz = sz;
            this.sub = sub;
        }

        public ReadResult[] ReadResultComponents
        {
            get
            {
                return sub;
            }
        }

        public static ReadResult wrap(ReadResult r, ReadResult[] results, string name)
        {
            return wrap(r.Pos + r.Sz, results, name);
        }

        public static ReadResult wrap(long pos, ReadResult[] results, string name)
        {
            long sz = 0, bytes = 0;
            if (results != null)
            {
                bytes = Marshal.SizeOf(typeof(uint));
                sz += bytes;
                foreach (var r in results)
                    sz += r.Sz;
            }
            return new ReadResultWrapper(pos, bytes, sz, name, results);
        }

        public string getName()
        {
            return name;
        }

        public static void writeOut(StreamWriter sw, ReadResult[] readResultComponents, MemoryMappedViewAccessor accessor)
        {
            writeOut(0, sw, readResultComponents, findMaxLen(0, readResultComponents), accessor);
        }

        private static void writeOut(int level, StreamWriter sw, ReadResult[] readResultComponents, int maxLen, MemoryMappedViewAccessor accessor)
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

        private static long printBytes(StreamWriter sw, MemoryMappedViewAccessor accessor, long pos, int len)
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

        private static int findMaxLen(int lvl, ReadResult[] readResultComponents)
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

    }

    public interface ReadResultN : ReadResult
    {
        uint Index { get; }
    }

    public class StructReadResult<T> : ReadResultN, ReadableN where T : Readable
    {
        private static readonly ReadResult[] EMPTY = new ReadResult[0];

        public readonly T value;
        private long pos, sz;
        private uint index;
        private readonly string name;

        public long Bytes { get { return sz; } }
        public long Pos { get { return pos; } }
        public long Sz { get { return sz; } }
        public uint Index { get { return index; } }

        public StructReadResult(string name)
        {
            this.name = name;
        }

        public StructReadResult(string name, T value) : this(name)
        {
            this.value = value;
        }

        public long Read(MemoryMappedViewAccessor accessor, long pos)
        {
            return Read(accessor, pos, 0);
        }

        public long Read(MemoryMappedViewAccessor accessor, long pos, uint index)
        {
            this.index = index;
            sz = (pos = value.Read(accessor, this.pos = pos)) - this.pos;
            return pos;
        }

        public ReadResult[] ReadResultComponents
        {
            get
            {
                return EMPTY;
            }
        }

        public string getName()
        {
            return name;
        }
    }

    public class StructTreeNodeProviderReadResult<T> : StructReadResult<T>, TreeNodeProvider where T : Readable, TreeNodeProvider
    {
        public StructTreeNodeProviderReadResult(string name, T value) : base(name, value) { }
        public StructTreeNodeProviderReadResult(string name) : base(name) { }

        public T1[] treeNodes<T1>(TreeNodeMaker<T1> maker)
        {
            return value.treeNodes(maker);
        }
    }

    public interface Readable
    {
        long Read(MemoryMappedViewAccessor accessor, long pos);
    }

    public interface ReadableN : Readable
    {
        uint Index { get; }
        long Read(MemoryMappedViewAccessor accessor, long pos, uint index);
    }


}
