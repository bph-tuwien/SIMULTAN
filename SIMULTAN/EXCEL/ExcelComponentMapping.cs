using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Excel
{
    public class ExcelComponentMapping
    {
        public List<long> Path { get; private set; }
        public string ToolName { get; private set; }
        public string ToolFilePath { get; private set; }
        public string RuleName { get; private set; }
        public int RuleIndexInTool { get; private set; }

        internal ExcelComponentMapping(List<long> _path, string _tool_name, string _tool_file_path, string _rule_name, int _rule_index_in_tool)
        {
            this.Path = new List<long>(_path);
            this.ToolName = _tool_name;
            this.ToolFilePath = _tool_file_path;
            this.RuleName = _rule_name;
            this.RuleIndexInTool = _rule_index_in_tool;
        }

        internal string ConstructKey()
        {
            return this.ToolName + "." + this.RuleIndexInTool + "." + this.RuleName + this.ConstructPathQuery();
        }

        internal string ConstructPathQuery()
        {
            return ExcelComponentMapping.ConstructQueryFromPath(this.Path);
        }

        internal static string ConstructQueryFromPath(List<long> _path)
        {
            if (_path == null) return string.Empty;
            if (_path.Count == 0) return string.Empty;

            string path_q = string.Empty;
            foreach (long id in _path)
            {
                path_q += "." + id.ToString();
            }
            return path_q;
        }

        internal static string ExtractPathFromKey(string _key)
        {
            if (string.IsNullOrEmpty(_key)) return string.Empty;

            string path_s = string.Empty;
            string[] key_comps = _key.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
            if (key_comps != null && key_comps.Length > 0)
            {
                foreach (string c in key_comps)
                {
                    long id = -1;
                    bool parsed = long.TryParse(c, out id);
                    if (parsed)
                        path_s += "." + id;
                    else
                        path_s = string.Empty;
                }
            }
            return path_s;
        }

        internal static bool IsSamePath(List<long> _path_1, List<long> _path_2)
        {
            if (_path_1 == null && _path_2 == null) return true;
            if (_path_1 != null && _path_2 == null) return false;
            if (_path_1 == null && _path_2 != null) return false;

            if (_path_1.Count != _path_2.Count) return false;
            if (_path_1.Count == 0) return true;

            bool same = _path_1.Zip(_path_2, (x, y) => x == y).Aggregate((x, y) => x && y);
            return same;
        }

        internal void AddToExport(ref StringBuilder _sb, string _key = null)
        {
            // general
            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.EXCEL_MAPPING);                           // EXCEL_MAPPING

            if (!(string.IsNullOrEmpty(_key)))
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_KEY).ToString());
                _sb.AppendLine(_key);
            }

            _sb.AppendLine(((int)ParamStructCommonSaveCode.CLASS_NAME).ToString());
            _sb.AppendLine(this.GetType().ToString());

            // mapping: path
            _sb.AppendLine(((int)ExcelMappingSaveCode.MAP_PATH).ToString());
            _sb.AppendLine(this.Path.Count.ToString());
            foreach (long pe in this.Path)
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.X_VALUE).ToString());
                _sb.AppendLine(pe.ToString());
            }
            // mapping: tool name
            _sb.AppendLine(((int)ExcelMappingSaveCode.MAP_TOOL_NAME).ToString());
            _sb.AppendLine(this.ToolName);
            // mapping: tool file path
            _sb.AppendLine(((int)ExcelMappingSaveCode.MAP_TOOL_FILE_PATH).ToString());
            _sb.AppendLine(this.ToolFilePath);
            // mapping: rule name
            _sb.AppendLine(((int)ExcelMappingSaveCode.MAP_RULE_NAME).ToString());
            _sb.AppendLine(this.RuleName);
            // mapping: rule index within the tool
            _sb.AppendLine(((int)ExcelMappingSaveCode.MAP_RULE_INDEX).ToString());
            _sb.AppendLine(this.RuleIndexInTool.ToString());
        }
    }
}
