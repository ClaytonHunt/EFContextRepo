using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace EntityFrameworkCore.Repository
{
    public class RepoContext<TContext> : IRepoContext<TContext> where TContext: DbContext
    {
        private readonly DbContext _context;
        private IList<KeyValuePair<Type, LambdaExpression>> _includes = new List<KeyValuePair<Type, LambdaExpression>>();

        public RepoContext(TContext context)
        {
                _context = context;
        }        

        public IRepoContext<TContext> Include<T, T2>(Expression<Func<T, T2>> path) where T : class
        {
            _includes.Add(new KeyValuePair<Type, LambdaExpression>(typeof(T), path));

            return this;
        }

        public async Task<bool> IsAny<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return await WithQuery<T, bool>(q => q.AnyAsync(predicate));
        }

        public async Task<T> GetFirst<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return await WithQuery<T, T>(q => q.FirstOrDefaultAsync(predicate));
        }

        public Task<IEnumerable<T>> GetWhere<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return WithQuery<T, IEnumerable<T>>(async q => await q.Where(predicate).ToListAsync());
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

                    var includeMethods = (from o in typeof(EntityFrameworkQueryableExtensions).GetMethods()
                                             where o.Name == "ThenInclude" &&
                                             o.IsGenericMethod
                                             let parameters = o.GetParameters()
                                             where parameters.Length == 2
                                             && parameters[1].ParameterType.IsGenericType
                                             select o );
                                             
                    var thenIncludeMethod =  includeMethods.Skip(1).Single().MakeGenericMethod(typeof(T), inc.Value.Parameters[0].Type, inc.Value.ReturnType);

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
