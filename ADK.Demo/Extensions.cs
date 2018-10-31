﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace ADK.Demo
{
    public static class Extensions
    {
        /// <summary>Splits an enumeration based on a predicate.</summary>
        /// <remarks>
        /// This method drops partitioning elements.
        /// </remarks>
        public static IEnumerable<IEnumerable<TSource>> Split<TSource>(
            this IEnumerable<TSource> source,
            Func<TSource, bool> partitionBy,
            bool removeEmptyEntries = false,
            int count = -1)
        {
            int yielded = 0;
            var items = new List<TSource>();
            foreach (var item in source)
            {
                if (!partitionBy(item))
                    items.Add(item);
                else if (!removeEmptyEntries || items.Count > 0)
                {
                    yield return items.ToArray();
                    items.Clear();

                    if (count > 0 && ++yielded == count) yield break;
                }
            }

            if (items.Count > 0) yield return items.ToArray();
        }

        public static IEnumerable<T> GetColumn<T>(this T[,] matrix, int columnNumber)
        {
            return Enumerable.Range(0, matrix.GetLength(0))
                    .Select(x => matrix[x, columnNumber]);
        }

        public static IEnumerable<T> GetRow<T>(this T[,] matrix, int rowNumber)
        {
            return Enumerable.Range(0, matrix.GetLength(1))
                    .Select(x => matrix[rowNumber, x]);
        }
    }
}
