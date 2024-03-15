using AutomatedTesting.Security;
using VueJSMVCDotNet;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AutomatedTesting.Models
{
    public struct sName
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public sName(string firstname,string lastname)
        {
            FirstName=firstname;
            LastName=lastname;
        }

        public sName(mPerson person)
        {
            FirstName=person.FirstName;
            LastName=person.LastName;
        }

        public static implicit operator sName(Hashtable data)
        {
            return new sName(
                data["FirstName"].ToString(),
                data["LastName"].ToString()
            );
        }
    }

    [ModelRoute("/models/mPerson")]
    [ModelJSFilePath("/resources/scripts/mPerson.js")]
    [SecurityRoleCheck(Constants.Rights.CAN_ACCESS)]
    public class mPerson : IModel
    {
        private static Random _rnd = new Random((int)DateTime.Now.Ticks);

        private string _firstName;
        [ModelRequiredField()]
        public string FirstName { get { return _firstName; } set { _firstName = value; } }

        private string _lastName;
        [ModelRequiredField()]
        public string LastName { get { return _lastName; } set { _lastName = value; } }
        private DateTime _birthday = DateTime.Now.AddYears(-20);
        public DateTime BirthDay
        {
            get { return _birthday; }
            set { _birthday = value; }
        }

        public int Age
        {
            get
            {
                return (int)Math.Floor(DateTime.Now.Subtract(_birthday).TotalDays/365);
            }
        }


        private mPerson(int id,string firstName, string lastName)
        {
            _firstName = firstName;
            _lastName = lastName;
            _id = id;
        }

        public mPerson() { }

        private int _id = 0;
        public string id { get { return _id.ToString(); } }

        public static mPerson[] Persons => new mPerson[]{
                new mPerson(1111,"Bob","Loblaw"),
                new mPerson(2222,"Fred","Flinston"),
                new mPerson(3333,"Barney","Rumble")
            };

        public const string KEY = "Persons";

        [ModelLoadMethod()]
        [SecurityRoleCheck(Constants.Rights.LOAD)]
        public static Task<mPerson> Load(string id, ISecureSession session,IDataStore store)
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
            return Task.FromResult<mPerson>(ret);
        }

        [ModelLoadAllMethod()]
        [SecurityRoleCheck(Constants.Rights.LOAD_ALL)]
        public static List<mPerson> LoadAll(ISecureSession session,IDataStore store)
        {
            return new List<mPerson>((mPerson[])store[KEY]??Persons);
        }

        [ModelDeleteMethod()]
        [SecurityRoleCheck(Constants.Rights.DELETE)]
        public bool Delete(ISecureSession session,IDataStore store)
        {
            bool ret = false;
            var persons = new List<mPerson>((mPerson[])store[KEY]??Persons);
            for (int x = 0; x < persons.Count; x++)
            {
                if (persons[x].id == this.id)
                {
                    persons.RemoveAt(x);
                    ret = true;
                    break;
                }
            }
            store[KEY]=persons.ToArray();
            return ret;
        }

        [ModelUpdateMethod()]
        [SecurityRoleCheck(Constants.Rights.UPDATE)]
        public bool Update(ISecureSession session, IDataStore store)
        {
            bool ret = false;
            var persons = new List<mPerson>((mPerson[])store[KEY]??Persons);
            for (int x = 0; x < persons.Count; x++)
            {
                if (persons[x].id == this.id)
                {
                    persons.RemoveAt(x);
                    persons.Insert(x, this);
                    ret = true;
                    break;
                }
            }
            store[KEY]=persons.ToArray();
            return ret;
        }

        [ModelSaveMethod()]
        [SecurityRoleCheck(Constants.Rights.SAVE)]
        public bool Save(ISecureSession session,IDataStore store)
        {
            if (this.FirstName=="DoNotSave")
                return false;
            this._id = new Random().Next(999999);
            var persons = new List<mPerson>((mPerson[])store[KEY]??Persons);
            persons.Add(this);  
            store[KEY] = persons.ToArray();
            return true;
        }

        [ModelListMethod()]
        public static List<mPerson> ListBobs(IDataStore store)
        {
            return new List<mPerson>((mPerson[])store[KEY]??Persons).Where(p => p.FirstName.ToLower()=="bob").ToList();
        }

        [ModelListMethod(paged:true)]
        public static List<mPerson> ListBobsPaged(IDataStore store,int pageStartIndex, int pageSize, out int totalPages)
        {
            mPerson[] bobs = new List<mPerson>((mPerson[])store[KEY]??Persons).Where(p => p.FirstName.ToLower()=="bob").ToArray();
            totalPages=(int)Math.Ceiling((decimal)bobs.Length/(decimal)pageSize);
            return bobs.Skip(pageStartIndex).Take(pageSize).ToList();
        }

        #region List Pars

        [ModelListMethod()]
        public static mPerson[] ListByDate(DateTime date, ILogger log)
        {
            log.LogTrace("Called List By Date");
            return Persons;
        }

        [ModelListMethod()]
        public static mPerson[] ListByInt(int val, ILogger log)
        {
            log.LogTrace("Called List By Integer");
            return Persons;
        }

        [ModelListMethod()]
        public static mPerson[] ListByLong(long val, ILogger log)
        {
            log.LogTrace("Called List By Long");
            return Persons;
        }

        [ModelListMethod()]
        public static mPerson[] ListByShort(short val, ILogger log)
        {
            log.LogTrace("Called List By Short");
            return Persons;
        }

        [ModelListMethod()]
        public static mPerson[] ListByByte(byte val, ILogger log)
        {
            log.LogTrace("Called List By Byte");
            return Persons;
        }

        [ModelListMethod()]
        public static mPerson[] ListByUInt(uint val, ILogger log)
        {
            log.LogTrace("Called List By UInteger");
            return Persons;
        }

        [ModelListMethod()]
        public static mPerson[] ListByULong(ulong val, ILogger log)
        {
            log.LogTrace("Called List By ULong");
            return Persons;
        }

        [ModelListMethod()]
        public static mPerson[] ListByUShort(ushort val, ILogger log)
        {
            log.LogTrace("Called List By UShort");
            return Persons;
        }

        [ModelListMethod()]
        public static mPerson[] ListByDouble(double val, ILogger log)
        {
            log.LogTrace("Called List By Double");
            return Persons;
        }

        [ModelListMethod()]
        public static mPerson[] ListByFloat(float val, ILogger log)
        {
            log.LogTrace("Called List By Float");
            return Persons;
        }

        [ModelListMethod()]
        public static mPerson[] ListByDecimal(decimal val, ILogger log)
        {
            log.LogTrace("Called List By Decimal");
            return Persons;
        }

        [ModelListMethod()]
        public static mPerson[] ListByGuid(Guid val, ILogger log)
        {
            log.LogTrace("Called List By Guid");
            return Persons;
        }

        [ModelListMethod()]
        public static mPerson[] ListByEnum(mDataTypes.TestEnums val, ILogger log)
        {
            log.LogTrace("Called List By Enum");
            return Persons;
        }

        [ModelListMethod()]
        public static mPerson[] ListByBoolean(bool val, ILogger log)
        {
            log.LogTrace("Called List By Boolean");
            return Persons;
        }

        #endregion

        [ModelListMethod(true)]
        [SecurityRoleCheck(Constants.Rights.SEARCH)]
        public static List<mPerson> Search(string q, int pageStartIndex, int pageSize, out int totalPages, ISecureSession session,IDataStore store)
        {
            List<mPerson> ret = new List<mPerson>();
            totalPages = 0;
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
                matches.AddRange(mPerson.Persons);
            totalPages = (int)Math.Ceiling((decimal)matches.Count / (decimal)pageSize);
            for (int x = 0; x < pageSize; x++)
            {
                if ((pageStartIndex*pageSize) + x >= matches.Count)
                {
                    break;
                }
                ret.Add(matches[(pageStartIndex * pageSize) + x]);
            }
            return ret;
        }

        [ExposedMethod(false)]
        [SecurityRoleCheck(Constants.Rights.METHOD)]
        public string GetFullName(ISecureSession session)
        {
            return string.Format("{0}, {1}", new object[] { LastName, FirstName });
        }

        [ExposedMethod(false)]
        [SecurityRoleCheck(Constants.Rights.METHOD)]
        public string GetFullName(string middleName)
        {
            return string.Format("{0}, {1} {2}", new object[] { LastName, FirstName,middleName });
        }

        [ExposedMethod]
        public bool IsNameMatch(sName name)
        {
            return string.Equals(FirstName, name.FirstName, StringComparison.InvariantCultureIgnoreCase)
                &&string.Equals(LastName, name.LastName, StringComparison.InvariantCultureIgnoreCase);
        }

        [ExposedMethod]
        public void SetFullName(string fullName,IDataStore store)
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

        [ExposedMethod]
        public bool IsFullName(string fullName)
        {
            return fullName==string.Format("{0}, {1}", new object[] { LastName, FirstName });
        }

        [ExposedMethod]
        public void ThrowInstanceException()
        {
            throw new Exception("Error in Instance Method");
        }

        [ExposedMethod(allowNullResponse: false, isSlow: true)]
        public string GetInstanceSlowTimespan()
        {
            DateTime now = DateTime.Now;
            Task.Delay(3456).Wait();
            return string.Format("This call took {0} ms to complete", DateTime.Now.Subtract(now).TotalMilliseconds);
        }

        [ExposedMethod(isSlow: true, arrayElementType: typeof(int))]
        public void InstanceSlowAddCall(AddItem addCall)
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

        [ExposedMethod(false)]
        [SecurityRoleCheck(Constants.Rights.STATIC_METHOD)]
        [NotNullArguement(new string[] {"lastName","firstName"})]
        public static string FormatName(ISecureSession session,string lastName,string firstName)
        {
            return string.Format("{0}, {1}", new object[] { firstName, lastName });
        }

        [ExposedMethod(false)]
        [SecurityRoleCheck(Constants.Rights.STATIC_METHOD)]
        public static string[] FormatNames(ISecureSession session, string[] lastName, string[] firstName)
        {
            List<string> result = new List<string>();
            for(int x = 0; x<lastName.Length; x++)
            {
                result.Add(FormatName(session, lastName[x], firstName[x]));
            }
            return result.ToArray();
        }
        

        [ExposedMethod(false)]
        [SecurityRoleCheck(Constants.Rights.STATIC_METHOD)]
        public static string FormatName(ISecureSession session, string lastName,string middleName, string firstName)
        {
            return string.Format("{2}, {0} {1}", new object[] { firstName,middleName, lastName });
        }

        [ExposedMethod(true)]
        public static object ProduceObject(bool isnull)
        {
            return (isnull ? null : new Hashtable()
            {
                {"key1","value1" }
            });
        }

        [ExposedMethod]
        public static void VoidMethodCall(string parameter) { }

        #region SlowCalls

        [ExposedMethod(isSlow: true, arrayElementType: typeof(int))]
        public static void SlowAddCall(AddItem addCall)
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

        [ExposedMethod(allowNullResponse: false, isSlow: true)]
        public static string GetSlowTimespan()
        {
            DateTime now = DateTime.Now;
            System.Threading.Thread.Sleep(3456);
            return string.Format("This call took {0} ms to complete", DateTime.Now.Subtract(now).TotalMilliseconds);
        }

        [ExposedMethod(allowNullResponse: false, isSlow: true)]
        public static string GetSlowTimeout()
        {
            DateTime now = DateTime.Now;
            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(120));
            return string.Format("This call took {0} ms to complete", DateTime.Now.Subtract(now).TotalMilliseconds);
        }

        [ExposedMethod(allowNullResponse: false, isSlow: true)]
        public static string GetSlowException()
        {
            DateTime now = DateTime.Now;
            System.Threading.Thread.Sleep(3456);
            throw new Exception("something error happened");
        }
        #endregion
    }
}
