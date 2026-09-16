# Design Patterns & Architectural Blueprint

This document specifies the standard patterns and base contracts that must be used across the solution.

---

## 1. Domain Entities & Base Entity Pattern

All domain entities must inherit from `BaseEntity<TId>` or `AuditableEntity<TId>`.

### In `LexisCampusDMS.Core.Domain/Common/BaseEntity.cs`:
```csharp
namespace LexisCampusDMS.Core.Domain.Common
{
    public abstract class BaseEntity<TId>
    {
        public TId Id { get; set; } = default!;
    }

    public abstract class AuditableEntity<TId> : BaseEntity<TId>
    {
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? LastModifiedAtUtc { get; set; }
        public string? LastModifiedBy { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAtUtc { get; set; }
    }
}
```

---

## 2. Generic Repository Pattern (`IGenericRepository<TEntity, TId>`)

### In `LexisCampusDMS.Core.Domain/Interfaces/IGenericRepository.cs`:
```csharp
using System.Linq.Expressions;
using LexisCampusDMS.Core.Domain.Common;

namespace LexisCampusDMS.Core.Domain.Interfaces
{
    public interface IGenericRepository<TEntity, TId> where TEntity : BaseEntity<TId>
    {
        Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TEntity>> FindAsync(
            Expression<Func<TEntity, bool>> predicate, 
            CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<TEntity> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            Expression<Func<TEntity, bool>>? predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
        
        Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        void Update(TEntity entity);
        void Remove(TEntity entity);
        void RemoveRange(IEnumerable<TEntity> entities);
    }
}
```

### In `LexisCampusDMS.Infraestructure.Persistence/Repositories/GenericRepository.cs`:
```csharp
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using LexisCampusDMS.Core.Domain.Common;
using LexisCampusDMS.Core.Domain.Interfaces;

namespace LexisCampusDMS.Infraestructure.Persistence.Repositories
{
    public class GenericRepository<TEntity, TId> : IGenericRepository<TEntity, TId> where TEntity : BaseEntity<TId>
    {
        protected readonly DbContext _context;
        protected readonly DbSet<TEntity> _dbSet;

        public GenericRepository(DbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = context.Set<TEntity>();
        }

        public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FindAsync([id], cancellationToken);
        }

        public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
        }

        public virtual async Task<IReadOnlyList<TEntity>> FindAsync(
            Expression<Func<TEntity, bool>> predicate, 
            CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);
        }

        public virtual async Task<(IReadOnlyList<TEntity> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            Expression<Func<TEntity, bool>>? predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<TEntity> query = _dbSet.AsNoTracking();

            if (predicate != null)
                query = query.Where(predicate);

            var totalCount = await query.CountAsync(cancellationToken);

            if (orderBy != null)
                query = orderBy(query);

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public virtual async Task<bool> ExistsAsync(
            Expression<Func<TEntity, bool>> predicate, 
            CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(predicate, cancellationToken);
        }

        public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(entity, cancellationToken);
            return entity;
        }

        public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddRangeAsync(entities, cancellationToken);
        }

        public virtual void Update(TEntity entity)
        {
            _dbSet.Update(entity);
        }

        public virtual void Remove(TEntity entity)
        {
            _dbSet.Remove(entity);
        }

        public virtual void RemoveRange(IEnumerable<TEntity> entities)
        {
            _dbSet.RemoveRange(entities);
        }
    }
}
```

---

## 3. Unit of Work Pattern (`IUnitOfWork`)

### In `LexisCampusDMS.Core.Domain/Interfaces/IUnitOfWork.cs`:
```csharp
namespace LexisCampusDMS.Core.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<TEntity, TId> Repository<TEntity, TId>() where TEntity : Common.BaseEntity<TId>;
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}
```

---

## 4. Result Envelope Pattern (`Result<T>` & `PagedResult<T>`)

Services must return strongly-typed `Result<T>` envelopes rather than throwing exceptions for expected business failures.

### In `LexisCampusDMS.Application/Wrappers/Result.cs`:
```csharp
namespace LexisCampusDMS.Application.Wrappers
{
    public class Result
    {
        public bool Succeeded { get; protected set; }
        public string? Message { get; protected set; }
        public List<string> Errors { get; protected set; } = new();

        public static Result Success(string? message = null) => new() { Succeeded = true, Message = message };
        public static Result Failure(string error) => new() { Succeeded = false, Errors = new() { error } };
        public static Result Failure(IEnumerable<string> errors) => new() { Succeeded = false, Errors = errors.ToList() };
    }

    public class Result<T> : Result
    {
        public T? Data { get; private set; }

        public static Result<T> Success(T data, string? message = null) => 
            new() { Succeeded = true, Data = data, Message = message };
        public new static Result<T> Failure(string error) => 
            new() { Succeeded = false, Errors = new() { error } };
        public new static Result<T> Failure(IEnumerable<string> errors) => 
            new() { Succeeded = false, Errors = errors.ToList() };
    }

    public class PagedResult<T> : Result<IReadOnlyList<T>>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public static PagedResult<T> Create(IReadOnlyList<T> data, int totalCount, int pageNumber, int pageSize) =>
            new()
            {
                Succeeded = true,
                Data = data,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
    }
}
```

---

## 5. Generic Service Pattern (`IGenericService<TDto, TCreateDto, TUpdateDto, TId>`)

### In `LexisCampusDMS.Application/Interfaces/IGenericService.cs`:
```csharp
using LexisCampusDMS.Application.Wrappers;

namespace LexisCampusDMS.Application.Interfaces
{
    public interface IGenericService<TDto, TCreateDto, TUpdateDto, TId>
    {
        Task<Result<TDto>> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
        Task<Result<IReadOnlyList<TDto>>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<PagedResult<TDto>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<Result<TDto>> CreateAsync(TCreateDto createDto, CancellationToken cancellationToken = default);
        Task<Result<TDto>> UpdateAsync(TId id, TUpdateDto updateDto, CancellationToken cancellationToken = default);
        Task<Result<bool>> DeleteAsync(TId id, CancellationToken cancellationToken = default);
    }
}
```
