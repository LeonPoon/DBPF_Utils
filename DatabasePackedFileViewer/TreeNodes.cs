using DBPF;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Windows.Forms;
using System.Collections.Generic;

namespace DatabasePackedFileViewer
{
    internal class FileTreeNode : TreeNode
    {

        public new OpenedFileNode Tag
        {
            get
            {
                return (OpenedFileNode)base.Tag;
            }
            set
            {
                base.Tag = value;
            }
        }
    }

    internal class InstanceTreeNode : TreeNode
    {
        public new EntryModel Tag
        {
            get
            {
                return (EntryModel)base.Tag;
            }
            set
            {
                base.Tag = value;
            }
        }
    }

    internal class SimpleTreeNode : TreeNode
    {
        public SimpleTreeNode(object tag, string name)
        {
            Tag = tag;
            Text = name;
        }
    }
}

