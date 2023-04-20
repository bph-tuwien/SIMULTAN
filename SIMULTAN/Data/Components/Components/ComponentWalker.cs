using System;
using System.Collections.Generic;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Provides methods to traverse the component hierarchy
    /// </summary>
    public class ComponentWalker
    {
        /// <summary>
        /// Returns an enumerable containing all parameters in the subtree
        /// </summary>
        /// <param name="component">The root component of the subtree</param>
        /// <returns>An IEnumerable containing all parameters in the subtree</returns>
        public static IEnumerable<SimBaseParameter> GetFlatParameters(SimComponent component)
        {
            return BreadthFirstTraversal(component, x => x.Parameters);
        }
        /// <summary>
        /// Returns an enumerable containing all parameters in the subtree with a certain T type
        /// </summary>
        /// <typeparam name="T">the type of the SimBaseParameter<typeparamref name="T"/></typeparam>
        /// <param name="component">The root component of the subtree</param>
        /// <returns>An IEnumerable containing all parameters in the subtree</returns>
        public static IEnumerable<T> GetFlatParameters<T>(SimComponent component) where T : SimBaseParameter
        {
            return BreadthFirstTraversal<T>(component, x => x.Parameters);
        }
        /// <summary>
        /// Performs a breath first traversal of all components in a subtree and returns a selectable list.
        /// </summary>
        /// <typeparam name="T">The type of the result elements</typeparam>
        /// <param name="component">The root component of the subtree</param>
        /// <param name="itemSelector">Selects the result items from each component</param>
        /// <param name="visitComponent">Only components for which the predicate returns True are visited</param>
        /// <returns></returns>
        public static IEnumerable<T> BreadthFirstTraversal<T>(SimComponent component,
            Func<SimComponent, IEnumerable<SimBaseParameter>> itemSelector, Predicate<SimComponent> visitComponent = null) where T : SimBaseParameter
        {
            Queue<SimComponent> q = new Queue<SimComponent>();
            q.Enqueue(component);

            while (q.Count != 0)
            {
                var c = q.Dequeue();
                foreach (var item in itemSelector(c))
                    yield return item as T;

                foreach (var subComponent in c.Components)
                    if (subComponent.Component != null && (visitComponent == null || visitComponent(subComponent.Component)))
                        q.Enqueue(subComponent.Component);
            }
        }

        /// <summary>
        /// Performs a breath first traversal of all components in a subtree and returns a selectable list.
        /// </summary>
        /// <param name="component">The root component of the subtree</param>
        /// <param name="itemSelector">Selects the result items from each component</param>
        /// <param name="visitComponent">Only components for which the predicate returns True are visited</param>
        /// <returns></returns>
        public static IEnumerable<SimBaseParameter> BreadthFirstTraversal(SimComponent component,
            Func<SimComponent, IEnumerable<SimBaseParameter>> itemSelector, Predicate<SimComponent> visitComponent = null)
        {
            Queue<SimComponent> q = new Queue<SimComponent>();
            q.Enqueue(component);

            while (q.Count != 0)
            {
                var c = q.Dequeue();
                foreach (var item in itemSelector(c))
                    yield return item;

                foreach (var subComponent in c.Components)
                    if (subComponent.Component != null && (visitComponent == null || visitComponent(subComponent.Component)))
                        q.Enqueue(subComponent.Component);
            }
        }

        /// <summary>
        /// Iterates over all components and performs an action. The operation is called recursively on child components
        /// </summary>
        /// <param name="components">The component collection to iterate over</param>
        /// <param name="action">The action which should be performed</param>
        public static void ForeachComponent(SimComponentCollection components, Action<SimComponent> action)
        {
            foreach (var comp in components)
                ForeachComponent(comp, action);
        }
        /// <summary>
        /// Iterates over the components and all children and performs an action. The operation is called recursively on child components
        /// </summary>
        /// <param name="component">The component to start with</param>
        /// <param name="action">The action which should be performed</param>
        public static void ForeachComponent(SimComponent component, Action<SimComponent> action)
        {
            if (component != null)
            {
                action(component);

                foreach (var subComponent in component.Components)
                    ForeachComponent(subComponent.Component, action);

            }
        }

        /// <summary>
        /// Returns an IEnumerable containing all parents of the component
        /// </summary>
        /// <param name="component">The component for which the parents should be returned</param>
        /// <param name="includeSelf">When set to True, the component is contained as first element.</param>
        /// <returns>An IEnumerable containing all parents of the component</returns>
        public static IEnumerable<SimComponent> GetParents(SimComponent component, bool includeSelf = true)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            if (includeSelf)
                yield return component;

            var p = component;
            while (p.Parent != null)
            {
                p = p.Parent;
                yield return p;
            }
        }


        /// <summary>
        /// Searches in the components children for the first matching component
        /// </summary>
        /// <param name="components">A list of child components</param>
        /// <param name="selector">A function to test each component for a condition</param>
        /// <returns></returns>
        public static SimComponent FirstOrDefault(SimComponent.SimChildComponentCollection components, Predicate<SimComponent> selector)
        {
            foreach (var comp in components)
            {
                if (comp != null)
                {
                    var result = FirstOrDefault(comp.Component, selector);
                    if (result != null)
                        return result;
                }
            }

            return null;
        }
        /// <summary>
        /// Searches in the component and all children for the first matching component
        /// </summary>
        /// <param name="component">The root component</param>
        /// <param name="selector">A function to test each component for a condition</param>
        /// <returns></returns>
        public static SimComponent FirstOrDefault(SimComponent component, Predicate<SimComponent> selector)
        {
            if (component != null)
            {
                if (selector(component))
                    return component;

                var childResult = FirstOrDefault(component.Components, selector);
                if (childResult != null)
                    return childResult;
            }

            return null;
        }

        /// <summary>
        /// Returns True when the component and all it's child components fulfill the predicate
        /// </summary>
        /// <param name="component">The root component</param>
        /// <param name="predicate">The predicate</param>
        /// <returns>True when the component and all it's child components fulfill the predicate</returns>
        public static bool All(SimComponent component, Predicate<SimComponent> predicate)
        {
            bool all = true;

            ForeachComponent(component, x => all &= predicate(x));

            return all;
        }

        /// <summary>
        /// Returns True when any of the component or it's child components fulfill the predicate
        /// </summary>
        /// <param name="component">The root component</param>
        /// <param name="predicate">The predicate</param>
        /// <returns>True when any of the component or it's child components fulfill the predicate</returns>
        public static bool Any(SimComponent component, Predicate<SimComponent> predicate)
        {
            bool any = false;

            ForeachComponent(component, x => any |= predicate(x));

            return any;
        }
    }
}
