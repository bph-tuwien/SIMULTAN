using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Utils;
using SIMULTAN.Utils.Collections;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SIMULTAN.Excel
{
    #region ENUMS
    public enum MappingSubject
    {
        Component = 0,
        Parameter = 1,
        Geometry = 2,
        GeometryPoint = 3,
        GeometryArea = 4,
        GeometricOrientation = 5,
        GeometricIncline = 6,
        Instance = 7
    }
    #endregion

    public class ExcelToolFactory : INotifyPropertyChanged
    {
        #region PROPERTIES, FIELDS

        public ProjectData ProjectData { get; }

        public ObservableCollection<ExcelTool> RegisteredTools { get; private set; }

        private void RegisteredTools_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (var oldItem in e.OldItems.OfType<ExcelTool>())
                    oldItem.Factory = null;
            if (e.NewItems != null)
                foreach (var newItem in e.NewItems.OfType<ExcelTool>())
                    newItem.Factory = this;
        }

        #endregion

        #region EVENTS

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string _propName)
        {
            if (_propName == null)
                return;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(_propName));
        }

        #endregion


        public ExcelToolFactory(ProjectData projectData)
        {
            if (projectData == null)
                throw new ArgumentNullException(nameof(projectData));

            this.ProjectData = projectData;
            this.RegisteredTools = new ObservableCollection<ExcelTool>();
            this.RegisteredTools.CollectionChanged += RegisteredTools_CollectionChanged;
        }

        public void ClearRecord()
        {
            //Required because clear does not have OldItems set
            this.RegisteredTools.ForEach(x => x.Factory = null);
            this.RegisteredTools.Clear();
        }




        #region TOOL MANAGEMENT
        public ExcelTool CreateEmpty()
        {
            ExcelTool created = new ExcelTool();
            this.RegisteredTools.Add(created);
            return created;
        }

        public ExcelTool CreateCopyOf(ExcelTool _tool, string nameCopyFormat)
        {
            ExcelTool copy = new ExcelTool(_tool, nameCopyFormat);
            this.RegisteredTools.Add(copy);
            return copy;
        }

        public void RemoveExcelTool(ExcelTool _tool)
        {
            if (_tool == null) return;
            this.RegisteredTools.Remove(_tool);
        }

        #endregion

        #region RULE MANAGEMENT

        public void RestoreDependencies(ProjectData projectData)
        {
            foreach (ExcelTool tool in this.RegisteredTools)
            {
                foreach (ExcelUnmappingRule um in tool.OutputRules)
                {
                    if (!um.UnmapByFilter && um.TargetParameterID != -1)
                    {
                        var lookupId = new SimId(projectData.Components.CalledFromLocation, um.TargetParameterID);
                        um.TargetParameter = projectData.IdGenerator.GetById<SimParameter>(lookupId);
                        um.TargetParameterID = -1;
                    }
                }
            }
        }

        #endregion
    }
}
