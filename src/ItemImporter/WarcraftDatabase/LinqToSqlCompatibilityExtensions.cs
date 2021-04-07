//<auto-generated>
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.EntityFrameworkCore;

namespace ValhallaLootList.ItemImporter.WarcraftDatabase
{
	public static class LinqToSqlCompatibilityExtensions
	{
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void InsertOnSubmit<T>(this DbSet<T> table, T entity) where T : class
		{
			table.Add(entity);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void InsertAllOnSubmit<T>(this DbSet<T> table, IEnumerable<T> entities) where T : class
		{
			table.AddRange(entities);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void DeleteOnSubmit<T>(this DbSet<T> table, T entity) where T : class
		{
			table.Remove(entity);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void DeleteAllOnSubmit<T>(this DbSet<T> table, IEnumerable<T> entities) where T : class
		{
			table.RemoveRange(entities);
		}
	}
}
