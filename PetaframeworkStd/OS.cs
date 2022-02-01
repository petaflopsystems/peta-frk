using Petaframework;
using PetaframeworkStd;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PetaframeworkStd
{
    public static class OS
    {
        public static bool IsWin() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool IsMac() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        public static bool IsGnu() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        public static string GetCurrent()
        {
            return
            (IsWin() ? "win" : null) ??
            (IsMac() ? "mac" : null) ??
            (IsGnu() ? "gnu" : null);
        }

        public static string GetAssemblyPath(string relativePath = "")
        {
            //var rootPath = PtfkEnvironment.CurrentEnvironment.WebHostEnvironment.ContentRootPath;
            if (PetaframeworkStd.OS.IsGnu())//For Docker Environment
            {
                String path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).ToLower();
                //path = path.Remove(0, 5);
                DirectoryInfo dockerDir = new DirectoryInfo(Path.Combine(path, relativePath));
                PtfkConsole.WriteConfig("App root path (GNU)", path);
                return Path.Combine(dockerDir.FullName);
            }
            else
            {
                String path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).ToLower();
                //path = path.Remove(0, 6);
                FileInfo file = new FileInfo(path);
                PtfkConsole.WriteConfig("App root path (Win/Mac)", path);
                return Path.Combine(path, relativePath) + "\\";
            }
        }
    }
}
