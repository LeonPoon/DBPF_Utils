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

using DBPF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;

namespace DatabasePackedFileViewer
{
    public partial class Form1 : Form
    {
        private TreeModel treeModel;
        private delegate void CallableDelegate();
        private string memText;

        public Form1()
        {
            InitializeComponent();
            openFileDialog1.InitialDirectory = Environment.SpecialFolder.MyDocuments.ToString();
            memText = lblMem.Text;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            userOpenFile();
        }

        private DialogResult userOpenFile()
        {
            DialogResult ret = openFileDialog1.ShowDialog(this);
            if (ret == DialogResult.OK)
            {
                bgWorkerOpenFile.RunWorkerAsync(canon(openFileDialog1.FileName));
            }
            return ret;
        }

        private void addFileToModel(string v)
        {
            throw new NotImplementedException();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            userOpenFile();
        }

        private void toolStripOpenButton_Click(object sender, EventArgs e)
        {
            userOpenFile();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            OpenFileDialog dlg = (OpenFileDialog)sender;
            var fpath = canon(dlg.FileName);
            foreach (OpenedFileNode n in treeModel.OpenedFiles)
                if (e.Cancel = string.Equals(n.fileName, fpath, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(this, dlg.FileName, "Already Opened", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }
        }

        private static string canon(string fpath)
        {
            return Path.GetFullPath(fpath);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            treeModel = new TreeModel(treeView1);
        }

        private void bgWorkerOpenFile_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = (BackgroundWorker)sender;
            string fileName = e.Argument as string;

            var mmf = MemoryMappedFile.CreateFromFile(fileName, System.IO.FileMode.Open);
            var f = Mapper.map(mmf);
            OpenedFileNode n = new OpenedFileNode(fileName, mmf, f);

            e.Result = n;

        }

        private void bgWorkerOpenFile_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            OpenedFileNode n = (OpenedFileNode)e.Result;
            treeModel.addOpenedFileNode(n);
        }

        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            TreeView tv = (TreeView)sender;
            var node = e.Node as InstanceTreeNode;
            if (node == null)
                return;

            openModel(node, node.Tag);
        }

        private void openModel(TreeNode node, EntryModel model)
        {
            ViewModel viewModel = model.ViewModel;

            if (viewModel == null)
            {
                viewModel = model.factory.createViewModel(model);
                XTabPage tabPage = viewModel.tabPage = new XTabPage();
                tabPage.ViewModel = viewModel;
                tabPage.myTabPage.saveButton.Click += delegate (object sender, EventArgs e) { saveFile(viewModel.model, tabPage.Text); };
                tabPage.myTabPage.closeButton.Click += delegate (object sender, EventArgs e) { closeTab(tabControl1, tabPage); }; ;

                tabControl1.TabPages.Add(tabPage);
            }

            tabControl1.SelectedTab = viewModel.tabPage;
        }

        private void contextMenuStripTreeNodeRClick_Opening(object sender, CancelEventArgs e)
        {
            var strip = (ContextMenuStrip)sender;
            showHideAllContextMenuItems(strip, false);

            for (var n = treeView1.SelectedNode as FileTreeNode; n != null;)
            {
                toolStripMenuItemCloseFile.Visible = true;
                return;
            }

            for (var n = treeView1.SelectedNode as InstanceTreeNode; n != null;)
            {
                saveFileToolStripMenuItem.Visible = true;
                return;
            }

            showHideAllContextMenuItems(strip, true);
            e.Cancel = true;
        }

        private void toolStripMenuItemCloseFile_Click(object sender, EventArgs e)
        {
            FileTreeNode n = (FileTreeNode)treeView1.SelectedNode;
            closeTabs(n);
            treeModel.remove(n.Tag);
        }

        private void closeTabs(TreeNode rootNode)
        {
            List<XTabPage> tabpages = new List<XTabPage>();
            foreach (XTabPage tab in tabControl1.TabPages)
            {
                EntryModel m = tab.ViewModel.model;
                TreeNode node = m.TreeNode;
                while (node != null)
                    if (node == rootNode)
                    {
                        tabpages.Add(tab);
                        break;
                    }
                    else
                        node = node.Parent;
            }
            foreach (XTabPage tab in tabpages)
                closeTab(tabControl1, tab);
        }

        private void closeTab(TabControl tabControl1, XTabPage tab)
        {
            tabControl1.TabPages.Remove(tab);
            tab.Dispose();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutBox1().ShowDialog(this);
        }

        private void contextMenuStripTreeNodeRClick_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            showHideAllContextMenuItems((ContextMenuStrip)sender, true);
        }

        private void showHideAllContextMenuItems(ContextMenuStrip strip, bool v)
        {
            foreach (ToolStripItem i in strip.Items)
                i.Visible = v;
        }

        private void saveFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InstanceTreeNode node = (InstanceTreeNode)treeView1.SelectedNode;
            var model = node.Tag;
            saveFile(model, node.Text);
        }

        private void saveFile(EntryModel model, string fileName)
        {
            saveFileDialog1.Title = string.Format("Save {0}", saveFileDialog1.FileName = fileName);

            long sz;
            MemoryMappedFile mmf;
            using (var r = model.getAccessor(out mmf, out sz))
                try
                {
                    saveFileDialog1.Filter = model.factory.getExtensionFilter(model, r, sz);
                    saveFileDialog1.DefaultExt = saveFileDialog1.Filter.Split('|')[1].Split(new char[] { '.' }, 2)[1];
                    if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                    {
                        using (var w = saveFileDialog1.OpenFile())
                        {
                            byte[] bytes = new byte[4096];
                            for (long pos = 0, s; sz > 0; pos += s, sz -= s)
                            {
                                s = sz > bytes.Length ? bytes.Length : sz;
                                r.ReadArray(pos, bytes, 0, (int)s);
                                w.Write(bytes, 0, (int)s);
                            }
                        }
                    }
                }
                finally
                {
                    if (mmf != null)
                        mmf.Dispose();
                }
        }

        private void gCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GC.Collect();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker w = (BackgroundWorker)sender;
            for (int i = 0; !w.CancellationPending; i++)
            {
                Barrier barrier = new Barrier(2);
                object o = new Tuple<Barrier, object>(barrier, e.Argument);
                w.ReportProgress(i, o);
                barrier.SignalAndWait();
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var bgw = (BackgroundWorker)sender;
            if (e.ProgressPercentage > 50)
            {
                bgw.CancelAsync();
                return;
            }
            Tuple<Barrier, object> o = (Tuple<Barrier, object>)e.UserState;
            {
                InstanceTreeNode n = (InstanceTreeNode)o.Item2;
                var m = n.Tag;
                var vm = m.ViewModel;
                if (vm == null)
                    openModel(n, m);
                else
                    closeTab(tabControl1, vm.tabPage);
                o.Item1.SignalAndWait();
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (backgroundWorker1.IsBusy)
                backgroundWorker1.CancelAsync();
            else
                backgroundWorker1.RunWorkerAsync(findNode(treeView1.Nodes));
        }

        private InstanceTreeNode findNode(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                var instanceNode = node as InstanceTreeNode;
                if ((instanceNode ?? (instanceNode = findNode(node.Nodes))) != null && instanceNode.Tag.indexTableEntry.size == 17067760)
                    return instanceNode;
            }
            return null;
        }

        private void memTimer_Tick(object sender, EventArgs e)
        {
            lblMem.Text = string.Format("{0}{1}M", memText, (Process.GetCurrentProcess().WorkingSet64 / 1048576).ToString(CultureInfo.InvariantCulture));
        }
    }
}
