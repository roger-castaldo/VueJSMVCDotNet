using AutomatedTesting.Models;
using AutomatedTesting.Security;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VueJSMVCDotNet.Attributes.ModelHandlers;
using VueJSMVCDotNet.Interfaces;

namespace AutomatedTesting.Handlers
{
    [ModelRouteAttribute(Constants.GroupModelRoute)]
    [SecurityRoleCheck(Constants.Rights.CAN_ACCESS)]
    public class mGroupHandler : IModelHandler<mGroup>
    {
        private static readonly List<mGroup> _groups = new();

        public static mGroup[] Groups => _groups.ToArray();

        static mGroupHandler()
        {
            _groups.AddRange(new mGroup[]
            {
                new mGroup("Flinstones",new mPerson[]{mPersonHandler.Persons[0],mPersonHandler.Persons[1]}),
                new mGroup("Anonymous",new mPerson[] {mPersonHandler.Persons[0]})
            });
        }
        ValueTask<mGroup> IModelHandler<mGroup>.LoadAsync(string id)
            => ValueTask.FromResult(_groups.FirstOrDefault(g => g.id==id));

        [ExposedMethodAttribute(allowNullResponse: true)]
        public List<mPerson> Search([ModelInstanceParameterAttribute] mGroup group, string name)
            => group.Search(name);

        [ExposedMethodAttribute]
        public bool ContainsPerson([ModelInstanceParameterAttribute] mGroup group, mPerson person)
            => group.ContainsPerson(person);

        [ExposedMethodAttribute]
        public bool ContainsPeople([ModelInstanceParameterAttribute] mGroup group, List<mPerson> persons)
            => group.ContainsPeople(persons);

        [ExposedMethodAttribute(allowNullResponse: true)]
        public mPerson FindFirst([ModelInstanceParameterAttribute] mGroup group, string name)
            => group.FindFirst(name);
    }
}
