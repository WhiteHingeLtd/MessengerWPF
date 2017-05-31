using System.Windows;

namespace MessengerWPF.UserControls
{
    /// <summary>
    /// Interaction logic for OtherMessage.xaml
    /// </summary>
    public partial class OtherMessage
    {
        /// <summary>
        /// Message user control from 
        /// </summary>other users
        public OtherMessage()
        {
            InitializeComponent();
        }
        /// <summary>
        /// 
        /// </summary>
        private void CopyMessageItem_Click(object sender, RoutedEventArgs e)
        {
            for (var i = 0; i < 10; i++)
            {
                try
                {
                    Clipboard.SetText(OtherMessageBox.Text);
                    return;
                }
                catch
                {
                    System.Threading.Thread.Sleep(1);
                }
                System.Threading.Thread.Sleep(10);
            }

        }
    }
}
