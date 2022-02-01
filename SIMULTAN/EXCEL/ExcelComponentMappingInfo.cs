using SIMULTAN.Data.Components;
using SIMULTAN.Data.Users;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Point4D = System.Windows.Media.Media3D.Point4D;

namespace SIMULTAN.Excel
{
    #region SIMPLE MAPPING FOR DISPLAY

    /// <summary>
    /// Only for display purposes.
    /// </summary>
    public class ExcelComponentMappingInfo
    {
        internal static uint NR_CREATED = 0;
        public uint Id { get; }

        public int Level { get; private set; }
        public bool IsAlongTheSubcomponentPath { get; private set; }
        public SimComponent MappedComponent { get; private set; }
        public ExcelTool MappedTool { get; private set; }
        public ExcelMappingNode SelectedRule { get; set; }

        public bool IsHiglighted { get; set; }

        public ExcelComponentMappingInfo(int _level, bool _along_subcomponent_path, SimComponent _comp, ExcelTool _tool)
        {
            this.Id = (NR_CREATED++);
            this.Level = _level;
            this.IsAlongTheSubcomponentPath = _along_subcomponent_path;
            this.MappedComponent = _comp;
            this.MappedTool = _tool;
            this.IsHiglighted = false;
        }

        public static List<long> GetPath(List<ExcelComponentMappingInfo> _list, ExcelComponentMappingInfo _entry)
        {
            if (_list == null || _entry == null) return new List<long>();
            if (_list.Count == 0) return new List<long>();
            if (_entry.Level == 0) return new List<long> { _entry.MappedComponent.Id.LocalId };

            int index_in_list = _list.IndexOf(_entry);
            int min_level = _entry.Level;
            List<long> path = new List<long>();
            if (0 <= index_in_list && index_in_list < _list.Count)
            {
                path.Add(_entry.MappedComponent.Id.LocalId);
                for (int i = index_in_list - 1; i >= 0; i--)
                {
                    if (_list[i].Level >= min_level) continue;

                    path.Add(_list[i].MappedComponent.Id.LocalId);
                    min_level = _list[i].Level;
                    if (_list[i].Level == 0)
                    {
                        path.Reverse();
                        return path;
                    }
                }
            }
            path.Reverse();
            return path;
        }
    }

    #endregion

}
