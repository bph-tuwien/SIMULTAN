using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Exchange.Connectors
{
    /// <summary>
    /// Class for connecting a component to a <see cref="Face"/>.
    /// Maintains parameters for area, with and height. Transfers the loop as a 3d point sequence to the default
    /// instance (of type <see cref="SimComponentInstance"/>) of the source component.
    /// Contains no hierarchy of volumes or faces. The hierarchy is reflected only in the component structure.
    /// </summary>
    internal class ConnectorToFace : ConnectorToBaseGeometry
    {
        private Volume volume = null;
        private Face face = null;

        #region .CTOR
        internal ConnectorToFace(ComponentGeometryExchange _comm_manager,
                                 SimComponent _source_parent_comp, SimComponent _source_comp, int _index_of_geometry_model, Face _target_face)
            : base(_comm_manager, _source_parent_comp, _source_comp, _index_of_geometry_model, _target_face)
        {
            var volumeInstance = _source_parent_comp.Instances.FirstOrDefault();
            if (volumeInstance != null && volumeInstance.InstanceType == SimInstanceType.Entity3D)
            {
                var firstGeometryPlacement = (SimInstancePlacementGeometry)volumeInstance.Placements.FirstOrDefault(
                    p => p is SimInstancePlacementGeometry pg && pg.FileId == _index_of_geometry_model);

                if (firstGeometryPlacement != null)
                {
                    var geometry = _target_face.ModelGeometry.GeometryFromId(firstGeometryPlacement.GeometryId);
                    if (geometry != null && geometry is Volume vol)
                    {
                        this.volume = vol;
                    }
                }
            }

            this.face = _target_face;
            this.face.PropertyChanged += this.Face_PropertyChanged;
        }

        private void Face_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BaseGeometry.Name))
            {
                this.DescriptiveSource.Name = string.Format("{0} 2D", this.face.Name);
            }
        }
        #endregion


        #region METHOD OVERRIDES

        /// <inheritdoc/>
        protected override bool SynchTargetIsAdmissible(BaseGeometry _target)
        {
            Face face = _target as Face;
            return (face != null && face.Id == this.TargetId);
        }

        /// <inheritdoc/>
        protected override void UpdateSourceParametersDelayed(BaseGeometry _target)
        {
            if (this.DescriptiveSource == null || this.comm_manager == null) return;
            Face f = _target as Face;
            if (f == null) return;

            using (AccessCheckingDisabler.Disable(this.DescriptiveSource.Factory))
            {
                // TODO: replace all 0.0s with the correct call to the target geometry

                var areas = FaceAlgorithms.AreaMinMax(f);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_AREA, areas.area);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_AREA_MIN, areas.offsetAreaMin);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_AREA_MAX, areas.offsetAreaMax);

                var sizes = FaceAlgorithms.Size(f);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_WIDTH, sizes.size.Width);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_WIDTH_MIN, sizes.minSize.Width);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_WIDTH_MAX, sizes.maxSize.Width);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_HEIGHT, sizes.size.Height);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_HEIGHT_MIN, sizes.minSize.Height);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_HEIGHT_MAX, sizes.maxSize.Height);

                var heights = FaceAlgorithms.HeightMinMax(f);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_K_F_AXES, heights.min);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_K_D_AXES, heights.max);

                // Update the path

                //Get PFace
                bool reverse = false;

                if (volume != null)
                {
                    var pface = f.PFaces.FirstOrDefault(x => x.Volume == volume);
                    if (pface != null)
                    {
                        if ((int)pface.Orientation * (int)f.Orientation == 1)
                            reverse = true;
                    }
                }

                List<Point3D> f_boundary = f.Boundary.Edges.Select(x => x.StartVertex.Position).ToList();
                if (reverse)
                    f_boundary.Reverse();

                //[0] because there can only be one instance for this type of connection
                this.DescriptiveSource.Instances[0].InstancePath = f_boundary;
            }
        }

        internal override void BeforeDeletion()
        {
            this.face.PropertyChanged -= Face_PropertyChanged;

            base.BeforeDeletion();
        }

        #endregion

    }
}
