using Assimp;
using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.DataMapping
{
    /// <summary>
    /// Collection of rules that describe how component/geometry data should be mapped into and from a table.
    /// The default implementation maps to a <see cref="SimMultiValueBigTable"/>, but reading needs to be implemented by client implementations.
    /// 
    /// The main target of the tool is to map data to Excel spreadsheets, then execute a macro and read back data. The transfer process
    /// to Excel as well as the macro execution needs to be implemented by client implementations.
    /// This decision was necessary to ensure that SIMULTAN can be run on machines without an active Microsoft Office installation.
    /// 
    /// Components need to be mapped to root rules in order to be traversed. The tool can either be run with a subset of the mapped rules,
    /// or with all mapped rules at once. Components that aren't mapped will not be traversed.
    /// </summary>
    public class SimDataMappingTool : SimObjectNew<SimDataMappingToolCollection>
    {
        #region Properties

        /// <summary>
        /// The name of the tool
        /// </summary>
        public string Name
        {
            get { return name; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException(nameof(value));

                if (name != value)
                {
                    name = value;
                    NotifyPropertyChanged(nameof(Name));
                }
            }
        }
        private string name;

        /// <summary>
        /// The name of the macro. When trying to run Excel macros, the macro name usually needs to be prefixed with the module name
        /// </summary>
        public string MacroName
        {
            get { return macroName; }
            set
            {
                if (macroName != value)
                {
                    macroName = value;
                    NotifyPropertyChanged(nameof(MacroName));
                }
            }
        }
        private string macroName;

        /// <summary>
        /// The rules to map data towards a table. Also stores the assignment of components to rules
        /// </summary>
        public SimDataMappingRootRuleCollection Rules { get; }
        /// <summary>
        /// The rules
        /// </summary>
        public SimDataMappingReadRuleCollection ReadRules { get; }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SimDataMappingTool"/> class
        /// </summary>
        /// <param name="name">The name of the tool</param>
        public SimDataMappingTool(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException(nameof(name));

            this.name = name;
            this.Rules = new SimDataMappingRootRuleCollection(this);
            this.ReadRules = new SimDataMappingReadRuleCollection(this);
        }

        /// <summary>
        /// Executes the tool. The result is a <see cref="SimMultiValueBigTable"/> for each SheetName that is written by the rules
        /// </summary>
        /// <param name="components">A list of components, or null when all mapped components should be mapped</param>
        /// <param name="tableNameFormat">Format for the name of tables. This name is primarily used to identify existing tables
        /// when overrideResults is set to True</param>
        /// <param name="overrideResults">When set to True, tables that have a matching name are overwritten by the tool. Otherwise, new tables
        /// are created</param>
        /// <param name="addNewTables">When set to True, newly created <see cref="SimMultiValueBigTable"/> instances are added to the project.
        /// Set this to False when only temporary results are required</param>
        /// <returns>A list of worksheet names with corresponding data for each sheet</returns>
        public List<(string sheetName, SimMultiValueBigTable table)> Execute(
            HashSet<SimComponent> components = null,
            string tableNameFormat = "{0}_{1}", bool overrideResults = true, bool addNewTables = true)
        {
            if (this.Factory == null)
                throw new InvalidOperationException("Tool is not part of a project");

            List<(string sheetName, SimMultiValueBigTable table)> resultTables = new List<(string sheetName, SimMultiValueBigTable table)>();

            SimMappedData data = new SimMappedData();
            SimTraversalState state = null;

            //Execute rules
            foreach (var rule in this.Rules)
            {
                var oldState = state;
                state = new SimTraversalState();
                if (oldState != null)
                    state.ModelsToRelease = oldState.ModelsToRelease;

                var ruleComponents = Rules.GetMappings(rule);

                foreach (var component in ruleComponents)
                {
                    if (components == null || components.Contains(component))
                        rule.Execute(component, state, data);
                }
            }

            if (state != null)
                state.ReleaseModels(this.Factory.ProjectData);

            //Transfer data to tables
            foreach (var sheetName in data.Data.Keys)
            {               
                //Create table
                SimMultiValueBigTable sheetTable = null;
                string tableName = string.Format(tableNameFormat, this.Name, sheetName);

                //Search for existing table
                if (overrideResults)
                {
                    sheetTable = this.Factory.ProjectData.ValueManager.FirstOrDefault(x => x.Name == tableName) as SimMultiValueBigTable;
                }

                var isNew = sheetTable == null;
                sheetTable = data.ConverToTable(sheetName, sheetTable);
                sheetTable.Name = tableName;

                if (isNew && addNewTables) //Create new table if non exists
                {
                    this.Factory.ProjectData.ValueManager.Add(sheetTable);
                }

                resultTables.Add((sheetName, sheetTable));
            }

            return resultTables;
        }
    
        /// <summary>
        /// Creates a deep copy of the tool and of all rules. Component mappings are not copied.
        /// </summary>
        /// <returns>A deep copy of the tool</returns>
        public SimDataMappingTool Clone()
        {
            var tool = new SimDataMappingTool(this.Name)
            {
                MacroName = this.MacroName,
            };

            tool.Rules.AddRange(this.Rules.Select(x => x.Clone()));
            tool.ReadRules.AddRange(this.ReadRules.Select(x => x.Clone()));

            return tool;
        }
    }
}
