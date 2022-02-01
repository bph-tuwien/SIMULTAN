using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Flags for permission on BaseGeometries
    /// </summary>
    [Flags]
    public enum GeometryOperationPermissions : UInt64
    {
        /// <summary>
        /// No interaction with the model is allowed
        /// </summary>
        None = 0,

        //Basic operations on geometry
        /// <summary>
        /// The user can select geometry
        /// </summary>
        Select = (1 << 0),
        /// <summary>
        /// The user can move geometry (grab, scale, rotate)
        /// </summary>
        Move = (1 << 1) + Select, //Move is only working with select enabled
        /// <summary>
        /// The user can delete geometry
        /// </summary>
        Delete = (1 << 2) + Select, //Deleting requires select
        /// <summary>
        /// The user can create new geometry (covers extrude and polyline drawing)
        /// </summary>
        Create = (1 << 3), // Create (aka polyline) requires extrude
        /// <summary>
        /// The user can split existing edges
        /// </summary>
        Split = (1 << 4) + Select,
        /// <summary>
        /// The user can remove points from EdgeLoops and Polylines
        /// </summary>
        Unsplit = (1 << 5) + Select,
        /// <summary>
        /// The user can move geometry to a new layer
        /// </summary>
        ModifyLayer = (1 << 6) + Select,
        /// <summary>
        /// The user can change the name of a Geometry
        /// </summary>
        ModifyName = (1 << 7) + Select,
        /// <summary>
        /// The user can modify the color of a Geometry
        /// </summary>
        ModifyColor = (1 << 8) + Select,
        /// <summary>
        /// The user can set/unset the parent of a BaseGeometry
        /// </summary>
        ModifyParent = (1 << 10) + Select,

        /// <summary>
        /// The user can do everything
        /// </summary>
        All = ~((UInt64)0)
    }

    /// <summary>
    /// Flags for permissions on layer
    /// </summary>
    public enum LayerOperationPermissions : UInt64
    {
        /// <summary>
        /// No interaction with the model is allowed
        /// </summary>
        None = 0,

        /// <summary>
        /// The user can create new layer
        /// </summary>
        Create = (1 << 0),
        /// <summary>
        /// The user can delete layer
        /// </summary>
        Delete = (1 << 1),
        /// <summary>
        /// The user can modify the color of a layer
        /// </summary>
        ModifyColor = (1 << 2),
        /// <summary>
        /// The user can modify the layer structure (move layer to other parent)
        /// </summary>
        ModifyTopolgy = (1 << 3),
        /// <summary>
        /// The user can modify layer names
        /// </summary>
        ModifyName = (1 << 4),

        /// <summary>
        /// The user can do everything
        /// </summary>
        All = ~((UInt64)0)
    }

    /// <summary>
    /// Flags for permissions on GeometryModels and linked Models
    /// </summary>
    public enum GeometryModelOperationPermissions : UInt64
    {
        /// <summary>
        /// No interaction with the model is allowed
        /// </summary>
        None = 0,

        /// <summary>
        /// The user can remove linked models
        /// </summary>
        RemoveLinked = (1 << 0),
        /// <summary>
        /// The user can link additional models to this model (also includes linking networks)
        /// </summary>
        AddLinked = (1 << 1),

        /// <summary>
        /// The user can do everything
        /// </summary>
        All = ~((UInt64)0)
    }


    /// <summary>
    /// Stores the permissions for a GeometryModel
    /// </summary>
    public class OperationPermission
    {
        /// <summary>
        /// Returns a permission set that allows the user to do nothing
        /// </summary>
        public static OperationPermission None
        {
            get
            {
                return new OperationPermission(GeometryModelOperationPermissions.None, GeometryOperationPermissions.None, LayerOperationPermissions.None);
            }
        }
        /// <summary>
        /// Returns the default permission set for wall models (everything allowed)
        /// </summary>
        public static OperationPermission DefaultWallModelPermissions
        {
            get
            {
                return new OperationPermission(
                    GeometryModelOperationPermissions.AddLinked | GeometryModelOperationPermissions.RemoveLinked,
                    GeometryOperationPermissions.Select | GeometryOperationPermissions.Move | GeometryOperationPermissions.Delete |
                    GeometryOperationPermissions.Create | GeometryOperationPermissions.Split | GeometryOperationPermissions.Unsplit |
                    GeometryOperationPermissions.ModifyLayer | GeometryOperationPermissions.ModifyName | GeometryOperationPermissions.ModifyColor |
                    GeometryOperationPermissions.ModifyParent,
                    LayerOperationPermissions.Create | LayerOperationPermissions.Delete | LayerOperationPermissions.ModifyColor |
                    LayerOperationPermissions.ModifyName | LayerOperationPermissions.ModifyTopolgy
                    );
            }
        }
        /// <summary>
        /// Returns the default permission set for Network models
        /// </summary>
        public static OperationPermission DefaultNetworkPermissions
        {
            get
            {
                return new OperationPermission(
                    GeometryModelOperationPermissions.None,
                    GeometryOperationPermissions.Move | GeometryOperationPermissions.Split | GeometryOperationPermissions.Unsplit |
                    GeometryOperationPermissions.ModifyName | GeometryOperationPermissions.ModifyParent,
                    LayerOperationPermissions.ModifyColor
                    );
            }
        }

        /// <summary>
        /// Returns a permission set that allows everything
        /// </summary>
        public static OperationPermission All
        {
            get
            {
                return new OperationPermission(
                    GeometryModelOperationPermissions.All, GeometryOperationPermissions.All, LayerOperationPermissions.All
                    );
            }
        }

        /// <summary>
        /// Stores the permissions for BaseGeometries
        /// </summary>
        public GeometryOperationPermissions GeometryPermissions { get; private set; }
        /// <summary>
        /// Stores the permissions for layer
        /// </summary>
        public LayerOperationPermissions LayerPermissions { get; private set; }
        /// <summary>
        /// Stores the permission for GeometryModels (and linked models)
        /// </summary>
        public GeometryModelOperationPermissions ModelPermissions { get; private set; }

        /// <summary>
        /// Initializes a new instance of the OperationPermission class
        /// </summary>
        /// <param name="modelPermissions">The GeometryModel permissions</param>
        /// <param name="geometryPermissions">The BaseGeometry permissions</param>
        /// <param name="layerPermissions">The Layer permissions</param>
        public OperationPermission(GeometryModelOperationPermissions modelPermissions,
            GeometryOperationPermissions geometryPermissions, LayerOperationPermissions layerPermissions)
        {
            this.GeometryPermissions = geometryPermissions;
            this.LayerPermissions = layerPermissions;
            this.ModelPermissions = modelPermissions;
        }
    }
}
