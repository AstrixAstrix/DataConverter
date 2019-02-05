using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddressSubscriberImport
{
    class Program
    {
        public static string[] columns =
         {
            "Type",
            "Address_1",
            "Address_2",
            "Plant_Key",
            "City",
            "State",
            "ZIP",
            "ZIP__4",
            "Inactive",
            "LIS",
            "Latitude",
            "Longitude",
            "Census_Tract",
            "Census_Block",
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
            "Addr_ID"
        };
        static string connectionString;

        public static ThreadSafeDataLayer Tsdl;
        static void Main(string[] args)
        {
            Console.WriteLine("Starting..");
            SelectedData result = null;
            Helper.SetThreadSafeDataLayer(out Tsdl, ConnectionString);

            int subcount = Helper.GetSubAddCount("ADDRESSDATA", "ADDRESSDATA2");
            var t = Task.Factory
                .StartNew(async () =>
            {
                var tasks = new List<Task>();
                using (UnitOfWork uow = new UnitOfWork(Tsdl))
                {
                    Console.WriteLine("Getting Data..COLTON");
                    result = Helper.GetData(uow, "AddressData", columns);
                }
                if (result != null)
                {
                    var listdict = Helper.GetDictionaryListFromData(result, columns);
                    Console.WriteLine("OK");
                    Console.WriteLine("Getting list of addresses with multiple...");
                    List<string> addressesWithMultipleSubs = Helper.GetSpecialAddresses(listdict);
                    addressesWithMultipleSubs.ForEach((x) => Console.WriteLine(x));
                    Console.WriteLine("OK");
                    Console.WriteLine("Handling addresses with multiple subs...");
                    await Helper.DoMultiAddys(addressesWithMultipleSubs, listdict, "COLTON");
                    Console.WriteLine("OK");
                    Console.WriteLine("Partitioning...");
                    var partsList = new List<List<Dictionary<string, string>>>();
                    listdict.Partition(Helper.GetSizeForPartition(listdict.Count))
                        .ForEach((x) => partsList.Add(x.ToList()));
                    Console.WriteLine($"{partsList.Count} parts of {partsList[0].Count} records.\n Beginning tasks...");
                    //kick of tasks in parallel
                    int i = 1;
                    Parallel.ForEach(partsList,
                                     (inlist) =>
                    {
                        Console.WriteLine($"Start Processing chunk { i}");
                        tasks.Add(Task.Factory.StartNew(() => Helper.ProcessList(inlist, "COLTON")));
                        Console.WriteLine($"End Processing chunk {i++}");
                    });

                    await Task.WhenAll(tasks.ToArray());
                    Console.WriteLine("Finished importing COLTON");
                    await Task.Delay(5000);
                    Console.WriteLine("Begin MONITOR");
                    using (UnitOfWork uow = new UnitOfWork(Tsdl))
                    {
                        Console.WriteLine("Getting Data..MONITOR");
                        result = Helper.GetData(uow, "AddressData2", columns);
                    }
                    if (result != null)
                    {
                        listdict = Helper.GetDictionaryListFromData(result, columns);
                        Console.WriteLine("OK");
                        Console.WriteLine("Getting list of addresses with multiple...");
                        addressesWithMultipleSubs = Helper.GetSpecialAddresses(listdict);
                        addressesWithMultipleSubs.ForEach((x) => Console.WriteLine(x));
                        Console.WriteLine("OK");
                        Console.WriteLine("Handling addresses with multiple subs...");
                        await Helper.DoMultiAddys(addressesWithMultipleSubs, listdict, "MONITOR");
                        Console.WriteLine("OK");
                        Console.WriteLine("Partitioning...");
                        partsList = new List<List<Dictionary<string, string>>>();
                        listdict.Partition(Helper.GetSizeForPartition(listdict.Count))
                            .ForEach((x) => partsList.Add(x.ToList()));
                        Console.WriteLine($"{partsList.Count} parts of {partsList[0].Count} records.\n Beginning tasks...");
                        //kick of tasks in parallel
                        i = 1;
                        Parallel.ForEach(partsList,
                                         (inlist) =>
                        {
                            Console.WriteLine($"Start Processing chunk { i}");
                            tasks.Add(Task.Factory.StartNew(() => Helper.ProcessList(inlist, "MONITOR")));
                            Console.WriteLine($"End Processing chunk {i++}");
                        });

                        await Task.WhenAll(tasks.ToArray());

                        Console.WriteLine("Finished importing MONITOR");
                        await Task.Delay(5000);
                    }
                }
            });//end outer task
            t.Wait();
            Console.WriteLine("DONE");
            Console.ReadKey();
        }

        public static string ConnectionString
        {
            get
            {
                if (connectionString != null) return connectionString;
                else
                {
                    connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
                    return connectionString;
                }
            }
            set { connectionString = value; }
        }
    }
}
