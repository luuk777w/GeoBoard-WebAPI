using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using GeoBoardWebAPI.DAL.Entities;

namespace GeoBoardWebAPI.DAL.Repositories
{
    public interface IRepository<T> where T : class, IAppEntity
    {
        T Find<TKey>(TKey id);
        Task<T> FindAsync<TKey>(TKey id);
        IQueryable<T> GetAll();
        void Add(T entity);
        Task AddAsync(T entity);
        int SaveChanges();
        Task<int> SaveChangesAsync();
        void AddRange(params T[] entities);
        Task AddRangeAsync(params T[] entities);
        void Update(T entity);
        void Remove(T entity);
        void RemoveRange(params T[] entities);
        Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        T FirstOrDefault(Expression<Func<T, bool>> predicate);
        EntityEntry<T> Attach(T entity);
        CollectionEntry<T, TProperty> Collection<TProperty>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression) where TProperty : class;
        CollectionEntry Collection<TProperty>(T entity, string propertyName) where TProperty : class;
        ReferenceEntry<T, TProperty> Reference<TProperty>(T entity, Expression<Func<T, TProperty>> propertyExpression) where TProperty : class;
        ReferenceEntry Reference<TProperty>(T entity, string propertyName) where TProperty : class;
        void AddOrUpdate(T entity);
        Task AddOrUpdateAsync(T entity);
    }

    public abstract class Repository<T> : IRepository<T> where T : class, IAppEntity
    {
        protected readonly ApplicationDbContext Context;
        protected DbSet<T> DbSet;

        public Repository(ApplicationDbContext context)
        {
            Context = context;
            DbSet = context.Set<T>();
        }

        public async Task AddOrUpdateAsync(T entity)
        {
            if (Context.Entry(entity) != null && Context.Entry(entity).State != EntityState.Added && Context.Entry(entity).State != EntityState.Detached)
            {
                Context.Entry(entity).State = EntityState.Modified;
            }
            else
            {
                entity.CreatedAt = DateTime.UtcNow;
                Context.Entry(entity).State = EntityState.Added;
            }
            await SaveChangesAsync();
        }

        public void AddOrUpdate(T entity)
        {
            if (Context.Entry(entity) != null && Context.Entry(entity).State != EntityState.Added && Context.Entry(entity).State != EntityState.Detached)
            {
                Context.Entry(entity).State = EntityState.Modified;
            }
            else
            {
                entity.CreatedAt = DateTime.UtcNow;
                Context.Entry(entity).State = EntityState.Added;
            }
            SaveChanges();
        }

        public void Add(T entity)
        {
            entity.CreatedAt = DateTime.UtcNow;
            Context.Set<T>().Add(entity);
        }

        public async Task AddAsync(T entity)
        {
            entity.CreatedAt = DateTime.UtcNow;
            await Context.Set<T>().AddAsync(entity);
        }

        public void AddRange(params T[] entities)
        {
            Context.Set<T>().AddRange(entities);
        }

        public async Task AddRangeAsync(params T[] entities)
        {
            await Context.Set<T>().AddRangeAsync(entities);
        }

        public T Find<TKey>(TKey id)
        {
            return DbSet.Find(id);
        }

        public async Task<T> FindAsync<TKey>(TKey id)
        {
            return await DbSet.FindAsync(id);
        }

        public IQueryable<T> GetAll()
        {
            return DbSet;
        }

        public void Update(T entity)
        {
            Context.Entry(entity).State = EntityState.Modified;
        }

        public void Remove(T entity)
        {
            Context.Set<T>().Remove(entity);
        }

        public void RemoveRange(params T[] entities)
        {
            Context.Set<T>().RemoveRange(entities);
        }

        public int SaveChanges()
        {
            return Context.SaveChanges();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await Context.SaveChangesAsync();
        }

        public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await Context.Set<T>().FirstOrDefaultAsync(predicate);
        }

        public T FirstOrDefault(Expression<Func<T, bool>> predicate)
        {
            return Context.Set<T>().FirstOrDefault(predicate);
        }

        public virtual CollectionEntry<T, TProperty> Collection<TProperty>(T entity, Expression<Func<T, IEnumerable<TProperty>>> propertyExpression) where TProperty : class
        {
            return Context.Entry(entity).Collection(propertyExpression);
        }

        public virtual CollectionEntry Collection<TProperty>(T entity, string propertyName) where TProperty : class
        {
            return Context.Entry(entity).Collection(propertyName);
        }

        public virtual ReferenceEntry<T, TProperty> Reference<TProperty>(T entity, Expression<Func<T, TProperty>> propertyExpression) where TProperty : class
        {
            return Context.Entry(entity).Reference(propertyExpression);
        }

        public virtual ReferenceEntry Reference<TProperty>(T entity, string propertyName) where TProperty : class
        {
            return Context.Entry(entity).Reference(propertyName);
        }


        public virtual T FindOneBy(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            return DbSet.FirstOrDefault(predicate);
        }

        public virtual Task<T> FindOneByAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            return DbSet.FirstOrDefaultAsync(predicate);
        }

        public virtual IQueryable<T> FindBy(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            return DbSet.Where(predicate);
        }

        public virtual bool Any(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            return DbSet.Any(predicate);
        }

        public virtual Task<bool> AnyAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        {
            return DbSet.AnyAsync(predicate);
        }

        public EntityEntry<T> Attach(T entity)
        {
            return Context.Set<T>().Attach(entity);
        }
    }
}
