using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace MessengerWPF.MessageStorage
{
    public class SqLite
    {
        public static string DbLocation = @"C:\WHL\Messenger.sqlite";
        public static string SqLiteConnStr = @"Data Source=C:\WHL\Messenger.sqlite;Version=3;";

        public static List<Dictionary<string, object>> SqLiteSelectDataDictionary(string Query)
        {
            //Create MSSQLPublic Connection
            var conn = new SQLiteConnection(SqLiteConnStr);
            try
            {
                conn.Open();
                var sqlquery = new SQLiteCommand(Query, conn);
                var returneddata = sqlquery.ExecuteReader();

               var returnme = new List<Dictionary<string, object>>();

                while (returneddata.Read())
                {
                    int fieldloop = 0;
                    Dictionary<string, object> row = new Dictionary<string, object>();
                    row.Clear();
                    while (fieldloop < returneddata.FieldCount)
                    {
                        row.Add(returneddata.GetName(fieldloop).ToLower(), returneddata[fieldloop]);
                        fieldloop = fieldloop + 1;
                    }
                    returnme.Add(row);


                }


                //Then return at the end with the data.
                return returnme;
            }
            catch (Exception ex)
            {
                throw ex;

            }
            finally
            {
                conn.Close();
            }
        }
        public static object SqLiteTestConn()
        {
            //From heere
            var conn = new SQLiteConnection(SqLiteConnStr);

            try
            {
                conn.Open();
                //To here should be the same every time. After here in this little block is where we actually do the work.

                // Do nothing with the connection.

                //Then return at the end with the data.
                return "Connection to " + conn.Database + " is active (" + DateTime.Now.ToLongDateString() + ")";

            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                conn.Close();
            }

        }
        public static object SqLiteSelectData(string query)
        {
            //From heere
            var conn = new SQLiteConnection(SqLiteConnStr);

            try
            {
                conn.Open();
                //To here should be the same every time. After here in this little block is where we actually do the work.

                var sqlquery = new SQLiteCommand(query, conn);
                var returneddata = sqlquery.ExecuteReader();

                var returnme = new ArrayList();
                var looper = 0;

                while (returneddata.Read())
                {
                    var fieldloop = 0;
                    var row = new ArrayList();
                    row.Clear();
                    while (fieldloop < returneddata.FieldCount)
                    {
                        row.Add(returneddata[fieldloop]);
                        fieldloop = fieldloop + 1;
                    }
                    returnme.Add(row);


                }


                //Then return at the end with the data.
                return returnme;

            }
            catch (Exception ex)
            {
                return ex.Message;

            }
            finally
            {
                conn.Close();
            }

        }
        public static object SqLiteInsertupdate(string query)
        {

            //From heere
            SQLiteConnection conn = new SQLiteConnection(SqLiteConnStr);


            try
            {
                
                conn.Open();
                //To here should be the same every time. After here in this little block is where we actually do the work.

                SQLiteCommand sqlquery = new SQLiteCommand(query, conn);

                return sqlquery.ExecuteNonQuery();



            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                conn.Close();
            }

        }

        public static void PrepareDb()
        {
            if (!File.Exists(DbLocation))
            {
                SQLiteConnection.CreateFile(DbLocation);
                Console.WriteLine("Created Database");
                Console.WriteLine("Creating Tables");
                var connection = new SQLiteConnection(SqLiteConnStr);
                connection.Open();
                var createTableStr = "CREATE TABLE messenger_threads (idmessenger_threads int,threadid varchar(20),participantid int,notified int,istwoway boolean);CREATE TABLE messenger_messages (messageid int, participantid int, messagecontent text,timestamp datetime, threadid int);";
                var command =  new SQLiteCommand(createTableStr,connection);
                command.ExecuteNonQuery();
                Console.WriteLine("Database prepared");
                connection.Close();
            }
            else
            {
                Console.WriteLine("DB Exists, aborting");
               
            }
        }
    }
}
