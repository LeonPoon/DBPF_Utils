using System;
using DBPF;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace DatabasePackedFileViewer
{
    public interface ViewerFactory
    {
        string getName(EntryModel model);
        string getViewName(EntryModel model);
        Control createView(EntryModel model);
        string getExtensionFilter(EntryModel model, MemoryMappedViewAccessor accessor, long sz);
    }

    public interface DisplayInfo
    {
        string getExtensionFilter(MemoryMappedViewAccessor accessor, long sz);
    }

    public class DefaultViewerFactory : ViewerFactory
    {
        private string name;

        public DefaultViewerFactory(String s)
        {
            this.name = s;
        }

        public Control createView(EntryModel model)
        {
            Control ctrl = null;
            long sz;
            var accessor = model.getAccessor(out sz);
            try { return ctrl = createView(model, accessor, sz); }
            catch { accessor.Dispose(); throw; }
            finally
            {
                if (ctrl != null)
                {
                    ctrl.Dock = DockStyle.Fill;
                    ctrl.Tag = accessor;
                }
            }
        }

        private Control createView(EntryModel model, MemoryMappedViewAccessor accessor, long sz)
        {
            for (var x = ImageDisplay.getImageFileInfo(accessor, sz); x != null;)
                return new ImageDisplay(x, model, accessor, sz);

            byte[] bytes = new byte[sz];
            accessor.ReadArray(0, bytes, 0, bytes.Length);
            var lbl = new Label();
            lbl.Text = Encoding.ASCII.GetString(bytes);
            return lbl;
        }

        public string getName(EntryModel model)
        {
            return name;
        }

        public string getViewName(EntryModel model)
        {
            return name ?? model.ToString();
        }

        public string getExtensionFilter(EntryModel model, MemoryMappedViewAccessor accessor, long sz)
        {
            for (DisplayInfo x = ImageDisplay.getImageFileInfo(accessor, sz); x != null;)
                return x.getExtensionFilter(accessor, sz);
            return "Binary file|*.bin";
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
