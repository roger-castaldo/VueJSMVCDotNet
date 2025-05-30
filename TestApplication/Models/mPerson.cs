using System;
using VueJSMVCDotNet.Attributes.Models;
using VueJSMVCDotNet.Interfaces;

namespace TestApplication.Models
{
    public class mPerson : IModel
    {
        private static Random _rnd = new Random((int)DateTime.UtcNow.Ticks);

        private string _firstName;
        [ModelRequiredField()]
        public string FirstName { get { return _firstName; } set { _firstName=value; } }

        private string _lastName;
        [ModelRequiredField()]
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

        public mPerson() {
            _id = Math.Abs(_rnd.Next());
        }

        internal mPerson(string firstName, string lastName)
        {
            _firstName=firstName;
            _lastName=lastName;
            _id = Math.Abs(_rnd.Next());
        }

        private int _id = 0;
        public string id { get { return _id.ToString(); } }
    }
}