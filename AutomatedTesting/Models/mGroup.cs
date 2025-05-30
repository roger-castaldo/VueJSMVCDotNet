using System;
using System.Collections.Generic;
using System.Linq;
using VueJSMVCDotNet.Attributes.Models;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Models
{
    #pragma warning disable IDE1006 // Naming Styles
    public class mGroup : IModel
#pragma warning restore IDE1006 // Naming Styles
    {
        private static readonly Random _rnd = new((int)DateTime.UtcNow.Ticks);

        [ModelRequiredFieldAttribute()]
        [ReadOnlyModelPropertyAttribute()]
        public string Name { get; set; }

        [ModelRequiredFieldAttribute()]
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

        public mPerson PrimaryPerson
        {
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

        internal mGroup(string name, mPerson[] people)
        {
            Name = name;
            People = people;
            _id = _rnd.Next();
        }

        public List<mPerson> Search(string name)
            => People.Where(p => p.FirstName.Contains(name, StringComparison.InvariantCultureIgnoreCase)||p.LastName.Contains(name, StringComparison.InvariantCultureIgnoreCase)).ToList();

        public bool ContainsPerson(mPerson person)
            => Array.Exists(People, p => Equals(p.id, person.id));

        public mPerson FindFirst(string name)
            => Array.Find(People, p => p.FirstName.Contains(name, StringComparison.InvariantCultureIgnoreCase)||p.LastName.Contains(name, StringComparison.InvariantCultureIgnoreCase));

        public bool ContainsPeople(List<mPerson> persons)
            => persons.Count(p => ContainsPerson(p))==persons.Count;
    }
}
