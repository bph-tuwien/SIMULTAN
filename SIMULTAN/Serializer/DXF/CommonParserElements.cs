using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    /// <summary>
    /// Provides syntax elements used by several file formats
    /// </summary>
    public static class CommonParserElements
    {
        /// <summary>
        /// Syntax for a version section
        /// </summary>
        public static DXFSectionParserElement<DXFParserInfo> VersionSectionElement =
            new DXFSectionParserElement<DXFParserInfo>(ParamStructTypes.VERSION_SECTION, new DXFEntityParserElement<DXFParserInfo>[]
            {
                new DXFEntityParserElement<DXFParserInfo>(ParamStructTypes.FILE_VERSION,
                    (data, info) => DXFParserInfo.Parse(data, info),
                    new DXFEntryParserElement[]
                    {
                        new DXFSingleEntryParserElement<ulong>(ParamStructCommonSaveCode.COORDS_X)
                    })
            });
    }
}
