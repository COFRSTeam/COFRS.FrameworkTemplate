namespace COFRS.Template
{
	partial class UserInputProject
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
            this.label1 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.SecuritySelector = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this.databaseTechnology = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.CompanyMoniker = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 32F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(454, 51);
            this.label1.TabIndex = 0;
            this.label1.Text = "COFRS REST Service";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::COFRS.Template.Properties.Resources.ico128;
            this.pictureBox1.Location = new System.Drawing.Point(21, 86);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(128, 128);
            this.pictureBox1.TabIndex = 1;
            this.pictureBox1.TabStop = false;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(162, 86);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(190, 44);
            this.label2.TabIndex = 2;
            this.label2.Text = "The Cookbook For RESTFul Services (COFRS) assists the developer in the cretion of" +
    " RESTful Services. ";
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(162, 145);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(190, 84);
            this.label3.TabIndex = 3;
            this.label3.Text = "We recommend that you protect your service using the OAuth2 / OpenID Connect prot" +
    "ocol. However, this functionality can be added later if you choose not to do so " +
    "initially.";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(401, 75);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(77, 13);
            this.label5.TabIndex = 6;
            this.label5.Text = "Security Model";
            // 
            // SecuritySelector
            // 
            this.SecuritySelector.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.SecuritySelector.FormattingEnabled = true;
            this.SecuritySelector.Items.AddRange(new object[] {
            "None",
            "OAuth2 / OpenId Connect"});
            this.SecuritySelector.Location = new System.Drawing.Point(404, 91);
            this.SecuritySelector.Name = "SecuritySelector";
            this.SecuritySelector.Size = new System.Drawing.Size(281, 21);
            this.SecuritySelector.TabIndex = 7;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(401, 126);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(112, 13);
            this.label6.TabIndex = 8;
            this.label6.Text = "Database Technology";
            // 
            // pictureBox2
            // 
            this.pictureBox2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pictureBox2.Location = new System.Drawing.Point(-19, 249);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(1025, 4);
            this.pictureBox2.TabIndex = 12;
            this.pictureBox2.TabStop = false;
            // 
            // _okButton
            // 
            this._okButton.Location = new System.Drawing.Point(521, 13);
            this._okButton.Name = "_okButton";
            this._okButton.Size = new System.Drawing.Size(75, 23);
            this._okButton.TabIndex = 13;
            this._okButton.Text = "OK";
            this._okButton.UseVisualStyleBackColor = true;
            this._okButton.Click += new System.EventHandler(this.OnOK);
            // 
            // _cancelButton
            // 
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.Location = new System.Drawing.Point(616, 13);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.Size = new System.Drawing.Size(75, 23);
            this._cancelButton.TabIndex = 14;
            this._cancelButton.Text = "Cancel";
            this._cancelButton.UseVisualStyleBackColor = true;
            // 
            // databaseTechnology
            // 
            this.databaseTechnology.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.databaseTechnology.FormattingEnabled = true;
            this.databaseTechnology.Items.AddRange(new object[] {
            "My SQL",
            "Postgresql",
            "SQL Server"});
            this.databaseTechnology.Location = new System.Drawing.Point(404, 142);
            this.databaseTechnology.Name = "databaseTechnology";
            this.databaseTechnology.Size = new System.Drawing.Size(281, 21);
            this.databaseTechnology.TabIndex = 15;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(401, 179);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(92, 13);
            this.label4.TabIndex = 16;
            this.label4.Text = "Company Moniker";
            // 
            // CompanyMoniker
            // 
            this.CompanyMoniker.Location = new System.Drawing.Point(404, 195);
            this.CompanyMoniker.Name = "CompanyMoniker";
            this.CompanyMoniker.Size = new System.Drawing.Size(279, 20);
            this.CompanyMoniker.TabIndex = 17;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.panel1.Controls.Add(this._okButton);
            this.panel1.Controls.Add(this._cancelButton);
            this.panel1.Location = new System.Drawing.Point(-8, 249);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(713, 100);
            this.panel1.TabIndex = 18;
            // 
            // UserInputProject
            // 
            this.AcceptButton = this._okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(695, 296);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.CompanyMoniker);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.databaseTechnology);
            this.Controls.Add(this.pictureBox2);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.SecuritySelector);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "UserInputProject";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "COFRS REST Service (.NET Framework)";
            this.Load += new System.EventHandler(this.OnLoad);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label5;
		public System.Windows.Forms.ComboBox SecuritySelector;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.PictureBox pictureBox2;
		private System.Windows.Forms.Button _okButton;
		private System.Windows.Forms.Button _cancelButton;
		public System.Windows.Forms.ComboBox databaseTechnology;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Panel panel1;
		public System.Windows.Forms.TextBox CompanyMoniker;
	}
}