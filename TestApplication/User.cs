using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using TestApplication.Security;

namespace TestApplication
{
    [ModelRoute("/models/User")]
    [ModelJSFilePath("/resources/scripts/models/User.js")]
    [IsLoggedIn()]
    public class User : IModel
    {
        private static User[] _USERS = new User[]{
            new User("Roger","Castaldo"),
            new User("Terry","Lammers"),
            new User("David","McQueen")
        };

        private string _firstName;
        public string FirstName { get { return _firstName; } set { _firstName = value; } }

        private string _lastName;
        public string LastName { get { return _lastName; }set{ _lastName = value; } }

        private string _test;
        [ReadOnlyModelProperty()]
        public string Test { get { return _test; } set { _test = value; } }

        public DateTime CreateDate
        {
            get { return new DateTime((long)new Random().Next() * (long)100000); }
        }

        private DateTime? _lastLogin;
        public DateTime? LastLoginDate
        {
            get { return _lastLogin; }
            set { _lastLogin=value; }
        }

        public int Seed
        {
            get { return new Random().Next(0, 100); }
        }

        private User(string firstName,string lastName)
        {
            _firstName = firstName;
            _lastName = lastName;
            byte[] buff = new byte[16];
            new Random((int)DateTime.Now.Ticks).NextBytes(buff);
            _id = new Guid(buff).ToString();
        }

        public User() { }

        [ModelLoadMethod()]
        public static User Load(string id)
        {
            User ret = null;
            foreach (User u in _USERS)
            {
                if (u.id == id)
                {
                    ret = u;
                    break;
                }
            }
            return ret;
        }

        [ModelLoadAllMethod()]
        public static List<User> LoadAll()
        {
            return new List<User>(_USERS);
        }

        private string _id;
        public string id { get { return _id; } }

        [ModelSaveMethod()]
        public bool Save() {
            byte[] buff = new byte[16];
            new Random((int)DateTime.Now.Ticks).NextBytes(buff);
            _id = new Guid(buff).ToString();
            lock (_USERS) {
                User[] tmp = new User[_USERS.Length + 1];
                Array.Copy(_USERS, tmp, _USERS.Length);
                tmp[_USERS.Length] = this;
                _USERS = tmp;
            }
            return true;
        }

        [ModelUpdateMethod()]
        public bool Update() {
            bool ret = false;
            for (int x = 0; x < _USERS.Length; x++)
            {
                if (_USERS[x].id == this.id)
                {
                    _USERS[x] = this;
                    ret = true;
                    break;
                }
            }
            return ret;
        }

        [ModelDeleteMethod()]
        public bool Delete() {
            bool ret = false;
            lock (_USERS)
            {
                var idx = -1;
                for (int x = 0; x < _USERS.Length; x++) {
                    if (_USERS[x].id == this.id)
                    {
                        idx = x;
                        break;
                    }
                }
                if (idx != -1)
                {
                    ret = true;
                    User[] tmp = new User[_USERS.Length - 1];
                    var index = 0;
                    for(int x = 0; x < _USERS.Length; x++)
                    {
                        if (x != idx)
                        {
                            tmp[index] = _USERS[x];
                            index++;
                        }
                    }
                    _USERS = tmp;
                }
            }
            return ret;
        }

        [ExposedMethod()]
        public void Logout() {
            System.Diagnostics.Debug.WriteLine("Logging out User {0}, {1}...",new object[]{
                LastName,
                FirstName
            });
        }

        [ExposedMethod()]
        public bool CanAccess(string path)
        {
            return new Random().Next(0, 10)>=5;
        }

        [ExposedMethod(allowNullResponse:true)]
        public static User Login(string username,string password) {
            User ret = null;
            lock (_USERS)
            {
                foreach (User u in _USERS)
                {
                    if (string.Format("{0}_{1}",new object[] { u.FirstName, u.LastName }).ToLower() == username.ToLower())
                    {
                        if (password.ToLower() == u.LastName.ToLower())
                        {
                            ret = u;
                            break;
                        }
                    }
                }
            }
            return ret;
        }

        [ModelListMethod(true)]
        public static List<User> Search(string filter, int pageStartIndex, int pageSize, out int totalPages)
        {
            totalPages = 0;
            List<User> tmp = new List<User>();
            lock (_USERS)
            {
                if (filter == null)
                {
                    tmp.AddRange(_USERS);
                }
                else {
                    foreach (User u in _USERS)
                    {
                        if (u.FirstName.ToLower().Contains(filter.ToLower()) || u.LastName.ToLower().Contains(filter.ToLower()))
                            tmp.Add(u);
                    }
                }
                totalPages = (int)Math.Floor((decimal)tmp.Count / (decimal)pageSize)+1;
            }
            List<User> ret = new List<User>();
            if (tmp.Count > pageStartIndex)
            {
                for (int x = pageStartIndex; x < tmp.Count; x++) {
                    ret.Add(tmp[x]);
                    if (ret.Count >= pageSize)
                        break;
                }
            }
            return (ret.Count == 0 ? null : ret);
        }

        [ModelListMethod(false)]
        public static List<User> SearchAll(string filter)
        {
            List<User> ret = new List<User>();
            lock (_USERS)
            {
                if (filter == null)
                {
                    ret.AddRange(_USERS);
                }
                else
                {
                    foreach (User u in _USERS)
                    {
                        if (u.FirstName.ToLower().Contains(filter.ToLower()) || u.LastName.ToLower().Contains(filter.ToLower()))
                            ret.Add(u);
                    }
                }
            }
            return ret;
        }
    }
}
