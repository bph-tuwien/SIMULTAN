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
        /// Creates a traversable hierarchy with levels for association with, e.g., Excel mapping rules.
        /// </summary>
        /// <param name="_comp">the calling component</param>
        /// <param name="list">key = level of found component, value = found sub- or referenced component</param>
        /// <param name="list_control">key = level of found component, value = key of the found sub- or referenced component</param>
        /// <param name="list_is_subcomp">key = level of found component, value = true if the path is along the subcomponents, false otherwise</param>
        /// <param name="_is_subcomponent">if the recursion is along the subcomponents = true, otherwise false</param>
        /// <param name="_level">the current level in the component subtree hierarchy (root = 0)</param>
        /// <param name="_level_ref">the current level in the component reference hierarchy (root = 0)</param>
        /// <param name="_max_level">maximal admissible level in the subtree</param>
        /// <param name="_max_level_ref">maximal admissible level in the reference hierarchy</param>
        /// <param name="_max_found">the threshold for found elements</param>
        public static bool GetFlatSubAndRefCompListWLevels(this SimComponent _comp, ref List<KeyValuePair<int, SimComponent>> list, ref List<KeyValuePair<int, SimId>> list_control,
                                                                            ref List<KeyValuePair<int, bool>> list_is_subcomp, bool _is_subcomponent,
                                                                            int _level, int _level_ref, int _max_level = 100, int _max_level_ref = 20, long _max_found = long.MaxValue)
        {
            if (list == null)
                list = new List<KeyValuePair<int, SimComponent>>();
            if (list_control == null)
                list_control = new List<KeyValuePair<int, SimId>>();
            if (list_is_subcomp == null)
                list_is_subcomp = new List<KeyValuePair<int, bool>>();

            // cut-off: 
            double currently_found = list.Count;
            if (currently_found >= _max_found)
                return false;
            if (_is_subcomponent && _level > _max_level)
                return true;
            if (!_is_subcomponent && _level_ref > _max_level_ref)
                return true;

            // avoid loops: newer
            if (list.Count > 2)
            {
                bool loop_detected = HasRepeatsOfSize(list_control, 2, 3);

                if (loop_detected)
                {
                    return true;
                }
            }

            list.Add(new KeyValuePair<int, SimComponent>(_level + _level_ref, _comp));
            list_control.Add(new KeyValuePair<int, SimId>(_level + _level_ref, _comp.Id));
            list_is_subcomp.Add(new KeyValuePair<int, bool>(_level + _level_ref, _is_subcomponent));

            if ((_comp.Components.Count < 1) &&
                (_comp.ReferencedComponents == null || _comp.ReferencedComponents.Count < 1))
                return true;

            _level++;
            //int next_level_sub = _level;
            foreach (var n in _comp.Components)
            {
                if (n.Component == null) continue;
                bool found_all = n.Component.GetFlatSubAndRefCompListWLevels(ref list, ref list_control, ref list_is_subcomp, true, _level, _level_ref, _max_level, _max_level_ref, _max_found);
                if (!found_all)
                    return false;
            }
            _level_ref++;
            //int next_level_ref = _level;
            SortedList<string, SimComponent> copy_rComps = new SortedList<string, SimComponent>(_comp.ReferencedComponents.Where(x => x.Target != null).ToDictionary(x => x.Slot.ToSerializerString(), x => x.Target));
            foreach (SimComponent n in copy_rComps.Values)
            {
                if (n == null) continue;
                bool found_all = n.GetFlatSubAndRefCompListWLevels(ref list, ref list_control, ref list_is_subcomp, false, _level, _level_ref, _max_level, _max_level_ref, _max_found);
                if (!found_all)
                    return false;
            }

            return true;
        }

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

        #region QUERIES: Geometry

        /// <summary>
        /// Performs a recursive search of the calling component's hierarchy and 
        /// returns a flat list of all components associated with a geometry id.
        /// </summary>
        /// <param name="_comp">the calling component</param>
        /// <returns>a collection of components associated with geometry</returns>
        public static IEnumerable<SimComponent> GetAllAssociatedWithGeometry(this SimComponent _comp)
        {
            List<SimComponent> found = new List<SimComponent>();
            if (_comp.InstanceType != SimInstanceType.None && _comp.Instances.Any(inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry)))
            {
                found.Add(_comp);
            }

            foreach (var entry in _comp.Components)
            {
                SimComponent sC = entry.Component;
                if (sC == null) continue;

                var sFound = sC.GetAllAssociatedWithGeometry();
                found.AddRange(sFound);
            }

            return found;
        }

        /// <summary>
        /// Returns all geometry ids from the given geometry file found in the instances of the component as a collection.
        /// If the file id entries in the geometric relationships are empty, they have been realized by the old geometry 
        /// viewer -> the file id will be set to the one provided by '_from_file_with_id'.
        /// </summary>
        /// <param name="_comp">the calling component</param>
        /// <param name="_from_file_with_id">the file id in the ressource manager, where the currently loaded geometry resides</param>
        /// <returns>a collection of ulong ids</returns>
        public static IEnumerable<ulong> GetAllAssociatedGeometryIds(this SimComponent _comp, int _from_file_with_id)
        {
            var matchingPlacements = PlacementsMatchingFile(_comp, _from_file_with_id);
            if (matchingPlacements.Count == 0)
            {
                foreach (var gr in _comp.Instances)
                {
                    foreach (var placement in gr.Placements)
                        if (placement is SimInstancePlacementGeometry gp && gp.FileId == -1)
                            gp.FileId = _from_file_with_id;
                }

                matchingPlacements = PlacementsMatchingFile(_comp, _from_file_with_id);
            }

            return matchingPlacements.Select(x => x.GeometryId).ToList();
        }

        private static List<SimInstancePlacementGeometry> PlacementsMatchingFile(SimComponent component, int fileId)
        {
            List<SimInstancePlacementGeometry> result = new List<SimInstancePlacementGeometry>();

            foreach (var inst in component.Instances)
            {
                foreach (var placement in inst.Placements)
                {
                    if (placement is SimInstancePlacementGeometry pg && pg.FileId == fileId)
                        result.Add(pg);
                }
            }

            return result;
        }

        #endregion

        #region Utils

        private static bool HasRepeatsOfSize<T1, T2>(IList<KeyValuePair<T1, T2>> _sequence, int _size, int _min_nr_matches) where T1 : IComparable<T1>
        {
            if (_sequence == null) return false;
            IList<KeyValuePair<T1, T2>> last_branch = ExtractLastMonotoneBranch(_sequence);

            if (last_branch.Count < 2) return false;
            bool detected = HasRepeatsInMonotoneBranchOfSize(last_branch, _size, _min_nr_matches);
            if (detected)
                return true;

            return false;
        }

        private static bool HasRepeatsInMonotoneBranchOfSize<T1, T2>(IList<KeyValuePair<T1, T2>> _sequence, int _size, int _min_nr_matches)
        {
            if (_sequence == null) return false;

            for (int i = 0; i < _sequence.Count - _size; i++)
            {
                int nr_matches = 1;
                List<KeyValuePair<T1, T2>> pattern = _sequence.Skip(i).Take(_size).ToList();
                for (int j = i + 1; j < _sequence.Count - _size; j++)
                {
                    List<KeyValuePair<T1, T2>> test = _sequence.Skip(j).Take(_size).ToList();
                    bool match_found = test.Zip(pattern, (t, p) => t.Value.Equals(p.Value)).Aggregate((x, y) => x & y);
                    if (match_found)
                    {
                        nr_matches++;
                        if (nr_matches >= _min_nr_matches)
                            return true;
                    }
                }
            }

            return false;
        }

        private static IList<KeyValuePair<T1, T2>> ExtractLastMonotoneBranch<T1, T2>(IList<KeyValuePair<T1, T2>> _sequence) where T1 : IComparable<T1>
        {
            IList<KeyValuePair<T1, T2>> branch = new List<KeyValuePair<T1, T2>>();
            if (_sequence == null) return branch;
            if (_sequence.Count < 2) return branch;


            int seq_length = _sequence.Count;
            T1 next_key = _sequence[seq_length - 1].Key;
            branch.Add(_sequence[seq_length - 1]);

            for (int i = seq_length - 2; i >= 0; i--)
            {
                T1 current_key = _sequence[i].Key;
                if (current_key.CompareTo(next_key) < 0)
                {
                    branch.Add(_sequence[i]);
                    next_key = _sequence[i].Key;
                }
            }

            branch = branch.Reverse().ToList();
            return branch;
        }

        #endregion
    }
}
