using AutomatedTesting.Security;
using Org.Reddragonit.VueJSMVCDotNet;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AutomatedTesting.Models
{
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


        private mPerson(string firstName, string lastName)
        {
            _firstName = firstName;
            _lastName = lastName;
            _id = Math.Abs(_rnd.Next(1,9999));
        }

        public mPerson() { }

        private int _id = 0;
        public string id { get { return _id.ToString(); } }

        private static List<mPerson> _persons = new List<mPerson>(new mPerson[]{
            new mPerson("Bob","Loblaw"),
            new mPerson("Fred","Flinston"),
            new mPerson("Barney","Rumble")
        });

        public static mPerson[] Persons { get { return _persons.ToArray(); } }

        [ModelLoadMethod()]
        [SecurityRoleCheck(Constants.Rights.LOAD)]
        public static mPerson Load(string id, ISecureSession session)
        {
            mPerson ret = null;
            foreach (mPerson per in _persons)
            {
                if (id == per.id)
                {
                    ret = per;
                    break;
                }
            }
            return ret;
        }

        [ModelLoadAllMethod()]
        [SecurityRoleCheck(Constants.Rights.LOAD_ALL)]
        public static List<mPerson> LoadAll(ISecureSession session)
        {
            return _persons;
        }

        [ModelDeleteMethod()]
        [SecurityRoleCheck(Constants.Rights.DELETE)]
        public bool Delete(ISecureSession session)
        {
            bool ret = false;
            for (int x = 0; x < _persons.Count; x++)
            {
                if (_persons[x].id == this.id)
                {
                    _persons.RemoveAt(x);
                    ret = true;
                    break;
                }
            }
            return ret;
        }

        [ModelUpdateMethod()]
        [SecurityRoleCheck(Constants.Rights.UPDATE)]
        public bool Update(ISecureSession session)
        {
            bool ret = false;
            for (int x = 0; x < _persons.Count; x++)
            {
                if (_persons[x].id == this.id)
                {
                    _persons.RemoveAt(x);
                    _persons.Insert(x, this);
                    ret = true;
                    break;
                }
            }
            return ret;
        }

        [ModelSaveMethod()]
        [SecurityRoleCheck(Constants.Rights.SAVE)]
        public bool Save(ISecureSession session)
        {
            this._id = new Random().Next(999999);
            _persons.Add(this);
            return true;
        }

        [ModelListMethod("/list/mPerson/bob")]
        public static List<mPerson> ListBobs()
        {
            return _persons.Where(p => p.FirstName.ToLower()=="bob").ToList();
        }

        [ModelListMethod("/list/mPerson/bob/pages",paged:true)]
        public static List<mPerson> ListBobsPaged(int pageStartIndex, int pageSize, out int totalPages)
        {
            mPerson[] bobs = _persons.Where(p => p.FirstName.ToLower()=="bob").ToArray();
            totalPages=(int)Math.Ceiling((decimal)bobs.Length/(decimal)pageSize);
            return bobs.Skip(pageStartIndex).Take(pageSize).ToList();
        }

        #region List Pars

        [ModelListMethod("/list/mPerson/bob/date?par={0}")]
        public static List<mPerson> ListByDate(DateTime date, ILog log)
        {
            log.Trace("Called List By Date");
            return _persons;
        }

        [ModelListMethod("/list/mPerson/bob/int?par={0}")]
        public static List<mPerson> ListByInt(int val, ILog log)
        {
            log.Trace("Called List By Integer");
            return _persons;
        }

        [ModelListMethod("/list/mPerson/bob/long?par={0}")]
        public static List<mPerson> ListByLong(long val, ILog log)
        {
            log.Trace("Called List By Long");
            return _persons;
        }

        [ModelListMethod("/list/mPerson/bob/short?par={0}")]
        public static List<mPerson> ListByShort(short val, ILog log)
        {
            log.Trace("Called List By Short");
            return _persons;
        }

        [ModelListMethod("/list/mPerson/bob/byte?par={0}")]
        public static List<mPerson> ListByByte(byte val, ILog log)
        {
            log.Trace("Called List By Byte");
            return _persons;
        }

        [ModelListMethod("/list/mPerson/bob/uint?par={0}")]
        public static List<mPerson> ListByUInt(uint val, ILog log)
        {
            log.Trace("Called List By UInteger");
            return _persons;
        }

        [ModelListMethod("/list/mPerson/bob/ulong?par={0}")]
        public static List<mPerson> ListByULong(ulong val, ILog log)
        {
            log.Trace("Called List By ULong");
            return _persons;
        }

        [ModelListMethod("/list/mPerson/bob/ushort?par={0}")]
        public static List<mPerson> ListByUShort(ushort val, ILog log)
        {
            log.Trace("Called List By UShort");
            return _persons;
        }

        [ModelListMethod("/list/mPerson/bob/double?par={0}")]
        public static List<mPerson> ListByDouble(double val, ILog log)
        {
            log.Trace("Called List By Double");
            return _persons;
        }

        [ModelListMethod("/list/mPerson/bob/float?par={0}")]
        public static List<mPerson> ListByFloat(float val, ILog log)
        {
            log.Trace("Called List By Float");
            return _persons;
        }

        [ModelListMethod("/list/mPerson/bob/decimal?par={0}")]
        public static List<mPerson> ListByDecimal(decimal val, ILog log)
        {
            log.Trace("Called List By Decimal");
            return _persons;
        }

        [ModelListMethod("/list/mPerson/bob/guid?par={0}")]
        public static List<mPerson> ListByGuid(Guid val, ILog log)
        {
            log.Trace("Called List By Guid");
            return _persons;
        }

        [ModelListMethod("/list/mPerson/bob/enum?par={0}")]
        public static List<mPerson> ListByEnum(mDataTypes.TestEnums val, ILog log)
        {
            log.Trace("Called List By Enum");
            return _persons;
        }

        [ModelListMethod("/list/mPerson/bob/boolean?par={0}")]
        public static List<mPerson> ListByBoolean(bool val, ILog log)
        {
            log.Debug("Called List By Boolean");
            return _persons;
        }

        #endregion

        [ModelListMethod("/search/mPerson?q={0}", true)]
        [SecurityRoleCheck(Constants.Rights.SEARCH)]
        public static List<mPerson> Search(string q, int pageStartIndex, int pageSize, out int totalPages, ISecureSession session)
        {
            List<mPerson> ret = new List<mPerson>();
            totalPages = 0;
            List<mPerson> matches = new List<mPerson>();
            if (q != null)
            {
                q = q.ToLower();
                for (int x = 0; x < _persons.Count; x++)
                {
                    if (_persons[x].FirstName.ToLower().Contains(q) ||
                    _persons[x].LastName.ToLower().Contains(q))
                    {
                        matches.Add(_persons[x]);
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
        public void SetFullName(string fullName)
        {
            string[] tmp = fullName.Split(',');
            _firstName=tmp[1].Trim();
            _lastName=tmp[0].Trim();
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
            System.Threading.Thread.Sleep(3456);
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
        public static string FormatName(ISecureSession session,string lastName,string firstName)
        {
            return string.Format("{0}, {1}", new object[] { firstName, lastName });
        }

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
