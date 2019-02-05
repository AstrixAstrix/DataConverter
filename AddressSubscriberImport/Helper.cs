using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks; 
using NewNetServices.Module; 
using NewNetServices.Module.BusinessObjects.CableManagement;

namespace AddressSubscriberImport
{
    public static class Helper
    {

        public static SelectedData GetData(Session session, string table, string[] columns = null)
        {
            string sql = $"SELECT {(columns == null ? "*" : string.Join(", ", columns))} from {table}";
            var result = session.ExecuteQuery(sql);
            return result;
        }
        public static List<Dictionary<string, string>> GetDictionaryListFromData(SelectedData data, string[] columns)
        {
            var ret = new List<Dictionary<string, string>>();
            foreach (var row in data.ResultSet[0].Rows)
            {
                Dictionary<string, string> rowDictionary = new Dictionary<string, string>();
                for (int i = 0; i < row.Values.Length && i < columns.Length; i++)
                {
                    rowDictionary[columns[i]] = $"{row.Values[i]}".Trim();
                }
                ret.Add(rowDictionary);
            }
            return ret;
        }
        public static void SetThreadSafeDataLayer(out ThreadSafeDataLayer Tsdl, string conn)
        {
            // Code that runs on application startup
            DevExpress.Xpo.Metadata.XPDictionary dict =
                new DevExpress.Xpo.Metadata.ReflectionDictionary();
            dict.GetDataStoreSchema(typeof(NewNetServicesModule).Assembly);
            DevExpress.Xpo.DB.IDataStore store =
                DevExpress.Xpo.XpoDefault
                .GetConnectionProvider(conn,
                                       DevExpress.Xpo.DB.AutoCreateOption.SchemaAlreadyExists);
            store = new DevExpress.Xpo.DB.DataCacheNode(new DevExpress.Xpo.DB.DataCacheRoot(store));
            var layer = new DevExpress.Xpo.ThreadSafeDataLayer(dict, store);
            Tsdl = layer;
        }
        public static int GetSizeForPartition(int collectionSize)
        {
            //would like to have 50 or so tasks per collection so
            int numtasks = 10;
            while (collectionSize / numtasks < 1)
                numtasks--;
            return collectionSize / numtasks;
        }
        public static object ProcessList(IEnumerable<Dictionary<string, string>> inlist, string srctable)
        {
            foreach (var row in inlist)
            { //create address, then subscribers    // just need the data from one of the rows since it is the same address for all the subs
                Guid newAddyGuid = CreateAddress(row, srctable);

                Console.WriteLine($"\n*******\n{System.Threading.Thread.CurrentThread.ManagedThreadId} | {System.Threading.Thread.CurrentThread.IsBackground} | {System.Threading.Thread.CurrentThread.Priority}");
                using (var uow = new UnitOfWork(Program.Tsdl))
                {
                    Guid newSubGuid = CreateSubscriber(uow, row, newAddyGuid, srctable);
                }
            }
            return null;


        }

        public static List<string> GetSpecialAddresses(List<Dictionary<string, string>> listdict)
        {
            List<string> ret = new List<string>();
            var distinctAddresses = listdict.Select(x => x["Address_1"] + x["City"] + x["State"]).Distinct();
            foreach (var VARIABLE in distinctAddresses)
            {
                if (listdict.Count(x => x["Address_1"] + x["City"] + x["State"] == VARIABLE) > 1)
                {
                    var curent = listdict.Where(x => x["Address_1"] + x["City"] + x["State"] == VARIABLE)
                        .Select(x => x["Address_1"]).First();
                    if (!ret.Contains(curent))
                    {
                        ret.Add(curent);

                    }
                }
            }
            return ret;
        }

        /*  
         *  "Type",
            "",
            "",
            "",
            " ",
            " ",
            " ",
            "ZIP__4",
            "Inactive",
            "LIS",
            "",
            "",
            "",
            "",
            "Deployment_Date",
            "Maximum_Download_Speed",
            "Maximum_Upload_Speed",
            "Census_Tract___Block",
            "SAC",
            "Tax_Area",
            "Plant_Area",
            "Serving_Area",
            "Service_Area",
            "Report_Area",
            "Work_Group",
            "PSAP",
            "E911_Community",
            "E911_County",
            "Change_By",
            "Change_Date",
            ""
            */
        static Guid CreateSubscriber(UnitOfWork uow, Dictionary<string, string> subrow, Guid newAddyGuid, string srctable)
        {
            Subscriber sub = new Subscriber(uow);
            try
            {
                Console.WriteLine($"Creating Subscriber..");
                if (!string.IsNullOrWhiteSpace(subrow["Address_2"]))
                    sub.LocationName = subrow["Address_2"];
                else if (!string.IsNullOrWhiteSpace(subrow["Plant_Key"]))
                    sub.LocationName = subrow["Plant_Key"];
                else sub.LocationName = subrow["Address_1"];
                if (uow.Query<LocationStatus>().Any(x => x.StatusName == "ACTIVE"))
                    sub.Status = uow.Query<LocationStatus>().FirstOrDefault(x => x.StatusName == "ACTIVE");
                else sub.Status = new LocationStatus(uow) { StatusName = "ACTIVE" };
                sub.SLID = srctable.First().ToString().ToUpper() + subrow["Addr_ID"];
                sub.ExternalSystemId = int.TryParse(subrow["Addr_ID"], out int eid) ? eid : 0;
                sub.SourceType = "MACC IMPORT:" + DateTime.Now.ToShortDateString().Replace(" ", "");
                sub.SourceTable = srctable;
                sub.Address = uow.GetObjectByKey<Address>(newAddyGuid);
                uow.CommitChanges();
                return sub.Oid;
            }
            catch (Exception ex)
            {
                NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"Exception in subscriber : {ex}");
                Console.WriteLine($"Exception in subscriber : {ex}");
                Console.ReadKey();
                return Guid.Empty;
            }
            finally
            {
                Console.WriteLine($"Created Subscriber {++CUR_SUBSCRIBER} of {TOTAL_SUB}   {((double)CUR_SUBSCRIBER / (double)TOTAL_SUB) * 100d} %  ");
            }
        }
        private static readonly object locker = new object();
        public static volatile int CUR_ADDRESS = 0, CUR_SUBSCRIBER = 0, TOTAL_SUB = 0, TOTAL_ADD = 0;
        public static async Task DoMultiAddys(List<string> addressesWithMultipleSubs, List<Dictionary<string, string>> listdict, string srctable)
        {
            await Task.Run(() =>
           {
               foreach (var addy in addressesWithMultipleSubs)
               {
                   lock (locker)
                   {
                       var subsforaddy = listdict.Where(x => x["Address_1"]/* + x["City"] + x["State"]*/ == addy);

                       //create address, then subscribers    // just need the data from one of the rows since it is the same address for all the subs
                       Guid newAddyGuid = CreateAddress(subsforaddy.ElementAt(0), srctable);

                       using (var uow = new UnitOfWork(Program.Tsdl))
                       {
                           int upper = subsforaddy.Count();
                           for (int i = upper - 1; i >= 0; i--)// in subsforaddy)
                           {
                               var subrow = subsforaddy.ElementAt(i);
                               Console.WriteLine("Process row...");
                               Guid newSubGuid = CreateSubscriber(uow, subrow, newAddyGuid, srctable);
                               Console.WriteLine($"*****\nlistdict count {listdict.Count}");
                               listdict.Remove(subrow);
                               Console.WriteLine($"listdict count {listdict.Count}\n*****");
                           }
                       }
                   }
               }
           });
        }
        const string subscriberTypeName = "Subscriber";
        private static Guid CreateAddress(Dictionary<string, string> row, string srctable)
        {
            Address ret = null;
            try
            {
                using (var uow = new UnitOfWork(Program.Tsdl))
                {
                    Console.WriteLine($"Creating Address.. ");
                    ret = new Address(uow);
                    ret.Street = row["Address_1"];
                    ret.City = row["City"];
                    ret.StateProvince = uow.Query<NewNetServices.Module.BusinessObjects.CableManagement.State>()
                        .FirstOrDefault(x => x.ShortName == row["State"] || x.LongName == row["State"]);
                    if (ret.StateProvince == null)
                    {
                        ret.StateProvince = new State(uow)
                        {
                            ShortName = row["State"],
                            LongName = row["State"],
                        };
                    }
                    ret.ZipPostal = row["ZIP"];//int.TryParse(row["ZIP"], out int zip) ? zip : 0;
                    ret.ExternalSystemId = int.TryParse(row["Addr_ID"], out int eid) ? eid : 0;
                    ret.Latitude = float.TryParse(row["Latitude"], out float lat) ? lat : 0f;
                    ret.Longitude = float.TryParse(row["Longitude"], out float lon) ? lon : 0f;
                    ret.CensusTract = float.TryParse(row["Census_Tract"], out float ct) ? ct : 0f;
                    ret.CensusBlock = float.TryParse(row["Census_Block"], out float cb) ? cb : 0f;
                    ret.SourceType = "MACC IMPORT:" + DateTime.Now.ToShortDateString().Replace(" ", "");
                    ret.SourceTable = srctable;
                    uow.CommitChanges();


                }
                return ret.Oid;
            }
            catch (Exception ex)
            {
                NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"Exception in adfdress : {ex}");
                Console.WriteLine($"Exception in adfdress : {ex}");
                Console.ReadKey();
                return Guid.Empty;
            }
            finally
            {
                Console.WriteLine($"Created Address {++CUR_ADDRESS} of {TOTAL_ADD}   {((double)CUR_ADDRESS / (double)TOTAL_ADD) * 100d} %  ");
            }
        }
        public static int GetSubAddCount(params string[] tables)
        {
            int ret = 0;
            using (UnitOfWork uow = new UnitOfWork(Program.Tsdl))
            {
                foreach (var table in tables)
                {
                    var res = uow.ExecuteQuery($"Select distinct {Program.columns[1]} from {table}");
                    TOTAL_ADD += res.ResultSet[0].Rows.Length;
                    res = uow.ExecuteQuery($"Select   {Program.columns[0]} from {table}");
                    TOTAL_SUB += res.ResultSet[0].Rows.Length;
                }
            }
            return TOTAL_ADD + TOTAL_SUB;
        }
    }
}
