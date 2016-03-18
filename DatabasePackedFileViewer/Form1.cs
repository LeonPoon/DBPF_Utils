using DBPF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DatabasePackedFileViewer
{
    public partial class Form1 : Form
    {
        private TreeModel treeModel;
        private delegate void CallableDelegate();

        public Form1()
        {
            InitializeComponent();
            openFileDialog1.InitialDirectory = Environment.SpecialFolder.MyDocuments.ToString();
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

            openModel(node, (EntryModel)node.Tag);
        }

        private void openModel(TreeNode node, EntryModel model)
        {
            TabPage tabPage = model.TabPage;

            if (tabPage == null)
            {
                var view = model.factory.createView(model);
                tabPage = model.TabPage = new TabPage(model.factory.getViewName(model));
                tabPage.Controls.Add(view);
                tabPage.Tag = model;
                tabControl1.TabPages.Add(tabPage);
            }

            tabControl1.SelectedTab = tabPage;
        }

        private void contextMenuStripTreeNodeRClick_Opening(object sender, CancelEventArgs e)
        {
            e.Cancel = !(treeView1.SelectedNode is FileTreeNode);
        }

        private void toolStripMenuItemCloseFile_Click(object sender, EventArgs e)
        {
            FileTreeNode n = (FileTreeNode)treeView1.SelectedNode;
            closeTabs(n);
            treeModel.remove((OpenedFileNode)n.Tag);
        }

        private void closeTabs(TreeNode rootNode)
        {
            List<TabPage> tabpages = new List<TabPage>();
            foreach (TabPage tab in tabControl1.TabPages)
            {
                EntryModel m = (EntryModel)tab.Tag;
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

            foreach (TabPage tab in tabpages)
            {
                ((EntryModel)tab.Tag).TabPage = null;
                tab.Tag = null;
                tabControl1.TabPages.Remove(tab);
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutBox1().ShowDialog(this);
        }
    }
}
