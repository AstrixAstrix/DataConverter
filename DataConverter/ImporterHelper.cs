using DataConverter.XPO;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Xpo;
using DevExpress.ExpressApp.Xpo;
using NewNetServices.Module.BusinessObjects.CableManagement;
using NewNetServices.Module.BusinessObjects.Core;
using NewNetServices.Module.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp.DC;

namespace DataConverter.Classes
{
    public static class ImporterHelper
    {
        public static void ProcessCablePair(UnitOfWork uow,
                                            Dictionary<string, string> row,
                                            string cpTable,
                                            ref int successfulCpairs,
                                            ref int errorsCpairs,
                                            EventHandler<ProgressMadeEventArgs> progressMade)
        {
            if (!uow.Query<PhysicalPair>().Any(x => x.ExternalSystemId.ToString() == row["ID"]))
            {
                object locker = new object();
                try
                {
                       
                        if (!string.IsNullOrWhiteSpace(row["CABLE"])) //look up cable we probably omorted already//{
                        { PhysicalPair cpair = new PhysicalPair(uow);

                        cpair.ExternalSystemId = int.Parse(row["ID"]);
                        cpair.SourceTable = cpTable; // int.Parse(row["LOCATIONID"]);  
                        cpair.Status = !string.IsNullOrWhiteSpace(row["STATUS"]) ? DefaultFields.GetBusinessObjectDefault<CablePairStatus>(uow, "StatusName", row["STATUS"]) : DefaultFields.GetBusinessObjectDefault<CablePairStatus>(uow, "StatusName", "UNKNOWN");
                        cpair.PairNumber = int.Parse(row["NUM"]);
                            try
                            {
                                PhysicalCable cable = uow.Query<PhysicalCable>().FirstOrDefault(x => x.ExternalSystemId.ToString() == row["CABLE"]);
                            if(cable != null)
                            {
                                //store guid and add to cable afterwards to avoid locking issues
                                //lock (locker)
                                //{
                                //    cable_cpairs_List.Add(new Tuple<Guid, Guid>(cable.Oid, cpair.Oid));
                                cable.CablePairs.Add(cpair);
                                cpair.Cable = cable;
                                //}
                                uow.CommitChanges();
                                successfulCpairs++;
                                progressMade?.Invoke("Cable Pairs",
                                                     new ProgressMadeEventArgs(new ImportedItem()
                                {
                                    Guid = cpair.Oid.ToString(),
                                    Id = cpair.ExternalSystemId?.ToString(),
                                    SourceTable = cpair.SourceTable,
                                    ImportStatus = "Success",
                                    RecordStatus = cpair.Status?.ToString(),
                                    Type = "CablePair"
                                }));
                            } else throw new Exception($"Cannot Find cable '{row["CABLE"]}' for pair '{cpair.ExternalSystemId}'");
                                // }
                            }
                            catch (Exception ex)
                            {                              
                                errorsCpairs++;
                                StaticHelperMethods.WriteOut(@"
                                uow.RollbackTransaction();
                                puow.CommitChanges();
                                errorsCpairs++\n" + $"{string.Join(", ", row)}" +
                                    $"\n{ex}" +
                                    $"\n{ex.StackTrace}" +
                                    $"\n{ex.Source}" +
                                    $"\n{ex.TargetSite}");
                            }
                        }
                        else
                        {
                            NewNetServices.Module.Core.StaticHelperMethods.WriteOut(@"  uow.RollbackTransaction();
                            throw new Exception($""Cable not found with id: {row[""CABLE ""]}"); //{
                          //  uow.RollbackTransaction();
                            throw new Exception($"Cable not found with id: {row["CABLE "]}"); //{
                        }
                    
                }
                catch (Exception ex)
                {
                    errorsCpairs++;
                    progressMade?.Invoke("Cable Pairs",
                                         new ProgressMadeEventArgs(new ImportedItem()
                                         {
                                             SourceTable = cpTable,
                                             ImportStatus = "Exception" + $"\t{ex.Message}",
                                             Type = "Exception"
                                         }));
                    StaticHelperMethods.WriteOut($"{ex}");
                }
            } //end if exists
            else
            {
                progressMade?.Invoke(null,
                                     new ProgressMadeEventArgs(new ImportedItem()
                                     {
                                         SourceTable = cpTable,
                                         ImportStatus = "OK",
                                         Type = $"Already Exists {row["ID"]}"
                                     }));
            }
        }

        public static Guid locUnknown = Guid.Empty;
        public static Guid wireCneterUnkown = Guid.Empty;
        public static Guid copperClassOid = Guid.Empty;
        public static Guid fiberClassOid = Guid.Empty;
        //public static Guid cblMediaUnknOid = Guid.Empty;
        //public static Guid conMediaUnknOid = Guid.Empty;
        public static Guid cblSize1Oid = Guid.Empty;
        public static Guid conSize1Oid = Guid.Empty;
        public static Guid cblTypeUnkOid = Guid.Empty;
        public static Guid conTypeUnkOid = Guid.Empty;
        public static Guid cblStatusUnkOid = Guid.Empty;
        public static Guid conStatusUnkOid = Guid.Empty;
        public static List<Tuple<Guid, Guid>> DestCableList = new List<Tuple<Guid, Guid>>();
        public static List<Tuple<Guid, Guid>> SourceConduitList = new List<Tuple<Guid, Guid>>();
        public static List<Tuple<Guid, Guid>> DestConduitList = new List<Tuple<Guid, Guid>>();

        public static async System.Threading.Tasks.Task ProcessCable(UnitOfWork puow, Dictionary<string, string> row, string cabTable, EventHandler<ProgressMadeEventArgs> progressMade, string stepName)
        {
            Stopwatch sw = Stopwatch.StartNew();
            const string tempfile = @"C:/Stopwatch/times.bat";
            //NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"",true,true,tempfile);
            try
            {

                //using (UnitOfWork puow =new UnitOfWork(puow.DataLayer))
                //{
                var sw3 = Stopwatch.StartNew();
                //puow.BeginTransaction();

                //Location source = puow.Query<Location>().FirstOrDefault(x => x.ExternalSystemId.ToString() == row["SOURCELOCATIONID"]);
                //Location destination = puow.Query<Location>().FirstOrDefault(x => x.ExternalSystemId.ToString() == row[""]);
                //if (source == null)
                //    source = DefaultFields.GetBusinessObjectDefault<Location>(puow, "LocationName", "UNKNOWN");
                //if (destination == null) destination = DefaultFields.GetBusinessObjectDefault<Location>(puow, "LocationName", "UNKNOWN");
                try
                {
                    PhysicalCable cable = new PhysicalCable(puow);
                    cable.Source = puow.GetObjectByKey<Location>(DefaultFields.LocationOid(puow, row["SOURCELOCATIONID"]));

                    cable.Destination =
                        puow.GetObjectByKey<Location>(DefaultFields.LocationOid(puow, row["DESTINATIONLOCATIONID"]));

                    cable.ExternalSystemId = int.Parse(row["CABLEID"]);
                    cable.Wirecenter = puow.GetObjectByKey<Wirecenter>(DefaultFields.WirecenterOid(puow, "UNKNOWN"));
                    cable.WorkOrder = !string.IsNullOrWhiteSpace(row["WORKORDERID"])
                        ? puow.GetObjectByKey<WorkOrder>(DefaultFields.WorkOrderOid(puow, row["WORKORDERID"]))
                        : null;
                    cable.SourceTable = cabTable; // int.Parse(row["LOCATIONID"]);                                
                    cable.Status = (!string.IsNullOrWhiteSpace(row["CABLESTATUS"])) ?
                        puow.GetObjectByKey<CableStatus>(DefaultFields.GetStatus<CableStatus>(puow, row["CABLESTATUS"])) : null;
                    cable.Route = (!string.IsNullOrWhiteSpace(row["CABLEROUTE"])) ?
                    puow.GetObjectByKey<Route>(DefaultFields.RouteOid(puow, row["CABLEROUTE"])) : null;
                    //dest_cable_List.Add(new Tuple<Guid,Guid>(Destination != null ? Destination.Oid : Guid.Empty, cable.Oid));
                    //source_cable_List.Add(new Tuple<Guid,Guid>(Source != null ? Source.Oid : Guid.Empty, cable.Oid));

                    cable.Information = !string.IsNullOrWhiteSpace(row["DESCRIPTION"])
                        ? row["DESCRIPTION"]
                        : "";
                    cable.Length = !string.IsNullOrWhiteSpace(row["CABLELENGTH"]) &&
                                     double.TryParse(row["CABLELENGTH"], out double l)
                         ? l
                         : 0;

                    cable.Class = (!string.IsNullOrWhiteSpace(row["FORC"])) ? puow.GetObjectByKey<CableClass>(DefaultFields.CableConduitClassOid(puow, row["FORC"], typeof(CableClass))) : null;
                    cable.Size = (!string.IsNullOrWhiteSpace(row["CABLESIZE"])) ? puow.GetObjectByKey<CableSize>(
                        DefaultFields.CableConduitSizeOid(puow, row["CABLESIZE"], int.TryParse(row["CABLESIZE"], out int sz) ? sz : 1, typeof(CableSize))) : null;
                    cable.Media = puow.GetObjectByKey<CableMedia>(DefaultFields.CableConduitMediaOid(puow, "Media", typeof(CableMedia)));

                    cable.Type = row["DROPCABLE"] == "1"
                        ? puow.GetObjectByKey<CableType>(DefaultFields.CableConduitTypeOid(puow, "DROPCABLE", typeof(CableType)))
                        : !string.IsNullOrWhiteSpace(row["CABLETYPE"])
                        ? puow.GetObjectByKey<CableType>(DefaultFields.CableConduitTypeOid(puow, row["CABLETYPE"], typeof(CableType)))
                          : puow.GetObjectByKey<CableType>(DefaultFields.CableConduitTypeOid(puow, "UNKNOWN", typeof(CableType)));
                    cable.CableName = cable.GeneratedName; //{
                    if (!string.IsNullOrWhiteSpace(row["INSTALLDATE"]) && DateTime.TryParse(row["INSTALLDATE"], out DateTime dt))
                        cable.DatePlaced = dt;
                    // puow.CommitTransaction(); 
                    puow.CommitChanges();
                    //  puow.CommitTransaction();

                    MainWindow.currentSuccess++;
                    NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"AfterCreation {sw3.Elapsed}", true, true, tempfile);
                    progressMade?.Invoke(stepName, new ProgressMadeEventArgs(new ImportedItem()
                    {
                        ImportStatus = "Success",
                        Type = "Cable"
                    }));
                }
                catch (Exception ex)
                {
                    NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{ex.Message}\n{ex.StackTrace}");
                    //puow.RollbackTransaction();
                    //puow.RollbackTransaction();
                    throw;
                }

            }
            catch (Exception ex)
            {
                MainWindow.currentErrors++;
                progressMade?.Invoke(stepName, new ProgressMadeEventArgs(new ImportedItem()
                {
                    SourceTable = cabTable,
                    ImportStatus = "Exception" + $"\t{ex.Message}",
                    Type = "Exception"
                }));
                StaticHelperMethods.WriteOut($"{ex}");
            }
            NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"CableMethodTime:\t{sw.Elapsed}", true, true, tempfile);
        }

        public static void ProcessConduit(UnitOfWork puow, Dictionary<string, string> row, string conTable,
            EventHandler<ProgressMadeEventArgs> progressMade, string stepName)
        {
            //"ID", "STATUS", "LENGTH", "TYPE", "CODE", "MEDIA", "WORKORDER", "CABLE", "INSTALLDATE" 
            //" ", " ", " ", " ", " ", " ", " ", " ", " " 
            Stopwatch sw = Stopwatch.StartNew();
            const string tempfile = @"C:/Stopwatch/times.bat";
            //NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"",true,true,tempfile);


            //using (UnitOfWork puow =new UnitOfWork(puow.DataLayer))
            //{
            var sw3 = Stopwatch.StartNew();
            //puow.BeginTransaction();

            //Location source = puow.Query<Location>().FirstOrDefault(x => x.ExternalSystemId.ToString() == row["SOURCELOCATIONID"]);
            //Location destination = puow.Query<Location>().FirstOrDefault(x => x.ExternalSystemId.ToString() == row[""]);
            //if (source == null)
            //    source = DefaultFields.GetBusinessObjectDefault<Location>(puow, "LocationName", "UNKNOWN");
            //if (destination == null) destination = DefaultFields.GetBusinessObjectDefault<Location>(puow, "LocationName", "UNKNOWN");
            try
            {
                Conduit conduit = new Conduit(puow);

                Cable cable = null;

                //look up cable we probably  imported already
                cable = !string.IsNullOrWhiteSpace(row["CABLE"])
                    ? puow.Query<Cable>().FirstOrDefault(x => x.ExternalSystemId.ToString() == row["CABLE"])
                    : null;

                conduit.Source = cable != null && cable.Source != null
                    ? cable.Source
                    : puow.GetObjectByKey<Location>(DefaultFields.LocationOid(puow, "UNKNOWN"));

                conduit.Destination = cable != null && cable.Source != null
                    ? cable.Source
                    : puow.GetObjectByKey<Location>(DefaultFields.LocationOid(puow, "UNKNOWN"));

                conduit.ExternalSystemId = int.Parse(row["ID"]);
                conduit.Wirecenter = puow.GetObjectByKey<Wirecenter>(DefaultFields.WirecenterOid(puow, "UNKNOWN"));
                conduit.WorkOrder = !string.IsNullOrWhiteSpace(row["WORKORDER"])
                    ? puow.GetObjectByKey<WorkOrder>(DefaultFields.WorkOrderOid(puow, row["WORKORDER"]))
                    : null;
                conduit.SourceTable = conTable; // int.Parse(row["LOCATIONID"]);                                
                conduit.Status = (!string.IsNullOrWhiteSpace(row["STATUS"]))
                    ? puow.GetObjectByKey<ConduitStatus>(DefaultFields.GetStatus<ConduitStatus>(puow, row["STATUS"]))
                    : null;  
                //dest_conduit_List.Add(new Tuple<Guid,Guid>(Destination != null ? Destination.Oid : Guid.Empty, conduit.Oid));
                //source_conduit_List.Add(new Tuple<Guid,Guid>(Source != null ? Source.Oid : Guid.Empty, conduit.Oid));
                 
                conduit.Length = !string.IsNullOrWhiteSpace(row["LENGTH"]) &&
                                 double.TryParse(row["LENGTH"], out double l)
                    ? l
                    : 0;

                conduit.Class =
                    puow.GetObjectByKey<ConduitClass>(
                        DefaultFields.CableConduitClassOid(puow, "Class", typeof(ConduitClass)));
                conduit.Size = puow.GetObjectByKey<ConduitSize>(
                    DefaultFields.CableConduitSizeOid(puow, row["CODE"]!=""?row["CODE"]:"1", 1, typeof(ConduitSize)));
                conduit.Media = puow.GetObjectByKey<ConduitMedia>(DefaultFields.CableConduitMediaOid(puow,
                    row["MEDIA"] != "" ? row["MEDIA"] : "Media", typeof(ConduitMedia)));

                conduit.Type = !string.IsNullOrWhiteSpace(row["TYPE"])
                    ? puow.GetObjectByKey<ConduitType>(
                        DefaultFields.CableConduitTypeOid(puow, row["TYPE"], typeof(ConduitType)))
                    : puow.GetObjectByKey<ConduitType>(
                        DefaultFields.CableConduitTypeOid(puow, "UNKNOWN", typeof(ConduitType)));
                conduit.ConduitName = conduit.GeneratedName; //{
                if (!string.IsNullOrWhiteSpace(row["INSTALLDATE"]) &&
                    DateTime.TryParse(row["INSTALLDATE"], out DateTime dt))
                    conduit.DatePlaced = dt;
                // puow.CommitTransaction(); 
                //puow.CommitChanges();
                ////  puow.CommitTransaction();////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

              //  NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"s3:\t{sw3.Elapsed}", true, true,
                 //   tempfile);
                puow.CommitChanges();
                //do class media stuffif  
                //try
                //{
                //    //lock (locker)
                //    //{
                //    if (conduit.Class != null)

                //    {
                //        //watch out for session mixing
                //        if (Size != null &&
                //            !conduit.Class.ConduitSizes.Any(x => x.Oid == conduit.Size.Oid))
                //            conduit.Class.ConduitSizes.Add(conduit.Size);
                //        if (Type != null &&
                //            !conduit.Class.ConduitTypes.Any(x => x.Oid == Type.Oid))
                //        {
                //            conduit.Type.Code = row["CODE"];
                //            conduit.Class.ConduitTypes.Add(conduit.Type);
                //        }
                //        if (Media != null &&
                //            !conduit.Class.ConduitMedia.Any(x => x.Oid == Media.Oid))
                //            conduit.Class.ConduitMedia.Add(conduit.Media);
                //    }
                //    tempuow.CommitTransaction();
                //    uow.CommitTransaction();
                //    // }
                //}
                //catch (Exception ex)
                //{
                //    NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"problem with media stuff{ex}");
                //}
                MainWindow.currentSuccess++;
                progressMade?.Invoke(stepName,
                    new ProgressMadeEventArgs(new ImportedItem()
                    {
                        Id = row["ID"],
                        SourceTable = conTable,
                        ImportStatus = "Success",
                        RecordStatus = "",
                        Type = "Conduit",
                        SubType = "" // row["SUBTYPE"]
                    }));

            } //end try
            catch (Exception ex)
            {

                MainWindow.currentErrors++;
                progressMade?.Invoke(stepName, new ProgressMadeEventArgs(new ImportedItem()
                {
                    SourceTable = conTable,
                    ImportStatus =
                "Exception" +
                    "\t{ex.Message}",
                    Type = "Exception"
                }));
                StaticHelperMethods.WriteOut($"{ex}");
            }

            NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"CONDUITMethodTime:\t{sw.Elapsed}", true, true,
                tempfile);
        }

        public static Pole CreatePole(Dictionary<string, string> row, UnitOfWork puow, string table)
        {
            /*"JUNCTIONS"  "OBJECTID", "APID", "STATUS", "NAME", "WO", "TYPE", "CITY",
            "INSTALLDATE_NEW", "ENTITYID", "ENTITYTYPE", "ACCESSPOINTTYPE", "ACCESSPOINTID", "REFERENCENAME",
            "REFERENCETYPECODE", "ENTITYNAME", "REGIONCODE", "SUBTYPE", "WOID", "ROUTE" */

            //StaticHelperMethods.WriteOut($"{string.Join("\t", row.Select(x => x.Key + ":" + x.Value))}");
            try
            {
                using (NestedUnitOfWork uow = puow.BeginNestedUnitOfWork())
                {
                    uow.BeginTransaction();
                    Pole pole = new Pole(uow);
                    pole.ExternalSystemId = int.Parse(row["ENTITYID"]);
                    uow.CommitChanges();
                    pole.SourceTable = table;
                    pole.SourceType = row["ENTITYTYPE"];
                    pole.LocationName = !string.IsNullOrWhiteSpace(row["ENTITYNAME"]) ? row["ENTITYNAME"] : "<EMPTY>";
                    pole.WorkOrder = !string.IsNullOrWhiteSpace(row["WO"]) ? DefaultFields.GetBusinessObjectDefault<WorkOrder>(uow, "OrderNumber", row["WO"]) : null;



                    pole.Type = !string.IsNullOrWhiteSpace(row["TYPE"]) ? DefaultFields.GetBusinessObjectDefault<PoleType>(uow, "TypeName", row["TYPE"])
                        : DefaultFields.GetBusinessObjectDefault<PoleType>(uow, "TypeName", "UNKNOWN");

                    pole.Status = !string.IsNullOrWhiteSpace(row["STATUS"]) ? DefaultFields.GetBusinessObjectDefault<LocationStatus>(uow, "StatusName", row["STATUS"])
                        : DefaultFields.GetBusinessObjectDefault<LocationStatus>(uow, "StatusName", "UNKNOWN");
                    pole.Comment = $"APId:{row["APID"] } ";
                    pole.Wirecenter = DefaultFields.GetBusinessObjectDefault<Wirecenter>(uow, new List<Tuple<string, object>>()                        {
                            new Tuple<string, object>("LocationName", "UNKNOWN")
                        });
                    pole.Boundary = !string.IsNullOrWhiteSpace(row["CITY"])
                        ? DefaultFields
                     .GetBusinessObjectDefault<Boundary>(uow,
                                                         new List<Tuple<string, object>>()
                        {
                            new Tuple<string, object>("Name", row["CITY"])
                        })
                        : null; //{
                    if (!string.IsNullOrWhiteSpace(row["INSTALLDATE_NEW"]) &&
                        DateTime.TryParse(row["INSTALLDATE_NEW"], out DateTime dt))
                        pole.DatePlaced = dt;
                    try
                    {
                        uow.CommitTransaction();
                        puow.CommitChanges();
                    }
                    catch //(Exception ex)
                    {
                        uow.RollbackTransaction();
                        throw;
                    }
                    return pole;
                }
            }
            catch (Exception ex)
            {
                NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{ex}");
                return null;// System.Threading.Tasks.Task.FromResult(null); ;
            }
        }

        public static Junction CreateJunction(Dictionary<string, string> row, UnitOfWork puow, string table)
        {
            /*"JUNCTIONS" ( "OBJECTID", "APID", "STATUS", "NAME", "WO", "TYPE", "CITY",
            "INSTALLDATE_NEW", "ENTITYID", "ENTITYTYPE", "ACCESSPOINTTYPE", "ACCESSPOINTID", "REFERENCENAME",
            "REFERENCETYPECODE", "ENTITYNAME", "REGIONCODE", "SUBTYPE", "WOID", "ROUTE") AS */

            try
            {
                using (NestedUnitOfWork uow = puow.BeginNestedUnitOfWork())
                {
                    uow.BeginTransaction();
                    Junction junk = new Junction(uow);
                    junk.ExternalSystemId = int.Parse(row["ENTITYID"]);
                    uow.CommitChanges();
                    junk.SourceTable = table;
                    junk.SourceType = row["ENTITYTYPE"];
                    junk.LocationName = !string.IsNullOrWhiteSpace(row["ENTITYNAME"]) ? row["ENTITYNAME"] : "<EMPTY>";
                    junk.WorkOrder = !string.IsNullOrWhiteSpace(row["WOID"]) ? DefaultFields.GetBusinessObjectDefault<WorkOrder>(uow, "OrderNumber", row["WO"]) : null;

                    junk.Type = !string.IsNullOrWhiteSpace(row["SUBTYPE"]) ? DefaultFields.GetBusinessObjectDefault<JunctionType>(uow, "TypeName", row["SUBTYPE"]) : DefaultFields.GetBusinessObjectDefault<JunctionType>(uow, "TypeName", "UNKNOWN");
                    junk.Route = !string.IsNullOrWhiteSpace(row["ROUTE"]) ? DefaultFields.GetBusinessObjectDefault<Route>(uow, "Name", row["ROUTE"]) : null;
                    junk.Status = !string.IsNullOrWhiteSpace(row["STATUS"]) ? DefaultFields.GetBusinessObjectDefault<LocationStatus>(uow, "StatusName", row["STATUS"]) : DefaultFields.GetBusinessObjectDefault<LocationStatus>(uow, "StatusName", "UNKNOWN");


                    junk.Comment = $"APId:{row["APID"] } ";
                    junk.Wirecenter = DefaultFields.GetBusinessObjectDefault<Wirecenter>(uow, "LocationName", "UNKNOWN");


                    junk.Boundary = !string.IsNullOrWhiteSpace(row["CITY"]) ? DefaultFields.GetBusinessObjectDefault<Boundary>(uow, "Name", row["CITY"]) : null; //{
                    if (!string.IsNullOrWhiteSpace(row["INSTALLDATE_NEW"]) && DateTime.TryParse(row["INSTALLDATE_NEW"], out DateTime dt))
                        junk.DatePlaced = dt;
                    try
                    {
                        uow.CommitTransaction();
                        puow.CommitChanges();
                    }
                    catch //(Exception ex)
                    {
                        uow.RollbackTransaction();
                        throw;
                    }
                    return junk;
                }
            }
            catch (Exception ex)
            {
                NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{ex}");
                return null;// System.Threading.Tasks.Task.FromResult(null); ;
            }
        }

        public static bool? ProcessJunction(UnitOfWork uow, Dictionary<string, string> row, string junkTable, EventHandler<ProgressMadeEventArgs> progressMade)
        {
            object junk = null;


            //make sure not imported already

            try
            {
                if (row["ENTITYTYPE"] == "POLE")
                //make pole instead
                {
                    if (!uow.Query<Pole>().Any(x => x.ExternalSystemId.ToString() == row["ENTITYID"]))
                    {
                        junk = CreatePole(row, uow, junkTable);
                    }
                }
                else if (row["ENTITYTYPE"] == "SUBSCRIBER")
                //skip cuz we do separately
                {
                    return null;
                }
                else
                {
                    if (!uow.Query<Junction>().Any(x => x.ExternalSystemId.ToString() == row["ENTITYID"]))
                    {
                        junk = CreateJunction(row, uow, junkTable);
                    }
                }

                if (junk != null)
                    return true;
                else if (uow.Query<Location>().Any(x => x.ExternalSystemId.ToString() == row["ENTITYID"]))
                    return null;
                else throw new System.Exception($"Failed to create Junction!!");


                //StaticHelperMethods.WriteOut($"SUCCESS TERMINAL:>");
            }
            catch (Exception ex)
            {
                StaticHelperMethods.WriteOut($"{ex}");
                return false;
            }

        }

        public static bool ProcessSubscriber(UnitOfWork puow, Dictionary<string, string> row, string subTable, Guid state, EventHandler<ProgressMadeEventArgs> progressMade
            , string stepName)
        {
            //StaticHelperMethods.WriteOut($"{string.Join("\t", row.Select(x => x.Key + ":" + x.Value))}");
            //make sure not imported already
            if (!puow.Query<Subscriber>().Any(x => x.ExternalSystemId.ToString() == row["SUBSCRIBERID"]))
            {
                using (NestedUnitOfWork uow = puow.BeginNestedUnitOfWork())
                {
                    uow.BeginTransaction();
                    try
                    {
                        Subscriber sub = new Subscriber(uow);
                        sub.ExternalSystemId = int.Parse(row["SUBSCRIBERID"]);
                        uow.CommitChanges();
                        sub.Wirecenter = DefaultFields.GetBusinessObjectDefault<Wirecenter>(uow, "LocationName", "UNKNOWN");
                        sub.SourceTable = subTable; // int.Parse(row["LOCATIONID"]); 
                        sub.LocationName = !string.IsNullOrWhiteSpace(row["SUBSCRIBERNAME"])
                            ? row["SUBSCRIBERNAME"]
                            : "<EMPTY>";
                        sub.FlexInt = !string.IsNullOrWhiteSpace(row["ACC_POINT_ID_FLEXINT"])
                            ? int.Parse(row["ACC_POINT_ID_FLEXINT"])
                            : 0;
                        sub.Status = !string.IsNullOrWhiteSpace(row["SUBSCRIBERSTATUS"])
                            ? DefaultFields.GetBusinessObjectDefault<LocationStatus>(uow, "StatusName", row["SUBSCRIBERSTATUS"])
                            : DefaultFields.GetBusinessObjectDefault<LocationStatus>(uow, "StatusName", "UNKNOWN");

                        sub.Type = !string.IsNullOrWhiteSpace(row["SUBSCRIBERTYPE"])
                            ? DefaultFields.GetBusinessObjectDefault<SubscriberType>(uow, "TypeName", row["SUBSCRIBERTYPE"])
                            : DefaultFields.GetBusinessObjectDefault<SubscriberType>(uow, "TypeName", "UNKNOWN");
                        //address
                        NewNetServices.Module.BusinessObjects.CableManagement.Address addy = null;
                        //see ifaddress alrady exists
                        if (!uow.Query<NewNetServices.Module.BusinessObjects.CableManagement.Address>()
                            .Any(x => x.FullAddress.Substring(0, row["FULLADDY"].Length) == row["FULLADDY"] &&
                                x.ZipPostal == row["ADDRESSZIP"]))
                        {
                            addy = new NewNetServices.Module.BusinessObjects.CableManagement.Address(uow);
                            string str = !string.IsNullOrWhiteSpace(row["FULLADDY"]) ? (row["FULLADDY"]) + " " : "";

                            addy.ExternalSystemId = sub.ExternalSystemId;
                            addy.SourceTable = "AddressTable";
                            addy.Street = str;
                            addy.ZipPostal = row["ADDRESSZIP"];
                            addy.City = !string.IsNullOrWhiteSpace(row["CITY"]) ? (str + row["CITY"]) + " " : "";

                            addy.StateProvince = uow.GetObjectByKey<NewNetServices.Module.BusinessObjects.CableManagement.State>(state);
                        }
                        else
                        {
                            addy = uow.Query<NewNetServices.Module.BusinessObjects.CableManagement.Address>()
                                .FirstOrDefault(x => x.FullAddress.Substring(0, row["FULLADDY"].Length) ==
                                    row["FULLADDY"] &&
                                    x.ZipPostal == row["ADDRESSZIP"]);
                        }
                        sub.Address = addy;
                        //  sub.Type = !string.IsNullOrWhiteSpace(row["SUBSCRIBERCODE"]) ? ImporterHelper.GetSubscriberType(uow, row["SUBSCRIBERCODE"]) : null;


                        uow.CommitTransaction();
                        uow.CommitChanges();
                        puow.CommitChanges();
                        MainWindow.currentSuccess++;
                        progressMade?.Invoke(stepName,
                                             new ProgressMadeEventArgs(new ImportedItem()
                                             {
                                                 Guid = sub.Oid.ToString(),
                                                 Id = sub.ExternalSystemId?.ToString(),
                                                 SourceTable = sub.SourceTable,
                                                 RecordStatus = sub.Status?.ToString(),
                                                 ImportStatus = "Success",
                                                 Type = "Subscriber",
                                                 SubType = $"{sub.Type?.TypeName}"
                                             }));
                        return true;

                    }
                    catch (Exception ex)
                    {
                        uow.RollbackTransaction();
                        StaticHelperMethods.WriteOut($"{ex}");
                        // return false;
                        MainWindow.currentErrors++;
                        progressMade?.Invoke(stepName,
                                             new ProgressMadeEventArgs(new ImportedItem()
                                             {
                                                 SourceTable = subTable,
                                                 ImportStatus = "Exception" + $"\t{ex.Message}",
                                                 Type = "Exception"
                                             }));
                        return false;
                    }
                }
            } //end if exists
            else
            {
                //SuccessfulSubscribers++;
                progressMade?.Invoke(stepName, new ProgressMadeEventArgs(new ImportedItem()
                {
                    SourceTable = subTable,
                    ImportStatus = "Success",
                    Type = $"Already Exists {row["SUBSCRIBERID"]}"
                }));

                return true;
            }
        }
        //        private static string defaultStatusName = "UNKNOWN";
        //        private static string defaultTypeName = "UNKNOWN";

        //        //public static Guid ImportSubscriber(string[] args, string cmsconnectionString, string oracleconnectionString, out string tinfo)
        //        //{
        //        //    List<string> labelInfo = new List<string>();
        //        //    Guid ret = Guid.Empty;
        //        //    using (var uow = NewNetServicesUnitOfWorkFactory.GetNewUnitOfWork(cmsconnectionString))
        //        //    {
        //        //        Subscriber sub = new Subscriber(uow);
        //        //        var stat = GetLocationStatus<LocationStatus>(uow);
        //        //        if (stat == null) stat = CreateDefaultLocationStatus(uow);
        //        //        sub.Status = stat;
        //        //        // now get type , if not exist, make
        //        //        sub.Type = GetSubscriberType(args, uow);
        //        //        sub.Handle = args[MainWindow.GID_Index];
        //        //        sub.Type = uow.Query<SubscriberType>().FirstOrDefault(x => x.TypeName == args[MainWindow.locLAYER_index]);
        //        //        sub.IsEngineering = true;
        //        //        string str = "";
        //        //        //now get label info
        //        //        using (var odw = new OracleDatabaseWorker(oracleconnectionString))
        //        //        {
        //        //            labelInfo = odw.GetInfoFromLabelTable(args[MainWindow.GID_Index]);
        //        //        }
        //        //        str = string.Join(" | ", labelInfo.ToArray());
        //        //        str += !string.IsNullOrWhiteSpace(args[MainWindow.locONT_ID_index]) ? $" ONT_ID: {args[MainWindow.locONT_ID_index]}" : "";
        //        //        if (str.Length > 99)
        //        //        {
        //        //            sub.Comment = str;
        //        //            sub.LocationName = str.Substring(0, 99);
        //        //        }
        //        //        else
        //        //            sub.LocationName = string.IsNullOrWhiteSpace(str) ? "<Empty>" : str;
        //        //        // //odw.SetGUIDFromGIDOracle<Location>(
        //        //        //  }
        //        //        tinfo = sub.LocationName;
        //        //        uow.CommitChanges();
        //        //        ret = sub.Oid;

        //        //    }
        //        //    return ret;
        //        //}

        //        //public static Guid ImportPole(string[] args, string cmsconnectionString, string oracleconnectionString, out string tinfo)
        //        //{
        //        //    List<string> labelInfo = new List<string>();
        //        //    Guid ret = Guid.Empty;
        //        //    using (var uow = NewNetServicesUnitOfWorkFactory.GetNewUnitOfWork(cmsconnectionString))
        //        //    {
        //        //        Pole sub = new Pole(uow);
        //        //        var stat = GetLocationStatus<LocationStatus>(uow);
        //        //        if (stat == null) stat = CreateDefaultLocationStatus(uow);
        //        //        sub.Status = stat;
        //        //        // now get type , if not exist, make
        //        //        sub.Type = GetPoleType(args, uow);
        //        //        sub.Handle = args[MainWindow.GID_Index];
        //        //        sub.Type = uow.Query<PoleType>().FirstOrDefault(x => x.TypeName == args[MainWindow.locLAYER_index]);
        //        //        sub.IsEngineering = true;
        //        //        string str;
        //        //        //now get label info
        //        //        using (var odw = new OracleDatabaseWorker(oracleconnectionString))
        //        //        {
        //        //            labelInfo = odw.GetInfoFromLabelTable(args[MainWindow.GID_Index]);
        //        //        }
        //        //        str = string.Join(" | ", labelInfo.ToArray());
        //        //        str += !string.IsNullOrWhiteSpace(args[MainWindow.locONT_ID_index]) ? $" ONT_ID: {args[MainWindow.locONT_ID_index]}" : "";
        //        //        if (str.Length > 99)
        //        //        {
        //        //            sub.Comment = str;
        //        //            sub.LocationName = str.Substring(0, 99);
        //        //        }
        //        //        else
        //        //            sub.LocationName = string.IsNullOrWhiteSpace(str) ? "<Empty>" : str;
        //        //        uow.CommitChanges();
        //        //        ret = sub.Oid;
        //        //        tinfo = sub.LocationName;
        //        //    }
        //        //    return ret;
        //        //}


        //        //public static Guid ImportJunction(string[] args, string cmsconnectionString, string oracleconnectionString, out string tinfo)
        //        //{
        //        //    List<string> labelInfo = new List<string>();
        //        //    Guid ret = Guid.Empty;
        //        //    using (var uow = NewNetServicesUnitOfWorkFactory.GetNewUnitOfWork(cmsconnectionString))
        //        //    {
        //        //        Junction sub = new Junction(uow);
        //        //        var stat = GetLocationStatus<LocationStatus>(uow);
        //        //        if (stat == null) stat = CreateDefaultLocationStatus(uow);
        //        //        sub.Status = stat;
        //        //        // now get type , if not exist, make
        //        //        sub.Type = GetJunctionType(args, uow);
        //        //        sub.Handle = args[MainWindow.GID_Index];
        //        //        sub.IsEngineering = true;
        //        //        sub.Type = uow.Query<JunctionType>().FirstOrDefault(x => x.TypeName == args[MainWindow.locLAYER_index]);

        //        //        string str;
        //        //        //now get label info
        //        //        using (var odw = new OracleDatabaseWorker(oracleconnectionString))
        //        //        {
        //        //            if (args[MainWindow.locSYM_NAME_index] == "PED")
        //        //            {
        //        //                var lroute = odw.GetJunctionRoute(sub.Handle);
        //        //                var lsize = odw.GetJunctionSize(sub.Handle);
        //        //                var ldate = odw.GetJunctionDate(sub.Handle);
        //        //                if (lroute.Count > 0)
        //        //                {
        //        //                    sub.Route = uow.Query<Route>().FirstOrDefault(x => x.Name == lroute[0]);
        //        //                }
        //        //                if (lsize.Count > 0)
        //        //                {
        //        //                    sub.Size = uow.Query<JunctionSize>().FirstOrDefault(x => x.Size == lsize[0]);
        //        //                }
        //        //                if (ldate.Count > 0 && ldate[0] != "XX")
        //        //                {
        //        //                    DateTime dt = new DateTime(2000 + Convert.ToInt32(ldate[0]), 1, 1);
        //        //                    sub.DatePlaced = dt;
        //        //                }
        //        //            }
        //        //            else if (args[MainWindow.locSYM_NAME_index] == "NT_PED")
        //        //            {
        //        //                var lsize = odw.GetJunctionSize(sub.Handle);
        //        //                if (lsize.Count > 0)
        //        //                {
        //        //                    sub.Size = uow.Query<JunctionSize>().FirstOrDefault(x => x.Size == lsize[0]);
        //        //                }
        //        //            }
        //        //            labelInfo = odw.GetInfoFromLabelTable(args[MainWindow.GID_Index]);
        //        //        }
        //        //        str = args[MainWindow.locTYPE_NAME_index] + string.Join(" | ", labelInfo.ToArray());
        //        //        str += !string.IsNullOrWhiteSpace(args[MainWindow.locONT_ID_index]) ? $" ONT_ID: {args[MainWindow.locONT_ID_index]}" : "";

        //        //        if (str.Length > 99)
        //        //        {
        //        //            sub.Comment = str;
        //        //            sub.LocationName = str.Substring(0, 99);
        //        //        }
        //        //        else
        //        //            sub.LocationName = string.IsNullOrWhiteSpace(str) ? "<Empty>" : str;
        //        //        uow.CommitChanges();
        //        //        ret = sub.Oid;
        //        //        tinfo = sub.LocationName;
        //        //    }
        //        //    return ret;
        //        //}

        //        //internal static Guid ImportCable(string[] args, string cmsconnectionString, string oracleconnectionString, out string tinfo)
        //        //{
        //        //    List<string> labelInfo = new List<string>();
        //        //    Guid ret = Guid.Empty;
        //        //    try
        //        //    {
        //        //        using (var uow = NewNetServicesUnitOfWorkFactory.GetNewUnitOfWork(cmsconnectionString))
        //        //        {
        //        //            PhysicalCable sub = new PhysicalCable(uow);
        //        //            //  uow.CommitChanges();
        //        //            var stat = GetCableStatus<CableStatus>(uow);
        //        //            if (stat == null) stat = CreateDefaultCableStatus(uow);
        //        //            sub.Status = stat;
        //        //            // now get type , if not exist, make
        //        //            //  sub.Type = GetPhysicalCableType(args, uow);
        //        //            sub.Handle = args[MainWindow.GID_Index];
        //        //            sub.IsEngineering = true;

        //        //            sub.Media = uow.Query<CableMedia>().FirstOrDefault(x => x.TypeName == "UNKNOWN");
        //        //            sub.Class = uow.Query<CableClass>().FirstOrDefault(x => x.TypeName == "Fiber");


        //        //            //now get label info
        //        //            using (var odw = new OracleDatabaseWorker(oracleconnectionString))
        //        //            {
        //        //                var lsize = odw.GetCableSize(sub.Handle);
        //        //                if (lsize.Count > 0)
        //        //                {
        //        //                    sub.Size = uow.Query<CableSize>().FirstOrDefault(x => x.Code == lsize[0]);
        //        //                }
        //        //                labelInfo = odw.GetInfoFromLabelTable(args[MainWindow.GID_Index]);
        //        //            }
        //        //            var str = args[MainWindow.cabTYPE_NAME_index] + string.Join(" | ", labelInfo.ToArray());
        //        //            if (str.Length > 99)
        //        //            {
        //        //                sub.Comment = str;
        //        //                sub.CableName = str.Substring(0, 99);
        //        //            }
        //        //            else
        //        //                sub.CableName = string.IsNullOrWhiteSpace(str) ? "<Empty>" : str;
        //        //            sub.Type = uow.Query<CableType>().FirstOrDefault(x => x.TypeName == args[MainWindow.cabLAYER_index]);

        //        //            uow.CommitChanges();
        //        //            tinfo = sub.CableName;
        //        //            ret = sub.Oid;

        //        //        }
        //        //        return ret;
        //        //    }
        //        //    catch (Exception ex)
        //        //    {
        //        //        NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{ex}");
        //        //        //throw;
        //        //        tinfo = ex.Message;
        //        //        return Guid.Empty;
        //        //    }
        //        //}


        //        public static PoleType GetPoleType(UnitOfWork uow, string name)
        //        {
        //            PoleType ret = null;

        //            ret = uow.Query<PoleType>().FirstOrDefault(x => x.TypeName == name);
        //            if (ret == null)
        //            {
        //                return new PoleType(uow)
        //                {
        //                    TypeName = name,
        //                    ShowInEngineering = true,
        //                };
        //            }
        //            else return ret;
        //        }


        //        public static JunctionType GetJunctionType(UnitOfWork uow, string name, string sourceType, string dbName)
        //        {
        //            JunctionType ret = null;
        //            string table = "", td = "";
        //            if (sourceType.Equals("PEDESTAL", StringComparison.InvariantCultureIgnoreCase) || sourceType.Equals("HANDHOLE", StringComparison.InvariantCultureIgnoreCase))
        //            {  //use lookup table from csvs to get meaningful type info
        //                NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"sourceType.Equals(\"PEDESTAL\", StringComparison.InvariantCultureIgnoreCase) || sourceType.Equals(\"HANDHOLE\", StringComparison.InvariantCultureIgnoreCase)\n", false);
        //                NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{sourceType.Equals("PEDESTAL", StringComparison.InvariantCultureIgnoreCase)} || {sourceType.Equals("HANDHOLE", StringComparison.InvariantCultureIgnoreCase)})");
        //                table = $"{dbName}_{sourceType}";
        //                NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{table}table");
        //                //should be a # like 5
        //                var str = $"select Code, {(sourceType.Equals("PEDESTAL", StringComparison.InvariantCultureIgnoreCase) ? "TYPE" : "DESCRIPTION")} from  {table} where id ='{name}'";
        //                NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{str}");
        //                var data = uow.ExecuteQuery(str);
        //                if (data.ResultSet.Count() == 1 && data.ResultSet.ElementAt(0).Rows.Length > 0)
        //                {
        //                    name = data?.ResultSet?.ElementAt(0)?.Rows[0]?.Values[0]?.ToString();
        //                    td = data?.ResultSet?.ElementAt(0)?.Rows[0]?.Values[1]?.ToString();
        //                    NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{name }{td}\n name td");
        //                }
        //            }
        //           return NewNetServices.Module.Core.DefaultFields.GetJunctionType(uow, name);
        //              
        //        }


        //        public static LocationStatus CreateDefaultLocationStatus(UnitOfWork nestedUow)
        //        {
        //            return new LocationStatus(nestedUow)
        //            {
        //                StatusName = defaultStatusName
        //            };
        //        }

        //        public static WorkOrder GetWorkOrder(UnitOfWork uow, string wonumber)
        //        {
        //            var wo = uow.Query<WorkOrder>().FirstOrDefault(x => x.OrderNumber == wonumber);
        //            return wo != null ? wo : new WorkOrder(uow) { OrderNumber = wonumber };
        //        }
        //        public static Route GetRoute(UnitOfWork uow, string id)
        //        {
        //            var wo = uow.Query<Route>().FirstOrDefault(x => x.Name == id);
        //            return wo != null ? wo : new Route(uow) { Name = id };
        //        }
        //        public static Boundary GetBoundary(UnitOfWork uow, string id)
        //        {
        //            return null;
        //            //var wo = uow.Query<Boundary>().FirstOrDefault(x => x.Name == id);
        //            //return wo != null ? wo : new Boundary(uow) { Name = id, Type = GetType<BoundaryType>(uow, "City") };
        //        }
        //        public static Location GetLocation(UnitOfWork nestedUow, string str)
        //        {
        //            int strid; int.TryParse(str, out strid);
        //            var loc = nestedUow.Query<Location>().FirstOrDefault(x => x.ExternalSystemId == strid);
        //            if (loc != null)
        //            {
        //                return loc;
        //            }
        //            else if ((loc = nestedUow.Query<Location>().FirstOrDefault(x => x.LocationName == "UNKNOWN")) != null)
        //            {

        //                return loc;
        //            }
        //            else
        //            {
        //                loc = new Location(nestedUow)
        //                {
        //                    LocationName = "UNKNOWN",
        //                    Status = NewNetServices.Module.BusinessObjects.Core.GlobalSystemSettings.GetInstanceFromDatabase(nestedUow).DefaultLocationStatusAvailable
        //                };
        //            }
        //            nestedUow.CommitChanges();
        //            return loc;
        //        }
        //        public static CableClass GetCableClass(UnitOfWork uow, string str)
        //        {
        //            object locker = new object();
        //            var cc = uow.Query<CableClass>().FirstOrDefault(x => x.TypeName == str);
        //            if (cc != null) return cc;
        //            else
        //            {
        //                lock (locker)
        //                {
        //                    cc= new CableClass(uow) { TypeName = str };
        //                    uow.CommitChanges();
        //                }
        //                return cc;
        //            }
        //        }
        //        public static CableSize GetCableSize(UnitOfWork uow, string str)
        //        {
        //            object locker = new object();
        //           
        //            var cc = uow.Query<CableSize>().FirstOrDefault(x => x.Count.ToString() == str || x.Code == str);
        //            if (cc != null) return cc;
        //            else
        //            {
        //                lock (locker)
        //                {
        //                    cc = new CableSize(uow) { Code = str, Count = int.TryParse(str, out int c) ? c : 0 };
        //                    uow.CommitChanges();
        //                }
        //                return cc;
        //            } 
        //        }

        //public object GetObject<T>(UnitOfWork uow, System.Type objType,CriteriaOperator criteria)where T:BaseObjectDateTimeStamps
        //{
        //    ITypesInfo iti = XpoTypesInfoHelper.GetTypesInfo();
        //return   uow.FindObjectAsync(objType, criteria);
        //     
        //}
        //public object GetObject(UnitOfWork uow,System.Type objType, string field, object value )
        //{
        //  
        //    if ()
        //}
        public class ProgressMadeEventArgs : EventArgs
        {
            public String Error { get; set; }

            public ImportedItem I = null;
            public ProgressMadeEventArgs(ImportedItem item)
            {
                I = item;
            }
        }
    }//end imported helper
    enum ErrorDescription
    {
        LocationNotFound
    }
}


//private void HandleStep2<T>()
//    where T : BaseObjectDateTimeStamps, IEngineeringEntity
//{
//    var tnames = typeof(T) == typeof(Location)
//        ? new string[]
//        {
//            junkTable,
//            subTable
//        }
//        : typeof(T) == typeof(Cable)
//            ? new string[]
//            {
//                cabTable
//            }
//            : new string[]
//            {
//                conTable
//            };
//    Console.WriteLine("Start of HandleStep2");
//    var tasks = new List<Task>();
//    List<string[]> oraclerows_big = new List<string[]>();
//    using (var odw = new OracleDatabaseWorker(tbOracleConnectionStringText))
//    {
//        oraclerows_big = odw.GetAllCmsMunsysItemsForTable($"SP_NN_{typeof(T).Name}", "ORIG_ID", "GID", "GUID");
//    }
//    Dispatcher.Invoke(() => ProgressBar.Maximum += oraclerows_big.Count);
//    var oparts = oraclerows_big.Partition(oraclerows_big.Count / 5 + 1);
//    Parallel.ForEach(oparts,
//                     new Action<IEnumerable<string[]>>((oraclerows) => // foreach (var oraclerows in oparts)
//    {
//        using (var uow = new UnitOfWork(Tsdl))
//        {
//            for (int i = 0; i < oraclerows.Count(); i++)
//            {
//                try
//                {
//                    var row = oraclerows.ElementAt(i);
//                    BaseObjectDateTimeStamps loc = null;

//                    foreach (var x in uow.Query<BaseObjectDateTimeStamps>()
//                        .Where(x => x.ExternalSystemId.HasValue && x.SourceTable != string.Empty))
//                    {
//                        if (x.ExternalSystemId.HasValue && int.TryParse(row[0], out int row0) &&
//                            x.ExternalSystemId == row0)
//                        {
//                            StaticHelperMethods.WriteOut($"externalids match \n{x.ExternalSystemId}==={int.Parse(row[0])}?");
//                            if (tnames.Contains(x.SourceTable))
//                            {
//                                loc = x;
//                                break;
//                            }
//                        }
//                    }
//                    //loc = (from x in uow.Query<BaseObjectDateTimeStamps>()
//                    //       where
//                    //      x.ExternalSystemId.HasValue && x.ExternalSystemId.Value.ToString().Replace(",", "").Equals(row[0], StringComparison.InvariantCultureIgnoreCase) &&
//                    //      tnames.Contains(x.SourceTable)
//                    //       select x)?.First();
//                    //set our handle to the gid and oracle to our oid
//                    if (loc != null && loc.GetType().GetProperties().Any(x => x.Name == "Handle"))
//                    {
//                        Console.WriteLine($"{string.Join(" | | ", row)} >> PASS");
//                        loc.SetMemberValue("Handle", row[1]);
//                        uow.CommitChanges();
//                        row[2] = loc.Oid.ToString().ToUpper();
//                        using (var odw = new OracleDatabaseWorker(tbOracleConnectionStringText))
//                        {
//                            odw.SetGuidFromGidOracle($"SP_NN_{typeof(T).Name.ToUpper()}", row[1], row[2]);
//                        }
//                        ProgressMade?.Invoke($"{i}:  Id {row[0]} {"Success"}",
//                                             new ProgressMadeEventArgs(new ImportedItem()
//                                             {
//                                                 Type = "Handle Swap",
//                                                 ImportStatus = "Success"
//                                             }));
//                        continue;
//                    }
//                    ProgressMade?.Invoke($"{i}:  Id {row[0]} Not found ",
//                                         new ProgressMadeEventArgs(new ImportedItem()
//                                         {
//                                             Type = "Not Found",
//                                             ImportStatus = "Exception" + $"\tNot Found in:{string.Join("\t ", tnames)}"
//                                         }));

//                    Console.WriteLine($"{string.Join(" | _ | ", row)} >> FAIL");
//                    Console.WriteLine(row[0] + "FAILED");
//                }
//                catch (Exception ex)
//                {
//                    ProgressMade?.Invoke($"X",
//                                         new ProgressMadeEventArgs(new ImportedItem()
//                                         {
//                                             Type = "Not Found",
//                                             ImportStatus = "Exception" + $"\t {ex}"
//                                         }));

//                    Console.WriteLine($"{ex}");
//                    Console.WriteLine("Exception" + "\n" + ex);
//                }
//            } //end for
//            uow.CommitChanges();
//        } //end uow
//    } //end foreac
//    ));
//}

//private void HandleStep2<T>(bool singlle)
//    where T : BaseObjectDateTimeStamps, IEngineeringEntity
//{
//    using (var odw = new OracleDatabaseWorker(tbOracleConnectionStringText))
//    {
//        var oraclerows_big = odw.GetAllCmsMunsysItemsForTable($"SP_NN_{typeof(T).Name}",
//                                                             -1,
//                                                             "ORIG_ID",
//                                                             "GID",
//                                                             "GUID");
//        Dispatcher.Invoke(() => ProgressBar.Maximum += oraclerows_big.Count);
//        var oparts = oraclerows_big.Partition(oraclerows_big.Count / 5 + 1);
//        foreach (var oraclerows in oparts)
//        {
//            using (var uow = new UnitOfWork(Tsdl))
//            {
//                for (int i = 0; i < oraclerows.Count(); i++)
//                {
//                    StaticHelperMethods.WriteOut($"{i}");
//                    try
//                    {
//                        var row = oraclerows.ElementAt(i);
//                        BaseObjectDateTimeStamps loc = null;
//                        var tnames = typeof(T) == typeof(Location)
//                            ? new string[]
//                            {
//                                junkTable,
//                                subTable
//                            }
//                            : typeof(T) == typeof(Cable)
//                                ? new string[]
//                                {
//                                    cabTable
//                                }
//                                : new string[]
//                                {
//                                    conTable
//                                };
//                        foreach (var x in uow.Query<BaseObjectDateTimeStamps>())
//                        {
//                            if (x.ExternalSystemId.HasValue &&
//                                x.ExternalSystemId.Value.ToString().Replace(",", string.Empty) == row[0])
//                            {
//                                if (tnames.Contains(x.SourceTable))
//                                {
//                                    loc = x;
//                                    break;
//                                }
//                            }
//                        }
//                        //loc = (from x in uow.Query<BaseObjectDateTimeStamps>()
//                        //       where
//                        //      x.ExternalSystemId.HasValue && x.ExternalSystemId.Value.ToString().Replace(",", "").Equals(row[0], StringComparison.InvariantCultureIgnoreCase) &&
//                        //      tnames.Contains(x.SourceTable)
//                        //       select x)?.First();
//                        //set our handle to the gid and oracle to our oid
//                        if (loc != null)
//                        {
//                            StaticHelperMethods.WriteOut($"{string.Join(" | | ", row)} >> PASS");
//                            loc.SetMemberValue("Handle", row[1]);
//                            row[2] = loc.Oid.ToString().ToUpper();
//                            odw.SetGuidFromGidOracle($"SP_NN_{typeof(T).Name.ToUpper()}", row[1], row[2]);
//                            ProgressMade?.Invoke(null,
//                                                 new ProgressMadeEventArgs(new ImportedItem()
//                                                 {
//                                                     Id = row[0],
//                                                     SourceTable = loc.SourceTable,
//                                                     ImportStatus = "Success"
//                                                 }));
//                            continue;
//                        }
//                        StaticHelperMethods.WriteOut($"{string.Join(" | _ | ", row)} >> FAIL");
//                        ProgressMade?.Invoke(null,
//                                             new ProgressMadeEventArgs(new ImportedItem()
//                                             {
//                                                 Id = row[0],
//                                                 SourceTable = " ????",
//                                                 ImportStatus = "Exception"
//                                             }));
//                    }
//                    catch (Exception ex)
//                    {
//                        StaticHelperMethods.WriteOut($"{ex}");
//                        ProgressMade?.Invoke(null,
//                                             new ProgressMadeEventArgs(new ImportedItem()
//                                             {
//                                                 SourceTable = " !!!!!!",
//                                                 ImportStatus = "Exception"
//                                             }));
//                    }
//                } //end for
//                uow.CommitChanges();
//            } //end uow
//        } //end foreac
//    } //end odw 
//}

