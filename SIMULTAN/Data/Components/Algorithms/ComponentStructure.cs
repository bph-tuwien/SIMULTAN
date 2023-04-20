using System;
using System.Collections.Generic;
using System.Linq;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Traversal of the component tree for Excel mapping. 
    /// Queries for parent chains, referenced components or subcomponents with certain types of <see cref="SimComponentInstance"/>.
    /// Queries of Properties via Reflection. Methods for managing and reacting to disconnected geometry.
    /// </summary>
    public static class ComponentStructure
    {
        #region TRAVERSAL

        /// <summary>
        /// Find the minimal set of components derivable from the given collection (e.g. if one component
        /// is a subcomponent of another, remove it from the collection).
        /// </summary>
        /// <param name="_comps">the components whose common forest we are looking for</param>
        /// <returns></returns>
        public static List<SimComponent> FindMinimalForestOf(IEnumerable<SimComponent> _comps)
        {
            List<SimComponent> comps = _comps.Distinct().ToList();
            // e.g. the following parent chains of components B, D, F and Z
            // A B C D
            // A B C H
            // A B F
            // X Y Z W
            // should result in:
            // B
            // Z

            int nr_comps = comps.Count;
            List<SimComponent> forest = new List<SimComponent>(comps);
            for (int i = 0; i < nr_comps; i++)
            {
                for (int j = i + 1; j < nr_comps; j++)
                {
                    if (comps[i].IsDirectOrIndirectChildOf(comps[j]))
                        forest.Remove(comps[i]);
                    else if (comps[j].IsDirectOrIndirectChildOf(comps[i]))
                        forest.Remove(comps[j]);
                }
            }

            return forest;
        }

        #endregion

        #region QUERIES: Parents

        /// <summary>
        /// Checks if the calling component is a sub-component (of a sub-component...) of the 
        /// given potential parent component.
        /// </summary>
        /// <param name="_comp">the calling component</param>
        /// <param name="_potential_parent">the potential parent component</param>
        /// <returns>true, if the calling component is a direct o indirect child of the potential parent</returns>
        public static bool IsDirectOrIndirectChildOf(this SimComponent _comp, SimComponent _potential_parent)
        {
            if (_potential_parent == null) return false;

            return ComponentWalker.GetParents(_comp).Contains(_potential_parent);
        }

        #endregion
    }
}
