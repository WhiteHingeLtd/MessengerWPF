using System.Windows;

namespace MessengerWPF
{
    /// <summary>
    /// Interaction logic for WPFMsgBox.xaml
    /// </summary>
    public partial class WPFMsgBox
    {
        /// <summary>
        /// Displays a WPF Styled MessageBox. Useful for TouchDisplays and Low Screen Size Devices
        /// </summary>
        /// <param name="title">Title of the MessageBox</param>
        /// <param name="body">Body of the MessageBox</param>
        public WPFMsgBox(string title = "Error",string body = "")
        {
            
            InitializeComponent();
            this.Body.Text = body;
            this.DialogTitle.Text = title;
            WHLClasses.GraphicsExtentions.EnableBlur(this);
        }
        /// <summary>
        /// Closes the window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
