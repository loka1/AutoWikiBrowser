﻿namespace WikiFunctions.Controls
{
    partial class PageContainsControl
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
            this.chkCaseSensitive = new System.Windows.Forms.CheckBox();
            this.chkAfterProcessing = new System.Windows.Forms.CheckBox();
            this.chkIsRegex = new System.Windows.Forms.CheckBox();
            this.txtContains = new System.Windows.Forms.RichTextBox();
            this.chkContains = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // chkCaseSensitive
            // 
            this.chkCaseSensitive.AutoSize = true;
            this.chkCaseSensitive.Location = new System.Drawing.Point(66, 26);
            this.chkCaseSensitive.Name = "chkCaseSensitive";
            this.chkCaseSensitive.Size = new System.Drawing.Size(94, 17);
            this.chkCaseSensitive.TabIndex = 10;
            this.chkCaseSensitive.Text = "Case sensitive";
            this.chkCaseSensitive.UseVisualStyleBackColor = true;
            this.chkCaseSensitive.CheckedChanged += new System.EventHandler(this.Invalidate_CheckedChanged);
            // 
            // chkAfterProcessing
            // 
            this.chkAfterProcessing.AutoSize = true;
            this.chkAfterProcessing.Location = new System.Drawing.Point(169, 26);
            this.chkAfterProcessing.Name = "chkAfterProcessing";
            this.chkAfterProcessing.Size = new System.Drawing.Size(81, 17);
            this.chkAfterProcessing.TabIndex = 11;
            this.chkAfterProcessing.Text = "Check after";
            this.chkAfterProcessing.UseVisualStyleBackColor = true;
            // 
            // chkIsRegex
            // 
            this.chkIsRegex.AutoSize = true;
            this.chkIsRegex.Location = new System.Drawing.Point(0, 26);
            this.chkIsRegex.Name = "chkIsRegex";
            this.chkIsRegex.Size = new System.Drawing.Size(57, 17);
            this.chkIsRegex.TabIndex = 9;
            this.chkIsRegex.Text = "Regex";
            this.chkIsRegex.UseVisualStyleBackColor = true;
            this.chkIsRegex.CheckedChanged += new System.EventHandler(this.Invalidate_CheckedChanged);
            // 
            // txtContains
            // 
            this.txtContains.DetectUrls = false;
            this.txtContains.Enabled = false;
            this.txtContains.Location = new System.Drawing.Point(100, 1);
            this.txtContains.Multiline = false;
            this.txtContains.Name = "txtContains";
            this.txtContains.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.txtContains.Size = new System.Drawing.Size(154, 20);
            this.txtContains.TabIndex = 8;
            this.txtContains.Text = "";
            // 
            // chkContains
            // 
            this.chkContains.Location = new System.Drawing.Point(0, 3);
            this.chkContains.Name = "chkContains";
            this.chkContains.Size = new System.Drawing.Size(89, 17);
            this.chkContains.TabIndex = 7;
            this.chkContains.Text = "&Contains:";
            this.chkContains.UseVisualStyleBackColor = true;
            this.chkContains.CheckedChanged += new System.EventHandler(this.chkSkipIfContains_CheckedChanged);
            // 
            // PageContainsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.chkCaseSensitive);
            this.Controls.Add(this.chkAfterProcessing);
            this.Controls.Add(this.chkIsRegex);
            this.Controls.Add(this.txtContains);
            this.Controls.Add(this.chkContains);
            this.Name = "PageContainsControl";
            this.Size = new System.Drawing.Size(260, 46);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox chkCaseSensitive;
        private System.Windows.Forms.CheckBox chkAfterProcessing;
        private System.Windows.Forms.CheckBox chkIsRegex;
        private System.Windows.Forms.RichTextBox txtContains;
        private System.Windows.Forms.CheckBox chkContains;
    }
}
