using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using MessengerWPF.UserControls;
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
        public DispatcherTimer RefreshLatestThread = new DispatcherTimer();
        public DispatcherTimer ThreadContactLoader = new DispatcherTimer();
        public static Employee authd = new Employee();
        private int CurrentThread = -1;
        private int LatestThreadID = -1;
        public MainWindow()
        {
            authd = null;
            InitializeComponent();
            
        }
        private void Messenger_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                
                var file = new System.IO.StreamReader("Z:\\DomainProfiles\\WHL\\AutoLogin");
                var payrollcode = file.ReadToEnd();
                payrollcode = payrollcode.Replace("qzu", "");
                authd = empcol.FindEmployeeByID(Int32.Parse(payrollcode));
                file.Close();
            }
            catch (Exception)
            {
                var loginwindow = new Login();
                loginwindow.ShowDialog();
            }
            ThreadLoader.DoWork += ThreadLoader_DoWork;
            
            ThreadRefreshTimer.Interval = new TimeSpan(0, 0, 0, 1);
            ThreadRefreshTimer.Tick += RefreshTimer_Tick;
            ThreadRefreshTimer.Start();
            RefreshLatestThread.Interval = new TimeSpan(0, 0, 0, 1);
            RefreshLatestThread.Tick += RefreshLatestThread_Tick;
            RefreshLatestThread.Start();

            ThreadContactLoader.Interval = new TimeSpan(0, 0, 0, 4);
            ThreadContactLoader.Tick += ThreadContactLoader_Tick;
            ThreadContactLoader.Start();

            LoadThreads();
            TypeBox.IsReadOnly = true;
            ThreadLoader.RunWorkerAsync();
            LoadContactInfo();
        }

        private void ThreadContactLoader_Tick(object sender, EventArgs e)
        {
            LoadThreads();
            LoadContactInfo();
        }

        private void RefreshLatestThread_Tick(object sender, EventArgs e)
        {
            if (!ThreadLoader.IsBusy)
                {
                ThreadLoader.RunWorkerAsync();
            }
        }

        private void ThreadLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {           
            ArrayList LastThread = MSSQLPublic.SelectData("SELECT TOP 1 ThreadID from whldata.messenger_threads ORDER BY ThreadID desc") as ArrayList;
            if (LastThread == null) throw new Exception("SQL Query Failed");
            var Meme = LastThread[0] as ArrayList;
            if (Meme == null) throw new Exception("SQL Query Failed");
            LatestThreadID = Convert.ToInt32(Meme[0]);
            }
            catch (Exception Ex)
            {
                Console.WriteLine(Ex.Message);
            }
        }

        private void LoadThreads()
        {
            ThreadsPanel.Children.Clear();
            try
            {
                var ThreadListQuery = MSSQLPublic.SelectData("SELECT a.*,b.messagecontent as Message,b.Timestamp as SendTime,b.participantid as sender FROM 	whldata.messenger_threads a Left Join (SELECT m1.* FROM whldata.messenger_messages m1 LEFT JOIN whldata.messenger_messages m2 ON (m1.threadid = m2.threadid AND m1.messageid < m2.messageid) WHERE m2.messageid IS NULL) b on b.threadid=a.ThreadID WHERE (a.participantid='" + authd.PayrollId.ToString() + "') ORDER BY b.timestamp DESC;") as ArrayList;
                if (ThreadListQuery == null) throw new Exception("SQL Query Failed");
                foreach (ArrayList Result in ThreadListQuery)
                {
                    var CheckList = CheckThreadUsers(Int32.Parse(Result[1].ToString()));
                    if (CheckList.Count > 0)
                    {
                        var refcontrol = new ThreadControl();
                        refcontrol.ThreadID = Int32.Parse(Result[1].ToString());
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
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
           
        }
        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            if (CurrentThread != -1)
            {
                ProcessThreadID(CurrentThread);
            }
        }


        private void HandleThreadClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var ctrl = sender as ThreadControl;
            CurrentThread = ctrl.ThreadID;
            ProcessThreadID(ctrl.ThreadID,true);
            TypeBox.IsReadOnly = false;
        }

        private List<string> CheckThreadUsers(int ThreadID,bool ignoreself = true)
        {
            var ReturnList = new List<string>();
            try
            {
                var Query = MSSQLPublic.SelectData("SELECT participantid FROM whldata.messenger_threads WHERE ThreadID like '" + ThreadID.ToString() + "';") as ArrayList;
                if (Query == null) throw new Exception("SQL Query Failed");
                foreach (ArrayList Result in Query)
                {
                    if (ignoreself)
                    {
                        if (!((Int32.Parse(Result[0].ToString()) == authd.PayrollId)))
                            //Check if we're a member of the thread
                        {
                            ReturnList.Add(empcol.FindEmployeeByID(Int32.Parse(Result[0].ToString())).FullName);
                        }
                    }
                    else
                    {
                        ReturnList.Add(empcol.FindEmployeeByID(Int32.Parse(Result[0].ToString())).FullName);
                    }
                }       
            }
            catch (Exception)
            {
                ReturnList.Clear();
            }
            return ReturnList;
        }
        private void ProcessThreadID(int ThreadID, bool FirstLoad=false)
        {
            MessageStack.Children.Clear();
            GC.Collect();
            var query = MSSQLPublic.SelectData("SELECT TOP 100 * from whldata.messenger_messages WHERE threadid like'" + ThreadID.ToString() + "' ORDER BY timestamp asc") as ArrayList;

            try
            {
                if (query == null) throw new Exception("SQL Query Failed");
                foreach (ArrayList result in query)
                {
                    string message = result[2].ToString();
                    if (message.Contains("http://apps.ad.whitehinge.com/Uploads") || message.Contains(".jpg") || message.Contains(".gif") || message.Contains(".jpeg") || message.Contains(".png") || message.Contains(".JPG") || message.Contains(".PNG"))
                    {
                        if (result[1].ToString() == authd.PayrollId.ToString())
                        {
                            var Msg = new UserPictureControl();
                            Msg.ImageContainer.Source = new BitmapImage(new Uri(message));
                            Msg.InitializeComponent();
                            Msg.MouseUp += Msg_MouseUp;
                            Msg.TouchUp += Msg_TouchUp;
                            MessageStack.Children.Add(Msg);
                        }
                        else
                        {
                            var OtherMsg = new OtherPictureControl();
                            OtherMsg.ImageContainer.Source = new BitmapImage(new Uri(message));
                            OtherMsg.SenderName.Text = empcol.FindEmployeeByID(Int32.Parse(result[1].ToString())).FullName;
                            OtherMsg.MouseUp += OtherMsg_MouseUp;
                            OtherMsg.TouchUp += OtherMsg_TouchUp;
                            OtherMsg.InitializeComponent();
                            MessageStack.Children.Add(OtherMsg);
                        }
                    }
                    if (result[1].ToString() == authd.PayrollId.ToString())
                    {
                        var Msg = new SelfMessage();
                        Msg.FromMessageBox.Text = result[2].ToString();
                        Msg.InitializeComponent();
                        MessageStack.Children.Add(Msg);
                    }
                    else
                    {
                        var Msg = new OtherMessage();
                        Msg.OtherMessageBox.Text = result[2].ToString();
                        Msg.SenderName.Text = empcol.FindEmployeeByID(Int32.Parse(result[1].ToString())).FullName;
                        Msg.InitializeComponent();
                        MessageStack.Children.Add(Msg);
                    }
                }
            }
            catch (Exception e)
            {
                var msgbox = new WPFMsgBox();
                msgbox.Body.Text = e.Message;
                msgbox.DialogTitle.Text = "Failed to load";
                msgbox.ShowDialog();
            }
            finally
            {
              if(FirstLoad) MessageScrollviewer.ScrollToEnd(); //Only scrolls to end on first load
               var who = CheckThreadUsers(ThreadID, false);
                if (who == null) ThreadUserTextBlock.Text = "";
                ThreadUserTextBlock.Text = "";
                foreach (string result in who)
                {
                    ThreadUserTextBlock.Text += result + ", ";
                }
            }

        }

        private void OtherMsg_TouchUp(object sender, System.Windows.Input.TouchEventArgs e)
        {
            var control = sender as UserPictureControl;
            var ShowPicture = new ActualPicture();
            ShowPicture.NewImage.Source = control.ImageContainer.Source;
            ShowPicture.InitializeComponent();
            ShowPicture.Show();
        }

        private void OtherMsg_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var control = sender as UserPictureControl;
            var ShowPicture = new ActualPicture();
            ShowPicture.NewImage.Source = control.ImageContainer.Source;
            ShowPicture.InitializeComponent();
            ShowPicture.Show();
        }

        private void Msg_TouchUp(object sender, System.Windows.Input.TouchEventArgs e)
        {
            var control = sender as UserPictureControl;
            var ShowPicture = new ActualPicture();
            ShowPicture.NewImage.Source = control.ImageContainer.Source;
            ShowPicture.InitializeComponent();
            ShowPicture.Show();
        }

        private void Msg_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var control = sender as UserPictureControl;
            var ShowPicture = new ActualPicture();
            ShowPicture.NewImage.Source = control.ImageContainer.Source;
            ShowPicture.InitializeComponent();
            ShowPicture.Show();
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
            if(ContactsPanel.IsVisible)
            {
                LoadContactInfo();
            }
        }
        private void LoadContactInfo() 
        {
            ContactsPanel.Children.Clear();
            foreach (Employee emp in empcol.Employees)
            {
                if (emp == authd) continue;
                if (emp.Visible)
                {                  
                    var ctrl = new ContactControl();
                    ctrl.ThreadUsers.Content = emp.FullName;
                    ctrl.ThreadUsers.Click += ThreadUsers_Click;
                    ctrl.AddToThreadButton.Click += AddToThreadButton_Click;
                    ctrl.EmployeeID = emp.PayrollId;
                    ctrl.InitializeComponent();
                    ContactsPanel.Children.Add(ctrl);
                }
            }
        }

        private void ThreadUsers_Click(object sender, RoutedEventArgs e)
        {
            var control = sender as Button;
            var parent = FindParent<ContactControl>(control);
            CreateNewThread(parent.EmployeeID);
        }

        private void AddToThreadButton_Click(object sender, RoutedEventArgs e)
        {
            var control = sender as Button;
            var parent = FindParent<ContactControl>(control);
            AddToThread(CurrentThread, parent.EmployeeID);
        }

        private void AddToThread(int ThreadID,int EmployeeID)
        {
            if (!(CheckForUserInThread(ThreadID,EmployeeID)))
            {
               MSSQLPublic.insertUpdate("INSERT INTO whldata.messenger_threads(ThreadID, participantid,IsTwoWay) VALUES('" + ThreadID.ToString() + "', '" + EmployeeID.ToString() + "',0);");
               MSSQLPublic.insertUpdate("UPDATE whldata.messenger_threads SET IsTwoWay=1 WHERE threadid='"+ThreadID.ToString()+"'");
               LoadContactInfo();
            }
        }
        private bool CheckForUserInThread(int ThreadID, int EmployeeID)
        {
            var Results = MSSQLPublic.SelectData("SELECT * from whldata.messenger_threads WHERE participantid='" + EmployeeID.ToString() + "' AND threadid='" + ThreadID.ToString() + "'") as ArrayList;
            if (Results.Count > 0) return true;
            else return false;
        }

        private void CreateNewThread(int EmployeeID)
        {
            var CheckForTwoWay = MSSQLPublic.SelectData("SELECT * from whldata.messenger_threads WHERE (participantid='"+authd.PayrollId.ToString()+"' OR participantid='"+EmployeeID.ToString()+"') AND IsTwoWay=1") as ArrayList;
            if (CheckForTwoWay == null) throw new Exception("SQL Query Failed");
            if (CheckForTwoWay.Count == 0)
            {
                int NewThread = LatestThreadID + 1;
                MSSQLPublic.insertUpdate("INSERT INTO whldata.messenger_threads (ThreadID, participantid,IsTwoWay) VALUES (" + NewThread.ToString() + "," + authd.PayrollId.ToString() + ",1)");
                MSSQLPublic.insertUpdate("INSERT INTO whldata.messenger_threads (ThreadID, participantid,IsTwoWay) VALUES (" + NewThread.ToString() + "," + EmployeeID.ToString() + ",1)");
                CurrentThread = NewThread;
            }
        }
        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            T parent = parentObject as T;
            if (parent != null) return parent;
            else return FindParent<T>(parentObject); //Intentional Recursive method
        }
    }
}


