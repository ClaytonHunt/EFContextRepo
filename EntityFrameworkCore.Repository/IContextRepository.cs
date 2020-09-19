using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace EntityFrameworkCore.Repository
{
    public interface IContextRepository<TRelation>
    {
        Task<T> Add<T>(T item) where T : class;
        Task<IEnumerable<T>> Add<T>(IEnumerable<T> range) where T : class;
        Task<T> GetFirst<T>(Expression<Func<T, bool>> predicate) where T : class;
        Task<IEnumerable<T>> GetWhere<T>(Expression<Func<T, bool>> predicate) where T : class;
        Task Delete<T>(T item) where T : class;
        Task Delete<T>(IEnumerable<T> range) where T : class;
        Task Delete<T>(Expression<Func<T, bool>> predicate) where T : class;
        Task<bool> IsAny<T>(Expression<Func<T, bool>> predicate) where T : class;
        IContextRepository<TRelation> Include<T, T2>(Expression<Func<T, T2>> path) where T : class;
        Task<T2> Transaction<T2>(Func<IContextRepository<TRelation>, Task<T2>> trans);
    }
}
