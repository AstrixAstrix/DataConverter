using DataConverter.Classes;
using DevExpress.ExpressApp;
//using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpf.Core;
using DevExpress.Xpo;
using NewNetServices.Module;
using NewNetServices.Module.BusinessObjects.CableManagement;
using NewNetServices.Module.BusinessObjects.Core;
using NewNetServices.Module.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static DataConverter.Classes.ImporterHelper;
// ReSharper disable CompareOfFloatsByEqualityOperator

namespace DataConverter
{
    ///need
    //    ///
    //Locations--CONDUIT             1356
    //POLE      6
    //Locations--CONDUIT_TAP  5
    //Locations GENERIC              31
    // ~~~~~~~~~~~~    Junctions--PEDISTAL             2193
    // ~~~~~~~~~~~~ Junctions--HANDHOLE         1125
    // ~~~~~~~~~~~~ Junctions--FIBER_PED          7
    // ~~~~~~~~~~~~ SUBSCRIBER       2102
    // ~~~~~~~~~~~~ Junctions X-BOX   2



    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : DXWindow
    {
        private const int MAXHANDLES = 64;

        public static string TbCmsConnectionStringText = string.Empty;

        public static ThreadSafeDataLayer Tsdl;
        public static volatile int currentErrors = 0;

        private double currentIteration = 0d;


        private int currentJobs;

        public static volatile int currentSuccess = 0;
        //string[] CityList = new string[] { "Marmon", "Epping", "Crosby", "Williston", "Tioga", "Ray" };
        private List<ImportedItem> importedItemsList = new List<ImportedItem>();

        private volatile List<ManualResetEvent> listofbools;
        private readonly object lockbox = new object();
        private Guid state = Guid.Empty;
        private string strstate = string.Empty;

        private Stopwatch sw = new Stopwatch();
        public static string tbOracleConnectionStringText = string.Empty;
        private double timeleft = 0.0;

        private long totalRecordsToProcess;
        private MyTuple<long, long> tuple = new MyTuple<long, long>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private event EventHandler<ImporterHelper.ProgressMadeEventArgs> ProgressMade;
        private event EventHandler<EventArgs> WorkBeginning;
        private event EventHandler<EventArgs> WorkCompleted;



        private void AddGridItem(ImportedItem e)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    lock (lockbox)
                    {
                        importedItemsList.Add(e);
                        GridImportedItems.RefreshData();
                        GridImportedItems.CurrentItem = GridImportedItems.GetRowByListIndex((GridImportedItems.ItemsSource as ICollection).Count -
                            1);
                    }
                }
                catch
                {
                }
            });
        }
        async Task BeginWork()
        {
           
            if (!string.IsNullOrWhiteSpace(TbCmsConnectionStringText) &&
                !string.IsNullOrWhiteSpace(tbOracleConnectionStringText))
            { 
               if(BDefaults) await CreateDefaults();

                if (Bwc)
                {
                    await Wirecenters();
                }
                 
                //junctions and subscribers 
                if (Bsub)
                {
                    await Addresses();
                    await Subscribers();
                }

                if (Bjunk)
                {
                    await Junctions();
                } //ftth


                if (Bolt)
                {

                    await Olts();
                }

                if (BoltPorts)
                {
                    await OltPorts();
                }
                if (BSplit)
                {
                    await Splitters();
                }
                if (BSplitPorts)
                {
                    await SplitterPorts();
                }
                if (Bcab)
                {
                    NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"Entering CablesWIthSpans");
                    NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"Entering CablesWIthSpans");
                    await CablesWithSpans();
                    NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"Entering Cables");                   
                    await Cables();
                }
                if (Bcon)
                {
                    await Conduits();
                }
                if (Bdgroup)
                {
                    await DesignationGroups();
                }
                if (Bcpair)
                {
                    await CablePairs();
                }
                if (Bdpair)
                {
                    await DesignationPairs();
                }
                if (Bcpdp)
                {
                    await DesignationPairsCablePairLink();
                }
                if (Bcall)
                {
                    await CableCallouts();
                }
                if (BOutDPairs) await OutDesignationPairs();
                if (BOltSplitDp) await OLT_Splitter_DP();
                if (BAssPl) await AssignmentsPrimaryLocations();
                if (BAssOlt) await AssignmentOlt();
                if (BAssDPair) await AssignmentDPairs();
                if (BAssSplitPort) await AssignmentSplitPort();
                // });
                //await Task.WhenAll(t);
            }
        }

        private void btnConvert_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                dbName = TbDb.Text;
                if (!wcTable.Contains(dbName))
                {
                    wcTable = dbName + "." + wcTable;
                    oltTable = dbName + "." + oltTable;
                    oltPortsTable = dbName + "." + oltPortsTable;
                    splitterTable = dbName + "." + splitterTable;
                    splitterPortsTable = dbName + "." + splitterPortsTable;
                    oltSplitterDpTable = dbName + "." + oltSplitterDpTable;
                    outdPairsTable = dbName + "." + outdPairsTable;
                    assignmentPrimlocTable = dbName + "." + assignmentPrimlocTable;
                    assignmentDPairsTable = dbName + "." + assignmentDPairsTable;
                    assignmentSplitPortTable = dbName + "." + assignmentSplitPortTable;
                    assignmentOltTable = dbName + "." + assignmentOltTable;
                    addressTable = dbName + "." + addressTable;
                    cabTable = dbName + "." + cabTable;
                    conTable = dbName + ".Conduits";
                    CABLEPAIR_Table = dbName + ".CablePairs";
                    dgroupTable = dbName + ".DesignationGroups";
                    cpdpTable = dbName + ".CABLEPAIRDESIGNATIONPAIR";
                    dpTable = dbName + ".DesignationPairs";
                    junkTable = dbName + ".JUNCTIONS";
                    subTable = dbName + ".Subscribers";
                }
                tbOracleConnectionStringText = TbOracleConnectionString.Text;
                TbCmsConnectionStringText = TbCmsConnectionString.Text;
                string conn = TbCmsConnectionStringText;
                //get thread safe klayer
                if (conn != null)
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
                if (Tsdl == null)
                {
                    //DevExpress.XtraEditors.XtraMessageBox.Show($"Couldn't create dl");
                    return;
                }
                //   MAINWorker.Connect(tsdl);

            }
            catch (Exception ex)
            {
                NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{ex}");
            }

            using (UnitOfWork uow = new UnitOfWork(Tsdl))
            {
                //using (UnitOfWork uow = MAINWorker.OptimisticLockingReadBehavior = LockingOption)
                //    uow = MAINWorker. LockingOption  STATE = (DefaultFields
                this.state = DefaultFields.GetBusinessObjectDefault<State>(uow,
                                                                           new List<Tuple<string, object>>()
                    {
                        new Tuple<string, object>("ShortName", State),
                        new Tuple<string, object>("LongName", (State == "IA" ? "Iowa" : State == "ND"? "North Dakota" :""))
                    })
                    .Oid;
            }
            BtnConvert.IsEnabled = false;
            Task.Factory
                .StartNew(() =>
           {
               Task.WhenAll(BeginWork());
               Dispatcher.Invoke(() => BtnConvert.IsEnabled = true);
           });
            //if (ToggleStep.IsChecked.HasValue && ToggleStep.IsChecked.Value)
            //{
            //    Dispatcher.Invoke(() => ProgressBar.Maximum = 0);

            //    WorkBeginning?.Invoke("GID GUID Matching..", null);
            //    HandleStep2<Location>();
            //    HandleStep2<Cable>();
            //}
            //else
            //{

        }







        //end importloaction
        private void ProcessBaseData(IEnumerable<List<Dictionary<string, string>>> dataSets)
        {


            //List<Task> tasks = new List<Task>();
            //var list_of_data_sets = dataSets.ToList();
            //List<Dictionary<string, string>> junkdata = list_of_data_sets[0];
            //List<Dictionary<string, string>> subdata = list_of_data_sets[1];
            //List<Dictionary<string, string>> cabdata = list_of_data_sets[2];
            //List<Dictionary<string, string>> condata = list_of_data_sets[3];



            //then conduits
            //totalRecordsToProcess = condata.Count; //subs and addresses
            //WorkBeginning?.Invoke(" Set Cables for conduit", EventArgs.Empty);
            SetConduitCables(null);
            //WorkCompleted?.Invoke("", EventArgs.Empty);

            //  totalRecordsToProcess = condata.Count; //subs and addresses
            //WorkBeginning?.Invoke(" Conduits", EventArgs.Empty);
            //await  Task.Factory.StartNew(() => CreateConduits(condata)));
            //WorkCompleted?.Invoke("", EventArgs.Empty);

            /// when locations are done, then do cables


            // }, cancellationToken,TaskCreationOptions.LongRunning );//end root task
        }



        private async Task ProcessData(string table, string[] cols, Func<List<Dictionary<string, string>>, List<List<Dictionary<string, string>>>> parter, Action<List<Dictionary<string, string>>> partProcessor)
        {
            if (string.IsNullOrEmpty(table))
            {
                throw new ArgumentException($"{nameof(table)} is null or empty.", nameof(table));
            }

            if (cols == null || cols.Length == 0)
            {
                throw new ArgumentException($"{nameof(cols)} is null or empty.", nameof(cols));
            }

            if (parter == null)
            {
                throw new ArgumentNullException(nameof(parter), $"{nameof(parter)} is null.");
            }

            if (partProcessor == null)
            {
                throw new ArgumentNullException(nameof(partProcessor), $"{nameof(partProcessor)} is null.");
            }

            List<Dictionary<string, string>> dataset = null;

            using (var odw = new OracleDatabaseWorker(tbOracleConnectionStringText))
            {
                dataset = odw.GetData(table, cols);
                TotalRecordsToProcess = dataset.Count;
                if (dataset.Count > 0)
                {
                    //  parter += mine;
                    List<List<Dictionary<string, string>>> datablocks = parter(dataset);
                    NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"datablocks : |{datablocks?.Count}| |{datablocks?[0]?.Count}|");
                    bool res = datablocks != null && datablocks.Count > 0;
                    res = res && await DoWorkInPartitions(datablocks, partProcessor);

                }
            }
        }
        //private void ProcessPairDataAsync(IEnumerable<List<Dictionary<string, string>>> dataSets)
        //{
        //    //junctions and subscribers 
        //    if (Bdgroup)
        //    {
        //        await DesignationGroups();
        //    }
        //    if (Bdpair)
        //    {
        //        await DesignationPairs();
        //    }
        //    if (Bcpair)
        //    {
        //        await CablePairs();
        //    }
        //    if (Bcpdp)
        //    {
        //        await DesignationPairsCablePairLink();
        //    }
        //    if (Bcall)
        //    {

        //    }
        //}




        private void SetConduitCables(List<Dictionary<string, string>> conData)
        {
            //  WorkBeginning?.Invoke("Create Cables and Conduits", EventArgs.Empty);
            if (Bcon)
            {
                //this is so that the xpobject type is created  on one thread
                using (UnitOfWork uow = new UnitOfWork(Tsdl))
                {
                    if (!uow.Query<Conduit>().Any())
                    {
                        Conduit dd = new Conduit(uow);
                        uow.CommitChanges();
                        uow.Delete(dd);
                        uow.CommitChanges();
                    }
                }
                //await Task.Run(() =>
                //{ //{
                object lockobj = new object();
                List<Task> myTasks = new List<Task>();
                var conDataParts = conData.Partition(conData.Count > 10 ? conData.Count / 10 : 1);
                conDataParts.ForEach((condata) =>
                {
                    myTasks.Add(Task.Factory
                        .StartNew(() =>
                        {
                            using (UnitOfWork uow = new UnitOfWork(Tsdl))
                            {  //do work
                                condata.ForEach((row) =>
                            {
                                Conduit con = uow.Query<Conduit>()
                            .FirstOrDefault(x => x.ExternalSystemId.ToString() == row["ID"] &&
                                x.SourceTable == conTable);

                                if (con != null)
                                {
                                    Cable cable = null;

                                    //look up cable we probably  imported already
                                    cable = !string.IsNullOrWhiteSpace(row["CABLE"])
                                    ? uow
                                        .Query<Cable>()
                                        .FirstOrDefault(x => x.ExternalSystemId.ToString() == row["CABLE"])
                                    : null;

                                    if (cable != null)
                                    {
                                        Location destination = cable.Destination;
                                        Location source = cable.Source;
                                        con.Source = source != null ? source : con.Source;
                                        con.Destination = destination != null ? destination : con.Destination;
                                        con.Cable = cable;
                                        cable.Conduit = con;
                                    }
                                }
                            });
                            }//end using
                        }));

                });//end for data parts
                Task.WaitAll(myTasks.ToArray());
            }//endifbcon
        }//endmethod

        private IEnumerable<ImportedItem> ItemsSource
        {
            get => (IEnumerable<ImportedItem>)GridImportedItems.ItemsSource;
            set => GridImportedItems.ItemsSource = value;
        }

        string State => dbName.Contains("KALONA") ? "IA" : "ND";

        private long TotalRecordsToProcess
        {
            get => totalRecordsToProcess;
            set
            {
                lock (lockbox)
                {
                    //in case i run more than one at a time, want t=he nums to be accurate
                    if (Reset)
                        totalRecordsToProcess = totalRecordsToProcess + value;
                    else
                        totalRecordsToProcess = value;
                }
                Dispatcher.Invoke(() => ProgressBar.Maximum = totalRecordsToProcess);
            }
        }

        public void DoWorkInPartitions(IEnumerable<List<Dictionary<string, string>>> partlist, Action<List<Dictionary<string, string>>> partaction)
        //where TPartList:IEnumerable<object>//,            where TList : IEnumerable<Dictionary<string,string>>
        // where TList:IEnumerable<object>
        {
            //this is so that the xpobject type is created  on one thread
            var partitionlist = partlist;

            object lockobj = new object();
            //list of each cable in this data set
            foreach (List<Dictionary<string, string>> dataset in partitionlist)
            {
                //have each cable's pairs handled on the same thread

                listofbools = new List<ManualResetEvent>();

                if (dataset != null)
                {
                    long its = 0;
                    //must keep waithandles under 64
                    while (listofbools.Count >= MAXHANDLES)
                    {
                        NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"Sleeping {++its}");
                        Task.Delay(1000);
                    }
                    var mre = new ManualResetEvent(false);

                    listofbools.Add(mre);
                    ///so i know when Hashtableread finishes
                    Action<object> myAction = (indata) =>
                {
                    try
                    {
                        partaction((List<Dictionary<string, string>>)indata);
                    }
                    finally
                    {
                        mre.Set();
                        listofbools.Remove(mre);
                    }
                };//endaction
                  //putin the threa pool 
                    ThreadPool.QueueUserWorkItem(x => myAction(dataset));
                }
            }

            WaitHandle.WaitAll(listofbools.ToArray());
            while (listofbools.Count > 0) Task.Delay(1000);

        }


        //public  Task<bool> DoWorkInPartitions<TSet, TActionInType>(TSet partlist,
        public async Task<bool> DoWorkInPartitions<TSet, TActionInType>(TSet partlist,
            Action<TActionInType>/*Action<List<Dictionary<string, string>>>*/ partaction)
                where TSet : IEnumerable<TActionInType>/*, IList<TActionInType>*/
                where TActionInType : IList /*, IList<TActionInType>*/
        {
            var partitionlist = partlist;
            //  ////DevExpress.XtraEditors.XtraMessageBox.Show("DoWorkInPatitions");
            //await    Task.Run(() =>
            //  {

            List<Task> tasklist = new List<Task>();
            object lockobj = new object();
            try
            {
                if (partitionlist == null || partitionlist.Count() == 0)
                // if (partitionlist == null || Count() == 0)
                {
                    return false;
                }
                List<TActionInType> pl = new List<TActionInType>();
                // pl = partitionlist.RandomSubset(partitionlist.Count()).ToList();
                if (Reverse)
                {
                    for (int i = partitionlist.Count() - 1; i >= 0; i--)
                    {
                        pl.Add(partitionlist.ElementAt(i));
                    }
                }
                else pl = partitionlist.ToList();
               
                //  else pl = partitionlist.ToList();
                //start task for each chunk of data
                //pl.Cast<TActionInType>().ForEach(
                //                  (dataset) =>
                int k = 0;
               pl.ForEach( 
                                  (dataset) =>
                {
                    NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"PARTITION ITERATION {++k} of {pl.Count} containing ");
                    if (dataset == null)
                    {
                        MessageBox.Show(" line924                   if(dataset==null)MessageBox.Show(");
                        return;
                    }

                    //lock(lockobj)
                    //{
                    tasklist.Add(Task.Factory
                        .StartNew(() =>
                   {
                       partaction((TActionInType)dataset);
                   },TaskCreationOptions.LongRunning));
                    //  } 
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{ex}");
                return false;
            }
            try
            {
                await Task.WhenAll(tasklist.ToArray());
                //  WaitHandle.WaitAll(listofbools.ToArray());
                // while (listofbools.Count > 0) Task.Delay(1000);
                return true;
            }
            catch (Exception ex)
            {
                NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{ex}");
                return false;
            }

        }

        #region Table_view Names
        private string addressTable = "AADDRESSES";
        private string assignmentDPairsTable = "ASSIGNMENT_DPAIR";
        private string assignmentOltTable = "ASSIGNMENT_OLT";
        private string assignmentPrimlocTable = "ASSIGNMENT_PRIMLOC";
        private string assignmentSplitPortTable = "ASSIGNMENT_SPLITPORT";
        private string cabTable = "CABLESWITHOUTSPANS";
        private string cabWithSpansTable = "CABLESWITHSPANS";
        private string conTable = "CONDUITs";
        private string cpdpTable = "CABLEPAIRDESIGNATIONPAIR";
        private string CABLEPAIR_Table = "CABLEPAIRS";
        private string dbName = "MSC_NCC";
        private string dgroupTable = "DESIGNATIONGROUPS";
        private string dpTable = "DESIGNATIONPAIRS";
        private string junkTable = "JUNCTIONS";
        private string oltPortsTable = "AOLTPORTS";
        private string oltSplitterDpTable = "AOLT_SPLITTER_DP";
        private string oltTable = "AOLTS";
        private string outdPairsTable = "aOUT_DPAIRS";
        private string splitterPortsTable = "ASPLITTERPORTS";
        private string splitterTable = "ASPLITTERS";
        private string subTable = "SUBSCRIBERS";
        private string wcTable = "AWIRECENTERS";
        #endregion
        #region Table_view Columns
        private string[] assignmentDPairsCols = { "ID", "DESIGNATEDPAIR" };
        private string[] assignmentOltCols = { "ID", "OLTPORTID" };
        private string[] assignmentPrimlocCols = { "ID", "ASSIGNMENTCLASS", "ASSIGNMENTPORT", "STATUS", "EFFECTIVEDATE", "CIRCUITID", "SUBSCRIBERID" };
        private string[] assignmentSplitPortCols = { "ID", "SPLITTERID" };
        private string[] cabColumns = new string[] {
            "FORC", "CABLEID", "COMMENTS", "CABLESTATUS", "CABLELENGTH", "CABLEROUTE", "WORKORDERID", "DROPCABLE",
            "CABLETYPE", "CABLECLASS", "CABLESIZE", "SOURCELOCATIONID", "DESTINATIONLOCATIONID", "DESCRIPTION",
            "INSTALLDATE"
        };
        private string[] conColumns = new string[] { "ID", "STATUS", "LENGTH", "TYPE", "CODE", "MEDIA", "WORKORDER", "CABLE", "INSTALLDATE" };
        private string[] cpColumns = new string[] { "ID", "NUM", "STATUS", "CABLE" };
        private string[] cpdpColumns = new string[] { "PAIRID", "COUNT", "STATUS", "CABLEID", "TU_ID", "LOGICALCOUNTID", "DESIGNATIONGROUPID", "LOGICALCOUNT", "DESIGNATIONGROUPNAME" };        //       List<Dictionary<string, string>> cabdata = new List<Dictionary<string, string>>();                
        private string[] dgroupColumns = new string[] { "DGID", "CLASS", "DGNAME", "STATUS", "CODE", "SOURCE", "MAXCOUNT" };
        private string[] dpColumns = new string[] { "ID", "COUNT", "DGROUP" };
        private string[] junctionCols = new string[] { "OBJECTID", "APID", "STATUS", "NAME", "WO", "TYPE", "CITY", "INSTALLDATE_NEW", "ENTITYID", "ENTITYTYPE", "ACCESSPOINTTYPE", "ACCESSPOINTID", "REFERENCENAME", "REFERENCETYPECODE", "ENTITYNAME", "REGIONCODE", "SUBTYPE", "WOID", "ROUTE" };
        private string[] oltCols = { "OLT_ID", "OLT_CODE", "SUB_RACK_CODE", "RACK_CODE", "CARD_NUM", "PORT_POSITION", "CR_LOGICAL_COUNT_ID", "CR_SITE_ID", "EQUIPOBJ_REF_ID", "OLTNAME" };
        private string[] oltPortsCols = { "OLTID", "OLTPORTID", "OLTPORTNUM" };
        private string[] oltSplitterDpCols = { "SPLITTERID", "INCABLEID", "INCABLENAME", "INCABLEOBJECTREF", "INCOUNTID", "INCOUNT", "OLTID" };
        private string[] outdPairsCols = { "SPLITTERPORTID", "DESIGNATIONPAIRID" };
        private string[] splitterCols = { "ID", "NAME", "OBJ_REF_ID", "EQ_HOLDER_ID", "CR_SITE_ID", "STATUS", "CREATION_DATE" };
        private string[] splitterPortsCols = { "ID", "NAME", "STATUS", "CR_EQUIPMENT_ID" };
        private string[] subCols = new string[] {"ADDYID", "STREET", "CITY", "STATE", "FLEXTEXT", "CODE", "SUBID"};
        private string[] addCols = new string[] {"ADDYID", "STREET", "CITY", "STATE", "FLEXTEXT", "CODE", "SUBID"};
        private string[] wcCols = new string[] {"REGION_ID", "REGION_CNL", "REGION_NAME", "CO_ID", "CO_CODE", "CO_NAME", "DESCRIPTION", "ID"};
  
        #endregion


        #region EventHandlers

        private void Decrement(object sender, EventArgs e)
        {
            lock (lockbox) currentJobs--;
        }

        private void DXWindow_Loaded(object sender, RoutedEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (send, X) =>
            {
                Console.WriteLine($"{X}\n******************************************************************************************************************\n");
                System.Diagnostics.Debug.WriteLineIf(true, $"{X}");
                NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{X}");
            };
            tbOracleConnectionStringText = TbOracleConnectionString.Text;
            TbCmsConnectionStringText = TbCmsConnectionString.Text;
            //AppDomain.CurrentDomain.FirstChanceException += (sdr, eventArgs) =>
            //{                lock(lockbox)
            //    {
            //        Debug.WriteLine(eventArgs.Exception.ToString());
            //        NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{eventArgs.Exception}");
            //    }
            //};
            DevExpress.Xpo.DB.ConnectionProviderSql.MaxDeadLockTryCount = 5;

            ItemsSource = importedItemsList;
            using (var odw = new OracleDatabaseWorker(tbOracleConnectionStringText))
            {
                StaticHelperMethods.WriteOut("ok");
            }
            ProgressBar.Visibility = Visibility.Collapsed;
            WorkCompleted += Decrement;
            WorkBeginning += Increment;
            WorkCompleted += OnWorkCompleted;
            WorkBeginning += OnWorkBeginning;
            ProgressMade += OnProgressMade;

        }

        private void Increment(object sender, EventArgs e)
        {
            lock (lockbox)
            {
                currentJobs++;
            }
        }
        private void OnProgressMade(object sender, ProgressMadeEventArgs e)
        {


            try
            {
                Dispatcher.Invoke(() =>
                {
                    TbSuccessful.Text = string.Empty + currentSuccess;
                    TbErrors.Text = string.Empty + currentErrors;

                    string send = "";

                    if (sender != null)
                    {
                        TbStep.Text = (string)sender;
                        send = (string)sender;
                    }
                    int elspd = sw.Elapsed.Milliseconds;
                    ProgressBar.Value = currentIteration++;
                    ProgressBar.Content = $"{ send}\tRecord: {currentIteration}\t\nSuccess: {currentSuccess}    Errors: {currentErrors}\t";

                    try
                    {
                        if (true)
                        {
                            double milliseconds = (double)sw.ElapsedMilliseconds;
                            double max = ProgressBar.Maximum;
                            double percent = (currentIteration / max) * 100;
                            double millisper = (milliseconds / currentIteration);
                            double remaining = (ProgressBar.Maximum - currentIteration);
                            double msleft = millisper * remaining;

                            //NewNetServices.Module.Core.StaticHelperMethods.WriteOut( " double "+max+" = "+progressBar.Maximum+ 
                            //"\n"+percent + " = (" + currentIteration+ "/ " + max + ") * 100;"+
                            //"\n" + millisper+" = (" + milliseconds+" / " + currentIteration + 
                            //"\n"+ remaining+" = (" + progressBar.Maximum + " - " + currentIteration+
                            // "\n" +  msleft+" = " + millisper + " * " + remaining+"; ");


                            timeleft = msleft / 1000d;

                            TimeR.Text = $"{sw.Elapsed.ToString().Substring(0, sw.Elapsed.ToString().IndexOf('.', 6))}\t {percent.ToString("F2")}%\t  About { TimeSpan.FromSeconds((timeleft)).ToString(@"hh\:mm\:ss")} Remaining.";
                            ProgressBar.Content += "\n" + TimeR.Text;
                        }
                        if (e.I != null && e.I.ImportStatus != null && e.I.ImportStatus.Contains("Exception"))
                        {
                            AddGridItem(e.I);
                        }
                    }
                    catch (Exception ex)
                    {
                        StaticHelperMethods.WriteOut($"{ex}");
                    }
                });
            }
            catch (Exception ex)
            {
                StaticHelperMethods.WriteOut($"{ex}");
            }
        }
        private void OnWorkBeginning(object sender, EventArgs e)
        {
            currentIteration = 0;
            currentSuccess = currentErrors = 0;

            Dispatcher.Invoke(() =>
            {
                TbStep.Text = sender?.ToString();
                tbOracleConnectionStringText = TbOracleConnectionString.Text;
                TbCmsConnectionStringText = TbCmsConnectionString.Text;
                sw.Restart();

                BtnConvert.IsEnabled = false;
                ProgressBar.Value = currentIteration;
                ProgressBar.IsEnabled = true;
                ProgressBar.Visibility = Visibility.Visible;
            });
        }

        private void OnWorkCompleted(object sender, EventArgs e)
        {
            //DevExpress.XtraEditors.XtraMessageBox.Show($"WORKCOM<PLETEED");
            AddGridItem(new ImportedItem() { ImportStatus = $"Success: {currentSuccess}\tErr: {currentErrors}", Type = sender.ToString() });
            sw.Stop();
            Dispatcher.Invoke(() =>
            {
                TbStep.Text = "Done!";
                BtnConvert.IsEnabled = true;
                if (ProgressBar.Value != ProgressBar.Maximum)
                {
                    StaticHelperMethods.WriteOut($"WTF progressBar.Value{ProgressBar.Value}" +
                        $"  progressBar.Maximum{ProgressBar.Maximum}");
                }
            });

            //OracleDatabaseWorker.SlapGuidIntoOracle(oracleconstr, masterUpdatestr);
            ////StaticHelperMethods.WriteOut($"{masterUpdatestr}");
            //masterUpdatestr = "";
            ////StaticHelperMethods.WriteOut($"OnPerformWorkCompleted ");// iteration {iteration}");
        }

        #endregion



        #region Bools

        public bool IsBusy => currentJobs > 0;
        public bool Reset => currentJobs > 1;
        public bool Reverse
        {
            get
            {
                bool ret = false;
                Dispatcher.Invoke(() => ret = Chkreverse.IsChecked.HasValue && Chkreverse.IsChecked.Value);
                return ret;
            }
        }
        private bool Bcab
        {
            get
            {
                bool ret = false; Dispatcher.Invoke(() => ret = Chkcab.IsChecked.HasValue && Chkcab.IsChecked.Value);
                return ret;
            }
        }
        private bool BAssPl
        {
            get
            {
                bool ret = false; Dispatcher.Invoke(() => ret = ChkAssPL.IsChecked.HasValue && ChkAssPL.IsChecked.Value);
                return ret;
            }
        }
        private bool BAssDPair
        {
            get
            {
                bool ret = false; Dispatcher.Invoke(() => ret = ChkAssDPair.IsChecked.HasValue && ChkAssDPair.IsChecked.Value);
                return ret;
            }
        }
        private bool BAssOlt
        {
            get
            {
                bool ret = false; Dispatcher.Invoke(() => ret = ChkAssOLT.IsChecked.HasValue && ChkAssOLT.IsChecked.Value);
                return ret;
            }
        }
        private bool BAssSplitPort
        {
            get
            {
                bool ret = false; Dispatcher.Invoke(() => ret = ChkAssSplitPort.IsChecked.HasValue && ChkAssSplitPort.IsChecked.Value);
                return ret;
            }
        }
        private bool Bcall
        {
            get
            {
                bool ret = false; Dispatcher.Invoke(() => ret = Chkcall.IsChecked.HasValue && Chkcall.IsChecked.Value);
                return ret;
            }
        } //"Junctions" Margin="5"></CheckBox>

        private bool Bcon
        {
            get
            {
                bool ret = false; Dispatcher.Invoke(() => ret = Chkcon.IsChecked.HasValue && this.Chkcon.IsChecked.Value);
                return ret;
            }
        }

        private bool Bcpair
        {
            get
            {
                bool ret = false; Dispatcher.Invoke(() => ret = Chkcpair.IsChecked.HasValue && Chkcpair.IsChecked.Value);
                return ret;
            }
        }  //"CPairs" Margin="5"></CheckBox>

        private bool Bcpdp
        {
            get
            {
                bool ret = false; Dispatcher.Invoke(() => ret = Chkcpdp.IsChecked.HasValue && Chkcpdp.IsChecked.Value);
                return ret;
            }
        }  //"CPDP" Margin="5"></CheckBox>

        private bool Bdgroup
        {
            get
            {
                bool ret = false; Dispatcher.Invoke(() => ret = this.Chkdgroup.IsChecked.HasValue && Chkdgroup.IsChecked.Value);
                return ret;
            }
        }  //"DGroups" Margin="5"></CheckBox>

        private bool Bdpair
        {
            get
            {
                bool ret = false; Dispatcher.Invoke(() => ret = Chkdpair.IsChecked.HasValue && Chkdpair.IsChecked.Value);
                return ret;
            }
        } //"DGroupsDPairs" Margin="5"></CheckBox>

        private bool Bjunk
        {
            get
            {
                bool ret = false; Dispatcher.Invoke(() => ret = Chkjunk.IsChecked.HasValue && Chkjunk.IsChecked.Value);
                return ret;
            }
        } //"Junctions" Margin="5"></CheckBox>

        private bool Bolt
        {
            get
            {
                bool ret = false; Dispatcher.Invoke(() => ret = ChkOlt.IsChecked.HasValue && this.ChkOlt.IsChecked.Value);
                return ret;
            }
        }
        private bool BoltPorts
        {
            get
            {
                bool ret = false; Dispatcher.Invoke(() => ret = ChkOltPorts.IsChecked.HasValue && this.ChkOltPorts.IsChecked.Value);
                return ret;
            }
        }
        private bool BOltSplitDp
        {
            get
            {
                bool ret = false;
                Dispatcher.Invoke(() => ret = ChkOltSplitDp.IsChecked.HasValue && ChkOltSplitDp.IsChecked.Value);
                return ret;
            }
        }
        private bool BOutDPairs
        {
            get
            {
                bool ret = false;
                Dispatcher.Invoke(() => ret = ChkOutDPairs.IsChecked.HasValue && ChkOutDPairs.IsChecked.Value);
                return ret;
            }
        }
        private bool BSplit
        {
            get
            {
                bool ret = false;
                Dispatcher.Invoke(() => ret = ChkSplit.IsChecked.HasValue && ChkSplit.IsChecked.Value);
                return ret;
            }
        }

        private bool BSplitPorts
        {
            get
            {
                bool ret = false;
                Dispatcher.Invoke(() => ret = ChkSplitPorts.IsChecked.HasValue && ChkSplitPorts.IsChecked.Value);
                return ret;
            }
        }

        private bool Bsub
        {
            get
            {
                bool ret = false; Dispatcher.Invoke(() => ret = Chksub.IsChecked.HasValue && Chksub.IsChecked.Value);
                return ret;
            }
        } //"Subs" Margin="5"></CheckBox>

        private bool Bwc
        {
            get
            {
                bool ret = false; Dispatcher.Invoke(() => ret = Chkwc.IsChecked.HasValue && Chkwc.IsChecked.Value); return ret;
            }
        }  private bool BDefaults
        {
            get
            {
                bool ret = false; Dispatcher.Invoke(() => ret = ChkDefaults.IsChecked.HasValue && ChkDefaults.IsChecked.Value); return ret;
            }
        }
        #endregion
        public static int Skip = 0;
        private void BtnSkip_Click(object sender, RoutedEventArgs e)
        {
            Skip++;
        }

        private void BtnUnSkip_Click(object sender, RoutedEventArgs e)
        {
            Skip--;
        }

        private void BtnAll_Click(object sender, RoutedEventArgs e)
        {
            Chkwc.IsChecked = true;
            Chkwc.IsChecked = true;
            ChkOlt.IsChecked = true;

            ChkOlt.IsChecked = true;
            ChkOltPorts.IsChecked = true;

            ChkOltPorts.IsChecked = true;
            ChkSplit.IsChecked = true;

            ChkSplit.IsChecked = true;
            ChkSplitPorts.IsChecked = true;

            ChkSplitPorts.IsChecked = true;
            ChkOltSplitDp.IsChecked = true;

            ChkOltSplitDp.IsChecked = true;
            ChkOutDPairs.IsChecked = true;

            ChkOutDPairs.IsChecked = true;
            ChkAssPL.IsChecked = true;

            ChkAssPL.IsChecked = true;
            ChkAssDPair.IsChecked = true;

            ChkAssDPair.IsChecked = true;
            ChkAssSplitPort.IsChecked = true;

            ChkAssSplitPort.IsChecked = true;
            ChkAssOLT.IsChecked = true;

            ChkAssOLT.IsChecked = true;
            Chkjunk.IsChecked = true;

            Chkjunk.IsChecked = true;
            Chksub.IsChecked = true;

            Chksub.IsChecked = true;
            Chkcab.IsChecked = true;

            Chkcab.IsChecked = true;
            Chkcon.IsChecked = true;

            Chkcon.IsChecked = true;
            Chkcpair.IsChecked = true;

            Chkcpair.IsChecked = true;
            Chkdgroup.IsChecked = true;

            Chkdgroup.IsChecked = true;
            Chkdpair.IsChecked = true;

            Chkdpair.IsChecked = true;
            Chkcpdp.IsChecked = true;

            Chkcpdp.IsChecked = true;
            Chkcall.IsChecked = true;

            Chkcall.IsChecked = true;
            Chkreverse.IsChecked = true;

            Chkreverse.IsChecked = true;
    
        }

        private void BtnNone_Click(object sender, RoutedEventArgs e)
        {
            Chkwc.IsChecked = false;
            Chkwc.IsChecked = false;
            ChkOlt.IsChecked = false;

            ChkOlt.IsChecked = false;
            ChkOltPorts.IsChecked = false;

            ChkOltPorts.IsChecked = false;
            ChkSplit.IsChecked = false;

            ChkSplit.IsChecked = false;
            ChkSplitPorts.IsChecked = false;

            ChkSplitPorts.IsChecked = false;
            ChkOltSplitDp.IsChecked = false;

            ChkOltSplitDp.IsChecked = false;
            ChkOutDPairs.IsChecked = false;

            ChkOutDPairs.IsChecked = false;
            ChkAssPL.IsChecked = false;

            ChkAssPL.IsChecked = false;
            ChkAssDPair.IsChecked = false;

            ChkAssDPair.IsChecked = false;
            ChkAssSplitPort.IsChecked = false;

            ChkAssSplitPort.IsChecked = false;
            ChkAssOLT.IsChecked = false;

            ChkAssOLT.IsChecked = false;
            Chkjunk.IsChecked = false;

            Chkjunk.IsChecked = false;
            Chksub.IsChecked = false;

            Chksub.IsChecked = false;
            Chkcab.IsChecked = false;

            Chkcab.IsChecked = false;
            Chkcon.IsChecked = false;

            Chkcon.IsChecked = false;
            Chkcpair.IsChecked = false;

            Chkcpair.IsChecked = false;
            Chkdgroup.IsChecked = false;

            Chkdgroup.IsChecked = false;
            Chkdpair.IsChecked = false;

            Chkdpair.IsChecked = false;
            Chkcpdp.IsChecked = false;

            Chkcpdp.IsChecked = false;
            Chkcall.IsChecked = false;

            Chkcall.IsChecked = false;
            Chkreverse.IsChecked = false;

            Chkreverse.IsChecked = false;
        }
        

        private void BtnInvert_Click(object sender, RoutedEventArgs e)
        {
            if (Chkwc.IsChecked.HasValue)
            {
                Chkwc.IsChecked = !Chkwc.IsChecked.Value;
            }
            if (ChkOlt.IsChecked.HasValue)
            {
                ChkOlt.IsChecked = !ChkOlt.IsChecked.Value;
            }
            if (ChkOltPorts.IsChecked.HasValue)
            {
                ChkOltPorts.IsChecked = !ChkOltPorts.IsChecked.Value;
            }
            if (ChkSplit.IsChecked.HasValue)
            {
                ChkSplit.IsChecked = !ChkSplit.IsChecked.Value;
            }
            if (ChkSplitPorts.IsChecked.HasValue)
            {
                ChkSplitPorts.IsChecked = !ChkSplitPorts.IsChecked.Value;
            }
            if (ChkOltSplitDp.IsChecked.HasValue)
            {
                ChkOltSplitDp.IsChecked = !ChkOltSplitDp.IsChecked.Value;
            }
            if (ChkOutDPairs.IsChecked.HasValue)
            {
                ChkOutDPairs.IsChecked = !ChkOutDPairs.IsChecked.Value;
            }
            if (ChkAssPL.IsChecked.HasValue)
            {
                ChkAssPL.IsChecked = !ChkAssPL.IsChecked.Value;
            }
            if (ChkAssDPair.IsChecked.HasValue)
            {
                ChkAssDPair.IsChecked = !ChkAssDPair.IsChecked.Value;
            }
            if (ChkAssSplitPort.IsChecked.HasValue)
            {
                ChkAssSplitPort.IsChecked = !ChkAssSplitPort.IsChecked.Value;
            }
            if (ChkAssOLT.IsChecked.HasValue)
            {
                ChkAssOLT.IsChecked = !ChkAssOLT.IsChecked.Value;
            }
            if (Chkjunk.IsChecked.HasValue)
            {
                Chkjunk.IsChecked = !Chkjunk.IsChecked.Value;
            }
            if (Chksub.IsChecked.HasValue)
            {
                Chksub.IsChecked = !Chksub.IsChecked.Value;
            }
            if (Chkcab.IsChecked.HasValue)
            {
                Chkcab.IsChecked = !Chkcab.IsChecked.Value;
            }
            if (Chkcon.IsChecked.HasValue)
            {
                Chkcon.IsChecked = !Chkcon.IsChecked.Value;
            }
            if (Chkcpair.IsChecked.HasValue)
            {
                Chkcpair.IsChecked = !Chkcpair.IsChecked.Value;
            }
            if (Chkdgroup.IsChecked.HasValue)
            {
                Chkdgroup.IsChecked = !Chkdgroup.IsChecked.Value;
            }
            if (Chkdpair.IsChecked.HasValue)
            {
                Chkdpair.IsChecked = !Chkdpair.IsChecked.Value;
            }
            if (Chkcpdp.IsChecked.HasValue)
            {
                Chkcpdp.IsChecked = !Chkcpdp.IsChecked.Value;
            }
            if (Chkcall.IsChecked.HasValue)
            {
                Chkcall.IsChecked = !Chkcall.IsChecked.Value;
            }
            if (Chkreverse.IsChecked.HasValue)
            {
                Chkreverse.IsChecked = !Chkreverse.IsChecked.Value;
            }
        }
    } //end class

    public class ImportedItem
    {
        public string Guid
        {
            get;
            set;
        }
        public string Id
        {
            get;
            set;
        }
        public string ImportStatus
        {
            get;
            set;
        }
        public string RecordStatus
        {
            get;
            set;
        }
        public string SourceTable
        {
            get;
            set;
        }
        public string SubType
        {
            get;
            set;
        }
        public string Type
        {
            get;
            set;
        }
    }
}


