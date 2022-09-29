﻿using AutomatedTesting.Security;
using Org.Reddragonit.VueJSMVCDotNet;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AutomatedTesting.Models
{
    [ModelRoute("/models/mPerson")]
    [ModelJSFilePath("/resources/scripts/mPerson.js", modelNamespace: "App.Models")]
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
        private DateTime _birthday;
        public DateTime BirthDay
        {
            get { return _birthday; }
            set { _birthday = value; }
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
        [SecurityRoleCheck(Constants.Rights.STATIC_METHOD)]
        public static string FormatName(ISecureSession session,string lastName,string firstName)
        {
            return string.Format("{0}, {1}", new object[] { firstName, lastName });
        }

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
    }
}