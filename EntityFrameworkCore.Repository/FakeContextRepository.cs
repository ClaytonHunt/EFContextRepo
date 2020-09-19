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

        public Task<T> Add<T>(T item) where T : class
        {
            if(DataSource.ContainsKey(typeof(T)))
            {
                var context = (DataSource[typeof(T)] as List<T>);
                
                context.Add(item);                              
            }
            else
            {
                DataSource.Add(typeof(T), new List<T> { item });
            }         
            
            return Task.FromResult(item);
        }

        public Task<IEnumerable<T>> Add<T>(IEnumerable<T> range) where T : class
        {
            if(DataSource.ContainsKey(typeof(T)))
            {
                var context = (DataSource[typeof(T)] as List<T>);
                foreach(var item in range)
                {
                    context.Add(item);
                }                
            }
            else
            {
                DataSource.Add(typeof(T), range.ToList());
            }         
            
            return Task.FromResult(range);
        }

        public Task Delete<T>(T item) where T : class
        {
            throw new NotImplementedException();
        }

        public Task Delete<T>(IEnumerable<T> range) where T : class
        {
            throw new NotImplementedException();
        }

        public Task Delete<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            throw new NotImplementedException();
        }

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

        public Task<T2> Transaction<T2>(Func<IContextRepository<TRelation>, Task<T2>> trans)
        {
            return trans(this);
        }
    }
}
