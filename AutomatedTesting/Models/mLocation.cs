using AutomatedTesting.Security;
using VueJSMVCDotNet.Attributes;
using VueJSMVCDotNet.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AutomatedTesting.Models
{
    [ModelRoute("/models/mLocation")]
    [ModelJSFilePath("/resources/scripts/mLocation.js")]
    [SecurityRoleCheck(Constants.Rights.CAN_ACCESS)]
    public class mLocation : IModel
    {
        private static Random _rnd = new Random((int)DateTime.Now.Ticks);

        [ModelRequiredField()]
        [ReadOnlyModelProperty()]
        public string Name { get; set; }

        [ModelRequiredField()]
        public mGroup[] Groups { get; set; }

        public List<mGroup> GroupList
        {
            get
            {
                return Groups==null ? null : new List<mGroup>(Groups);
            }
            set
            {
                if (value==null)
                    Groups=null;
                else
                    Groups=value.ToArray();
            }
        }

        public mGroup PrimaryGrooup
        {
            get { return (Groups==null ? null : Groups.FirstOrDefault()); }
            set
            {
                if (Groups==null)
                    Groups = new mGroup[] { value };
                else
                    Groups[0]=value;
            }
        }

        private int _id = 0;
        public string id => _id.ToString();

        public mLocation() { }

        private mLocation(string name, mGroup[] groups)
        {
            Name = name;
            Groups=groups;
            _id = _rnd.Next();
        }

        private static List<mLocation> _locations = new List<mLocation>();

        public static mLocation[] Locations => _locations.ToArray();

        static mLocation()
        {
            _locations.AddRange(new mLocation[]
            {
                new mLocation("Flinstones",new mGroup[]{mGroup.Groups[0]}),
                new mLocation("Anonymous",new mGroup[] {mGroup.Groups[1]})
            });
        }

        [ModelLoadMethod]
        public static mLocation Load(string id)
        {
            return _locations.FirstOrDefault(g => g.id==id);
        }

        [ExposedMethod(allowNullResponse: true)]
        public List<mPerson> Search(string name)
        {
            name=name.ToLower();
            List<mPerson> results = new List<mPerson>();
            foreach (mGroup g in Groups)
                results.AddRange(g.Search(name));
            return results;
        }

        [ExposedMethod]
        public bool ContainsPerson(mPerson person)
        {
            return Groups.Any(g => g.ContainsPerson(person));
        }

        [ExposedMethod]
        public bool ContainsPeople(List<mPerson> persons)
        {
            return Groups.Any(g => g.ContainsPeople(persons));
        }

        [ExposedMethod(allowNullResponse: true)]
        public mPerson? FindFirst(string name)
        {
            name=name.ToLower();
            mPerson result = null;
            foreach (mGroup g in Groups)
            {
                result=g.FindFirst(name);
                if (result!=null)
                    break;
            }
            return result;
        }
    }
}
