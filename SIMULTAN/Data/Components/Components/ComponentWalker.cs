using SIMULTAN.Data.Geometry;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Provides methods to traverse the component hierarchy
    /// </summary>
    public static class ComponentWalker
    {
        #region Obsolete

        /// <summary>
        /// Returns an enumerable containing all parameters in the subtree with a certain T type
        /// </summary>
        /// <typeparam name="T">the type of the SimBaseParameter<typeparamref name="T"/></typeparam>
        /// <param name="component">The root component of the subtree</param>
        /// <returns>An IEnumerable containing all parameters in the subtree</returns>
        [Obsolete]
        public static IEnumerable<T> GetFlatParameters<T>(SimComponent component) where T : SimBaseParameter
        {
            return GetFlatParameters_BreadthFirstTraversal<T>(component, x => x.Parameters);
        }

        /// <summary>
        /// Performs a breath first traversal of all components in a subtree and returns a selectable list.
        /// </summary>
        /// <typeparam name="T">The type of the result elements</typeparam>
        /// <param name="component">The root component of the subtree</param>
        /// <param name="itemSelector">Selects the result items from each component</param>
        /// <param name="visitComponent">Only components for which the predicate returns True are visited</param>
        /// <returns></returns>
        [Obsolete]
        private static IEnumerable<T> GetFlatParameters_BreadthFirstTraversal<T>(SimComponent component,
            Func<SimComponent, IEnumerable<SimBaseParameter>> itemSelector, Predicate<SimComponent> visitComponent = null) where T : class
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

        #endregion

        /// <summary>
        /// Returns an enumerable containing all parameters in the subtree
        /// </summary>
        /// <param name="component">The root component of the subtree</param>
        /// <returns>An IEnumerable containing all parameters in the subtree</returns>
        public static IEnumerable<SimBaseParameter> GetFlatParameters(SimComponent component)
        {
            return BreadthFirstTraversalMany<SimBaseParameter>(component, x => x.Parameters);
        }

        /// <summary>
        /// Performs a breath first traversal of all components in a subtree and returns a selectable list.
        /// </summary>
        /// <param name="component">The root component of the subtree</param>
        /// <param name="itemSelector">Selects the result items from each component</param>
        /// <param name="visitComponent">Only components for which the predicate returns True are visited</param>
        /// <returns>The items selected by the itemSelector for all components that match the visitComponent predicate</returns>
        public static IEnumerable<T> BreadthFirstTraversalMany<T>(SimComponent component,
            Func<SimComponent, IEnumerable<T>> itemSelector, Predicate<SimComponent> visitComponent = null)
        {
            Queue<SimComponent> q = new Queue<SimComponent>();
            q.Enqueue(component);

            return BreadthFirstTraversalManyInternal(q, itemSelector, visitComponent);
        }
        /// <summary>
        /// Performs a breath first traversal of all components in a collection and returns a selectable list.
        /// </summary>
        /// <param name="components">The root components of the subtree</param>
        /// <param name="itemSelector">Selects the result items from each component</param>
        /// <param name="visitComponent">Only components for which the predicate returns True are visited</param>
        /// <returns>The items selected by the itemSelector for all components that match the visitComponent predicate</returns>
        public static IEnumerable<T> BreadthFirstTraversalMany<T>(SimComponentCollection components,
            Func<SimComponent, IEnumerable<T>> itemSelector, Predicate<SimComponent> visitComponent = null)
        {
            Queue<SimComponent> q = new Queue<SimComponent>();
            components.ForEach(c => q.Enqueue(c));

            return BreadthFirstTraversalManyInternal(q, itemSelector, visitComponent);
        }
        /// <summary>
        /// Performs a breath first traversal of all components in a collection and returns a selectable list.
        /// </summary>
        /// <param name="components">The root components of the subtree</param>
        /// <param name="itemSelector">Selects the result items from each component</param>
        /// <param name="visitComponent">Only components for which the predicate returns True are visited</param>
        /// <returns>The items selected by the itemSelector for all components that match the visitComponent predicate</returns>
        public static IEnumerable<T> BreadthFirstTraversalMany<T>(SimComponent.SimChildComponentCollection components,
            Func<SimComponent, IEnumerable<T>> itemSelector, Predicate<SimComponent> visitComponent = null)
        {
            Queue<SimComponent> q = new Queue<SimComponent>();
            components.Where(x => x.Component != null).ForEach(c => q.Enqueue(c.Component));

            return BreadthFirstTraversalManyInternal(q, itemSelector, visitComponent);
        }
        private static IEnumerable<T> BreadthFirstTraversalManyInternal<T>(Queue<SimComponent> q,
            Func<SimComponent, IEnumerable<T>> itemSelector, Predicate<SimComponent> visitComponent = null)
        {
            while (q.Count != 0)
            {
                var c = q.Dequeue();
                if (visitComponent == null || visitComponent(c))
                    foreach (var item in itemSelector(c))
                        yield return item;

                foreach (var subComponent in c.Components)
                    if (subComponent.Component != null)
                        q.Enqueue(subComponent.Component);
            }
        }

        /// <summary>
        /// Performs a breath first traversal of all components in a collection and returns a selectable list.
        /// </summary>
        /// <param name="component">The root component of the subtree</param>
        /// <param name="itemSelector">Selects the result item from each component</param>
        /// <param name="visitComponent">Only components for which the predicate returns True are visited</param>
        /// <returns>The items selected by the itemSelector for all components that match the visitComponent predicate</returns>
        public static IEnumerable<T> BreadthFirstTraversal<T>(SimComponent component,
            Func<SimComponent, T> itemSelector, Predicate<SimComponent> visitComponent = null)
        {
            Queue<SimComponent> q = new Queue<SimComponent>();
            q.Enqueue(component);

            return BreadthFirstTraversalInternal(q, itemSelector, visitComponent);
        }
        /// <summary>
        /// Performs a breath first traversal of all components in a collection and returns a selectable list.
        /// </summary>
        /// <param name="components">The root components of the subtree</param>
        /// <param name="itemSelector">Selects the result item from each component</param>
        /// <param name="visitComponent">Only components for which the predicate returns True are visited</param>
        /// <returns>The items selected by the itemSelector for all components that match the visitComponent predicate</returns>
        public static IEnumerable<T> BreadthFirstTraversal<T>(SimComponentCollection components,
            Func<SimComponent, T> itemSelector, Predicate<SimComponent> visitComponent = null)
        {
            Queue<SimComponent> q = new Queue<SimComponent>();
            components.ForEach(c => q.Enqueue(c));

            return BreadthFirstTraversalInternal(q, itemSelector, visitComponent);
        }
        /// <summary>
        /// Performs a breath first traversal of all components in a collection and returns a selectable list.
        /// </summary>
        /// <param name="components">The root components of the subtree</param>
        /// <param name="itemSelector">Selects the result item from each component</param>
        /// <param name="visitComponent">Only components for which the predicate returns True are visited</param>
        /// <returns>The items selected by the itemSelector for all components that match the visitComponent predicate</returns>
        public static IEnumerable<T> BreadthFirstTraversal<T>(SimComponent.SimChildComponentCollection components,
            Func<SimComponent, T> itemSelector, Predicate<SimComponent> visitComponent = null)
        {
            Queue<SimComponent> q = new Queue<SimComponent>();
            components.Where(x => x.Component != null).ForEach(c => q.Enqueue(c.Component));

            return BreadthFirstTraversalInternal(q, itemSelector, visitComponent);
        }
        private static IEnumerable<T> BreadthFirstTraversalInternal<T>(Queue<SimComponent> q,
            Func<SimComponent, T> itemSelector, Predicate<SimComponent> visitComponent = null)
        {
            while (q.Count != 0)
            {
                var c = q.Dequeue();
                if (visitComponent == null || visitComponent(c))
                    yield return itemSelector(c);

                foreach (var subComponent in c.Components)
                    if (subComponent.Component != null)
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
        /// Searches in the components for the first matching component
        /// </summary>
        /// <param name="components">A list of child components</param>
        /// <param name="selector">A function to test each component for a condition</param>
        /// <returns>The first component that matches the predicate</returns>
        public static SimComponent FirstOrDefault(SimComponentCollection components, Predicate<SimComponent> selector)
        {
            return BreadthFirstTraversal(components, x => x, selector).FirstOrDefault();
        }
        /// <summary>
        /// Searches in the components children for the first matching component
        /// </summary>
        /// <param name="components">A list of child components</param>
        /// <param name="selector">A function to test each component for a condition</param>
        /// <returns>The first component in the subtree that matches the predicate</returns>
        public static SimComponent FirstOrDefault(SimComponent.SimChildComponentCollection components, Predicate<SimComponent> selector)
        {
            return BreadthFirstTraversal(components, x => x, selector).FirstOrDefault();
        }
        /// <summary>
        /// Searches in the component and all children for the first matching component
        /// </summary>
        /// <param name="component">The root component</param>
        /// <param name="selector">A function to test each component for a condition</param>
        /// <returns></returns>
        public static SimComponent FirstOrDefault(SimComponent component, Predicate<SimComponent> selector)
        {
            return BreadthFirstTraversal(component, x => x, selector).FirstOrDefault();
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

        /// <summary>
        /// Returns all components that match the predicate
        /// </summary>
        /// <param name="components">The root component collection</param>
        /// <param name="predicate">A function to test each component for a condition.</param>
        /// <returns>The components that match the predicate</returns>
        public static IEnumerable<SimComponent> Where(SimComponentCollection components, Predicate<SimComponent> predicate)
        {
            return BreadthFirstTraversal(components, x => x, predicate);
        }
        /// <summary>
        /// Returns all components that match the predicate
        /// </summary>
        /// <param name="component">The root component</param>
        /// <param name="predicate">A function to test each component for a condition.</param>
        /// <returns>The components that match the predicate</returns>
        public static IEnumerable<SimComponent> Where(SimComponent component, Predicate<SimComponent> predicate)
        {
            return BreadthFirstTraversal(component, x => x, predicate);
        }
        /// <summary>
        /// Returns all components that match the predicate
        /// </summary>
        /// <param name="components">The root component collection</param>
        /// <param name="predicate">A function to test each component for a condition.</param>
        /// <returns>The components that match the predicate</returns>
        public static IEnumerable<SimComponent> Where(SimComponent.SimChildComponentCollection components, Predicate<SimComponent> predicate)
        {
            return BreadthFirstTraversal(components, x => x, predicate);
        }

        /// <summary>
        /// Tries to find the instance value of the given parameter and geometry.
        /// </summary>
        /// <param name="parameter">The parameter to get the instance value of</param>
        /// <param name="geometry">The geometry to get the instance value for</param>
        /// <returns>The persistant instance value of the parameter and geometry. Null if not found</returns>
        /// <exception cref="ArgumentNullException">If an argument is null</exception>
        public static object GetInstanceValue(this SimBaseParameter parameter, BaseGeometry geometry)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));

            var instance = parameter.Component.GetInstance(geometry);
            if (instance == null)
                return null;

            return instance.InstanceParameterValuesPersistent[parameter];
        }

        /// <summary>
        /// Tries to find the instance of the given component and geometry.
        /// </summary>
        /// <param name="component">The component</param>
        /// <param name="geometry">The geometry</param>
        /// <returns>The instance, or null if not found.</returns>
        /// <exception cref="ArgumentNullException">If an argument is null</exception>
        public static SimComponentInstance GetInstance(this SimComponent component, BaseGeometry geometry)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));

            var fileId = geometry.ModelGeometry.Model.File.Key;
            var instance = component.Instances.FirstOrDefault(c =>
                c.Placements.OfType<SimInstancePlacementGeometry>().Any(x => x.FileId == fileId && x.GeometryId == geometry.Id));

            return instance;
        }
    }
}
