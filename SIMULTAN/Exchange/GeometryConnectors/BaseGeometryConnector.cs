using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Exchange.GeometryConnectors
{
    /// <summary>
    /// Base class for all specific geometry connectors.
    /// Geometry connectors are used to synchronize a single component instance with it's appropriate geometry
    /// </summary>
    internal abstract class BaseGeometryConnector
    {
        /// <summary>
        /// Returns the geometry associated with the instance
        /// </summary>
        internal abstract BaseGeometry Geometry { get; }

        /// <summary>
        /// The instance placement
        /// </summary>
        internal SimInstancePlacementGeometry Placement { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseGeometryConnector"/> class
        /// </summary>
        /// <param name="placement">The geometry placement</param>
        protected BaseGeometryConnector(SimInstancePlacementGeometry placement)
        {
            if (placement == null)
                throw new ArgumentNullException(nameof(placement));
            if (placement.Instance == null || placement.Instance.Component == null)
                throw new ArgumentException("Placement has to be attached to a component");

            this.Placement = placement;
            this.Placement.State = SimInstancePlacementState.Valid;
        }

        /// <summary>
        /// This method gets called when the <see cref="GeometryModelData.GeometryChanged"/> event has been invoked
        /// for the <see cref="Geometry"/> stored in this connector
        /// </summary>
        internal abstract void OnGeometryChanged();
        /// <summary>
        /// This method gets called when the <see cref="GeometryModelData.TopologyChanged"/> event has been invoked
        /// for the <see cref="Geometry"/> stored in this connector
        /// </summary>
        internal abstract void OnTopologyChanged();

        /// <summary>
        /// This method gets called when the <see cref="Geometry"/> instance should be changed. This mainly happens when
        /// the <see cref="GeometryModelData"/> has been replaced. The new geometry will have the same Id, but will not
        /// be the same <see cref="BaseGeometry"/> instance.
        /// </summary>
        /// <param name="geometry">The new geometry instance</param>
        /// <returns>True when the new base geometry fits to the connector. Otherwise False</returns>
        internal abstract bool ChangeBaseGeometry(BaseGeometry geometry);
        /// <summary>
        /// This method gets called when the <see cref="Geometry"/> instance should be changed. This mainly happens when
        /// the <see cref="GeometryModelData"/> has been replaced. The new geometry will have the same Id, but will not
        /// be the same <see cref="BaseGeometry"/> instance.
        /// This method is called when the <see cref="ChangeBaseGeometry(BaseGeometry)"/> method is called and allows subclasses to react
        /// to the change
        /// </summary>
        /// <param name="oldGeometry">The old geometry instance</param>
        /// <param name="newGeometry">The new geometry instance</param>
        protected abstract void OnTargetGeometryChanged(BaseGeometry oldGeometry, BaseGeometry newGeometry);

        /// <summary>
        /// Called when the instance placement represented by this connector is removed from the component model.
        /// Also called when the instance is removed or when the component is removed.
        /// </summary>
        internal abstract void OnPlacementRemoved();
        /// <summary>
        /// Called when the <see cref="Geometry"/> has been deleted. Either because the <see cref="GeometryModelData.GeometryRemoved"/>
        /// event has been invoked, or because the <see cref="GeometryModelData"/> has been replaced and the geometry is missing.
        /// </summary>
        internal virtual void OnGeometryRemoved()
        {
            this.Placement.State = SimInstancePlacementState.InstanceTargetMissing;
        }
        /// <summary>
        /// Called when the name of a BaseGeometry has changed
        /// </summary>
        /// <param name="geometry">The geometry in which the name has changed</param>
        internal virtual void OnGeometryNameChanged(BaseGeometry geometry) { }

        /// <summary>
        /// Called when the connector has been added to the connector lookup tables.
        /// Use this method to initially update parameter values.
        /// </summary>
        internal abstract void OnConnectorsInitialized();
    }

    /// <summary>
    /// Base class for all specific geometry connectors. This instance adds a typed geometry property
    /// Geometry connectors are used to synchronize a single component instance with it's appropriate geometry
    /// </summary>
    internal abstract class BaseGeometryConnector<T> : BaseGeometryConnector
        where T : BaseGeometry
    {
        /// <inheritdoc />
        internal sealed override BaseGeometry Geometry
        {
            get
            {
                return geometry;
            }
        }

        /// <summary>
        /// A more specificly typed Geometry
        /// </summary>
        internal T TypedGeometry 
        {
            get { return geometry; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                var old = geometry;
                this.geometry = value;

                OnTargetGeometryChanged(old, this.geometry);
            }
        }
        private T geometry;

        /// <inheritdoc />
        internal override sealed bool ChangeBaseGeometry(BaseGeometry geometry)
        {
            if (geometry is T tgeom)
            {
                TypedGeometry = tgeom;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseGeometryConnector{T}"/> class
        /// </summary>
        /// <param name="geometry">The geometry represented by this connector</param>
        /// <param name="placement">The component instance represented by this connector</param>
        /// <exception cref="ArgumentNullException"></exception>
        protected BaseGeometryConnector(T geometry, SimInstancePlacementGeometry placement)
            : base(placement)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));

            //Do not call setter of TypedGeometry to prevent OnTargetGeometryChanged from being called 
            this.geometry = geometry;
        }
    }
}
