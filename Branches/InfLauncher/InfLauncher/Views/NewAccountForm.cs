using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using InfLauncher.Controllers;
using InfLauncher.Models;

namespace InfLauncher.Views
{
    public partial class NewAccountForm : Form
    {
        private MainController _controller;

        public NewAccountForm()
        {
            InitializeComponent();
        }

        public NewAccountForm(MainController controller)
        {
            InitializeComponent();

            _controller = controller;
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            var username = txtboxUsername.Text;
            var password = txtboxPassword.Text;
            var email = txtboxEmail.Text;

            if(!Account.IsValidUsername(username))
            {
                MessageBox.Show(@"Username must be longer than 4 characters.");
                return;
            }

            if(!Account.IsValidPassword(password))
            {
                MessageBox.Show(@"Password cannot be left blank.");
                return;
            }

            if(!Account.IsValidEmail(email))
            {
                MessageBox.Show(@"Invalid email format.");
                return;
            }

            _controller.RegisterAccount(new Account.AccountRegistrationRequestModel(username, password, email));
        }
    }
}
