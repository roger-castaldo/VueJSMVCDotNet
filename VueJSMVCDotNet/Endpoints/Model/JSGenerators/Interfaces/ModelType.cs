using VueJSMVCDotNet.Attributes.ModelHandlers;
using VueJSMVCDotNet.Attributes.Models;
using VueJSMVCDotNet.Interfaces;

namespace VueJSMVCDotNet.Endpoints.Model.JSGenerators.Interfaces
{
    internal record ModelType
    {
        public static IEnumerable<PropertyInfo> ExtractProperties(Type modelType)
            => modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(pi => pi.GetCustomAttribute<ModelIgnorePropertyAttribute>(false)==null
                        && !Equals(pi.Name, "id")
                        && !(pi.PropertyType.FullName?.Contains("+KeyCollection")??false)
                        && (pi.GetGetMethod()?.GetParameters()?? []).Length == 0);

        public ModelType(Type type, Type handlerType, Func<Type, string?> mapImport)
        {
            Type=type;
            HandlerType=handlerType;
            Properties = ExtractProperties(Type);
            InstanceMethods = HandlerType.GetMethods(Constants.METHOD_FLAGS)
                .Where(m => m.GetCustomAttribute<ExposedMethodAttribute>(false)!=null && Helper.IsExposedMethodInstance(m));
            StaticMethods = HandlerType.GetMethods(Constants.METHOD_FLAGS)
                .Where(m => m.GetCustomAttribute<ExposedMethodAttribute>(false)!=null && !Helper.IsExposedMethodInstance(m));
            SaveMethod = Array.Find(
                HandlerType.GetMethods(Constants.METHOD_FLAGS),
                mi => mi.GetCustomAttribute<ModelSaveMethodAttribute>(false)!=null
            );
            UpdateMethod = Array.Find(
                HandlerType.GetMethods(Constants.METHOD_FLAGS),
                mi => mi.GetCustomAttribute<ModelUpdateMethodAttribute>(false)!=null
            );
            DeleteMethod = Array.Find(
                HandlerType.GetMethods(Constants.METHOD_FLAGS),
                mi => mi.GetCustomAttribute<ModelDeleteMethodAttribute>(false)!=null
            );
            LinkedTypes = Properties.Where(pi => pi.CanRead)
                            .Select(pi => Utility.ExtractUnderlyingType(pi.PropertyType).type)
                            .Where(t => t.GetInterfaces().Contains(typeof(IModel)))
                            .Concat(
                                InstanceMethods.Concat(StaticMethods)
                                .Select(mi => Utility.ExtractUnderlyingType(mi.ReturnType).type)
                                .Where(t => t.GetInterfaces().Contains(typeof(IModel)))
                            )
                            .Concat(
                                InstanceMethods.Concat(StaticMethods)
                                .Select(mi => ((ExposedMethodAttribute)mi.GetCustomAttributes(typeof(ExposedMethodAttribute), false)[0]).ArrayElementType)
                                .Where(t => t!=null && t.GetInterfaces().Contains(typeof(IModel)))
                            )
                            .Where(t => !Equals(t, type))
                            .Distinct()
                            .Select(t => Tuple.Create<Type, string>(t!, mapImport(t!)??string.Empty))
                            .Where(t => !string.IsNullOrWhiteSpace(t.Item2));
        }
        public Type Type { get; private init; }
        public Type HandlerType { get; private init; }
        public IEnumerable<PropertyInfo> Properties { get; private init; }
        public IEnumerable<MethodInfo> InstanceMethods { get; private init; }
        public IEnumerable<MethodInfo> StaticMethods { get; private init; }
        public MethodInfo? SaveMethod { get; private init; }
        public MethodInfo? UpdateMethod { get; private init; }
        public MethodInfo? DeleteMethod { get; private init; }
        public IEnumerable<Tuple<Type, string>> LinkedTypes { get; private init; }

    }
}
