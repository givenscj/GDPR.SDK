using GDPR.Applications;
using System;
using System.Collections.Generic;
using System.Web.Http;
using GDPR.Common.Classes;
using System.Configuration;
using System.IO;
using System.Reflection;
using GDPR.Common.Messages;
using GDPR.Common;
using Newtonsoft.Json;

namespace GDPR.SDK.Controllers
{
    public class GDPRWebApiController : ApiController, IGDPRDataSubjectActions
    {
        public EncryptionContext ctx { get; set; }
        public GDPRApplicationBase app { get; set; }

        public GDPRWebApiController()
        {
            //TODO: override this with your specific application class...
            string assemblyName = ConfigurationManager.AppSettings["ApplicationAssembly"];
            string typeclass = ConfigurationManager.AppSettings["ApplicationClass"];

            Assembly assembly = Assembly.Load(assemblyName);

            Type pType = Type.GetType(typeclass);
            app = (GDPRApplicationBase)Activator.CreateInstance(pType);
        }

        [System.Web.Http.Route("api/SendTestMessage")]
        [System.Web.Http.HttpGet]

        public void SendTestMessage()
        {
            EncryptionContext ctx = new EncryptionContext();
            ctx.Encrypt = true;
            ctx.Path = ConfigurationManager.AppSettings["PrivateKeyPath"];
            ctx.Id = ConfigurationManager.AppSettings["ApplicationId"];
            ctx.Password = ConfigurationManager.AppSettings["PrivateKeyPassword"];

            PingMessage pm = new PingMessage();
            pm.Status = "Hello World";
            pm.ApplicationId = Guid.Parse(ConfigurationManager.AppSettings["ApplicationId"]);

            MessageHelper.SendMessageViaQueue(pm, ctx);
        }

        public bool Authorize()
        {
            //get the public key in the request header...
            string publicKeyMessage = this.Request.Headers.GetValues("PublicKeyMessage").ToString();

            //utilize our private key to decrypt...


            return false;
        }

        [System.Web.Http.Route("api/Ping")]
        [System.Web.Http.HttpGet]

        public string Ping()
        {
            return "Pong";
        }

        public List<Record> GetAllRecords(GDPRSubject search)
        {
            List<Record> msgs = app.GetAllRecords(search);
            return msgs;
        }

        public List<GDPRSubject> RecordSearch(GDPRSubject search)
        {
            List<GDPRSubject> results = app.SubjectSearch(search);
            return results;
        }

        public ExportInfo ExportData(GDPRSubject subject)
        {
            return app.ExportData("", subject);            
        }

        public ExportInfo ExportData(string applicationSubjectId)
        {
            return app.ExportData(applicationSubjectId);
        }

        public void ValidateSubject(GDPRSubject subject)
        {
            ((IGDPRDataSubjectActions)app).ValidateSubject(subject);
        }

        
        public void AnonymizeRecord(Record r)
        {
            ((IGDPRDataSubjectActions)app).AnonymizeRecord(r);
        }

        public List<GDPRSubject> SubjectSearch(GDPRSubject search)
        {
            return ((IGDPRDataSubjectActions)app).SubjectSearch(search);
        }

        


        public List<GDPRSubject> GetAllSubjects(int skip, int count, DateTime? changeDate)
        {
            return ((IGDPRDataSubjectActions)app).GetAllSubjects(skip, count, changeDate);
        }

        public List<BaseGDPRMessage> GetChanges(DateTime changeDate)
        {
            return ((IGDPRDataSubjectActions)app).GetChanges(changeDate);
        }

        public ExportInfo ExportData(string applicationSubjectId, GDPRSubject s)
        {
            return ((IGDPRDataSubjectActions)app).ExportData(applicationSubjectId, s);
        }

        public void Discover()
        {
            ((IGDPRDataSubjectActions)app).Discover();
        }

        [System.Web.Http.Route("api/RecieveRequest")]
        [System.Web.Http.HttpPost]
        public string RecieveRequest([FromBody] string body)
        {
            bool valid = false;

            try
            {
                GDPRMessageWrapper message = JsonConvert.DeserializeObject<GDPRMessageWrapper>(body);

                if (message.IsEncrypted)
                {
                    valid = MessageHelper.ValidateMessage(message);
                }
                else
                    valid = true;

                if (valid)
                {
                    Type t = Type.GetType("GDPR.Common.Messages.BaseApplicationMessage, GDPR.Common, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
                    BaseApplicationMessage actionMessage = (BaseApplicationMessage)Newtonsoft.Json.JsonConvert.DeserializeObject(message.Object, t);
                    ProcessRequest(actionMessage, ctx);
                }
                else
                    return "NotValid";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return "OK";
        }

        public void ProcessRequest(BaseApplicationMessage message, EncryptionContext ctx)
        {
            if (ctx == null)
                app.ProcessRequest(message, this.ctx);
            else
                app.ProcessRequest(message, ctx);
        }

        public void Consent(string applicationSubjectId)
        {
            app.Consent(applicationSubjectId);
        }

        public void Unconsent(string applicationSubjectId)
        {
            app.Unconsent(applicationSubjectId);
        }

        List<Record> IGDPRDataSubjectActions.GetAllRecords(GDPRSubject search)
        {
            return app.GetAllRecords(search);
        }

        public ExportInfo ExportData(List<Record> records)
        {
            return app.ExportData(records);
        }

        public void AnonymizeSubject(GDPRSubject subject)
        {
            app.AnonymizeSubject(subject);
        }

        public bool SubjectCreateIn(GDPRSubject subject)
        {
            return app.SubjectCreateIn(subject);
        }

        public bool SubjectCreateOut(GDPRSubject subject)
        {
            return app.SubjectCreateOut(subject);
        }

        public RecordCollection SubjectDeleteIn(GDPRSubject subject)
        {
            return app.SubjectDeleteIn(subject);
        }

        public bool SubjectDeleteOut(GDPRSubject subject)
        {
            return app.SubjectDeleteOut(subject);
        }

        public bool SubjectUpdateIn(GDPRSubject subject)
        {
            return app.SubjectUpdateIn(subject);
        }

        public bool SubjectUpdateOut(GDPRSubject subject)
        {
            return app.SubjectUpdateOut(subject);
        }

        public bool SubjectHoldIn(GDPRSubject subject)
        {
            return app.SubjectHoldIn(subject);
        }

        public bool SubjectHoldOut(GDPRSubject subject)
        {
            return app.SubjectHoldOut(subject);
        }

        public void SubjectNotify(GDPRSubject subject)
        {
            app.SubjectNotify(subject);
        }

        public bool RecordCreateIn(Record r)
        {
            return RecordCreateIn(r);
        }

        public bool RecordCreateOut(Record r)
        {
            return RecordCreateOut(r);
        }

        public bool RecordDeleteIn(Record r)
        {
            return RecordDeleteIn(r);
        }

        public bool RecordDeleteOut(Record r)
        {
            return RecordDeleteOut(r);
        }

        public void RecordHold(Record r)
        {
            RecordHold(r);
        }

        public bool RecordUpdateIn(Record old, Record update)
        {
            return RecordUpdateIn(old, update);
        }

        public bool RecordUpdateOut(Record r)
        {
            return RecordUpdateOut(r);
        }

        public void PhoneNormalization()
        {
            throw new NotImplementedException();
        }
    }
}
