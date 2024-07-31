using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Projects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Propagates geometric parameters to a parameter.
    /// The geometric property is defined by the <see cref="GeometryProperty"/> property
    /// Geometric properties are only updated when the affected <see cref="GeometryModel"/> is loaded.
    /// </summary>
    public class SimGeometryParameterSource : SimParameterValueSource
    {
        #region Properties

        /// <summary>
        /// The geometric property that should be written to the parameter. Aggregation over multiple instance is done depending on the property.
        /// </summary>
        public SimGeometrySourceProperty GeometryProperty { get; }
        /// <inheritdoc/>
        public override SimBaseParameter TargetParameter
        {
            get
            {
                return base.TargetParameter;
            }
            internal set
            {
                base.TargetParameter = value;

                if (value != null)
                {
                    value.InstancePropagationMode = SimParameterInstancePropagation.PropagateNever;
                    UpdateAllInstanceFilters();
                }
            }
        }

        /// <summary>
        /// Tags used to filter geometry files, all the tags have to match.
        /// </summary>
        public SimTaxonomyEntryReferenceCollection FilterTags
        {
            get;
        }

        private void FilterTags_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateAllInstanceFilters();
            if (TargetParameter != null && this.TargetParameter.Component != null && this.TargetParameter.Component.Factory != null)
                this.TargetParameter.Component.Factory.ProjectData.ComponentGeometryExchange.OnParameterSourceFilterChanged(this);
        }

        /// <summary>
        /// Dictionary of relevant instances and if they pass the sources filter tags.
        /// </summary>
        public Dictionary<SimComponentInstance, bool> InstancesPassingFilter { get; }

        #endregion

        #region .CTOR

        private SimGeometryParameterSource()
        {
            this.InstancesPassingFilter = new Dictionary<SimComponentInstance, bool>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimGeometrySourceProperty" /> class
        /// </summary>
        /// <param name="geometryProperty">The geometric property that should be written to the parameter</param>
        public SimGeometryParameterSource(SimGeometrySourceProperty geometryProperty)
            : this()
        {
            this.GeometryProperty = geometryProperty;
            this.FilterTags = new SimTaxonomyEntryReferenceCollection();
            this.FilterTags.CollectionChanged += FilterTags_CollectionChanged;
        }

        /// <summary>
        /// Creates a deep clone of the original.
        /// </summary>
        /// <param name="original">The original parameter source</param>
        public SimGeometryParameterSource(SimGeometryParameterSource original)
            : this()
        {
            this.GeometryProperty = original.GeometryProperty;
            this.FilterTags = new SimTaxonomyEntryReferenceCollection(original.FilterTags.Select(x => new SimTaxonomyEntryReference(x)));
            this.FilterTags.CollectionChanged += FilterTags_CollectionChanged;
        }

        #endregion

        #region Instance Filter

        /// <summary>
        /// Returns if the given instance passes the tag filtering.
        /// </summary>
        /// <param name="instance">The instance to test</param>
        /// <returns>True if the instance passes the tag filter.</returns>
        public bool InstancePassesFilter(SimComponentInstance instance)
        {
            if (InstancesPassingFilter.TryGetValue(instance, out bool result))
            {
                return result;
            }
            return false;
        }

        /// <summary>
        /// Updates the <see cref="InstancesPassingFilter"/> lookup for the given instance.
        /// </summary>
        /// <param name="instance">The instance to update the filter lookup for.</param>
        public void UpdateInstanceFilter(SimComponentInstance instance)
        {
            if (TargetParameter != null && TargetParameter.Factory != null && TargetParameter.Factory.ProjectData != null &&
                !this.TargetParameter.Factory.ProjectData.Components.IsLoading)
            {
                // only need to test one geo placement, as one instance can only be associated with one geometry file
                bool pass = false;
                var placement = instance.Placements.FirstOrDefault(x => x is SimInstancePlacementGeometry);
                if (placement != null)
                {
                    var geoPlacement = (SimInstancePlacementGeometry)placement;
                    var resource = instance.Component.Factory.ProjectData.AssetManager.GetResource(geoPlacement.FileId) as ResourceFileEntry;

                    if (resource != null && resource.Tags.All(x => x.Target != null))
                    {
                        var resourceTags = resource.Tags.ToHashSet();
                        pass = FilterTags.All(x => resourceTags.Contains(x));
                    }
                }
                InstancesPassingFilter[instance] = pass;
            }
        }

        /// <summary>
        /// Updates the <see cref="InstancesPassingFilter"/> lookup for all instances of the target parameter.
        /// </summary>
        public void UpdateAllInstanceFilters()
        {
            if (TargetParameter != null && TargetParameter.Factory != null && TargetParameter.Factory.ProjectData != null &&
                !this.TargetParameter.Factory.ProjectData.Components.IsLoading)
            {
                InstancesPassingFilter.Clear();
                if (TargetParameter != null && TargetParameter.Component != null)
                {
                    foreach (var instance in TargetParameter.Component.Instances)
                    {
                        UpdateInstanceFilter(instance);
                    }
                }
            }
        }

        /// <summary>
        /// To notfiy the parameter source that the filter on its associated resource entry has changed.
        /// Updates the instance filter and connectors.
        /// </summary>
        /// <param name="entry">The resource entry that changed</param>
        public void NotifyResourceEntryFilterChanged(ResourceEntry entry)
        {
            UpdateAllInstanceFilters();
            TargetParameter?.Factory.ProjectData.ComponentGeometryExchange.OnParameterSourceFilterChanged(this);
        }

        #endregion

        #region SimParameterValueSource

        /// <inheritdoc/>
        public override SimParameterValueSource Clone()
        {
            return new SimGeometryParameterSource(this);
        }

        /// <inheritdoc />
        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            FilterTags.CollectionChanged -= FilterTags_CollectionChanged;
        }

        /// <summary>
        /// Restores all taxonomy entry references after the default taxonomies were updated.
        /// </summary>
        /// <param name="projectData">The ProjectData</param>
        public override void RestoreDefaultTaxonomyReferences(ProjectData projectData)
        {
            base.RestoreDefaultTaxonomyReferences(projectData);
            FilterTags.RestoreDefaultTaxonomyReferences(projectData);
        }
        #endregion
    }
}
