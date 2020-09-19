using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace EntityFrameworkCore.Repository
{
    public class RepoContext<TRelation, TContext> : IContextRepository<TRelation> where TContext : DbContext
    {
        private readonly TContext _context;
        private IList<KeyValuePair<Type, LambdaExpression>> _includes = new List<KeyValuePair<Type, LambdaExpression>>();
        private bool _enableSaveChanges = true;

        public RepoContext(TContext context)
        {
            _context = context;
            _context.ChangeTracker.LazyLoadingEnabled = false;
        }

        public async Task<T> Add<T>(T item) where T : class
        {
            var result = (await _context.Set<T>().AddAsync(item)).Entity;

            if (_enableSaveChanges)
            {
                await _context.SaveChangesAsync();
            }

            return result;
        }

        public async Task<IEnumerable<T>> Add<T>(IEnumerable<T> range) where T : class
        {
            await _context.Set<T>().AddRangeAsync(range);

            if (_enableSaveChanges)
            {
                await _context.SaveChangesAsync();
            }

            return range;
        }

        public async Task<T> GetFirst<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return await WithQuery<T, T>(q => q.FirstOrDefaultAsync(predicate));
        }

        public Task<IEnumerable<T>> GetWhere<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return WithQuery<T, IEnumerable<T>>(async q => await q.Where(predicate).ToListAsync());
        }

        public async Task Delete<T>(T item) where T : class
        {
            _context.Set<T>().Remove(item);

            if (_enableSaveChanges)
            {
                await _context.SaveChangesAsync();
            }
        }

        public async Task Delete<T>(IEnumerable<T> range) where T : class
        {
            _context.Set<T>().RemoveRange(range);

            if (_enableSaveChanges)
            {
                await _context.SaveChangesAsync();
            }
        }

        public async Task Delete<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            var set = _context.Set<T>();

            set.RemoveRange(set.AsNoTracking().Where(predicate));

            if (_enableSaveChanges)
            {
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsAny<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return await WithQuery<T, bool>(q => q.AnyAsync(predicate));
        }

        public IContextRepository<TRelation> Include<T, T2>(Expression<Func<T, T2>> path) where T : class
        {
            _includes.Add(new KeyValuePair<Type, LambdaExpression>(typeof(T), path));

            return this;
        }

        public async Task<T2> Transaction<T2>(Func<IContextRepository<TRelation>, Task<T2>> trans)
        {
            try
            {
                _enableSaveChanges = false;

                var result = await trans(this);

                _enableSaveChanges = true;

                await _context.SaveChangesAsync();

                return result;
            }
            finally
            {
                _enableSaveChanges = false;
            }
        }

        private Task<T2> WithQuery<T, T2>(Func<IQueryable<T>, Task<T2>> act) where T : class
        {
            var dbset = _context.Set<T>().AsNoTracking();

            Func<Type, Type> ParameterType = (t) => t.IsGenericType ? t.GenericTypeArguments[0] : t;

            var bottomLevelIncludes = _includes.Where(x => _includes.All(y => ParameterType(y.Value.Body.Type) != x.Value.ReturnType));

            foreach (var inc in bottomLevelIncludes)
            {
                dbset = ApplyIncludeChain(dbset, inc);
            }

            var result = act(dbset);

            _includes.Clear();

            return result;
        }

        private IQueryable<T> ApplyIncludeChain<T>(IQueryable<T> query, KeyValuePair<Type, LambdaExpression> inc) where T : class
        {
            Func<Type, Type> ParameterType = (t) => t.IsGenericType ? t.GenericTypeArguments[0] : t;
            var nextLevelUp = _includes.Where(x => ParameterType(x.Value.ReturnType) == inc.Value.Parameters[0].Type);
            var result = query;

            if (nextLevelUp.Any())
            {
                foreach (var n in nextLevelUp)
                {
                    result = ApplyIncludeChain(query, n);

                    var thenIncludeMethod = (from o in typeof(EntityFrameworkQueryableExtensions).GetMethods()
                                             where o.Name == "ThenInclude" &&
                                             o.IsGenericMethod
                                             let parameters = o.GetParameters()
                                             where parameters.Length == 2
                                             && parameters[1].ParameterType.IsGenericType
                                             select o).Skip(1).Single().MakeGenericMethod(typeof(T), inc.Value.Parameters[0].Type, inc.Value.ReturnType);

                    result = (IQueryable<T>)thenIncludeMethod.Invoke(null, new object[] { result, inc.Value });
                }
            }
            else
            {
                var includeMethod = (from o in typeof(EntityFrameworkQueryableExtensions).GetMethods()
                                     where o.Name == "Include" &&
                                     o.IsGenericMethod
                                     let parameters = o.GetParameters()
                                     where parameters.Length == 2
                                     && parameters[1].ParameterType.IsGenericType
                                     select o).Single().MakeGenericMethod(ParameterType(inc.Value.Type), inc.Value.ReturnType);

                result = (IQueryable<T>)includeMethod.Invoke(null, new object[] { query, inc.Value });
            }

            return result;
        }
    }
}
