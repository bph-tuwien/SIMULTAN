using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Projects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Legacy
{
    public static class RepairMissingValueFieldReferences
    {
        /// <summary>
        /// Saves the references from component parameters to value fields in a text file.
        /// </summary>
        /// <param name="_file">the file</param>
        /// <param name="components">the component manager of all relevant components</param>
        /// <param name="_verbose">if true, add information about the referenced objects</param>
        public static void ExportAllRelationshipsToValues(FileInfo _file, SimComponentCollection components, bool _verbose)
        {
            if (string.IsNullOrEmpty(_file.Extension))
                throw new ArgumentException("The file has no valid extension!");
            if (components == null)
                throw new ArgumentNullException(nameof(components));

            StringBuilder sb = new StringBuilder();
            var references = components.SelectMany(x => GetAllReferencesParameterMultiValueDetailed(x, _verbose));
            foreach (var r in references)
            {
                sb.AppendLine(r.Serialize());
                if (_verbose)
                    sb.AppendLine(r.SerializeInfo());
            }
            string content = sb.ToString();
            using (FileStream fs = File.Create(_file.FullName))
            {
                byte[] content_B = Encoding.UTF8.GetBytes(content);
                fs.Write(content_B, 0, content_B.Length);
            }
        }

        /// <summary>
        /// Imports the relationships between component parameters and values from a text file.
        /// </summary>
        /// <param name="_project">the project containing the components and values</param>
        /// <param name="_file">the text file</param>
        /// <param name="_verbose">if true, there are additional infos about the objects in the relationship</param>
        public static void ImportSomeRelationshipsToValues(HierarchicalProject _project, FileInfo _file, bool _verbose)
        {
            if (_project == null)
                throw new ArgumentNullException(nameof(_project));
            if (!File.Exists(_file.FullName))
                throw new ArgumentException("The given file does not exist!");

            List<ComponentToMultiValueReference> refs = new List<ComponentToMultiValueReference>();
            using (FileStream fs = new FileStream(_file.FullName, FileMode.Open))
            {
                using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                {
                    string line = null;
                    while ((line = sr.ReadLine()) != null)
                    {
                        var r = ComponentToMultiValueReference.Deserialize(line);
                        if (r != null)
                        {
                            if (_verbose)
                            {
                                var line_add = sr.ReadLine();
                                if (line_add != null)
                                {
                                    r.AddInfo(line_add);
                                }
                            }
                            refs.Add(r);
                        }
                    }
                }
            }

            if (_verbose)
                ReinstateReferencesDetailedWoIds(_project.AllProjectDataManagers.Components, _project.AllProjectDataManagers.ValueManager, refs);
            else
                ReinstateReferencesDetailed(_project.AllProjectDataManagers.ValueManager, refs);
        }


        /// <summary>
        /// Retrieve all references from parameters to value fields for the calling component. Includes
        /// all subcomponents and the <see cref="SimMultiValuePointer"/> data of the parameter.
        /// </summary>
        /// <param name="_comp">the calling component</param>
        /// <param name="_verbose">if true, include additional information</param>
        /// <returns>relationships in detail</returns>
        private static IEnumerable<ComponentToMultiValueReference> GetAllReferencesParameterMultiValueDetailed(SimComponent _comp, bool _verbose)
        {
            List<ComponentToMultiValueReference> references = new List<ComponentToMultiValueReference>();
            foreach (var entry in _comp.Parameters)
            {
                if (entry.MultiValuePointer == null)
                    continue;
                references.Add(GetReferenceFromPointer(entry.MultiValuePointer, _comp, _verbose));
            }
            foreach (var entry in _comp.Components)
            {
                if (entry.Component != null)
                {
                    IEnumerable<ComponentToMultiValueReference> sReferences = GetAllReferencesParameterMultiValueDetailed(entry.Component, _verbose);
                    references.AddRange(sReferences);
                }
            }
            return references;
        }


        /// <summary>
        /// Set the references between component parameters and the values according to
        /// the given references including the exact pointer coordinates by looking for ids.
        /// </summary>
        /// <param name="mvFactory">the MultiValue manager</param>
        /// <param name="references">the reference records</param>
        private static void ReinstateReferencesDetailed(SimMultiValueCollection mvFactory,
            IEnumerable<ComponentToMultiValueReference> references)
        {
            if (mvFactory == null || references == null) return;

            Dictionary<SimId, SimComponent> cache = new Dictionary<SimId, SimComponent>();
            foreach (var entry in references)
            {
                SimMultiValue mv = mvFactory.FirstOrDefault(x => x.Id.LocalId == entry.MultiValueId);
                if (mv != null && mv.MVType == entry.Type)
                {
                    SimComponent c = null;
                    if (cache.ContainsKey(entry.ComponentId))
                        c = cache[entry.ComponentId];
                    else
                    {
                        c = mvFactory.ProjectData.IdGenerator.GetById<SimComponent>(new SimId(entry.ComponentId.LocalId));
                        if (c != null && c.Id.GlobalId == mvFactory.CalledFromLocation?.GlobalID)
                            cache.Add(c.Id, c);
                    }
                    if (c != null && c.Id.GlobalId == mvFactory.CalledFromLocation?.GlobalID)
                    {
                        SetParameterMultiValuePointerAccToRefDetailed(c, entry, mv, true);
                    }
                }
            }
            cache.Clear();
        }

        /// <summary>
        /// Set the references between component parameters and the values according to
        /// the given references including the exact pointer coordinates by applying matching.
        /// </summary>
        /// <param name="components">the component manager</param>
        /// <param name="multiValues">The multivalues where ValueFields should be searched for</param>
        /// <param name="references">the reference records</param>
        private static void ReinstateReferencesDetailedWoIds(SimComponentCollection components, SimMultiValueCollection multiValues,
            IEnumerable<ComponentToMultiValueReference> references)
        {
            if (components == null || references == null) return;

            foreach (var reference in references)
            {
                // if there is a match for more than one value, skip -> no way to pick the correct one
                var mv_matches = multiValues.Where(x => ComponentToMultiValueReference.IsMatch(reference, x));
                int nr_vmatches = mv_matches.Count();
                //if (nr_vmatches != 1) Console.WriteLine("Abnormal # of value matches: {0} - {1}", nr_vmatches, reference.MultiValueInfo);
                if (nr_vmatches != 1) continue;

                SimMultiValue mv = mv_matches.First();
                if (mv != null && mv.MVType == reference.Type)
                {
                    SimComponent c = null;
                    List<SimComponent> user_matches = new List<SimComponent>();
                    GetMatches(components, reference, user_matches);

                    // if there is no or more than one match for the component and parameter combo, skip
                    if (user_matches.Count == 1)
                    {
                        c = user_matches.First();
                        SetParameterMultiValuePointerAccToRefDetailed(c, reference, mv, false);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the specific pointer to the parameter in the given reference.
        /// </summary>
        /// <param name="_comp">the calling component</param>
        /// <param name="_reference">the reference details</param>
        /// <param name="_mv">the value which the parameter should reference</param>
        /// <param name="_use_id">if true, serch the parameter by id, otherwise by a match</param>
        private static void SetParameterMultiValuePointerAccToRefDetailed(SimComponent _comp, ComponentToMultiValueReference _reference, SimMultiValue _mv, bool _use_id)
        {
            if (_mv == null || _reference == null) return;

            SimParameter p = null;
            if (_use_id)
                p = _comp.Factory.ProjectData.IdGenerator.GetById<SimParameter>(new SimId(_comp.Factory.CalledFromLocation, _reference.ParameterId));
            else
                p = ComponentToMultiValueReference.FindMatch(_reference, _comp);
            if (p != null)
            {
                switch (_reference.Type)
                {
                    case SimMultiValueType.Field3D:
                        if (_mv is SimMultiValueField3D && _reference is ComponentToMultiValueTableReference)
                        {
                            ComponentToMultiValueTableReference tref = _reference as ComponentToMultiValueTableReference;
                            p.MultiValuePointer = new SimMultiValueField3D.SimMultiValueField3DPointer(_mv as SimMultiValueField3D, tref.AxisValueX, tref.AxisValueY, tref.AxisValueZ);
                        }
                        break;
                    case SimMultiValueType.Function:
                        if (_mv is SimMultiValueFunction && _reference is ComponentToMultiValueFunctionReference)
                        {
                            ComponentToMultiValueFunctionReference fref = _reference as ComponentToMultiValueFunctionReference;
                            p.MultiValuePointer = new SimMultiValueFunction.MultiValueFunctionPointer(_mv as SimMultiValueFunction, fref.GraphName, fref.AxisValueX, fref.AxisValueY);
                        }
                        break;
                    case SimMultiValueType.BigTable:
                        if (_mv is SimMultiValueBigTable && _reference is ComponentToMultiValueBigTableReference)
                        {
                            ComponentToMultiValueBigTableReference bref = _reference as ComponentToMultiValueBigTableReference;
                            p.MultiValuePointer = new SimMultiValueBigTable.SimMultiValueBigTablePointer(_mv as SimMultiValueBigTable, bref.RowIndex, bref.ColumnIndex);
                        }
                        break;
                }
            }
        }


        private static ComponentToMultiValueReference GetReferenceFromPointer(SimMultiValuePointer ptr, SimComponent component, bool verbose)
        {
            ComponentToMultiValueReference reference = null;

            switch (ptr)
            {
                case SimMultiValueBigTable.SimMultiValueBigTablePointer btPtr:
                    reference = new ComponentToMultiValueBigTableReference(component.Id,
                        ptr.TargetParameter.Id.LocalId, ptr.ValueField.Id.LocalId, btPtr.Row, btPtr.Column);
                    break;
                case SimMultiValueFunction.MultiValueFunctionPointer fPtr:
                    reference = new ComponentToMultiValueFunctionReference(component.Id,
                        ptr.TargetParameter.Id.LocalId, ptr.ValueField.Id.LocalId, fPtr.GraphName, fPtr.AxisValueX, fPtr.AxisValueY);
                    break;
                case SimMultiValueField3D.SimMultiValueField3DPointer f3dPtr:
                    reference = new ComponentToMultiValueTableReference(component.Id, ptr.TargetParameter.Id.LocalId, ptr.ValueField.Id.LocalId,
                        f3dPtr.AxisValueX, f3dPtr.AxisValueY, f3dPtr.AxisValueZ);
                    break;
                default:
                    throw new NotImplementedException("Pointer type not supported");
            }

            if (verbose)
                reference?.AddDescriptiveInfo(component, ptr.TargetParameter, ptr.ValueField);

            return reference;
        }

        private static void GetMatches(IEnumerable<SimComponent> components, ComponentToMultiValueReference reference, List<SimComponent> results)
        {
            foreach (var comp in components)
            {
                if (comp != null)
                {
                    if (ComponentToMultiValueReference.FindMatch(reference, comp) != null)
                        results.Add(comp);

                    GetMatches(comp.Components.Select(x => x.Component), reference, results);
                }
            }
        }
    }
}
