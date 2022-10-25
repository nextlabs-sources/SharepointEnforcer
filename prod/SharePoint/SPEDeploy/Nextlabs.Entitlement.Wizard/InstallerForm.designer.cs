namespace Nextlabs.Entitlement.Wizard
{
  partial class InstallerForm
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

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InstallerForm));
        this.contentPanel = new System.Windows.Forms.Panel();
        this.buttonPanel = new System.Windows.Forms.TableLayoutPanel();
        this.cancelButton = new System.Windows.Forms.Button();
        this.prevButton = new System.Windows.Forms.Button();
        this.nextButton = new System.Windows.Forms.Button();
        this.vendorLabel = new System.Windows.Forms.Label();
        this.topSeparatorPanel = new System.Windows.Forms.Panel();
        this.bottomSeparatorPanel = new System.Windows.Forms.Panel();
        this.titlePanel = new System.Windows.Forms.Panel();
        this.logoPicture = new System.Windows.Forms.PictureBox();
        this.subTitleLabel = new System.Windows.Forms.Label();
        this.titleLabel = new System.Windows.Forms.Label();
        this.buttonPanel.SuspendLayout();
        this.titlePanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.logoPicture)).BeginInit();
        this.SuspendLayout();
        // 
        // contentPanel
        // 
        resources.ApplyResources(this.contentPanel, "contentPanel");
        this.contentPanel.Name = "contentPanel";
        // 
        // buttonPanel
        // 
        resources.ApplyResources(this.buttonPanel, "buttonPanel");
        this.buttonPanel.Controls.Add(this.cancelButton, 3, 0);
        this.buttonPanel.Controls.Add(this.prevButton, 1, 0);
        this.buttonPanel.Controls.Add(this.nextButton, 2, 0);
        this.buttonPanel.Controls.Add(this.vendorLabel, 0, 0);
        this.buttonPanel.Name = "buttonPanel";
        // 
        // cancelButton
        // 
        resources.ApplyResources(this.cancelButton, "cancelButton");
        this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.cancelButton.ImageKey = global::Nextlabs.Entitlement.Wizard.Resources.CommonUIStrings_en_US.controlSubTitleOptions;
        this.cancelButton.Name = "cancelButton";
        this.cancelButton.UseVisualStyleBackColor = true;
        this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
        // 
        // prevButton
        // 
        resources.ApplyResources(this.prevButton, "prevButton");
        this.prevButton.ImageKey = global::Nextlabs.Entitlement.Wizard.Resources.CommonUIStrings_en_US.controlSubTitleOptions;
        this.prevButton.Name = "prevButton";
        this.prevButton.UseVisualStyleBackColor = true;
        this.prevButton.Click += new System.EventHandler(this.prevButton_Click);
        // 
        // nextButton
        // 
        resources.ApplyResources(this.nextButton, "nextButton");
        this.nextButton.ImageKey = global::Nextlabs.Entitlement.Wizard.Resources.CommonUIStrings_en_US.controlSubTitleOptions;
        this.nextButton.Name = "nextButton";
        this.nextButton.UseVisualStyleBackColor = true;
        this.nextButton.Click += new System.EventHandler(this.nextButton_Click);
        // 
        // vendorLabel
        // 
        resources.ApplyResources(this.vendorLabel, "vendorLabel");
        this.vendorLabel.ForeColor = System.Drawing.SystemColors.GrayText;
        this.vendorLabel.ImageKey = global::Nextlabs.Entitlement.Wizard.Resources.CommonUIStrings_en_US.controlSubTitleOptions;
        this.vendorLabel.Name = "vendorLabel";
        this.vendorLabel.UseCompatibleTextRendering = true;
        // 
        // topSeparatorPanel
        // 
        resources.ApplyResources(this.topSeparatorPanel, "topSeparatorPanel");
        this.topSeparatorPanel.BackColor = System.Drawing.SystemColors.ControlDark;
        this.topSeparatorPanel.Name = "topSeparatorPanel";
        // 
        // bottomSeparatorPanel
        // 
        resources.ApplyResources(this.bottomSeparatorPanel, "bottomSeparatorPanel");
        this.bottomSeparatorPanel.BackColor = System.Drawing.SystemColors.ControlDark;
        this.bottomSeparatorPanel.Name = "bottomSeparatorPanel";
        // 
        // titlePanel
        // 
        resources.ApplyResources(this.titlePanel, "titlePanel");
        this.titlePanel.BackColor = System.Drawing.Color.White;
        this.titlePanel.Controls.Add(this.logoPicture);
        this.titlePanel.Controls.Add(this.subTitleLabel);
        this.titlePanel.Controls.Add(this.titleLabel);
        this.titlePanel.Name = "titlePanel";
        // 
        // logoPicture
        // 
        resources.ApplyResources(this.logoPicture, "logoPicture");
        this.logoPicture.BackColor = System.Drawing.Color.Transparent;
        this.logoPicture.Name = "logoPicture";
        this.logoPicture.TabStop = false;
        // 
        // subTitleLabel
        // 
        resources.ApplyResources(this.subTitleLabel, "subTitleLabel");
        this.subTitleLabel.BackColor = System.Drawing.Color.Transparent;
        this.subTitleLabel.ImageKey = global::Nextlabs.Entitlement.Wizard.Resources.CommonUIStrings_en_US.controlSubTitleOptions;
        this.subTitleLabel.Name = "subTitleLabel";
        // 
        // titleLabel
        // 
        resources.ApplyResources(this.titleLabel, "titleLabel");
        this.titleLabel.BackColor = System.Drawing.Color.Transparent;
        this.titleLabel.ImageKey = global::Nextlabs.Entitlement.Wizard.Resources.CommonUIStrings_en_US.controlSubTitleOptions;
        this.titleLabel.Name = "titleLabel";
        // 
        // InstallerForm
        // 
        resources.ApplyResources(this, "$this");
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.CancelButton = this.cancelButton;
        this.ControlBox = false;
        this.Controls.Add(this.topSeparatorPanel);
        this.Controls.Add(this.bottomSeparatorPanel);
        this.Controls.Add(this.contentPanel);
        this.Controls.Add(this.titlePanel);
        this.Controls.Add(this.buttonPanel);
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "InstallerForm";
        this.buttonPanel.ResumeLayout(false);
        this.buttonPanel.PerformLayout();
        this.titlePanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this.logoPicture)).EndInit();
        this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.Panel titlePanel;
    private System.Windows.Forms.Panel contentPanel;
    private System.Windows.Forms.Panel topSeparatorPanel;
    private System.Windows.Forms.Panel bottomSeparatorPanel;
    private System.Windows.Forms.Button nextButton;
    private System.Windows.Forms.Button cancelButton;
    private System.Windows.Forms.Button prevButton;
    private System.Windows.Forms.Label titleLabel;
    private System.Windows.Forms.Label subTitleLabel;
    private System.Windows.Forms.TableLayoutPanel buttonPanel;
    private System.Windows.Forms.PictureBox logoPicture;
    private System.Windows.Forms.Label vendorLabel;
  }
}