using System.Windows;

namespace MessengerWPF
{
    /// <summary>
    /// Interaction logic for WPFMsgBox.xaml
    /// </summary>
    public partial class WPFMsgBox
    {
        public WPFMsgBox()
        {
            
            InitializeComponent();
            WHLClasses.GraphicsExtentions.EnableBlur(this);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
