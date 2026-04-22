using System.Collections.Generic;
using UnityEngine;

namespace Utility.Extensions
{
    /// <summary>
    /// GameObject utility functions
    /// </summary>
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Finds all components of type in self and children
        /// </summary>
        /// <param name="self"><see cref="UnityEngine.Component"/> to look</param>
        /// <param name="depth">
        /// child layers to include relative to <see cref="self"/>.
        /// 0 = self only (same as Unity's GetComponent), 1 = self + children, etc.
        /// </param>
        /// <param name="includeInactive">whether to include inactive child GameObjects in the search</param>
        /// <typeparam name="T">type of component to find</typeparam>
        /// <returns>list of components found</returns>
        public static List<T> GetComponentsInSelfAndChildrenRecursive<T>(this Component self, int depth = int.MaxValue, bool includeInactive = false)
            where T : class
        {
            if (depth < 0)
            {
                return new List<T>();
            }

            List<T> components = new List<T>();

            components.AddRange(self.GetComponents<T>());
            components.AddRange(self.GetComponentsInChildrenRecursive<T>(depth: depth - 1, includeInactive: includeInactive));

            return components;
        }

        /// <summary>
        /// Finds all components of type in self and children
        /// </summary>
        /// <param name="self"><see cref="UnityEngine.GameObject"/> to look</param>
        /// <param name="depth">
        /// child layers to include relative to <see cref="self"/>.
        /// 0 = self only (same as Unity's GetComponent), 1 = self + children, etc.
        /// </param>
        /// <param name="includeInactive">whether to include inactive child GameObjects in the search</param>
        /// <typeparam name="T">type of component to find</typeparam>
        /// <returns>list of components found</returns>
        public static List<T> GetComponentsInSelfAndChildrenRecursive<T>(this GameObject self, int depth = int.MaxValue, bool includeInactive = false)
            where T : class
        {
            return self.transform.GetComponentsInSelfAndChildrenRecursive<T>(depth: depth, includeInactive: includeInactive);
        }

        /// <summary>
        /// Finds all components of type in children
        /// </summary>
        /// <param name="parent">parent <see cref="UnityEngine.Component"/> to look</param>
        /// <param name="depth">
        /// child layers to include relative to <see cref="parent"/>.
        /// 0 = no layers, 1 = direct children, etc.
        /// </param>
        /// <param name="includeInactive">whether to include inactive child GameObjects in the search</param>
        /// <typeparam name="T">type of component to find</typeparam>
        /// <returns>list of components found</returns>
        public static List<T> GetComponentsInChildrenRecursive<T>(this Component parent, int depth = int.MaxValue, bool includeInactive = false)
            where T : class
        {
            if (depth <= 0)
            {
                return new List<T>();
            }

            List<T> components = new List<T>();

            foreach (Transform child in parent.transform)
            {
                if (!includeInactive && !child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                components.AddRange(child.GetComponents<T>());
                components.AddRange(child.GetComponentsInChildrenRecursive<T>(depth: depth - 1, includeInactive: includeInactive));
            }

            return components;
        }

        /// <summary>
        /// Finds all components of type in children
        /// </summary>
        /// <param name="parent">parent <see cref="UnityEngine.GameObject"/> to look</param>
        /// <param name="depth">
        /// child layers to include relative to <see cref="parent"/>.
        /// 0 = no layers, 1 = direct children, etc.
        /// </param>
        /// <param name="includeInactive">whether to include inactive child GameObjects in the search</param>
        /// <typeparam name="T">type of component to find</typeparam>
        /// <returns>list of components found</returns>
        public static List<T> GetComponentsInChildrenRecursive<T>(this GameObject parent, int depth = int.MaxValue, bool includeInactive = false)
            where T : class
        {
            return parent.transform.GetComponentsInChildrenRecursive<T>(depth: depth, includeInactive: includeInactive);
        }

        /// <summary>
        /// Finds all components of type in children, excluding specified type.
        /// </summary>
        /// <param name="parent">parent <see cref="UnityEngine.Component"/> to look</param>
        /// <typeparam name="T">type of component to find</typeparam>
        /// <typeparam name="TExclude">type of component to exclude</typeparam>
        /// <returns>list of components found</returns>
        public static List<T> GetComponentsInChildrenExcluding<T, TExclude>(this Component parent)
            where T : class
            where TExclude : class
        {
            var results = new List<T>();
            Traverse<T, TExclude>(parent.transform, results);
            return results;
        }

        /// <summary>
        /// Finds all components of type in children, excluding specified type.
        /// </summary>
        /// <param name="parent">parent <see cref="UnityEngine.GameObject"/> to look</param>
        /// <typeparam name="T">type of component to find</typeparam>
        /// <typeparam name="TExclude">type of component to exclude</typeparam>
        /// <returns>list of components found</returns>
        public static List<T> GetComponentsInChildrenExcluding<T, TExclude>(this GameObject parent)
            where T : class
            where TExclude : class
        {
            return parent.transform.GetComponentsInChildrenExcluding<T, TExclude>();
        }

        private static void Traverse<T, TExclude>(Transform node, List<T> results)
            where T : class
            where TExclude : class
        {
            foreach (Transform child in node)
            {
                if (child.GetComponent<T>() is { } component)
                    results.Add(component);

                if (child.GetComponent<TExclude>() == null)
                    Traverse<T, TExclude>(child, results);
            }
        }
    }
}
