using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace EntityFrameworkCore.Repository
{
    public interface IRepoContext<TContext> where TContext: DbContext
    {
        IRepoContext<TContext> Include<T, T2>(Expression<Func<T, T2>> path) where T : class;
        Task<bool> IsAny<T>(Expression<Func<T, bool>> predicate) where T : class;
        Task<T> GetFirst<T>(Expression<Func<T, bool>> predicate) where T : class;
        Task<IEnumerable<T>> GetWhere<T>(Expression<Func<T, bool>> predicate) where T : class;
    }
}
