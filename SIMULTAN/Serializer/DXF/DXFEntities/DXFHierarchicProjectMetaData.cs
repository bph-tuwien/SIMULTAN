using SIMULTAN.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    /// <summary>
    /// Wrapper class for <see cref="HierarchicProjectMetaData"/>
    /// </summary>
    internal class DXFHierarchicProjectMetaData : DXFEntity
    {
        #region CLASS MEMBERS

        public Guid dxf_ProjectId { get; private set; }
        public Dictionary<Guid, string> dxf_ChildProjects { get; }
        private int nr_dxf_ChildProjects;
        private Guid current_child_id;
        private string current_cild_path;

        internal HierarchicProjectMetaData dxf_parsed;

        #endregion

        #region .CTOR

        public DXFHierarchicProjectMetaData()
        {
            this.dxf_ProjectId = new Guid(); // 0000...

            this.dxf_ChildProjects = new Dictionary<Guid, string>();
            this.nr_dxf_ChildProjects = 0;
            this.current_child_id = new Guid();
            this.current_cild_path = string.Empty;
        }

        #endregion

        #region OVERRIDES: Read Property

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ProjectSaveCode.PROJECT_ID:
                    this.dxf_ProjectId = new Guid(this.Decoder.FValue);
                    break;
                case (int)ProjectSaveCode.NR_OF_CHILD_PROJECTS:
                    this.nr_dxf_ChildProjects = this.Decoder.IntValue();
                    break;
                case (int)ProjectSaveCode.CHILD_PROJECT_ID:
                    if (this.nr_dxf_ChildProjects > this.dxf_ChildProjects.Count)
                    {
                        this.current_child_id = new Guid(this.Decoder.FValue);
                    }
                    break;
                case (int)ProjectSaveCode.CHILD_PROJECT_REL_PATH:
                    if (this.nr_dxf_ChildProjects > this.dxf_ChildProjects.Count)
                    {
                        this.current_cild_path = this.Decoder.FValue;
                        if (!this.current_child_id.Equals(new Guid()))
                        {
                            this.dxf_ChildProjects.Add(this.current_child_id, this.current_cild_path);
                            this.current_child_id = new Guid();
                            this.current_cild_path = string.Empty;
                        }
                    }
                    break;
                default:
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    break;
            }

        }

        #endregion

        #region OVERRIDES: Post-Processing

        internal override void OnLoaded()
        {
            base.OnLoaded();

            this.dxf_parsed = new HierarchicProjectMetaData(this.dxf_ProjectId, this.dxf_ChildProjects);
            if (this.Decoder is DXFDecoderMeta)
            {
                (this.Decoder as DXFDecoderMeta).ParsedMetaData = this.dxf_parsed;
            }
        }

        #endregion
    }
}
