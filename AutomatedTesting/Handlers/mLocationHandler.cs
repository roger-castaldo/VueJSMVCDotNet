using AutomatedTesting.Models;
using AutomatedTesting.Security;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VueJSMVCDotNet.Attributes.ModelHandlers;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Handlers
{
    [ModelRouteAttribute(Constants.LocationModelRoute)]
    [SecurityRoleCheck(Constants.Rights.CAN_ACCESS)]
    public class mLocationHandler : IModelHandler<mLocation>
    {
        private static List<mLocation> _locations = new List<mLocation>();

        public static mLocation[] Locations => _locations.ToArray();

        static mLocationHandler()
        {
            _locations.AddRange(new mLocation[]
            {
                new mLocation("Flinstones",new mGroup[]{mGroupHandler.Groups[0]}),
                new mLocation("Anonymous",new mGroup[] { mGroupHandler.Groups[1]})
            });
        }
        ValueTask<mLocation> IModelHandler<mLocation>.LoadAsync(string id)
            => ValueTask.FromResult(_locations.FirstOrDefault(g => g.id==id));

        [ExposedMethodAttribute(allowNullResponse: true)]
        public List<mPerson> Search([ModelInstanceParameter] mLocation location, string name)
        {
            name=name.ToLower();
            List<mPerson> results = new List<mPerson>();
            foreach (mGroup g in location.Groups)
                results.AddRange(g.Search(name));
            return results;
        }

        [ExposedMethodAttribute]
        public bool ContainsPerson([ModelInstanceParameter] mLocation location, mPerson person)
        {
            return location.Groups.Any(g => g.ContainsPerson(person));
        }

        [ExposedMethodAttribute]
        public bool ContainsPeople([ModelInstanceParameter] mLocation location, List<mPerson> persons)
        {
            return location.Groups.Any(g => g.ContainsPeople(persons));
        }

        [ExposedMethodAttribute(allowNullResponse: true)]
        public mPerson FindFirst([ModelInstanceParameter] mLocation location, string name)
        {
            name=name.ToLower();
            mPerson result = null;
            foreach (mGroup g in location.Groups)
            {
                result=g.FindFirst(name);
                if (result!=null)
                    break;
            }
            return result;
        }
    }
}
