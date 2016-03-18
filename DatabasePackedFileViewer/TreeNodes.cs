﻿using DBPF;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Windows.Forms;
using System.Collections.Generic;

namespace DatabasePackedFileViewer
{
    internal class FileTreeNode : TreeNode
    {
    }

    internal class InstanceTreeNode : TreeNode
    {
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
