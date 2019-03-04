using DevExpress.Xpo;
using NewNetServices.Module;
using NewNetServices.Module.BusinessObjects.CableManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CableCalloutCreator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string TbCmsConnectionStringText = string.Empty;
        public static ThreadSafeDataLayer Tsdl;
        private UnitOfWork uow = null;
        private double totalNumberOfRecords;

        public double TotalNumberOfRecords
        {
            get => this.totalNumberOfRecords;
            set
            {
                this.totalNumberOfRecords = value;
                Dispatcher.Invoke(() => progressBar.Maximum = value);
            }
        }

        public MainWindow()
        {
            InitializeComponent(); 
                    }
        
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
        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            CreateDataLayer();
            uow = new UnitOfWork(Tsdl);
            var t = Begin();
            Task.WhenAll(t);
        }
        private async Task StartCallouts()
        {

            //assume 
            var Cables = uow.Query<PhysicalCable>();
            Parallel.ForEach(Cables,
                             (X) =>
            {



            });
        }
        private async Task Begin()
        {
            ResetProgressBar();
          await  StartCallouts();
        }

        private void ResetProgressBar()
        {
            progressBar.Value = 0;
            progressBar.Maximum = 100;
            progressBar.Content = "";
        }

    }
}
