namespace MessengerWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
                WHLClasses.Reporting.ErrorReporting.ReportException(e.Exception, false);
                e.Handled = true;
        }
    }
}
