
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace Nextlabs.Entitlement.Wizard
{
  public partial class EULAControl : InstallerControl
  {
    public EULAControl()
    {
      InitializeComponent();

      this.Load += new EventHandler(EULAControl_Load);
    }

    protected internal override void Open(InstallOptions options)
    {
      Form.NextButton.Enabled = acceptCheckBox.Checked; 
    }

    private void EULAControl_Load(object sender, EventArgs e)
    {

      string filename = InstallConfiguration.EULA;
      if (!String.IsNullOrEmpty(filename))
      {
        try
        {
          this.richTextBox.LoadFile(filename);
          acceptCheckBox.Enabled = true;
        }

        catch (IOException)
        {
          this.richTextBox.Lines = new string[] { "Error! Could not load " + filename };
        }
      }
    }

    private void acceptCheckBox_CheckedChanged(object sender, EventArgs e)
    {
      Form.NextButton.Enabled = acceptCheckBox.Checked;
    }

    private void EULAControl_Load_1(object sender, EventArgs e)
    {

    }


  }
}
