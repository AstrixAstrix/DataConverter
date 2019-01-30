using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using DevExpress.Xpo;

namespace DataConverter.XPO
{
    public class NewNetServicesUnitOfWorkFactory
    {
        public static UnitOfWork GetNewUnitOfWork(string cs)

        {
            var uow = new UnitOfWork()
            {
                AutoCreateOption = DevExpress.Xpo.DB.AutoCreateOption.SchemaAlreadyExists,
                ConnectionString = cs
            };
            return uow;
        }
    }
  
}