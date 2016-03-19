using System;
using DBPF;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace DatabasePackedFileViewer
{
    public class ViewModel : IDisposable
    {
        public readonly MemoryMappedViewAccessor accessor;
        public readonly DisplayInfo disp;
        public readonly EntryModel model;
        public readonly long sz;

        public ViewModel(EntryModel model, MemoryMappedViewAccessor accessor, long sz, DisplayInfo disp)
        {
            this.model = model;
            this.accessor = accessor;
            this.sz = sz;
            this.disp = disp;
        }

        public void Dispose()
        {
            accessor.Dispose();
        }

        internal string getViewName()
        {
            return disp.getViewName(model, accessor, sz);
        }
    }

    public interface ViewerFactory
    {
        string getViewName(EntryModel model, MemoryMappedViewAccessor accessor, long sz);
        string getCategoryName(EntryModel model, MemoryMappedViewAccessor accessor, long sz);
        ViewModel createView(EntryModel model, MemoryMappedViewAccessor accessor, long sz);
        string getExtensionFilter(EntryModel model, MemoryMappedViewAccessor accessor, long sz);
    }

    public interface DisplayInfo
    {
        string getExtensionFilter(MemoryMappedViewAccessor accessor, long sz);
        string getViewName(EntryModel model, MemoryMappedViewAccessor accessor, long sz);
        Control createControl(EntryModel model, MemoryMappedViewAccessor accessor, long sz);
    }

    internal class BaseDisplayInfo : DisplayInfo
    {
        public Control createControl(EntryModel model, MemoryMappedViewAccessor accessor, long sz)
        {
            byte[] bytes = new byte[sz];
            accessor.ReadArray(0, bytes, 0, bytes.Length);
            var lbl = new Label();
            lbl.Text = Encoding.ASCII.GetString(bytes);
            return lbl;
        }

        public string getExtensionFilter(MemoryMappedViewAccessor accessor, long sz)
        {
            return "Binary file|*.bin";
        }

        public string getViewName(EntryModel model, MemoryMappedViewAccessor accessor, long sz)
        {
            return model.ToString();
        }
    }

    public class DefaultViewerFactory : ViewerFactory
    {
        private static DisplayInfo DEFAULT_DISPLAY_INFO = new BaseDisplayInfo();
        private string name;

        public DefaultViewerFactory(String s)
        {
            this.name = s;
        }

        public ViewModel createView(EntryModel model, MemoryMappedViewAccessor accessor, long sz)
        {
            var disp = getDisplayInfo(model, accessor, sz);
            var view = new ViewModel(model, accessor, sz, disp);
            return view;
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
            FACTS.Add(new TypeGroupInstance(0x8a2482b9, 0x4a2482bb, 0), new DefaultViewerFactory("PNG"));
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
