using System;
using System.Collections.Generic;
using System.Linq;

namespace BeerBot.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> Random<T>(this IEnumerable<T> source, int numOfItems)
        {
            var sourceList = source.ToList();
            numOfItems = Math.Min(numOfItems, sourceList.Count);
            var selectedItems = new HashSet<int>();
            for (int i = 0; i < numOfItems; i++)
            {
                int random = GetUniqueRandomNumber(sourceList.Count, selectedItems);
                yield return sourceList[random];
            }
        }

        private static int GetUniqueRandomNumber(int maxValue, HashSet<int> selectedItems)
        {
            int random;
            do
            {
                random = GetRandomNumber(maxValue);
            } while (selectedItems.Contains(random));
            selectedItems.Add(random);
            return random;
        }

        private static readonly Random RandomGenerator = new Random();

        private static int GetRandomNumber(int maxValue)
        {
            lock (RandomGenerator)
            {
                return RandomGenerator.Next(maxValue);
            }
        }
    }
}