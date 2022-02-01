using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Petaframework
{
    public static class PtfkConsole
    {
        static bool? lastVal = null;
        static bool? Enabled = null;
        static string[] SessionsIdToTrace = new string[] { };

        static Func<bool> ActionEnabled;
        static Func<string> ActionSessionIdsToTrace;

        /// <summary>
        /// Set Console settings
        /// </summary>
        /// <param name="actionEnabled"></param>
        /// <param name="actionSessionIdsToTrace"></param>
        public static void Set(Func<bool> actionEnabled, Func<string> actionSessionIdsToTrace)
        {
            ActionEnabled = actionEnabled;
            ActionSessionIdsToTrace = actionSessionIdsToTrace;
        }

        static string _locker = "";
        private static bool? IsEnabled()
        {
            if (ActionEnabled != null)
            {
                bool e = ActionEnabled.Invoke();
                lock (_locker)
                {
                    var msg = "[Debug.PtfkConsole] Enabled? " + e;
                    if (lastVal == null)
                    {
                        lastVal = false;
                        return null;
                    }
                    else
                    if (!_lastMessages.Contains(msg) && lastVal != e)
                    {
                        Print(msg);
                        Enabled = lastVal = e;
                    }
                }
                return e;
            }
            return Enabled;
        }

        private static string[] GetSessions()
        {
            if (ActionSessionIdsToTrace != null)
            {
                string e = ActionSessionIdsToTrace.Invoke();
                if (!String.IsNullOrWhiteSpace(e))
                    return e.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
            return SessionsIdToTrace;
        }

        static List<String> _lastMessages = new List<string>();
        /// <summary>
        /// Writes the specified string value of configuration, followed by the current line terminator, to the standard output stream.
        /// </summary>
        /// <param name="message">The value to write.</param>
        public static void WriteLine(string message, params string[] args)
        {
            var e = IsEnabled();
            if (e == null || e.Value)
                Print(args?.Length > 0 ? string.Format(message, args) : message);
        }

        /// <summary>
        /// Writes the specified string value of configuration, followed by the current line terminator, to the standard output stream.
        /// </summary>
        /// <param name="exceptionToWrite">The Exception object to write.</param>
        public static void WriteLine(Exception exceptionToWrite)
        {
            var e = IsEnabled();
            if (e == null || e.Value)
                Print(Petaframework.Tools.ToJson(exceptionToWrite, true));
        }

        private static void Print(string message, int skipFrames = 2, string messageRef = "")
        {
            if (skipFrames == 2)
            {
                var i = 0;
                var diag = new System.Diagnostics.StackFrame(i, true);
                while (diag != null && !String.IsNullOrWhiteSpace(diag.GetFileName()) && diag.GetFileName().Contains(nameof(Petaframework)))
                {
                    i++;
                    diag = new System.Diagnostics.StackFrame(i, true);
                }
                skipFrames = i;
            }
            lock (_lastMessages)
            {
                if (!String.IsNullOrWhiteSpace(messageRef))
                {
                    Console.WriteLine(String.Concat(GetForegroundColorEscapeCode(ConsoleColor.DarkYellow) + "#ptfk-trace:" + GetForegroundColorEscapeCode(Console.ForegroundColor), " " + messageRef, Environment.NewLine, "              " + message, Environment.NewLine));
                    return;
                }
                var diag = new System.Diagnostics.StackFrame(skipFrames, true);
                var t = " " + diag.GetFileName() + ":" + (diag).GetFileLineNumber();

                if (_lastMessages.Count > 0 && _lastMessages.LastOrDefault().Equals(t))
                    return;
                _lastMessages = new List<string>();

                _lastMessages.Add(t);
                Console.WriteLine(String.Concat(GetForegroundColorEscapeCode(ConsoleColor.DarkYellow) + "#ptfk-trace:" + GetForegroundColorEscapeCode(Console.ForegroundColor), t, Environment.NewLine, "              " + message, Environment.NewLine));
            }
        }

        public static void WriteError(string message)
        {
            var i = 0;
            var diag = new System.Diagnostics.StackFrame(i, true);
            while (diag != null && diag.GetFileName().Contains(nameof(Petaframework)))
            {
                i++;
                diag = new System.Diagnostics.StackFrame(i, true);
            }
            var skipFrames = i;
            var t = " " + diag.GetFileName() + ":" + (diag).GetFileLineNumber();
            Console.WriteLine(String.Concat(GetForegroundColorEscapeCode(ConsoleColor.Red) + "#ptfk-error:" + GetForegroundColorEscapeCode(Console.ForegroundColor), t, Environment.NewLine, "              " + message, Environment.NewLine));
        }

        static List<String> _confMessages = new List<string>();
        /// <summary>
        /// Writes the specified string value of configuration, followed by the current line terminator, to the standard output stream. Write only one time.
        /// </summary>
        /// <param name="configName">The configuration identifier to write</param>
        /// <param name="message">The value to write.</param>
        public static void WriteConfig(string configName, string message)
        {
            lock (_confMessages)
            {
                var t = " [" + configName + "]";

                if (_confMessages.Count > 0 && _confMessages.Contains(t))
                    return;

                _confMessages.Add(t);
                Console.WriteLine(String.Concat(GetForegroundColorEscapeCode(ConsoleColor.Blue) + "#ptfk-config:" + GetForegroundColorEscapeCode(Console.ForegroundColor), t, Environment.NewLine, "              " + message, Environment.NewLine));
            }
        }

        private static string GetForegroundColorEscapeCode(ConsoleColor color)
        {
            switch (color)
            {
                case ConsoleColor.Black:
                    return "\x1B[30m";
                case ConsoleColor.DarkRed:
                    return "\x1B[31m";
                case ConsoleColor.DarkGreen:
                    return "\x1B[32m";
                case ConsoleColor.DarkYellow:
                    return "\x1B[33m";
                case ConsoleColor.DarkBlue:
                    return "\x1B[34m";
                case ConsoleColor.DarkMagenta:
                    return "\x1B[35m";
                case ConsoleColor.DarkCyan:
                    return "\x1B[36m";
                case ConsoleColor.Gray:
                    return "\x1B[37m";
                case ConsoleColor.Red:
                    return "\x1B[1m\x1B[31m";
                case ConsoleColor.Green:
                    return "\x1B[1m\x1B[32m";
                case ConsoleColor.Yellow:
                    return "\x1B[1m\x1B[33m";
                case ConsoleColor.Blue:
                    return "\x1B[1m\x1B[34m";
                case ConsoleColor.Magenta:
                    return "\x1B[1m\x1B[35m";
                case ConsoleColor.Cyan:
                    return "\x1B[1m\x1B[36m";
                case ConsoleColor.White:
                    return "\x1B[1m\x1B[37m";
                default:
                    return "\x1B[39m\x1B[22m"; // default foreground color
            }
        }

        /// <summary>
        /// Writes the specified string value, followed by the current line terminator, to the standard output stream.
        /// </summary>
        /// <param name="message">The value to write.</param>
        /// <param name="toStringfy">Object to stringify and write.</param>
        /// <param name="ignoreGlobalConfig">Flag to ignore the global config. If True, always will write the message. Default value: false.</param>
        public static void WriteLine(string message, object toStringfy, bool ignoreGlobalConfig = false)
        {            
            if (ignoreGlobalConfig)
            {
                Print(message + ":" + Petaframework.Tools.ToJson(toStringfy, true), 1);
            }
            else
                WriteLine(message + ":" + Petaframework.Tools.ToJson(toStringfy, true));
        }

        public static void WriteLine(string message, bool ignoreGlobalConfig)
        {
            if (ignoreGlobalConfig)
            {
                Print(message, 1);
            }
            else
                WriteLine(message);
        }

        public static void WriteLine(string message, bool ignoreGlobalConfig, string messageRef)
        {
            if (ignoreGlobalConfig)
            {
                Print(message, 2, messageRef);
            }
            else
                WriteLine(message);
        }

        /// <summary>
        /// Writes the specified string value, followed by the current line terminator, to the standard output stream.
        /// </summary>
        /// <param name="session">Writing session.</param>
        /// <param name="message">The value to write.</param>
        public static void WriteLine(this IPtfkSession session, string message)
        {
            var e = IsEnabled();
            if (e.HasValue && e.Value && (GetSessions().Contains(session.Login)))
            {
                WriteLine("[SId: " + session.Login + "]" + message);
            }
        }

    }
}
