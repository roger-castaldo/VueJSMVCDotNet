using System;
using System.Collections.Generic;
using TestApplication.Security;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;

namespace TestApplication
{
    [ModelRouteAttribute("/models/User")]
    [ModelJSFilePath("/resources/scripts/models/User.js")]
    [IsLoggedIn()]
    public class User : IModel
    {        
        public string FirstName { get; set; }
        public string LastName { get; set; }

        [ReadOnlyModelPropertyAttribute()]
        public string Test { get; set; }

        public DateTime CreateDate
        {
            get { return new DateTime((long)new Random().Next() * (long)100000); }
        }

        public DateTime? LastLoginDate { get; set; }

        public int Seed =>new Random().Next(0, 100); 

        internal User(string firstName, string lastName)
        {
            FirstName = firstName;
            LastName = lastName;
            byte[] buff = new byte[16];
            new Random((int)DateTime.Now.Ticks).NextBytes(buff);
            _id = new Guid(buff).ToString();
        }

        public User() { }

        private string _id;

        public void SetID(string id)
        {
            _id = id;
        }
        public string id { get { return _id; } }
    }
}
