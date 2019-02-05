using DevExpress.Xpo;
using NewNetServices.Module.BusinessObjects.CableManagement;
using NewNetServices.Module.BusinessObjects.Core;
//using Oracle.DataAccess.Client;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;


namespace DataConverter.Classes
{
    public class OracleDatabaseWorker : IDisposable
    {
        private OracleCommand cmd;
        private OracleConnection conn = new OracleConnection();
        private string cs;

        public OracleDatabaseWorker(Session session)
        {
            GlobalSystemSettings gss = session.Query<GlobalSystemSettings>().First();
            string user = gss.OracleDatabaseUserId;
            string password = gss.OracleDatabasePassword;
            string source = gss.OracleDatabaseSource;
            //"Data Source=//SERVER:PORT/INSTANCE_NAME;USER=XXX;PASSWORD=XXX"
            // string ds = $"Data Source=//kevindeveloper\\XE;USER={user};PASSWORD={password}";
            cs = $"User Id={user};Password={password};Data Source={source}";
            NewNetServices.Module.Core.StaticHelperMethods.WriteOut(cs);
            conn = new OracleConnection(cs);// AppDomain.CurrentDomain.SetupInformation.ConfigurationFile.IndexOf("connectionString")); // C#
            conn.Open();
        }
        //public OracleDatabaseWorker(string cs )
        //{
        //    try
        //    {
        //        conn = new OracleConnection();// AppDomain.CurrentDomain.SetupInformation.ConfigurationFile.IndexOf("connectionString")); // C#
        //        //conn.DatabaseName = "MSC_KALONA";//Open();
        //        //conn.DataSource = "newnetservices.us/XE";//Open();
        //        conn.ConnectionString = cs;// = "CRN";
        //                                   //  conn.ChangeDatabase(dbName);
        //        conn.Open();
        //        //conn.ClientId = dbName;
        //        //conn.ChangeDatabase(dbName);
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("" + ex);
        //    }
        //}
        public OracleDatabaseWorker(string cs)
        {
            try
            {
                conn = new OracleConnection();// AppDomain.CurrentDomain.SetupInformation.ConfigurationFile.IndexOf("connectionString")); // C#
                //conn.DatabaseName = "MSC_KALONA";//Open();
                //conn.DataSource = "newnetservices.us/XE";//Open();
                conn.ConnectionString = cs;// = "CRN";
                NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{cs}");            //  conn.ChangeDatabase(dbName);
                conn.Open();
                //conn.ClientId = dbName;
                //conn.ChangeDatabase(dbName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("" + ex);
            }
        }

        public void SetGuidFromGidOracle(string table, string gid, string guid)
        {
            try
            {
                OracleCommand command = new OracleCommand();

                command.Connection = conn;
                //        command.Connection.Ope n ();
                command.CommandType = CommandType.Text;
                command.CommandText = $"update {table} set  guid = '{guid}' where GID = {gid}";
                NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{command.CommandText}");

                NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"BEFORE");
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{ex}");
            }

        }

        public void Dispose()
        {
            this.conn.Close();
        }

        public List<string[]> GetAllCmsMunsysItems(int orderByColumn, params string[] columns)
        {
            //only accounting for columans that are string type. Gids are ints and assumed to be FIRST COLOUMN in columns parameters. if orderby greater that 0, order by that param index
            List<string[]> returnList = new List<string[]>();
            int numCols = columns.Length;
            bool containsGid = columns.Contains("GID");

            cmd = new OracleCommand();

            cmd.Connection = conn;
            cmd.CommandText = "SELECT msc_kalona.ACC_POINT.* FROM msc_kalona.ACC_POINT";
            //string selectColumns = string.Join(", ", columns);
            //// cmd.CommandText = $"select GID,GUID, TYPE_NAME  from SP_NN_LOCATION where GUID is not null";
            //if (orderByColumn > -1)
            //{
            //    cmd.CommandText = $"SELECT { selectColumns} FROM (" +
            //    $"Select { selectColumns} FROM SP_NN_LOCATION" +
            //    $"union" +
            //    $"Select { selectColumns} FROM SP_NN_CABLE" +
            //    $"union" +
            //    $"Select { selectColumns} FROM SP_NN_CONDUIT" +
            //    $"union " +
            //    $"Select { selectColumns} FROM SP_NN_MISCFACILITY)" +
            //    $"  order by {columns[orderByColumn]}";
            //}
            //else
            //{
            //    cmd.CommandText =
            //    $"Select { selectColumns} FROM SP_NN_LOCATION" +
            //    $"union" +
            //    $"Select { selectColumns} FROM SP_NN_CABLE" +
            //    $"union" +
            //    $"Select { selectColumns} FROM SP_NN_CONDUIT" +
            //    $"union " +
            //    $"Select { selectColumns} FROM SP_NN_MISCFACILITY"
            //    ;
            //}

            cmd.CommandType = CommandType.Text;

            OracleDataReader dr = cmd.ExecuteReader();
            if (dr.HasRows)
            {

                //if (dr.HasRows)
                //{
                //    while (dr.Read())
                //    {
                //        retList.Add(dr.GetInt64(0));
                //    }
                //}
                while (dr.Read())
                {
                    var numCol = dr.FieldCount;
                    string[] arrToAdd = new string[numCol];
                    for (int i = 0; i < numCol; i++)
                    {
                        arrToAdd[i] = dr.GetValue(i).ToString();
                    }
                    returnList.Add(arrToAdd);
                }
            }

            return returnList;
        }
        public List<string[]> GetAllCmsMunsysItemsForTable(string table, int orderByColumn, params string[] columns)
        {
            //only accounting for columans that are string type. Gids are ints and assumed to be FIRST COLOUMN in columns parameters. if orderby greater that 0, order by that param index
            List<string[]> returnList = new List<string[]>();
            
            cmd = new OracleCommand();

            cmd.Connection = conn;
            string selectColumns = string.Join(", ", columns);
            // cmd.CommandText = $"select GID,GUID, TYPE_NAME  from SP_NN_LOCATION where GUID is not null";
            if (orderByColumn > -1)
                cmd.CommandText = $"select {selectColumns} from {table} order by {columns[orderByColumn]}".ToUpperInvariant();
            else
                cmd.CommandText = $"select {selectColumns} from {table}".ToUpperInvariant();/* where rownum<300*/
                                                                         // cmd.CommandText = $"select {selectColumns} from {table} where sym_name like 'PED'";/* where rownum<300*/
            NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{cmd.CommandText}");
            cmd.CommandType = CommandType.Text;
            int numCols = columns.Length;
            string[] arrToAdd = new string[numCols];
            OracleDataReader dr = cmd.ExecuteReader();
            try
            {
                if (dr.HasRows)
                {

                    while (dr.Read())
                    {
                        arrToAdd = new string[numCols];
                        for (int i = 0; i < numCols; i++)
                        {
                            arrToAdd[i] = dr.GetValue(i).ToString();
                        }
                        returnList.Add(arrToAdd);
                    }
                }
            }
            catch (Exception ex)
            {
                NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{string.Join(" ", arrToAdd)}!!!!!!!!!!!!!!!!!!!!!{ex}");
            }

            return returnList;
        } public List<string[]> GetAllCmsMunsysItemsForTable(string table,  params string[] columns)
        {
            //only accounting for columans that are string type. Gids are ints and assumed to be FIRST COLOUMN in columns parameters. if orderby greater that 0, order by that param index
            List<string[]> returnList = new List<string[]>();
            
            cmd = new OracleCommand();

            cmd.Connection = conn;
            string selectColumns = string.Join(", ", columns);
           
                cmd.CommandText = $"select {selectColumns} from {table} where guid is null".ToUpperInvariant();/* where rownum<300*/
                                                                         // cmd.CommandText = $"select {selectColumns} from {table} where sym_name like 'PED'";/* where rownum<300*/
            NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{cmd.CommandText}");
            cmd.CommandType = CommandType.Text;
            int numCols = columns.Length;
            string[] arrToAdd = new string[numCols];
            OracleDataReader dr = cmd.ExecuteReader();
            try
            {
                if (dr.HasRows)
                {

                    while (dr.Read())
                    {
                        arrToAdd = new string[numCols];
                        for (int i = 0; i < numCols; i++)
                        {
                            arrToAdd[i] = dr.GetValue(i).ToString();
                        }
                        returnList.Add(arrToAdd);
                    }
                }
            }
            catch (Exception ex)
            {
                NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{string.Join(" ", arrToAdd)}!!!!!!!!!!!!!!!!!!!!!{ex}");
            }

            return returnList;
        } 
       // public async   Task<List<Dictionary<string, string>>> GetData(string table, params string[] columns)
        public  List<Dictionary<string, string>> GetData(string table, params string[] columns)
        {
            //only accounting for columans that are string type. Gids are ints and assumed to be FIRST COLOUMN in columns parameters. if orderby greater that 0, order by that param index
            try
            {
                List<Dictionary<string, string>> returnList = new List<Dictionary<string, string>>();
                int numCols = columns.Length;

                cmd = new OracleCommand();

                cmd.Connection = conn;
                string selectColumns = string.Join(", ", columns);
                // cmd.CommandText = $"select GID,GUID, TYPE_NAME  from SP_NN_LOCATION where GUID is not null";

                cmd.CommandText = $"select {selectColumns} from  {table}";
                NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{cmd.CommandText}");
                cmd.CommandType = CommandType.Text;
                var dr =    cmd.ExecuteReader();
              //  var dr = await  cmd.ExecuteReaderAsync();
                if (dr.HasRows)
                {
                    try
                    {

                        while (dr.Read())
                        {
                            Dictionary<string, string> innerList = new Dictionary<string, string>();
                            for (int i = 0; i < numCols; i++)
                            {
                                innerList.Add(columns[i], dr.GetValue(i).ToString());
                                // NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{innerList[columns[i]]}",false);
                            }
                            //     NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"<-----{string.Join(" ", innerList.Select(x => x.Key + " : " + x.Value))} /---->");


                            returnList.Add(innerList);
                        }
                    }


                    catch (Exception ex)
                    {
                        NewNetServices.Module.Core.StaticHelperMethods.WriteOut($" !!!!!!!!!!!!!!!!!!!!!{ex}");
                    }
                }
                //  NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{new String('*', 100)}");
                return returnList;
            }
            catch (Exception x)
            {
                NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{x}");
                return new List<Dictionary<string, string>>();
            }
        }

       
    }
}