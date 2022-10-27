using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.MultiValues;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Legacy
{
    /// <summary>
    /// Abstract base class for all references btw component parameters and SimMultiValue fields.
    /// </summary>
    public abstract class ComponentToMultiValueReference
    {
        /// <summary>
        /// Serialization delimiter btw. property values.
        /// </summary>
        public const string DELIMITER = ";";
        /// <summary>
        /// The component's id.
        /// </summary>
        public SimId ComponentId { get; }
        /// <summary>
        /// The parameter's id.
        /// </summary>
        public long ParameterId { get; }
        /// <summary>
        /// The multivalue id.
        /// </summary>
        public long MultiValueId { get; }
        /// <summary>
        /// The type of the multivalue field.
        /// </summary>
        public SimMultiValueType Type { get; }

        /// <summary>
        /// Descriptive information about the component (global).
        /// </summary>
        public string ComponentLocationInfo { get; set; }
        /// <summary>
        /// Descriptive information about the component (local).
        /// </summary>
        public string ComponentInfo { get; set; }
        /// <summary>
        /// Descriptive information about the parameter.
        /// </summary>
        public string ParameterInfo { get; set; }
        /// <summary>
        /// Descriptive information about the values.
        /// </summary>
        public string MultiValueInfo { get; set; }

        /// <summary>
        /// Initializes an instance of ComponentToMultiValueReference.
        /// </summary>
        /// <param name="componentId">the component id</param>
        /// <param name="parameterId">the parameter id</param>
        /// <param name="valueId">the id of the value field</param>
        /// <param name="type">the type of the value field</param>
        protected ComponentToMultiValueReference(SimId componentId, long parameterId, long valueId, SimMultiValueType type)
        {
            this.ComponentId = componentId;
            this.ParameterId = parameterId;
            this.MultiValueId = valueId;
            this.Type = type;
        }

        /// <summary>
        /// Serializes the instance to a string.
        /// </summary>
        /// <returns></returns>
        public virtual string Serialize()
        {
            return this.ComponentId.GlobalId.ToString() + DELIMITER + this.ComponentId.LocalId.ToString() + DELIMITER + this.ParameterId.ToString() + DELIMITER + this.MultiValueId.ToString() + DELIMITER + this.Type;
        }
        /// <summary>
        /// Serializes the instance additional info to a string.
        /// </summary>
        /// <returns></returns>
        public string SerializeInfo()
        {
            return this.ComponentInfo + DELIMITER + this.ParameterInfo + DELIMITER + this.MultiValueInfo;
        }

        /// <summary>
        /// Deserializes the input string to a specific subtype of ComponentToMultiValueReference.
        /// </summary>
        /// <param name="input">the input string to deserialize</param>
        /// <returns>an instance of subtype ComponentToMultiValueReference or null</returns>
        public static ComponentToMultiValueReference Deserialize(string input)
        {
            if (string.IsNullOrEmpty(input)) return null;

            string[] components = input.Split(new string[] { DELIMITER }, StringSplitOptions.RemoveEmptyEntries);
            if (components.Length > 4)
            {
                bool p1a = Guid.TryParse(components[0], out Guid idC_global);
                bool p1b = long.TryParse(components[1], out long idC_local);
                bool p2 = long.TryParse(components[2], out long idP);
                bool p3 = long.TryParse(components[3], out long idMV);
                bool p4 = Enum.TryParse(components[3], out SimMultiValueType type);
                if (p1a && p1b && p2 && p3 && p4)
                {
                    switch (type)
                    {
                        case SimMultiValueType.BigTable:
                            if (components.Length >= 6)
                            {
                                bool p5 = int.TryParse(components[4], out int rInd);
                                bool p6 = int.TryParse(components[5], out int cInd);
                                if (p5 && p6)
                                    return new ComponentToMultiValueBigTableReference(new SimId(idC_global, idC_local), idP, idMV, rInd, cInd);
                            }
                            break;
                        case SimMultiValueType.Function:
                            if (components.Length >= 7)
                            {
                                string gN = components[4];
                                bool p6 = double.TryParse(components[5], NumberStyles.Float, new NumberFormatInfo(), out double aX);
                                bool p7 = double.TryParse(components[6], NumberStyles.Float, new NumberFormatInfo(), out double aY);
                                if (!string.IsNullOrEmpty(gN) && p6 && p7)
                                    return new ComponentToMultiValueFunctionReference(new SimId(idC_global, idC_local), idP, idMV, gN, aX, aY);
                            }
                            break;
                        case SimMultiValueType.Field3D:
                            if (components.Length >= 7)
                            {
                                bool p5 = double.TryParse(components[4], NumberStyles.Float, new NumberFormatInfo(), out double aX);
                                bool p6 = double.TryParse(components[5], NumberStyles.Float, new NumberFormatInfo(), out double aY);
                                bool p7 = double.TryParse(components[6], NumberStyles.Float, new NumberFormatInfo(), out double aZ);
                                if (p5 && p6 && p7)
                                    return new ComponentToMultiValueTableReference(new SimId(idC_global, idC_local), idP, idMV, aX, aY, aZ);
                            }
                            break;
                    }
                }
            }
            return null;
        }

        internal void AddDescriptiveInfo(SimComponent component, SimParameter parameter, SimMultiValue value)
        {
            this.ComponentInfo = ExtractDescriptiveInfoForReference(component);
            this.ParameterInfo = ExtractDescriptiveInfoForReference(parameter);
            this.MultiValueInfo = ExtractDescriptiveInfoForReference(value);
        }

        private static string ExtractDescriptiveInfoForReference(SimComponent component)
        {
            if (component != null)
            {
                string self = "NAME: " + component.Name + " DESCRIPTION: " + component.Description + " CURRENT_SLOT: " + component.CurrentSlot
                      + " sC: " + component.Components.Count + " sP: " + component.Parameters.Count;

                var parentComp = (SimComponent)component.Parent;
                string parent = (parentComp == null) ? string.Empty :
                    " PARENT_NAME: " + parentComp.Name + " " + parentComp.Description + " " + parentComp.CurrentSlot;
                string parentP = string.Empty;
                if (parentComp != null && component.Parent.Parent != null)
                    parentP = " PPARENT_NAME: " + parentComp.Name + " " + parentComp.Description;
                return self + parent + parentP;
            }
            else
                return string.Empty;
        }
        private static string ExtractDescriptiveInfoForReference(SimParameter parameter)
        {
            if (parameter != null)
                return "NAME: " + parameter.TaxonomyEntry.Name + " UNIT: " + parameter.Unit + " PROPAGATION: " + parameter.Propagation;
            else
                return string.Empty;
        }
        private static string ExtractDescriptiveInfoForReference(SimMultiValue value)
        {
            if (value != null)
            {
                string output = "NAME: " + value.Name;
                if (value is SimMultiValueBigTable)
                    output += " R: " + (value as SimMultiValueBigTable).RowHeaders.Count + " C: " + (value as SimMultiValueBigTable).ColumnHeaders.Count;
                else if (value is SimMultiValueField3D)
                    output += " X: " + (value as SimMultiValueField3D).XAxis.Count + " Y: " + (value as SimMultiValueField3D).YAxis.Count + " Z: " + (value as SimMultiValueField3D).ZAxis.Count;
                else if (value is SimMultiValueFunction)
                    output += " GrN: " + (value as SimMultiValueFunction).Graphs.Count + " Z: " + (value as SimMultiValueFunction).ZAxis.Count;
                return output;
            }
            else
                return string.Empty;
        }

        public static SimParameter FindMatch(ComponentToMultiValueReference reference, SimComponent c)
        {
            if (c == null || reference == null) return null;

            int nrP_matches = 0;
            SimParameter last_match = null;
            if (ExtractDescriptiveInfoForReference(c) == reference.ComponentInfo)
            {
                foreach (var entry in c.Parameters)
                {
                    if (ExtractDescriptiveInfoForReference(entry) == reference.ParameterInfo)
                    {
                        nrP_matches++;
                        last_match = entry;
                    }
                }
            }
            if (nrP_matches == 1)
                return last_match;
            else
                return null;
        }

        /// <summary>
        /// Matches the given reference with the value using name and size, not ids.
        /// </summary>
        /// <param name="reference">the reference</param>
        /// <param name="mv">the value field</param>
        /// <returns>true, if it is a match</returns>
        public static bool IsMatch(ComponentToMultiValueReference reference, SimMultiValue mv)
        {
            if (reference == null || mv == null) return false;
            return ExtractDescriptiveInfoForReference(mv) == reference.MultiValueInfo;
        }

        /// <summary>
        /// Adds descriptive information from a string.
        /// </summary>
        /// <param name="_input">the additional information about component, parameter and value</param>
        public void AddInfo(string _input)
        {
            string[] components = _input.Split(new string[] { DELIMITER }, StringSplitOptions.None);
            if (components.Length == 3)
            {
                this.ComponentInfo = components[0];
                this.ParameterInfo = components[1];
                this.MultiValueInfo = components[2];
            }
        }
    }

    /// <summary>
    /// Reference btw a component parameter and a MultiValueBigTable.
    /// </summary>
    public class ComponentToMultiValueBigTableReference : ComponentToMultiValueReference
    {
        /// <summary>
        /// The index of the table row.
        /// </summary>
        public int RowIndex { get; }
        /// <summary>
        /// The index of the table column.
        /// </summary>
        public int ColumnIndex { get; }

        /// <summary>
        /// Initializes an instance of type ComponentToMultiValueBigTableReference.
        /// </summary>
        /// <param name="componentId">the id of the component</param>
        /// <param name="parameterId">the id of the parameter</param>
        /// <param name="valueId">the id of the value field</param>
        /// <param name="rowIndex">the row index of the table</param>
        /// <param name="columnIndex">the column index of the table</param>
        public ComponentToMultiValueBigTableReference(SimId componentId, long parameterId, long valueId, int rowIndex, int columnIndex)
            : base(componentId, parameterId, valueId, SimMultiValueType.BigTable)
        {
            this.RowIndex = rowIndex;
            this.ColumnIndex = columnIndex;
        }
        /// <inheritdoc/>
        public override string Serialize()
        {
            return base.Serialize() + DELIMITER + this.RowIndex.ToString() + DELIMITER + this.ColumnIndex.ToString();
        }
    }

    /// <summary>
    /// Reference btw a component parameter and a SimMultiValueFunction.
    /// </summary>
    public class ComponentToMultiValueFunctionReference : ComponentToMultiValueReference
    {
        /// <summary>
        /// The name of the function graph.
        /// </summary>
        public string GraphName { get; }
        /// <summary>
        /// The value along the X axis.
        /// </summary>
        public double AxisValueX { get; }
        /// <summary>
        /// The value aong the Y axis.
        /// </summary>
        public double AxisValueY { get; }

        /// <summary>
        /// Initializes an instance of type ComponentToMultiValueFunctionReference.
        /// </summary>
        /// <param name="componentId">the id of the component</param>
        /// <param name="parameterId">the id of the parameter</param>
        /// <param name="valueId">the id of the value field</param>
        /// <param name="graphName">the name of the function graph</param>
        /// <param name="axisValueX">the value along the x axis</param>
        /// <param name="axisValueY">the value along the y axis</param>
        public ComponentToMultiValueFunctionReference(SimId componentId, long parameterId, long valueId, string graphName, double axisValueX, double axisValueY)
            : base(componentId, parameterId, valueId, SimMultiValueType.Function)
        {
            this.GraphName = graphName;
            this.AxisValueX = axisValueX;
            this.AxisValueY = axisValueY;
        }
        /// <inheritdoc/>
        public override string Serialize()
        {
            return base.Serialize() + DELIMITER + this.GraphName + DELIMITER + this.AxisValueX.ToString() + DELIMITER + this.AxisValueY;
        }
    }

    /// <summary>
    /// Reference btw a component parameter and a SimMultiValueField3D.
    /// </summary>
    public class ComponentToMultiValueTableReference : ComponentToMultiValueReference
    {
        /// <summary>
        /// The value along the X axis.
        /// </summary>
        public double AxisValueX { get; }
        /// <summary>
        /// The value along the Y axis.
        /// </summary>
        public double AxisValueY { get; }
        /// <summary>
        /// The value along the Z axis.
        /// </summary>
        public double AxisValueZ { get; }

        /// <summary>
        /// Initializes an instance of type ComponentToMultiValueTableReference.
        /// </summary>
        /// <param name="componentId">the id of the component</param>
        /// <param name="parameterId">the id of the parameter</param>
        /// <param name="valueId">the id of the value field</param>
        /// <param name="axisValueX">the value along the X axis</param>
        /// <param name="axisValueY">the value along the Y axis</param>
        /// <param name="axisValueZ">the value along the Z axis</param>
        public ComponentToMultiValueTableReference(SimId componentId, long parameterId, long valueId, double axisValueX, double axisValueY, double axisValueZ)
            : base(componentId, parameterId, valueId, SimMultiValueType.Field3D)
        {
            this.AxisValueX = axisValueX;
            this.AxisValueY = axisValueY;
            this.AxisValueZ = axisValueZ;
        }
        /// <inheritdoc/>
        public override string Serialize()
        {
            return base.Serialize() + DELIMITER + this.AxisValueX.ToString() + DELIMITER + this.AxisValueY.ToString() + DELIMITER + this.AxisValueZ.ToString();
        }
    }
}
