namespace VueJSMVCDotNet
{
    internal static class Constants
    {
        public const string INITIAL_DATA_KEY = "#initialData";
        public const string TO_JSON_VARIABLE = "#toJSON";
        public const string PARSE_FUNCTION_NAME = "_parse";
        public static readonly BindingFlags STORE_DATA_METHOD_FLAGS = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        public static readonly BindingFlags LOAD_METHOD_FLAGS = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;
        public static readonly BindingFlags INSTANCE_METHOD_FLAGS = BindingFlags.Public|BindingFlags.Instance;
        public static readonly BindingFlags STATIC_INSTANCE_METHOD_FLAGS = BindingFlags.Public | BindingFlags.Static;
        public const string VUE_IMPORT_NAME = "vue";
        public static class Events
        {
            public const string MODEL_LOADED = "loaded";
            public const string MODEL_DESTROYED = "destroyed";
            public const string MODEL_UPDATED = "updated";
            public const string MODEL_SAVED = "saved";
            public const string MODEL_PARSED = "parsed";
            public const string LIST_MODEL_LOADED = "model_loaded";
            public const string LIST_MODEL_DESTROYED = "model_destroyed";
            public const string LIST_MODEL_UPDATED = "model_updated";
            public const string LIST_LOADED = "loaded";
        }
    }
}
