using System;
using System.IO;

namespace Petaframework
{
    public class Settings
    {
        /// <summary>
        /// Indicates the Storage Strategy to preserve Files
        /// </summary>        
        public enum StorageMode
        {
            /// <summary>
            /// Default option. Save the files in Database Management System
            /// </summary>
            OnDBMS,
            /// <summary>
            /// Save the files in Content Database. It necessary to inform the root path (ContentDBRootPath) to preserve files 
            /// </summary>
            OnContentDB
        }

        public class Storage
        {
            public static string Path
            {
                get
                {

                    try
                    {
                        var p = Strict.ConfigurationManager.CurrentConfiguration["AppConfiguration:" + Constants.StorageModeKey];
                        return p;
                    }
                    catch (Exception)
                    {
                        return Constants.OnStorageDBMSFlag;
                    }
                }


            }

            /// <summary>
            /// Indicates the root path to preserve files. The default path is the [Current Assembly Directory]/PtfkContentDB
            /// </summary>
            public static string ContentDBRootPath { get; set; } = System.IO.Path.Combine(new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location).Directory.FullName, "PtfkContentDB");

            /// <summary>
            /// Indicates the Storage strategy to preserve files to entire system
            /// </summary>
            public StorageMode SystemStorageMode { get; protected set; } = StorageMode.OnDBMS;
        }



        public class Http
        {
            public static long FileSizeRequestLimit
            {
                get
                {
                    try
                    {
                        return Convert.ToInt64(Strict.ConfigurationManager.CurrentConfiguration["AppConfiguration:" + Constants.FileSizeRequestLimitKey]);
                    }
                    catch (Exception)
                    {
                        return 20000000;
                    }
                }

            }
        }

    }
}
