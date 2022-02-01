using System;
using System.Collections.Generic;

namespace Petaframework
{
    internal static class ErrorTable
    {
        /// <summary>
        /// It occurs when an Entity's business class is not informed. By default, all entities must have the business class informed in the body of the SetBusiness method implemented from the IPtfkEntity interface.
        /// </summary>
        public static void Err001()
        {
            throw PtfkException("001", new Exception("Err001 - Inform Extended Business Class of IBusiness<T>."));
        }

        /// <summary>
        /// Occurs when a value is passed to an Entity property that does not exist.
        /// </summary>
        /// <param name="propertyName">Property name</param>
        public static void Err002(string propertyName)
        {
            throw PtfkException("002", new NotImplementedException("Err002 - Property '" + propertyName + "' not found!"));
        }

        /// <summary>
        /// Occurs when an Entity property set to IsOption is not of the List&lt;ListItem&gt; data type.
        /// </summary>
        public static void Err003()
        {
            throw PtfkException("003", new Exception("Err003 - Properties with IsOption attribute can only be of type List<ListItem>"));
        }

        /// <summary>
        /// Occurs when a LINQ-related code exception is thrown.
        /// </summary>
        /// <param name="entityLocation">The location of the code where the exception occurs</param>
        /// <param name="rootError">Inner Exception Message</param>
        /// <param name="ex">Thrown exception</param>
        public static void Err004(string entityLocation, string rootError, Exception ex = null)
        {
            Exception e;
            if (ex != null)
                e = new Exception("Err004 - Error in Entity: " + entityLocation + " >> " + rootError, ex);
            else
                e = new Exception("Err004 - Error in Entity: " + entityLocation + " >> " + rootError);
            throw PtfkException("004", e);
        }

        /// <summary>
        /// Occurs when a log cannot be saved.
        /// </summary>
        /// <param name="ex">Exception thrown</param>
        public static void Err005(Exception ex)
        {
            throw PtfkException("005", ex);
        }

        /// <summary>
        /// Occurs when an IsPrimary attribute is not found on any Entity properties.
        /// </summary>
        /// <param name="entityName">Entity Name</param>
        public static void Err006(string entityName)
        {
            throw PtfkException("006", new KeyNotFoundException("Err006 - A property with class attribute 'PrimaryKey' at " + entityName + " entity has not finded. It's required!"));
        }

        /// <summary>
        /// It occurs when an Entity property has context-based loading(OptionsContext) and the name of the entity (EntityName) linked to that property is not informed.This exception is only thrown when the OptionsType is equal to RequestMode.ClientSide.
        /// </summary>
        /// <param name="propName">Entity Property Name</param>
        public static void Err007(string propName)
        {
            throw PtfkException("007", new KeyNotFoundException("Err007 - Attribute 'EntityName' of " + propName + " property not informed. It's required!"));
        }

        /// <summary>
        /// Occurs when instantiating a PtfkFormStruct object the Id property is not assigned.
        /// </summary>
        public static void Err008()
        {
            throw PtfkException("008", new Exception("Err008 - Primary Key Attribute [ID] not informed in entity!"));
        }

        /// <summary>
        /// Occurs when a Filter(Filter Object) is not assigned in the PtfkFormStruct class. By default, all table queries must contain a FilterObject.
        /// </summary>
        public static void Err009()
        {
            throw PtfkException("009", new NotImplementedException("Err009 - Need to inform the FilterObject property of dForm object!"));
        }

        /// <summary>
        /// Occurs when a media file could not be saved.
        /// </summary>
        public static void Err010()
        {
            throw PtfkException("010", new Exception("Err010 - Error on save media!"));
        }

        /// <summary>
        /// Occurs when access permission for a process task is not found. It is necessary to check if the task appears in the Permissions.json file or another permissions controller.
        /// </summary>
        /// <param name="task">Task name</param>
        /// <param name="process">Process name</param>
        public static void Err011(string task, string process)
        {
            throw PtfkException("011", new UnauthorizedAccessException(string.Format("Err011 - Workflow permissions not found to task [{0}] and process [{1}]!", task, process)));
        }

        /// <summary>
        /// It occurs when a user(IPtv Session) tries to access a process that does not have permission.
        /// </summary>
        /// <param name="user">User session</param>
        /// <param name="process">Process name</param>
        public static void Err012(string user, string process)
        {
            throw PtfkException("012", new UnauthorizedAccessException(String.Format("Err012 - Permission denied on process [{0}] to user [{1}]!", process, user)));
        }

        /// <summary>
        /// It occurs when the User (IPtfSession) does not have access permission on the proccess instance.
        /// </summary>
        /// <param name="user">User session</param>
        /// <param name="task">Task name</param>
        public static void Err013(PetaframeworkStd.Interfaces.IPtfkSession user, string task)
        {
            throw PtfkException("013", new UnauthorizedAccessException(String.Format("Err013 - Workflow permission denied on task [{0}] to user [{1}]!", task, user.Login)));
        }

        /// <summary>
        /// It occurs when it's not possible identify the user session (IPtfkSession).
        /// </summary>
        public static void Err014()
        {
            throw PtfkException("014", new UnauthorizedAccessException("Err014 - Permission denied!"));
        }
        /// <summary>
        /// It occurs when it was not possible to find the binary with the workflow classes (extensions of IPtfkWorkfow). The workflow project must have a reference to EntityframeworkCore.dll.
        /// </summary>
        public static void Err015()
        {
            throw PtfkException("015", new Exception("It hasn't possible found Workflow Assembly. Add " + nameof(Petaframework) + " assembly to Workflow project!"));
        }

        /// <summary>
        /// It occurs when, in a hierarchical process, the hierarchy of the user's department is not found in the user's session (IPtfkSession).
        /// </summary>
        /// <param name="session"></param>
        public static void Err016(PetaframeworkStd.Interfaces.IPtfkSession session)
        {
            throw PtfkException("016", new Exception("Departmental Hierarchy Not Found [" + session.Login + "]!"));
        }

        /// <summary>
        /// Occurs when the HtmlToPdfConverter.dll binary was not found in the published solution.
        /// </summary>
        public static void Err017()
        {
            throw PtfkException("017", new PtfkException("HtmlConverterToPdf not found!"));
        }

        /// <summary>
        /// Occurs when it is not possible to save a media from an Entity property.
        /// </summary>
        /// <param name="propName">property name</param>
        /// <param name="ex">Exception thrown</param>
        public static void Err018(String propName, Exception ex)
        {
            throw PtfkException("018", new Exception(String.Format("Error on save {0}.", propName), ex));
        }


        public static void Err019(String ptfkConfigName)
        {
            throw PtfkException("019", new Exception(String.Format("Ptfk class ({0}) not found. Set extenstion UsePetaframework of IApplicationBuilder configuration on startup", ptfkConfigName)));
        }

        private static PtfkException PtfkException(String code, Exception e)
        {
            var ex = new PtfkException(String.Format("Error code {0}", code), e);
            ex.Code = code;
            throw ex;
        }
    }
}
