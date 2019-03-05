using DataConverterStep2.Classes;
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
// ReSharper disable CompareOfFloatsByEqualityOperator

namespace DataConverterStep2
{


    public partial class DataConvertII : DXWindow
    {
        private const int MAXHANDLES = 64;

        public static string TbCmsConnectionStringText = string.Empty;

        public static ThreadSafeDataLayer Tsdl;
        public static volatile int currentErrors = 0;
        public static volatile int currentSuccess = 0;
        UnitOfWork uow;
        private readonly object lockbox = new object();
        private Guid state = Guid.Empty;
        private string strstate = string.Empty;
        private Stopwatch sw = new Stopwatch();
        public static string tbOracleConnectionStringText = string.Empty;
        private double timeleft = 0.0;
        private long totalRecordsToProcess;
        public DataConvertII()
        {
            InitializeComponent();
            CreateDataLayer();
            uow = new UnitOfWork(Tsdl);
        }

        private event EventHandler<EventArgs> ProgressMade;
        private event EventHandler<EventArgs> WorkBeginning;
        private event EventHandler<EventArgs> WorkCompleted;


        void CreateDataLayer()
        {
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
        }
        private async Task HandleStep2<T>(string str) where T : BaseObjectDateTimeStamps, IEngineeringEntity
        {
            var lst = uow.Query<T>();
            using (var odw = new OracleDatabaseWorker(tbOracleConnectionStringText))
            {
                int i = 0;
                foreach (var item in lst)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(item.Handle) && long.TryParse(item.Handle, out long gid))
                        {
                            await odw.SetGUIDFromGIDOracleAsync(gid, item.Oid.ToString().ToUpper(), $"SP_NN_{typeof(T).Name}");
                            NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{typeof(T).Name} {++i}");   // await odw.SetGUIDFromGIDOracleAsync(gid, item.Oid.ToString().ToUpper(), $"SP_NN_{typeof(T).Name}");
                            currentSuccess++;
                            ProgressMade?.Invoke(str, null);
                        }
                    }
                    catch (Exception ex)
                    {
                        currentErrors++;
                        NewNetServices.Module.Core.StaticHelperMethods.WriteOut($" {i}\n{ex}");
                    }
                }
            }
        }
        async Task BeginWork()
        {
            WorkBeginning?.Invoke("Bullshit", null);
            TotalRecordsToProcess = uow.Query<Location>().Count() +
                uow.Query<Cable>().Count() +
                uow.Query<Conduit>().Count() +
                uow.Query<MiscFacility>().Count();

            if (!string.IsNullOrWhiteSpace(TbCmsConnectionStringText) &&
                !string.IsNullOrWhiteSpace(tbOracleConnectionStringText))
            {
                await Task.WhenAll(
                  HandleStep2<Location>("Locations"),
                  HandleStep2<Cable>("Cables"),
                  HandleStep2<Conduit>("Conduits"),
                  HandleStep2<MiscFacility>("Miscfacilitys"));

            }
            WorkCompleted?.Invoke("No More Bullshit", null);
        }

        private void btnConvert_Click(object sender, RoutedEventArgs e)
        {
            try
            {
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

            BtnConvert.IsEnabled = false;
            Task.Factory
                .StartNew(() =>
           {
               Task.WhenAll(BeginWork());
               Dispatcher.Invoke(() => BtnConvert.IsEnabled = true);
           });

        }


        private long TotalRecordsToProcess
        {
            get => totalRecordsToProcess;
            set
            {
                lock (lockbox)
                {
                    //in case i run more than one at a time, want t=he nums to be accurate

                    totalRecordsToProcess = value;
                }
                Dispatcher.Invoke(() => ProgressBar.Maximum = totalRecordsToProcess);
            }
        }


        #region EventHandlers

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

            using (var odw = new OracleDatabaseWorker(tbOracleConnectionStringText))
            {
                StaticHelperMethods.WriteOut("ok");
            }
            ProgressBar.Visibility = Visibility.Collapsed;
            WorkCompleted += OnWorkCompleted;
            WorkBeginning += OnWorkBeginning;
            ProgressMade += OnProgressMade;

        }

        private void OnProgressMade(object sender, EventArgs e)
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
        int currentIteration = 0;
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

    } //end class

}


