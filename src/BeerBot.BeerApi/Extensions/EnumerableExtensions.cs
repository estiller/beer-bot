using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BeerBot.BeerApi.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<TSource> EmptyIfNoFilters<TSource>(this IEnumerable<TSource> source, params ICollection[] filters)
        {
            return filters.All(c => c.Count == 0) ? Enumerable.Empty<TSource>() : source;
        }

        public static IEnumerable<TSource> FilterBy<TSource, TFilter>(this IEnumerable<TSource> source, ICollection<TFilter> filters, Func<TSource, TFilter> selector)
        {
            return filters.Count == 0 ? source : source.Where(item => filters.Contains(selector(item)));
        }

        public static IEnumerable<TSource> FilterByCaseInsensitive<TSource>(this IEnumerable<TSource> source, ICollection<string> filters, Func<TSource, string> selector)
        {
            return filters.Count == 0 ? source : source.Where(item => filters.Contains(selector(item), StringComparer.OrdinalIgnoreCase));
        }

        public static IEnumerable<TSource> FilterBySearchTerms<TSource>(this IEnumerable<TSource> source, ICollection<string> searchTerms, Func<TSource, string> selector)
        {
            return searchTerms.Count == 0 ? source : source.Where(item => searchTerms.Any(searchTerm => selector(item).ToLower().Contains(searchTerm.ToLower())));
        }

        public static IEnumerable<TSource> FilterByRange<TSource>(this IEnumerable<TSource> source, float? min, float? max, Func<TSource, float?> selector)
        {
            if (min.HasValue)
                source = source.Where(item => selector(item) >= min.Value);
            if (max.HasValue)
                source = source.Where(item => selector(item) <= max.Value);
            return source;
        }
    }
}