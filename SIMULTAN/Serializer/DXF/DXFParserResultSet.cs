using SIMULTAN.Data;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        internal T Get<T>(ExcelMappingSaveCode code, T defaultValue)
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
        /// Gets a SimId by reading two entries from the dataset.
        /// When the global Id equals <see cref="Guid.Empty"/> and the local id is not 0, the global Id gets replaced with projectId.
        /// </summary>
        /// <param name="globalIdCode">DXF code for the global id</param>
        /// <param name="localIdCode">DXF code for the local id</param>
        /// <param name="projectId">Global Id of the current project</param>
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
        internal SimId GetSimId(ValueMappingSaveCode globalIdCode, ValueMappingSaveCode localIdCode, Guid projectId)
        {
            return GetSimId((int)globalIdCode, (int)localIdCode, projectId);
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
