using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Petaframework.Strict
{
    public class MongoDBContext : Petaframework.Interfaces.IPtfkDbContext, IMongoDbContext
    {
        public MongoDBContext(MongoDBConnection mongoDBConnection)
        {
            try
            {
                MongoClientSettings settings = MongoClientSettings.FromUrl(new MongoUrl(mongoDBConnection.ConnectionString));
                if (mongoDBConnection.IsSSL)
                {
                    settings.SslSettings = new SslSettings { EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 };
                }
                var mongoClient = new MongoClient(settings);
                MongoDB = mongoClient.GetDatabase(mongoDBConnection.Database);
            }
            catch (Exception ex)
            {
                throw new Exception("Database connection fail!", ex);
            }
            currDatabase = new MongoDatabase(this);
        }
        private IMongoDatabase MongoDB { get; }
        internal IClientSessionHandle Session { get; set; }
        private MongoClient MongoClient { get; set; }

        private readonly List<Func<Task>> _commands;

        private readonly List<KeyValuePair<String, object>> InMemoryCollections = new List<KeyValuePair<string, object>>();

        private IMongoCollection<TEntity> InMemory<TEntity>(String name)
        {
            var o = InMemoryCollections.Where(x => x.Key.Equals(name));
            IMongoCollection<TEntity> DbSet = o.FirstOrDefault() as IMongoCollection<TEntity>;
            if (o.Count() == 0)
            {
                DbSet = MongoDB.GetCollection<TEntity>(name);
                InMemoryCollections.Add(new KeyValuePair<string, object>(name, DbSet));
            }
            return DbSet;
        }

        public IModel Model => new Petaframework.Strict.MongoDbContextModel();

        MongoDatabase currDatabase;
        public IDbContextTransaction Database => currDatabase;

        public bool IsInTransaction { get; internal set; }


        private void AddCommand(Func<Task> func)
        {
            _commands.Add(func);
        }

        public TEntity Add<TEntity>([NotNull] TEntity entity) where TEntity : class
        {
            var DbSet = InMemory<TEntity>(typeof(TEntity).Name);
            _commands.Add(() => DbSet.InsertOneAsync(entity));
            return entity;
        }

        public virtual TEntity Remove<TEntity>([NotNull] TEntity entity) where TEntity : class, PetaframeworkStd.Interfaces.IEntity
        {
            var DbSet = InMemory<TEntity>(typeof(TEntity).Name);
            _commands.Add(() => DbSet.DeleteOneAsync(Builders<TEntity>.Filter.Eq("_id", entity.Id)));
            return default;
        }

        public virtual int SaveChanges()
        {
            using (Session = MongoClient.StartSession())
            {
                if (!IsInTransaction)
                    Session.StartTransaction();

                var commandTasks = _commands.Select(c => c());

                Task.WhenAll(commandTasks).Wait();

                if (!IsInTransaction)
                    Session.CommitTransactionAsync().Wait();

            }
            return _commands.Count();
        }

        public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            using (Session = await MongoClient.StartSessionAsync(null, cancellationToken))
            {
                if (!IsInTransaction)
                    Session.StartTransaction();

                var commandTasks = _commands.Select(c => c());

                await Task.WhenAll(commandTasks);

                if (!IsInTransaction)
                    await Session.CommitTransactionAsync(cancellationToken);
            }

            return _commands.Count;
        }

        public virtual DbSet<TEntity> Set<TEntity>() where TEntity : class
        {
            var DbSet = InMemory<TEntity>(typeof(TEntity).Name);
            var all = DbSet.Find(Builders<TEntity>.Filter.Empty);
            var a = all.ToList();
            var b = new DbSet<TEntity>();
            b.AddRange(a);
            return b;
        }

        public virtual TEntity Update<TEntity>([NotNull] TEntity entity) where TEntity : class, PetaframeworkStd.Interfaces.IEntity
        {
            var DbSet = InMemory<TEntity>(typeof(TEntity).Name);
            _commands.Add(() => DbSet.ReplaceOneAsync(Builders<TEntity>.Filter.Eq("_id", entity.Id), entity));
            return entity;
        }

        public virtual DbSet<TEntity> Query<TEntity>() where TEntity : class
        {
            return Set<TEntity>();
        }
    }

    public class DbSet<TEntity> : List<TEntity>
    {
        public IQueryable<TEntity> AsNoTracking()
        {
            return this.AsQueryable();
        }
    }

    public class MongoDatabase : Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction
    {
        private MongoDBContext mongoDBContext;

        public MongoDatabase([NotNullAttribute] MongoDBContext mongoDBContext)
        {
            this.mongoDBContext = mongoDBContext;
        }

        public IDbContextTransaction BeginTransaction()
        {
            mongoDBContext.IsInTransaction = true;
            mongoDBContext.Session.StartTransaction();
            return this;
        }

        public Guid TransactionId => new Guid();

        public void Commit()
        {
            mongoDBContext?.SaveChanges();
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            await mongoDBContext?.SaveChangesAsync(cancellationToken);
        }

        public void Dispose()
        {
            mongoDBContext?.Session?.Dispose();
            GC.SuppressFinalize(this);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }

        public void Rollback()
        {
            mongoDBContext?.Session?.AbortTransaction();
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            await mongoDBContext?.Session?.AbortTransactionAsync(cancellationToken);
        }
    }

    public interface IMongoDbContext
    {
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction Database { get; }
        DbSet<TEntity> Set<TEntity>() where TEntity : class;
        DbSet<TEntity> Query<TEntity>() where TEntity : class;
        int SaveChanges();

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        TEntity Update<TEntity>([NotNullAttribute] TEntity entity) where TEntity : class, PetaframeworkStd.Interfaces.IEntity;

        TEntity Add<TEntity>([NotNullAttribute] TEntity entity) where TEntity : class;

        TEntity Remove<TEntity>([NotNullAttribute] TEntity entity) where TEntity : class, PetaframeworkStd.Interfaces.IEntity;

    }
}
