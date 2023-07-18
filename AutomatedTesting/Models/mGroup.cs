using AutomatedTesting.Security;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutomatedTesting.Models
{
    [ModelRoute("/models/mGroup")]
    [SecurityRoleCheck(Constants.Rights.CAN_ACCESS)]
#pragma warning disable IDE1006 // Naming Styles
    public class mGroup : IModel
#pragma warning restore IDE1006 // Naming Styles
    {
        private static readonly Random _rnd = new((int)DateTime.Now.Ticks);

        [ModelRequiredField()]
        [ReadOnlyModelProperty()]
        public string Name { get; set; }

        [ModelRequiredField()]
        public mPerson[] People { get; set; }

        public List<mPerson> PeopleList
        {
            get
            {
                return (People==null ? null : new List<mPerson>(People));
            }
            set
            {
                if (value==null)
                    People=null;
                else
                    People=value.ToArray();
            }
        }

        public mPerson PrimaryPerson { 
            get { return (People?.FirstOrDefault()); }
            set
            {
                if (People==null)
                    People = new mPerson[] { value };
                else
                    People[0]=value;
            }
        }

        private readonly int _id = 0;
        public string id => _id.ToString();

        public mGroup() { }

        private mGroup(string name, mPerson[] people)
        {
            Name = name;
            People = people;
            _id = _rnd.Next();
        }

        private static readonly List<mGroup> _groups = new();

        public static mGroup[] Groups => _groups.ToArray();

        static mGroup()
        {
            _groups.AddRange(new mGroup[]
            {
                new mGroup("Flinstones",new mPerson[]{mPerson.Persons[0],mPerson.Persons[1]}),
                new mGroup("Anonymous",new mPerson[] {mPerson.Persons[0]})
            });
        }

        [ModelLoadMethod]
        public static mGroup Load(string id)
        {
            return _groups.FirstOrDefault(g => g.id==id);
        }

        [ExposedMethod(allowNullResponse:true)]
        public List<mPerson> Search(string name)
        {
            name=name.ToLower();
            return People.Where(p => p.FirstName.ToLower().Contains(name) || p.LastName.ToLower().Contains(name)).ToList();
        }

        [ExposedMethod]
        public bool ContainsPerson(mPerson person)
        {
            return People.Any(p => p.id==person.id);
        }

        [ExposedMethod]
        public bool ContainsPeople(List<mPerson> persons)
        {
            return persons.Count(p => ContainsPerson(p))==persons.Count;
        }

        [ExposedMethod(allowNullResponse:true)]
        public mPerson? FindFirst(string name)
        {
            name=name.ToLower();
            return People.FirstOrDefault(p => p.FirstName.ToLower().Contains(name) || p.LastName.ToLower().Contains(name));
        }
    }
}
