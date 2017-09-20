using System;
using System.Reflection;
using UnityEditor;

namespace WoLfulus.LineEnding
{
    public static class LogEntry
    {
        public static object instance = null;

        public static string condition
        {
            get
            {
                return (string)_condition.GetValue(instance);
            }
        }

        public static int errorNum
        {
            get
            {
                return (int)_errorNum.GetValue(instance);
            }
        }

        public static string file
        {
            get
            {
                return (string)_file.GetValue(instance);
            }
        }

        public static int line
        {
            get
            {
                return (int)_line.GetValue(instance);
            }
        }

        public static Mode mode
        {
            get
            {
                return (Mode)((int)_mode.GetValue(instance));
            }
        }

        public static int instanceID
        {
            get
            {
                return (int)_instanceID.GetValue(instance);
            }
        }

        public static int identifier
        {
            get
            {
                return (int)_identifier.GetValue(instance);
            }
        }

        public static int isWorldPlaying
        {
            get
            {
                return (int)_isWorldPlaying.GetValue(instance);
            }
        }

        private static Type _type = null;

        private static FieldInfo _condition;
        private static FieldInfo _errorNum;
        private static FieldInfo _file;
        private static FieldInfo _line;
        private static FieldInfo _mode;
        private static FieldInfo _instanceID;
        private static FieldInfo _identifier;
        private static FieldInfo _isWorldPlaying;

        static LogEntry()
        {
            Initialize();
        }

        static void Initialize()
        {
            if (_type == null)
            {
                var flags = BindingFlags.Instance | BindingFlags.Public;

                var assembly = Assembly.GetAssembly(typeof(Editor));
                _type = assembly.GetType("UnityEditorInternal.LogEntry");
                if (_type == null) // 2017 Fix
                {
                    _type = assembly.GetType("UnityEditor.LogEntry");
                }

                _condition = _type.GetField("condition", flags);
                _errorNum = _type.GetField("errorNum", flags);
                _file = _type.GetField("file", flags);
                _line = _type.GetField("line", flags);
                _mode = _type.GetField("mode", flags);
                _instanceID = _type.GetField("instanceID", flags);
                _identifier = _type.GetField("identifier", flags);
                _isWorldPlaying = _type.GetField("isWorldPlaying", flags);

                instance = Activator.CreateInstance(_type);
            }
        }
    }
}
