using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DatabasePackedFileViewer
{
    public partial class MyTabPage : UserControl
    {
        public new XTabPage Tag
        {
            get
            {
                return (XTabPage)base.Tag;
            }
            set
            {
                base.Tag = value;
            }
        }

        private readonly Control control;
        public event EventHandler saveButton_Clicked;
        public event EventHandler closeButton_Clicked;

        public MyTabPage() : this(null)
        {
        }

        public MyTabPage(Control control)
        {
            InitializeComponent();
            if ((this.control = control) != null)
            {
                control.Dock = DockStyle.Fill;
                toolStripContainer1.ContentPanel.Controls.Add(control);
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (saveButton_Clicked != null)
                saveButton_Clicked(Tag, e);
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            if (closeButton_Clicked != null)
                closeButton_Clicked(Tag, e);
        }
    }

    public class XTabPage : TabPage
    {
        private ViewModel ViewModel
        {
            get { return Tag; }
        }
        public readonly MyTabPage control;

        public new ViewModel Tag
        {
            get
            {
                return (ViewModel)base.Tag;
            }
            set
            {
                base.Tag = value;
            }
        }

        public XTabPage(ViewModel viewModel) : base(viewModel.getViewName())
        {
            EntryModel model = viewModel.model;
            Controls.Add(control = new MyTabPage((Tag = viewModel).disp.createControl(model, viewModel.accessor, viewModel.sz))
            {
                Dock = DockStyle.Fill,
                Tag = this
            });
        }

        internal void doClosing()
        {
            ViewModel.model.TabPage = null;
            ViewModel.Dispose();
            Tag = null;
        }
    }
}
