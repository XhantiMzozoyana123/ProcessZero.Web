using Microsoft.EntityFrameworkCore;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Infrastructure.Services
{
    /// <summary>
    /// Implements a generic data extraction service that iterates over all entity types
    /// registered in the ApplicationDbContext and returns their data as a dictionary
    /// of table names mapped to lists of entity objects.
    /// </summary>
    public class DataLakeService : IDataLakeService
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes a new instance of the DataLakeService with the provided database context.
        /// </summary>
        /// <param name="context">The application database context used to access entity sets.</param>
        public DataLakeService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Extracts all rows from all database tables registered in the ApplicationDbContext.
        /// The returned dictionary keys correspond to the database table names, and the values
        /// are the corresponding entity objects serialized as dynamic objects.
        /// </summary>
        /// <returns>A dictionary mapping table names to their extracted row data.</returns>
        public async Task<IDictionary<string, IEnumerable<object>>> ExtractAllTablesAsync()
        {
            var result = new Dictionary<string, IEnumerable<object>>();

            foreach (var entityType in _context.Model.GetEntityTypes())
            {
                var clrType = entityType.ClrType;

                // DbContext.Set<TEntity>()
                var setMethod = typeof(DbContext)
                    .GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!
                    .MakeGenericMethod(clrType);

                var dbSet = setMethod.Invoke(_context, null)!;

                // EntityFrameworkQueryableExtensions.ToListAsync<TEntity>()
                var toListAsyncMethod = typeof(EntityFrameworkQueryableExtensions)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .First(m =>
                        m.Name == nameof(EntityFrameworkQueryableExtensions.ToListAsync) &&
                        m.GetParameters().Length == 2)
                    .MakeGenericMethod(clrType);

                var task = (Task)toListAsyncMethod.Invoke(
                    null,
                    new object[] { dbSet, CancellationToken.None })!;

                await task;

                var resultProperty = task.GetType().GetProperty("Result")!;
                var entities = ((IEnumerable)resultProperty.GetValue(task)!)
                    .Cast<object>()
                    .ToList();

                result.Add(entityType.GetTableName()!, entities);
            }

            return result;
        }
    }
}
