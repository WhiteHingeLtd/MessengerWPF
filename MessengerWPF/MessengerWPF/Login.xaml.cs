using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WHLClasses;
namespace MessengerWPF
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login
    {
        /// <summary>
        /// 
        /// </summary>
        public static Employee CurrentEmployee = new Employee();
        private EmployeeCollection _empcol = new EmployeeCollection();
        private bool _requirespin;

        /// <summary>
        /// 
        /// </summary>
        public Login()
        {
            InitializeComponent();
            LoginScanBox.Focus();
        }

        /// <summary>
        /// Processes the login screen data
        /// </summary>
        /// <param name="data">The string from the textbox</param>
        private void ProcessData(string data)
        {
            if (data.StartsWith("qzu"))
            {
                MainWindow.AuthdEmployee = _empcol.FindEmployeeByID(int.Parse(data.Replace("qzu", "")));
                Close();
            }
            else if (data.Length > 0 & data.Length < 3)
            {
                LoginTitle.Text = _empcol.FindEmployeeByID(Convert.ToInt32(data)).FullName + " Please enter your Pin";
                CurrentEmployee = _empcol.FindEmployeeByID(Convert.ToInt32(data));
                _requirespin = true;
            }
            else if (_requirespin)
            {
                if (CurrentEmployee.CheckPin(data))
                {
                    MainWindow.AuthdEmployee = CurrentEmployee;
                     _requirespin = false;
                    Close();
                }
                else
                {
                    var msg = new WPFMsgBox("Error", "That is the wrong pin, please try again");                      
                    msg.ShowDialog();
                }
            }
        }

        private void LoginScanBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                e.Handled = true;
                ProcessData(LoginScanBox.Text);

                LoginScanBox.Text = "";
                LoginScanBox.Focus();
            }
        }

        private void KeypadEnter_Click(object sender, RoutedEventArgs e)
        {
            ProcessData(LoginScanBox.Text);
            LoginScanBox.Text = "";
            LoginScanBox.Focus();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            LoginScanBox.Text = "";
            LoginScanBox.Focus();
        }

        private void Keypad1_Click(object sender, RoutedEventArgs e)
        {
            var Ctrl = sender as Button;
            if (Ctrl != null)
            {
            LoginScanBox.Text += Ctrl.Content;
            LoginScanBox.Focus();
            }
        }
    }
}
