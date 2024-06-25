using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using VueJSMVCDotNet;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

namespace TestApplication
{
    [ModelRouteAttribute("/models/mPerson")]
    [ModelJSFilePath("/resources/scripts/mPerson.js")]
    public class mPerson : IModel
    {
        private static Random _rnd = new Random((int)DateTime.Now.Ticks);

        private string _firstName;
        [ModelRequiredFieldAttribute()]
        public string FirstName { get { return _firstName; } set { _firstName=value; } }

        private string _lastName;
        [ModelRequiredFieldAttribute()]
        public string LastName { get { return _lastName; } set { _lastName=value; } }
        private DateTime _birthday;
        public DateTime BirthDay
        {
            get { return _birthday; }
            set { _birthday=value; }
        }


        private Guid? _testNullable;
        public Guid? TestNullable
        {
            get { return _testNullable; }
            set { _testNullable=value; }
        }

        private mPerson(string firstName, string lastName)
        {
            _firstName=firstName;
            _lastName=lastName;
            _id = Math.Abs(_rnd.Next());
        }

        public mPerson() { }
        private int _id = 0;
        public string id { get { return _id.ToString(); } }

        private static List<mPerson> _persons = new List<mPerson>(new mPerson[]{
            new mPerson("Bob","Loblaw"),
            new mPerson("Fred","Flinston"),
            new mPerson("Barney","Rumble")
        });

        [ModelLoadMethodAttribute()]
        public static mPerson Load(string id, ISecureSession session)
        {
            System.Diagnostics.Debug.WriteLine(((SessionManager)session).Start);
            mPerson ret = null;
            foreach (mPerson per in _persons)
            {
                if (id==per.id)
                {
                    ret=per;
                    break;
                }
            }
            return ret;
        }

        [ModelLoadAllMethodAttribute()]
        public static List<mPerson> LoadAll(ISecureSession session)
        {
            System.Diagnostics.Debug.WriteLine(((SessionManager)session).Start);
            return _persons;
        }

        [ModelDeleteMethodAttribute()]
        public bool Delete(ISecureSession session)
        {
            System.Diagnostics.Debug.WriteLine(((SessionManager)session).Start);
            bool ret = false;
            for (int x = 0; x<_persons.Count; x++)
            {
                if (_persons[x].id==this.id)
                {
                    _persons.RemoveAt(x);
                    ret=true;
                    break;
                }
            }
            return ret;
        }

        [ModelUpdateMethodAttribute()]
        public bool Update(ISecureSession session)
        {
            System.Diagnostics.Debug.WriteLine(((SessionManager)session).Start);
            bool ret = false;
            for (int x = 0; x<_persons.Count; x++)
            {
                if (_persons[x].id==this.id)
                {
                    _persons.RemoveAt(x);
                    _persons.Insert(x, this);
                    ret=true;
                    break;
                }
            }
            return ret;
        }

        [ModelSaveMethodAttribute()]
        public bool Save(ISecureSession session)
        {
            System.Diagnostics.Debug.WriteLine(((SessionManager)session).Start);
            this._id = new Random().Next(999999);
            _persons.Add(this);
            return true;
        }

        [ModelListMethodAttribute(true)]
        public static List<mPerson> Search(string q, int pageStartIndex, int pageSize, out int totalPages, ISecureSession session)
        {
            System.Diagnostics.Debug.WriteLine(((SessionManager)session).Start);
            List<mPerson> ret = new List<mPerson>();
            totalPages=0;
            if (q != null)
            {
                q = q.ToLower();
                List<mPerson> matches = new List<mPerson>();
                for (int x = 0; x < _persons.Count; x++)
                {
                    if (_persons[x].FirstName.ToLower().Contains(q) ||
                    _persons[x].LastName.ToLower().Contains(q))
                    {
                        matches.Add(_persons[x]);
                    }
                }
                totalPages = (int)Math.Ceiling((decimal)matches.Count / (decimal)pageSize);
                for (int x = 0; x < pageSize; x++)
                {
                    if (pageStartIndex + x >= matches.Count)
                    {
                        break;
                    }
                    ret.Add(matches[pageStartIndex + x]);
                }
            }
            return ret;
        }

        [ExposedMethodAttribute(false)]
        public string GetFullName(ISessionManager session)
        {
            System.Diagnostics.Debug.WriteLine(session.Start);
            return string.Format("{0}, {1}", new object[] { LastName, FirstName });
        }

        [ExposedMethodAttribute(true)]
        public static mPerson TestNull()
        {
            return null;
        }

        [ExposedMethodAttribute(false)]
        public static bool IsGuid(Guid id)
        {
            return true;
        }

        [ExposedMethodAttribute(false)]
        public static bool AreGuids(ISessionManager session, Guid[] guids)
        {
            return true;
        }

        [ModelListMethodAttribute(false)]
        public static List<mPerson> ByGuid(Guid id)
        {
            return _persons;
        }

        [ExposedMethodAttribute(isSlow: true, arrayElementType: typeof(int))]
        public static void SlowStatic(AddItem addCall)
        {
            int idx = 0;
            while (idx<10)
            {
                System.Threading.Thread.Sleep(1000);
                addCall(idx, false);
                idx++;
            }
            addCall(idx, true);
        }

        [ExposedMethodAttribute(allowNullResponse: false, isSlow: true)]
        public static string GetSlowTimespan()
        {
            DateTime now = DateTime.Now;
            System.Threading.Thread.Sleep(3456);
            return string.Format("This call took {0} ms to complete", DateTime.Now.Subtract(now).TotalMilliseconds);
        }

        [ExposedMethodAttribute(allowNullResponse: false, arrayElementType: typeof(string))]
        public void GenerateNames(AddItem addCall)
        {
            for (int x = 0; x<3; x++)
            {
                switch (x)
                {
                    case 0:
                        addCall(_firstName, false);
                        break;
                    case 1:
                        addCall(_lastName, false);
                        break;
                    case 2:
                        addCall(string.Format("{0}, {1}", new Object[] { _lastName, _firstName }), false);
                        break;
                }
                System.Threading.Thread.Sleep(1000);
            }
            addCall(string.Format("{1} {0}", new Object[] { _lastName, _firstName }), true);
        }

        [ExposedMethodAttribute()]
        public static string ReadFile(IFormFile contentFile)
        {
            var reader = new StreamReader(contentFile.OpenReadStream());
            var result = $"{contentFile.Name} = {reader.ReadToEnd()}";
            return result;
        }
    }
}