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
using System.IO;

namespace DatabasePackedFileViewer
{

    public partial class ImageDisplay : UserControl
    {
        private static readonly ImageDisplayInfo IMAGE_INFO_PNG = new ImageDisplayInfo(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }, "PNG file|*.png");
        private static readonly ReadOnlyCollection<ImageDisplayInfo> IMAGE_INFO_COLLECTION = Array.AsReadOnly(new ImageDisplayInfo[] {
            IMAGE_INFO_PNG
        });
        private EntryModel model;
        private MemoryMappedViewAccessor accessor;
        private long sz;

        public static ImageDisplayInfo getImageFileInfo(MemoryMappedViewAccessor accessor, long sz)
        {
            foreach (var h in IMAGE_INFO_COLLECTION)
                if (h.isThisInfo(accessor, sz))
                    return h;
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

    public class ImageDisplayInfo : DisplayInfo
    {
        private readonly string filter;
        private readonly ReadOnlyCollection<byte> headerBytes;

        public ImageDisplayInfo(byte[] headerBytes, String filter)
        {
            this.headerBytes = Array.AsReadOnly(headerBytes);
            this.filter = filter;
        }

        public bool isThisInfo(MemoryMappedViewAccessor accessor, long sz)
        {
            if (sz < headerBytes.Count)
                return false;

            byte[] bytes = new byte[headerBytes.Count];
            accessor.ReadArray(0, bytes, 0, bytes.Length);
            for (int i = 0; i < bytes.Length; i++)
                if (bytes[i] != headerBytes[i])
                    return false;
            return true;
        }

        public string getExtensionFilter(MemoryMappedViewAccessor accessor, long sz)
        {
            return filter;
        }
    }
}
