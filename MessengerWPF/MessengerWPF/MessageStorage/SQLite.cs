using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace MessengerWPF.MessageStorage
{
    /// <summary>
    /// Class to handle all SQLite Interaction
    /// </summary>
    public static class SqLite
    {
        /// <summary>
        /// Current DBLocation, Defaults to the AppData folder
        /// </summary>
        public static string DbLocation = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WHL\Messenger.sqlite";
        /// <summary>
        /// Connection String, Defaults to the AppData folder
        /// </summary>
        private static string SqLiteConnStr = @"Data Source="+ Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WHL\Messenger.sqlite";

        /// <summary>
        /// Allows for selection of a list of dictionaries based on the Input query
        /// </summary>
        /// <param name="Query">Query to be run on the SQLite Database</param>
        /// <returns>A list of Dictionaries. Each dict represents a row, key represents the column name.</returns>
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
                    var fieldloop = 0;
                    var row = new Dictionary<string, object>();
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
        /// <summary>
        /// Tests the connection between the user and the SQLite Database
        /// </summary>
        /// <returns>The success of the connection</returns>
        public static bool SqLiteTestConn()
        {
            //From heere
            var conn = new SQLiteConnection(SqLiteConnStr);

            try
            {
                conn.Open();
                //To here should be the same every time. After here in this little block is where we actually do the work.

                // Do nothing with the connection.

                //Then return at the end with the data.
                Console.WriteLine("Connection to " + conn.Database + " is active (" + DateTime.Now.ToLongDateString() + ")");
                return true;
                    

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            finally
            {
                conn.Close();
            }

        }
        /// <summary>
        /// Depreciated method for quick use. Returns an arraylist of arraylists 
        /// </summary>
        /// <param name="query">Query to be run on the SQLite Database</param>
        /// <returns>ArrayList of ArrayLists</returns>
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
        /// <summary>
        /// Use for any none select query, returns a result based on the query inputted
        /// </summary>
        /// <param name="query">Query to be run on the SQLite Database</param>
        /// <returns></returns>
        public static object SqliteOtherQuery(string query)
        {

            //From heere
            var conn = new SQLiteConnection(SqLiteConnStr);


            try
            {
                
                conn.Open();
                //To here should be the same every time. After here in this little block is where we actually do the work.

                var sqlquery = new SQLiteCommand(query, conn);

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
        /// <summary>
        /// Prepares the database for use with the messenger application.
        /// </summary>
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
