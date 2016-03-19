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

            openModel(node, node.Tag);
        }

        private void openModel(TreeNode node, EntryModel model)
        {
            XTabPage tabPage = model.TabPage;

            if (tabPage == null)
            {
                long sz;
                var accessor = model.getAccessor(out sz);
                try
                {
                    var view = model.factory.createView(model, accessor, sz);
                    tabPage = model.TabPage = new XTabPage(view);
                    tabControl1.TabPages.Add(tabPage);
                    tabPage.control.saveButton_Clicked += delegate (object sender, EventArgs e) { saveButton_Clicked((XTabPage)sender, e); };
                    tabPage.control.closeButton_Clicked += delegate (object sender, EventArgs e) { closeButton_Clicked((XTabPage)sender, e); }; ;
                }
                catch { accessor.Dispose(); throw; }
            }

            tabControl1.SelectedTab = tabPage;
        }

        private void closeButton_Clicked(XTabPage sender, EventArgs e)
        {
            closeTab(tabControl1, sender);
        }

        private void saveButton_Clicked(XTabPage sender, EventArgs e)
        {
            saveFile(sender.Tag.model, sender.Text);
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
                EntryModel m = tab.Tag.model;
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
            tab.doClosing();
            tabControl1.TabPages.Remove(tab);
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
            using (var r = model.getAccessor(out sz))
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
        }
    }
}
