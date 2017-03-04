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
    public partial class Login : Window
    {
        public static Employee CurrentEmployee = new Employee();
        public EmployeeCollection empcol = new EmployeeCollection();
        private bool requirespin = false;
        public Login()
        {
            InitializeComponent();
            LoginScanBox.Focus();
        }
        private void ProcessData(string Data)
        {
            if (Data.StartsWith("qzu"))
            {
                MainWindow.authd = empcol.FindEmployeeByID(Int32.Parse(Data.Replace("qzu", "")));
                this.Close();
            }
            else if (Data.Length > 0 & Data.Length < 3)
            {
                LoginTitle.Text = empcol.FindEmployeeByID(Convert.ToInt32(Data)).FullName + " Please enter your Pin";
                CurrentEmployee = empcol.FindEmployeeByID(Convert.ToInt32(Data));
                requirespin = true;
            }
            else if (requirespin)
            {
                if (CurrentEmployee.CheckPin(Data))
                {
                    MainWindow.authd = CurrentEmployee;
                     requirespin = false;
                    this.Close();
                }
                else
                {
                    var msg = new WPFMsgBox();
                    msg.Body.Text = "That is the wrong pin, please try again";
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
            var button = sender as Button;
            LoginScanBox.Text += button.Content;
            LoginScanBox.Focus();
        }
    }
}
