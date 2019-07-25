using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Org.Reddragonit.VueJSMVCDotNet
{
    internal static class Constants
    {
        public static readonly DateTime UTC = new DateTime(1970, 1, 1, 00, 00, 00, DateTimeKind.Utc);
        public const string PARSERS_VARIABLE = "parsers";
        public const string INITIAL_DATA_KEY = "_initialData";
        public const string TO_JSON_VARIABLE = "ModelToJSON";
        public const string STATICS_VARAIBLE = "staticCalls";
        public static readonly BindingFlags STORE_DATA_METHOD_FLAGS = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        public static readonly BindingFlags LOAD_METHOD_FLAGS = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;
        public static class Events
        {
            public const string MODEL_LOADED = "loaded";
            public const string MODEL_DESTROYED = "destroyed";
            public const string MODEL_UPDATED = "updated";
            public const string MODEL_SAVED = "saved";
        }
    }
}
