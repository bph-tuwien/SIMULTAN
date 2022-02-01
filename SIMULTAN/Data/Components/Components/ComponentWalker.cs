using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public static IEnumerable<SimParameter> GetFlatParameters(SimComponent component)
        {
            return BreadthFirstTraversal(component, x => x.Parameters);
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
            Func<SimComponent, IEnumerable<T>> itemSelector, Predicate<SimComponent> visitComponent = null)
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
    }
}
