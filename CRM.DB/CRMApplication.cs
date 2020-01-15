using CRM.DB;
using GDPR.Common;
using GDPR.Common.Classes;
using GDPR.Common.Core;
using GDPR.Common.Enums;
using GDPR.Common.Messages;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.EntityClient;
using System.Linq;

namespace GDPR.Applications
{
    public class CRMApplication : GDPRApplicationBase
    {
        CRM.DB.CRMEntities e = new CRM.DB.CRMEntities();

        public CRMApplication()
        {
            this._version = "1.0.0.0";
            this._shortName = "CRM";
            this._longName = "CRM";

            this.TemplateId = Guid.Parse("602EC2FF-38C6-4E70-9D1A-94C6524B4ECD");

            e = GetCRMDBContext("");
        }

        public static CRMEntities GetCRMDBContext(string connectionString)
        {
            const string metaData = "res://*/CRM.csdl|res://*/CRM.ssdl|res://*/CRM.msl";
            const string appName = "EntityFramework";
            const string providerName = "System.Data.SqlClient";

            EntityConnectionStringBuilder efBuilder = new EntityConnectionStringBuilder();
            efBuilder.Metadata = metaData;
            efBuilder.Provider = providerName;
            efBuilder.ProviderConnectionString = connectionString;

            return new CRMEntities(efBuilder.ConnectionString);
        }

        public override List<Record> GetAllRecordTypes()
        {
            List<Record> items = new List<Record>();

            items.Add(new Record { Type = "User" });
            

            return items;
        }

        public override List<BaseGDPRMessage> GetChanges(DateTime changeDate)
        {
            List<BaseGDPRMessage> messages = new List<BaseGDPRMessage>();
            string sql = string.Format("select * from customer where createdate >= '{0}'", changeDate);
            //find all customers that are new...
            List <Customer> newcustomers = e.Customers.SqlQuery(sql).ToList();

            foreach(Customer c in newcustomers)
            {
                BaseCreateMessage cm = new BaseCreateMessage();
                cm.ApplicationSubjectId = c.CustomerId.ToString();
                cm.Direction = MessageDirection.TowardsPlatform;
                cm.ApplicationId = this.ApplicationId;
                GDPRSubject s = new GDPRSubject();
                s.EmailAddresses.Add(new GDPRSubjectEmail { EmailAddress = c.Email });
                cm.Subject = s;
                messages.Add(cm);
            }

            sql = string.Format("select * from customer where modifydate >= '{0}' and createdate < modifydate", changeDate);

            //find all customers that have been modified...
            List<Customer> modifiedcustomers = e.Customers.SqlQuery(sql).ToList();

            foreach(Customer c in modifiedcustomers)
            {
                BaseUpdateMessage cm = new BaseUpdateMessage();
                cm.ApplicationSubjectId = c.CustomerId.ToString();
                cm.Direction = MessageDirection.TowardsPlatform;
                cm.ApplicationId = this.ApplicationId;
                GDPRSubject s = new GDPRSubject();
                s.EmailAddresses.Add(new GDPRSubjectEmail { EmailAddress = c.Email });
                cm.Subject = s;
                messages.Add(cm);
            }            

            return messages;
        }
        
        void GetAllRecords()
        {
            List<Customer> customers = e.Customers.ToList();

            foreach(Customer c in customers)
            {
                BaseCreateMessage cm = new BaseCreateMessage();
                cm.ApplicationSubjectId = c.CustomerId.ToString();
                cm.ApplicationId = this.ApplicationId;
                core.SendMessage(cm, ctx);
            }
        }

        
        public override RecordCollection SubjectDeleteIn(GDPRSubject subject)
        {
            List<Record> records = new List<Record>();

            foreach (GDPRSubjectEmail se  in subject.EmailAddresses)
            {
                string sql = string.Format("Delete from Customer where email = '{0}'", se.EmailAddress);
                e.Database.ExecuteSqlCommand(sql);
            }

            return new RecordCollection(records);
        }

        public override List<Record> GetAllRecords(GDPRSubject search)
        {
            List<Record> subjects = new List<Record>();

            foreach (GDPRSubjectEmail se in search.EmailAddresses)
            {
                string sql = string.Format("select * from customer where email ='{0}'", se.EmailAddress);

                //find all customers that are new...
                List<Customer> searchResults = e.Customers.SqlQuery(sql).ToList();

                foreach (Customer c in searchResults)
                {
                    Record r = new Record();
                    r.RecordId = c.CustomerId.ToString();
                    r.Type = "Customer";
                    subjects.Add(r);
                }
            }

            return subjects;
        }

        public override ExportInfo ExportData(string applicationSubjectId, GDPRSubject s)
        {
            //package the customer record as a json file...
            string fileName = string.Format("CRM_{0}.json", applicationSubjectId);
            string sql = string.Format("select * from customer where customerid = '{0}'", applicationSubjectId);

            //find all customers that have been modified...
            Customer subject = e.Customers.SqlQuery("exec GetCustomer @p0", Guid.Parse(applicationSubjectId)).FirstOrDefault();
            string json = JsonConvert.SerializeObject(subject);

            string filePath = string.Format("c:\\temp\\{0}", fileName);
            System.IO.File.AppendAllText(filePath, json);

            //Copy the file to the storage account
            string blobUrl = GDPRCore.Current.UploadBlob(this.ApplicationId, filePath);

            ExportInfo ei = new ExportInfo();
            ei.Urls.Add(blobUrl);
            ei.FileType = "json";
            return ei;
        }
    }
}
