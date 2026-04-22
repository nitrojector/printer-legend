using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utility.Extensions
{
    public static class ListExtensions
    {
        /// <summary>
        /// Shuffles the list in place using the Fisher-Yates algorithm.
        /// </summary>
        /// <param name="list">list to shuffle</param>
        public static void Shuffle<T>(this List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>
        /// Returns a shuffled copy of the list, leaving the original list unchanged.
        /// </summary>
        /// <param name="list">list to shuffle</param>
        /// <returns>shuffled list</returns>
        public static List<T> Shuffled<T>(this List<T> list)
        {
            var copy = new List<T>(list);
            copy.Shuffle();
            return copy;
        }
    }
}
