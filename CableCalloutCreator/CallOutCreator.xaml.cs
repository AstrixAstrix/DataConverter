using DevExpress.Xpo;
using NewNetServices.Module;
using NewNetServices.Module.BusinessObjects.CableManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace CableCalloutCreator
{
    /// <summary>
    /// Interaction logic for CallOutCreator.xaml
    /// </summary>
    public partial class CallOutCreator : DevExpress.Xpf.Core.DXWindow
    {
        public static string TbCmsConnectionStringText = string.Empty;
        public static ThreadSafeDataLayer Tsdl;
        volatile float count = 0;
        Stopwatch sw = new Stopwatch();
        private double totalNumberOfRecords;
        Timer updateTimer;
        public CallOutCreator()
        {
            InitializeComponent();
        }
        string curstatus = "";
        private async Task Begin()
        {
            updateTimer = new Timer();
            updateTimer.Interval = 3000;
            updateTimer.Elapsed += (s,e) =>
            {

                Dispatcher.Invoke(() =>
                {
                    Console.WriteLine($"{count} callout completed ");
                    progressBar.Value = count;                      
                    progressBar.Content = curstatus;
                });
            };
            updateTimer.Start();
            ResetProgressBar();
            var t = Task.Factory.StartNew(() => StartCallouts());
            await Task.WhenAll(t);
        }
        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            CreateDataLayer();
            var t = Begin();
            Task.WhenAll(t);
        }
        private void UpdateTextEtc(string final)
        {
            count++; 
              curstatus = $"{count} cables so far in {sw.Elapsed}. CO: '{final}' {(TotalNumberOfRecords - count)} to go."; }

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
        private string GetDGroupName(CablePair cp)
        {
            return GetDGroupName(cp.DesignationPair);
        }
        private string GetDGroupName(DesignationPair pair)
        {
            return !string.IsNullOrWhiteSpace(pair?.DesignationGroup?.CableName)
                                               ? pair.DesignationGroup.CableName
                                               : "XD";
        }
        private void ResetProgressBar()
        {
            sw.Restart();
            progressBar.Value = 0;
            progressBar.Maximum = 100;
            progressBar.Content = string.Empty;
        }
        private async Task StartCallouts()
        {
            List<Guid> Cables;
            //assume 
            using (var uow = new UnitOfWork(Tsdl))
                Cables = uow.Query<PhysicalCable>().Select(x => x.Oid).ToList();
            TotalNumberOfRecords = Cables.Count();
            /// var plr =
            await Task.Run(() =>
            {
                Cables.ForEach((guid) =>
               {      //

                   using (var uow = new UnitOfWork(Tsdl))
                   {
                       var X = uow.GetObjectByKey<Cable>(guid);
                       try
                       {
                           string cablename = X.CableName, dgroup = string.Empty;// this is so that we know when the count) switched and we need a new line
                           var cpairs = X.CablePairs.OrderBy(x => x.PairNumber).ToArray();
                           //go through pairs and construct callout
                           List<string> calloutBuilder = new List<string>();
                           StringBuilder lineBuilder = new StringBuilder();
                           int n = 0;// to keep track of iteration count per line
                           for (int i = 0; i < cpairs.Length; i++)
                           {
                               var pair = cpairs[i];
                               if (n++ == 0)//beginning
                               {
                                   try
                                   { //<pairNum>:<DesignationGroupName>, <pairnumstart-pairnumnend>|
                                     //<pairNum>:XD, <pairnumstart-pairnumnend>|

                                       dgroup = GetDGroupName(pair);
                                       //only want on beginning of each line
                                       lineBuilder.Append($"{pair.PairNumber}: {dgroup}, {pair.PairNumber}-");
                                   }
                                   catch (Exception ex)
                                   {
                                       NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{ex}");
                                   }
                               }
                               else // if (i <= cpairs.Length - 1)//middle
                               {                   
                                   string nextdgroup = null;
                                   try
                                   {
                                       bool endline = false;
                                       if (i == cpairs.Length - 1)
                                           endline = true;
                                       else
                                       {
                                           // must have another row so check its dgroup
                                           //if these are different, then we need to start a new line and finish this line
                                           var nextpair = cpairs[i + 1];
                                             nextdgroup = GetDGroupName(nextpair);
                                           if (nextdgroup != dgroup)
                                               endline = true;
                                       }
                                       if (endline)//end
                                       {
                                           n = 0;
                                           lineBuilder.Append($"{pair.PairNumber}");
                                           //only add XD lines if they are on the end of the bunch, last pairs
                                           if (dgroup != "XD" || (i == cpairs.Length - 1))
                                               calloutBuilder.Add(lineBuilder.ToString());
                                           NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{lineBuilder}");
                                           lineBuilder.Clear();
                                       }
                                   }
                                   catch (Exception ex)
                                   {
                                       NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{ex}");
                                   }
                               }
                           }//end for cpairs
                            //clear call out for this cable and insert value
                           string final = string.Join("|", calloutBuilder);
                           X.CallOut1 = final;
                           uow.CommitChanges();//Async(new DevExpress.Xpo.AsyncCommitCallback(CompleteRead));

                           Dispatcher.BeginInvoke(new Action(() =>
                            {
                                UpdateTextEtc(final);
                            }),
                                                   null);
                       }
                       catch (Exception ex)
                       {
                           NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{ex}");
                       }
                   }//end using
               });//end foreach
            });
            //await Task.FromResult(plr);
        }

        public double TotalNumberOfRecords
        {
            get => this.totalNumberOfRecords;
            set
            {
                this.totalNumberOfRecords = value;
                Dispatcher.Invoke(() => progressBar.Maximum = value);
            }
        }
    }
}
