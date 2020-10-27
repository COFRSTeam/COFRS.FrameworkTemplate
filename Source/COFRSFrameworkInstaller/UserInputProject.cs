using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace COFRSFrameworkInstaller
{
	public partial class UserInputProject : Form
	{
		public UserInputProject()
		{
			InitializeComponent();
		}

		private void OnLoad(object sender, EventArgs e)
		{
			SecuritySelector.SelectedIndex = 1;
			databaseTechnology.SelectedIndex = 2;
		}

		private void OnOK(object sender, EventArgs e)
		{
			if (string.IsNullOrWhiteSpace(CompanyMoniker.Text))
			{
				MessageBox.Show("Company moniker cannot be blank.\r\nThe company moniker is a short name for your company or organization, similiar to a stock market ticker symbol. Select a 6 to twelve character name that describes your company or organization.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			DialogResult = DialogResult.OK;
			Close();
		}
	}
}
