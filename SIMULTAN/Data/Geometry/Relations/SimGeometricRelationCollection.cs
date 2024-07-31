using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Exchange.SimNetworkConnectors;
using SIMULTAN.Projects;
using SIMULTAN.Utils;
using SIMULTAN.Utils.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Event args for when a <see cref="SimGeometryRelation"/> got changed
    /// </summary>
    public class GeometryRelationChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The relation that got changed.
        /// </summary>
        public SimGeometryRelation Relation { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeometryRelationChangedEventArgs"/> class.
        /// </summary>
        /// <param name="relation">The relation.</param>
        public GeometryRelationChangedEventArgs(SimGeometryRelation relation)
        {
            this.Relation = relation;
        }
    }

    /// <summary>
    /// Managed Collection for <see cref="SimGeometryRelation"/>
    /// </summary>
    public class SimGeometryRelationCollection : SimManagedCollection<SimGeometryRelation>
    {
        /// <summary>
        /// If the project is currently being loaded
        /// </summary>
        public bool IsLoading { get; private set; }

        private MultiDictionary<SimBaseGeometryReference, SimGeometryRelation> relationLookup;

        /// <summary>
        /// Creates a new <see cref="SimGeometryRelationCollection"/>
        /// </summary>
        /// <param name="owner"></param>
        public SimGeometryRelationCollection(ProjectData owner) : base(owner)
        {
            relationLookup = new MultiDictionary<SimBaseGeometryReference, SimGeometryRelation>();
        }

        #region Collection overrides

        /// <inheritdoc />
        protected override void InsertItem(int index, SimGeometryRelation item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (item.Factory != null)
                throw new ArgumentException("SimGeometryRelation already belongs to a factory");

            SetValues(item);
            RegisterRelation(item);
            base.InsertItem(index, item);
        }

        /// <inheritdoc />
        protected override void RemoveItem(int index)
        {
            var oldItem = this[index];
            UnsetValues(oldItem);
            UnregisterRelation(oldItem);
            base.RemoveItem(index);
        }

        /// <inheritdoc />
        protected override void ClearItems()
        {
            this.ForEach(x =>
            {
                UnsetValues(x);
                UnregisterRelation(x);
            });
            base.ClearItems();
        }

        /// <inheritdoc />
        protected override void SetItem(int index, SimGeometryRelation item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (item.Factory != null)
                throw new ArgumentException("SimGeometryRelation already belongs to a factory");

            var oldItem = this[index];
            UnsetValues(oldItem);
            UnregisterRelation(oldItem);

            SetValues(item);
            RegisterRelation(item);
            base.SetItem(index, item);
        }

        #endregion

        private void UnsetValues(SimGeometryRelation relation)
        {
            if (ProjectData.IdGenerator != null)
                ProjectData.IdGenerator.Remove(relation);

            // remove location from SimId but key global/local Ids
            relation.Id = new SimId(relation.Id.GlobalId, relation.Id.LocalId);
            relation.Factory = null;
        }

        private void SetValues(SimGeometryRelation relation)
        {
            if (relation.Factory != null)
                throw new ArgumentException("Geometric relation cannot be added to another geometric relation collection");

            if (relation.Id == SimId.Empty)
            {
                relation.Id = ProjectData.IdGenerator.NextId(relation, CalledFromLocation);
            }
            else
            {
                if (IsLoading)
                {
                    relation.Id = new SimId(CalledFromLocation, relation.Id.LocalId);
                    ProjectData.IdGenerator.Reserve(relation, relation.Id);
                }
                else
                {
                    if (relation.Id.GlobalId != Guid.Empty && relation.Id.GlobalId != CalledFromLocation.GlobalID)
                    {
                        throw new NotSupportedException("Relation may only be added from the same project, otherwise reset IDs!");
                    }
                    else
                    {
                        relation.Id = new SimId(CalledFromLocation, relation.Id.LocalId);
                        ProjectData.IdGenerator.Reserve(relation, relation.Id);
                    }
                }
            }

            relation.Factory = this;
        }

        /// <summary>
        /// Call when loading of the project started
        /// </summary>
        public void StartLoading()
        {
            IsLoading = true;
        }

        /// <summary>
        /// Call when loading of the project stopped
        /// </summary>
        public void StopLoading()
        {
            IsLoading = false;
        }

        private void RegisterRelation(SimGeometryRelation relation)
        {
            relationLookup.Add(relation.Source, relation);
            relationLookup.Add(relation.Target, relation);
        }

        private void UnregisterRelation(SimGeometryRelation relation)
        {
            relationLookup.Remove(relation.Source, relation);
            relationLookup.Remove(relation.Target, relation);
        }

        /// <summary>
        /// Returns all the relations associated with this BaseGeometry.
        /// </summary>
        /// <param name="reference">The geometry reference</param>
        /// <returns>All the relations associated with this BaseGeometry</returns>
        public IEnumerable<SimGeometryRelation> GetRelationsOf(SimBaseGeometryReference reference)
        {
            if (relationLookup.TryGetValues(reference, out var values))
            {
                return values;
            }
            return new List<SimGeometryRelation>();
        }

        /// <summary>
        /// Returns all the relations associated with this BaseGeometry.
        /// </summary>
        /// <param name="baseGeometry">The geometry</param>
        /// <returns>All the relations associated with this BaseGeometry</returns>
        public IEnumerable<SimGeometryRelation> GetRelationsOf(BaseGeometry baseGeometry)
        {
            var reference = new SimBaseGeometryReference(this.ProjectData.Owner.GlobalID, baseGeometry);
            return GetRelationsOf(reference);
        }

        /// <summary>
        /// Returns all relations associated with the given <see cref="GeometryModel"/>.
        /// </summary>
        /// <param name="geometryModel">The geometry model to get the relations for</param>
        /// <returns>All relations associated with the given <see cref="GeometryModel"/>.</returns>
        public HashSet<SimGeometryRelation> GetRelationsOf(GeometryModel geometryModel)
        {
            return relationLookup.Where(x => x.Key.ProjectId == ProjectData.Owner.GlobalID && x.Key.FileId == geometryModel.File.Key).
                SelectMany(x => x.Value).ToHashSet();
        }

        /// <summary>
        /// Returns relations that originate from the provided <see cref="BaseGeometry"/>.
        /// So the relations where this geometry is the <see cref="SimGeometryRelation.Source"/>.
        /// </summary>
        /// <param name="fromGeometry">The from geometry.</param>
        /// <returns>Relations that originate from the provided <see cref="BaseGeometry"/>.</returns>
        public IEnumerable<SimGeometryRelation> GetRelationsFrom(BaseGeometry fromGeometry)
        {
            var reference = new SimBaseGeometryReference(this.ProjectData.Owner.GlobalID, fromGeometry);
            return GetRelationsOf(reference).Where(x => x.Source.Equals(reference));
        }


        /// <summary>
        /// Returns relations that target the provided <see cref="BaseGeometry"/>.
        /// So the relations where this geometry is the <see cref="SimGeometryRelation.Target"/>.
        /// </summary>
        /// <param name="toGeometry">The to geometry.</param>
        /// <returns>Relations that target the provided <see cref="BaseGeometry"/>.</returns>
        public IEnumerable<SimGeometryRelation> GetRelationsTo(BaseGeometry toGeometry)
        {
            var reference = new SimBaseGeometryReference(this.ProjectData.Owner.GlobalID, toGeometry);
            return GetRelationsOf(reference).Where(x => x.Target.Equals(reference));
        }

        /// <summary>
        /// Delegate for the <see cref="GeometryRelationChanged"/> event
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The event args.</param>
        public delegate void GeometryRelationChangedHandler(object sender, GeometryRelationChangedEventArgs args);
        /// <summary>
        /// Event that gets called when one of the <see cref="SimGeometryRelationCollection"/>'s relations got changed.
        /// Used so it is not necessary to hook up an event to each relation independently.
        /// </summary>
        public event GeometryRelationChangedHandler GeometryRelationChanged;

        /// <summary>
        /// Called to signal the <see cref="GeometryRelationChanged"/> event.
        /// </summary>
        /// <param name="relation">The relation that got changed.</param>
        public void OnGeometryRelationChanged(SimGeometryRelation relation)
        {
            GeometryRelationChanged?.Invoke(this, new GeometryRelationChangedEventArgs(relation));
        }

        /// <summary>
        /// Restores all taxonomy entry references after the default taxonomies were updated.
        /// </summary>
        public void RestoreDefaultTaxonomyReferences()
        {
            foreach (var relation in this)
            {
                if (relation.RelationType != null)
                {
                    var entry = ProjectData.IdGenerator.GetById<SimTaxonomyEntry>(relation.RelationType.TaxonomyEntryId);
                    relation.RelationType = new SimTaxonomyEntryReference(entry);
                }
            }
        }
    }
}
