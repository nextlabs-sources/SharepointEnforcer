namespace Nextlabs.Entitlement.Wizard
{
    partial class FinishedControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FinishedControl));
            this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.FooterLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // tableLayoutPanel
            // 
            resources.ApplyResources(this.tableLayoutPanel, "tableLayoutPanel");
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            // 
            // FooterLabel
            // 
            resources.ApplyResources(this.FooterLabel, "FooterLabel");
            this.FooterLabel.ImageKey = global::Nextlabs.Entitlement.Wizard.Resources.CommonUIStrings.controlSubTitleOptions;
            this.FooterLabel.Name = "FooterLabel";
            // 
            // FinishedControl
            // 
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.FooterLabel);
            this.Controls.Add(this.tableLayoutPanel);
            this.Name = "FinishedControl";
            this.Load += new System.EventHandler(this.FinishedControl_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
      private System.Windows.Forms.Label FooterLabel;
    }
}
