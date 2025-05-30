using AutomatedTesting.Models;
using AutomatedTesting.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using VueJSMVCDotNet;
using VueJSMVCDotNet.Attributes.ModelHandlers;
using VueJSMVCDotNet.Endpoints.Model;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Handlers
{
    [ModelRouteAttribute(Constants.PersonModelRoute)]
    [SecurityRoleCheck(Constants.Rights.CAN_ACCESS)]
    public class mPersonHandler(IDataStore store) : IModelHandler<mPerson>
    {
        public static mPerson[] Persons => new mPerson[]{
                new mPerson(1111,"Bob","Loblaw"),
                new mPerson(2222,"Fred","Flinston"),
                new mPerson(3333,"Barney","Rumble")
            };

        public const string KEY = "Persons";

        [SecurityRoleCheck(Constants.Rights.LOAD)]
        ValueTask<mPerson> IModelHandler<mPerson>.LoadAsync(string id)
        {
            mPerson ret = null;
            var persons = (mPerson[])store[KEY]??Persons;
            foreach (mPerson per in persons)
            {
                if (id == per.id)
                {
                    ret = per;
                    break;
                }
            }
            return ValueTask.FromResult<mPerson>(ret);
        }

        [ModelLoadAllMethodAttribute()]
        [SecurityRoleCheck(Constants.Rights.LOAD_ALL)]
        public List<mPerson> LoadAll()
        {
            return new List<mPerson>((mPerson[])store[KEY]??Persons);
        }

        [ModelDeleteMethodAttribute()]
        [SecurityRoleCheck(Constants.Rights.DELETE)]
        public bool Delete([ModelIDParameter]string id)
        {
            bool ret = false;
            var persons = new List<mPerson>((mPerson[])store[KEY]??Persons);
            for (int x = 0; x < persons.Count; x++)
            {
                if (persons[x].id == id)
                {
                    persons.RemoveAt(x);
                    ret = true;
                    break;
                }
            }
            store[KEY]=persons.ToArray();
            return ret;
        }

        [ModelUpdateMethodAttribute()]
        [SecurityRoleCheck(Constants.Rights.UPDATE)]
        public bool Update([ModelInstanceParameter] mPerson person)
        {
            bool ret = false;
            var persons = new List<mPerson>((mPerson[])store[KEY]??Persons);
            for (int x = 0; x < persons.Count; x++)
            {
                if (persons[x].id == person.id)
                {
                    persons.RemoveAt(x);
                    persons.Insert(x, person);
                    ret = true;
                    break;
                }
            }
            store[KEY]=persons.ToArray();
            return ret;
        }

        [ModelSaveMethodAttribute()]
        [SecurityRoleCheck(Constants.Rights.SAVE)]
        public string Save([ModelInstanceParameter]mPerson person)
        {
            if (person.FirstName=="DoNotSave")
                return null;
            person.id = new Random().Next(999999).ToString();
            var persons = new List<mPerson>((mPerson[])store[KEY]??Persons);
            persons.Add(person);
            store[KEY] = persons.ToArray();
            return person.id;
        }

        [ModelListMethodAttribute()]
        public List<mPerson> ListBobs()
        {
            return new List<mPerson>((mPerson[])store[KEY]??Persons).Where(p => p.FirstName.ToLower()=="bob").ToList();
        }

        [ModelListMethodAttribute(paged: true)]
        public PagedResult<mPerson> ListBobsPaged([PageStartIndexParameter] int pageStartIndex, [PageSizeParameter]int pageSize)
        {
            mPerson[] bobs = new List<mPerson>((mPerson[])store[KEY]??Persons).Where(p => p.FirstName.ToLower()=="bob").ToArray();
            var totalPages=(int)Math.Ceiling((decimal)bobs.Length/(decimal)pageSize);
            return new(bobs.Skip(pageStartIndex).Take(pageSize).ToList(),totalPages);
        }

        #region List Pars

        [ModelListMethodAttribute()]
        public mPerson[] ListByDate(DateTime date, [FromServices] ILogger log)
        {
            log.LogTrace("Called List By Date");
            return Persons;
        }

        [ModelListMethodAttribute()]
        public mPerson[] ListByInt(int val, [FromServices] ILogger log)
        {
            log.LogTrace("Called List By Integer");
            return Persons;
        }

        [ModelListMethodAttribute()]
        public mPerson[] ListByLong(long val, [FromServices] ILogger log)
        {
            log.LogTrace("Called List By Long");
            return Persons;
        }

        [ModelListMethodAttribute()]
        public mPerson[] ListByShort(short val, [FromServices] ILogger log)
        {
            log.LogTrace("Called List By Short");
            return Persons;
        }

        [ModelListMethodAttribute()]
        public mPerson[] ListByByte(byte val, [FromServices] ILogger log)
        {
            log.LogTrace("Called List By Byte");
            return Persons;
        }

        [ModelListMethodAttribute()]
        public mPerson[] ListByUInt(uint val, [FromServices] ILogger log)
        {
            log.LogTrace("Called List By UInteger");
            return Persons;
        }

        [ModelListMethodAttribute()]
        public mPerson[] ListByULong(ulong val, [FromServices] ILogger log)
        {
            log.LogTrace("Called List By ULong");
            return Persons;
        }

        [ModelListMethodAttribute()]
        public mPerson[] ListByUShort(ushort val, [FromServices] ILogger log)
        {
            log.LogTrace("Called List By UShort");
            return Persons;
        }

        [ModelListMethodAttribute()]
        public mPerson[] ListByDouble(double val, [FromServices] ILogger log)
        {
            log.LogTrace("Called List By Double");
            return Persons;
        }

        [ModelListMethodAttribute()]
        public mPerson[] ListByFloat(float val, [FromServices] ILogger log)
        {
            log.LogTrace("Called List By Float");
            return Persons;
        }

        [ModelListMethodAttribute()]
        public mPerson[] ListByDecimal(decimal val, [FromServices] ILogger log)
        {
            log.LogTrace("Called List By Decimal");
            return Persons;
        }

        [ModelListMethodAttribute()]
        public  mPerson[] ListByGuid(Guid val, [FromServices] ILogger log)
        {
            log.LogTrace("Called List By Guid");
            return Persons;
        }

        [ModelListMethodAttribute()]
        public mPerson[] ListByEnum(mDataTypes.TestEnums val, [FromServices] ILogger log)
        {
            log.LogTrace("Called List By Enum");
            return Persons;
        }

        [ModelListMethodAttribute()]
        public mPerson[] ListByBoolean(bool val, [FromServices] ILogger log)
        {
            log.LogTrace("Called List By Boolean");
            return Persons;
        }

        #endregion

        [ModelListMethodAttribute(true)]
        [SecurityRoleCheck(Constants.Rights.SEARCH)]
        public PagedResult<mPerson> Search(string q, [PageStartIndexParameter] int pageStartIndex,[PageSizeParameter] int pageSize, ISecureSession session)
        {
            List<mPerson> ret = new List<mPerson>();
            var totalPages = 0;
            List<mPerson> matches = new List<mPerson>();
            var persons = new List<mPerson>((mPerson[])store[KEY]??Persons);
            if (q != null)
            {
                q = q.ToLower();
                for (int x = 0; x < persons.Count; x++)
                {
                    if (persons[x].FirstName.ToLower().Contains(q) ||
                    persons[x].LastName.ToLower().Contains(q))
                    {
                        matches.Add(Persons[x]);
                    }
                }

            }
            else
                matches.AddRange(Persons);
            totalPages = (int)Math.Ceiling((decimal)matches.Count / (decimal)pageSize);
            for (int x = 0; x < pageSize; x++)
            {
                if ((pageStartIndex*pageSize) + x >= matches.Count)
                {
                    break;
                }
                ret.Add(matches[(pageStartIndex * pageSize) + x]);
            }
            return new(ret,totalPages);
        }

        [ExposedMethodAttribute(false)]
        [SecurityRoleCheck(Constants.Rights.METHOD)]
        public string GetFullName([ModelInstanceParameter] mPerson person)
        => person.GetFullName();

        [ExposedMethodAttribute(false)]
        [SecurityRoleCheck(Constants.Rights.METHOD)]
        public string GetFullName([ModelInstanceParameter]mPerson person, string middleName)
        => person.GetFullName(middleName);

        [ExposedMethodAttribute]
        public bool IsNameMatch([ModelInstanceParameter]mPerson person,sName name)
        {
            return string.Equals(person.FirstName, name.FirstName, StringComparison.InvariantCultureIgnoreCase)
                &&string.Equals(person.LastName, name.LastName, StringComparison.InvariantCultureIgnoreCase);
        }

        [ExposedMethodAttribute]
        public void SetFullName([ModelIDParameter] string id,string fullName)
        {
            string[] tmp = fullName.Split(',');
            var persons = new List<mPerson>((mPerson[])store[KEY]??Persons);
            for (var x = 0; x<persons.Count; x++)
            {
                if (persons[x].id==id)
                {
                    var me = persons[x];
                    persons.RemoveAt(x);
                    me.FirstName = tmp[1].Trim();
                    me.LastName = tmp[0].Trim();
                    persons.Insert(x, me);
                }
            }
            store[KEY]=persons.ToArray();
        }

        [ExposedMethodAttribute]
        public bool IsFullName([ModelInstanceParameter] mPerson person, string fullName)
        {
            return fullName==string.Format("{0}, {1}", new object[] { person.LastName, person.FirstName });
        }

        [ExposedMethodAttribute]
        public void ThrowInstanceException([ModelIDParameter]string id)
        {
            throw new Exception("Error in Instance Method");
        }

        [ExposedMethodAttribute(allowNullResponse: false, isSlow: true)]
        public string GetInstanceSlowTimespan([ModelInstanceParameter]mPerson person)
        {
            DateTime now = DateTime.UtcNow;
            Task.Delay(3456).Wait();
            return string.Format("This call took {0} ms to complete", DateTime.UtcNow.Subtract(now).TotalMilliseconds);
        }

        [ExposedMethodAttribute(isSlow: true, arrayElementType: typeof(int))]
        public void InstanceSlowAddCall([ModelInstanceParameter] mPerson person, AddItem addCall)
        {
            int idx = 0;
            while (idx < 5)
            {
                System.Threading.Thread.Sleep(1000);
                addCall(idx, false);
                idx++;
            }
            addCall(idx, true);
        }

        [ExposedMethodAttribute(false)]
        [SecurityRoleCheck(Constants.Rights.STATIC_METHOD)]
        [NotNullArguementAttribute(new string[] { "lastName", "firstName" })]
        public string FormatName(ISecureSession session, string lastName, string firstName)
            => FormatName(lastName, firstName);

        internal static string FormatName(string lastName, string firstName)
            => string.Format("{0}, {1}", new object[] { firstName, lastName});

        [ExposedMethodAttribute(false)]
        [SecurityRoleCheck(Constants.Rights.STATIC_METHOD)]
        public string[] FormatNames(ISecureSession session, string[] lastName, string[] firstName)
        {
            List<string> result = new List<string>();
            for (int x = 0; x<lastName.Length; x++)
            {
                result.Add(FormatName(session, lastName[x], firstName[x]));
            }
            return result.ToArray();
        }


        [ExposedMethodAttribute(false)]
        [SecurityRoleCheck(Constants.Rights.STATIC_METHOD)]
        public string FormatName(ISecureSession session, string lastName, string middleName, string firstName)
        {
            return string.Format("{2}, {0} {1}", new object[] { firstName, middleName, lastName });
        }

        [ExposedMethodAttribute(true)]
        public object ProduceObject(bool isnull)
        {
            return (isnull ? null : new Hashtable()
            {
                {"key1","value1" }
            });
        }

        [ExposedMethodAttribute]
        public void VoidMethodCall(string parameter) { }

        #region SlowCalls

        [ExposedMethodAttribute(isSlow: true, arrayElementType: typeof(int))]
        public void SlowAddCall(AddItem addCall)
        {
            int idx = 0;
            while (idx < 5)
            {
                System.Threading.Thread.Sleep(1000);
                addCall(idx, false);
                idx++;
            }
            addCall(idx, true);
        }

        [ExposedMethodAttribute(allowNullResponse: false, isSlow: true)]
        public string GetSlowTimespan()
        {
            DateTime now = DateTime.UtcNow;
            System.Threading.Thread.Sleep(3456);
            return string.Format("This call took {0} ms to complete", DateTime.UtcNow.Subtract(now).TotalMilliseconds);
        }

        [ExposedMethodAttribute(allowNullResponse: false, isSlow: true)]
        public string GetSlowTimeout()
        {
            DateTime now = DateTime.UtcNow;
            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(120));
            return string.Format("This call took {0} ms to complete", DateTime.UtcNow.Subtract(now).TotalMilliseconds);
        }

        [ExposedMethodAttribute(allowNullResponse: false, isSlow: true)]
        public string GetSlowException()
        {
            DateTime now = DateTime.UtcNow;
            System.Threading.Thread.Sleep(3456);
            throw new Exception("something error happened");
        }
        #endregion

        [ExposedMethod]
        public bool CheckSpecialItems(HttpContext context, IHeaderDictionary headers, IRequestCookieCollection requestCookies, IResponseCookies responseCookies, IDataStore dataStore)
        {
            return context!=null && headers!=null && requestCookies!=null && requestCookies!=null && responseCookies!=null && dataStore!=null;
        }

        [EventStreamMethod]
        public async Task StreamUsers(ChannelWriter<object> writer,CancellationToken cancellationToken)
        {
            var idx = 0;
            while (!cancellationToken.IsCancellationRequested && idx<Persons.Length)
            {
                await writer.WriteAsync(Persons[idx], cancellationToken);
                await Task.Delay(TimeSpan.FromMilliseconds(50));
                idx++;
            }
            writer.Complete();
        }

        [EventStreamMethod]
        public async Task StreamUserProperties([ModelInstanceParameter]mPerson person, ChannelWriter<object> writer, CancellationToken cancellationToken)
        {
            await writer.WriteAsync(person.FirstName, cancellationToken);
            await Task.Delay(TimeSpan.FromMilliseconds(50));
            await writer.WriteAsync(person.LastName, cancellationToken);
            await Task.Delay(TimeSpan.FromMilliseconds(50));
            await writer.WriteAsync(person.Age, cancellationToken);
            await Task.Delay(TimeSpan.FromMilliseconds(50));
            writer.Complete();
        }

        [EventStreamMethod]
        public async Task StreamUserPropertiesById(int id, ChannelWriter<object> writer, CancellationToken cancellationToken)
        {
            var person = await ((IModelHandler<mPerson>)this).LoadAsync(id.ToString())!;
            await writer.WriteAsync(person.FirstName, cancellationToken);
            await Task.Delay(TimeSpan.FromMilliseconds(50));
            await writer.WriteAsync(person.LastName, cancellationToken);
            await Task.Delay(TimeSpan.FromMilliseconds(50));
            await writer.WriteAsync(person.Age, cancellationToken);
            await Task.Delay(TimeSpan.FromMilliseconds(50));
            writer.Complete();
        }
    }
}
