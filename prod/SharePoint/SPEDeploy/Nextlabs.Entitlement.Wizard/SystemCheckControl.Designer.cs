namespace Nextlabs.Entitlement.Wizard
{
  partial class SystemCheckControl
  {
    /// <summary> 
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary> 
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Component Designer generated code

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    /// 
    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SystemCheckControl));
        this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
        this.messageLabel = new System.Windows.Forms.Label();
        this.SuspendLayout();
        // 
        // tableLayoutPanel
        // 
        resources.ApplyResources(this.tableLayoutPanel, "tableLayoutPanel");
        this.tableLayoutPanel.Name = "tableLayoutPanel";
        // 
        // messageLabel
        // 
        resources.ApplyResources(this.messageLabel, "messageLabel");
        this.messageLabel.ImageKey = global::Nextlabs.Entitlement.Wizard.Resources.CommonUIStrings.controlSubTitleOptions;
        this.messageLabel.Name = "messageLabel";
        // 
        // SystemCheckControl
        // 
        resources.ApplyResources(this, "$this");
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this.tableLayoutPanel);
        this.Controls.Add(this.messageLabel);
        this.Name = "SystemCheckControl";
        this.Load += new System.EventHandler(this.SystemCheckControl_Load_1);
        this.ResumeLayout(false);

    }
    #endregion

    private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
    private System.Windows.Forms.Label messageLabel;
  }
}
