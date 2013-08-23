using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace BakaBox
{
    /// <summary>
    /// Handles parsing string values to appropriate types
    /// </summary>
    [DebuggerStepThrough]
    public static class Parse
    {
        #region Static Constructor
        //prepares the Parse class
        static Parse()
        { Parse._CustomParsing = new Dictionary<Type, Func<string, object>>(); }
        #endregion

        #region Properties
        //holds custom parse methods
        private static Dictionary<Type, Func<string, object>> _CustomParsing;
        #endregion

        #region Public Methods
        /// <summary>
        /// Registers a custom parsing method
        /// </summary>
        public static void RegisterType<T>(Func<string, object> compare)
        {
            Type type = typeof(T);
            Parse._CustomParsing.Remove(type);
            Parse._CustomParsing.Add(type, compare);
        }

        /// <summary>
        /// Attempts to parse a value and return the result
        /// but falls back to the default value if the conversion fails
        /// </summary>
        public static T TryParse<T>(string value)
        {
            return Parse.TryParse(value, default(T));
        }

        /// <summary>
        /// Attempts to parse a value and falls back to the
        /// provided default value if the conversion fails
        /// </summary>
        public static T TryParse<T>(string value, T @default)
        {
            value = (value ?? string.Empty).ToString();
            Type type = typeof(T);

            //so much can go wrong here, just default to the
            //fall back type if the conversions go bad
            try
            {

                //perform custom parsing first
                if (Parse._PerformCustomParse(type, value, ref @default)) return @default;

                //check if this is a nullable and if we should use a child type
                //this might not work with VB since the nullable name could be different
                if (type.IsGenericType && type.Name.StartsWith("Nullable`"))
                {

                    //underlying type for a nullable appears to be the first argument
                    type = type.GetGenericArguments().FirstOrDefault();

                    //if no type was found then five up
                    if (type == null) { return @default; }

                    //try custom parsing with the underlying type if this was a nullable
                    if (Parse._PerformCustomParse(type, value, ref @default)) return @default;
                }

                //try the remaining parsing methods
                if (type.IsEnum && Parse._PerformEnumParse(type, value, ref @default))
                    return @default;
                if (Parse._PerformParse(type, value, ref @default)) return @default;

                //finally, just try a conversion
                Parse._PerformConvert(type, value, ref @default);
                return @default;

            }
            //settle for the default
            catch
            {
                return @default;
            }

        }
        #endregion

        #region Checking Values
        //uses custom parsing methods
        private static bool _PerformCustomParse<T>(Type with, string value, ref T result)
        {

            //if there is no custom type, cancel
            if (!Parse._CustomParsing.ContainsKey(with)) { return false; }

            //find the conversion
            Func<string, object> parse = Parse._CustomParsing[with];

            //attempt to parse
            try
            {
                object converted = parse(value);
                bool success = converted is T;
                if (success) { result = (T)converted; }
                return success;
            }
            //if the attempt failed
            catch
            {
                return false;
            }

        }

        //tries to parse using an Enum
        private static bool _PerformEnumParse<T>(Type with, string value, ref T result)
        {

            //check for a result
            try
            {
                object parsed = Enum.Parse(with, value, true);
                bool success = parsed is T;
                if (success) { result = (T)parsed; }
                return success;
            }
            catch
            {
                return false;
            }

        }

        //searches for a 'Parse' method
        private static bool _PerformParse<T>(Type with, string value, ref T result)
        {

            //make sure a try parse was even found
            MethodInfo method = with.GetMethods().FirstOrDefault(item =>
              item.Name.Equals("parse", StringComparison.OrdinalIgnoreCase) &&
              item.IsStatic);
            if (method == null) { return false; }

            //check for a result
            try
            {
                object parsed = method.Invoke(null, new object[] { value, result });
                bool success = parsed is T;
                if (success) { result = (T)parsed; }
                return success;
            }
            catch
            {
                return false;
            }

        }

        //performs common conversions
        private static bool _PerformConvert<T>(Type type, string value, ref T result)
        {
            object convert = Convert.ChangeType(value, type);
            bool success = convert is T;

            //update the type if needed
            if (success) { result = (T)convert; }
            return success;
        }
        #endregion
    }

    /// <summary>
    /// BakaBox to quickly parse string types
    /// </summary>
    [DebuggerStepThrough]
    public static class StringExtensions
    {
        /// <summary>
        /// Parses a string to the correct type or default value
        /// </summary>
        public static T ToType<T>(this string value)
        {
            return value.ToType(default(T));
        }

        /// <summary>
        /// Parses a string to the correct type or default value
        /// </summary>
        public static T ToType<T>(this string value, T @default)
        {
            return Parse.TryParse(value, @default);
        }
    }
}
