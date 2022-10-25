
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace Nextlabs.Entitlement.Wizard
{
  public partial class WelcomeControl : InstallerControl
  {
    public WelcomeControl()
    {
      InitializeComponent();

      messageLabel.Text = InstallConfiguration.FormatString(messageLabel.Text);
    }

    private void WelcomeControl_Load(object sender, EventArgs e)
    {

    }
  }
}
