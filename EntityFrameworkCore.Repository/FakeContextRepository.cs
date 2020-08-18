using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace EntityFrameworkCore.Repository
{
    internal class FakeContextRepository<TRelation> : IContextRepository<TRelation>
    {
        public IDictionary<Type, object> DataSource { get; set; } = new Dictionary<Type, object>();

        public Task<T> GetFirst<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return Task.FromResult(DataSource.ContainsKey(typeof(T)) ? 
                (DataSource[typeof(T)] as List<T>).AsQueryable().FirstOrDefault(predicate) : null);
        }

        public Task<IEnumerable<T>> GetWhere<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return Task.FromResult(DataSource.ContainsKey(typeof(T)) ?
                (DataSource[typeof(T)] as List<T>).AsQueryable().Where(predicate).AsEnumerable() : new List<T>().AsEnumerable());
        }

        public IContextRepository<TRelation> Include<T, T2>(Expression<Func<T, T2>> path) where T : class
        {
            return this;
        }        

        public Task<bool> IsAny<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return Task.FromResult(DataSource.ContainsKey(typeof(T)) ?
                (DataSource[typeof(T)] as List<T>).AsQueryable().Any(predicate) : false);
        }
    }
}
