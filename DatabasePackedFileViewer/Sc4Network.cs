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
using Sc4Network;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Windows.Forms;

namespace DatabasePackedFileViewer
{
    class Sc4NetworkIndexSubFileViewerFactory : DefaultViewerFactory
    {
        class Sc4NetworkIndexSubFileDisplayInfo : TextDisplayInfo
        {
            public override Control createControl(ViewModel viewModel)
            {
                long pos;
                var file = NetworkIndexSubFile.instantiate(viewModel.accessor, 0, out pos);
                string str;
                using (MemoryStream ms = new MemoryStream())
                {
                    using (var sw = new StreamWriter(ms, Encoding.ASCII))
                        ReadResultWrapper.writeOut(sw, file.ReadResultComponents, viewModel.accessor);
                    str = Encoding.ASCII.GetString(ms.GetBuffer());
                }
                TextDisplay disp = new TextDisplay();
                disp.textBox1.Text = str;
                return disp;
            }
        }

        private static readonly Sc4NetworkIndexSubFileDisplayInfo DEFAULT_DISPLAY_INFO = new Sc4NetworkIndexSubFileDisplayInfo();

        public Sc4NetworkIndexSubFileViewerFactory() : base("Network Index Subfile")
        {
        }

        protected override DisplayInfo getDisplayInfo(EntryModel model, MemoryMappedViewAccessor accessor, long sz)
        {
            return DEFAULT_DISPLAY_INFO;
        }

    }
}
