using System;
using System.Collections.Generic;
using System.Linq;
using VueJSMVCDotNet.Attributes.Models;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Models
{

    public class mLocation : IModel
    {
        private static Random _rnd = new Random((int)DateTime.UtcNow.Ticks);

        [ModelRequiredFieldAttribute()]
        [ReadOnlyModelPropertyAttribute()]
        public string Name { get; set; }

        [ModelRequiredFieldAttribute()]
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

        internal mLocation(string name, mGroup[] groups)
        {
            Name = name;
            Groups=groups;
            _id = _rnd.Next();
        }
    }
}
