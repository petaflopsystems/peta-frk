using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Petaframework.Interfaces;
using Petaframework.Strict;
using PetaframeworkStd.Exceptions;
using PetaframeworkStd.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static Petaframework.Settings;

namespace Petaframework
{
    /// <summary>
    /// Class that used for File Upload
    /// </summary>
    [Serializable]
    [NotMapped]
    public class PtfkFileInfo
    {
        private static string _FUP_SESSION_NAME = "_PTFK_UserFiles_";
        private static Boolean _UsePtfkMediaEngine { get { return BusinessClassMedia != null; } }

        private const int DELETE_FILES_AFTER = 1;

        internal static object BusinessClassMedia { get; set; }

        public String UID { get; set; }
        [JsonIgnore]
        public String OwnerID { get; set; }
        [JsonIgnore]
        public System.IO.FileInfo FileInfo { get; set; }
        public String EntityName { get; set; }
        [JsonIgnore]
        public long ParentID { get; set; }
        public String Name { get; set; }
        public String EntityProperty { get; set; }
        public String ExternalInfo { get; set; }
        [JsonIgnore]
        public Boolean ToDelete { get; set; } = false;

        [JsonIgnore]
        private string _hash = string.Empty;
        [JsonIgnore]
        public string Hash
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_hash) || !FileInfo.Exists)
                    return _hash;
                _hash = Tools.GetHashMD5(FileInfo);
                return _hash;
            }
            set
            {
                _hash = value;
            }
        }
        [JsonIgnore]
        public long MediaID { get; set; } = 0;

        [JsonIgnore]
        public System.Threading.Tasks.Task<Object> GenericArg { get; set; }


        private static DirectoryInfo di;

        public static String GetServerPath(String ownerID)
        {
            return "~/_Temp/" + ownerID + "/";
        }

        public static DirectoryInfo GetDirectoryInfo(IPtfkSession session)
        {
            di = new DirectoryInfo(Server.MapPath(GetServerPath(session.Login)));
            if (!di.Exists)
                di.Create();
            else
            {
                foreach (var item in di.GetFiles().Where(x => x.LastWriteTimeUtc <= DateTime.Now.AddHours(-DELETE_FILES_AFTER)))
                {
                    try
                    {
                        item.Delete();
                    }
                    catch (Exception ex) { }
                }
            }
            return di;
        }

        public static List<PtfkFileInfo> MaterializeFiles(
  IPtfkForm entity,
  string entityProperty)
        {
            return PtfkFileInfo.GetFilesPrivate(entity.GetOwner(), entity.GetType().Name, entityProperty, entity.Id, entity, (ILogger)null);
        }

        private static List<PtfkFileInfo> GetFilesPrivate(
  IPtfkSession session,
  string entityName,
  string entityProperty,
  long entityID,
  IPtfkForm form = null,
  ILogger _logger = null)
        {
            List<PtfkFileInfo> files = PtfkFileInfo.GetFiles(session);
            EventId id = new Random().Next();
            _logger?.Log<PtfkFileInfo>(LogLevel.Information, id, null, new Exception(Tools.ToJson(new
            {
                AllInMemory = files,
                Owner = session?.Current?.Login
            }, true, false)), null);
            List<PtfkFileInfo> list1 = files.Where<PtfkFileInfo>((Func<PtfkFileInfo, bool>)(x => x.EntityName.Equals(entityName) && (x.ParentID.Equals(0) && (x.EntityProperty.Equals(entityProperty) || x.EntityProperty.EndsWith("_" + entityProperty) || entityProperty.EndsWith("_" + x.EntityProperty))) && x.FileInfo.Exists)).ToList<PtfkFileInfo>();
            List<PtfkFileInfo> list2 = files.Where<PtfkFileInfo>((Func<PtfkFileInfo, bool>)(x => x.EntityName.Equals(entityName) && (x.ParentID.Equals(entityID) && (x.EntityProperty.Equals(entityProperty) || x.EntityProperty.EndsWith("_" + entityProperty) || entityProperty.EndsWith("_" + x.EntityProperty))) && x.FileInfo.Exists)).ToList<PtfkFileInfo>();
            IPtfkEntity entityByClassName = Tools.GetIPtfkEntityByClassName(session.GetType().Assembly, entityName, session, (ILogger)null);
            if (entityByClassName == null)
            {
                PtfkFileInfo.LogInfo(113, _logger, session, id);
                if (form != null && list1.Count<PtfkFileInfo>() > 0 && list2.Count<PtfkFileInfo>() == 0)
                {
                    files.ForEach((System.Action<PtfkFileInfo>)(x => x.FileInfo = new FileInfo(x.FileInfo.FullName)));
                    list2 = files.Where<PtfkFileInfo>((Func<PtfkFileInfo, bool>)(x => x.EntityName.Equals(entityName) && !x.ToDelete && (x.ParentID.Equals(entityID) || x.ParentID.Equals(0)) && (x.EntityProperty.Equals(entityProperty) || x.EntityProperty.EndsWith("_" + entityProperty) || entityProperty.EndsWith("_" + x.EntityProperty)) && x.FileInfo.Exists)).ToList<PtfkFileInfo>();
                }
                return list2;
            }
            PropertyInfo property1 = entityByClassName.GetType().GetProperty(entityProperty);
            Type type1 = Nullable.GetUnderlyingType(property1.PropertyType);
            if ((object)type1 == null)
                type1 = property1.PropertyType;
            Type t1 = type1;
            if (entityProperty.StartsWith(Constants.EntityPtfkFileInfoPrefix))
            {
                PtfkFileInfo.LogInfo(128, _logger, session, id);
                if (form != null && list1.Count<PtfkFileInfo>() > 0 && list2.Count<PtfkFileInfo>() == 0)
                {
                    PtfkFileInfo.LogInfo(131, _logger, session, id);
                    files.ForEach((System.Action<PtfkFileInfo>)(x => x.FileInfo = new FileInfo(x.FileInfo.FullName)));
                    list2 = files.Where<PtfkFileInfo>((Func<PtfkFileInfo, bool>)(x => x.EntityName.Equals(entityName) && !x.ToDelete && (x.ParentID.Equals(entityID) || x.ParentID.Equals(0)) && (x.EntityProperty.Equals(entityProperty) || x.EntityProperty.EndsWith("_" + entityProperty) || entityProperty.EndsWith("_" + x.EntityProperty)) && x.FileInfo.Exists)).ToList<PtfkFileInfo>();
                }
                //else
                // if (list2.Count() == 0)
                //    list2 = files.Where<PtfkFileInfo>((Func<PtfkFileInfo, bool>)(x => x.EntityName.Equals(entityName) && !x.ToDelete && (!String.IsNullOrWhiteSpace(x.UID) && x.UID.Equals(entityID)) && (x.EntityProperty.Equals(entityProperty) || x.EntityProperty.EndsWith("_" + entityProperty) || entityProperty.EndsWith("_" + x.EntityProperty)) && x.FileInfo.Exists)).ToList<PtfkFileInfo>();
                return PtfkFileInfo.GetUniqueFiles(list2, t1);
            }
            try
            {
                PtfkFileInfo.LogInfo(141, _logger, session, id);
                entityProperty = Constants.EntityPtfkFileInfoPrefix + entityProperty;
                list2 = PtfkFileInfo.GetFiles(session).Where<PtfkFileInfo>((Func<PtfkFileInfo, bool>)(x => x.EntityName.Equals(entityName) && (x.ParentID.Equals(entityID) && (x.EntityProperty.Equals(entityProperty) || x.EntityProperty.EndsWith("_" + entityProperty) || entityProperty.EndsWith("_" + x.EntityProperty))) && x.FileInfo.Exists)).ToList<PtfkFileInfo>();
                if (form != null && list1.Count<PtfkFileInfo>() > 0 && list2.Count<PtfkFileInfo>() == 0)
                {
                    PtfkFileInfo.LogInfo(146, _logger, session, id);
                    files.ForEach((System.Action<PtfkFileInfo>)(x => x.FileInfo = new FileInfo(x.FileInfo.FullName)));
                    list2 = files.Where<PtfkFileInfo>((Func<PtfkFileInfo, bool>)(x => x.EntityName.Equals(entityName) && !x.ToDelete && (x.ParentID.Equals(entityID) || x.ParentID.Equals(0)) && (x.EntityProperty.Equals(entityProperty) || x.EntityProperty.EndsWith("_" + entityProperty) || entityProperty.EndsWith("_" + x.EntityProperty)) && x.FileInfo.Exists)).ToList<PtfkFileInfo>();
                }
                PropertyInfo property2 = entityByClassName.GetType().GetProperty(entityProperty);
                Type type2 = Nullable.GetUnderlyingType(property2.PropertyType);
                if ((object)type2 == null)
                    type2 = property2.PropertyType;
                Type t2 = type2;
                return PtfkFileInfo.GetUniqueFiles(list2, t2);
            }
            catch (Exception ex)
            {
                PtfkFileInfo.LogError(131, _logger, session, id, ex);
                return list2;
            }
        }

        private static void LogInfo(int line, ILogger _logger, IPtfkSession session, EventId id)
        {
            _logger?.Log<PtfkFileInfo>(LogLevel.Information, id, null, new Exception(Tools.ToJson((object)new
            {
                Line = line,
                Owner = session?.Current?.Login
            }, true, false)), null);
        }

        private static void LogError(
          int line,
          ILogger _logger,
          IPtfkSession session,
          EventId id,
          Exception ex)
        {
            _logger?.Log<PtfkFileInfo>(LogLevel.Error, id, null, new Exception(Tools.ToJson((object)new
            {
                Line = line,
                Owner = session?.Current?.Login,
                Message = ex.Message,
                InnerException = ex.InnerException,
                StackTrace = ex.StackTrace
            }, true, false)), null);
        }

        private static List<PtfkFileInfo> GetUniqueFiles(
  List<PtfkFileInfo> docs,
  Type t)
        {
            if (docs.Count() == 0)
                return docs;
            if (Tools.DoesTypeWereSimilar(t, typeof(PtfkFileInfo)))
                return new List<PtfkFileInfo>()
        {
          docs.OrderBy(x => x.FileInfo.LastWriteTime).LastOrDefault()
        };
            if (!Tools.DoesTypeWereSimilar(t, typeof(ICollection<PtfkFileInfo>)))
                return docs;
            List<PtfkFileInfo> source1 = new List<PtfkFileInfo>();
            foreach (PtfkFileInfo doc in docs)
            {
                PtfkFileInfo d = doc;
                IEnumerable<PtfkFileInfo> source2 = source1.Where(x => x.Hash.Equals(d.Hash));
                if (source2.Count<PtfkFileInfo>() == 0)
                    source1.Add(d);
                else
                    source1.Add(source2.OrderBy(x => x.FileInfo.LastWriteTime).LastOrDefault());
            }
            return source1;
        }

        public static List<PtfkFileInfo> GetFiles(
          IPtfkSession session,
          string entityName,
          string entityProperty,
          long entityID,
          ILogger _logger = null)
        {
            return PtfkFileInfo.GetFilesPrivate(session, entityName, entityProperty, entityID, (IPtfkForm)null, _logger);
        }

        internal static List<PtfkFileInfo> GetFiles(IPtfkSession session)
        {
            lock (PtfkFileInfo._FUP_SESSION_NAME)
            {
                object obj = PtfkFileInfo.Get(session);
                List<PtfkFileInfo> source = new List<PtfkFileInfo>();
                try
                {
                    if (obj != null)
                        source = ((IEnumerable<PtfkFileInfo>)obj).Where<PtfkFileInfo>((Func<PtfkFileInfo, bool>)(x => x.OwnerID.Equals(session.Login))).ToList<PtfkFileInfo>();
                }
                catch (Exception ex)
                {
                }
                finally
                {
                    if (source.Count<PtfkFileInfo>() == 0)
                    {
                        FileInfo[] files = ((IEnumerable<FileInfo>)PtfkFileInfo.GetDirectoryInfo(session).GetFiles("*.*", SearchOption.AllDirectories)).Where<FileInfo>((Func<FileInfo, bool>)(x => x.LastWriteTimeUtc <= DateTime.UtcNow.AddHours(-1.0))).ToArray<FileInfo>();
                        new Thread((ThreadStart)(() => PtfkFileInfo.DeleteFiles(files))).Start();
                    }
                }
                source.ForEach((System.Action<PtfkFileInfo>)(x => x.FileInfo = new FileInfo(x.FileInfo.FullName)));
                return source;
            }
        }

        internal static void DeleteFiles(FileInfo[] files)
        {
            foreach (FileInfo file in files)
            {
                try
                {
                    file.Delete();
                }
                catch (Exception ex)
                {
                }
            }
        }

        private static object Get(IPtfkSession owner)
        {
            try
            {
                if (Current.Session == null)
                    PtfkFileInfo.Set(owner, (object)new List<PtfkFileInfo>());
                if (!String.IsNullOrWhiteSpace(owner.Login))
                    return Current.Session[PtfkFileInfo._FUP_SESSION_NAME][owner.Login];
                return null;
            }
            catch (Exception ex)
            {
                PtfkFileInfo.Set(owner, (object)new List<PtfkFileInfo>());
                return PtfkFileInfo.Get(owner);
            }
        }

        private static void Set(IPtfkSession owner, object filesList)
        {
            if (Current.Session == null)
                Current.Session = new Dictionary<string, Dictionary<string, object>>();
            if (!Current.Session.ContainsKey(PtfkFileInfo._FUP_SESSION_NAME))
                Current.Session[PtfkFileInfo._FUP_SESSION_NAME] = new Dictionary<string, object>();
            if (!String.IsNullOrWhiteSpace(owner.Login))
                Current.Session[PtfkFileInfo._FUP_SESSION_NAME][owner.Login] = filesList;
        }

        private static PtfkFileInfo UploadFilePrivate<T>(
  T form,
  string propertyName,
  string fileExtension,
  byte[] fileContent,
  string fileName,
  string externalInfo = null)
  where T : class, IPtfkForm, IPtfkEntity
        {
            FileInfo fileInfo = new FileInfo(Path.Combine(PtfkFileInfo.GetDirectoryInfo(form.Owner.Current).FullName, propertyName + (fileExtension.StartsWith(".") ? "" : ".") + fileExtension));
            File.WriteAllBytes(fileInfo.FullName, fileContent);
            PtfkFileInfo file = new PtfkFileInfo()
            {
                FileInfo = fileInfo,
                OwnerID = form.Owner.Current.Login,
                UID = Guid.NewGuid().ToString(),
                EntityName = form.GetType().Name,
                ParentID = ((IPtfkEntity)form).Id,
                Name = fileInfo.Name,
                EntityProperty = propertyName,
                ExternalInfo = externalInfo
            };
            string mirroredOf = form.GetMirroredOf(propertyName);
            if (mirroredOf != null)
            {
                PropertyInfo property = form.GetType().GetProperty(mirroredOf);
                if (property != (PropertyInfo)null && property.PropertyType == typeof(string))
                {
                    string json = property.GetValue((object)form)?.ToString();
                    IList<string> stringList = (IList<string>)null;
                    if (string.IsNullOrWhiteSpace(json))
                        stringList = (IList<string>)new List<string>();
                    else if (Tools.IsJsonArray(json))
                        stringList = Tools.FromJson<IList<string>>(json, false);
                    stringList.Add(file.Hash);
                    property.SetValue((object)form, (object)Tools.ToJson((object)stringList));
                    PtfkMedia ptfkMedia = new PtfkMedia();
                    ptfkMedia.Bytes = File.ReadAllBytes(file.FileInfo.FullName);
                    ptfkMedia.EntityName = file.EntityName;
                    ptfkMedia.Extension = file.FileInfo.Extension;
                    ptfkMedia.Hash = file.Hash;
                    ptfkMedia.Name = string.IsNullOrWhiteSpace(fileName) ? file.Name : fileName;
                    ptfkMedia.Path = "*";
                    ptfkMedia.Size = file.FileInfo.Length;
                    ptfkMedia.EntityProperty = propertyName;
                    ptfkMedia.ExternalInfo = externalInfo;
                    try
                    {
                        ((object)form as PtfkForm<T>).CurrentGenerator.CurrentPageConfig.CurrDForm.MediaFiles.Add((IPtfkMedia)ptfkMedia);
                    }
                    catch (Exception ex)
                    {
                        throw new FormConfigurationException("No has found configuration informations to this Form.");
                    }
                }
            }
            PtfkFileInfo.AddFile(form.Owner, file, (ILogger)null);
            return file;
        }

        /// <summary>
        /// Uploads a user's file to the file persistence environment.
        /// </summary>
        /// <param name="form">The form that contains the user's file</param>
        /// <param name="propertyName">The property name that contains de user's file on the form</param>
        /// <param name="fileExtension">The file's extension</param>
        /// <param name="fileContent">The file's content</param>
        /// <returns>PtfkFileInfo</returns>
        public static PtfkFileInfo UploadFile<T>(
         T form,
         string propertyName,
         string fileExtension,
         FileInfo fileInfo)
         where T : class, IPtfkForm, IPtfkEntity
        {
            return PtfkFileInfo.UploadFilePrivate<T>(form, propertyName, fileExtension, File.ReadAllBytes(fileInfo.FullName), fileInfo.Name, (string)null);
        }

        /// <summary>
        /// Uploads a user's file to the file persistence environment.
        /// </summary>
        /// <param name="form">The form that contains the user's file</param>
        /// <param name="propertyName">The property name that contains de user's file on the form</param>
        /// <param name="fileExtension">The file's extension</param>
        /// <param name="fileContent">The file's content</param>
        /// <param name="fileName">File Name</param>
        /// <param name="externalInfo">External Info to Persist</param>
        /// <returns>PtfkFileInfo</returns>
        public static PtfkFileInfo UploadFile<T>(
          T form,
          string propertyName,
          string fileExtension,
          byte[] fileContent,
          string fileName = null,
          string externalInfo = null)
          where T : class, IPtfkForm, IPtfkEntity
        {
            return PtfkFileInfo.UploadFilePrivate<T>(form, propertyName, fileExtension, fileContent, fileName, externalInfo);
        }

        public static void AddFile(IPtfkSession session, PtfkFileInfo file, ILogger _logger = null)
        {
            lock (PtfkFileInfo._FUP_SESSION_NAME)
            {
                List<PtfkFileInfo> ptfkFileInfoList = (List<PtfkFileInfo>)PtfkFileInfo.Get(session);
                if (ptfkFileInfoList == null)
                {
                    ptfkFileInfoList = new List<PtfkFileInfo>();
                    ptfkFileInfoList.Add(file);
                }
                else
                {
                    int index = ptfkFileInfoList.FindIndex((Predicate<PtfkFileInfo>)(x => x.UID.Equals(file.UID) && (x.ParentID.Equals(file.ParentID) && x.OwnerID.Equals(file.OwnerID)) && x.EntityProperty.Equals(file.EntityProperty)));
                    if (index >= 0)
                    {
                        if (ptfkFileInfoList[index].ToDelete)
                            file.ToDelete = true;
                        ptfkFileInfoList[index] = file;
                    }
                    else
                        ptfkFileInfoList.Add(file);
                }
                PtfkFileInfo.Set(session, (object)ptfkFileInfoList);
                Assembly assembly = PtfkFileInfo.BusinessClassMedia.GetType().Assembly;
                string entityName = file.EntityName;
                PrivatePtfkSession privatePtfkSession = new PrivatePtfkSession();
                privatePtfkSession.Login = file.OwnerID;
                ILogger _logger1 = _logger;
                Tools.GetIPtfkEntityByClassName(assembly, entityName, (IPtfkSession)privatePtfkSession, _logger1).OnFileUploaded(new PtfkEventArgs<PtfkFileInfo>(ref file));
            }
        }

        public static void ClearFiles(string entityName, IPtfkSession session, params long[] parentID)
        {
            foreach (long num in ((IEnumerable<long>)parentID).Distinct<long>())
            {
                long lID = 0;
                long.TryParse(num.ToString(), out lID);
                lock (PtfkFileInfo._FUP_SESSION_NAME)
                {
                    if (lID <= 0)
                    {
                        var includedList = GetFiles(session).Where(x => !x.OwnerID.Equals(session.Login) &&
             !x.EntityName.Equals(entityName)
             ).ToList();
                        PtfkFileInfo.Set(session, (object)PtfkFileInfo.GetFiles(session).Where<PtfkFileInfo>((Func<PtfkFileInfo, bool>)(x => !includedList.Contains(x))).ToList<PtfkFileInfo>());
                    }
                    else
                    {
                        List<PtfkFileInfo> includedList = PtfkFileInfo.GetFiles(session).Where<PtfkFileInfo>((Func<PtfkFileInfo, bool>)(x => !x.OwnerID.Equals(session.Login) && !x.ParentID.Equals(lID) && !x.EntityName.Equals(entityName))).ToList<PtfkFileInfo>();
                        PtfkFileInfo.Set(session, (object)PtfkFileInfo.GetFiles(session).Where<PtfkFileInfo>((Func<PtfkFileInfo, bool>)(x => !includedList.Contains(x))).ToList<PtfkFileInfo>());
                    }
                }
            }
        }


        private static void RemoveFile(IPtfkSession session, PtfkFileInfo file)
        {
            List<PtfkFileInfo> ptfkFileInfoList = (List<PtfkFileInfo>)PtfkFileInfo.Get(session);
            ptfkFileInfoList?.Remove(file);
            PtfkFileInfo.Set(session, (object)ptfkFileInfoList);
            file.FileInfo.Delete();
        }

        public static void DeleteFile(IPtfkSession session, string qquuid)
        {
            lock (PtfkFileInfo._FUP_SESSION_NAME)
            {
                object obj = PtfkFileInfo.Get(session);
                PtfkFileInfo file = obj == null ? new PtfkFileInfo() : ((IEnumerable<PtfkFileInfo>)obj).Where<PtfkFileInfo>((Func<PtfkFileInfo, bool>)(x => x.UID.Equals(qquuid))).FirstOrDefault<PtfkFileInfo>();
                try
                {
                    if (PtfkFileInfo.IsNewFile(file))
                    {
                        PtfkFileInfo.RemoveFile(session, file);
                    }
                    else
                    {
                        file.ToDelete = true;
                        PtfkFileInfo.AddFile(session, file, (ILogger)null);
                    }
                }
                catch (Exception ex)
                {
                }
            }
        }

        internal static void InitializeBusinessClass(IPtfkForm entity, IPtfkSession session)
        {
            BusinessClassMedia = Tools.GetIBusinessMediaClass(entity.GetType().Assembly, session.Login);
        }

        /// <summary>
        /// Loads entity files to the user interface environment
        /// </summary>
        /// <param name="entity">Entity that has the files for loading</param>
        /// <param name="propertyName">Nome da propriedade da entidade que contém o arquivo</param>
        /// <param name="session">Then Owner Session that will view the file</param>
        public static void LoadFilesFromEntity(IPtfkForm entity, String propertyName, IPtfkSession session)
        {
            lock (_FUP_SESSION_NAME)
            {
                InitializeBusinessClass(entity, session);

                var ID = Convert.ToInt64(entity.GetType().GetProperty(entity.GetIdAttributeName()).GetValue(entity));
                var obj = entity.GetType().GetProperty(propertyName).GetValue(entity);
                if (ID > 0 && obj != null)
                {
                    if (!_UsePtfkMediaEngine)
                    {
                        MaterializeFile(session, ID, obj as byte[], entity, propertyName);
                    }
                    else
                    {
                        string jsonStr = "";
                        List<String> hashList = new List<string>();

                        try
                        {
                            hashList = JsonConvert.DeserializeObject<List<String>>(obj as String);
                            var instance = Activator.CreateInstance(BusinessClassMedia.GetType(), new object[] { session });
                            var method = BusinessClassMedia.GetType().GetMethods().Where(x => x.Name.Equals(nameof(IPtfkBusiness<object>.List)) && x.GetParameters().Length == 0).FirstOrDefault();
                            var list = method.Invoke(BusinessClassMedia, null) as IQueryable<IPtfkMedia>;

                            var medias = from m in list
                                         where m.EntityId.Equals(ID) && m.EntityName.Equals(entity.GetType().Name) &&
                                         string.IsNullOrWhiteSpace(m.IsDeath) &&
                                        !string.IsNullOrWhiteSpace(m.Hash) && hashList.Contains(m.Hash)
                                         select m;
                            foreach (var item in medias)
                            {
                                switch (StorageMode)
                                {
                                    case StorageMode.OnContentDB:
                                        var filePath = GetStorageRelativeFilePath(entity, propertyName, item);
                                        var file = new FileInfo(Path.Combine(Settings.Storage.Path, filePath));
                                        if (file.Exists)
                                            MaterializeFile(session, ID, File.ReadAllBytes(file.FullName), entity, propertyName, item);
                                        else if (!file.Exists && item.Bytes?.Length > 0)
                                            MaterializeFile(session, ID, item.Bytes, entity, propertyName, item);
                                        else
                                            throw new FileNotFoundOnStorageException(filePath);
                                        break;
                                    default:
                                        MaterializeFile(session, ID, item.Bytes, entity, propertyName, item);
                                        break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }
        }

        internal static String GetStorageAbsoluteFilePath(IPtfkForm entity, String propertyName, IPtfkMedia media, bool toSaveName = false)
        {
            switch (StorageMode)
            {
                case StorageMode.OnContentDB:
                    return Path.Combine(Settings.Storage.Path, GetStorageRelativeFilePath(entity, propertyName, media, toSaveName));
                    break;
                default:
                    return string.Empty;
                    break;
            }
        }

        internal static String GetStorageRelativeFilePath(IPtfkForm entity, String propertyName, IPtfkMedia media, bool toSaveName = false)
        {
            var name = Guid.NewGuid().ToString() + media.Extension;
            if (!toSaveName && media?.Path.Trim() != Constants.OnStorageDBMSFlag && !String.IsNullOrWhiteSpace(media?.Path))
                return media.Path;

            if (toSaveName && StorageMode == StorageMode.OnContentDB && media.Hash == Tools.GetHashMD5(media.Bytes))
                if (media.Path.Trim() != Constants.OnStorageDBMSFlag)
                    return media.Path;
                else
                {
                    var di = new DirectoryInfo(Path.Combine(Settings.Storage.Path, GetStorageFolderPath(entity, propertyName)));
                    if (di.Exists)
                    {
                        var f = Directory.GetFiles(di.FullName, "*.*").Where(x => Tools.GetHashMD5(new FileInfo(x)).Equals(media.Hash)).FirstOrDefault();
                        if (f != null)
                        {
                            return Path.Combine(GetStorageFolderPath(entity, propertyName), f);
                        }
                    }
                }

            if (entity != null)
                return Path.Combine(GetStorageFolderPath(entity, propertyName), name);
            else
                return Path.Combine(propertyName, media?.Name);
        }

        internal static String GetStorageFolderPath(IPtfkForm entity, String propertyName)
        {
            if (entity != null)
                return Path.Combine(entity.ClassName, entity.Id.ToString(), propertyName);
            throw new FileNotFoundOnStorageException("Storage Path has not been generated!");
        }

        internal static StorageMode StorageMode
        {
            get
            {
                var p = Settings.Storage.Path;
                if (Directory.Exists(p))
                    return Settings.StorageMode.OnContentDB;
                return Settings.StorageMode.OnDBMS;
            }
        }

        private static void MaterializeFile(
    IPtfkSession session,
    long ID,
    byte[] obj,
    IPtfkForm entity,
    string propertyName,
    IPtfkMedia item = null)
        {
            string path2 = Guid.NewGuid().ToString() + (item == null ? "" : item.Extension);
            FileInfo file1 = new FileInfo(Path.Combine(PtfkFileInfo.GetDirectoryInfo(session).FullName, path2));
            File.WriteAllBytes(file1.FullName, obj);
            IPtfkSession session1 = session;
            PtfkFileInfo file2 = new PtfkFileInfo();
            file2.FileInfo = file1;
            file2.OwnerID = session.Login;
            file2.UID = ID.ToString();
            file2.EntityName = entity.GetType().Name;
            file2.ParentID = ID;
            file2.Name = file1.Name;
            file2.Hash = item == null ? Tools.GetHashMD5(file1) : item.Hash;
            file2.MediaID = item == null ? 0 : item.Id;
            file2.EntityProperty = propertyName;
            ILogger logger = entity.Logger;
            PtfkFileInfo.AddFile(session1, file2, logger);
        }

        private static Boolean IsNewFile(PtfkFileInfo file)
        {
            long lID = -1;
            long.TryParse(file.UID, out lID);
            if (lID < 0)//Novo arquivo
                return true;
            return false;
        }

        //internal static long SaveMedia(IPtfkMedia media, IPtfkSession session)
        //{
        //    try
        //    {
        //        var instance = Activator.CreateInstance(BusinessClassMedia.GetType(), new object[] { session });

        //        var toClass = Activator.CreateInstance(BusinessClassMedia.GetType().BaseType.GetGenericArguments()[0]);

        //        foreach (var prop in media.GetType().GetProperties())
        //        {
        //            var val = prop.GetValue(media);
        //            if (val != null && prop.SetMethod != null)
        //                toClass.GetType().GetProperty(prop.Name).SetValue(toClass, val);
        //        }

        //        var mediaSaved = BusinessClassMedia.GetType().GetMethod(nameof(IPtfkBusiness<object>.Save)).Invoke(instance, new object[] { toClass }) as System.Threading.Tasks.Task;
        //        mediaSaved.Wait();
        //        return (toClass as IPtfkEntity).Id;
        //    }
        //    catch (Exception ex)
        //    {
        //        return 0;
        //    }
        //}

        internal static long SaveMedia(IPtfkForm entity, IPtfkMedia media, IPtfkSession session)
        {
            try
            {
                object instance = Activator.CreateInstance(PtfkFileInfo.BusinessClassMedia.GetType(), session);
                object toClass = Activator.CreateInstance(PtfkFileInfo.BusinessClassMedia.GetType().BaseType.GetGenericArguments()[0]);
                foreach (var prop in media.GetType().GetProperties())
                {
                    object val = prop.GetValue(media);
                    if (val != null && prop.SetMethod != (MethodInfo)null)
                        toClass.GetType().GetProperty(prop.Name).SetValue(toClass, val);
                }
                IPtfkMedia ptfkMedia = (((IEnumerable<MethodInfo>)PtfkFileInfo.BusinessClassMedia.GetType().GetMethods()).Where<MethodInfo>((Func<MethodInfo, bool>)(method => method.Name == nameof(IPtfkBusiness<object>.List) && ((IEnumerable<ParameterInfo>)method.GetParameters()).Count<ParameterInfo>() == 0)).FirstOrDefault<MethodInfo>().Invoke(instance, (object[])null) as IQueryable<IPtfkMedia>).Where<IPtfkMedia>((System.Linq.Expressions.Expression<Func<IPtfkMedia, bool>>)(m => m.Hash.Equals(media.Hash) && m.EntityId.Equals(media.EntityId) && m.EntityName.Equals(media.EntityName) && m.EntityProperty.Equals(media.EntityProperty))).FirstOrDefault<IPtfkMedia>();
                if (ptfkMedia != null && ptfkMedia.Id > 0)
                {
                    switch (StorageMode)
                    {
                        case StorageMode.OnContentDB:
                            //var filePath = GetStorageRelativeFilePath(entity, media.EntityProperty, media);
                            var file = new FileInfo(GetStorageAbsoluteFilePath(entity, media.EntityProperty, media, true));

                            if (!file.Directory.Exists)
                                file.Directory.Create();

                            if (media.Bytes != null && !file.FullName.EndsWith(ptfkMedia.Path))
                            {
                                File.WriteAllBytes(file.FullName, media.Bytes);

                                ptfkMedia.Bytes = null;
                                ptfkMedia.Path = file.FullName.Substring(file.FullName.IndexOf(GetStorageFolderPath(entity, media.EntityProperty)));
                            }

                            break;
                        default:
                            break;
                    }

                    ptfkMedia.IsDeath = null;
                    toClass = ptfkMedia;
                }
                else
                {
                    {
                        switch (StorageMode)
                        {
                            case StorageMode.OnContentDB:

                                IPtfkMedia iMedia = toClass as IPtfkMedia;

                                //var filePath = GetStorageRelativeFilePath(entity, media.EntityProperty, media);
                                var file = new FileInfo(GetStorageAbsoluteFilePath(entity, iMedia.EntityProperty, iMedia, true));

                                if (!file.Directory.Exists)
                                    file.Directory.Create();

                                if (iMedia.Bytes != null)
                                {
                                    File.WriteAllBytes(file.FullName, iMedia.Bytes);

                                    iMedia.Bytes = null;
                                    iMedia.Path = file.FullName.Substring(file.FullName.IndexOf(GetStorageFolderPath(entity, iMedia.EntityProperty)));
                                }

                                break;
                            default:
                                break;
                        }
                    }
                }
              (PtfkFileInfo.BusinessClassMedia.GetType().GetMethod(nameof(IPtfkBusiness<object>.Save)).Invoke(instance, new object[1] { toClass }) as Task).Wait();
                return (toClass as IPtfkEntity).Id;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        internal static void RefreshFile(IPtfkSession session, FileInfo file, FileInfo newLocation)
        {
            foreach (PtfkFileInfo ptfkFileInfo in ((IEnumerable<PtfkFileInfo>)PtfkFileInfo.Get(session)).Where<PtfkFileInfo>((Func<PtfkFileInfo, bool>)(x => x.FileInfo.FullName.ToLower().Equals(file.FullName.ToLower()))))
                ptfkFileInfo.FileInfo = new FileInfo(newLocation.FullName);
        }

        internal static async void InactivateMedias(IPtfkSession session, long entityID, string entityName, params long[] ActiveIDs)
        {
            try
            {
                //Not inactivate if ActiveIDs is empty
                if (ActiveIDs.Count() == 0)
                    return;

                var instance = Activator.CreateInstance(BusinessClassMedia.GetType(), new object[] { session });
                var method = BusinessClassMedia.GetType().GetMethods().Where(x => x.Name.Equals(nameof(IPtfkBusiness<object>.List)) && x.GetParameters().Length == 0).FirstOrDefault();

                var list = method.Invoke(instance, null) as IQueryable<IPtfkMedia>;
                var all = list.Where(x => x != null && !String.IsNullOrEmpty(x.EntityName) && x.EntityId.Equals(entityID) && x.EntityName.Equals(entityName)).ToList();
                var medias = (from m in all
                              where m.EntityId.Equals(entityID) && m.EntityName.Equals(entityName) &&
                              string.IsNullOrWhiteSpace(m.IsDeath) &&
                              !ActiveIDs.Contains(m.Id)
                              select m).ToList();
                var type = BusinessClassMedia.GetType().BaseType.GetGenericArguments()[0];
                foreach (var item in medias)
                {
                    item.IsDeath = "1";
                    await (BusinessClassMedia.GetType().GetMethod(nameof(IPtfkBusiness<object>.Update)).Invoke(instance, new object[] { item }) as System.Threading.Tasks.Task);
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
