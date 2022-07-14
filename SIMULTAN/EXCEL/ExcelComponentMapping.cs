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
        /// <summary>
        /// List of component Ids that were traversed to reach the current component
        /// </summary>
        public IReadOnlyList<long> Path { get; private set; }
        public string ToolName { get; private set; }
        public string RuleName { get; private set; }
        public int RuleIndexInTool { get; private set; }

        internal ExcelComponentMapping(IEnumerable<long> _path, string _tool_name, string _rule_name, int _rule_index_in_tool)
        {
            this.Path = new List<long>(_path);
            this.ToolName = _tool_name;
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

        internal static string ConstructQueryFromPath(IEnumerable<long> _path)
        {
            if (_path == null) return string.Empty;

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

        internal static bool IsSamePath(IEnumerable<long> _path_1, IEnumerable<long> _path_2)
        {
            if (_path_1 == null && _path_2 == null) return true;
            if (_path_1 == null || _path_2 == null) return false;

            var enum1 = _path_1.GetEnumerator();
            var enum2 = _path_2.GetEnumerator();

            bool canMove1 = enum1.MoveNext();
            bool canMove2 = enum2.MoveNext();

            while (canMove1 && canMove2)
            {
                if (enum1.Current != enum2.Current)
                    return false;

                canMove1 = enum1.MoveNext();
                canMove2 = enum2.MoveNext();
            }

            if (canMove1 != canMove2)
                return false;

            return true;
        }
    }
}
