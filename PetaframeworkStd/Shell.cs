using PetaframeworkStd.Commands;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace PetaframeworkStd
{
    public static class Shell
    {
        public class Response
        {
            public int code { get; set; }
            public string stdout { get; set; }
            public string stderr { get; set; }
        }

        public enum Output
        {
            Hidden,
            Internal,
            External
        }

        private static string GetFileName()
        {
            string fileName = "";
            try
            {
                switch (OS.GetCurrent())
                {
                    case "win":
                        fileName = "cmd.exe";
                        break;
                    case "mac":
                    case "gnu":
                        fileName = "/bin/bash";
                        break;
                    default:
                        return String.Empty;
                }
                return fileName;
            }
            catch (Exception Ex)
            {
                throw Ex;
            }
        }

        private static string CommandConstructor(string cmd, Output? output = Output.Hidden, string dir = "")
        {
            try
            {
                switch (OS.GetCurrent())
                {
                    case "win":
                        if (!String.IsNullOrEmpty(dir))
                        {
                            dir = $" \"{dir}\"";
                        }

                        cmd = $"/c \"{cmd}\"";
                        break;
                    case "mac":
                    case "gnu":
                        if (!String.IsNullOrEmpty(dir))
                        {
                            dir = $" '{dir}'";
                        }
                       
                        cmd = $"-c \"{cmd}\"";
                        break;
                }
                return cmd;
            }
            catch (Exception Ex)
            {
                throw Ex;
            }
        }

        private static Response Term(string cmd, Output? output = Output.Hidden, string dir = "", string runtimePath = "", bool dependentOfDotNetEXE = false)
        {
            var result = new Response();
            var stderr = new StringBuilder();
            var stdout = new StringBuilder();
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = dependentOfDotNetEXE ? "dotnet.exe" : GetFileName();
                startInfo.Arguments = dependentOfDotNetEXE ? cmd : CommandConstructor(cmd, output, dir);
                startInfo.RedirectStandardOutput = !(output == Output.External);
                startInfo.RedirectStandardError = !(output == Output.External);
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = !(output == Output.External);
                if (!String.IsNullOrEmpty(dir) && output != Output.External)
                {
                    startInfo.WorkingDirectory = dir;
                }
                if (!String.IsNullOrWhiteSpace(runtimePath))
                    startInfo.WorkingDirectory = runtimePath;

                using (Process process = Process.Start(startInfo))
                {
                    switch (output)
                    {
                        case Output.Internal:
                            // $"".fmNewLine();

                            while (!process.StandardOutput.EndOfStream)
                            {
                                string line = process.StandardOutput.ReadLine();
                                stdout.AppendLine(line);
                                Console.WriteLine(line);
                            }

                            while (!process.StandardError.EndOfStream)
                            {
                                string line = process.StandardError.ReadLine();
                                stderr.AppendLine(line);
                                Console.WriteLine(line);
                            }
                            break;
                        case Output.Hidden:
                            stdout.AppendLine(process.StandardOutput.ReadToEnd());
                            stderr.AppendLine(process.StandardError.ReadToEnd());
                            break;
                    }
                    process.WaitForExit();
                    result.stdout = stdout.ToString();
                    result.stderr = stderr.ToString();
                    result.code = process.ExitCode;
                }
            }
            catch (Exception Ex)
            {
                Console.WriteLine(Ex.Message);
                result.stderr = Ex.Message;
                result.code = -1;
            }
            return result;
        }

        public static bool Check()
        {
            Response result = Term("dotnet --version", Output.Hidden);
            Console.WriteLine(result.code.ToString());
            if (result.code == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static Boolean RunDotNetScript(FileInfo dllFile, out ResultClass scriptResult, params string[] args)
        {
            if (!dllFile.Exists)
                throw new FileNotFoundException(dllFile.FullName);

            if (!Check())
            {
                scriptResult = new ResultClass { Success = false, Message = "dotnet not installed!", EndDate = DateTime.Now };
                return false;
            }

            Response result = Term(@"""" + dllFile.FullName + @""" " + String.Join(@" ", args) + @" ", Output.Internal, "", "", true);

            var line = "";
            try
            {
                line = (result.stdout + result.stderr).Trim().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList().LastOrDefault();                
            }
            catch (Exception ex)
            {

            }
            if (!String.IsNullOrWhiteSpace(line))
            {
                try
                {
                    scriptResult = Petaframework.Tools.FromJson<ResultClass>(line);
                    return scriptResult.Success;
                }
                catch (Exception ex)
                {
                    scriptResult = new ResultClass { Success = (result.code == 0), Message = line, EndDate = DateTime.Now };
                    return scriptResult.Success;
                }                
            }

            scriptResult = new ResultClass { Success = (result.code == 0), Message = (result.stdout + result.stderr).Trim(), EndDate = DateTime.Now };
            return scriptResult.Success;
        }
    }
}
