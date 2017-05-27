using System;
using System.Windows.Media.Imaging;
using WHLClasses;

namespace MessengerWPF.UserControls
{
    /// <summary>
    /// Interaction logic for ContactControl.xaml
    /// </summary>
    public partial class ContactControl
    {
        /// <summary>
        /// 
        /// </summary>
        public int EmployeeID = -1;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ForCurrent"></param>
        public ContactControl(bool ForCurrent = false)
        {
            InitializeComponent();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="emp"></param>
        /// <param name="ForCurrent"></param>
        public ContactControl(Employee emp,bool ForCurrent = true)
        {
            InitializeComponent();
            ThreadUsers.Content = emp.FullName;
            EmployeeID = emp.PayrollId;
            if (ForCurrent)
            {
                AddImage.Source = new BitmapImage(new Uri("/Icons/removeicon.png", UriKind.Relative));
            }
        }
    }
}