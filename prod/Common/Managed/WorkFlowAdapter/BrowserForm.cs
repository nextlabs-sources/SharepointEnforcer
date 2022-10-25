using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WorkFlowAdapter
{
    public partial class BrowserForm : Form
    {
        public BrowserForm()
        {
            InitializeComponent();
            //HideCloseButton = true;
            bCloseInside = false;
            m_iDeltaHeight = this.Height - this.webBrowser.Height;
            m_iDeltaWidth = this.Width - this.webBrowser.Width;
            this.WindowState = FormWindowState.Maximized;
            return;
        }
        public void Navigate(string strUrl)
        {
            webBrowser.Navigate(strUrl);
        }
        public void Stop()
        {
            webBrowser.Stop();
        }
        private void BrowserForm_Resize(object sender, EventArgs e)
        {
            this.webBrowser.Height = this.Height - m_iDeltaHeight;
            this.webBrowser.Width = this.Width - m_iDeltaWidth;
        }
        int m_iDeltaHeight;
        int m_iDeltaWidth;
        public bool bCloseInside;
        //public bool HideCloseButton
        //{
        //    get { return _HideCloseButton; }
        //    set { _HideCloseButton = value; }
        //}
        //private bool _HideCloseButton;

    }
}