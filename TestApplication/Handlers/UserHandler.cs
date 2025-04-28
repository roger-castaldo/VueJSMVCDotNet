using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestApplication.Security;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Endpoints.Model;
using VueJSMVCDotNet.Interfaces;

namespace TestApplication.Handlers
{
    [ModelRouteAttribute("/models/User")]
    [IsLoggedIn()]
    public class UserHandler : IModelHandler<User>
    {
        private static User[] _USERS = new User[]{
            new User("Roger","Castaldo"),
            new User("Terry","Lammers"),
            new User("David","McQueen")
        };
        ValueTask<User> IModelHandler<User>.LoadAsync(string id)
            => ValueTask.FromResult(Array.Find(_USERS,u => Equals(u.id, id)));

        [ModelLoadAllMethodAttribute()]
        public IEnumerable<User> LoadAll()
            => _USERS;

        [ModelSaveMethodAttribute()]
        public string Save([ModelInstanceParameter()] User user)
        {
            byte[] buff = new byte[16];
            new Random((int)DateTime.Now.Ticks).NextBytes(buff);
            user.SetID(new Guid(buff).ToString());
            lock (_USERS)
            {
                User[] tmp = new User[_USERS.Length + 1];
                Array.Copy(_USERS, tmp, _USERS.Length);
                tmp[_USERS.Length] = user;
                _USERS = tmp;
            }
            return user.id;
        }

        [ModelUpdateMethodAttribute()]
        public bool Update([ModelInstanceParameter()] User user)
        {
            bool ret = false;
            for (int x = 0; x < _USERS.Length; x++)
            {
                if (Equals(_USERS[x].id,user.id))
                {
                    _USERS[x] = user;
                    ret = true;
                    break;
                }
            }
            return ret;
        }

        [ModelDeleteMethodAttribute()]
        public bool Delete([ModelIDParameter()] string id)
        {
            bool ret = false;
            lock (_USERS)
            {
                var idx = -1;
                for (int x = 0; x < _USERS.Length; x++)
                {
                    if (Equals(id, _USERS[x].id))
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
                    for (int x = 0; x < _USERS.Length; x++)
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

        [ExposedMethodAttribute()]
        public void Logout([ModelInstanceParameter()]User user)
        {
            System.Diagnostics.Debug.WriteLine($"Logging out User {user.LastName}, {user.FirstName}...");
        }

        [ExposedMethodAttribute()]
        public bool CanAccess([ModelInstanceParameter()]User user, string path)
        {
            return new Random().Next(0, 10)>=5;
        }

        [ExposedMethodAttribute(allowNullResponse: true)]
        public User Login(string username, string password)
        {
            User ret = null;
            lock (_USERS)
            {
                foreach (User u in _USERS)
                {
                    if (string.Format("{0}_{1}", new object[] { u.FirstName, u.LastName }).ToLower() == username.ToLower())
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

        [ModelListMethodAttribute(true)]
        public PagedResult<User> Search(string filter, [PageStartIndexParameter()] int pageStartIndex,[PageSizeParameter()] int pageSize)
        {
            var totalPages = 0;
            List<User> tmp = new List<User>();
            lock (_USERS)
            {
                if (filter == null)
                {
                    tmp.AddRange(_USERS);
                }
                else
                {
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
                for (int x = pageStartIndex; x < tmp.Count; x++)
                {
                    ret.Add(tmp[x]);
                    if (ret.Count >= pageSize)
                        break;
                }
            }
            return new(ret,totalPages);
        }

        [ModelListMethodAttribute(false)]
        public List<User> SearchAll(string filter)
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
