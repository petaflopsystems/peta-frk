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
