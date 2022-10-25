
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace Nextlabs.Entitlement.Wizard
{
  public partial class UpgradeControl : InstallerControl
  {
    private readonly InstallProcessControl processControl;

    public UpgradeControl()
    {
      this.processControl = Program.CreateProcessControl();
      InitializeComponent();

      messageLabel.Text = InstallConfiguration.FormatString(messageLabel.Text);

      string upgradeDescription = InstallConfiguration.UpgradeDescription;
      if (upgradeDescription != null)
      {
        upgradeDescriptionLabel.Text = upgradeDescription;
      }
    }

    protected internal override void Open(InstallOptions options)
    {
      bool enable = removeRadioButton.Checked || RestoreBox.Checked;
      if (removeRadioButton.Checked)
      {
          Form.Operation = InstallOperation.Uninstall;
      }
      else if (RestoreBox.Checked)
      {
          Form.Operation = InstallOperation.UpgradeRestore;
      }
      if (enable)
      {
          Form.NextButton.Enabled = enable;
      }
    }

    protected internal override void Close(InstallOptions options)
    {
      Form.ContentControls.Add(processControl);
    }

    private void removeRadioButton_CheckedChanged(object sender, EventArgs e)
    {
        if (removeRadioButton.Checked)
        {
            Form.Operation = InstallOperation.Uninstall;
            RestoreBox.Checked = false;
            Form.NextButton.Enabled = true;
        }
        else if (!RestoreBox.Checked)
        {
            Form.NextButton.Enabled = false;
        }
    }

    private void UpgradeControl_Load(object sender, EventArgs e)
    {

    }

    private void RestoreBox_CheckedChanged(object sender, EventArgs e)
    {
        if (RestoreBox.Checked)
        {
            Form.Operation = InstallOperation.UpgradeRestore;
            removeRadioButton.Checked = false;
            Form.NextButton.Enabled = true;
        }
        else if (!removeRadioButton.Checked)
        {
            Form.NextButton.Enabled = false;
        }
    }
  }
}
