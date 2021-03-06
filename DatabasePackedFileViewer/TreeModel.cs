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

using DBPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Windows.Forms;

namespace DatabasePackedFileViewer
{
    internal class TreeModel
    {
        private TreeView treeView1;

        private readonly List<OpenedFileNode> openedFiles = new List<OpenedFileNode>();

        public TreeModel(TreeView treeView1)
        {
            this.treeView1 = treeView1;
        }

        public IEnumerable<OpenedFileNode> OpenedFiles { get { return openedFiles; } }

        internal void addOpenedFileNode(OpenedFileNode n)
        {
            openedFiles.Add(n);
            treeView1.Nodes.Add(n.treeNode);
        }

        internal void remove(OpenedFileNode model)
        {
            foreach (TreeNode n in treeView1.Nodes)
                if (n.Tag == model)
                {
                    treeView1.Nodes.Remove(n);
                    openedFiles.Remove(model);
                    model.mmf.Dispose();
                    return;
                }
        }
    }

    internal interface NodeModel
    {
        TreeNode TreeNode { get; }
    }

    public class OpenedFileNode : NodeModel
    {
        public readonly DBPFile dbpf;
        public readonly string fileName;
        public readonly TreeNode treeNode = new FileTreeNode();
        internal readonly MemoryMappedFile mmf;
        public TreeNode TreeNode { get { return treeNode; } }
        public readonly NamesByTGI namesProvider = new Simcity4NamesByTGI();

        public OpenedFileNode(string fileName, MemoryMappedFile mmf, DBPFile dbpf)
        {
            this.mmf = mmf;
            this.fileName = fileName;
            this.dbpf = dbpf;
            treeNode.Tag = this;
            treeNode.ToolTipText = fileName;
            treeNode.Text = Path.GetFileName(fileName);

            TreeNode rootNodeByName = new TreeNode("Nodes by Name");
            NodesByNameRootModel nodesByNameModel = new NodesByNameRootModel(rootNodeByName);
            treeNode.Nodes.Add(rootNodeByName);

            TreeNode rootNode = new TreeNode("Nodes by Type IDs");
            treeNode.Nodes.Add(rootNode);

            var indexTable = dbpf.indexTable;
            foreach (var typeId in indexTable.Keys)
            {
                var groups = indexTable[typeId];
                TreeNode node = new SimpleTreeNode(groups, String.Format("type={0:X8}", typeId));
                foreach (var groupId in groups.Keys)
                {
                    var instances = groups[groupId];
                    TreeNode groupNode = new SimpleTreeNode(instances, String.Format("group={0:X8}", groupId));
                    foreach (var instanceId in instances.Keys)
                    {
                        var inst = instances[instanceId];
                        DBDirectoryEntry dbDirEntry;
                        InstanceTreeNode n = new InstanceTreeNode();
                        EntryModel model = new EntryModel(this, inst, dbpf.dbDirEntries.TryGetValue(inst.tgi, out dbDirEntry) ? dbDirEntry : null, n);

                        groupNode.Nodes.Add(n);

                        var nodeName = model.getCategory();
                        if (nodeName != null)
                            nodesByNameModel.addToName(nodeName, model);
                    }
                    node.Nodes.Add(groupNode);
                }
                rootNode.Nodes.Add(node);
            }
        }
    }

    internal class NodesByNameModel : List<TreeNode>, NodeModel
    {
        public readonly TreeNode treeNode;
        public TreeNode TreeNode { get { return treeNode; } }

        public NodesByNameModel(string nodeName)
        {
            treeNode = new TreeNode(nodeName);
            treeNode.Tag = this;
        }

        internal void Add(EntryModel model)
        {
            TreeNode node = new InstanceTreeNode();
            node.Tag = model;
            node.Text = model.ToString();

            Add(node);
            treeNode.Nodes.Add(node);
        }
    }

    internal class NodesByNameRootModel : Dictionary<string, NodesByNameModel>, NodeModel
    {
        private readonly TreeNode rootNodeByName;
        public TreeNode TreeNode { get { return rootNodeByName; } }

        public NodesByNameRootModel(TreeNode rootNodeByName)
        {
            (this.rootNodeByName = rootNodeByName).Tag = this;
        }

        internal void addToName(string nodeName, EntryModel model)
        {
            NodesByNameModel nodes;
            if (!TryGetValue(nodeName, out nodes))
            {
                Add(nodeName, nodes = new NodesByNameModel(nodeName));
                rootNodeByName.Nodes.Add(nodes.treeNode);
            }
            nodes.Add(model);
        }
    }

    public class EntryModel : NodeModel
    {
        public readonly IndexTableEntry indexTableEntry;
        private readonly DBDirectoryEntry dBDirectoryEntry;
        public readonly TreeNode treeNode;
        public readonly ViewerFactory factory;
        public readonly OpenedFileNode fileNode;

        public TreeNode TreeNode { get { return treeNode; } }

        private ViewModel viewModel;
        public ViewModel ViewModel
        {
            get { return viewModel; }
            set
            {
                ViewModel viewModel = this.viewModel;
                if (value == viewModel)
                    return;
                if (value != null && viewModel != null)
                    throw new ArgumentException();
                this.viewModel = value;
            }
        }

        public EntryModel(OpenedFileNode fileNode, IndexTableEntry indexTableEntry, TreeNode treeNode) : this(fileNode, indexTableEntry, null, treeNode)
        {
        }

        public EntryModel(OpenedFileNode fileNode, IndexTableEntry indexTableEntry, DBDirectoryEntry dBDirectoryEntry, TreeNode treeNode)
        {
            this.fileNode = fileNode;

            this.indexTableEntry = indexTableEntry;
            this.dBDirectoryEntry = dBDirectoryEntry;
            this.treeNode = treeNode;

            treeNode.Tag = this;
            treeNode.Text = ToString();

            this.factory = fileNode.namesProvider.getFor(indexTableEntry.tgi);
        }

        public override string ToString()
        {
            return dBDirectoryEntry == null ? indexTableEntry.ToString() : String.Format("{0},inflatedSz={1}", indexTableEntry, dBDirectoryEntry.size);
        }

        public string getCategory()
        {
            return factory.getCategoryName(this, null, 0);
        }

        internal MemoryMappedViewAccessor getAccessor(out MemoryMappedFile mmf, out long sz)
        {
            sz = indexTableEntry.size;
            var accessor = fileNode.mmf.CreateViewAccessor(indexTableEntry.fileOffset, sz, MemoryMappedFileAccess.Read);
            if (dBDirectoryEntry == null)
            {
                mmf = null;
                return accessor;
            }

            try
            {
                sz = dBDirectoryEntry.size;
                mmf = MemoryMappedFile.CreateNew(Guid.NewGuid().ToString(), sz);
                using (var accessor_w = mmf.CreateViewAccessor(0, sz, MemoryMappedFileAccess.ReadWrite))
                    for (long decom = fileNode.dbpf.decompress(accessor, accessor_w); decom != sz;)
                        throw new ArgumentOutOfRangeException(string.Format("decompressed bytes: expected={0}, actual={1}", sz, decom));
                return mmf.CreateViewAccessor(0, sz, MemoryMappedFileAccess.Read);
            }
            finally
            {
                accessor.Dispose();
            }
        }
    }
}
