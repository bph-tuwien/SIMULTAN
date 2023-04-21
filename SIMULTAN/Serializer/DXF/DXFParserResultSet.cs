using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Taxonomy;
using System;
using System.Collections.Generic;

namespace SIMULTAN.Serializer.DXF
{
    /// <summary>
    /// Stores the results of a DXF reading operation
    /// </summary>
    public class DXFParserResultSet
    {
        private Dictionary<int, object> data = new Dictionary<int, object>();

        /// <summary>
        /// Adds a new data entry
        /// </summary>
        /// <param name="code">The code of the entry</param>
        /// <param name="data">The parsed data of the entry</param>
        internal void Add(int code, object data)
        {
            this.data.Add(code, data);
        }

        /// <summary>
        /// Gets the data for a given code, or a default value when the code does not exist in the data set
        /// </summary>
        /// <typeparam name="T">Type of the result</typeparam>
        /// <param name="code">The code to search for</param>
        /// <param name="defaultValue">The default value returned when the code does not exist</param>
        /// <returns>The data for a given code, or a default value when the code does not exist in the data set</returns>
        internal T Get<T>(int code, T defaultValue)
        {
            if (this.data.TryGetValue(code, out var result))
                return (T)result;
            return defaultValue;
        }


        /// <summary>
        /// Gets the data for a given code, or a default value when the code does not exist in the data set
        /// </summary>
        /// <typeparam name="T">Type of the result</typeparam>
        /// <param name="code">The code to search for</param>
        /// <param name="defaultValue">The default value returned when the code does not exist</param>
        /// <returns>The data for a given code, or a default value when the code does not exist in the data set</returns>
        internal T Get<T>(ComponentSaveCode code, T defaultValue)
        {
            return this.Get<T>((int)code, defaultValue);
        }
        /// <summary>
        /// Gets the data for a given code, or a default value when the code does not exist in the data set
        /// </summary>
        /// <typeparam name="T">Type of the result</typeparam>
        /// <param name="code">The code to search for</param>
        /// <param name="defaultValue">The default value returned when the code does not exist</param>
        /// <returns>The data for a given code, or a default value when the code does not exist in the data set</returns>
        internal T Get<T>(UserSaveCode code, T defaultValue)
        {
            return this.Get<T>((int)code, defaultValue);
        }
        /// <summary>
        /// Gets the data for a given code, or a default value when the code does not exist in the data set
        /// </summary>
        /// <typeparam name="T">Type of the result</typeparam>
        /// <param name="code">The code to search for</param>
        /// <param name="defaultValue">The default value returned when the code does not exist</param>
        /// <returns>The data for a given code, or a default value when the code does not exist in the data set</returns>
        internal T Get<T>(ProjectSaveCode code, T defaultValue)
        {
            return this.Get<T>((int)code, defaultValue);
        }
        /// <summary>
        /// Gets the data for a given code, or a default value when the code does not exist in the data set
        /// </summary>
        /// <typeparam name="T">Type of the result</typeparam>
        /// <param name="code">The code to search for</param>
        /// <param name="defaultValue">The default value returned when the code does not exist</param>
        /// <returns>The data for a given code, or a default value when the code does not exist in the data set</returns>
        internal T Get<T>(CalculatorMappingSaveCode code, T defaultValue)
        {
            return this.Get<T>((int)code, defaultValue);
        }
        /// <summary>
        /// Gets the data for a given code, or a default value when the code does not exist in the data set
        /// </summary>
        /// <typeparam name="T">Type of the result</typeparam>
        /// <param name="code">The code to search for</param>
        /// <param name="defaultValue">The default value returned when the code does not exist</param>
        /// <returns>The data for a given code, or a default value when the code does not exist in the data set</returns>
        internal T Get<T>(ComponentInstanceSaveCode code, T defaultValue)
        {
            return this.Get<T>((int)code, defaultValue);
        }

        /// <summary>
        /// Gets the data for a given code, or a default value when the code does not exist in the data set
        /// </summary>
        /// <typeparam name="T">Type of the result</typeparam>
        /// <param name="code">The code to search for</param>
        /// <param name="defaultValue">The default value returned when the code does not exist</param>
        /// <returns>The data for a given code, or a default value when the code does not exist in the data set</returns>
        internal T Get<T>(SitePlannerSaveCode code, T defaultValue)
        {
            return this.Get<T>((int)code, defaultValue);
        }
        /// <summary>
        /// Gets the data for a given code, or a default value when the code does not exist in the data set
        /// </summary>
        /// <typeparam name="T">Type of the result</typeparam>
        /// <param name="code">The code to search for</param>
        /// <param name="defaultValue">The default value returned when the code does not exist</param>
        /// <returns>The data for a given code, or a default value when the code does not exist in the data set</returns>
        internal T Get<T>(ParamStructCommonSaveCode code, T defaultValue)
        {
            return this.Get<T>((int)code, defaultValue);
        }
        /// <summary>
        /// Gets the data for a given code, or a default value when the code does not exist in the data set
        /// </summary>
        /// <typeparam name="T">Type of the result</typeparam>
        /// <param name="code">The code to search for</param>
        /// <param name="defaultValue">The default value returned when the code does not exist</param>
        /// <returns>The data for a given code, or a default value when the code does not exist in the data set</returns>
        internal T Get<T>(MultiValueSaveCode code, T defaultValue)
        {
            return this.Get<T>((int)code, defaultValue);
        }
        /// <summary>
        /// Gets the data for a given code, or a default value when the code does not exist in the data set
        /// </summary>
        /// <typeparam name="T">Type of the result</typeparam>
        /// <param name="code">The code to search for</param>
        /// <param name="defaultValue">The default value returned when the code does not exist</param>
        /// <returns>The data for a given code, or a default value when the code does not exist in the data set</returns>
        internal T Get<T>(ParameterSaveCode code, T defaultValue)
        {
            return this.Get<T>((int)code, defaultValue);
        }
        /// <summary>
        /// Gets the data for a given code, or a default value when the code does not exist in the data set
        /// </summary>
        /// <typeparam name="T">Type of the result</typeparam>
        /// <param name="code">The code to search for</param>
        /// <param name="defaultValue">The default value returned when the code does not exist</param>
        /// <returns>The data for a given code, or a default value when the code does not exist in the data set</returns>
        internal T Get<T>(CalculationSaveCode code, T defaultValue)
        {
            return this.Get<T>((int)code, defaultValue);
        }
        /// <summary>
        /// Gets the data for a given code, or a default value when the code does not exist in the data set
        /// </summary>
        /// <typeparam name="T">Type of the result</typeparam>
        /// <param name="code">The code to search for</param>
        /// <param name="defaultValue">The default value returned when the code does not exist</param>
        /// <returns>The data for a given code, or a default value when the code does not exist in the data set</returns>
        internal T Get<T>(GeoMapSaveCode code, T defaultValue)
        {
            return this.Get<T>((int)code, defaultValue);
        }
        /// <summary>
        /// Gets the data for a given code, or a default value when the code does not exist in the data set
        /// </summary>
        /// <typeparam name="T">Type of the result</typeparam>
        /// <param name="code">The code to search for</param>
        /// <param name="defaultValue">The default value returned when the code does not exist</param>
        /// <returns>The data for a given code, or a default value when the code does not exist in the data set</returns>
        internal T Get<T>(ComponentAccessTrackerSaveCode code, T defaultValue)
        {
            return this.Get<T>((int)code, defaultValue);
        }
        /// <summary>
        /// Gets the data for a given code, or a default value when the code does not exist in the data set
        /// </summary>
        /// <typeparam name="T">Type of the result</typeparam>
        /// <param name="code">The code to search for</param>
        /// <param name="defaultValue">The default value returned when the code does not exist</param>
        /// <returns>The data for a given code, or a default value when the code does not exist in the data set</returns>
        internal T Get<T>(DataMappingSaveCode code, T defaultValue)
        {
            return this.Get<T>((int)code, defaultValue);
        }
        /// <summary>
        /// Gets the data for a given code, or a default value when the code does not exist in the data set
        /// </summary>
        /// <typeparam name="T">Type of the result</typeparam>
        /// <param name="code">The code to search for</param>
        /// <param name="defaultValue">The default value returned when the code does not exist</param>
        /// <returns>The data for a given code, or a default value when the code does not exist in the data set</returns>
        internal T Get<T>(ChatItemSaveCode code, T defaultValue)
        {
            return this.Get<T>((int)code, defaultValue);
        }
        /// <summary>
        /// Gets the data for a given code, or a default value when the code does not exist in the data set
        /// </summary>
        /// <typeparam name="T">Type of the result</typeparam>
        /// <param name="code">The code to search for</param>
        /// <param name="defaultValue">The default value returned when the code does not exist</param>
        /// <returns>The data for a given code, or a default value when the code does not exist in the data set</returns>
        internal T Get<T>(ResourceSaveCode code, T defaultValue)
        {
            return this.Get<T>((int)code, defaultValue);
        }
        /// <summary>
        /// Gets the data for a given code, or a default value when the code does not exist in the data set
        /// </summary>
        /// <typeparam name="T">Type of the result</typeparam>
        /// <param name="code">The code to search for</param>
        /// <param name="defaultValue">The default value returned when the code does not exist</param>
        /// <returns>The data for a given code, or a default value when the code does not exist in the data set</returns>
        internal T Get<T>(AssetSaveCode code, T defaultValue)
        {
            return this.Get<T>((int)code, defaultValue);
        }
        /// <summary>
        /// Gets the data for a given code, or a default value when the code does not exist in the data set
        /// </summary>
        /// <typeparam name="T">Type of the result</typeparam>
        /// <param name="code">The code to search for</param>
        /// <param name="defaultValue">The default value returned when the code does not exist</param>
        /// <returns>The data for a given code, or a default value when the code does not exist in the data set</returns>
        internal T Get<T>(UserComponentListSaveCode code, T defaultValue)
        {
            return this.Get<T>((int)code, defaultValue);
        }
        /// <summary>
        /// Gets the data for a given code, or a default value when the code does not exist in the data set
        /// </summary>
        /// <typeparam name="T">Type of the result</typeparam>
        /// <param name="code">The code to search for</param>
        /// <param name="defaultValue">The default value returned when the code does not exist</param>
        /// <returns>The data for a given code, or a default value when the code does not exist in the data set</returns>
        internal T Get<T>(TaxonomySaveCode code, T defaultValue)
        {
            return this.Get<T>((int)code, defaultValue);
        }
        /// <summary>
        /// Gets the data for a given code, or a default value when the code does not exist in the data set
        /// </summary>
        /// <typeparam name="T">Type of the result</typeparam>
        /// <param name="code">The code to search for</param>
        /// <param name="defaultValue">The default value returned when the code does not exist</param>
        /// <returns>The data for a given code, or a default value when the code does not exist in the data set</returns>
        internal T Get<T>(FlowNetworkSaveCode code, T defaultValue)
        {
            return this.Get<T>((int)code, defaultValue);
        }
        /// <summary>
        /// Gets the data for a given code, or a default value when the code does not exist in the data set
        /// </summary>
        /// <typeparam name="T">Type of the result</typeparam>
        /// <param name="code">The code to search for</param>
        /// <param name="defaultValue">The default value returned when the code does not exist</param>
        /// <returns>The data for a given code, or a default value when the code does not exist in the data set</returns>
        internal T Get<T>(SimNetworkSaveCode code, T defaultValue)
        {
            return this.Get<T>((int)code, defaultValue);
        }
        /// <summary>
        /// Gets the data for a given code, or a default value when the code does not exist in the data set
        /// </summary>
        /// <typeparam name="T">Type of the result</typeparam>
        /// <param name="code">The code to search for</param>
        /// <param name="defaultValue">The default value returned when the code does not exist</param>
        /// <returns>The data for a given code, or a default value when the code does not exist in the data set</returns>
        internal T Get<T>(ValueMappingSaveCode code, T defaultValue)
        {
            return this.Get<T>((int)code, defaultValue);
        }
        /// <summary>
        /// Gets the data for a given code, or a default value when the code does not exist in the data set
        /// </summary>
        /// <typeparam name="T">Type of the result</typeparam>
        /// <param name="code">The code to search for</param>
        /// <param name="defaultValue">The default value returned when the code does not exist</param>
        /// <returns>The data for a given code, or a default value when the code does not exist in the data set</returns>
        internal T Get<T>(GeometryRelationSaveCode code, T defaultValue)
        {
            return this.Get<T>((int)code, defaultValue);
        }

        /// <summary>
        /// Gets a SimId by reading two entries from the dataset.
        /// When the global Id equals <see cref="Guid.Empty"/> and the local id is not 0, the global Id gets replaced with projectId.
        /// </summary>
        /// <param name="globalIdCode">DXF code for the global id</param>
        /// <param name="localIdCode">DXF code for the local id</param>
        /// <param name="projectId">Global Id of the current project</param>
        /// <returns>The SimId loaded from the two save codes</returns>
        internal SimId GetSimId(int globalIdCode, int localIdCode, Guid projectId)
        {
            var globalId = Get<Guid>(globalIdCode, Guid.Empty);
            var localId = Get<long>(localIdCode, 0L);

            if (globalId == Guid.Empty && localId != 0)
                globalId = projectId;

            return new SimId(globalId, localId);
        }
        /// <summary>
        /// Gets a SimId by reading two entries from the dataset.
        /// When the global Id equals <see cref="Guid.Empty"/> and the local id is not 0, the global Id gets replaced with projectId.
        /// </summary>
        /// <param name="globalIdCode">DXF code for the global id</param>
        /// <param name="localIdCode">DXF code for the local id</param>
        /// <param name="projectId">Global Id of the current project</param>
        /// <returns>The SimId loaded from the two save codes</returns>
        internal SimId GetSimId(DataMappingSaveCode globalIdCode, DataMappingSaveCode localIdCode, Guid projectId)
        {
            return GetSimId((int)globalIdCode, (int)localIdCode, projectId);
        }
        /// <summary>
        /// Gets a SimId by reading two entries from the dataset.
        /// When the global Id equals <see cref="Guid.Empty"/> and the local id is not 0, the global Id gets replaced with projectId.
        /// </summary>
        /// <param name="globalIdCode">DXF code for the global id</param>
        /// <param name="localIdCode">DXF code for the local id</param>
        /// <param name="projectId">Global Id of the current project</param>
        /// <returns>The SimId loaded from the two save codes</returns>
        internal SimId GetSimId(ComponentSaveCode globalIdCode, ComponentSaveCode localIdCode, Guid projectId)
        {
            return GetSimId((int)globalIdCode, (int)localIdCode, projectId);
        }
        /// <summary>
        /// Gets a SimId by reading two entries from the dataset.
        /// When the global Id equals <see cref="Guid.Empty"/> and the local id is not 0, the global Id gets replaced with projectId.
        /// </summary>
        /// <param name="globalIdCode">DXF code for the global id</param>
        /// <param name="localIdCode">DXF code for the local id</param>
        /// <param name="projectId">Global Id of the current project</param>
        /// <returns>The SimId loaded from the two save codes</returns>
        internal SimId GetSimId(ResourceSaveCode globalIdCode, ResourceSaveCode localIdCode, Guid projectId)
        {
            return GetSimId((int)globalIdCode, (int)localIdCode, projectId);
        }
        /// <summary>
        /// Gets a SimId by reading two entries from the dataset.
        /// When the global Id equals <see cref="Guid.Empty"/> and the local id is not 0, the global Id gets replaced with projectId.
        /// </summary>
        /// <param name="globalIdCode">DXF code for the global id</param>
        /// <param name="localIdCode">DXF code for the local id</param>
        /// <param name="projectId">Global Id of the current project</param>
        /// <returns>The SimId loaded from the two save codes</returns>
        internal SimId GetSimId(CalculatorMappingSaveCode globalIdCode, CalculatorMappingSaveCode localIdCode, Guid projectId)
        {
            return GetSimId((int)globalIdCode, (int)localIdCode, projectId);
        }
        /// <summary>
        /// Gets a SimId by reading two entries from the dataset.
        /// When the global Id equals <see cref="Guid.Empty"/> and the local id is not 0, the global Id gets replaced with projectId.
        /// </summary>
        /// <param name="globalIdCode">DXF code for the global id</param>
        /// <param name="localIdCode">DXF code for the local id</param>
        /// <param name="projectId">Global Id of the current project</param>
        /// <returns>The SimId loaded from the two save codes</returns>
        internal SimId GetSimId(ParameterSaveCode globalIdCode, ParameterSaveCode localIdCode, Guid projectId)
        {
            return GetSimId((int)globalIdCode, (int)localIdCode, projectId);
        }
        /// <summary>
        /// Gets a SimId by reading two entries from the dataset.
        /// When the global Id equals <see cref="Guid.Empty"/> and the local id is not 0, the global Id gets replaced with projectId.
        /// </summary>
        /// <param name="globalIdCode">DXF code for the global id</param>
        /// <param name="localIdCode">DXF code for the local id</param>
        /// <param name="projectId">Global Id of the current project</param>
        /// <returns>The SimId loaded from the two save codes</returns>
        internal SimId GetSimId(MultiValueSaveCode globalIdCode, MultiValueSaveCode localIdCode, Guid projectId)
        {
            return GetSimId((int)globalIdCode, (int)localIdCode, projectId);
        }
        /// <summary>
        /// Gets a SimId by reading two entries from the dataset.
        /// When the global Id equals <see cref="Guid.Empty"/> and the local id is not 0, the global Id gets replaced with projectId.
        /// </summary>
        /// <param name="globalIdCode">DXF code for the global id</param>
        /// <param name="localIdCode">DXF code for the local id</param>
        /// <param name="projectId">Global Id of the current project</param>
        /// <returns>The SimId loaded from the two save codes</returns>
        internal SimId GetSimId(SitePlannerSaveCode globalIdCode, SitePlannerSaveCode localIdCode, Guid projectId)
        {
            return GetSimId((int)globalIdCode, (int)localIdCode, projectId);
        }
        /// <summary>
        /// Gets a SimId by reading two entries from the dataset.
        /// When the global Id equals <see cref="Guid.Empty"/> and the local id is not 0, the global Id gets replaced with projectId.
        /// </summary>
        /// <param name="globalIdCode">DXF code for the global id</param>
        /// <param name="localIdCode">DXF code for the local id</param>
        /// <param name="projectId">Global Id of the current project</param>
        /// <returns>The SimId loaded from the two save codes</returns>
        internal SimId GetSimId(ParamStructCommonSaveCode globalIdCode, ParamStructCommonSaveCode localIdCode, Guid projectId)
        {
            return GetSimId((int)globalIdCode, (int)localIdCode, projectId);
        }
        /// <summary>
        /// Gets a SimId by reading two entries from the dataset.
        /// When the global Id equals <see cref="Guid.Empty"/> and the local id is not 0, the global Id gets replaced with projectId.
        /// </summary>
        /// <param name="globalIdCode">DXF code for the global id</param>
        /// <param name="localIdCode">DXF code for the local id</param>
        /// <param name="projectId">Global Id of the current project</param>
        /// <returns>The SimId loaded from the two save codes</returns>
        internal SimId GetSimId(GeometryRelationSaveCode globalIdCode, GeometryRelationSaveCode localIdCode, Guid projectId)
        {
            return GetSimId((int)globalIdCode, (int)localIdCode, projectId);
        }
        /// <summary>
        /// Gets a SimId by reading two entries from the dataset.
        /// When the global Id equals <see cref="Guid.Empty"/> and the local id is not 0, the global Id gets replaced with projectId.
        /// </summary>
        /// <param name="globalIdCode">DXF code for the global id</param>
        /// <param name="localIdCode">DXF code for the local id</param>
        /// <param name="projectId">Global Id of the current project</param>
        /// <returns>The SimId loaded from the two save codes</returns>
        internal SimId GetSimId(AssetSaveCode globalIdCode, AssetSaveCode localIdCode, Guid projectId)
        {
            return GetSimId((int)globalIdCode, (int)localIdCode, projectId);
        }
        /// <summary>
        /// Gets a SimId by reading two entries from the dataset.
        /// When the global Id equals <see cref="Guid.Empty"/> and the local id is not 0, the global Id gets replaced with projectId.
        /// </summary>
        /// <param name="globalIdCode">DXF code for the global id</param>
        /// <param name="localIdCode">DXF code for the local id</param>
        /// <param name="projectId">Global Id of the current project</param>
        /// <returns>The SimId loaded from the two save codes</returns>
        internal SimId GetSimId(ComponentInstanceSaveCode globalIdCode, ComponentInstanceSaveCode localIdCode, Guid projectId)
        {
            return GetSimId((int)globalIdCode, (int)localIdCode, projectId);
        }
        /// <summary>
        /// Gets a SimId by reading two entries from the dataset.
        /// When the global Id equals <see cref="Guid.Empty"/> and the local id is not 0, the global Id gets replaced with projectId.
        /// </summary>
        /// <param name="globalIdCode">DXF code for the global id</param>
        /// <param name="localIdCode">DXF code for the local id</param>
        /// <param name="projectId">Global Id of the current project</param>
        /// <returns>The SimId loaded from the two save codes</returns>
        internal SimId GetSimId(ValueMappingSaveCode globalIdCode, ValueMappingSaveCode localIdCode, Guid projectId)
        {
            return GetSimId((int)globalIdCode, (int)localIdCode, projectId);
        }

        /// <summary>
        /// Get the SimSlot by reading two entries for the slot extension and for the <see cref="SimTaxonomyEntry"/> local id.
        /// </summary>
        /// <param name="extensionSaveCode">Save code for the extension</param>
        /// <param name="taxonomyEntryGlobalIdSaveCode">Save code for the global id of the <see cref="SimTaxonomyEntry"/></param>
        /// <param name="taxonomyEntrySaveCode">Save code for the <see cref="SimTaxonomyEntry"/> local id</param>
        /// <param name="projectId">The id of the project, used to make the SimId for the <see cref="SimTaxonomyEntryReference"/></param>
        /// <returns>The <see cref="SimSlot"/> with the slot extension and taxonomy entry reference containing only the taxonomy entry id</returns>
        internal SimSlot GetSlot(ParamStructCommonSaveCode extensionSaveCode, ComponentSaveCode taxonomyEntryGlobalIdSaveCode, ComponentSaveCode taxonomyEntrySaveCode, Guid projectId)
        {
            return GetSlot((int)extensionSaveCode, (int)taxonomyEntryGlobalIdSaveCode, (int)taxonomyEntrySaveCode, projectId);
        }

        /// <summary>
        /// Get the SimSlot by reading two entries for the slot extension and for the <see cref="SimTaxonomyEntry"/> local id.
        /// </summary>
        /// <param name="extensionSaveCode">Save code for the extension</param>
        /// <param name="taxonomyEntryGlobalIdSaveCode">Save code for the global id of the <see cref="SimTaxonomyEntry"/></param>
        /// <param name="taxonomyEntrySaveCode">Save code for the <see cref="SimTaxonomyEntry"/> local id</param>
        /// <param name="projectId">The id of the project, used to make the SimId for the <see cref="SimTaxonomyEntryReference"/></param>
        /// <returns>The <see cref="SimSlot"/> with the slot extension and taxonomy entry reference containing only the taxonomy entry id</returns>
        internal SimSlot GetSlot(int extensionSaveCode, int taxonomyEntryGlobalIdSaveCode, int taxonomyEntrySaveCode, Guid projectId)
        {
            var extension = Get<String>(extensionSaveCode, "");
            var taxEntId = GetSimId(taxonomyEntryGlobalIdSaveCode, taxonomyEntrySaveCode, projectId);
            if (taxEntId.LocalId == 0)
                throw new Exception("Slot taxonomy entry local id cannot be zero");
            var taxRef = new SimTaxonomyEntryReference(taxEntId);
            return new SimSlot(taxRef, extension);
        }

        /// <summary>
        /// Clears the content of the data set
        /// </summary>
        internal void Clear()
        {
            this.data.Clear();
        }
    }
}
