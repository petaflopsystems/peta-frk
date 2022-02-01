using Microsoft.AspNetCore.Http;
using Petaframework.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using PetaframeworkStd.Interfaces;
using System.Linq;

namespace Petaframework.Strict
{
    public class UserRepository
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private static System.Collections.Generic.List<IPtfkSession> HotCache;
        private static System.Collections.Generic.List<KeyValuePair<IPtfkSession, DateTime>> TimeChecker;

        public static System.Collections.Generic.List<IPtfkSession> List()
        {
            if (UserRepository.HotCache == null || UserRepository.HotCache.Count() == 0)
                UserRepository.HotCache = ConfigurationManager.ReadOrWriteUsers();
            return UserRepository.HotCache;
        }

        public static void Save(params IPtfkSession[] usersList)
        {
            UserRepository.HotCache = ConfigurationManager.ReadOrWriteUsers(((IEnumerable<IPtfkSession>)usersList).ToArray());
        }

        public static DateTime LastUpdate()
        {
            return ConfigurationManager.LastWriteUsersUpdate();
        }

        /// <summary>
        /// Check the last day that this session was updated
        /// </summary>
        /// <param name="session">Session to check</param>
        /// <returns>Last time that this session was updated</returns>
        public static DateTime LastUpdate(IPtfkSession session)
        {

            if (TimeChecker == null)
            {
                TimeChecker = new List<KeyValuePair<IPtfkSession, DateTime>>();
                TimeChecker.Add(new KeyValuePair<IPtfkSession, DateTime>(session, DateTime.Now));
            }
            else
            {
                var n = TimeChecker.Where(x => x.Key.Login.Equals(session.Login)).FirstOrDefault();
                if (String.IsNullOrWhiteSpace(session.Login))
                    TimeChecker.Add(new KeyValuePair<IPtfkSession, DateTime>(session, DateTime.Now));
                else
                {
                    if ((DateTime.Now - n.Value).Days > 1)
                    {
                        TimeChecker.Remove(n);
                        TimeChecker.Add(new KeyValuePair<IPtfkSession, DateTime>(session, DateTime.Now));
                    }
                    else
                    {
                        return n.Value;
                    }
                }
            }
            return DateTime.MinValue;
        }
    }
}