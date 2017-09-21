using System;
using System.Reflection;
using UnityEditor;

namespace WoLfulus.LineEnding
{
    [InitializeOnLoad]
    public static class LogEntries
    {
        private static Type _type;

        private static PropertyInfo _consoleFlags;
        private static MethodInfo _SetConsoleFlag;
        private static MethodInfo _RowGotDoubleClicked;
        private static MethodInfo _GetStatusText;
        private static MethodInfo _GetStatusMask;
        private static MethodInfo _StartGettingEntries;
        private static MethodInfo _EndGettingEntries;
        private static MethodInfo _GetFirstTwoLinesEntryTextAndModeInternal;
        private static MethodInfo _GetEntryCount;
        private static MethodInfo _GetEntryInternal;
        private static MethodInfo _GetCount;
        private static MethodInfo _GetCountsByType;
        private static MethodInfo _GetStatusViewErrorIndex;
        private static MethodInfo _ClickStatusBar;
        private static MethodInfo _Clear;


        static LogEntries()
        {
            var flags = BindingFlags.Static | BindingFlags.Public;

            if (_type == null)
            {
                Assembly assembly = Assembly.GetAssembly(typeof(Editor));
                _type = assembly.GetType("UnityEditorInternal.LogEntries");
                if (_type == null) // 2017 Fix
                {
                    _type = assembly.GetType("UnityEditor.LogEntries");
                }

                _consoleFlags = _type.GetProperty("consoleFlags", flags);
                _SetConsoleFlag = _type.GetMethod("SetConsoleFlag", flags);

                _RowGotDoubleClicked = _type.GetMethod("RowGotDoubleClicked", flags);

                _GetStatusText = _type.GetMethod("GetStatusText", flags);
                _GetStatusMask = _type.GetMethod("GetStatusMask", flags);

                _StartGettingEntries = _type.GetMethod("StartGettingEntries", flags);
                _EndGettingEntries = _type.GetMethod("EndGettingEntries", flags);

                _GetFirstTwoLinesEntryTextAndModeInternal = _type.GetMethod("GetFirstTwoLinesEntryTextAndModeInternal", flags);

                _GetEntryCount = _type.GetMethod("GetEntryCount", flags);
                _GetEntryInternal = _type.GetMethod("GetEntryInternal", flags);

                _GetCount = _type.GetMethod("GetCount", flags);
                _GetCountsByType = _type.GetMethod("GetCountsByType", flags);

                _GetStatusViewErrorIndex = _type.GetMethod("GetStatusViewErrorIndex", flags);
                _ClickStatusBar = _type.GetMethod("ClickStatusBar", flags);

                _Clear = _type.GetMethod("Clear", flags);
            }
        }

        public static int consoleFlags
        {
            get
            {
                return (int)_consoleFlags.GetValue(null, null);
            }
            set
            {
                _consoleFlags.SetValue(null, value, null);
            }
        }

        public static void RowGotDoubleClicked(int index)
        {
            _RowGotDoubleClicked.Invoke(null, new object[1] { index });
        }

        public static string GetStatusText()
        {
            return (string)_GetStatusText.Invoke(null, new object[0]);
        }

        public static int GetStatusMask()
        {
            return (int)_GetStatusMask.Invoke(null, new object[0]);
        }

        public static int StartGettingEntries()
        {
            return (int)_StartGettingEntries.Invoke(null, new object[0]);
        }

        public static void SetConsoleFlag(int bit, bool value)
        {
            _SetConsoleFlag.Invoke(null, new object[2] { bit, value });
        }

        public static void EndGettingEntries()
        {
            _EndGettingEntries.Invoke(null, new object[0]);
        }

        public static int GetCount()
        {
            return (int)_GetCount.Invoke(null, new object[0]);
        }

        public static void GetCountsByType(ref int errorCount, ref int warningCount, ref int logCount)
        {
            _GetCountsByType.Invoke(null, new object[3] { errorCount, warningCount, logCount });
        }

        public static void GetFirstTwoLinesEntryTextAndModeInternal(int row, ref int mask, ref string outString)
        {
            _GetFirstTwoLinesEntryTextAndModeInternal.Invoke(null, new object[3] { row, mask, outString });
        }

        public static bool GetEntryInternal(int row, object outputEntry)
        {
            return (bool)_GetEntryInternal.Invoke(null, new object[2] { row, outputEntry });
        }

        public static int GetEntryCount(int row)
        {
            return (int)_GetEntryCount.Invoke(null, new object[1] { row });
        }

        public static void Clear()
        {
            _Clear.Invoke(null, new object[0]);
        }

        public static int GetStatusViewErrorIndex()
        {
            return (int)_GetStatusViewErrorIndex.Invoke(null, new object[0]);
        }

        public static void ClickStatusBar(int count)
        {
            _ClickStatusBar.Invoke(null, new object[1] { count });
        }
    }
}
