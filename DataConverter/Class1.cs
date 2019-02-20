

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
using System.ComponentModel;
using System.Diagnostics;
using DataConverter;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpf.Core;
using DevExpress.Xpo;
using DevExpress.XtraEditors;
using NewNetServices.Module.BusinessObjects.CableManagement;
using NewNetServices.Module.BusinessObjects.Core;
using DataConverter.Classes;
using DataConverter.XPO;
using static DataConverter.Classes.ImporterHelper;
using DevExpress.ExpressApp;
using NewNetServices.Module.Core;
using Task = System.Threading.Tasks.Task;
using System.Reflection;
using System.IO;
using State = NewNetServices.Module.BusinessObjects.CableManagement.State;

namespace DataConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : DXWindow
    {


        public const string UNK = "UNKNOWN";

        private Guid olttypeOid;

        private async Task AddressDefaults()
        {
            List<string> states = null;
            using (var odw = new OracleDatabaseWorker(tbOracleConnectionStringText))
            {
                states = odw.GetListForDataColumn(@"select distinct state from subscribers where state is not null"); /// will be size|type|class if size is null 0, if type null then 'null'
            }


            //status
            var del = new MyDelegate((lst) =>
            {
                using (var uow = new UnitOfWork(Tsdl))
                {
                    foreach (var str in lst
                    .Where(s => s != ""))
                    {
                        if (!uow.Query<State>().Any(x => x.ShortName == str || x.LongName == str))
                        {
                            State cs = new State(uow) { ShortName = str, LongName = str };
                            uow.CommitChanges();
                        }
                    }
                    return true;
                }
            });
            IAsyncResult stateres = del.BeginInvoke(states, null, null);


            await Task.FromResult(stateres);
        }

        private async Task AssignmentDefaults()
        {

            using (var uow = new UnitOfWork(Tsdl))
            {
                if (!uow.Query<AssignmentType>().Any(x => x.TypeName == UNK))
                {
                    var at = new AssignmentType(uow) { TypeName = UNK, TypeDescription = UNK };
                    uow.CommitChanges();
                }
            }
                using (var odw = new OracleDatabaseWorker(tbOracleConnectionStringText))
            {
                //just good olfd status for now
                List<string> statuses = odw.GetDistinctDataColumn(assignmentPrimlocTable, "STATUS");
                List<string> Classes = odw.GetDistinctDataColumn(assignmentPrimlocTable, "ASSIGNMENTCLASS");

                statuses.Add(GlobalSystemSettings.AssignmentStatusActive);
                //class
                var del = new MyDelegate((lst) =>
                {
                    using (var uow = new UnitOfWork(Tsdl))
                    {
                        foreach (var str in lst.Where(s =>
                            s != "" && !uow.Query<AssignmentStatus>().Select(x => x.StatusName).Contains(s)))
                        {
                            AssignmentStatus cs = new AssignmentStatus(uow) { StatusName = str };
                            uow.CommitChanges();
                        }
                    }
                    return true;
                });
                IAsyncResult sres = del.BeginInvoke(statuses, null, null);

                del = new MyDelegate((lst) =>
               {
                   using (var uow = new UnitOfWork(Tsdl))
                   {
                       foreach (var str in lst.Where(s =>
                           s != "" && !uow.Query<AssignmentClass>().Select(x => x.Class).Contains(s)))
                       {
                           AssignmentClass cs = new AssignmentClass(uow) { Class = str };
                           uow.CommitChanges();
                       }
                   }
                   return true;
               });
                IAsyncResult cres = del.BeginInvoke(Classes, null, null);

                await Task.FromResult(sres);
                await Task.FromResult(cres);

            }
        }

        private async Task CablePairDefaults()
        {
            using (var odw = new OracleDatabaseWorker(tbOracleConnectionStringText))
            {
                //just good olfd status for now
                List<string> pairstatus = odw.GetDistinctDataColumn(CABLEPAIR_Table, "STATUS");

                //class
                var del = new MyDelegate((lst) =>
                {
                    using (var uow = new UnitOfWork(Tsdl))
                    {
                        foreach (var str in lst.Where(s =>
                            s != "" && !uow.Query<CablePairStatus>().Select(x => x.StatusName).Contains(s)))
                        {
                            CablePairStatus cs = new CablePairStatus(uow) { StatusName = str };
                            uow.CommitChanges();
                        }
                    }
                    return true;
                });
                IAsyncResult classres = del.BeginInvoke(pairstatus, null, null);

                await Task.FromResult(pairstatus);
            }
        }



        private async Task DPairDefaults()
        {
        }

        private async Task Junction_LocationDefaults()
        {
            using (var odw = new OracleDatabaseWorker(tbOracleConnectionStringText))
            {

                List<string> boundaries = odw.GetDistinctDataColumn(junkTable, "CITY");
                List<string> types = odw.GetDistinctDataColumn(junkTable, "SUBTYPE");
                types.Add("JUNCTION")
;
                List<string> statuses = odw.GetDistinctDataColumn(junkTable, "STATUS");
                statuses.Add(UNK);
                List<string> routes = odw.GetDistinctDataColumn(junkTable, "ROUTE");

                //unknjown junction
                using (var uow = new UnitOfWork(Tsdl))
                {
                    var gss = GlobalSystemSettings.GetInstanceFromDatabase(uow);
                    var junc = new Junction(uow)
                    {
                        LocationName = UNK,
                        Status = gss.DefaultLocationStatusActive
                    };
                    uow.CommitChanges();
                }
                //routes
                var del = new MyDelegate((lst) =>
             {
                 using (var uow = new UnitOfWork(Tsdl))
                 {

                     foreach (var str in lst
                         .Where(s => s != ""))
                     {
                         if (!uow.Query<Route>().Any(x => x.Name == str))

                         {
                             Route cs = new Route(uow) { Name = str };
                             uow.CommitChanges();
                         }
                     }
                 }
                 return true;
             });
                IAsyncResult routeres = del.BeginInvoke(routes, null, null);
                 
                await Task.FromResult(routes); 

                //boundaries
                del = new MyDelegate((lst) =>
               {
                   using (var uow = new UnitOfWork(Tsdl))
                   {
                       foreach (var str in lst
                         .Where(s => s != ""))
                       {
                            if (!string.IsNullOrWhiteSpace(str)&&!uow.Query<Boundary>().Any(x => x.Name== str))

                           {
                               Boundary cs = new Boundary(uow) { Name = str  };
                               uow.CommitChanges();
                           }
                       }
                   }
                   return true;
               });
                IAsyncResult boundres = del.BeginInvoke(boundaries, null, null);
                await Task.FromResult(boundres); 
                //type
                del = new MyDelegate((lst) =>
               {
                   using (var uow = new UnitOfWork(Tsdl))
                   {
                       foreach (var str in lst
                         .Where(s => s != ""))
                       {
                           if (!uow.Query<JunctionType>().Any(x => x.TypeName == str))

                           {
                               JunctionType cs = new JunctionType(uow) { TypeName = str, TypeDescription = str };
                               uow.CommitChanges();
                           }
                       }
                   }
                   return true;
               });
                IAsyncResult typeres = del.BeginInvoke(types, null, null);
                await Task.FromResult(typeres); 
                //class
                del = new MyDelegate((lst) =>
               {
                   using (var uow = new UnitOfWork(Tsdl))
                   {
                       foreach (var str in lst
                          .Where(s => s != ""))
                       {
                           if (!uow.Query<LocationStatus>().Any(x => x.StatusName == str))

                           {
                               LocationStatus cs = new LocationStatus(uow) { StatusName = str };
                               uow.CommitChanges();
                           }
                       }
                   }
                   return true;
               });
                IAsyncResult statusres = del.BeginInvoke(statuses, null, null); 
                await Task.FromResult(statusres);
            }

        }

        private async Task OLTDefaults()
        {

            var tasks = new List<Task>();

            using (var odw = new OracleDatabaseWorker(tbOracleConnectionStringText))
            {
                //olt eqtype
                var del = new MyDelegate((lst) =>
                {
                    using (var uow = new UnitOfWork(Tsdl))
                    {
                        var gss = GlobalSystemSettings.GetInstanceFromDatabase(uow);
                        ////////// 
                        if (gss.OLT_EquipmentType == null)
                        {
                            var ot = new EquipmentType(uow) { TypeName = "OLT" };
                            uow.Save(ot);
                            uow.CommitChanges();
                            gss.OLT_EquipmentType = ot;

                            uow.CommitChanges();
                        }
                    }
                    return true;
                });


                IAsyncResult sizeres = del.BeginInvoke(null, null, null);


                ////status
                //del = new MyDelegate((lst) =>
                //{
                //    using (var uow = new UnitOfWork(Tsdl))
                //    {
                //        foreach (var str in lst
                //            .Where(s => s != "" && !uow.Query<LocationStatus>().Select(x => x.StatusName).Contains(s))
                //        )
                //        {
                //            LocationStatus cs = new LocationStatus(uow) { StatusName = str };
                //            uow.CommitChanges();
                //        }
                //    }
                //    return true;
                //});
                //IAsyncResult statusres = del.BeginInvoke(oltstatuses, null, null);



                await Task.FromResult(sizeres);
                //await Task.FromResult(statusres);



            }
        }
        private async Task OLTPortDefaults()
        {
            using (var odw = new OracleDatabaseWorker(tbOracleConnectionStringText))
            {

                //olt eqtype
                using (var puow = new UnitOfWork(Tsdl))
                {
                    using (var uow = new UnitOfWork(Tsdl))
                    {
                        var gss = GlobalSystemSettings.GetInstanceFromDatabase(uow);
                        ////////// 
                        if (gss.OLT_PortType == null)
                        {
                            var pt = new PortType(uow) { TypeName = "OLTPORT" };
                            gss.OLT_PortType = pt;

                            uow.CommitChanges();
                        }
                        olttypeOid = gss
                            .OLT_PortType.Oid;
                    }

                    await Task.Delay(1);
                }


            }//using ORACLE odw
        }

        private async Task PoleDefaults()
        {
            using (var uow = new UnitOfWork(Tsdl))
            {   //do work
                if(!uow.Query<PoleType>().Any(x=>x.TypeName=="POLE"))
                {
                    var pt = new PoleType(uow)
                    {
                        TypeName = "POLE",
                        TypeDescription = "POLE",
                    };
                    uow.CommitChanges();
                }
            }
        }

        private async Task SplitterDefaults()
        {

            var tasks = new List<Task>();

            using (var odw = new OracleDatabaseWorker(tbOracleConnectionStringText))
            {
                List<string> wirecentersids = odw.GetDistinctDataColumn(splitterTable, "CR_SITE_ID");
                ;
                List<string> splitterstatuses = odw.GetDistinctDataColumn(splitterTable, "STATUS");
                splitterstatuses.Add("UNKNOWN");

                //splitter eqtype
                var del = new MyDelegate((lst) =>
                {
                    using (var uow = new UnitOfWork(Tsdl))
                    {
                        var gss = GlobalSystemSettings.GetInstanceFromDatabase(uow);
                        ////////// 
                        if (gss.SplitterEquipmentType == null)
                        {
                            var eq = new EquipmentType(uow) { TypeName = "SPLITTER" };
                            uow.Save(eq);
                            uow.CommitChanges();
                            gss.SplitterEquipmentType = eq;
                            uow.CommitChanges();
                        }
                        splittertypeOid = gss.SplitterEquipmentType.Oid;
                    }
                    return true;
                });


                IAsyncResult sizeres = del.BeginInvoke(null, null, null);


                await Task.FromResult(sizeres);
                //status
                del = new MyDelegate((lst) =>
                {
                    using (var uow = new UnitOfWork(Tsdl))
                    {
                        foreach (var str in lst
                            .Where(s => s != "" && !uow.Query<LocationStatus>().Select(x => x.StatusName).Contains(s))
                        )
                        {
                            LocationStatus cs = new LocationStatus(uow) { StatusName = str };
                            uow.CommitChanges();
                        }
                    }
                    return true;
                });
                IAsyncResult statusres = del.BeginInvoke(splitterstatuses, null, null);

                await Task.FromResult(statusres);
                //wirecenter
                del = new MyDelegate((lst) =>
                {
                    using (var uow = new UnitOfWork(Tsdl))
                    {
                        foreach (var str in lst
                            .Where(s => s != "" && !uow.Query<Wirecenter>().Select(x => x.ExternalSystemId.ToString()).Contains(s))
                        )
                        {
                            Wirecenter cs = new Wirecenter(uow) { ExternalSystemId = int.TryParse(str, out int eid) ? eid : 0, LocationName = str, CLLI = str, SourceTable = splitterTable };
                            uow.CommitChanges();
                        }

                    }

                    return true;
                });
                IAsyncResult wcres = del.BeginInvoke(wirecentersids, null, null);

                await Task.FromResult(wcres);




            }
        }

        private async Task SplitterPortDefaults()
        {
            using (var odw = new OracleDatabaseWorker(tbOracleConnectionStringText))
            {
                List<string> splitterportstatuses = await Task.FromResult(odw.GetDistinctDataColumn(splitterPortsTable, "STATUS"));
                //splitter eqtype
                using (var puow = new UnitOfWork(Tsdl))
                {
                    using (var uow = new UnitOfWork(Tsdl))
                    {
                        var gss = GlobalSystemSettings.GetInstanceFromDatabase(uow);
                        ////////// 
                        if (gss.SplitterPortType == null)
                        {
                            gss.SplitterPortType =
                                 new PortType(uow) { TypeName = "SPLITTERPORT" };
                            uow.CommitChanges();
                        }
                        splittertypeOid = gss
                            .SplitterPortType.Oid;
                    }

                    //status
                    var del = new MyDelegate((lst) =>
                   {
                       using (var uow = new UnitOfWork(Tsdl))
                       {
                           foreach (var str in lst
                               .Where(s => s != "" && !uow.Query<LocationStatus>().Select(x => x.StatusName)
                                               .Contains(s))
                           )
                           {
                               if (!uow.Query<LocationStatus>().Any(x => x.StatusName == str))
                               {
                                   LocationStatus cs = new LocationStatus(uow) { StatusName = str };
                                   uow.CommitChanges();
                               }
                           }
                           return true;
                       }
                   });
                    IAsyncResult statusres = del.BeginInvoke(splitterportstatuses, null, null);


                }


            }//using ORACLE odw
        }

        private async Task SubscriberDefaults()
        {
            List<string> distinctstates = null;
            using (var odw = new OracleDatabaseWorker(tbOracleConnectionStringText))
            {
                distinctstates =await Task.FromResult( odw.GetDistinctDataColumn(subTable, "STATE"));
            }
            List<Task> ts = new List<Task>();
            var del = new MyDelegate((lst) =>
           {
               using (var uow = new UnitOfWork(Tsdl))
               {
                   if (!uow.Query<SubscriberType>().Any(x => x.TypeName == "SUBSCRIBER"))
                   {
                       var st = new SubscriberType(uow) { TypeName = "SUBSCRIBER" };
                       uow.CommitChanges();
                   }
                   //do states 
                   foreach (var st in lst)
                   {
                       if (!uow.Query<NewNetServices.Module.BusinessObjects.CableManagement.State>().Any(x => x.ShortName == st || x.LongName == st))
                       {
                           var stat = new NewNetServices.Module.BusinessObjects.CableManagement.State(uow) { ShortName = st, LongName = st };
                           uow.CommitChanges();
                       }
                   }
               }
               return true;
           });
            var stateres = del.BeginInvoke(distinctstates, null, null);
        }

        private async Task WirecenterDefaults()
        {

            try
            {
                // Make
                //wirecentere types
                using (var odw = new OracleDatabaseWorker(tbOracleConnectionStringText))
                {
                    List<string> wctypes = odw.GetListForDataColumn("select distinct description from awirecenters where description is not null");
                    wctypes.Add(UNK);

                    //type
                    var del = new MyDelegate((lst) =>
                    {
                        using (var uow = new UnitOfWork(Tsdl))
                        {
                            foreach (var str in lst.Where(s => s != ""))
                            {
                                if (!uow.Query<WirecenterType>().Any(x => x.TypeName == str))
                                {
                                    WirecenterType cs = new WirecenterType(uow) { TypeName = str, TypeDescription = str };
                                    uow.CommitChanges();
                                }
                            }
                        }
                        return true;
                    });
                    IAsyncResult typeres = del.BeginInvoke(wctypes, null, null);

                    using (var uow = new UnitOfWork(Tsdl))
                    {
                        Wirecenter wc = uow.Query<Wirecenter>().FirstOrDefault(x => x.LocationName == UNK || x.CLLI == UNK);
                        if (wc == null)
                        {

                            wc = new Wirecenter(uow)
                            {
                                LocationName = UNK,
                                CLLI = UNK,
                                Type = uow.Query<WirecenterType>().FirstOrDefault(x => x.TypeName == UNK)
                            };

                            uow.CommitChanges();

                        }
                    }
                    await Task.FromResult(typeres);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("" + ex);
            }

        }

        private async Task WorkOrderDefaults()
        {
            using (var odw = new OracleDatabaseWorker(tbOracleConnectionStringText))
            {
                //just good olfd status for now
                List<string> wos = odw.GetDistinctDataColumn("MSC_NCC.CR_PROJECT", "ID");
                var del = new MyDelegate((lst) =>
                             {
                                 using (var uow = new UnitOfWork(Tsdl))
                                 {
                                     foreach (var str in lst.Where(s =>
                                         s != "" && !uow.Query<WorkOrder>().Select(x => x.ExternalSystemId.ToString()).Contains(s)))
                                     {
                                         if (int.TryParse(str, out int id))
                                         {
                                             WorkOrder cs = new WorkOrder(uow) { ExternalSystemId = id, OrderNumber = id.ToString() };
                                             uow.CommitChanges();
                                         }
                                     }
                                 }
                                 return true;
                             });
                IAsyncResult classres = del.BeginInvoke(wos, null, null);

                await Task.FromResult(classres);
            }
        }

        public async Task CableDefaults()
        {

            using (var odw = new OracleDatabaseWorker(tbOracleConnectionStringText))
            {
                List<string> cableclass = odw.GetListForDataColumn(@"select distinct class from (
                                                                                Select  case when cable_type=1 then 'COPPER' else case when cable_type=2 then 'FIBER' else 'UNKNOWN' end end as Class
                                                                                from cable_Type where cable_type is not null order by cable_type)");
                List<string> cablesizes = odw.GetListForDataColumn("Select distinct capacity from cable_type where capacity is not null order by capacity");
                List<string> wiredimensions = odw.GetListForDataColumn("Select diameter from cable_type where diameter is not null order by diameter");
                List<string> cablemedia = new List<string>() { "Media" };
                List<string> cabletypes = odw.GetListForDataColumn("Select distinct std_code from cable_Type where std_code is not null order by std_code"); ;
                List<string> cablestatuses = odw.GetListForDataColumn(@"select distinct CABLESTATUS from(
                                                                                            select CASE cast(cs.Status as varchar(50)) WHEN '5' 
                                                                                            THEN 'Active' 
                                                                                            when '6' then 'UNKNOWN'
                                                                                            else cast(cs.Status as varchar(50)) end   AS CABLESTATUS
                                                                                            from cable_seg cs)
                                                                                                                                ");

                List<string> classrelationships = odw.GetListForDataColumn(@"select  Capacity||'|'||std_code||'|'||class  as capsizeclass from(
                Select DISTINCT  case when Capacity is null then 0 else Capacity end as CAPACITY,case when  std_code is null then  'null' else std_code end as std_code, case when cable_type=1 then 'COPPER' else case when cable_type=2 then 'FIBER' else 'UNKNOWN' end end as CLASS
                from cable_Type where capacity is not null order by capacity)"); /// will be size|type|class if size is null 0, if type null then 'null'
                List<string> cableroutes = odw.GetDistinctDataColumn(cabTable, "CABLEROUTE");
                //extras 
                //class
                var del = new MyDelegate((lst) =>
                {
                    using (var uow = new UnitOfWork(Tsdl))
                    {
                        foreach (var str in lst.Where(s => s != ""))
                        {
                            if (!uow.Query<CableClass>().Any(x => x.TypeName == str))
                            {
                                CableClass cs = new CableClass(uow) { TypeName = str, TypeDescription = str };
                                uow.CommitChanges();
                            }
                        }
                    }
                    return true;
                });
                IAsyncResult classres = del.BeginInvoke(cableclass, null, null);
                //size
                del = new MyDelegate((lst) =>
                {
                    using (var uow = new UnitOfWork(Tsdl))
                    {
                        foreach (var str in lst.Where(s => s != ""))
                        {
                            if (!uow.Query<CableSize>().Any(x => "" + x.Count == str))
                            {
                                CableSize cs = new CableSize(uow) { Count = int.Parse(str), Code = str };
                                uow.CommitChanges();
                            }
                        }
                    }
                    return true;
                });
                IAsyncResult sizeres = del.BeginInvoke(cablesizes, null, null);

                //wiredimensions
                del = new MyDelegate((lst) =>
                {
                    using (var uow = new UnitOfWork(Tsdl))
                    {
                        foreach (var str in lst.Where(s => s != ""))
                        {
                            if (!uow.Query<WireDimension>().Any(x => "" + x.Gauge == str))
                            {
                                WireDimension cs = new WireDimension(uow) { Gauge = str };
                                uow.CommitChanges();
                            }
                        }
                    }
                    return true;
                });
                IAsyncResult wdres = del.BeginInvoke(wiredimensions, null, null);

                //type
                del = new MyDelegate((lst) =>
                {
                    using (var uow = new UnitOfWork(Tsdl))
                    {
                        foreach (var _str in lst.Where(s => s != ""))
                        {
                            string str = ImporterHelper.GetAllUntilNumber(_str);
                            if (!uow.Query<CableType>().Any(x => "" + x.TypeName == str))
                            {
                                CableType cs = new CableType(uow) { TypeName = str, TypeDescription = str };
                                uow.CommitChanges();
                            }
                        }
                    }
                    return true;
                });
                IAsyncResult typeres = del.BeginInvoke(cabletypes, null, null);

                //routes
                del = new MyDelegate((lst) =>
                {
                    using (var uow = new UnitOfWork(Tsdl))
                    {
                        foreach (var str in lst
                        .Where(s => s != ""))
                        {
                            if (!uow.Query<Route>().Any(x => x.Name == str))
                            {
                                Route cs = new Route(uow) { Name = str };
                                uow.CommitChanges();
                            }
                        }
                    }
                    return true;
                });
                IAsyncResult routeres = del.BeginInvoke(cableroutes, null, null);

                //status
                del = new MyDelegate((lst) =>
                {
                    using (var uow = new UnitOfWork(Tsdl))
                    {
                        foreach (var str in lst
                        .Where(s => s != ""))
                        {
                            if (!uow.Query<CableStatus>().Any(x => x.StatusName == str))
                            {
                                CableStatus cs = new CableStatus(uow) { StatusName = str };
                                uow.CommitChanges();
                            }
                        }
                        return true;
                    }
                });
                IAsyncResult statusres = del.BeginInvoke(cablestatuses, null, null);


                //media
                del = new MyDelegate((lst) =>
                {
                    using (var uow = new UnitOfWork(Tsdl))
                    {
                        foreach (var str in lst
                        .Where(s => s != ""))
                        {
                            if (!uow.Query<CableMedia>().Any(x => x.TypeName == str))

                            {
                                CableMedia cs = new CableMedia(uow) { TypeName = str, TypeDescription = str };
                                uow.CommitChanges();
                            }
                        }
                        return true;
                    }
                });
                IAsyncResult mediares = del.BeginInvoke(cablemedia, null, null);
                //have to wait for this stuff to finish before the next

                await Task.FromResult(classres);
                await Task.FromResult(sizeres);
                await Task.FromResult(typeres);
                await Task.FromResult(mediares);
                //size typ class rel
                del = new MyDelegate((lst) =>
                {
                    using (var uow = new UnitOfWork(Tsdl))
                    {
                        CableClass cclass = null;
                        CableMedia cmedia = uow.Query<CableMedia>().FirstOrDefault(x => x.TypeName == "Media");
                        foreach (var str in lst
                        .Where(s => s != ""))
                        {
                            //pipdelimited str
                            var arr = str.Split('|');
                            cclass = uow.Query<CableClass>().FirstOrDefault(x => x.TypeName == arr[2]);
                            CableType ctype = uow.Query<CableType>().FirstOrDefault(x => x.TypeName == arr[1]);
                            CableSize csize = uow.Query<CableSize>().FirstOrDefault(x => x.Count.ToString() == arr[0]);
                            if (cclass != null)
                            {
                                if (ctype != null && !cclass.CableTypes.Contains(ctype)) cclass.CableTypes.Add(ctype);
                                if (csize != null && !cclass.CableSizes.Contains(csize)) cclass.CableSizes.Add(csize);
                                uow.CommitChanges();
                            }
                        }

                        if (cmedia != null && !cclass.CableMedia.Contains(cmedia)) cclass.CableMedia.Add(cmedia);
                        return true;
                    }
                });
                IAsyncResult relres = del.BeginInvoke(classrelationships, null, null);

                await Task.FromResult(statusres);
                await Task.FromResult(relres);
            }
        }



        //"DGID", "CLASS", "DGNAME", "STATUS", "CODE", "SOURCE", "MAXCOUNT"
        public async Task ConduitDefaults()
        {
            var tasks = new List<Task>();

            using (var odw = new OracleDatabaseWorker(tbOracleConnectionStringText))
            {
                List<string> conduitsizes = new List<string>() { "1" };// odw.GetDistinctDataColumn(cabTable, "CABLESIZE");
                List<string> conduitmedia = new List<string>() { "Media" };
                List<string> conduittypes = odw.GetDistinctDataColumn(conTable, "TYPE"); ;
                List<string> conduitstatuses = odw.GetDistinctDataColumn(conTable, "STATUS");


                //size
                var del = new MyDelegate((lst) =>
                {
                    using (var uow = new UnitOfWork(Tsdl))
                    {
                        foreach (var str in lst.Where(s => s != "" && !uow.Query<ConduitSize>().Select(x => x.Count.ToString()).Contains(s)))
                        {
                            ConduitSize cs = new ConduitSize(uow) { Count = int.Parse(str), Code = str };
                            uow.CommitChanges();
                        }
                    }
                    return true;
                });
                IAsyncResult sizeres = del.BeginInvoke(conduitsizes, null, null);

                //type
                del = new MyDelegate((lst) =>
                {
                    using (var uow = new UnitOfWork(Tsdl))
                    {
                        foreach (var str in lst.Where(s => s != "" && !uow.Query<ConduitType>().Select(x => x.TypeName).Contains(s))
                        )
                        {
                            ConduitType cs = new ConduitType(uow) { TypeName = str };
                            uow.CommitChanges();
                        }
                    }
                    return true;
                });
                IAsyncResult typeres = del.BeginInvoke(conduittypes, null, null);

                //status
                del = new MyDelegate((lst) =>
                {
                    using (var uow = new UnitOfWork(Tsdl))
                    {
                        foreach (var str in lst
                        .Where(s => s != "" && !uow.Query<ConduitStatus>().Select(x => x.StatusName).Contains(s))
                        )
                        {
                            ConduitStatus cs = new ConduitStatus(uow) { StatusName = str };
                            uow.CommitChanges();
                        }
                    }
                    return true;
                });
                IAsyncResult statusres = del.BeginInvoke(conduitstatuses, null, null);


                //media
                del = new MyDelegate((lst) =>
                {
                    using (var uow = new UnitOfWork(Tsdl))
                    {
                        if (!uow.Query<ConduitClass>().Any(x => x.TypeName == "Class"))
                        {

                            ConduitClass cc = new ConduitClass(uow) { TypeName = "Class" };

                            uow.CommitChanges();
                        }
                        foreach (var str in lst
                        .Where(s => s != "" && !uow.Query<ConduitMedia>().Select(x => x.TypeName).Contains(s))
                        )
                        {
                            ConduitMedia cs = new ConduitMedia(uow) { TypeName = str };
                            uow.CommitChanges();
                        }
                    }
                    return true;
                });
                IAsyncResult mediares = del.BeginInvoke(conduitmedia, null, null);


                await Task.FromResult(sizeres);
                await Task.FromResult(typeres);
                await Task.FromResult(statusres);
                await Task.FromResult(mediares);
            }
        }
        public async Task CreateDefaults()
        {
            //DevExpress.XtraEditors.XtraMessageBox.Show($"Start");
            List<Task> tasks = new List<System.Threading.Tasks.Task>
            {
                CableDefaults(),
                DesignationGroupDefaults(),
                ConduitDefaults(),
                SplitterDefaults(),
                SplitterPortDefaults(),
                OLTDefaults(),
                OLTPortDefaults(),
                Junction_LocationDefaults(),
                SubscriberDefaults(),
                WirecenterDefaults(),
                WorkOrderDefaults(),
                CablePairDefaults(),
                DPairDefaults(),
                PoleDefaults(),
                AssignmentDefaults(),
                AddressDefaults()
            };
            await Task.WhenAll(tasks);
            //DevExpress.XtraEditors.XtraMessageBox.Show($"End");
            //location stuff

        }
        public async Task DesignationGroupDefaults()
        {

            using (var odw = new OracleDatabaseWorker(tbOracleConnectionStringText))
            {
                //List<string> cableclass = odw.GetListForDataColumn(@"select distinct class from designationgroups");
                //List<string> cablesizes = odw.GetListForDataColumn(@"SELECT distinct mc.MaxCount  from designationgroups");
                List<string> cablestatuses = new List<string>() { "Active" };

                List<string> classrelationships = odw.GetListForDataColumn(@"select distinct classsize from
(SELECT   class ||'|'|| MaxCount as classsize from designationgroups where maxcount is not null order by maxcount)"); /// will be size|type|class if size is null 0, if type null then 'null'


                //status
                var del = new MyDelegate((lst) =>
                 {
                     using (var uow = new UnitOfWork(Tsdl))
                     {
                         foreach (var str in lst
                         .Where(s => s != ""))
                         {
                             if (!uow.Query<CableStatus>().Any(x => x.StatusName == str))
                             {
                                 CableStatus cs = new CableStatus(uow) { StatusName = str };
                                 uow.CommitChanges();
                             }
                         }
                         return true;
                     }
                 });
                IAsyncResult statusres = del.BeginInvoke(cablestatuses, null, null);


                //size typ class rel
                del = new MyDelegate((lst) =>
                {
                    using (var uow = new UnitOfWork(Tsdl))
                    {
                        CableClass cclass = null;
                        CableMedia cmedia = uow.Query<CableMedia>().FirstOrDefault(x => x.TypeName == "Media");
                        foreach (var str in lst
                        .Where(s => s != ""))
                        {
                            //pipdelimited str
                            var arr = str.Split('|');
                            //making type the same as the class per andyrew
                            cclass = uow.Query<CableClass>().FirstOrDefault(x => x.TypeName == arr[0]);
                            CableType ctype = uow.Query<CableType>().FirstOrDefault(x => x.TypeName == arr[0]);
                            //create class
                            if (cclass == null)
                            {
                                cclass = new CableClass(uow) { TypeName = arr[0], TypeDescription = arr[0] };
                                if (cmedia != null) cclass.CableMedia.Add(cmedia);
                                uow.CommitChanges();
                            }
                            //create type
                            if (ctype == null)
                            {
                                ctype = new CableType(uow) { TypeName = arr[0], TypeDescription = arr[0] };
                                cclass.CableTypes.Add(ctype);
                                uow.CommitChanges();
                            }

                            //have to do his check so that we associate desgroups sizes and classes separate from regular cables
                            CableSize csize = cclass.CableSizes.FirstOrDefault(x => x.Count.ToString() == arr[1]);
                            //create size
                            if (csize == null && int.TryParse(arr[1], out int count))
                            {
                                csize = new CableSize(uow) { Count = count, Code = "" + count };
                                cclass.CableSizes.Add(csize);
                                uow.CommitChanges();
                            }
                            uow.CommitChanges();
                        }

                        return true;
                    }
                });
                IAsyncResult relres = del.BeginInvoke(classrelationships, null, null);

                await Task.FromResult(statusres);
                await Task.FromResult(relres);
            }
        }


    }
}
