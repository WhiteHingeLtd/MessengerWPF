using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using MessengerWPF.UserControls;
using System;
using System.Linq;
using WHLClasses;
using System.Windows.Threading;
namespace MessengerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public EmployeeCollection empcol = new EmployeeCollection();
        public BackgroundWorker ThreadLoader = new BackgroundWorker();
        public DispatcherTimer ThreadRefreshTimer = new DispatcherTimer();
        public static Employee authd = new Employee();
        private int CurrentThread = -1;
        private int SelectedTab = 0;
        public MainWindow()
        {
            authd = null;
            InitializeComponent();
            
        }
        private void InitializeWorkers()
        {

        }
        private void Messenger_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string payrollcode;
                System.IO.StreamReader file = new System.IO.StreamReader("Z:\\DomainProfiles\\WHL\\AutoLogin");
                payrollcode = file.ReadToEnd();
                payrollcode = payrollcode.Replace("qzu", "");
                authd = empcol.FindEmployeeByID(Int32.Parse(payrollcode));
                file.Close();
            }
            catch (Exception)
            {
                var loginwindow = new Login();
                loginwindow.ShowDialog();
            }

            
            ThreadRefreshTimer.Interval = new TimeSpan(0, 0, 0, 1);
            ThreadRefreshTimer.Tick += RefreshTimer_Tick;
            ThreadRefreshTimer.Start();
            // var ThreadListquery = MSSQLPublic.SelectData("SELECT DISTINCT * from whldata.messenger_threads WHERE participantid like '"+authd.PayrollId.ToString()+"'") as ArrayList;
            LoadThreads();
            TypeBox.IsReadOnly = true;
        }
        private void LoadThreads()
        {
            ThreadsPanel.Children.Clear();
            var ThreadListQuery = MSSQLPublic.SelectData("SELECT a.*,b.messagecontent as Message,b.Timestamp as SendTime,b.participantid as sender FROM 	whldata.messenger_threads a Left Join (SELECT m1.* FROM whldata.messenger_messages m1 LEFT JOIN whldata.messenger_messages m2 ON (m1.threadid = m2.threadid AND m1.messageid < m2.messageid) WHERE m2.messageid IS NULL) b on b.threadid=a.ThreadID WHERE (a.participantid='" + authd.PayrollId.ToString() + "') ORDER BY b.timestamp DESC;") as ArrayList;
            foreach (ArrayList result in ThreadListQuery)
            {
                var CheckList = CheckThreadUsers(Int32.Parse(result[1].ToString())) as List<string>;
                if (CheckList.Count > 0)
                {
                    var refcontrol = new ThreadControl();
                    refcontrol.ThreadID = Int32.Parse(result[1].ToString());
                    refcontrol.ThreadUsers.Text = "";
                    foreach (String User in CheckList)
                    {
                        refcontrol.ThreadUsers.Text += User + " ";
                    }
                    refcontrol.MouseUp += HandleThreadClick;
                    refcontrol.InitializeComponent();
                    ThreadsPanel.Children.Add(refcontrol);
                }
            }
        }
        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            if (CurrentThread != -1)
            {
                ProcessThreadID(CurrentThread);
            }
            if (SelectedTab == 0)
            {
                LoadThreads();
            }
            else if (SelectedTab == 1)
            {
                LoadContactInfo();
            }
        }


        private void HandleThreadClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var ctrl = sender as ThreadControl;
            CurrentThread = ctrl.ThreadID;
            ProcessThreadID(ctrl.ThreadID);
            TypeBox.IsReadOnly = false;
        }

        private List<string> CheckThreadUsers(int ThreadID)
        {
            var ReturnList = new List<string>();
            try
            {
                var Query = MSSQLPublic.SelectData("SELECT participantid FROM whldata.messenger_threads WHERE ThreadID like '" + ThreadID.ToString() + "';") as ArrayList;
                foreach (ArrayList Result in Query)
                {
                    if (!((Int32.Parse(Result[0].ToString()) == authd.PayrollId))) //Check if we're a member of the thread
                    {
                        ReturnList.Add(empcol.FindEmployeeByID(Int32.Parse(Result[0].ToString())).FullName);
                    }
                }
            }
            catch (Exception)
            {
            }
            return ReturnList;
        }
        private void ProcessThreadID(int ThreadID)
        {
            MessageStack.Children.Clear();
            GC.Collect();
            var query = MSSQLPublic.SelectData("SELECT TOP 100 * from whldata.messenger_messages WHERE threadid like'" + ThreadID.ToString() + "' ORDER BY timestamp asc") as ArrayList;
            try
            {
                foreach (ArrayList result in query)
                {
                    if (result[1].ToString() == authd.PayrollId.ToString())
                    {
                        var msg = new SelfMessage();
                        msg.FromMessageBox.Text = result[2].ToString();
                        msg.InitializeComponent();
                        MessageStack.Children.Add(msg);
                    }
                    else
                    {
                        var msg = new OtherMessage();
                        msg.OtherMessageBox.Text = result[2].ToString();
                        msg.SenderName.Text = empcol.FindEmployeeByID(Int32.Parse(result[1].ToString())).FullName;
                        msg.InitializeComponent();
                        MessageStack.Children.Add(msg);
                    }
                }
            }
            catch (Exception e)
            {
                var msgbox = new WPFMsgBox();
                msgbox.Body.Text = e.Message.ToString();
                msgbox.DialogTitle.Text = "Failed to load";
                msgbox.ShowDialog();
            }
            finally
            {
                MessageScrollviewer.ScrollToEnd();
            }

        }

        private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Return)
            {
                if (CurrentThread != -1)
                {
                    SendMessage(CurrentThread, TypeBox.Text);
                    TypeBox.Text = "";
                }
            }
        }
        private void SendMessage(int ThreadID, string Message)
        {
            string SafeMsg = Message.Replace(";", "");
            SafeMsg = SafeMsg.Replace("--", "");
            SafeMsg = SafeMsg.Replace("'", "''");
            if (SafeMsg != "")
            {
                var query = MSSQLPublic.insertUpdate("INSERT INTO whldata.messenger_messages (participantid,messagecontent,timestamp,threadid) VALUES (" + authd.PayrollId.ToString() + ",'" + SafeMsg + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + ThreadID.ToString() + "')");
            }
            RefreshTimer_Tick(null, null);
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentThread != -1)
                {
                SendMessage(CurrentThread, TypeBox.Text);
                TypeBox.Text = "";
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            e.Handled = true;
            if (ThreadsPanel.IsVisible == true)
            {
                SelectedTab = 0;
            }
            else if(ContactsPanel.IsVisible == true)
            {
                SelectedTab = 1;
                LoadContactInfo();
            }
        }
        private void LoadContactInfo()
        {
            foreach (Employee emp in empcol.Employees)
            {
                if (emp.Visible == true)
                {
                    var ctrl = new ThreadControl();
                    ctrl.ThreadUsers.Text = emp.FullName;
                    ctrl.InitializeComponent();
                    ContactsPanel.Children.Add(ctrl);
                }
            }
        }
    }
}


