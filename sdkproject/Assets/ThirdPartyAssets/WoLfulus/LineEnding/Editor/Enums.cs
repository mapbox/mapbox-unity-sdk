
using System;

namespace WoLfulus.LineEnding
{
    [Flags]
    public enum Mode
    {
        Error = 1,
        Assert = 2,
        Log = 4,
        Fatal = 16,
        DontPreprocessCondition = 32,
        AssetImportError = 64,
        AssetImportWarning = 128,
        ScriptingError = 256,
        ScriptingWarning = 512,
        ScriptingLog = 1024,
        ScriptCompileError = 2048,
        ScriptCompileWarning = 4096,
        StickyError = 8192,
        MayIgnoreLineNumber = 16384,
        ReportBug = 32768,
        DisplayPreviousErrorInStatusBar = 65536,
        ScriptingException = 131072,
        DontExtractStacktrace = 262144,
        ShouldClearOnPlay = 524288,
        GraphCompileError = 1048576,
        ScriptingAssertion = 2097152,
    }

    [Flags]
    public enum ConsoleFlags
    {
        Collapse = 1,
        ClearOnPlay = 2,
        ErrorPause = 4,
        Verbose = 8,
        StopForAssert = 16,
        StopForError = 32,
        Autoscroll = 64,
        LogLevelLog = 128,
        LogLevelWarning = 256,
        LogLevelError = 512,
    }
}