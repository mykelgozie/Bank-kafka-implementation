using System.Linq.Expressions;

namespace Bank.Application.Interface
{
    public interface IGenericRepository<T> where T : class
    {
            Task AddAsync(T entity);
            void Update(T entity);
            void Delete(T entity);
        Task<IQueryable<T>> GetByAsync(Expression<Func<T, bool>> expression);
        Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> expression);
    }
}
