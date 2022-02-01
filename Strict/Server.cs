using PetaframeworkStd;
using System;
using System.IO;

namespace Petaframework.Strict
{
    public class Server
    {
        private static String CONFIG_PATH
        {
            get
            {
                //if (PetaframeworkStd.OS.IsGnu())//For Docker Environment
                //{
                //    String path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).ToLower();
                //    //path = path.Remove(0, 5);
                //    DirectoryInfo dockerDir = new DirectoryInfo(Path.Combine(path, "wwwroot"));
                //    PtfkConsole.WriteConfig("Server root path:" + dockerDir.FullName);
                //    return Path.Combine(dockerDir.FullName);
                //}
                //else
                //if (Tools.IsProductionEnvironment)
                //{
                //    String path = Path.GetDirectoryName(System.Reflection.Assembly.GetAssembly(typeof(Server)).Location).ToLower();
                //    //path = path.Remove(0, 6);
                //    FileInfo file = new FileInfo(path);
                //    return Path.Combine(file.FullName, "wwwroot", "");

                //}
                //else
                //{
                //    String path = Path.GetDirectoryName(System.Reflection.Assembly.GetAssembly(typeof(Server)).Location).ToLower();
                //    //path = path.Remove(0, 6);
                //    FileInfo file = new FileInfo(path);
                //    PtfkConsole.WriteLine(Path.Combine(file.Directory.Parent.Parent.FullName, "wwwroot"));
                //    return Path.Combine(file.Directory.Parent.Parent.FullName, "wwwroot") + "\\";
                //}
                return PetaframeworkStd.OS.GetAssemblyPath("wwwroot");
            }
        }

        public static string MapPath(string path)
        {
            path = path.Replace("\\", "/");

            var webPath = PtfkEnvironment.CurrentEnvironment?.WebHostEnvironment?.WebRootPath;
            if (String.IsNullOrWhiteSpace(webPath))
            {
                webPath = CONFIG_PATH;
            }

            if (path.StartsWith("~"))
                if (webPath.EndsWith(@"//") || webPath.EndsWith(@"\"))
                    return path.Replace(path.StartsWith("~/") ? "~/" : "~", webPath);
                else
                    return path.Replace("~", webPath + "/");
            else
                return String.Concat(webPath, path);
        }

        public static FileInfo GetFileOnServer(string relativePath)
        {
            var path = MapPath(relativePath);
            return new FileInfo(path);
        }
    }
}
