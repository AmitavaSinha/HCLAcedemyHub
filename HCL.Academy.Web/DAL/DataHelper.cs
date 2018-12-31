using System;
using System.Data;
using System.Data.SqlClient;

namespace HCL.Academy.Web.DAL
{
    public class DataHelper
    {
        //this is the default time for ADO.NET connection object.
        private int DataCmdTimeout = 240;

        [ThreadStatic]
        public SqlConnection DataConn = new SqlConnection();

        private SqlCommand cmd;


        public DataHelper() { }

        public DataHelper(string ConnectionString)
        {
            DataConn.ConnectionString = ConnectionString;
            DataConn.Open();
        }

        public string Connection
        {
            set
            {
                DataConn.ConnectionString = value;
            }
        }

        public int CmdTimeout
        {
            set
            {
                DataCmdTimeout = value;
            }
        }

        public DataHelper(SqlConnection conn)
        {
            DataConn = conn;
        }

        public SqlCommand Cmd
        {
            get
            {
                return cmd;
            }
            set
            {
                cmd = value;
            }
        }

        
        /// <summary>
        /// this enum is used to indicate whether the connection was provided by the caller, or created by SqlHelper, so that
        /// we can set the appropriate CommandBehavior when calling ExecuteReader()
        /// </summary>
        private enum SqlConnectionOwnership
        {
            /// <summary>Connection is owned and managed by SqlHelper</summary>
            Internal,
            /// <summary>Connection is owned and managed by the caller</summary>
            External
        }

        #region ExecuteNonQuery
        /// <summary>
        /// Execute a SqlCommand (that returns no resultset and takes no parameters) against the database specified in 
        /// the connection string. 
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  int result = ExecuteNonQuery(connString, CommandType.StoredProcedure, "PublishOrders");
        /// </remarks>
        /// <param name="connectionString">a valid connection string for a SqlConnection</param>
        /// <param name="connection">a valid SqlConnection Object</param>
        /// <param name="commandText">the stored procedure name or T-SQL command</param>
        /// <param name="commandType">the type of stmt being executed; CommandType(TEXT, SP)</param>
        /// <returns>an int representing the number of rows affected by the command</returns>
        public int ExecuteNonQuery(string connectionString, string commandText, CommandType commandType)
        {
            if (DataConn.State == ConnectionState.Open)
            {
                DataConn.Close();
            }
            DataConn.ConnectionString = connectionString;
            return ExecuteNonQuery(DataConn, commandText, commandType, null, null);
        }
        public int ExecuteNonQuery(string commandText, CommandType commandType)
        {
            return ExecuteNonQuery(DataConn, commandText, commandType, null, null);
        }
        public int ExecuteNonQuery(string commandText, CommandType commandType, SqlTransaction trans)
        {
            return ExecuteNonQuery(DataConn, commandText, commandType, trans, null);
        }
        public int ExecuteNonQuery(string commandText, CommandType commandType, params SqlParameter[] commandParameters)
        {
            return ExecuteNonQuery(DataConn, commandText, commandType, null, commandParameters);
        }
        public int ExecuteNonQuery(string commandText, CommandType commandType, SqlTransaction trans, params SqlParameter[] commandParameters)
        {
            return ExecuteNonQuery(DataConn, commandText, commandType, trans, commandParameters);
        }

        private int ExecuteNonQuery(SqlConnection conn, string commandText, CommandType commandType, SqlTransaction trans, params SqlParameter[] commandParameters)
        {
            //SqlCommand cmd = new SqlCommand();
            Cmd = new SqlCommand();
            //PrepareCommand(cmd, conn, commandText, commandType, trans, commandParameters);
            //return (int)cmd.ExecuteNonQuery();
            PrepareCommand(Cmd, conn, commandText, commandType, trans, commandParameters);
            return (int)Cmd.ExecuteNonQuery();

        }
        #endregion ExecuteNonQuery

        #region ExecuteScalar
        /// <summary>
        /// Execute a SqlCommand (that returns a 1x1 resultset and takes no parameters) against the database specified in 
        /// the connection string. 
        /// </summary>
        /// <remarks>
        /// e.g.:  
        ///  int orderCount = (int)ExecuteScalar(connString, CommandType.StoredProcedure, "GetOrderCount");
        /// </remarks>
        /// <param name="connectionString">a valid connection string for a SqlConnection</param>
        /// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">the stored procedure name or T-SQL command</param>
        /// <returns>an object containing the value in the 1x1 resultset generated by the command</returns>
        public object ExecuteScalar(string ConnectionString, string CommandText, CommandType commandType)
        {
            if (DataConn.State == ConnectionState.Open)
            {
                DataConn.Close();
            }
            DataConn.ConnectionString = ConnectionString;
            return ExecuteScalar(DataConn, CommandText, commandType, null, null);
        }

        public object ExecuteScalar(string CommandText, CommandType commandType)
        {
            return ExecuteScalar(DataConn, CommandText, commandType, null, null);
        }
        public object ExecuteScalar(string commandText, CommandType commandType, SqlTransaction trans, params SqlParameter[] commandParameters)
        {
            return ExecuteScalar(DataConn, commandText, commandType, trans, commandParameters);
        }
        public object ExecuteScalar(string commandText, CommandType commandType, params SqlParameter[] commandParameters)
        {
            return ExecuteScalar(DataConn, commandText, commandType, null, commandParameters);
        }
        private object ExecuteScalar(SqlConnection conn, string commandText, CommandType commandType, SqlTransaction trans, params SqlParameter[] commandParameters)
        {
            SqlCommand cmd = new SqlCommand();
            PrepareCommand(cmd, conn, commandText, commandType, trans, commandParameters);
            object returnValue;

            returnValue = cmd.ExecuteScalar();

            return returnValue;
        }
        #endregion ExecuteScalar

        #region DataReader
        /// <summary>
        /// Create and prepare a SqlCommand, and call ExecuteReader with the appropriate CommandBehavior.
        /// </summary>
        /// <remarks>
        /// If we created and opened the connection, we want the connection to be closed when the DataReader is closed.
        /// 
        /// If the caller provided the connection, we want to leave it to them to manage.
        /// </remarks>
        /// <param name="ConnectionString">a valid connection string, on which to execute this command</param>
        /// <param name="commandText">the stored procedure name or T-SQL command</param>
        /// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
        /// <param name="connectionOwnership">indicates whether the connection parameter was provided by the caller, or created by SqlHelper</param>
        /// <returns>SqlDataReader containing the results of the command</returns>
        public SqlDataReader ExecuteDataReader(string ConnectionString, string CommandText, CommandType commandType)
        {
            if (DataConn.State == ConnectionState.Open)
            {
                DataConn.Close();
            }
            DataConn.ConnectionString = ConnectionString;
            return ExecuteDataReader(DataConn, CommandText, commandType, null);
        }
        public SqlDataReader ExecuteDataReader(string CommandText, CommandType commandType)
        {
            return ExecuteDataReader(DataConn, CommandText, commandType, null);
        }
        public SqlDataReader ExecuteDataReader(string CommandText, CommandType commandType, params SqlParameter[] commandParameters)
        {
            return ExecuteDataReader(DataConn, CommandText, commandType, commandParameters);
        }
        private SqlDataReader ExecuteDataReader(SqlConnection conn, string CommandText, CommandType commandType, params SqlParameter[] commandParameters)
        {
            SqlCommand cmd = new SqlCommand();
            PrepareCommand(cmd, conn, CommandText, commandType, commandParameters);
            //create a reader
            SqlDataReader dr;
            // call ExecuteReader with the appropriate CommandBehavior
            //Important...connection will stay open while scrolling through result-set
            dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
            // detach the SqlParameters from the command object, so they can be used again.
            cmd.Parameters.Clear();
            return dr;
        }
        #endregion DataReader

        #region DataSet
        public DataSet ExecuteDataSet(string connectionString, string commandText, CommandType commandType)
        {
            if (DataConn.State == ConnectionState.Open)
            {
                DataConn.Close();
            }
            DataConn.ConnectionString = connectionString;
            return ExecuteDataSet(DataConn, commandText, commandType, null, null);
        }
        public DataSet ExecuteDataSet(string commandText, CommandType commandType)
        {
            return ExecuteDataSet(DataConn, commandText, commandType, null, null);
        }
        public DataSet ExecuteDataSet(string commandText, CommandType commandType, params SqlParameter[] commandParameters)
        {
            return ExecuteDataSet(DataConn, commandText, commandType, null, commandParameters);
        }
        public DataSet ExecuteDataSet(string commandText, CommandType commandType, SqlTransaction trans)
        {
            return ExecuteDataSet(DataConn, commandText, commandType, trans, null);
        }
        public DataSet ExecuteDataSet(string commandText, CommandType commandType, SqlTransaction trans, params SqlParameter[] commandParameters)
        {
            return ExecuteDataSet(DataConn, commandText, commandType, trans, commandParameters);
        }
        private DataSet ExecuteDataSet(SqlConnection conn, string commandText, CommandType commandType, SqlTransaction trans, params SqlParameter[] commandParameters)
        {
            //Ensure the connection is open !
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            //SqlCommand cmd = new SqlCommand();
            Cmd = new SqlCommand();
            //PrepareCommand(cmd, conn, commandText, commandType, trans, commandParameters);
            PrepareCommand(Cmd, conn, commandText, commandType, trans, commandParameters);
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds);
            return ds;
        }
        #endregion DataSet

        #region Boolean
        public Boolean ExecuteBoolean(string commandText, CommandType commandType, params SqlParameter[] commandParameters)
        {
            return ExecuteBoolean(DataConn, commandText, commandType, null, commandParameters);
        }

        private Boolean ExecuteBoolean(SqlConnection conn, string commandText, CommandType commandType, SqlTransaction trans, params SqlParameter[] commandParameters)
        {
            //Ensure the connection is open !
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }

            SqlCommand cmd = new SqlCommand();
            PrepareCommand(cmd, conn, commandText, commandType, trans, commandParameters);
            Boolean returnValue = false;
            Boolean boolreturnValue = false;
            cmd.ExecuteNonQuery();

            foreach (SqlParameter p in commandParameters)
            {
                //check for derived output value with no value assigned
                if ((p.Direction == ParameterDirection.Output))
                {
                    returnValue = (Boolean)p.Value;
                    break;
                }
            }

            if (returnValue == true)
            {
                boolreturnValue = true;
            }
            else
            {
                boolreturnValue = false;
            }
            return boolreturnValue;
        }


        #endregion Boolean

        #region Private Utility for DataHelper
        private void PrepareCommand(SqlCommand command, SqlConnection connection, string commandText, CommandType commandType, SqlParameter[] commandParameters)
        {
            PrepareCommand(command, connection, commandText, commandType, null, commandParameters);
        }
        private void PrepareCommand(SqlCommand command, SqlConnection connection, string commandText, CommandType commandType, SqlTransaction trans, SqlParameter[] commandParameters)
        {
            //if the provided connection is not open, we will open it
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
            //associate the connection with the command
            command.Connection = connection;
            command.CommandTimeout = DataCmdTimeout;
            //set the command text (stored procedure name or SQL statement)
            command.CommandText = commandText;
            command.CommandType = commandType;
            if (trans != null)
            {
                command.Transaction = trans;
            }

            //attach the command parameters if they are provided
            if (commandParameters != null)
            {
                AttachParameters(command, commandParameters);
            }
            return;
        }
        private void AttachParameters(SqlCommand command, SqlParameter[] commandParameters)
        {
            foreach (SqlParameter p in commandParameters)
            {
                //check for derived output value with no value assigned
                if ((p.Direction == ParameterDirection.InputOutput) && (p.Value == null))
                {
                    p.Value = DBNull.Value;
                }

                command.Parameters.Add(p);
            }
        }
        #endregion Private Utility for DataHelper
    }
}