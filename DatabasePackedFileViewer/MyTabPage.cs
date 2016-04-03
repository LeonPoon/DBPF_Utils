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

using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace DatabasePackedFileViewer
{
    public partial class MyTabPage : UserControl
    {
        public ViewModel ViewModel { get { return (ViewModel)Tag; } set { Tag = setViewModel(value); } }
        public new IDisposable Tag { get { return (IDisposable)base.Tag; } set { base.Tag = value; } }

        public MyTabPage()
        {
            InitializeComponent();
            if (components == null)
                components = new Container();
        }

        private ViewModel setViewModel(ViewModel viewModel)
        {
            ViewModel ViewModel = this.ViewModel;
            if (viewModel == ViewModel)
                return viewModel;

            if (ViewModel != null)
            {
                toolStripContainer1.ContentPanel.Controls.Clear();
                components.Remove(ViewModel);
                ViewModel = null;
            }

            if (viewModel != null)
            {
                var control = viewModel.dispInfo.createControl(viewModel);
                control.Dock = DockStyle.Fill;
                toolStripContainer1.ContentPanel.Controls.Add(control);
                components.Add(viewModel);
            }

            return viewModel;
        }

    }

    public class XTabPage : TabPage
    {
        public ViewModel ViewModel
        {
            get { return myTabPage.ViewModel; }
            set
            {
                myTabPage.ViewModel = value;
                if (value != null)
                    Text = value.getViewName();
            }
        }

        public readonly MyTabPage myTabPage = new MyTabPage();

        public XTabPage()
        {
            myTabPage.Dock = DockStyle.Fill;
            Controls.Add(myTabPage);
        }
    }
}
