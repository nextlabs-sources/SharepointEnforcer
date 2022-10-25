namespace Nextlabs.Entitlement.Wizard
{
  partial class CompletionControl
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
    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CompletionControl));
        this.detailsTextBox = new System.Windows.Forms.TextBox();
        this.detailsLabel = new System.Windows.Forms.Label();
        this.SuspendLayout();
        // 
        // detailsTextBox
        // 
        resources.ApplyResources(this.detailsTextBox, "detailsTextBox");
        this.detailsTextBox.BackColor = System.Drawing.Color.White;
        this.detailsTextBox.Name = "detailsTextBox";
        this.detailsTextBox.ReadOnly = true;
        // 
        // detailsLabel
        // 
        resources.ApplyResources(this.detailsLabel, "detailsLabel");
        this.detailsLabel.ImageKey = global::Nextlabs.Entitlement.Wizard.Resources.CommonUIStrings_en_US.controlSubTitleOptions;
        this.detailsLabel.Name = "detailsLabel";
        // 
        // CompletionControl
        // 
        resources.ApplyResources(this, "$this");
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this.detailsTextBox);
        this.Controls.Add(this.detailsLabel);
        this.Name = "CompletionControl";
        this.ResumeLayout(false);
        this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.TextBox detailsTextBox;
    private System.Windows.Forms.Label detailsLabel;
  }
}
