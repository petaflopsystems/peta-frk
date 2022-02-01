using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Text;

namespace Petaframework.Strict
{
    public class MongoDbContextModel : IModel
    {
        public object this[string name] => Constants.MongoDBName;

        public IAnnotation FindAnnotation(string name)
        {
            throw new NotImplementedException();
        }

        public IEntityType FindEntityType(string name)
        {
            throw new NotImplementedException();
        }

        public IEntityType FindEntityType(string name, string definingNavigationName, IEntityType definingEntityType)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IAnnotation> GetAnnotations()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IEntityType> GetEntityTypes()
        {
            throw new NotImplementedException();
        }
    }

    public class MongoDBConnection
    {
        private string StringifyConnection { get; set; }

        public string ConnectionString { get; set; }
        public string Database { get; set; }
        public bool IsSSL { get; set; }

        public MongoDBConnection() { }

        public MongoDBConnection(string stringifyConnection)
        {
            this.StringifyConnection = stringifyConnection;
            var mConn = Tools.FromJson<MongoDBConnection>(stringifyConnection);
            this.ConnectionString = mConn.ConnectionString;
            this.Database = mConn.Database;
            this.IsSSL = mConn.IsSSL;
        }
    }
}
