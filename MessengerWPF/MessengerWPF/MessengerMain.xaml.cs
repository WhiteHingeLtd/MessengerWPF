using MessengerWPF.UserControls;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MessengerWPF.MessageStorage;
using WHLClasses;
using WHLClasses.Notifications;
using WHLClasses.SQL.SQLException;


namespace MessengerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow 
    {
        #region Variables
        private EmployeeCollection _empcol = new EmployeeCollection();
        private BackgroundWorker _threadLoader = new BackgroundWorker();
        private BackgroundWorker _threadFinder = new BackgroundWorker();
        private BackgroundWorker _sqLiteWriter = new BackgroundWorker();
        private DispatcherTimer _threadRefreshTimer = new DispatcherTimer();
        private DispatcherTimer _refreshLatestThread = new DispatcherTimer();
        private DispatcherTimer _threadContactLoader = new DispatcherTimer();
        private DispatcherTimer _currentStatusChecker = new DispatcherTimer();
        
        private Dictionary<string, string> _currentThreads = new Dictionary<string, string>();
        /// <summary>
        /// This Dictionary uses the threadId as the key and provides a list of users
        /// </summary>
        public Dictionary<int, List<string>> ThreadsWithUsers = new Dictionary<int, List<string>>();
        /// <summary>
        /// For this Dictionary the Key is the Thread and the Value is the latest message id
        /// </summary>
        private Dictionary<int, int> _userLastThreadNoti = new Dictionary<int, int>(); 
        /// <summary>
        /// 
        /// </summary>
        public static Employee AuthdEmployee = new Employee();
        private bool _isOffline;
        private int _currentThread = -1;
        private int _latestThreadId = -1;
        private int _lastMessageInThread = -1;
        private bool _pauseMessageRefreshing = false;
        
        private bool _updateSqliteDb;
        #endregion
        #region Program Load Functions.
        /// <summary>
        /// MainWindow Initializiation
        /// </summary>
        public MainWindow()
        {
            AuthdEmployee = null;
            InitializeComponent();
        }
        /// <summary>
        /// Interaction for the program's initial load
        /// </summary>
        private void Messenger_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {                
                var file = new StreamReader(@"Z:\DomainProfiles\WHL\AutoLogin");
                var payrollcode = file.ReadToEnd();
                payrollcode = payrollcode.Replace("qzu", "");
                AuthdEmployee = _empcol.FindEmployeeByID(int.Parse(payrollcode));
                file.Close();
            }
            catch (Exception)
            {
                var loginwindow = new Login();
                loginwindow.ShowDialog();
            }      
            while (AuthdEmployee == null)
            {
                var loginwindow = new Login();
                loginwindow.ShowDialog();
            }
            _isOffline = SQLServer.TestConn();

            _threadLoader.DoWork += ThreadLoader_DoWork;
            _threadLoader.WorkerSupportsCancellation = true;
            _threadLoader.RunWorkerAsync();
            
            _threadRefreshTimer.Interval = new TimeSpan(0, 0, 0, 1);
            _threadRefreshTimer.Tick += RefreshTimer_Tick;
            _threadRefreshTimer.Start();
            _refreshLatestThread.Interval = new TimeSpan(0, 0, 0, 10);
            _refreshLatestThread.Tick += RefreshLatestThread_Tick;
            _refreshLatestThread.Start();

            _threadContactLoader.Interval = new TimeSpan(0, 0, 0, 30);
            _threadContactLoader.Tick += ThreadLoaderTimerTick;
            _threadContactLoader.Start();

            _threadFinder.DoWork += ThreadFinder_DoWork;
            _threadFinder.RunWorkerCompleted += ThreadLoader_RunWorkerCompleted;
            _threadFinder.WorkerSupportsCancellation = true;

            _sqLiteWriter.DoWork += _SqLiteWriter_DoWork;
            _sqLiteWriter.RunWorkerCompleted += _SqLiteWriter_RunWorkerCompleted;

            _currentStatusChecker.Tick += _currentStatusChecker_Tick;
            _currentStatusChecker.Interval = new TimeSpan(1000);
            _currentStatusChecker.Start();

            LoadThreads(); //Load the thread data
            TypeBox.IsReadOnly = true;
            LoadContactInfo();
            SqLite.PrepareDb();
            _updateSqliteDb = true;
        }

        private void _currentStatusChecker_Tick(object sender, EventArgs e)
        {
            var wasOffline = false;
            try
            {
                while (!SQLServer.TestConn())
                {
                    wasOffline = true;
                    _threadContactLoader.Stop();
                    _threadLoader.CancelAsync();
                    _threadRefreshTimer.Stop();
                    _refreshLatestThread.Stop();
                    _threadFinder.CancelAsync();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);

            }
            finally
            {
                if (wasOffline)
                {
                    _threadContactLoader.Start();
                    _threadLoader.RunWorkerAsync();
                    _threadRefreshTimer.Start();
                    _refreshLatestThread.Start();
                    _threadFinder.RunWorkerAsync();
                }

            }

        }

        private void _SqLiteWriter_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            
        }
        /// <summary>
        /// SqLite Test
        /// </summary>
        private void _SqLiteWriter_DoWork(object sender, DoWorkEventArgs e)
        {
            var messageQuery = "SELECT * from whldata.messenger_messages WHERE threadID = 0 ";
            var threadQuery = "SELECT * from whldata.messenger_threads where participantid = " +
                              AuthdEmployee.PayrollId.ToString() + ";";
            foreach (var pair in _currentThreads)
            {
                messageQuery += " OR threadid = " + pair.Key;
            }
            var results = SQLServer.MSSelectDataDictionary(messageQuery);
            var threadresults = SQLServer.MSSelectDataDictionary(threadQuery);
            foreach (var result in results)
            {
                var query =
                    "REPLACE INTO messenger_messages (messageid,participantid,messagecontent,timestamp,threadid) VALUES ('" +
                    result["messageid"].ToString() + "','" + result["participantid"].ToString() + "','" +
                    result["messagecontent"] + "','" + result["timestamp"].ToString() + "','" + result["threadid"].ToString() + "');";
                Console.WriteLine(SqLite.SqliteOtherQuery(query));
            }
            foreach (var result in threadresults)
            {
                var query =
                    "REPLACE INTO messenger_messages (idmessenger_threads,threadid,participantid, notified,istwoway) VALUES ('" +
                    result["idmessenger_threads"].ToString() + "','" + result["threadid"].ToString() + "','" +
                    result["participantid"].ToString() + "','" + result["notified"].ToString() + "','" + result["istwoway"].ToString() +
                    "');";
                Console.WriteLine(SqLite.SqliteOtherQuery(query));
            }
        }



        #endregion
        #region Timers
        private void ThreadLoaderTimerTick(object sender, EventArgs e)
        {
            LoadThreads();
        }

        private void RefreshLatestThread_Tick(object sender, EventArgs e)
        {
            if (!_threadLoader.IsBusy)
                {
                _threadLoader.RunWorkerAsync();
            }
            if (!_threadFinder.IsBusy)
            {
                _threadFinder.RunWorkerAsync();
            }
        }

        [DebuggerStepThrough]
        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            if (_currentThread != -1 && !(_pauseMessageRefreshing))
            {
                ProcessThreadId(_currentThread);
            }
        }
        #endregion
        #region Load Menu Data
        /// <summary>
        /// Updates the _latestThreadID for creating new threads.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ThreadLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {           
                var lastThread = SQLServer.SelectData("SELECT TOP 1 threadId from whldata.messenger_threads ORDER BY threadId desc") as ArrayList;
                if (lastThread == null) throw new Exception("SQL Query Failed");
                var latestThreadRow = lastThread[0] as ArrayList;
                if (latestThreadRow == null) throw new Exception("SQL Query Failed");
                _latestThreadId = Convert.ToInt32(latestThreadRow[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        /// <summary>
        /// Updates the ThreadList
        /// </summary>
        private void LoadThreads()
        {
            ThreadsPanel.Children.Clear();
            try
            {
                var threadListQuery = SQLServer.MSSelectDataDictionary("SELECT a.*,b.messagecontent as Message,b.Timestamp as SendTime,b.participantid as sender FROM 	whldata.messenger_threads a Left Join (SELECT m1.* FROM whldata.messenger_messages m1 LEFT JOIN whldata.messenger_messages m2 ON (m1.threadid = m2.threadid AND m1.messageid < m2.messageid) WHERE m2.messageid IS NULL) b on b.threadid=a.threadId WHERE (a.participantid='" + AuthdEmployee.PayrollId.ToString() + "') ORDER BY b.timestamp DESC;");
                if (threadListQuery == null) throw new Exception("SQL Query Failed");
                foreach (var result in threadListQuery)
                {
                    var checkList = CheckThreadUsers(int.Parse(result["threadid"].ToString()));
                    if (checkList.Count > 0)
                    {
                        var refcontrol = new ThreadControl
                        {
                            ThreadId = int.Parse(result["threadid"].ToString())
                            
                        };
                        refcontrol.ThreadUsers.Text = "";
                        foreach (var user in checkList)
                        {
                            refcontrol.ThreadUsers.Text += user.FullName + ",";
                        }
                        refcontrol.ThreadUsers.Text = refcontrol.ThreadUsers.Text.Trim().TrimEnd(',');
                        refcontrol.LastMessage.Text = result["message"].ToString();
                        refcontrol.MouseUp += HandleThreadClick;
                        refcontrol.InitializeComponent();
                        ThreadsPanel.Children.Add(refcontrol);
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
           
        }
        /// <summary>
        /// Updates the contacts list
        /// </summary>
        private void LoadContactInfo()
        {
            ContactsPanel.Children.Clear();
            foreach (var emp in _empcol.Employees)
            {
                if (emp == AuthdEmployee) continue;
                if (!emp.Visible)
                {

                }
                else
                {
                    var ctrl = new ContactControl
                    {
                        EmployeeID = emp.PayrollId,                       
                    };
                    ctrl.ThreadUsers.Content = emp.FullName;
                    ctrl.ThreadUsers.Click += ThreadUsers_Click;
                    ctrl.AddToThreadButton.Click += AddToThreadButton_Click;
                    ctrl.InitializeComponent();
                    ContactsPanel.Children.Add(ctrl);
                }
            }
        }
        #endregion
        #region "Process Threads"
        /// <summary>
        /// Loads messages for a specified thread
        /// </summary>
        /// <param name="threadId"></param>
        /// <param name="firstLoad"></param>
        /// <param name="amountToLoad"></param>
        private void ProcessThreadId(int threadId, bool firstLoad=false,int amountToLoad=100)
        {
           
            GC.Collect();
            var who = CheckThreadUsers(threadId, false);
            var queryResults = null as ArrayList;
            try
            { 
                if(firstLoad)
                {
                    var query = SQLServer.SelectData("SELECT TOP "+amountToLoad.ToString()+" * from whldata.messenger_messages WHERE threadid like'" + threadId.ToString() + "' ORDER BY messageid desc") as ArrayList;
                    if (query == null) throw new NullReferenceException();
                    query.Reverse();
                    queryResults = query;
                
                    _lastMessageInThread = -1;
                    MessageStack.Children.Clear();
                }
                else
                {
                    var query = SQLServer.SelectData("SELECT TOP " + amountToLoad.ToString() + " * from whldata.messenger_messages WHERE threadid like'" + threadId.ToString() + "' AND messageid > '"+ _lastMessageInThread.ToString() + "' ORDER BY messageid desc") as ArrayList;
                    if (query == null) throw new NullReferenceException();
                    query.Reverse();
                    queryResults = query;
                }
            }
            catch (NullReferenceException)
            { }
            try
            {
                if (!who.Contains(AuthdEmployee))
                {
                    firstLoad = false;
                    _currentThread = -1;
                    MessageStack.Children.Clear();
                    _lastMessageInThread = -1;
                    who.Clear();
                }
                else if (queryResults == null) throw new Exception("SQL Query Failed");
                else if (queryResults.Count == 0) return;
                else
                {
                    foreach (ArrayList result in queryResults)
                    {
                        var message = result[2].ToString().ToLower();
                        if (message.Contains(".jpg") || message.Contains(".jpeg") || message.Contains(".png"))
                        {
                            if (result[1].ToString() == AuthdEmployee.PayrollId.ToString())
                            {
                                var msg = new UserPictureControl();
                                msg.ImageContainer.Source = new BitmapImage(new Uri(message));
                                msg.InitializeComponent();
                                msg.MouseUp += Msg_MouseUp;
                                msg.TouchUp += Msg_TouchUp;
                                MessageStack.Children.Add(msg);
                            }
                            else
                            {
                                var otherMsg = new OtherPictureControl();
                                otherMsg.ImageContainer.Source = new BitmapImage(new Uri(message));
                                otherMsg.SenderName.Text = _empcol.FindEmployeeByID(int.Parse(result[1].ToString())).FullName;
                                otherMsg.MouseUp += OtherMsg_MouseUp;
                                otherMsg.TouchUp += OtherMsg_TouchUp;
                                otherMsg.InitializeComponent();
                                MessageStack.Children.Add(otherMsg);
                            }
                        }
                        else if (message.Contains("https://") || message.Contains("http://"))
                        {
                            if (result[1].ToString() == AuthdEmployee.PayrollId.ToString())
                            {
                                var msg = new SelfMessage();
                                msg.FromMessageBox.Text = "";
                                msg.FromMessageBox.Inlines.Clear();
                                var splitStrings = Regex.Split(result[2].ToString(), " ");
                            
                                foreach (var splits in splitStrings)
                                {
                                    if (splits.Contains("http://") || splits.Contains("https://"))
                                    {
                                        var splitHyperlinks = Regex.Split(splits, " ");
                                        foreach (var hyperLinks in splitHyperlinks)
                                        {
                                            if (hyperLinks.Contains("http://") || hyperLinks.Contains("https://"))
                                            {
                                                var newHyperLink = new Hyperlink
                                                {
                                                    NavigateUri = new Uri(hyperLinks),
                                                };
                                                newHyperLink.Inlines.Add(hyperLinks);
                                                newHyperLink.RequestNavigate += NewHyperLink_RequestNavigate;
                                                msg.FromMessageBox.Inlines.Add(newHyperLink);
                                                msg.FromMessageBox.Inlines.Add(" ");
                                            }
                                            else
                                            {
                                                msg.FromMessageBox.Inlines.Add(splits + " ");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        msg.FromMessageBox.Inlines.Add(splits + " ");
                                    }
                                }
                                msg.InitializeComponent();
                                MessageStack.Children.Add(msg);
                            }
                            else if (result[1].ToString() == "0")
                            {
                                var msg = new SystemNotiControl();
                                msg.SystemNoti.Text = result[2].ToString();
                                msg.InitializeComponent();
                                MessageStack.Children.Add(msg);
                            }
                            else if (result[1].ToString() != AuthdEmployee.PayrollId.ToString())
                            {
                                var msg = new OtherMessage();
                                msg.OtherMessageBox.Text = "";
                                msg.OtherMessageBox.Inlines.Clear();
                                var splitStrings = Regex.Split(result[2].ToString(), " ");
                                var lastString = splitStrings.Last();
                                foreach (var splits in splitStrings)
                                {
                                    bool isLast;
                                    if (splits == lastString) isLast = true;
                                    else isLast = false;
                                    if (splits.Contains("http://") || splits.Contains("https://"))
                                    {
                                        var splitHyperlinks = Regex.Split(splits, " ");
                                        foreach (var hyperLinks in splitHyperlinks)
                                        {
                                            if (hyperLinks.Contains("http://") || hyperLinks.Contains("https://"))
                                            {
                                                var newHyperLink = new Hyperlink();
                                                newHyperLink.NavigateUri = new Uri(hyperLinks);
                                                newHyperLink.Inlines.Add(hyperLinks);
                                                newHyperLink.RequestNavigate += NewHyperLink_RequestNavigate;
                                                if (isLast) msg.OtherMessageBox.Inlines.Add(newHyperLink);
                                                else
                                                {
                                                    msg.OtherMessageBox.Inlines.Add(newHyperLink + " ");
                                                }
                                            
                                            }
                                            else
                                            {
                                                if (isLast) msg.OtherMessageBox.Inlines.Add(splits);
                                                else
                                                {
                                                    msg.OtherMessageBox.Inlines.Add(splits + " ");
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (isLast) msg.OtherMessageBox.Inlines.Add(splits);
                                        else
                                        {
                                            msg.OtherMessageBox.Inlines.Add(splits + " ");
                                        }
                                    }
                                }
                                msg.SenderName.Text = _empcol.FindEmployeeByID(int.Parse(result[1].ToString())).FullName;
                                msg.InitializeComponent();
                                MessageStack.Children.Add(msg);
                            }
                        }
                        else if (result[1].ToString() == AuthdEmployee.PayrollId.ToString())
                        {
                            var msg = new SelfMessage();
                            msg.FromMessageBox.Text = result[2].ToString();
                            msg.InitializeComponent();
                            MessageStack.Children.Add(msg);
                        }
                        else if (result[1].ToString() == "0")
                        {
                            var msg = new SystemNotiControl();
                            msg.SystemNoti.Text = result[2].ToString();
                            msg.InitializeComponent();
                            MessageStack.Children.Add(msg);
                        }
                        else if (result[1].ToString() != AuthdEmployee.PayrollId.ToString())
                        {
                            var msg = new OtherMessage();
                            msg.OtherMessageBox.Text = result[2].ToString();
                            msg.SenderName.Text = _empcol.FindEmployeeByID(Int32.Parse(result[1].ToString())).FullName;
                            msg.InitializeComponent();
                            MessageStack.Children.Add(msg);

                            if (!firstLoad)
                            {
                                TextOnlyNotification[] notiText =
                                {
                                    new TextOnlyNotification(result[2].ToString(), HandleNoti)
                                };
                                Notification.CreateNotification("Messenger", notiText,20);
                            }
                        }

                    }
                }
            }
            catch (Exception e)
            {
                if (e.Message != "Skip Foreach")
                {
                    var msgbox = new WPFMsgBox();
                    msgbox.Body.Text = e.Message;
                    msgbox.DialogTitle.Text = "Failed to load";
                    msgbox.ShowDialog();
                }
                
            }
            finally
            {
                if (firstLoad) MessageScrollviewer.ScrollToEnd(); //Only scrolls to end on first load

                ThreadUserTextBlock.Text = "";
                if (firstLoad)
                {
                    CurrentThreadPanel.Children.Clear();
                    foreach (var result in who)
                    {
                        var ctrl = new ContactControl(result);
                        ctrl.ThreadUsers.Content = result.FullName;
                        ctrl.AddToThreadButton.Click += RemoveFromThreadClick;
                        ctrl.InitializeComponent();
                        CurrentThreadPanel.Children.Add(ctrl);

                    }
                }
                
                if (who == null || who.Count == 0)
                {
                    ThreadUserTextBlock.Text = "";

                }
                else
                {
                    
                    foreach (var result in who)
                    {
                        ThreadUserTextBlock.Text += result.FullName + ", ";
                    }
                    char[] removecomma = { ',', ' ' };
                    ThreadUserTextBlock.Text = ThreadUserTextBlock.Text.TrimEnd(removecomma);
                }


                var lastMessage = SQLServer.MSSelectDataDictionary("SELECT TOP 1 messageid from whldata.messenger_messages WHERE threadid like'" + threadId.ToString() + "' ORDER BY messageid desc");
                if (lastMessage.Count > 0)
                {
                    try
                    {
                        _lastMessageInThread = Convert.ToInt32(lastMessage[0]["messageid"]);
                    }
                    catch (Exception)
                    {
                        _lastMessageInThread = -1;
                    }
                    

                }
                else _lastMessageInThread = -1;

            }
            

        }

        private void RemoveFromThreadClick(object sender, RoutedEventArgs routedEventArgs)
        {
            if (_currentThread != -1)
            {
                var parent = FindParent<ContactControl>(sender as Button) as ContactControl;
                RemoveFromThread(_currentThread, parent.EmployeeID);
            }
        }

        private void NewHyperLink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.ToString());
        }
        #endregion
        #region Notification Handling

        private Stopwatch _notiStopwatch = new Stopwatch();
        private void ThreadFinder_DoWork(object sender, DoWorkEventArgs e)
        {
            _notiStopwatch.Reset();
            _notiStopwatch.Start();
            _currentThreads.Clear();
            var threads = SQLServer.MSSelectDataDictionary("SELECT threadid from whldata.messenger_threads WHERE participantid='" + AuthdEmployee.PayrollId.ToString() + "'ORDER BY threadid desc");
            if (threads != null)
            {
                foreach (var result in threads)
                {
                    _currentThreads.Add(result["threadid"].ToString(), AuthdEmployee.PayrollId.ToString());
                }
            }
            _userLastThreadNoti.Clear();
            foreach (var entry in _currentThreads)
            {
                var latestMessage = SQLServer.SelectData("SELECT TOP 1 messageid from whldata.messenger_messages WHERE threadid='" + entry.Key + "' ORDER BY messageid desc;") as ArrayList;
                if (latestMessage == null || latestMessage.Count == 0) continue;
                try
                {
                    var result = latestMessage[0] as ArrayList;
                    if (result == null) continue;
                    _userLastThreadNoti.Add(Convert.ToInt32(entry.Key), Convert.ToInt32(result[0].ToString()));
                }
                catch (NullReferenceException)
                {
                }

            }
            if (File.Exists(SqLite.DbLocation) && !(_sqLiteWriter.IsBusy) && _updateSqliteDb)
            {
                _sqLiteWriter.RunWorkerAsync();
                _updateSqliteDb = false;
            }
            LoadNotifications();
        }
        private void ThreadLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_notisArrayList.Count > 0) DisplayNotifications(_notisArrayList);
            _notiStopwatch.Stop();
            Console.WriteLine(_notiStopwatch.ElapsedMilliseconds);
        }
        private void DisplayNotifications(List<Dictionary<string,object>> notis)
        {
            foreach (var noti in notis)
            {
                NotificationComponent[] notiText =
                {
                    new TextOnlyNotification(noti["messagecontent"].ToString(), HandleNoti)
                };
                var currentNoti = Notification.CreateNotification(_empcol.FindEmployeeByID(Convert.ToInt32(noti["participantid"])).FullName,notiText,-1F,HandleNotiReal);




                //Notification.CreateNotification("Messenger", notiText, 20);
            }
                

        }

        private void HandleNotiReal(object sender, NotificationBase bNotificationBase)
        {
            
        }

        private List<Dictionary<string, object>> _notisArrayList = new List<Dictionary<string, object>>();
        private void LoadNotifications()
        {
            System.Threading.Thread.Sleep(1000);
            var userLastThreadSafe = _userLastThreadNoti;
            _notisArrayList.Clear();
            foreach (var threads in userLastThreadSafe)
            {
                if (threads.Key == _currentThread) continue;
                var latestMessage = SQLServer.MSSelectDataDictionary("SELECT * from whldata.messenger_messages WHERE threadid='"+threads.Key.ToString()+"' AND messageid > '"+threads.Value.ToString() + "';") ;
                if (latestMessage == null || latestMessage.Count == 0) continue;
                foreach (var result in latestMessage)
                {
                    if (result["participantid"].ToString() == AuthdEmployee.PayrollId.ToString()) continue;
                    _notisArrayList.Add(result);

                }
            
            }
        }
        #endregion
        #region Click Events
        private void HandleNoti(object sender, NotificationComponent e)
        {
            
        }
        private void HandleThreadClick(object sender, MouseButtonEventArgs e)
        {
            var ctrl = sender as ThreadControl;
            if (sender is ThreadControl)
            {
                _currentThread = ctrl.ThreadId;
                ProcessThreadId(ctrl.ThreadId, true);
                TypeBox.IsReadOnly = false;
                TypeBox.Focus();
            }
        }
        private void OtherMsg_TouchUp(object sender, TouchEventArgs e)
        {
            var control = sender as OtherPictureControl;
            if (sender is OtherPictureControl)
            {
                var showPicture = new ActualPicture();
                showPicture.NewImage.Source = control.ImageContainer.Source;
                showPicture.InitializeComponent();
                showPicture.Show();
            }
        }

        private void OtherMsg_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var control = sender as OtherPictureControl;
            if (control != null)
            {
                var showPicture = new ActualPicture();
                showPicture.NewImage.Source = control.ImageContainer.Source;
                showPicture.InitializeComponent();
                showPicture.Show();
            }
        }

        private void Msg_TouchUp(object sender, TouchEventArgs e)
        {
            var control = sender as UserPictureControl;
            if (control != null)
            {
                var showPicture = new ActualPicture();
                showPicture.NewImage.Source = control.ImageContainer.Source;
                showPicture.InitializeComponent();
                showPicture.Show();
            }
        }

        private void Msg_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var control = sender as UserPictureControl;
            if (control != null)
            {
                var showPicture = new ActualPicture();
                showPicture.NewImage.Source = control.ImageContainer.Source;
                showPicture.InitializeComponent();
                showPicture.Show();
            }
        }

        private async void TextBox_KeyDown(object sender, KeyEventArgs e)

        {
            if (e.Key != Key.Return)
            {

            }
            else
            {
                if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                {
                    TypeBox.Text = TypeBox.Text + Environment.NewLine;
                }
                else if (_currentThread != -1)
                {
                    var sendMessageTask = SendMessageasync(_currentThread, TypeBox.Text);

                    TypeBox.Text = "";
                    RefreshTimer_Tick(null, null);
                    ThreadLoaderTimerTick(null, null);
                    var success = await sendMessageTask;
                    if (!success) throw new SQLGeneric("Failed to send message");
                }
            }
        }
        private void SettingsImageButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsImageButton.Visibility = Visibility.Collapsed;
            //throw new NotImplementedException("It's a feature"); //It's a feature
        }

        private void OptionsClose_Click(object sender, RoutedEventArgs e)
        {
            SettingsImageButton.Visibility = Visibility.Visible;
        }


        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentThread != -1)
            {
                var sendMessageTask = SendMessageasync(_currentThread, TypeBox.Text);

                TypeBox.Text = "";
                RefreshTimer_Tick(null, null);
                ThreadLoaderTimerTick(null, null);
                var success = await sendMessageTask;
                if (!success) throw new SQLGeneric("Failed to send message");
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
            if (_currentThread != -1) AddToThread(_currentThread, parent.EmployeeID);
        }
        /// <summary>
        /// Uses the Win32 OpenFileDialog function to load a photograph
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SendPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentThread != -1)
            {
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.DefaultExt = ".jpg";
                dlg.Filter = "Image Files |*.jpeg;*.png;*.jpg";



                var result = dlg.ShowDialog();
                if (result == true)
                {
                    var notidirectory = @"\\WIN-NOHLS1H9ER8\Data Storage\NotiTest\" + DateTime.Now.Ticks.ToString() + "_" + dlg.SafeFileName;
                    File.Copy(dlg.FileName, notidirectory);
                    var sendMessageTask = SendMessageasync(_currentThread, notidirectory);

                    TypeBox.Text = "";
                    RefreshTimer_Tick(null, null);
                    ThreadLoaderTimerTick(null, null);
                    var success = sendMessageTask.Result;
                    if (!success) throw new SQLGeneric("Failed to send message");
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        private void LeaveThreadButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Do you want to leave this thread?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                RemoveFromThread(_currentThread,AuthdEmployee.PayrollId);
            }
        }
        #endregion

        #region Thread Controller
        /// <summary>
        /// Adds a user to the specified thread
        /// </summary>
        /// <param name="threadId">The targeted thread</param>
        /// <param name="employeeId">The payrollid of the targeted employee</param>
        private void AddToThread(int threadId,int employeeId)
        {
            if (!CheckForUserInThread(threadId,employeeId))
            {
                SQLServer.MSInsertUpdate("INSERT INTO whldata.messenger_threads(threadId, participantid,IsTwoWay) VALUES('" + threadId.ToString() + "', '" + employeeId.ToString() + "',0);");
                SQLServer.MSInsertUpdate("UPDATE whldata.messenger_threads SET IsTwoWay=0 WHERE threadid='"+threadId.ToString()+"'");
               LoadThreads();
            }
        }
        private void CreateNewThread(int employeeId)
        {
            var checkForTwoWay = SQLServer.MSSelectDataDictionaryAsync("SELECT * from whldata.messenger_threads f where f.participantid ='" + AuthdEmployee.PayrollId.ToString()+ "' AND exists(SELECT * from whldata.messenger_threads s WHERE s.participantid = '" + employeeId.ToString()+ "' AND s.threadid = f.threadid) AND f.IsTwoWay = 1");
            if (checkForTwoWay == null) throw new Exception("SQL Query Failed");
            if (checkForTwoWay.Result.Count == 0)
            {
                var newThread = _latestThreadId + 1;
                SQLServer.MSInsertUpdate("INSERT INTO whldata.messenger_threads (threadId, participantid,IsTwoWay) VALUES (" + newThread.ToString() + "," + AuthdEmployee.PayrollId.ToString() + ",1)");
                SQLServer.MSInsertUpdate("INSERT INTO whldata.messenger_threads (threadId, participantid,IsTwoWay) VALUES (" + newThread.ToString() + "," + employeeId.ToString() + ",1)");
                _currentThread = newThread;
                MessageStack.Children.Clear();
                LoadThreads();
            }
        }

        private void RemoveFromThread(int threadId, int employeeid)
        {
            SQLServer.MSInsertUpdate("DELETE FROM whldata.messenger_threads WHERE threadid='"+threadId.ToString()+ "' AND participantid = '"+employeeid.ToString()+"';");
            SQLServer.MSInsertUpdate("INSERT INTO whldata.messenger_messages  (participantid,messagecontent,timestamp,threadid) VALUES (0,N'"+ _empcol.FindEmployeeByID(employeeid).FullName + " has been removed from the thread',Current_timestamp,'" + threadId.ToString() + "')");
            if (CheckThreadUsers(_currentThread).Count == 0)
            {
                _currentThread = -1;
                TypeBox.IsReadOnly = true;
            }
        }
        #endregion
        #region Functions
        private async Task<bool> SendMessageasync(int ThreadID, string Message)
        {
            try
            {
                var safeMsg = Message.Replace(";", "");
                safeMsg = safeMsg.Replace("--", "");
                safeMsg = safeMsg.Replace("'", "''");
                safeMsg = safeMsg.Trim();
                if (safeMsg != "")
                {
                    SQLServer.MSInsertUpdate("INSERT INTO whldata.messenger_messages (participantid,messagecontent,timestamp,threadid) VALUES (" + AuthdEmployee.PayrollId.ToString() + ",N'" + safeMsg + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + ThreadID.ToString() + "')");
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }


        }
        private bool CheckForUserInThread(int ThreadID, int EmployeeID)
        {
            var results = SQLServer.MSSelectDataDictionary("SELECT * from whldata.messenger_threads WHERE participantid='" + EmployeeID.ToString() + "' AND threadid='" + ThreadID.ToString() + "'");
            if (results == null) return false;
            if (results.Count > 0) return true;
            else return false;
        }
        /// <summary>
        /// This function returns a list of Users in the thread
        /// </summary>
        /// <param name="threadId">The ID of the thread</param>
        /// <param name="ignoreself">Boolean to check if the function should ignore the user</param>
        /// <returns>A list of users in the specified thread</returns>
        private List<Employee> CheckThreadUsers(int threadId, bool ignoreself = true)
        {
            var returnList = new List<Employee>();
            try
            {
                var query = SQLServer.MSSelectDataDictionary("SELECT participantid FROM whldata.messenger_threads WHERE threadId like '" + threadId.ToString() + "';");
                if (query == null) throw new Exception("SQL Query Failed");
                foreach (var result in query)
                {
                    if (ignoreself)
                    {
                        if ((int.Parse(result["participantid"].ToString()) != AuthdEmployee.PayrollId))
                        //Check if we're a member of the thread
                        {
                            returnList.Add(_empcol.FindEmployeeByID(int.Parse(result["participantid"].ToString())));
                        }
                    }
                    else
                    {
                        returnList.Add(_empcol.FindEmployeeByID(int.Parse(result["participantid"].ToString())));
                    }
                }
            }
            catch (Exception)
            {
                returnList.Clear();
            }
            return returnList;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">The required type</typeparam>
        /// <param name="child">The current UI Object</param>
        /// <returns>The specified UI element</returns>
        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            //get parent item
            var parentObject = VisualTreeHelper.GetParent(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            var parent = parentObject as T;
            if (parent != null) return parent;
            return FindParent<T>(parentObject); //Intentional Recursive method
        }
#endregion
        private async void Messenger_Closed(object sender, EventArgs e)
        {
            await Task.Run(() => SendMessageasync(0,"")).ConfigureAwait(false);
            _threadLoader.CancelAsync();
            _threadFinder.CancelAsync();
        }

    }
}


