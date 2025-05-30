using System;
using System.Collections;
using VueJSMVCDotNet.Attributes.Models;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Models
{
    public struct sName
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public sName(string firstname, string lastname)
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

    
    public class mPerson : IModel
    {
        private static Random _rnd = new Random((int)DateTime.UtcNow.Ticks);

        private string _firstName;
        [ModelRequiredFieldAttribute()]
        public string FirstName { get { return _firstName; } set { _firstName = value; } }

        private string _lastName;
        [ModelRequiredFieldAttribute()]
        public string LastName { get { return _lastName; } set { _lastName = value; } }
        private DateTime _birthday = DateTime.UtcNow.AddYears(-20);
        public DateTime BirthDay
        {
            get { return _birthday; }
            set { _birthday = value; }
        }

        public int Age
        {
            get
            {
                return (int)Math.Floor(DateTime.UtcNow.Subtract(_birthday).TotalDays/365);
            }
        }


        internal mPerson(int id, string firstName, string lastName)
        {
            _firstName = firstName;
            _lastName = lastName;
            _id = id;
        }

        public mPerson() { }

        private int _id = 0;
        public string id { get { return _id.ToString(); } set { _id=int.Parse(value); } }

        public string GetFullName()
            => string.Format("{0}, {1}", new object[] { LastName, FirstName });
        public string GetFullName(string middleName)
        => string.Format("{0}, {1} {2}", new object[] { LastName, FirstName, middleName});
    }
}
