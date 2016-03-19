using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.ObjectModel;
using System.IO.MemoryMappedFiles;

namespace DatabasePackedFileViewer
{
    using System.IO;
    using ImageDisplayInfo = ReadOnlyCollection<byte>;
    public partial class ImageDisplay : UserControl
    {
        public static readonly ImageDisplayInfo BYTES_HEADER_PNG = Array.AsReadOnly(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a });
        public static readonly ReadOnlyCollection<ImageDisplayInfo> BYTES_HEADERS = Array.AsReadOnly(new ImageDisplayInfo[] {
            BYTES_HEADER_PNG
        });
        private EntryModel model;
        private MemoryMappedViewAccessor accessor;
        private long sz;

        public static ImageDisplayInfo getImageFileInfo(MemoryMappedViewAccessor accessor, long sz)
        {
            foreach (var h in BYTES_HEADERS)
                if (sz >= h.Count)
                {
                    byte[] bytes = new byte[h.Count];
                    accessor.ReadArray(0, bytes, 0, bytes.Length);
                    for (int i = 0; i < bytes.Length; i++)
                        if (bytes[i] != h[i])
                        {
                            bytes = null;
                            break;
                        }
                    if (bytes != null)
                        return h;
                }
            return null;
        }

        public ImageDisplay() : this(null, null, null, 0)
        {
        }

        public ImageDisplay(ImageDisplayInfo imageInf, EntryModel model, MemoryMappedViewAccessor accessor, long sz)
        {
            this.model = model;
            this.accessor = accessor;
            this.sz = sz;
            InitializeComponent();
        }

        private void ImageDisplay_Load(object sender, EventArgs e)
        {
            if (accessor == null)
                return;
            byte[] bytes = new byte[sz];
            accessor.ReadArray(0, bytes, 0, bytes.Length);
            using (var ms = new MemoryStream(bytes))
                pictureBox1.Image = Image.FromStream(ms);
        }
    }
}
