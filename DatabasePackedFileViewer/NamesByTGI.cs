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
using DBPF;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO.MemoryMappedFiles;
using System.ComponentModel;

namespace DatabasePackedFileViewer
{
    public class ViewModel : Component
    {
        public readonly MemoryMappedViewAccessor accessor;
        public readonly DisplayInfo dispInfo;
        public readonly EntryModel model;
        public readonly long sz;
        private readonly MemoryMappedFile mmf;

        public XTabPage tabPage;

        public ViewModel(EntryModel model, MemoryMappedFile mmf, MemoryMappedViewAccessor accessor, long sz, DisplayInfo dispInfo)
        {
            this.mmf = mmf;
            this.model = model;
            this.accessor = accessor;
            this.sz = sz;
            this.dispInfo = dispInfo;
            model.ViewModel = this;
        }

        protected override void Dispose(bool disposing)
        {
            model.ViewModel = null;
            try
            {
                base.Dispose(disposing);
            }
            finally
            {
                try
                {
                    accessor.Dispose();
                }
                finally
                {
                    if (mmf != null)
                        mmf.Dispose();
                }
            }
        }

        internal string getViewName()
        {
            return dispInfo.getViewName(model, accessor, sz);
        }
    }

    public interface ViewerFactory
    {
        string getViewName(EntryModel model, MemoryMappedViewAccessor accessor, long sz);
        string getCategoryName(EntryModel model, MemoryMappedViewAccessor accessor, long sz);
        ViewModel createViewModel(EntryModel model);
        string getExtensionFilter(EntryModel model, MemoryMappedViewAccessor accessor, long sz);
    }

    public interface DisplayInfo
    {
        string getExtensionFilter(MemoryMappedViewAccessor accessor, long sz);
        string getViewName(EntryModel model, MemoryMappedViewAccessor accessor, long sz);
        Control createControl(ViewModel viewModel);
    }

    public class DefaultViewerFactory : ViewerFactory
    {
        private static DisplayInfo DEFAULT_DISPLAY_INFO = new TextDisplayInfo();
        private string name;

        public DefaultViewerFactory(String s)
        {
            this.name = s;
        }

        public ViewModel createViewModel(EntryModel model)
        {
            long sz;
            MemoryMappedFile mmf;
            var accessor = model.getAccessor(out mmf, out sz);
            try
            {
                var disp = getDisplayInfo(model, accessor, sz);
                var view = new ViewModel(model, mmf, accessor, sz, disp);
                return view;
            }
            catch
            {
                accessor.Dispose();
                throw;
            }
        }

        private DisplayInfo getDisplayInfo(EntryModel model, MemoryMappedViewAccessor accessor, long sz)
        {
            for (var x = ImageDisplay.getImageFileInfo(accessor, sz); x != null;)
                return x;
            return DEFAULT_DISPLAY_INFO;
        }

        public string getCategoryName(EntryModel model, MemoryMappedViewAccessor accessor, long sz)
        {
            // should detect instead
            return name;
        }

        public string getViewName(EntryModel model, MemoryMappedViewAccessor accessor, long sz)
        {
            return name ?? getDisplayInfo(model, accessor, sz).getViewName(model, accessor, sz);
        }

        public string getExtensionFilter(EntryModel model, MemoryMappedViewAccessor accessor, long sz)
        {
            return getDisplayInfo(model, accessor, sz).getExtensionFilter(accessor, sz);
        }
    }

    public interface NamesByTGI
    {
        ViewerFactory getFor(TypeGroupInstance tgi);
    }

    public class Simcity4NamesByTGI : NamesByTGI
    {
        internal static readonly ViewerFactory DEF_VIEW_FACT = new DefaultViewerFactory(null);
        internal static readonly Dictionary<TypeGroupInstance, ViewerFactory> FACTS = new Dictionary<TypeGroupInstance, ViewerFactory>();

        static Simcity4NamesByTGI()
        {
            // savegames
            FACTS.Add(new TypeGroupInstance(0xca16374f, 0, 0), new DefaultViewerFactory("Network Subfile 2"));
            FACTS.Add(new TypeGroupInstance(0xc9c05c6e, 0, 0), new DefaultViewerFactory("Network Subfile 1"));
            FACTS.Add(new TypeGroupInstance(0x6a0f82b2, 0, 0), new DefaultViewerFactory("Network Index Subfile"));
            FACTS.Add(new TypeGroupInstance(0x8a2482b9, 0x4a2482bb, 0), new DefaultViewerFactory("Savegame PNG"));
            FACTS.Add(new TypeGroupInstance(0x856DDBAC, 0, 0), new DefaultViewerFactory("PNG"));
        }

        public ViewerFactory getFor(TypeGroupInstance tgi)
        {
            ViewerFactory fact;
            return FACTS.TryGetValue(tgi, out fact)
                || FACTS.TryGetValue(new TypeGroupInstance(tgi._typeId, tgi._instanceId, 0), out fact)
                || FACTS.TryGetValue(new TypeGroupInstance(tgi._typeId, 0, 0), out fact)
                ? fact : DEF_VIEW_FACT;
        }
    }
}
