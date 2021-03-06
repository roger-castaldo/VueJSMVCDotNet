using System;
using Org.Reddragonit.VueJSMVCDotNet.Interfaces;
using Org.Reddragonit.VueJSMVCDotNet.Attributes;
using System.Collections.Generic;

namespace TestApplication{
    [ModelRoute("/models/mPerson")]
    [ModelJSFilePath("/resources/scripts/mPerson.js")]
    public class mPerson : IModel
    {
        private static Random _rnd = new Random((int)DateTime.Now.Ticks);

        private string _firstName;
        [ModelRequiredField()]
        public string FirstName{get{return _firstName;}set{_firstName=value;}}

        private string _lastName;
        [ModelRequiredField()]
        public string LastName{get{return _lastName;}set{_lastName=value;}}

        private mPerson(string firstName,string lastName){
            _firstName=firstName;
            _lastName=lastName;
            _id = Math.Abs(_rnd.Next());
        } 

        public mPerson(){}
        private int _id=0;
        public string id {get{return _id.ToString();}}

        private static List<mPerson> _persons = new List<mPerson>(new mPerson[]{
            new mPerson("Bob","Loblaw"),
            new mPerson("Fred","Flinston"),
            new mPerson("Barney","Rumble")
        });

        [ModelLoadMethod()]
        public static mPerson Load(string id,ISecureSession session){
            System.Diagnostics.Debug.WriteLine(((SessionManager)session).Start);
            mPerson ret=null;
            foreach (mPerson per in _persons){
                if (id==per.id){
                    ret=per;
                    break;
                }
            }
            return ret;
        }

        [ModelLoadAllMethod()]
        public static List<mPerson> LoadAll(ISecureSession session){
            System.Diagnostics.Debug.WriteLine(((SessionManager)session).Start);
            return _persons;
        }

        [ModelDeleteMethod()]
        public bool Delete(ISecureSession session){
            System.Diagnostics.Debug.WriteLine(((SessionManager)session).Start);
            bool ret=false;
            for(int x=0;x<_persons.Count;x++){
                if (_persons[x].id==this.id){
                    _persons.RemoveAt(x);
                    ret=true;
                    break;
                }
            }
            return ret;
        }

        [ModelUpdateMethod()]
        public bool Update(ISecureSession session){
            System.Diagnostics.Debug.WriteLine(((SessionManager)session).Start);
            bool ret=false;
            for(int x=0;x<_persons.Count;x++){
                if (_persons[x].id==this.id){
                    _persons.RemoveAt(x);
                    _persons.Insert(x,this);
                    ret=true;
                    break;
                }
            }
            return ret;
        }

        [ModelSaveMethod()]
        public bool Save(ISecureSession session){
            System.Diagnostics.Debug.WriteLine(((SessionManager)session).Start);
            _persons.Add(this);
            return true;
        }

        [ModelListMethod("/search/mPerson?q={0}",true)]
        public static List<mPerson> Search(string q,ISecureSession session,int pageStartIndex, int pageSize, out int totalPages)
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

        [ExposedMethod(false)]
        public string GetFullName(ISecureSession session){
            System.Diagnostics.Debug.WriteLine(((SessionManager)session).Start);
            return string.Format("{0}, {1}",new object[]{LastName,FirstName});
        }

        [ExposedMethod(true)]
        public static mPerson TestNull(){
            return null;
        }

        [ExposedMethod(false)]
        public static bool IsGuid(Guid id){
            return true;
        }

        [ModelListMethod("/search/person/byguid?id={0}",false)]
        public static List<mPerson> ByGuid(Guid id){
            return _persons;
        }


    }
}