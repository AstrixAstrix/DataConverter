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
        public void AssignmentDPairs()
        {
            var stepName = new StackTrace().GetFrame(0).GetMethod().Name;// "Assignment DPairs";
            var t = System.Threading.Tasks.Task.Factory
           .StartNew(() =>
           {
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
               ProcessData(assignmentDPairsTable, assignmentDPairsCols, func, processorAction);
               WorkCompleted?.Invoke(stepName, null);
           });
            t.Wait();
        }
        public void AssignmentOlt()
        {
            var stepName = new StackTrace().GetFrame(0).GetMethod().Name;// "Assignment->OLT ";
            var t = System.Threading.Tasks.Task.Factory
             .StartNew(() =>
             {
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
                             //"ID" assignmentid?, "OLTPORTID"
                             //"ID" assignmentid?, "OLTPORTID"
                             try
                             {
                                 if (!string.IsNullOrWhiteSpace(row["ID"]) &&
                                     !string.IsNullOrWhiteSpace(row["OLTPORTID"]))
                                 {
                                     var sp = uow.Query<Equipment>()
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
                 ProcessData(assignmentOltTable, assignmentOltCols, func, processorAction);
                 WorkCompleted?.Invoke(stepName, null);
             });
            t.Wait();
        }

        public void AssignmentSplitPort()
        {
            var stepName = new StackTrace().GetFrame(0).GetMethod().Name;// "Assignment->SplitterPorts";
            var t = System.Threading.Tasks.Task.Factory
             .StartNew(() =>
             {
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
                                     var sp = uow.Query<Equipment>()
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
                 ProcessData(assignmentSplitPortTable, assignmentSplitPortCols, func, processorAction);
                 WorkCompleted?.Invoke(stepName, null);
             });
            t.Wait();
        }
        public void AssignmentsPrimaryLocations()
        {
            var stepName = new StackTrace().GetFrame(0).GetMethod().Name;// "Assignments and Primary Locations";
            var t = System.Threading.Tasks.Task.Factory
             .StartNew(() =>
             {
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
                             if (Skip > 0)
                             {
                                 Skip--;
                                 return;
                             }
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
                                                 ? DefaultFields.GetBusinessObjectDefault<AssignmentClass>(uow,
                                                                                                           "Class",
                                                                                                           row["ASSIGNMENTCLASS"])
                                                 : DefaultFields.GetBusinessObjectDefault<AssignmentClass>(uow,
                                                                                                           "Class",
                                                                                                           "UNKNOWN");
                                         assignment.Status = !string.IsNullOrWhiteSpace(row["STATUS"])
                                                 ? DefaultFields.GetBusinessObjectDefault<AssignmentStatus>(uow,
                                                                                                            "StatusName",
                                                                                                            row["Status"])
                                                 : DefaultFields.GetBusinessObjectDefault<AssignmentStatus>(uow,
                                                                                                            "StatusName",
                                                                                                            "UNKNOWN");
                                         if (!string.IsNullOrWhiteSpace(row["EFFECTIVEDATE"]))
                                             assignment.EffectiveDate = DateTime.TryParse(row["EFFECTIVEDATE"],
                                                                                              out DateTime dt)
                                                     ? dt
                                                     : DateTime.Now;
                                         assignment.CircuitID = row["CIRCUITID"];
                                         assignment.PrimaryLocation = uow.Query<Subscriber>()
                                                 .FirstOrDefault(x => x.ExternalSystemId.ToString() == row["SUBSCRIBERID"]);
                                         assignment.Type = DefaultFields.GetBusinessObjectDefault<AssignmentType>(uow,
                                                                                                            "TypeName",
                                                                                                            "UNKNOWN");
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
                 ProcessData(this.assignmentPrimlocTable, this.assignmentPrimlocCols, Func, processorAction);
                 WorkCompleted?.Invoke(stepName, null);
             });
            t.Wait();
        }
        public void CableCallouts()
        {
            var stepName = new StackTrace().GetFrame(0).GetMethod().Name;// "Cable Callouts";
            var t = System.Threading.Tasks.Task.Factory
            .StartNew(() =>
            {
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


                                string dgroupName = "";

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

                Parallel.ForEach(datalist, getCalloutActionact);
                WorkCompleted?.Invoke(stepName, null);
            });
            t.Wait();
        }

        public void CablePairs()
        {   CreateFileWithColumnHeaders(cpairsexeptionlist, cpColumns);
         
            var stepName = new StackTrace().GetFrame(0).GetMethod().Name;// "Cable Pairs";

            var t = System.Threading.Tasks.Task.Factory
             .StartNew(() =>
             {
                 Func<List<Dictionary<string, string>>,
                List<List<Dictionary<string, string>>>> func =
                (inlist) =>
                {
                    //list of each cable in this data set
                    var distinctablelist = inlist.Where(x => x.ContainsKey("CABLE") && x["CABLE"]?.Trim() != "")
                            .Select(x => x["CABLE"])
                            .Distinct();
                    var ret = new List<List<Dictionary<string, string>>>();
                    //have each cable's pairs handled on the same thread
                    foreach (var cableid in distinctablelist)
                    {
                        ret.Add(inlist.Where(x => x["CABLE"] ==
                                                        cableid)
                            .Select(x => x)
                            .ToList());
                    }
                    return ret;
                };
                 Action<List<Dictionary<string, string>>> processorAction = (inlist) =>
                 {
                     using (var uow = new UnitOfWork(Tsdl))
                     {
                         foreach (var row in inlist)
                         {

                             if (!uow.Query<PhysicalPair>().Any(x => x.ExternalSystemId.ToString() == row["ID"]))
                             {
                                 object locker = new object();
                                 try
                                 {
                                     //        //do a trans action and roll back if necessary

                                     if (!string.IsNullOrWhiteSpace(row["CABLE"])) //look up cable we probably impmorted already//{
                                     {
                                         try
                                         {
                                             var cable = uow.Query<PhysicalCable>()
                                                 .FirstOrDefault(x => x.ExternalSystemId.ToString() == row["CABLE"]);
                                             if (cable != null)
                                             {
                                                 var cpair = new PhysicalPair(uow);

                                                 cpair.ExternalSystemId = int.Parse(row["ID"]);
                                                 cpair.SourceTable = cpTable; // int.Parse(row["LOCATIONID"]);  
                                                 cpair.Status = !string.IsNullOrWhiteSpace(row["STATUS"])
                                                     ? uow.GetObjectByKey<CablePairStatus>(NewNetServices.Module.Core.DefaultFields
                                                         .GetStatus<CablePairStatus>(uow, row["STATUS"]))
                                                     : uow.GetObjectByKey<CablePairStatus>(NewNetServices.Module.Core.DefaultFields
                                                         .GetStatus<CablePairStatus>(uow, "UNKNOWN"));
                                                 cpair.PairNumber = int.Parse(row["NUM"]);
                                                 //store guid and add to cable afterwards to avoid locking issues
                                                 //lock (locker)
                                                 //{
                                                 //    cable_cpairs_List.Add(new Tuple<Guid, Guid>(cable.Oid, cpair.Oid));
                                                 cable.CablePairs.Add(cpair);
                                                 cpair.Cable = cable;
                                                 //}
                                                 uow.CommitChanges();
                                                 currentSuccess++;
                                                 ProgressMade?.Invoke(stepName,
                                                                      new ImporterHelper.ProgressMadeEventArgs(new ImportedItem()
                                                                      {
                                                                          Guid = cpair.Oid.ToString(),
                                                                          Id = cpair.ExternalSystemId?.ToString(),
                                                                          SourceTable = cpair.SourceTable,
                                                                          ImportStatus = "Success",
                                                                          RecordStatus = cpair.Status?.ToString(),
                                                                          Type = "CablePair"
                                                                      }));
                                             }
                                             // }
                                         }
                                         catch (Exception ex)
                                         {
                                             currentErrors++;
                                             NewNetServices.Module.Core.StaticHelperMethods
                                                 .WriteOut(@" 
                                                        puow.CommitChanges();
                                                        errorsCpairs++\n" +
                                                 $"{string.Join(", ", row)}" +
                                                 $"\n{ex.Source}" +
                                                 $"\n{ex.TargetSite}");
                                NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{string.Join(",", row)}\n", cpairsexeptionlist);
                                         }
                                     }
                                     else
                                     {
                                         NewNetServices.Module.Core.StaticHelperMethods
                                             .WriteOut(@"  uow.RollbackTransaction();
                                                throw new Exception($""Cable not found with id: {row[""CABLE ""]}"); //{ 
                                         throw new Exception($"Cable not found with id: {row["CABLE "]}"); //{
                                     }
                                 }
                                 catch (Exception ex)
                                 {
                                NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{string.Join(",", row)}\n",cpairsexeptionlist);
                                     currentErrors++;
                                     ProgressMade?.Invoke(stepName,
                                                          new ImporterHelper.ProgressMadeEventArgs(new ImportedItem()
                                                          {
                                                              SourceTable = cpTable,
                                                              ImportStatus = "Exception" + $"\t{ex.Message}",
                                                              Type = "Exception"
                                                          }));
                                     NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{ex}");
                                 }

                             }
                         }//for
                     }//using
                 };

                 WorkBeginning?.Invoke(stepName, EventArgs.Empty);
                 ProcessData(cpTable, cpColumns, func, processorAction);
                 WorkCompleted?.Invoke(stepName, EventArgs.Empty);
             });
            t.Wait();
        }

        public void Cables()
        {
            var stepName = new StackTrace().GetFrame(0).GetMethod().Name;// "Cables";
            var t = System.Threading.Tasks.Task.Factory
             .StartNew(() =>
             {
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
                 Action<List<Dictionary<string, string>>> processorAction = (cabdata) =>
                 {

                     using (UnitOfWork uow = new UnitOfWork(Tsdl))
                     {
                         //do work
                         cabdata.ForEach(async (row) =>
                             {
                                 if (Skip > 0)
                                 {
                                     Skip--;
                                     return;
                                 }
                                 bool test = false;
                                 //StaticHelperMethods.WriteOut($"{string.Join("\t", row.Select(x => x.Key + ":" + x.Value))}");
                                 //make sure not imported already
                                 test = (!uow.Query<Cable>()
                                        .Any(x => x.ExternalSystemId.ToString() == row["CABLEID"]));
                                 if (test)
                                 {
                                     await ImporterHelper.ProcessCable(uow,
                                                row,
                                                cabTable,
                                                ProgressMade,
                                                stepName);
                                     uow.CommitChanges();
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
                 ProcessData(cabTable, cabColumns, func, processorAction);
                 //await ProcessData(X cabTable, cabColumns, func, processor_action);
                 WorkCompleted?.Invoke(stepName, EventArgs.Empty);
             });
            t.Wait();
        }
        public void Conduits()
        {
            var stepName = new StackTrace().GetFrame(0).GetMethod().Name;// "Conduits";
            var t = System.Threading.Tasks.Task.Factory
         .StartNew(() =>
         {
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
             ProcessData(conTable, conColumns, func, processorAction);
             WorkCompleted?.Invoke(stepName, EventArgs.Empty);
         });
            t.Wait();
        } //endmethod//endmethod 

        public void DesignationGroups()
        {
            var stepName = new StackTrace().GetFrame(0).GetMethod().Name;// new StackTrace().GetFrame(0).GetMethod().Name;
            var t = System.Threading.Tasks.Task.Factory
            .StartNew(() =>
            {
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
                        if (Skip > 0)
                        {
                            Skip--;
                            return;
                        }
                        using (UnitOfWork uow = new UnitOfWork(Tsdl))
                        {
                            //StaticHelperMethods.WriteOut($"{string.Join("\t", row.Select(x => x.Key + ":" + x.Value))}");
                            //make sure not imported already
                            if (!uow.Query<DesignationGroup>()
                                    .Any(x => x.ExternalSystemId.ToString() == row["DGID"]))
                            {
                                try
                                {
                                    var dg = new DesignationGroup(uow);

                                    //{
                                    dg.ExternalSystemId = int.Parse(row["DGID"]);
                                    uow.CommitChanges();
                                    //    }
                                    dg.SourceTable = dgroupTable; // int.Parse(row["LOCATIONID"]);  
                                                                  // dg.Status = !string.IsNullOrWhiteSpace(row["STATUS"]) ? ImporterHelper.GetStatus<CableStatus>(uow, row["STATUS"]) : null;
                                    dg.CableName = row["DGNAME"];
                                    lock (lockbox) ///////////
                                    {
                                        dg.Wirecenter = DefaultFields
                                            .GetBusinessObjectDefault<Wirecenter>(uow, "LocationName", "UNKNOWN");
                                    }
                                    if (!string.IsNullOrWhiteSpace(row["SOURCE"]))
                                    {
                                        dg.Source = uow.Query<Location>()
                                            .FirstOrDefault(x => x.ExternalSystemId.ToString() == row["SOURCE"]);
                                    }
                                    uow.CommitChanges();
                                    currentSuccess++;
                                    ProgressMade?.Invoke(stepName,
                                        new ProgressMadeEventArgs(new ImportedItem()
                                        {
                                            Guid = dg.Oid.ToString(),
                                            Id = dg.ExternalSystemId?.ToString(),
                                            SourceTable = dg.SourceTable,
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
                ProcessData(dgroupTable, dgroupColumns, func, processorAction);
                WorkCompleted?.Invoke(stepName, EventArgs.Empty);
            });
            t.Wait();


        }
        public const string dpairsexeptionlist = @"C:/EXES/dpX.csv";
        public const string cpairsexeptionlist = @"C:/EXES/cpX.csv";
        public const string cableexeptionlist = @"C:/EXES/cableX.csv";
        public void DesignationPairs()
        {
            CreateFileWithColumnHeaders(dpairsexeptionlist, dpColumns);
            var stepName = new StackTrace().GetFrame(0).GetMethod().Name;
            var t = System.Threading.Tasks.Task.Factory
            .StartNew(() =>
            {
                Func<List<Dictionary<string, string>>,
                    List<List<Dictionary<string, string>>>> func =
                    (inlist) =>
                    {
                        //list of each cable in this data set
                        var distinctablelist = inlist.Where(x => x.ContainsKey("DGROUP") && x["DGROUP"]?.Trim() != "")
                                .Select(x => x["DGROUP"])
                                .Distinct();
                        var ret = new List<List<Dictionary<string, string>>>();
                        //have each cable's pairs handled on the same thread
                        foreach (var cableid in distinctablelist)
                        {
                            ret.Add(inlist.Where(x => x["DGROUP"] ==
                                                      cableid)
                                .Select(x => x)
                                .ToList());
                        }
                        return ret;
                    };
                Action<List<Dictionary<string, string>>> processorAction = (inlist) =>
                {
                    foreach (var row in inlist)
                    {
                        using (var puow = new UnitOfWork(Tsdl))
                        {
                            try
                            {
                                if (!puow.Query<DesignationPair>()
                                    .Any(x => x.ExternalSystemId.ToString() == row["ID"]))
                                {


                                    if (!string.IsNullOrWhiteSpace(row["DGROUP"])
                                    ) //look up designation we probably omorted already//{
                                    {
                                        try
                                        {
                                            var dgroup = puow.Query<DesignationGroup>()
                                                .FirstOrDefault(x => x.ExternalSystemId.ToString() ==
                                                                     row["DGROUP"]);
                                            if (dgroup != null)
                                            {
                                                var dp = new DesignationPair(puow);
                                                dp.ExternalSystemId = int.Parse(row["ID"]);
                                                dp.SourceTable = dpTable;
                                                dp.PairNumber = int.Parse(row["COUNT"]);
                                                dp.Status = puow.GetObjectByKey<CablePairStatus>(NewNetServices.Module.Core.DefaultFields
                                                    .GetStatus<CablePairStatus>(puow, "UNKNOWN"));
                                                dgroup.DesignationPairs.Add(dp);

                                                puow.CommitChanges();
                                                currentSuccess++;
                                            }
                                            else
                                            {
                                                throw new Exception(
                                                    $"DesignationGroup not found with id: {row["DGROUP"]}");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"    {ex}");
                                        }
                                    }
                                    else throw new Exception($"DesignationGroup info missing "); //{
                                } //endif exist
                                ProgressMade?.Invoke(stepName,
                                             new ProgressMadeEventArgs(new ImportedItem()
                                             {
                                                 ImportStatus = "Success",
                                                 Type = "DesignationPair"
                                             }));
                            }
                            catch (Exception ex)
                            {
                                NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{string.Join(",", row)}\n", dpairsexeptionlist);
                                ProgressMade?.Invoke(stepName,
                                    new ProgressMadeEventArgs(new ImportedItem()
                                    {
                                        SourceTable = dpTable,
                                        ImportStatus = "Exception" + $"\t{ex.Message}",
                                        Type = "Exception"
                                    }));
                                NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"{ex}");
                            }
                        } //end using
                    } //endfor
                };
                WorkBeginning?.Invoke(stepName, EventArgs.Empty);
                ProcessData(dpTable, dpColumns, func, processorAction);
                WorkCompleted?.Invoke(stepName, EventArgs.Empty);
            });
            t.Wait();
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
        public void DesignationPairsCablePairLink()
        {
            var stepName = new StackTrace().GetFrame(0).GetMethod().Name;
            var t = System.Threading.Tasks.Task.Factory
            .StartNew(() =>
            {
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
                ProcessData(this.cpdpTable, this.cpdpColumns, Func, processorAction);
                WorkCompleted?.Invoke(stepName, null);
            });
            t.Wait();
        }
        public void Junctions()
        //public void Junctions()
        {
            var stepName = new StackTrace().GetFrame(0).GetMethod().Name;// "Junctions";

            int I = 0;

            /////*******************
            var t = System.Threading.Tasks.Task.Factory
           .StartNew(() =>
           {
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
                                       if (Skip > 0)
                                       {
                                           Skip--;
                                           return;
                                       }
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
               ProcessData(junkTable, junctionCols, func, processorAction);
               WorkCompleted?.Invoke(stepName, EventArgs.Empty);
           });
            t.Wait();

            /////*******************
        }//
        public void OLT_Splitter_DP()
        {
            var stepName = new StackTrace().GetFrame(0).GetMethod().Name;
            //****************OLT>splitter>DP method
            //method to split into separate tasks
            var t = System.Threading.Tasks.Task.Factory
              .StartNew(() =>
              {
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
                          var splittertypeOid = (GlobalSystemSettings.GetInstanceFromDatabase(puow).SplitterEquipmentType !=
                                  null
                              ? GlobalSystemSettings.GetInstanceFromDatabase(puow).SplitterEquipmentType
                              : NewNetServices.Module.Core.DefaultFields
                                  .GetBusinessObjectDefault<EquipmentType>(puow, "TypeName", "SPLITTER")).Oid;
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
                                  //set to Splitter type default
                                  //"SPLITTERID", "INCABLEID", "INCABLENAME", "INCABLEOBJECTREF", "INCOUNTID", "INCOUNT", "OLTID"
                                  //"SPLITTERID", "INCABLEID", "INCABLENAME", "INCABLEOBJECTREF", "INCOUNTID", "INCOUNT", "OLTID"
                                  //TODO:
                                  puow.CommitTransaction();
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
                                  puow.RollbackTransaction();
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
                  ProcessData(oltSplitterDpTable, oltSplitterDpCols, func, processorAction);
                  WorkCompleted?.Invoke(stepName, null);
              });
            t.Wait();
        }
        //****************OLT>splitter>DP}
        public void OltPorts()
        {
            var stepName = new StackTrace().GetFrame(0).GetMethod().Name;
            var t = System.Threading.Tasks.Task.Factory
             .StartNew(() =>
             {   //****************Splitter method
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
                 ProcessData(oltPortsTable, oltPortsCols, func, processorAction);
                 WorkCompleted?.Invoke(stepName, null);
             });
            t.Wait();
            //****************OLTports
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public void Olts()
        {
            var stepName = new StackTrace().GetFrame(0).GetMethod().Name;
            var t = System.Threading.Tasks.Task.Factory
            .StartNew(() =>
            {  //****************Splitter method
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
                                if (Skip > 0)
                                {
                                    Skip--;
                                    return;
                                }
                                if (!puow.Query<Equipment>().Any(x => x.ExternalSystemId.ToString() == row["OLT_ID"]))
                                {
                                    var olttypeOid = (GlobalSystemSettings.GetInstanceFromDatabase(puow).OLT_EquipmentType != null
                                     ? GlobalSystemSettings.GetInstanceFromDatabase(puow).OLT_EquipmentType
                                     : NewNetServices.Module.Core.DefaultFields.GetBusinessObjectDefault<EquipmentType>(puow, "TypeName", "OLT")).Oid;

                                    if (puow.InTransaction)
                                    {
                                        puow.RollbackTransaction();
                                        NewNetServices.Module.Core.StaticHelperMethods.WriteOut($"Intrans");
                                    }
                                    puow.BeginTransaction();
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
                ProcessData(oltTable, oltCols, func, processorAction);
                WorkCompleted?.Invoke(stepName, null);
            });
            t.Wait();
            //****************OLT
        }

        public void OutDesignationPairs()
        {
            var stepName = new StackTrace().GetFrame(0).GetMethod().Name;
            //set splitter port as source for dpairs
            var t = System.Threading.Tasks.Task.Factory
            .StartNew(() =>
            {
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
                ProcessData(outdPairsTable, outdPairsCols, func, processorAction);
                WorkCompleted?.Invoke(stepName, null);
            });
            t.Wait();
        }
        public void SplitterPorts()
        {
            var stepName = new StackTrace().GetFrame(0).GetMethod().Name;
            var t = System.Threading.Tasks.Task.Factory
            .StartNew(() =>
            {   //****************Splitter method

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
                        var splitterPorttypeOid = (GlobalSystemSettings.GetInstanceFromDatabase(puow).SplitterPortType !=
                                null
                            ? GlobalSystemSettings.GetInstanceFromDatabase(puow).SplitterPortType
                            : NewNetServices.Module.Core.DefaultFields
                                .GetBusinessObjectDefault<PortType>(puow, "TypeName", "SPLITTERPORT")).Oid;
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
                                    ? NewNetServices.Module.Core.DefaultFields
                                        .GetBusinessObjectDefault<LocationStatus>(puow, "StatusName", row["STATUS"])
                                    : null;
                                splitterPort.Equipment = puow.Query<Equipment>()
                                    .FirstOrDefault(x => x.ExternalSystemId.ToString() == row["CR_EQUIPMENT_ID"]);

                                splitterPort.Wirecenter = splitterPort.Equipment?.Wirecenter;

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
                ProcessData(splitterPortsTable, splitterPortsCols, func, processorAction);
                WorkCompleted?.Invoke(stepName, null);
                //****************SplitterPort
            });
            t.Wait();
        }
        public void Splitters()
        {
            var stepName = new StackTrace().GetFrame(0).GetMethod().Name;
            var t = System.Threading.Tasks.Task.Factory
           .StartNew(() =>
           {  //****************Splitter method
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
                       var splittertypeOid = (GlobalSystemSettings.GetInstanceFromDatabase(puow).SplitterEquipmentType !=
                               null
                           ? GlobalSystemSettings.GetInstanceFromDatabase(puow).SplitterEquipmentType
                           : NewNetServices.Module.Core.DefaultFields
                               .GetBusinessObjectDefault<EquipmentType>(puow, "TypeName", "SPLITTER")).Oid;

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
               ProcessData(splitterTable, splitterCols, func, processorAction);
               WorkCompleted?.Invoke(stepName, null);
               //****************Splitter
           });
            t.Wait();
        }

        public void Subscribers()
        {
            var stepName = new StackTrace().GetFrame(0).GetMethod().Name;
            Task t = System.Threading.Tasks.Task.Factory
            .StartNew(() =>
            {    //****************Splitter method
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
                                if (Skip > 0)
                                {
                                    Skip--;
                                    return;
                                }
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
                ProcessData(subTable, subCols, func, processorAction);//.ConfigureAwait(true);
                WorkCompleted?.Invoke(stepName, null);
            });
            t.Wait();

        }
        public void Wirecenters()
        {
            var stepName = new StackTrace().GetFrame(0).GetMethod().Name;
            var t = System.Threading.Tasks.Task.Factory
           .StartNew(() =>
           {
               Func<List<Dictionary<string, string>>, List<List<Dictionary<string, string>>>> func = (inlist) =>
               {
                   List<List<Dictionary<string, string>>> ret = new List<List<Dictionary<string, string>>>();
                   var pts = inlist.Partition(GetSizeForPartition(inlist.Count));
                   pts.ForEach((subdata) => ret.Add(subdata.ToList()));

                   return ret;
               };
               Action<List<Dictionary<string, string>>> processorAction = ((indata) =>
               {
                   var data = (List<Dictionary<string, string>>)indata;
                   using (var uow = new UnitOfWork(Tsdl))
                   {
                       foreach (var row in data)
                       {//"REGION_ID", "REGION_CNL", "REGION_NAME", "CO_ID", "CO_CODE", "CO_NAME"
                           if (Skip > 0)
                           {
                               Skip--;
                               return;
                           }
                           if (!uow.Query<Wirecenter>().Any(x => x.ExternalSystemId.ToString() == row["CO_ID"]))
                           {
                               uow.BeginTransaction();
                               var wc = new Wirecenter(uow);
                               try
                               {
                                   wc.ExternalSystemId = int.TryParse(row["CO_ID"], out int coid) ? coid : 0;
                                   wc.SourceTable = wcTable;
                                   wc.Status = NewNetServices.Module.Core.DefaultFields
                                         .GetBusinessObjectDefault<LocationStatus>(uow,
                                                                                   "StatusName",
                                                                                   GlobalSystemSettings.LocationStatusUnknown);
                                   wc.LocationName = row["CO_NAME"];
                                   wc.CLLI = row["CO_CODE"];
                                   wc.FlexText = row["REGION_NAME"];
                                   wc.FlexInt = int.TryParse(row["REGION_ID"], out int id) ? id : 0;
                                   currentSuccess++;
                                   uow.CommitTransaction();
                                   uow.CommitChanges();
                                   ProgressMade?.Invoke(stepName,
                                                          new ProgressMadeEventArgs(new ImportedItem()
                                                          { ImportStatus = "Success", Type = "Wirecenter" }));
                               }
                               catch (Exception ex)
                               {
                                   uow.RollbackTransaction();
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
               ProcessData(wcTable, wcCols, func, processorAction);
               WorkCompleted?.Invoke(stepName, null);
           });
            t.Wait();

        }
        /// <summary>
        /// pass in row dictionary for link
        /// </summary>

        #endregion
    }//class
}//namespace
