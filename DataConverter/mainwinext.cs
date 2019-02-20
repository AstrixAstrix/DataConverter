﻿
using DataConverter.Classes;
using DevExpress.ExpressApp;
using DevExpress.Xpf.Core;
using DevExpress.Xpo;
using NewNetServices.Module.BusinessObjects.CableManagement;
using NewNetServices.Module.BusinessObjects.Core;
using NewNetServices.Module.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using static DataConverter.Classes.ImporterHelper;
using Task = System.Threading.Tasks.Task;

namespace DataConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : DXWindow
    {


        public int GetSizeForPartition(int collectionSize)
        {
            //would like to have 50 or so tasks per collection so
            int numtasks = 50;
            while (collectionSize / numtasks < 1)
                numtasks--;
            return collectionSize / numtasks;
        }

        #region MainWorkerMethods
        public async Task AssignmentDPairs()
        {
            var stepName = "Assignment -> Designation Pairs";//new StackTrace().GetFrame(0).GetMethod().Name;// "Assignment DPairs";

            //this is so that the xpobject type is created  on one thread
            Func<List<Dictionary<string, string>>, List<List<Dictionary<string, string>>>> func = (inlist) =>
            {
                List<List<Dictionary<string, string>>> ret = new List<List<Dictionary<string, string>>>();
                var subDataParts = inlist.Partition(GetSizeForPartition(inlist.Count));
                subDataParts.ForEach((subdata) => ret.Add(subdata.ToList()));

                return ret;
            };
            Action<List<Dictionary<string, string>>> processorAction = (inlist) =>
            {
                using (var uow = new UnitOfWork(Tsdl))
                {
                    inlist.ForEach((row) =>
        {
            if (Skip > 0)
            {
                Skip--;
                return;
            }
            uow.BeginTransaction();
            //"ID", "DESIGNATEDPAIR"
            try
            {
                if (!string.IsNullOrWhiteSpace(row["ID"]) &&
    !string.IsNullOrWhiteSpace(row["DESIGNATEDPAIR"]))
                {
                    var assign = uow.Query<Assignment>()
        .FirstOrDefault(x => x.ExternalSystemId.ToString() == row["ID"] && /*
make aure not olt port*/
           x.SourceTable == splitterPortsTable);
                    var dp = uow.Query<DesignationPair>()
        .FirstOrDefault(x => x.ExternalSystemId.ToString() == row["DESIGNATEDPAIR"]);
                    if (assign != null & dp != null)
                    {
                        assign.DesignatedPairs.Add(dp);
                        uow.CommitTransaction();
                        uow.CommitChanges();
                        currentSuccess++;
                        ProgressMade?.Invoke(stepName,
                                new ProgressMadeEventArgs(new ImportedItem()
                                {
                                    ImportStatus = "Success"
                                }));
                    }
                    else
                    {
                        throw new Exception($"Bad Data assignment {row["ID"]} dpair {row["DESIGNATEDPAIR"]}");
                    }
                }
                else uow.RollbackTransaction();
            }
            catch (Exception ex)
            {
                currentErrors++;
                uow.RollbackTransaction();
                ProgressMade?.Invoke(stepName,
                new ProgressMadeEventArgs(new ImportedItem()
                {
                    ImportStatus = $"Exception {ex}"
                }));
            }
        });
                }//end using

            };//end action
            WorkBeginning?.Invoke(stepName, null);
            await ProcessData(assignmentDPairsTable, assignmentDPairsCols, func, processorAction);
            WorkCompleted?.Invoke(stepName, null);

        }
        public async Task AssignmentOlt()
        {
            var stepName = "Assignment -> OLTSs";// new StackTrace().GetFrame(0).GetMethod().Name;// "Assignment->OLT ";

            //this is so that the xpobject type is created  on one thread
            Func<List<Dictionary<string, string>>, List<List<Dictionary<string, string>>>> func = (inlist) =>
            {
                List<List<Dictionary<string, string>>> ret = new List<List<Dictionary<string, string>>>();
                var subDataParts = inlist.Partition(GetSizeForPartition(inlist.Count));
                subDataParts.ForEach((subdata) => ret.Add(subdata.ToList()));

                return ret;
            };
            Action<List<Dictionary<string, string>>> processorAction = (inlist) =>
            {
                using (var uow = new UnitOfWork(Tsdl))
                {
                    inlist.ForEach((row) =>
        {

            uow.BeginTransaction();
            //"ID" assignmentid?, "OLTPORTID"
            //"ID" assignmentid?, "OLTPORTID"
            try
            {
                if (!string.IsNullOrWhiteSpace(row["ID"]) &&
    !string.IsNullOrWhiteSpace(row["OLTPORTID"]))
                {
                    var sp = uow.Query<Port>()
         .FirstOrDefault(x => x.ExternalSystemId.ToString() == row["OLTPORTID"] && /*
make aure not olt port*/
             x.SourceTable == splitterPortsTable);
                    var assign = uow.Query<Assignment>()
         .FirstOrDefault(x => x.ExternalSystemId.ToString() == row["ID"]);
                    if (sp != null & assign != null)
                    {
                        assign.Resources.Add(sp);
                        uow.CommitTransaction();
                        uow.CommitChanges();
                        currentSuccess++;
                        ProgressMade?.Invoke(stepName,
                                 new ProgressMadeEventArgs(new ImportedItem()
                                 {
                                     ImportStatus = "Success"
                                 }));
                    }
                    else
                    {
                        throw new Exception($"Bad Data  ");
                    }
                }
                else uow.RollbackTransaction();
            }
            catch (Exception ex)
            {
                currentErrors++;
                uow.RollbackTransaction();
                ProgressMade?.Invoke(stepName,
                  new ProgressMadeEventArgs(new ImportedItem()
                  {
                      ImportStatus = $"Exception {ex}"
                  }));
            }
        });
                }//end using

            };//end action
            WorkBeginning?.Invoke(stepName, null);
            await ProcessData(assignmentOltTable, assignmentOltCols, func, processorAction);
            WorkCompleted?.Invoke(stepName, null);

        }

        public async Task AssignmentSplitPort()
        {
            var stepName = "Assignment -> Splitter Ports";// new StackTrace().GetFrame(0).GetMethod().Name;// "Assignment->SplitterPorts";

            //this is so that the xpobject type is created  on one thread
            Func<List<Dictionary<string, string>>, List<List<Dictionary<string, string>>>> func = (inlist) =>
            {
                List<List<Dictionary<string, string>>> ret = new List<List<Dictionary<string, string>>>();
                var subDataParts = inlist.Partition(GetSizeForPartition(inlist.Count));
                subDataParts.ForEach((subdata) => ret.Add(subdata.ToList()));

                return ret;
            };
            Action<List<Dictionary<string, string>>> processorAction = (inlist) =>
            {
                using (var uow = new UnitOfWork(Tsdl))
                {
                    inlist.ForEach((row) =>
        {
            if (Skip > 0)
            {
                Skip--;
                return;
            }
            uow.BeginTransaction();
            //"ID" assignmentid?, "SPLITTERID"
            //"ID" assignmentid?, "SPLITTERID"
            try
            {
                if (!string.IsNullOrWhiteSpace(row["ID"]) &&
    !string.IsNullOrWhiteSpace(row["SPLITTERID"]))
                {
                    var sp = uow.Query<Port>()
         .FirstOrDefault(x => x.ExternalSystemId.ToString() == row["SPLITTERID"] && /*
make aure not olt port*/
             x.SourceTable == splitterPortsTable);
                    var assign = uow.Query<Assignment>()
         .FirstOrDefault(x => x.ExternalSystemId.ToString() == row["ID"]);
                    if (sp != null & assign != null)
                    {
                        assign.Resources.Add(sp);
                        uow.CommitTransaction();
                        uow.CommitChanges();
                        currentSuccess++;
                        ProgressMade?.Invoke(stepName,
                                 new ProgressMadeEventArgs(new ImportedItem()
                                 {
                                     ImportStatus = "Success"
                                 }));
                    }
                    else
                    {
                        throw new Exception($"Bad Data  ");
                    }
                }
                else uow.RollbackTransaction();
            }
            catch (Exception ex)
            {
                currentErrors++;
                uow.RollbackTransaction();
                ProgressMade?.Invoke(stepName,
                  new ProgressMadeEventArgs(new ImportedItem()
                  {
                      ImportStatus = $"Exception {ex}"
                  }));
            }
        });
                }//end using

            };//end action
            WorkBeginning?.Invoke(stepName, null);
            await ProcessData(assignmentSplitPortTable, assignmentSplitPortCols, func, processorAction);
            WorkCompleted?.Invoke(stepName, null);

        }
        public async Task AssignmentsPrimaryLocations()
        {
            var stepName = "Assignments and rimary Locations";//new StackTrace().GetFrame(0).GetMethod().Name;// "Assignments and Primary Locations";

            List<List<Dictionary<string, string>>> Func(List<Dictionary<string, string>> inlist)
            {
                List<List<Dictionary<string, string>>> ret = new List<List<Dictionary<string, string>>>();
                foreach (var list in inlist.Partition(GetSizeForPartition(inlist.Count)))
                {
                    ret.Add(list.ToList());
                }
                return ret;
            }
            //action to take on eas=ch task data

            Action<List<Dictionary<string, string>>> processorAction = (inlist) =>
            {///                   ask ANDREW about ports TODO:
                //"", " ", "ASSIGNMENTPORT", " ", "", "", "SUBSCRIBERID"

                foreach (var row in inlist)
                {
                    using (var uow = new UnitOfWork(Tsdl))
                    {
                        uow.BeginTransaction();
                        if (!uow.Query<Assignment>().Any(x => x.ExternalSystemId.ToString() == row["ID"]))
                        {
                            try
                            {
                                var assignment = new Assignment(uow);
                                assignment.ExternalSystemId = int.TryParse(row["ID"], out int aid) ? aid : 0;
                                assignment.AssignmentClass = !string.IsNullOrWhiteSpace(row["ASSIGNMENTCLASS"])
                                    ? uow.Query<AssignmentClass>()
                                        .FirstOrDefault(x => x.Class == row["ASSIGNMENTCLASS"]) : null;
                                assignment.Status = !string.IsNullOrWhiteSpace(row["STATUS"])
                                    ? uow.Query<AssignmentStatus>()
                                        .FirstOrDefault(x =>
                                            x.StatusName == (row["STATUS"] == "2"
                                                ? GlobalSystemSettings.AssignmentStatusActive
                                                : row["STATUS"]))
                                    : uow.Query<AssignmentStatus>().FirstOrDefault(x => x.StatusName == "UNKNOWN");
                                if (!string.IsNullOrWhiteSpace(row["EFFECTIVEDATE"]))
                                    assignment.EffectiveDate = DateTime.TryParse(row["EFFECTIVEDATE"],
                                                                         out DateTime dt)
                                ? dt
                                : DateTime.Now;
                                assignment.CircuitID = row["CIRCUITID"];
                                assignment.PrimaryLocation = uow.Query<Subscriber>()
                            .FirstOrDefault(x => x.ExternalSystemId.ToString() == row["SUBSCRIBERID"]);
                                assignment.Type = uow.Query<AssignmentType>().FirstOrDefault(x => x.TypeName == UNK);
                                uow.CommitTransaction();
                                uow.CommitChanges();
                                currentSuccess++;
                                ProgressMade?.Invoke(stepName,
                                             new ImporterHelper.ProgressMadeEventArgs(new ImportedItem()
                                             {
                                                 SourceTable = assignmentPrimlocTable,
                                                 ImportStatus = "Success",
                                                 Type = "Assignment primary locations"
                                             }));
                            }
                            catch (Exception ex)
                            {
                                uow.RollbackTransaction();
                                currentErrors++;
                                ProgressMade?.Invoke(stepName,
                                             new ImporterHelper.ProgressMadeEventArgs(new ImportedItem()
                                             {
                                                 SourceTable = assignmentPrimlocTable,
                                                 ImportStatus = "Exception" + $"\t{ex.Message}",
                                                 Type = "Assignment primary locations"
                                             }));
                                NewNetServices.Module.Core.StaticHelperMethods
                            .WriteOut($"{ex}\ndatarow : " +
                            string.Join("\t | \t", row.Select(x => $"{x.Key}:{x.Value}")));
                            }
                        }
                    } //endusing
                }

            };


            WorkBeginning?.Invoke(stepName, EventArgs.Empty);
            await ProcessData(this.assignmentPrimlocTable, this.assignmentPrimlocCols, Func, processorAction);
            WorkCompleted?.Invoke(stepName, null);

        }
        public async Task CableCallouts()
        {
            var stepName = "Call outs";// new StackTrace().GetFrame(0).GetMethod().Name;// "Cable Callouts";

            List<KeyValuePair<Guid, List<Guid>>> datalist = new List<KeyValuePair<Guid, List<Guid>>>();
            using (var uow = new UnitOfWork(Tsdl))
            {
                foreach (var cab in uow.Query<Cable>())
                {
                    if (Skip > 0)
                    {
                        Skip--;
                        break;
                    }
                    datalist.Add(new KeyValuePair<Guid, List<Guid>>(cab.Oid,
                                                     cab.CablePairs
                    .OrderBy(x => x.PairNumber)
                    .Select(x => x.Oid)
                    .ToList()));
                }
            }
            Action<KeyValuePair<Guid, List<Guid>>> getCalloutActionact = (indata) =>


            {
                using (var uow = new UnitOfWork(Tsdl))
                {
                    KeyValuePair<Guid, List<Guid>> data = indata;
                    int lowlevel = 0, highlevel = 0, ceiling = data.Value.Count;
                    var cab = uow.GetObjectByKey<Cable>(data.Key);
                    try
                    {
                        foreach (var item in data.Value)
                        {
                            uow.BeginTransaction();

                            var cp = uow.GetObjectByKey<CablePair>(item);//cab.CablePairs.OrderBy(x => x.PairNumber)


                            string dgroupName = string.Empty;

                            if (cp.DesignationPair != null)
                            {
                                if (string.IsNullOrWhiteSpace(dgroupName))
                                {
                                    dgroupName = cp.DesignationPair.DesignationGroup?.CableName;
                                }
                                else
                                {
                                    if (dgroupName != cp.DesignationPair.DesignationGroup?.CableName)
                                    {
                                        throw new UserFriendlyException($"Designation Groups do not match {dgroupName} -> {cp.DesignationPair.DesignationGroup?.CableName}!!");
                                    }
                                }
                                if (lowlevel == 0) //then start range
                                {
                                    lowlevel = cp.PairNumber;
                                }

                                highlevel = cp.PairNumber;
                            }
                            else break; //only car about contiguous connections
                        }//endfor

                        string co = cab.CableName +
                        $" ({lowlevel}, {highlevel}) /XD {((highlevel) >= ceiling ? "--" : "(" + (highlevel + 1).ToString() + " - " + ceiling + ")")}";
                        cab.CallOut1 = co;
                        uow.CommitTransaction();
                        uow.CommitChanges();
                        currentSuccess++;
                        ProgressMade?.Invoke(stepName,
                     new ProgressMadeEventArgs(new ImportedItem()
                     {
                         ImportStatus = "Success",
                         Type = "Cable Callout",
                         RecordStatus = co
                     }));
                    }
                    catch (Exception ex)
                    {
                        uow.RollbackTransaction();
                        currentErrors++;
                        ProgressMade?.Invoke(stepName,
                     new ProgressMadeEventArgs(new ImportedItem()
                     {
                         ImportStatus = "Exception",
                         Type = "Cable Callout",
                         RecordStatus = ex.Message
                     }));
                    }
                }//using
            };//action 

            //action to take on eas=ch task data
            WorkBeginning?.Invoke(stepName, null);

            await Task.WhenAll(Task.Run(() => datalist.ForEach(getCalloutActionact)));
            WorkCompleted?.Invoke(stepName, null);

        }

        string cpairfilepath = @"E:\DataConverter\DataConverter\cpairs.sql";
        string dpairfilepath = @"E:\DataConverter\DataConverter\dpairs.sql";
        public async Task CablePairs()
        {
            var stepName = "CablePairs";// new StackTrace().GetFrame(0).GetMethod().Name;// "Cable Pairs";

            using (var uow = new UnitOfWork(Tsdl))
            {
                try
                {
                    var before = uow.Query<PhysicalPair>().Count();
                    NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"before {before}");
                    string sql = File.ReadAllText(cpairfilepath);
                    NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"\n\nCPAIRSQL\n\n{sql}");
                    WorkBeginning?.Invoke(stepName, EventArgs.Empty);

                    await Task.FromResult(uow.ExecuteNonQuery(sql));
                    WorkCompleted?.Invoke(stepName, EventArgs.Empty);

                    var after = uow.Query<PhysicalPair>().Count();
                    NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"before {before} after {after}");
                }
                catch (Exception ex)
                {
                    NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{ex}");
                }
            }

            //    try
            //    {
            //        CreateFileWithColumnHeaders(cpairsexeptionlist, cpColumns);

            //    }
            //    catch (Exception e)
            //    {
            //        Console.WriteLine(e);
            //        // throw;
            //    }
            //   
            //    Func<List<Dictionary<string, string>>,
            //    List<List<Dictionary<string, string>>>> func =
            //    (inlist) =>
            //    {
            //        List<string> distinctablelist = null;
            //        using (var odw = new OracleDatabaseWorker(tbOracleConnectionStringText))
            //        {
            //            distinctablelist = odw.GetDistinctDataColumn(CABLEPAIR_Table, "CABLE");
            //        }
            //        //list of each cable in this data set

            //        var ret = new List<List<Dictionary<string, string>>>();
            //        double MAX = ((double)inlist.Count / 50.0) > 1 ? ((double)inlist.Count / 50.0) : (double)inlist.Count;
            //        List<Dictionary<string, string>> templist = new List<Dictionary<string, string>>();
            //        //have each cable's pairs handled on the same thread
            //        for (int i = 0; i < distinctablelist.Count; i++)
            //        {
            //            string cableid = distinctablelist[i];
            //            templist.AddRange(inlist.Where(x => x["CABLE"] ==
            //                cableid)
            //.Select(x => x)
            //.ToList());
            //            if (templist.Count > MAX || i == distinctablelist.Count - 1)
            //            {
            //                ret.Add(templist);
            //                templist = new List<Dictionary<string, string>>();
            //            }
            //        }
            //        return ret;
            //    };
            //    Action<List<Dictionary<string, string>>> processorAction = (inlist) =>
            //    {
            //        NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"inList partition {inlist.Count} items");
            //        using (var uow = new UnitOfWork(Tsdl))
            //        {
            //            foreach (var row in inlist)
            //            {

            //                if (!uow.Query<PhysicalPair>().Any(x => x.ExternalSystemId.ToString() == row["ID"]))
            //                {
            //                    object locker = new object();
            //                    try
            //                    {
            //                        //        //do a trans action and roll back if necessary

            //                        if (!string.IsNullOrWhiteSpace(row["CABLE"]))
            //                        {
            //                            try
            //                            {
            //                                var cable = uow.Query<PhysicalCable>() //look up cable we probably impmorted already//{
            //                        .FirstOrDefault(x => x.ExternalSystemId.ToString() == row["CABLE"]);
            //                                if (cable != null)
            //                                {
            //                                    var cpair = new PhysicalPair(uow);

            //                                    cpair.ExternalSystemId = int.Parse(row["ID"]);
            //                                    cpair.SourceTable = CABLEPAIR_Table; // int.Parse(row["LOCATIONID"]);  
            //                                    cpair.Status = !string.IsNullOrWhiteSpace(row["STATUS"])
            //                                        ? uow.Query<CablePairStatus>()
            //                                            .FirstOrDefault(x => x.StatusName == row["STATUS"])
            //                                        : null;
            //                                    cpair.PairNumber = int.Parse(row["NUM"]);
            //                                    //store guid and add to cable afterwards to avoid locking issues
            //                                    //lock (locker)
            //                                    //{
            //                                    //    cable_cpairs_List.Add(new Tuple<Guid, Guid>(cable.Oid, cpair.Oid));
            //                                    cable.CablePairs.Add(cpair);
            //                                    cpair.Cable = cable;
            //                                    //}
            //                                    uow.CommitChanges();
            //                                    currentSuccess++;
            //                                    ProgressMade?.Invoke(stepName,
            //                                             new ImporterHelper.ProgressMadeEventArgs(new ImportedItem()
            //                                             {
            //                                                 Guid = cpair.Oid.ToString(),
            //                                                 Id = cpair.ExternalSystemId?.ToString(),
            //                                                 SourceTable = cpair.SourceTable,
            //                                                 ImportStatus = "Success",
            //                                                 RecordStatus = cpair.Status?.ToString(),
            //                                                 Type = "CablePair"
            //                                             }));
            //                                    continue;
            //                                }

            //                                NewNetServices.Module.Core.StaticHelperMethods.WriteOut(
            //                                    $"NO CABLE {row["CABLE"]}");
            //                                ProgressMade?.Invoke(stepName,
            //                                    new ImporterHelper.ProgressMadeEventArgs(new ImportedItem()
            //                                    {
            //                                        ImportStatus = "CABLE doesn't exist " + row["CABLE"],
            //                                        Type = "CablePair"
            //                                    }));
            //                                // }
            //                            }
            //                            catch (Exception ex)
            //                            {
            //                                currentErrors++;
            //                                NewNetServices.Module.Core.StaticHelperMethods
            //                        .WriteOut(@" 
            //        puow.CommitChanges();
            //        errorsCpairs++\n" +
            //                        $"{string.Join(", ", row)}" +
            //                        $"\n{ex.Source}" +
            //                        $"\n{ex.TargetSite}");
            //                                NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{string.Join(",", row.Select(x => x.Value).ToArray())}\n", cpairsexeptionlist);
            //                            }
            //                        }
            //                        else
            //                        {
            //                            NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"NO CABLE {row["CABLE"]}");
            //                            ProgressMade?.Invoke(stepName,
            //                                new ImporterHelper.ProgressMadeEventArgs(new ImportedItem()
            //                                {

            //                                    ImportStatus = "NOCABLE",
            //                                    Type = "CablePair"
            //                                }));
            //                            NewNetServices.Module.Core.StaticHelperMethods
            //                    .WriteOut(@"  uow.RollbackTransaction();
            //throw new Exception($""Cable not found with id: {row[""CABLE ""]}"); //{ 
            //                            throw new Exception($"Cable not found with id: {row["CABLE "]}"); //{
            //                        }
            //                    }
            //                    catch (Exception ex)
            //                    {
            //                        NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{string.Join(",", row.Select(x => x.Value).ToArray())}\n", cpairsexeptionlist);
            //                        currentErrors++;
            //                        ProgressMade?.Invoke(stepName,
            //                              new ImporterHelper.ProgressMadeEventArgs(new ImportedItem()
            //                              {
            //                                  SourceTable = CABLEPAIR_Table,
            //                                  ImportStatus = "Exception" + $"\t{ex.Message}",
            //                                  Type = "Exception"
            //                              }));
            //                        NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{ex}");
            //                    }

            //                }
            //            }//for
            //        }//using
            //    };

            //    WorkBeginning?.Invoke(stepName, EventArgs.Empty);
            //    await ProcessData(CABLEPAIR_Table, cpColumns, func, processorAction);
            //    WorkCompleted?.Invoke(stepName, EventArgs.Empty);

        }

        public async Task Cables()
        {
            var stepName = $"Cabes and pairs w/o spans";//new StackTrace().GetFrame(0).GetMethod().Name;// "Cables";

            //this is so that the xpobject type is created  on one thread
            Func<List<Dictionary<string, string>>,
            List<List<Dictionary<string, string>>>> func =
            (inlist) =>
            {
                List<List<Dictionary<string, string>>> ret = new List<List<Dictionary<string, string>>>();
                var cableparts = inlist.Partition(GetSizeForPartition(inlist.Count));
                cableparts.ForEach((subdata) => ret.Add(subdata.ToList()));

                return ret;
            };
            Action<List<Dictionary<string, string>>> processorAction = async (cabdata) =>
             {

                 using (UnitOfWork uow = new UnitOfWork(Tsdl))
                 {
                     //do work
                     cabdata.ForEach(async (row) =>
                    {

                        bool test = false;
                        //StaticHelperMethods.WriteOut($"{string.Join("\t", row.Select(x => x.Key + ":" + x.Value))}");
                        //make sure not imported already
                        test = (!uow.Query<Cable>()
                         .Any(x => x.ExternalSystemId.ToString() == row["CABLEID"]));
                        if (test)
                        {
                            PhysicalCable cable = new PhysicalCable(uow);
                            cable.Source = uow.Query<Location>().FirstOrDefault(x => x.ExternalSystemId.ToString() == row["SOURCELOCATIONID"]);
                            cable.Destination = uow.Query<Location>().FirstOrDefault(x => x.ExternalSystemId.ToString() == row["DESTINATIONLOCATIONID"]);
                            //                          ImporterHelper.ProcessCable(uow,
                            //row,
                            //cabTable,
                            //ProgressMade,
                            //stepName);
                            using (var odw = new OracleDatabaseWorker(tbOracleConnectionStringText))
                            {
                                //var cpairs = odw.GetData(CABLEPAIR_Table, $" where CABLEID = '{row["CABLEID"]}'", $"Cabes and pairs w/o spans");
                                //var cptask = ImporterHelper.DoCablePairs(uow, cable.ExternalSystemId, CABLEPAIR_Table, cpColumns);
                                var pc = ImporterHelper.ProcessCable(cable, row, cabTable, ProgressMade, $"Cabes and pairs w/o spans");
                                //await Task.WhenAll(cptask, pc);
                                //var pairs = await Task.FromResult(cptask);
                                //pairs.Result.ForEach((p) =>
                                //{
                                //    cable.CablePairs.Add(p);
                                //    p.Cable = cable;
                                //});
                            }
                            uow.CommitChanges();
                            //currentSuccess++;
                            //ProgressMade?.Invoke(stepName,
                            //    new ProgressMadeEventArgs(new ImportedItem()
                            //    {
                            //        SourceTable = cabTable,
                            //        ImportStatus = "OK",
                            //        Type = $"Cabes and pairs w/o spans"
                            //    }));
                        }
                        else
                        {
                            //  successfulCable++;
                            ProgressMade?.Invoke(stepName,
                                 new ProgressMadeEventArgs(new ImportedItem()
                                 {
                                     SourceTable = cabTable,
                                     ImportStatus = "OK",
                                     Type = $"Already Exists {row["CABLEID"]}"
                                 }));
                        }
                    });
                 } //end uow 

             };
            WorkBeginning?.Invoke(stepName, EventArgs.Empty);
            await ProcessData(cabTable, cabColumns, func, processorAction);
            //await ProcessData(X cabTable, cabColumns, func, processor_action);
            WorkCompleted?.Invoke(stepName, EventArgs.Empty);

        }
        private readonly object locker = new object();
        public async Task CablesWithSpans()
        {
            var stepName = "Cables and pairs with Spans";//new StackTrace().GetFrame(0).GetMethod().Name;// "Cables";

            //this is so that the xpobject type is created  on one thread
            Func<List<Dictionary<string, string>>,
            List<List<Dictionary<string, string>>>> func =
            (inlist) =>
            {
                List<List<Dictionary<string, string>>> ret = new List<List<Dictionary<string, string>>>();
                var grouplist = inlist.GroupBy(x => x["CABLEID"]);
                grouplist.ForEach((subdata) => ret.Add(subdata.ToList()));
                return ret;
            };
            Action<List<Dictionary<string, string>>> processorAction = async (cabdata) =>
             {
                 string cableID = cabdata?[0]["CABLEID"];
                 /*"FORC", "CABLEID", "COMMENTS", "CABLESTATUS", "CABLELENGTH", "CABLEROUTE", "WORKORDERID", "DROPCABLE",
             "CABLETYPE", "CABLECLASS", "CABLESIZE", "SOURCELOCATIONID", "DESTINATIONLOCATIONID", "DESCRIPTION",
             "INSTALLDATE"*/
                 Location MainSource = null;
                 Location MainDestination = null;

                 using (UnitOfWork uow = new UnitOfWork(Tsdl))
                 {

                     Tuple<string, string> SRCDEST;
                     lock (locker)
                     {
                         using (var odw = new OracleDatabaseWorker(tbOracleConnectionStringText))
                         {
                             SRCDEST = odw.GetSingleSourceDestLocationsFromSpanGroupData(cableID);
                             NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"SRCDEST {SRCDEST.Item1} - {SRCDEST.Item2}");
                         }
                     }
                     if (SRCDEST == null) return;
                     string keyB = string.Empty, keyA = string.Empty;
                     if (cabdata.Any(x => x["SOURCELOCATIONID"] == SRCDEST.Item1))
                     {
                         MainSource = uow.Query<Location>().FirstOrDefault(x => x.ExternalSystemId.ToString() == SRCDEST.Item1);
                         NewNetServices.Module.Core.StaticHelperMethods.WriteOut($" MainSource{ MainSource?.ExternalSystemId}");
                         keyA = "SOURCELOCATIONID";
                         MainDestination = uow.Query<Location>().FirstOrDefault(x => x.ExternalSystemId.ToString() == SRCDEST.Item2);
                         NewNetServices.Module.Core.StaticHelperMethods.WriteOut($" MainDestination { MainDestination?.ExternalSystemId}");
                         keyB = "DESTINATIONLOCATIONID";
                     }
                     else
                     {
                         MainDestination = uow.Query<Location>().FirstOrDefault(x => x.ExternalSystemId.ToString() == SRCDEST.Item1);
                         NewNetServices.Module.Core.StaticHelperMethods.WriteOut($" MainDestination { MainDestination?.ExternalSystemId}");
                         keyB = "SOURCELOCATIONID";
                         MainSource = uow.Query<Location>().FirstOrDefault(x => x.ExternalSystemId.ToString() == SRCDEST.Item2);
                         NewNetServices.Module.Core.StaticHelperMethods.WriteOut($" MainSource{ MainSource?.ExternalSystemId}");
                         keyA = "DESTINATIONLOCATIONID";
                     }
                     if (MainSource == null || MainDestination == null)
                     {
                         NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"XXXX MainSource == null || MainDestination == null {MainSource} == null || {MainDestination} == null");
                         return;
                     }
                     NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"keyA {keyA} keyB {keyB}");
                     cabdata.ForEach((x) => NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"cabData {string.Join(", ", x)}"));
                     //now arrange rows in order src->dest-src-dest etc
                     /*"FORC", "CABLEID", "COMMENTS", "CABLESTATUS", "CABLELENGTH", "CABLEROUTE", "WORKORDERID", "DROPCABLE",
                 "CABLETYPE", "CABLECLASS", "CABLESIZE", "SOURCELOCATIONID", "DESTINATIONLOCATIONID", "DESCRIPTION",
                 "INSTALLDATE"*/

                     //  orderedData.Add(cabdata.FirstOrDefault(x => x["SOURCELOCATIONID"] == "" + MainDestination.ExternalSystemId || x["DESTINATIONLOCATIONID"] == "" + MainDestination.ExternalSystemId));

                     bool test = false;
                     //StaticHelperMethods.WriteOut($"{string.Join("\t", row.Select(x => x.Key + ":" + x.Value))}");
                     //make sure not imported already
                     test = (!uow.Query<Cable>()
                  .Any(x => x.ExternalSystemId.ToString() == cableID));
                     if (test)
                     {
                         int TotalLength = 0;
                         //  do spans
                         List<CableSpan> spans = new List<CableSpan>();
                         foreach (var row in cabdata)
                         {
                             CableSpan span = new CableSpan(uow)
                             {
                                 Source = uow.Query<Location>().FirstOrDefault(x => x.ExternalSystemId.ToString() == row["SOURCELOCATIONID"]),
                                 Destination = uow.Query<Location>().FirstOrDefault(x => x.ExternalSystemId.ToString() == row["DESTINATIONLOCATIONID"]),
                                 Length = int.TryParse(row["CABLELENGTH"], out int len) ? len : 0
                             };

                             spans.Add(span);
                         };
                         TotalLength = spans.Select(x => x.Length).Sum();
                         //process the rest
                         var cable = new PhysicalCable(uow)
                         {
                             Source = MainSource,
                             Destination = MainDestination,
                             Length = TotalLength
                         };
                         //assign spans to cable
                         spans.ForEach((I) => I.Cable = cable);
                         cable.CableSpans.AddRange(spans);
                         uow.CommitChanges();
                         using (var odw = new OracleDatabaseWorker(tbOracleConnectionStringText))
                         {
                             //var cpairs = odw.GetData(CABLEPAIR_Table, $" where CABLEID = '{cableID}'", cpColumns);
                             //var cptask = ImporterHelper.DoCablePairs(uow, cable.ExternalSystemId, CABLEPAIR_Table, cpColumns);
                             var pc = ImporterHelper.ProcessCable(cable, cabdata[0], cabTable, ProgressMade, stepName);
                             //await Task.WhenAll(cptask, pc);
                             //var pairs = await Task.FromResult(cptask);
                             //pairs.Result.ForEach((p) =>
                             //{
                             //    cable.CablePairs.Add(p);
                             //    p.Cable = cable;
                             //});
                             uow.CommitChanges();
                         }

                     }



                 }//end using
                  //do work  each data set represent a grouped cable, (with spans)


                 //  successfulCable++;
                 ProgressMade?.Invoke(stepName,
                  new ProgressMadeEventArgs(new ImportedItem()
                  {
                      SourceTable = cabTable,
                      ImportStatus = "OK",
                      Type = $"Already Exists {cableID}"
                  }));

             };


            WorkBeginning?.Invoke(stepName, EventArgs.Empty);
            //await ProcessData(cabWithSpansTable, cabColumns, func, processorAction);
            using (var odw = new OracleDatabaseWorker(tbOracleConnectionStringText))
            {
                var dataset = await Task.FromResult(odw.GetData(cabWithSpansTable, cabColumns));
                TotalRecordsToProcess = dataset.Count;
                if (dataset.Count > 0)
                {
                    //  parter += mine;
                    List<List<Dictionary<string, string>>> datablocks = func(dataset);
                    NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"datablocks : |{datablocks?.Count}| |{datablocks?[0]?.Count}|");
                    bool res = datablocks != null && datablocks.Count > 0;
                    int k = 0;
                    datablocks.ForEach(
                                       (ds) =>
                                       {
                                           NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"PARTITION ITERATION {++k} of {datablocks.Count} containing ");
                                           if (dataset == null)
                                           {
                                               MessageBox.Show(" line924                   if(dataset==null)MessageBox.Show(");
                                               return;
                                           }

                                           //lock(lockobj)
                                           //{

                                           processorAction(ds);

                                           //  } 
                                       });

                }
            }
            //await ProcessData(X cabTable, cabColumns, func, processor_action);
            WorkCompleted?.Invoke(stepName, EventArgs.Empty);

        }
        public async Task Conduits()
        {
            var stepName = "Conduits";//new StackTrace().GetFrame(0).GetMethod().Name;// "Conduits";

            //this is so that the xpobject type is created  on one thread
            Func<List<Dictionary<string, string>>,
            List<List<Dictionary<string, string>>>> func =
            (inlist) =>
            {
                List<List<Dictionary<string, string>>> ret = new List<List<Dictionary<string, string>>>();
                var subDataParts = inlist.Partition(GetSizeForPartition(inlist.Count));
                subDataParts.ForEach((subdata) => ret.Add(subdata.ToList()));

                return ret;
            };
            Action<List<Dictionary<string, string>>> processorAction = (inlist) =>
            {
                using (UnitOfWork uow = new UnitOfWork(Tsdl))
                {
                    //do work
                    inlist.ForEach((row) =>
                        {
                            if (Skip > 0)
                            {
                                Skip--;
                                return;
                            }
                            bool test = (!uow.Query<Conduit>().Any(x => x.ExternalSystemId.ToString() == row["ID"]));
                            if (test)
                            {
                                ImporterHelper.ProcessConduit(uow,
                row,
                conTable,
                ProgressMade, stepName);
                            }
                            else
                            {
                                //  successfulCable++;
                                ProgressMade?.Invoke(stepName,
                             new ProgressMadeEventArgs(new ImportedItem()
                             {
                                 SourceTable = conTable,
                                 ImportStatus = "OK",
                                 Type = $"Already Exists {row["CABLE"]}"
                             }));
                            }

                            //make sure not imported already
                        }); //end condata.ForEach((row) =>
                } //end uow  //StaticHelperMethods.WriteOut($"{string.Join("\t", row.Select(x => x.Key + ":" + x.Value))}");


            }; //end conDataParts.ForEach( (condata
            WorkBeginning?.Invoke(stepName, EventArgs.Empty);
            await ProcessData(conTable, conColumns, func, processorAction);
            WorkCompleted?.Invoke(stepName, EventArgs.Empty);

        } //endmethod//endmethod 

        public async Task DesignationGroups()
        {
            var stepName = "DesignationGroups";//new StackTrace().GetFrame(0).GetMethod().Name;// new StackTrace().GetFrame(0).GetMethod().Name;

            //this is so that the xpobject type is created  on one thread
            Func<List<Dictionary<string, string>>,
            List<List<Dictionary<string, string>>>> func =
            (inlist) =>
            {
                List<List<Dictionary<string, string>>> ret = new List<List<Dictionary<string, string>>>();
                var subDataParts = inlist.Partition(GetSizeForPartition(inlist.Count));
                subDataParts.ForEach((subdata) => ret.Add(subdata.ToList()));

                return ret;
            };
            Action<List<Dictionary<string, string>>> processorAction = (inlist) =>
            {
                inlist.ForEach((row) =>
        {

            using (UnitOfWork uow = new UnitOfWork(Tsdl))
            {
                //StaticHelperMethods.WriteOut($"{string.Join("\t", row.Select(x => x.Key + ":" + x.Value))}");
                //make sure not imported already
                if (!uow.Query<DesignationGroup>()
                    .Any(x => x.ExternalSystemId.ToString() == row["DGID"]))
                {
                    try
                    {
                        var cable = new DesignationGroup(uow);

                        //{
                        cable.ExternalSystemId = int.Parse(row["DGID"]);
                        uow.CommitChanges();
                        //    }"DGID", "CLASS", "DGNAME", "STATUS", "CODE", "SOURCE", "MAXCOUNT"
                        //    }" ", " ", " ", " ", "CODE", " ", " "
                        //TODO: what is BarCodeControl for?
                        cable.SourceTable = dgroupTable; // int.Parse(row["LOCATIONID"]);  
                                                         // dg.Status = !string.IsNullOrWhiteSpace(row["STATUS"]) ? ImporterHelper.GetStatus<CableStatus>(uow, row["STATUS"]) : null;
                        cable.CableName = row["DGNAME"];

                        //No Status?
                        //cable.Status = row["STATUS"] != "" ? uow.Query<CableStatus>().FirstOrDefault(x => x.StatusName == row["STATUS"]) :uow.Query<CableStatus>().FirstOrDefault(x => x.StatusName =="Active")  ;
                        cable.Wirecenter = uow.Query<Wirecenter>().FirstOrDefault(x => x.LocationName == UNK);

                        if (!string.IsNullOrWhiteSpace(row["CLASS"]))
                        {
                            cable.Class = uow.Query<CableClass>()
                    .FirstOrDefault(x => x.TypeName == row["CLASS"]);
                            cable.Type = cable.Class.CableTypes.FirstOrDefault(x => x.TypeName == row["CLASS"]);
                            cable.Size = cable.Class.CableSizes.FirstOrDefault(x => x.Count.ToString() == row["MAXCOUNT"]);
                        }

                        if (!string.IsNullOrWhiteSpace(row["SOURCE"]))
                        {
                            cable.Source = uow.Query<Location>()
                    .FirstOrDefault(x => x.ExternalSystemId.ToString() == row["SOURCE"]);
                        }
                        uow.CommitChanges();
                        currentSuccess++;
                        ProgressMade?.Invoke(stepName,
            new ProgressMadeEventArgs(new ImportedItem()
            {
                Guid = cable.Oid.ToString(),
                Id = cable.ExternalSystemId?.ToString(),
                SourceTable = cable.SourceTable,
                ImportStatus = "Success",
                //RecordStatus = dg.Status?.ToString(),
                Type = "DesignationGroup"
            }));
                    }
                    catch (Exception ex)
                    {
                        currentErrors++;
                        ProgressMade?.Invoke(stepName,
            new ProgressMadeEventArgs(new ImportedItem()
            {
                SourceTable = dgroupTable,
                ImportStatus = "Exception" + $"\t{ex.Message}",
                Type = "Exception"
            }));
                        StaticHelperMethods.WriteOut($"{ex}");
                    }
                } //end if exists
                else
                {
                    // SuccessfulDesignationGroup++;
                    ProgressMade?.Invoke(stepName,
                        new ProgressMadeEventArgs(new ImportedItem()
                        {
                            SourceTable = dgroupTable,
                            ImportStatus = "Success",
                            Type = $"Already Exists {row["DGID"]}"
                        }));
                }
            } //EndInit using unitofwrk
        });
            };
            WorkBeginning?.Invoke(stepName, EventArgs.Empty);
            await ProcessData(dgroupTable, dgroupColumns, func, processorAction);
            WorkCompleted?.Invoke(stepName, EventArgs.Empty);



        }
        public const string dpairsexeptionlist = @"C:/EXES/dpX.csv";
        public const string cpairsexeptionlist = @"C:/EXES/cpX.csv";
        public const string cableexeptionlist = @"C:/EXES/cableX.csv";
        public const string conexeptionlist = @"C:/EXES/cableX.csv";
        public async Task DesignationPairs()
        {
            var stepName = "DesPairs";// new StackTrace().GetFrame(0).GetMethod().Name;// "Cable Pairs";

            using (var uow = new UnitOfWork(Tsdl))
            {
                try
                {
                    var before = uow.Query<DesignationPair>().Count();
                    NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"before {before}");
                    string sql = File.ReadAllText(dpairfilepath);
                    NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"\n\ndPAIRSQL\n\n{sql}");
                    WorkBeginning?.Invoke(stepName, EventArgs.Empty);

                    await Task.FromResult(uow.ExecuteNonQuery(sql));
                    WorkCompleted?.Invoke(stepName, EventArgs.Empty);

                    var after = uow.Query<DesignationPair>().Count();
                    NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"before {before} after {after}");
                }
                catch (Exception ex)
                {
                    NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{ex}");
                }
            }
            //  CreateFileWithColumnHeaders(dpairsexeptionlist, dpColumns);
            //    var stepName = "Designation Pairs";// new StackTrace().GetFrame(0).GetMethod().Name;

            //    Func<List<Dictionary<string, string>>,
            //    List<List<Dictionary<string, string>>>> func =
            //    (inlist) =>
            //    {
            //        //list of each cable in this data set
            //        var distinctablelist = inlist.Where(x => x.ContainsKey("DGROUP") && x["DGROUP"]?.Trim() != "")
            //            .Select(x => x["DGROUP"])
            //            .Distinct();
            //        var ret = new List<List<Dictionary<string, string>>>();
            //        //have each cable's pairs handled on the same thread
            //        foreach (var cableid in distinctablelist)
            //        {
            //            ret.Add(inlist.Where(x => x["DGROUP"] ==
            //              cableid)
            //.Select(x => x)
            //.ToList());
            //        }
            //        return ret;
            //    };
            //    Action<List<Dictionary<string, string>>> processorAction = (inlist) =>
            //    {
            //        foreach (var row in inlist)
            //        {
            //            using (var puow = new UnitOfWork(Tsdl))
            //            {
            //                try
            //                {
            //                    if (!puow.Query<DesignationPair>()
            //        .Any(x => x.ExternalSystemId.ToString() == row["ID"]))
            //                    {


            //                        if (!string.IsNullOrWhiteSpace(row["DGROUP"])
            //            ) //look up designation we probably omorted already//{
            //                        {
            //                            try
            //                            {
            //                                var dgroup = puow.Query<DesignationGroup>()
            //                        .FirstOrDefault(x => x.ExternalSystemId.ToString() ==
            //                                             row["DGROUP"]);
            //                                if (dgroup != null)
            //                                {
            //                                    var dp = new DesignationPair(puow);
            //                                    dp.ExternalSystemId = int.Parse(row["ID"]);
            //                                    dp.SourceTable = dpTable;
            //                                    dp.PairNumber = int.Parse(row["COUNT"]);
            //                                    dp.Status = puow.GetObjectByKey<CablePairStatus>(NewNetServices.Module.Core.DefaultFields
            //                            .GetStatus<CablePairStatus>(puow, "UNKNOWN"));
            //                                    dgroup.DesignationPairs.Add(dp);

            //                                    puow.CommitChanges();
            //                                    currentSuccess++;
            //                                }
            //                                else
            //                                {
            //                                    throw new Exception(
            //                            $"DesignationGroup not found with id: {row["DGROUP"]}");
            //                                }
            //                            }
            //                            catch (Exception ex)
            //                            {
            //                                NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"    {ex}");
            //                            }
            //                        }
            //                        else throw new Exception($"DesignationGroup info missing "); //{
            //                    } //endif exist
            //                    ProgressMade?.Invoke(stepName,
            //                             new ProgressMadeEventArgs(new ImportedItem()
            //                             {
            //                                 ImportStatus = "Success",
            //                                 Type = "DesignationPair"
            //                             }));
            //                }
            //                catch (Exception ex)
            //                {
            //                    NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{string.Join(",", row)}\n", dpairsexeptionlist);
            //                    ProgressMade?.Invoke(stepName,
            //        new ProgressMadeEventArgs(new ImportedItem()
            //        {
            //            SourceTable = dpTable,
            //            ImportStatus = "Exception" + $"\t{ex.Message}",
            //            Type = "Exception"
            //        }));
            //                    NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{ex}");
            //                }
            //            } //end using
            //        } //endfor
            //    };
            //    WorkBeginning?.Invoke(stepName, EventArgs.Empty);
            //    await ProcessData(dpTable, dpColumns, func, processorAction);
            //    WorkCompleted?.Invoke(stepName, EventArgs.Empty);

        }
        public static void CreateFileWithColumnHeaders(string file, string[] cols)
        {
            try
            {
                if (!Directory.Exists(System.IO.Path.GetDirectoryName(file)))
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file));
                File.WriteAllText(file, string.Join(",", cols) + "\n");
            }
            catch (Exception ex)
            {
                DXMessageBox.Show($"WTF couldn't make file???????\n{ex}");
            }
        }
        public async Task DesignationPairsCablePairLink()
        {
            var stepName = "DesignationPairsCablePairLink";// new StackTrace().GetFrame(0).GetMethod().Name;

            List<List<Dictionary<string, string>>> Func(List<Dictionary<string, string>> inlist)
            {
                List<List<Dictionary<string, string>>> ret = new List<List<Dictionary<string, string>>>();
                foreach (var cableid in inlist.Select(x => x["CABLEID"]).Distinct())
                {
                    ret.Add(inlist.Where(x => x["CABLEID"] == cableid).ToList());
                }
                return ret;
            }
            //action to take on eas=ch task data

            Action<List<Dictionary<string, string>>> processorAction = (inlist) =>
            {
                foreach (var row in inlist)
                {
                    if (Skip > 0)
                    {
                        Skip--;
                        return;
                    }
                    using (var uow = new UnitOfWork(Tsdl))
                    {
                        try
                        {
                            //StaticHelperMethods.WriteOut($"{string.Join("\t", row.Select(x => x.Key + ":" + x.Value))}");

                            CablePair cp = null;

                            //make sure data is populated and hasn't been processed previously

                            if (string.IsNullOrWhiteSpace(row["PAIRID"]) ||
                                !int.TryParse(row["PAIRID"], out int cpid))
                            {
                                throw new Exception($"Bad Data {row["PAIRID"]}");
                            }
                            if (row["LOGICALCOUNTID"] == "1") continue;//skip

                            ;
                            if ((cp = uow.Query<CablePair>().FirstOrDefault(x => x.ExternalSystemId == cpid)) == null)
                                throw new Exception($"Cable Pair {cpid} not found"); // 
                            if (!string.IsNullOrWhiteSpace(cp.SourceType))
                            /* means this record has  been processed in an earlier run*/
                            {
                                //added previously
                                //Successfulcp_dpLinks++;
                                ProgressMade?.Invoke(stepName,
                                                                         new ImporterHelper.ProgressMadeEventArgs(new ImportedItem()
                                                                         {
                                                                             ImportStatus = "Success",
                                                                             Type = "Already Done"
                                                                         }));
                            }
                            else if (string.IsNullOrWhiteSpace(row["LOGICALCOUNTID"]))
                            {
                                throw new Exception($"Designation PairId missing : {row["LOGICALCOUNTID"]} \ndatarow : ");
                            }
                            else
                            {
                                DesignationPair dp = null;
                                if (int.TryParse(row["LOGICALCOUNTID"], out int dpid) &&
                            (dp =
                                uow.Query<DesignationPair>()
                                    .FirstOrDefault(x => x.ExternalSystemId == dpid)) !=
                            null)
                                {
                                    cp.SourceType = "CablePairLinked"; //means processed
                                    cp.SourceTable = cpdpTable;
                                    //add pair to designationpair.cableair collection
                                    dp.CablePairs.Add(cp);
                                    //set cablepair's designationpair to this despair
                                    cp.DesignationPair = dp;
                                    uow.CommitChanges();
                                    currentSuccess++;
                                    ProgressMade?.Invoke("Cable Pair -> Designation Pair Link",
                                new ImporterHelper.ProgressMadeEventArgs(new ImportedItem()
                                {
                                    SourceTable = cpdpTable,
                                    ImportStatus = "Success",
                                    Type = "CablePair -> DesignationPair Link"
                                }));
                                }
                                else
                                    throw new Exception(
                                $"Designation PairId invalid or not found: {row["LOGICALCOUNTID"]} \ndatarow : " +
                                string.Join("\t | \t", row.Select(x => $"{x.Key}:{x.Value}")));
                            }


                        }
                        catch (Exception ex)
                        {
                            uow.RollbackTransaction();
                            currentErrors++;
                            ProgressMade?.Invoke(stepName,
                    new ImporterHelper.ProgressMadeEventArgs(new ImportedItem()
                    {
                        SourceTable = dpTable,
                        ImportStatus = "Exception" + $"\t{ex.Message}",
                        Type = "CablePair -> DesignationPair Link Exception"
                    }));
                            NewNetServices.Module.Core.StaticHelperMethods
                    .WriteOut($"{ex}\ndatarow : " +
                              string.Join("\t | \t", row.Select(x => $"{x.Key}:{x.Value}")));
                        }
                    } //endusing
                }

            };


            WorkBeginning?.Invoke(stepName, EventArgs.Empty);
            await ProcessData(this.cpdpTable, this.cpdpColumns, Func, processorAction);
            WorkCompleted?.Invoke(stepName, null);

        }
        public async Task Junctions()
        {
            var stepName = "Junctions";// new StackTrace().GetFrame(0).GetMethod().Name;// "Junctions";


            /////*******************

            Func<List<Dictionary<string, string>>,
            List<List<Dictionary<string, string>>>> func =
            (inlist) =>
            {
                List<List<Dictionary<string, string>>> ret = new List<List<Dictionary<string, string>>>();
                var subDataParts = inlist.Partition(GetSizeForPartition(inlist.Count));
                subDataParts.ForEach((subdata) => ret.Add(subdata.ToList()));

                return ret;
            };
            //action to take on eas=ch task data
            Action<List<Dictionary<string, string>>> processorAction = (inlist) =>
            {
                using (var uow = new UnitOfWork(Tsdl))
                {   //do work
                    inlist.ForEach((row) =>

                        {
                            var result = (ImporterHelper.ProcessJunction(uow,
                                                                          row,
                                                                          junkTable,
                                                                          ProgressMade));
                            if (result.HasValue)
                            {
                                if (result.Value)
                                {
                                    currentSuccess++;
                                    ProgressMade?.Invoke(stepName,
                                 new ProgressMadeEventArgs(new ImportedItem()
                                 {
                                     Id = row["ENTITYID"],
                                     SourceTable = junkTable,

                                     ImportStatus = "Success",

                                     Type = "Junction",
                                     SubType = row["ENTITYTYPE"]
                                 }));
                                }
                                else
                                {
                                    currentErrors++;

                                    ProgressMade?.Invoke(stepName,
                                     new ProgressMadeEventArgs(new ImportedItem()
                                     {
                                         SourceTable = junkTable,
                                         ImportStatus = "Exception Junction",
                                         Type = "Exception"
                                     }));
                                }
                            }
                            else
                            {
                                ProgressMade?.Invoke(stepName,
                             new ProgressMadeEventArgs(new ImportedItem()
                             {
                                 SourceTable = junkTable,
                                 ImportStatus = "Exists",
                                 Type = "Exists"
                             }));
                            }

                        }); //end for row
                }//end uow
            };


            WorkBeginning?.Invoke(stepName, EventArgs.Empty);
            await ProcessData(junkTable, junctionCols, func, processorAction);
            WorkCompleted?.Invoke(stepName, EventArgs.Empty);


            /////*******************
        }//
        public async Task OLT_Splitter_DP()
        {
            var stepName = "OLT_Splitter_DP";// new StackTrace().GetFrame(0).GetMethod().Name;
                                             //****************OLT>splitter>DP method
                                             //method to split into separate tasks

            Func<List<Dictionary<string, string>>, List<List<Dictionary<string, string>>>> func = (inlist) =>
            {
                List<List<Dictionary<string, string>>> ret = new List<List<Dictionary<string, string>>>();
                var splitterCodes = (from x in inlist select x["SplitterID"]).Distinct();
                splitterCodes.ForEach((oc) =>
        {
            ret.Add(inlist.Where(x => x["SplitterID"] == oc).ToList());
        });
                return ret;
            };
            //action to take on eas=ch task data
            /*   Action<List<Dictionary<string, string>>> */
            Action<List<Dictionary<string, string>>> processorAction = (inlist) =>
            {
                using (var puow = new UnitOfWork(Tsdl))
                {
                    Guid? splittertypeOid = GlobalSystemSettings.GetInstanceFromDatabase(puow).SplitterEquipmentType?.Oid;
                    var olttypeOid = GlobalSystemSettings.GetInstanceFromDatabase(puow).OLT_EquipmentType.Oid;
                    foreach (var row in inlist)
                    {
                        try
                        {
                            //set to Splitter type default
                            //"SPLITTERID", "INCABLEID", "INCABLENAME", "INCABLEOBJECTREF", "INCOUNTID", "INCOUNT", "OLTID"
                            //"", "INCABLEID", "INCABLENAME", "INCABLEOBJECTREF", "INCOUNTID", "INCOUNT", "OLTID"
                            var splitter = puow.Query<Equipment>().FirstOrDefault(x => x.EquipmentType.Oid == splittertypeOid && string.Empty + x.ExternalSystemId == row["SPLITTERID"]);
                            var olt = puow.Query<Equipment>().FirstOrDefault(x => x.EquipmentType.Oid == olttypeOid && string.Empty + x.ExternalSystemId == row["OLTID"]);
                            if (olt != null && splitter != null)
                            {
                                olt.SubLocations.Add(splitter);
                                splitter.ServingLocations.Add(olt);
                            }
                            puow.CommitChanges();

                            var pair = puow.Query<DesignationPair>().FirstOrDefault(x => string.Empty + x.ExternalSystemId == row["INCOUNTID"]);
                            if (pair != null && splitter != null)
                                pair.Locations.Add(splitter);
                            puow.CommitChanges();
                            currentSuccess++;
                            ProgressMade?.Invoke(stepName,
                               new ProgressMadeEventArgs(new ImportedItem()
                               {
                                   RecordStatus = "Success"
                               }));
                        }
                        catch (Exception ex)
                        {
                            currentErrors++;
                            ProgressMade?.Invoke(stepName,
                               new ProgressMadeEventArgs(new ImportedItem()
                               {
                                   RecordStatus = $"Exception {ex.Message}"
                               }));
                            NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{ex}");
                        }
                    }
                }
            };
            WorkBeginning?.Invoke(stepName, null);
            await ProcessData(oltSplitterDpTable, oltSplitterDpCols, func, processorAction);
            WorkCompleted?.Invoke(stepName, null);

        }
        //****************OLT>splitter>DP}
        public async Task OltPorts()
        {
            var stepName = "OltPorts";// new StackTrace().GetFrame(0).GetMethod().Name;
                                      //****************Splitter method
                                      //****************OLTPorts
                                      //method to split into separate tasks
            Func<List<Dictionary<string, string>>, List<List<Dictionary<string, string>>>> func = (inlist) =>
            {
                List<List<Dictionary<string, string>>> ret = new List<List<Dictionary<string, string>>>();
                var oltCodes = (from x in inlist select x["OLTID"]).Distinct();
                oltCodes.ForEach((oc) =>
        {
            ret.Add(inlist.Where(x => x["OLTID"] == oc).ToList());
        });
                return ret;
            };
            //action to take on eas=ch task data
            Action<List<Dictionary<string, string>>> processorAction = (inlist) =>
            {
                using (var puow = new UnitOfWork(Tsdl))
                {
                    var olttypeOid = (GlobalSystemSettings.GetInstanceFromDatabase(puow).OLT_PortType != null
        ? GlobalSystemSettings.GetInstanceFromDatabase(puow).OLT_PortType
        : NewNetServices.Module.Core.DefaultFields.GetBusinessObjectDefault<PortType>(puow, "TypeName", "OLT PORT")).Oid;
                    foreach (var row in inlist)
                    {
                        if (Skip > 0)
                        {
                            Skip--;
                            return;
                        }
                        //"OLTID", "OLTPORTID", "OLTPORTNUM"
                        //" ", " ", "OLTPORTNUM"
                        puow.BeginTransaction();
                        try
                        {
                            var olt = new Port(puow);
                            //set to olt type default
                            olt.PortType = puow.GetObjectByKey<PortType>(olttypeOid);
                            olt.ExternalSystemId = int.TryParse(row["OLTPORTID"], out int oltid) ? oltid : 0;
                            olt.Equipment = puow.Query<Equipment>()
                     .FirstOrDefault(x => x.ExternalSystemId.ToString() == row["OLTID"]);
                            olt.SourceTable = oltPortsTable;
                            olt.Status = GlobalSystemSettings.GetInstanceFromDatabase(puow)
                 .DefaultLocationStatusUnknown;
                            olt.Number = int.TryParse(row["OLTPORTNUM"], out int pnum) ? pnum : 0; ;
                            olt.LocationName = row["OLTPORTNUM"];
                            //olt.Rack = row["RACK_CODE"];
                            //olt.RackNum = int.TryParse(row["SUB_RACK_CODE"], out int src) ? src : 0;
                            //olt.Shelf = row["CARD_NUM"];
                            //olt.ShelfNum = int.TryParse(row["PORT_POSITION"], out int sn) ? sn : 0;
                            olt.Wirecenter = olt.Equipment?.Wirecenter;// puow.Query<Wirecenter>().FirstOrDefault(x => x.ExternalSystemId.ToString() == row["CR_SITE_ID"]);

                            puow.CommitTransaction();
                            puow.CommitChanges();
                            currentSuccess++;
                            ProgressMade?.Invoke(stepName,
                                      new ProgressMadeEventArgs(new ImportedItem()
                                      { RecordStatus = "Success" }));

                        }
                        catch (Exception ex)
                        {
                            currentErrors++;
                            puow.RollbackTransaction();
                            ProgressMade?.Invoke(stepName,
                                     new ProgressMadeEventArgs(new ImportedItem()
                                     { RecordStatus = $"Exception {ex.Message}" }));
                            NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{ex}");
                        }

                    }

                }
            };
            WorkBeginning?.Invoke(stepName, null);
            await ProcessData(oltPortsTable, oltPortsCols, func, processorAction);
            WorkCompleted?.Invoke(stepName, null);

            //****************OLTports
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task Olts()
        {
            var stepName = "OLTS";// new StackTrace().GetFrame(0).GetMethod().Name;
                                  //****************Splitter method
                                  //****************OLT
                                  //method to split into separate tasks
            Func<List<Dictionary<string, string>>, List<List<Dictionary<string, string>>>> func = (inlist) =>
            {
                List<List<Dictionary<string, string>>> ret = new List<List<Dictionary<string, string>>>();
                var oltCodes = (from x in inlist select x["OLT_CODE"]).Distinct();
                oltCodes.ForEach((oc) =>
        {
            ret.Add(inlist.Where(x => x["OLT_CODE"] == oc).ToList());
        });
                return ret;
            };
            //action to take on eas=ch task data
            Action<List<Dictionary<string, string>>> processorAction = (inlist) =>
            {
                using (var puow = new UnitOfWork(Tsdl))
                {
                    foreach (var row in inlist)
                    {

                        if (!puow.Query<Equipment>().Any(x => x.ExternalSystemId.ToString() == row["OLT_ID"]))
                        {
                            if (GlobalSystemSettings.GetInstanceFromDatabase(puow).OLT_EquipmentType == null)
                            {
                                var ot = new EquipmentType(puow) { TypeName = "OLT" };
                                puow.Save(ot);
                                puow.CommitChanges();
                                GlobalSystemSettings.GetInstanceFromDatabase(puow).OLT_EquipmentType = ot;

                                puow.CommitChanges();
                            }
                            var olttypeOid = GlobalSystemSettings.GetInstanceFromDatabase(puow).OLT_EquipmentType.Oid;

                            try
                            {
                                var olt = new Equipment(puow);
                                //set to olt type default
                                olt.EquipmentType = puow.GetObjectByKey<EquipmentType>(olttypeOid);
                                olt.ExternalSystemId = int.TryParse(row["OLT_ID"], out int oltid) ? oltid : 0;
                                olt.SourceTable = oltTable;
                                olt.LocationName = row["OLTNAME"];
                                olt.Status = GlobalSystemSettings.GetInstanceFromDatabase(puow)
                            .DefaultLocationStatusUnknown;
                                olt.Rack = row["RACK_CODE"];
                                olt.RackNum = int.TryParse(row["SUB_RACK_CODE"], out int src) ? src : 0;
                                olt.Shelf = row["CARD_NUM"];
                                olt.ShelfNum = int.TryParse(row["PORT_POSITION"], out int sn) ? sn : 0;
                                olt.Wirecenter = puow.Query<Wirecenter>()
                            .FirstOrDefault(x => x.ExternalSystemId.ToString() == row["CR_SITE_ID"]);

                                puow.CommitChanges();
                                currentSuccess++;
                                ProgressMade?.Invoke(stepName,
                                             new ProgressMadeEventArgs(new ImportedItem()
                                             { RecordStatus = "Success" }));
                            }
                            catch (Exception ex)
                            {
                                currentErrors++;
                                ProgressMade?.Invoke(stepName,
                                             new ProgressMadeEventArgs(new ImportedItem()
                                             { RecordStatus = $"Exception {ex.Message}" }));
                                NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{ex}");
                            }
                        }//if doesn't exist
                        else
                        {
                            ProgressMade?.Invoke(stepName,
                                                                            new ProgressMadeEventArgs(new ImportedItem()
                                                                            { RecordStatus = "Exists" }));
                        }
                    }

                }
            };

            WorkBeginning?.Invoke(stepName, null);
            await ProcessData(oltTable, oltCols, func, processorAction);
            WorkCompleted?.Invoke(stepName, null);

            //****************OLT
        }

        public async Task OutDesignationPairs()
        {
            var stepName = "OutDesignationPairs";// new StackTrace().GetFrame(0).GetMethod().Name;
                                                 //set splitter port as source for dpairs
                                                 //this is so that the xpobject type is created  on one thread
            Func<List<Dictionary<string, string>>, List<List<Dictionary<string, string>>>> func = (inlist) =>
            {
                List<List<Dictionary<string, string>>> ret = new List<List<Dictionary<string, string>>>();
                var subDataParts = inlist.Partition(GetSizeForPartition(inlist.Count));
                subDataParts.ForEach((subdata) => ret.Add(subdata.ToList()));

                return ret;
            };
            Action<List<Dictionary<string, string>>> processorAction = (inlist) =>
            {
                using (var uow = new UnitOfWork(Tsdl))
                {
                    inlist.ForEach((row) =>
        {
            if (Skip > 0)
            {
                Skip--;
                return;
            }
            uow.BeginTransaction();
            //"SPLITTERPORTID", "DESIGNATIONPAIRID"
            try
            {
                if (!string.IsNullOrWhiteSpace(row["SPLITTERPORTID"]) &&
        !string.IsNullOrWhiteSpace(row["DESIGNATIONPAIRID"]))
                {
                    var sp = uow.Query<Port>()
        .FirstOrDefault(x => x.ExternalSystemId.ToString() == row["SPLITTERPORTID"] && /*
make aure not olt port*/
            x.SourceTable == splitterPortsTable);
                    var dp = uow.Query<DesignationPair>()
        .FirstOrDefault(x => x.ExternalSystemId.ToString() == row["DESIGNATIONPAIRID"]);
                    if (sp != null & dp != null)
                    {
                        dp.Source = sp;
                        uow.CommitTransaction();
                        uow.CommitChanges();
                        currentSuccess++;
                        ProgressMade?.Invoke(stepName,
                                 new ProgressMadeEventArgs(new ImportedItem()
                                 {
                                     ImportStatus = "Success"
                                 }));
                    }
                    else
                    {
                        throw new Exception($"Bad Data SPLITTERPORTID {row["SPLITTERPORTID"]} dpair{row["DESIGNATIONPAIRID"]}");
                    }
                }
                else uow.RollbackTransaction();
            }
            catch (Exception ex)
            {
                currentErrors++;
                uow.RollbackTransaction();
                ProgressMade?.Invoke(stepName,
                 new ProgressMadeEventArgs(new ImportedItem()
                 {
                     ImportStatus = $"Exception {ex}"
                 }));
            }
        });
                }//end using

            };//end action
            WorkBeginning?.Invoke(stepName, null);
            await ProcessData(outdPairsTable, outdPairsCols, func, processorAction);
            WorkCompleted?.Invoke(stepName, null);

        }
        public async Task SplitterPorts()
        {
            var stepName = "Splitter Ports";// new StackTrace().GetFrame(0).GetMethod().Name;
                                            //****************Splitter method

            //****************SplitterPort method
            //method to split into separate tasks
            /*   Func<List<Dictionary<string, string>>, List<List<Dictionary<string, string>>>>*/
            Func<List<Dictionary<string, string>>, List<List<Dictionary<string, string>>>> func = (inlist) =>
            {
                List<List<Dictionary<string, string>>> ret = new List<List<Dictionary<string, string>>>();
                var splitterPortCodes = (from x in inlist select x["CR_EQUIPMENT_ID"]).Distinct();
                splitterPortCodes.ForEach((oc) =>
        {
            ret.Add(inlist.Where(x => x["CR_EQUIPMENT_ID"] == oc).ToList());
        });
                return ret;
            };
            //action to take on eas=ch task data
            /*   Action<List<Dictionary<string, string>>> */
            Action<List<Dictionary<string, string>>> processorAction = (inlist) =>
            {
                using (var puow = new UnitOfWork(Tsdl))
                {
                    splitterPorttypeOid = Guid.Empty;
                    if (GlobalSystemSettings.GetInstanceFromDatabase(puow).SplitterPortType == null)
                    {
                        GlobalSystemSettings.GetInstanceFromDatabase(puow).SplitterPortType = new PortType(puow) { TypeName = "SPLITTERPORT" };
                        puow.CommitChanges();
                    }
                    splitterPorttypeOid = GlobalSystemSettings.GetInstanceFromDatabase(puow).SplitterPortType.Oid;

                    foreach (var row in inlist)
                    {
                        if (Skip > 0)
                        {
                            Skip--;
                            return;
                        }
                        puow.BeginTransaction();
                        try
                        {
                            var splitterPort = new Port(puow);
                            //set to SplitterPort type default
                            //"ID", "NAME", "STATUS", "CR_EQUIPMENT_ID"
                            //" ", " ", " ", ""
                            splitterPort.PortType = puow.GetObjectByKey<PortType>(splitterPorttypeOid);
                            splitterPort.ExternalSystemId = int.TryParse(row["ID"], out int splitterPortid)
                ? splitterPortid
                : 0;
                            splitterPort.SourceTable = splitterPortsTable;
                            splitterPort.LocationName = row["NAME"];
                            splitterPort.Status = !string.IsNullOrWhiteSpace(row["STATUS"])
                ? puow.GetObjectByKey<LocationStatus>(NewNetServices.Module.Core.DefaultFields
                .GetStatus<LocationStatus>(puow, row["STATUS"]))
                : null;
                            splitterPort.Equipment = puow.Query<Equipment>()
                .FirstOrDefault(x => x.ExternalSystemId.ToString() == row["CR_EQUIPMENT_ID"]);

                            splitterPort.Wirecenter = splitterPort.Equipment?.Wirecenter;

                            puow.CommitChanges();
                            currentSuccess++;
                            ProgressMade?.Invoke(stepName,
                             new ProgressMadeEventArgs(new ImportedItem()
                             { RecordStatus = "Success" }));
                        }
                        catch (Exception ex)
                        {
                            currentErrors++;
                            ProgressMade?.Invoke(stepName,
                             new ProgressMadeEventArgs(new ImportedItem()
                             { RecordStatus = $"Exception {ex.Message}" }));
                            NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{ex}");
                        }
                    }
                }
            };
            WorkBeginning?.Invoke(stepName, null);
            await ProcessData(splitterPortsTable, splitterPortsCols, func, processorAction);
            WorkCompleted?.Invoke(stepName, null);
            //****************SplitterPort

        }

        delegate bool MyDelegate(List<string> lst);

        //         public static Guid LocationOid(UnitOfWork uow, string locname)
        //         {
        //             BaseObject obj = null;
        // 
        //             obj = uow.Query<Location>().FirstOrDefault(x => x.LocationName == locname || x.ExternalSystemId.ToString() == locname);
        // 
        //             if (obj != null) return obj.Oid;
        // 
        //             return (new Wirecenter(uow) { LocationName = locname, ExternalSystemId = int.TryParse(locname, out int ln) ? ln : 0 }).Oid;
        //         }
        //         public static Guid WirecenterOid(UnitOfWork uow, string locname)
        //         {
        //             BaseObject obj = null;
        // 
        //             obj = uow.Query<Wirecenter>().FirstOrDefault(x => x.LocationName == locname);
        // 
        //             if (obj != null) return obj.Oid;
        // 
        //             return (new Wirecenter(uow) { LocationName = locname }).Oid;
        //         }
        //         public static Guid WorkOrderOid(UnitOfWork uow, string OrderNumber)
        //         {
        //             BaseObject obj = null;
        // 
        //             obj = uow.Query<WorkOrder>().FirstOrDefault(x => x.OrderNumber == OrderNumber);
        // 
        //             if (obj != null) return obj.Oid;
        // 
        //             return (new WorkOrder(uow) { OrderNumber = OrderNumber }).Oid;
        //         }
        //         public static Guid RouteOid(UnitOfWork uow, string Name)
        //         {
        //             BaseObject obj = null;
        // 
        //             obj = uow.Query<Route>().FirstOrDefault(x => x.Name == Name);
        // 
        //             if (obj != null) return obj.Oid;
        // 
        //             return (new Route(uow) { Name = Name }).Oid;
        //         }
        //         public static Guid CableConduitSizeOid(UnitOfWork uow, string code, int count, System.Type objType)
        //         {
        //             BaseObject obj = null;
        //             if (objType == typeof(CableSize))
        //                 obj = uow.Query<CableSize>().FirstOrDefault(x => x.Code == code && x.Count == count);
        //             else obj = uow.Query<ConduitSize>().FirstOrDefault(x => x.Code == code && x.Count == count);
        // 
        //             if (obj != null) return obj.Oid;
        // 
        //             return (objType == typeof(CableSize)
        //                 ? (new CableSize(uow) { Code = code, Count = count }).Oid
        //                 : (new ConduitSize(uow) { Code = code, Count = count }).Oid);
        //         }
        //         public static Guid CableConduitTypeOid(UnitOfWork uow, string typeName, System.Type objType)
        //         {
        //             BaseObject obj = null;
        //             if (objType == typeof(CableType))
        //                 obj = uow.Query<CableType>().FirstOrDefault(x => x.TypeName == typeName);
        //             else obj = uow.Query<ConduitType>().FirstOrDefault(x => x.Code == typeName);
        // 
        //             if (obj != null) return obj.Oid;
        // 
        //             return (objType == typeof(CableType)
        //                 ? (new CableType(uow) { TypeName = typeName }).Oid
        //                 : (new ConduitType(uow) { Code = typeName }).Oid);
        //         }
        //         public static Guid CableConduitClassOid(UnitOfWork uow, string TypeName, System.Type objType)
        //         {
        //             BaseObject obj = null;
        //             if (objType == typeof(CableClass))
        //                 obj = uow.Query<CableClass>().FirstOrDefault(x => x.TypeName == TypeName);
        //             else obj = uow.Query<ConduitClass>().FirstOrDefault(x => x.TypeName == TypeName);
        // 
        //             if (obj != null) return obj.Oid;
        // 
        //             return (objType == typeof(CableClass)
        //                 ? (new CableClass(uow) { TypeName = TypeName }).Oid
        //                 : (new ConduitClass(uow) { TypeName = TypeName }).Oid);
        //         }
        public Guid splittertypeOid = Guid.Empty;
        public Guid splitterPorttypeOid = Guid.Empty;
        public async Task Splitters()
        {
            var stepName = "Splitters";// new StackTrace().GetFrame(0).GetMethod().Name;
                                       //****************Splitter method
                                       //method to split into separate tasks
                                       /*   Func<List<Dictionary<string, string>>, List<List<Dictionary<string, string>>>>*/
            Func<List<Dictionary<string, string>>, List<List<Dictionary<string, string>>>> func = (inlist) =>
            {
                List<List<Dictionary<string, string>>> ret = new List<List<Dictionary<string, string>>>();

                var enumble = inlist.Partition(GetSizeForPartition(inlist.Count));
                enumble.ForEach(x => ret.Add(x.ToList()));
                return ret;
            };
            //action to take on eas=ch task data
            /*   Action<List<Dictionary<string, string>>> */
            Action<List<Dictionary<string, string>>> processorAction = (inlist) =>
            {
                using (var puow = new UnitOfWork(Tsdl))
                {
                    splittertypeOid = Guid.Empty;
                    if (GlobalSystemSettings.GetInstanceFromDatabase(puow).SplitterEquipmentType == null)
                    {
                        GlobalSystemSettings.GetInstanceFromDatabase(puow).SplitterEquipmentType = new EquipmentType(puow) { TypeName = "SPLITTER" };
                        puow.CommitChanges();
                    }
                    splittertypeOid = GlobalSystemSettings.GetInstanceFromDatabase(puow).SplitterEquipmentType.Oid;

                    foreach (var row in inlist)
                    {
                        try
                        {
                            var splitter = new Equipment(puow);
                            //set to Splitter type default
                            //"ID", "NAME", "OBJ_REF_ID", "EQ_HOLDER_ID", "CR_SITE_ID", "STATUS", "CREATION_DATE"
                            //"", "", "OBJ_REF_ID", "EQ_HOLDER_ID", " ", " ", " "
                            //"", "", "OBJ_REF_ID", "EQ_HOLDER_ID", "", "", "CREATION_DATE"
                            splitter.EquipmentType = puow.GetObjectByKey<EquipmentType>(splittertypeOid);
                            splitter.ExternalSystemId = int.TryParse(row["ID"], out int splitterid) ? splitterid : 0;
                            splitter.SourceTable = splitterTable;
                            splitter.LocationName = row["NAME"];
                            splitter.Status = !string.IsNullOrWhiteSpace(row["STATUS"])
                ? NewNetServices.Module.Core.DefaultFields
                .GetBusinessObjectDefault<LocationStatus>(puow, "StatusName", row["STATUS"])
                : null;
                            if (!string.IsNullOrWhiteSpace(row["CREATION_DATE"]))
                                splitter.DatePlaced = DateTime.TryParse(row["CREATION_DATE"], out DateTime dt)
                    ? dt
                    : DateTime.Now;

                            splitter.Wirecenter = puow.Query<Wirecenter>()
                .FirstOrDefault(x => x.ExternalSystemId.ToString() == row["CR_SITE_ID"]);

                            puow.CommitChanges();
                            currentSuccess++;
                            ProgressMade?.Invoke(stepName,
                            new ProgressMadeEventArgs(new ImportedItem()
                            { RecordStatus = "Success" }));
                        }
                        catch (Exception ex)
                        {
                            currentErrors++;
                            ProgressMade?.Invoke(stepName,
                            new ProgressMadeEventArgs(new ImportedItem()
                            { RecordStatus = $"Exception {ex.Message}" }));
                            NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{ex}");
                        }
                    }
                }
            };
            WorkBeginning?.Invoke(stepName, null);
            await ProcessData(splitterTable, splitterCols, func, processorAction);
            WorkCompleted?.Invoke(stepName, null);
            //****************Splitter

        }

        public async Task Subscribers()
        {
            var stepName = "Subscribers";// new StackTrace().GetFrame(0).GetMethod().Name;
                                         //****************Splitter method
            Func<List<Dictionary<string, string>>,
            List<List<Dictionary<string, string>>>> func =
            (inlist) =>
            {
                List<List<Dictionary<string, string>>> ret = new List<List<Dictionary<string, string>>>();
                var subDataParts = inlist.Partition(GetSizeForPartition(inlist.Count));
                subDataParts.ForEach((subdata) => ret.Add(subdata.ToList()));

                return ret;
            };
            //action to take on eas=ch task data
            Action<List<Dictionary<string, string>>> processorAction = (inlist) =>
            {
                //do work
                inlist.ForEach((row) =>

                    {
                        //  "ADDYID", "STREET", "CITY", "STATE", "FLEXTEXT", "CODE", "SUBID"
                        using (var uow = new UnitOfWork(Tsdl))
                        {
                            Debug.Assert(uow != null, nameof(uow) + " != null");
                            if (ImporterHelper.ProcessSubscriber(uow,
                row,
                subTable,
                state,
                ProgressMade, stepName))
                            {
                                Console.WriteLine(@"Subscibers");
                            }
                            else
                            {
                                Console.WriteLine(@"Subscriber err");
                            }
                        } //end uow
                    }); //end for row

            };

            WorkBeginning?.Invoke(stepName, null);
            await ProcessData(subTable, subCols, func, processorAction);//.ConfigureAwait(true);
            WorkCompleted?.Invoke(stepName, null);

            StaticHelperMethods.WriteOut("Done????");
        }
        public async Task Addresses()
        {
            var stepName = "Addresses";// new StackTrace().GetFrame(0).GetMethod().Name;
                                       //****************Splitter method
            Func<List<Dictionary<string, string>>,
            List<List<Dictionary<string, string>>>> func =
            (inlist) =>
            {
                List<List<Dictionary<string, string>>> ret = new List<List<Dictionary<string, string>>>();
                var subDataParts = inlist.Partition(GetSizeForPartition(inlist.Count));
                subDataParts.ForEach((subdata) => ret.Add(subdata.ToList()));

                return ret;
            };
            //action to take on eas=ch task data
            Action<List<Dictionary<string, string>>> processorAction = (inlist) =>
            {
                //do work
                inlist.ForEach((row) =>
                    {
                        using (var uow = new UnitOfWork(Tsdl))
                        {
                            if (ImporterHelper.ProcessAddress(uow,
                row,
                addressTable,
                ProgressMade, stepName))
                            {
                                Console.WriteLine(@"Addresss");
                            }
                            else
                            {
                                Console.WriteLine(@"Address err");
                            }
                        } //end uow
                    }); //end for row

            };

            WorkBeginning?.Invoke(stepName, null);
            await ProcessData(addressTable, addCols, func, processorAction);//.ConfigureAwait(true);
            WorkCompleted?.Invoke(stepName, null);

            StaticHelperMethods.WriteOut("Done????");
        }
        public async Task Wirecenters()
        {
            var stepName = "Wirecenters";// new StackTrace().GetFrame(0).GetMethod().Name;

            Func<List<Dictionary<string, string>>, List<List<Dictionary<string, string>>>> func = (inlist) =>
            {
                List<List<Dictionary<string, string>>> ret = new List<List<Dictionary<string, string>>>();
                var pts = inlist.Partition(50);
                pts.ForEach((subdata) => ret.Add(subdata.ToList()));

                return ret;
            };
            Action<List<Dictionary<string, string>>> processorAction = (async (indata) =>
            {
                var data = (List<Dictionary<string, string>>)indata;
                using (var uow = new UnitOfWork(Tsdl))
                {
                    foreach (var row in data)
                    {//"REGION_ID", "REGION_CNL", "REGION_NAME", "CO_ID", "CO_CODE", "CO_NAME"
                        NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"WIRECENTER {string.Join(", ", row != null ? row.Select(x=>x.Value) : new string[] { "NOTHING" })}");
                        if (!uow.Query<Wirecenter>().Any(x => x.ExternalSystemId.ToString() == row["ID"]))
                        {
                            var wc = new Wirecenter(uow);
                            try
                            {
                                wc.ExternalSystemId = int.TryParse(row["ID"], out int coid) ? coid : 0;
                                wc.SourceTable = wcTable;
                                wc.Status = uow.Query<LocationStatus>().FirstOrDefault(x => x.StatusName ==
                                                                GlobalSystemSettings.LocationStatusUnknown);
                                wc.LocationName = !string.IsNullOrWhiteSpace(row["CO_NAME"]) ? row["CO_NAME"] : row["CO_CODE"];
                                wc.CLLI = row["CO_CODE"];
                                wc.FlexText = row["REGION_NAME"];
                                wc.FlexInt = int.TryParse(row["REGION_ID"], out int id) ? id : 0;
                                //description of Central Office or Remote and will use those as types.

                                if (!String.IsNullOrWhiteSpace(row["DESCRIPTION"]))
                                    wc.Type = uow.Query<WirecenterType>().FirstOrDefault(x => x.TypeName == row["DESCRIPTION"]);
                                else
                                    wc.Type = uow.Query<WirecenterType>().FirstOrDefault(x => x.TypeName == UNK);
                                currentSuccess++;
                                uow.CommitChanges();
                                ProgressMade?.Invoke(stepName,
                                      new ProgressMadeEventArgs(new ImportedItem()
                                      { ImportStatus = "Success", Type = "Wirecenter" }));
                            }
                            catch (Exception ex)
                            {
                                NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{ex}");
                                currentErrors++;
                                ProgressMade?.Invoke(stepName,
                                      new ProgressMadeEventArgs(new ImportedItem()
                                      { ImportStatus = "Exception " + ex.Message }));
                            }
                        }
                    }
                }//using
            });
            WorkBeginning?.Invoke(stepName, null);
            await ProcessData(wcTable, wcCols, func, processorAction);
            WorkCompleted?.Invoke(stepName, null);
            //            });
            //             t.Wait();
            StaticHelperMethods.WriteOut($"HI");
        }

        #endregion
        /// <summary>
        /// pass in row dictionary for link
        /// </summary>
    }//class
}//namespace
