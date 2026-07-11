using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query;

namespace BiometricPushServer.Tests.TestHelpers
{
    /// <summary>
    /// Wraps an in-memory list as an IQueryable that supports EF Core async operations
    /// (CountAsync, ToListAsync, etc.) in unit tests — without requiring a real database.
    /// </summary>
    internal class TestAsyncQueryable<T> : IOrderedQueryable<T>, IAsyncEnumerable<T>
    {
        private readonly IQueryable<T> _inner;

        public TestAsyncQueryable(IEnumerable<T> source)
        {
            _inner = source.AsQueryable();
            Provider = new TestAsyncQueryProvider<T>(_inner.Provider);
            Expression = _inner.Expression;
        }

        public Type ElementType => _inner.ElementType;
        public Expression Expression { get; }
        public IQueryProvider Provider { get; }

        public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new TestAsyncEnumerator<T>(_inner.GetEnumerator());
    }

    internal class TestAsyncQueryProvider<T> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

        public IQueryable CreateQuery(Expression expression) => _inner.CreateQuery(expression);

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            var innerQuery = _inner.CreateQuery<TElement>(expression);
            return new TestAsyncQueryable<TElement>(innerQuery);
        }

        public object? Execute(Expression expression) => _inner.Execute(expression);

        public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var resultType = typeof(TResult).GetGenericArguments().FirstOrDefault() ?? typeof(TResult);

            // Call the generic Execute<TResult>(Expression) overload
            var executeMethod = typeof(IQueryProvider)
                .GetMethods()
                .Single(m => m.Name == nameof(IQueryProvider.Execute) && m.IsGenericMethod)
                .MakeGenericMethod(resultType);

            var result = executeMethod.Invoke(_inner, new object[] { expression });

            var taskMethod = typeof(Task)
                .GetMethods()
                .Single(m => m.Name == nameof(Task.FromResult) && m.IsGenericMethod)
                .MakeGenericMethod(resultType);

            return (TResult)taskMethod.Invoke(null, new[] { result })!;
        }
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        internal TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;

        public T Current => _inner.Current;

        public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(_inner.MoveNext());

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    internal static class AsyncQueryableExtensions
    {
        /// <summary>Returns a queryable backed by a list that supports EF Core async operations.</summary>
        public static IQueryable<T> AsAsyncQueryable<T>(this IEnumerable<T> source) =>
            new TestAsyncQueryable<T>(source);
    }
}
