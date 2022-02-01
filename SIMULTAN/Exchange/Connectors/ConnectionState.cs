using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SIMULTAN.DataExchange.Connectors
{
    /// <summary>
    /// Flags reflecting the state of the connection within a connector or when attempting to create one.
    /// </summary>
    [Flags]
    public enum ConnectionState
    {
        /// <summary>
        /// Connection OK
        /// </summary>
        OK = 0,
        /// <summary>
        /// There is no source component, the connector has no function
        /// </summary>
        SOURCE_COMPONENT_NULL = 1,
        /// <summary>
        /// There is no target geometry,  the connector has no function
        /// </summary>
        TARGET_GEOMETRY_NULL = 2,
        /// <summary>
        /// The declared component relationship to geometry and the type 
        /// of the target geometry do not match.
        /// </summary>
        SOURCE_COMPONENT_TARGET_GEOMETRY_MISMATCH = 4,
        /// <summary>
        /// The parent connector has no target geometry, this might not be an issue, just info
        /// </summary>
        TARGET_GEOMETRY_IN_PARENT_MISSING = 8,
        /// <summary>
        /// The parent connector has no source component, this might not be an issue, just info
        /// </summary>
        SOURCE_COMPONENT_IN_PARENT_MISSING = 16,
        /// <summary>
        /// The target geometries in the component, which is about to be connected, and its parent
        /// do not form an admissible geometry hierarchy.
        /// </summary>
        TARGET_GEOMETRIES_MISMATCH = 32,
        /// <summary>
        /// The source component, which is about to be connected, and its parent
        /// do not form an admissible component hierarchy for connection to geometry.
        /// </summary>
        SOURCE_COMPONENTS_MISMATCH = 64,
        /// <summary>
        /// The component, that is about to be connected to geometry, is already connected. 
        /// For components that can be connected to only one geometry instance.
        /// </summary>
        SOURCE_ALREADY_CONNECTED = 128,
        /// <summary>
        /// The component, that is about to be connected to the given geometry, is already connected to it.
        /// For components that can be connected to multiple geometry instances.
        /// </summary>
        SOURCE_ALREADY_CONNECTED_TO_TARGET = 256,
        /// <summary>
        /// The component, that is about to be connected to geometry, contains automatically
        /// created components from a possible previous connection. This might not be an issue, just info
        /// </summary>
        SOURCE_STRUCTURE_CONTAINS_CORPSES = 512
    }

    /// <summary>
    /// Info about the information synchronization between the component and geometry in a connector.
    /// </summary>
    public enum SynchronizationState
    {
        /// <summary>
        /// Some unknown or unexpected error occurred
        /// </summary>
        UNKNOWN = 0,
        /// <summary>
        /// OK
        /// </summary>
        SYNCHRONIZED,
        /// <summary>
        /// The component carries old information (when a change is triggered by the geometry)
        /// </summary>
        SOURCE_COMPONENT_NOT_UP_TO_DATE,
        /// <summary>
        /// The geometry carries old information (when a change is triggered by the component)
        /// </summary>
        TARGET_GEOMETRY_NOT_UP_TO_DATE,
        /// <summary>
        /// The source component is about to be deleted, the connector loses its function.
        /// </summary>
        SOURCE_COMPONENT_IS_BEING_DELETED,
        /// <summary>
        /// The target geometry is about to be deleted, the connector loses its function.
        /// </summary>
        TARGET_GEOMETRY_IS_BEING_DELETED
    }

    /// <summary>
    /// Used when managing connectors. Reflects only major changes (none, created, deleted, source deleted, target deleted).
    /// </summary>
    public enum ConnectionChange
    {
        /// <summary>
        /// Connector unchanged
        /// </summary>
        NONE = 0,
        /// <summary>
        /// Connector was just created
        /// </summary>
        CREATED,
        /// <summary>
        /// Connector is about to be deleted
        /// </summary>
        DELETED,
        /// <summary>
        /// The source component in the connector was deleted
        /// </summary>
        SOURCE_DELETED,
        /// <summary>
        /// The target geometry in the connector was deleted
        /// </summary>
        TARGET_DELETED
    }

    /// <summary>
    /// Flags, used by the ComponentGeometryExchange class, 
    /// reflecting the admissibility of dissolving a connection represented by a connector.
    /// </summary>
    [Flags]
    public enum DisconnectionState
    {
        /// <summary>
        /// Connection can be dissolved
        /// </summary>
        OK = 0,
        /// <summary>
        /// The connector could not be found
        /// </summary>
        CONNECTION_MISSING = 1,
        /// <summary>
        /// The connection is either missing its soource component or its target geometry
        /// </summary>
        CONNECTION_NOT_FUNCTIONAL = 2,
        /// <summary>
        /// The deletion of the connection will have consequences for other components
        /// </summary>
        CONNECTION_IMPACTS_OTHER_COMPONENTS = 4,
        /// <summary>
        /// The deletion of the connection will have consequences for other component instances
        /// </summary>
        CONNECTION_IMPACTS_OTHER_INSTANCES = 8,
        /// <summary>
        /// The deletion of the connection will have consequences for other geometry
        /// </summary>
        CONNECTION_IMPACTS_OTHER_GEOMETRY = 16
    }

    /// <summary>
    /// Evaluates the admissibility of a connection between component and geometry, 
    /// considering their properties as well as their respective context.
    /// </summary>
    public static class ConnectionStateEvaluator
    {
        /// <summary>
        /// Evaluates the state of a potential connection between 
        /// one component TYPE '_source' and one geometric instance'_target'.
        /// </summary>
        /// <param name="_parent_source">The component parent of the component to be connected</param>
        /// <param name="_parent_target">The geometry parent of the geometry to be connected</param>
        /// <param name="_source">the component to be connected</param>
        /// <param name="_target">the geometry to be connected</param>
        /// <returns></returns>
        public static ConnectionState Evaluate(SimComponent _parent_source, BaseGeometry _parent_target, SimComponent _source, BaseGeometry _target)
        {
            ConnectionState evaluation = ConnectionState.OK;

            if (_source == null)
                evaluation |= ConnectionState.SOURCE_COMPONENT_NULL;
            if (_target == null)
                evaluation |= ConnectionState.TARGET_GEOMETRY_NULL;

            if (_source != null && _target != null)
            {
                if (!ConnectionStateEvaluator.AreAMatch(_source.InstanceType, _target))
                    evaluation |= ConnectionState.SOURCE_COMPONENT_TARGET_GEOMETRY_MISMATCH;
            }

            evaluation |= ConnectionStateEvaluator.GetMatchState(_parent_source, _source);
            evaluation |= ConnectionStateEvaluator.GetMatchState(_parent_target, _target);

            return evaluation;
        }

        internal static bool AreAMatch(SimInstanceType instanceType, BaseGeometry _target)
        {
            switch (instanceType)
            {
                case SimInstanceType.None:
                    return false;
                case SimInstanceType.Entity3D:
                case SimInstanceType.GeometricVolume:
                    return (_target is Volume);
                case SimInstanceType.GeometricSurface:
                    return (_target is Face || _target is EdgeLoop || _target is Edge || _target is Vertex);
                case SimInstanceType.Attributes2D:
                    return (_target is Face);
                case SimInstanceType.NetworkNode:
                    return (_target is Volume || _target is Face);
                case SimInstanceType.NetworkEdge:
                case SimInstanceType.Group:
                case SimInstanceType.BuiltStructure:
                default:
                    return false;
            }
        }

        private static ConnectionState GetMatchState(SimComponent _parent, SimComponent _child)
        {
            if (_parent == null && _child == null)
            {
                return ConnectionState.OK;
            }
            else if (_parent == null)
            {
                SimInstanceType c_rel = _child.InstanceType;
                if (c_rel == SimInstanceType.GeometricVolume || c_rel == SimInstanceType.GeometricSurface)
                    return ConnectionState.SOURCE_COMPONENT_IN_PARENT_MISSING;
                else
                    return ConnectionState.OK;
            }
            else if (_child == null)
            {
                return ConnectionState.OK;
            }
            else
            {
                if (ConnectionStateEvaluator.AreAMatch(_parent, _child))
                    return ConnectionState.OK;
                else
                    return ConnectionState.SOURCE_COMPONENTS_MISMATCH;
            }
        }

        private static bool AreAMatch(SimComponent _parent, SimComponent _child)
        {
            SimInstanceType p_rel = _parent.InstanceType;
            SimInstanceType c_rel = _child.InstanceType;

            switch (p_rel)
            {
                case SimInstanceType.None:
                    return true;
                case SimInstanceType.Entity3D:
                    return (c_rel == SimInstanceType.None || c_rel == SimInstanceType.GeometricVolume || c_rel == SimInstanceType.GeometricSurface);
                case SimInstanceType.GeometricVolume:
                    return (c_rel == SimInstanceType.None || c_rel == SimInstanceType.GeometricVolume);
                case SimInstanceType.GeometricSurface:
                    return (c_rel == SimInstanceType.None || c_rel == SimInstanceType.GeometricSurface);
                case SimInstanceType.Attributes2D:
                    return (c_rel == SimInstanceType.None || c_rel == SimInstanceType.Attributes2D);
                case SimInstanceType.NetworkNode:
                case SimInstanceType.NetworkEdge:
                case SimInstanceType.Group:
                    return (c_rel == SimInstanceType.None);
                default:
                    return true;
            }
        }


        private static ConnectionState GetMatchState(BaseGeometry _parent, BaseGeometry _child)
        {
            if (_parent == null && _child == null)
            {
                return ConnectionState.OK;
            }
            else if (_parent == null)
            {
                if (_child is Volume)
                    return ConnectionState.OK;
                else
                    return ConnectionState.TARGET_GEOMETRY_IN_PARENT_MISSING;
            }
            else if (_child == null)
            {
                return ConnectionState.OK;
            }
            else
            {
                if (ConnectionStateEvaluator.AreAMatchInComponent(_parent, _child))
                    return ConnectionState.OK;
                else
                    return ConnectionState.TARGET_GEOMETRIES_MISMATCH;
            }
        }

        private static bool AreAMatchInComponent(BaseGeometry _parent, BaseGeometry _child)
        {
            return ((_parent is Volume && _child is Volume) ||
                    (_parent is Volume && _child is Face) ||
                    (_parent is Face && _child is Face) ||
                    (_parent is Face && _child is EdgeLoop) ||
                    (_parent is Volume && _child is Edge) ||
                    (_parent is Volume && _child is Vertex));
        }
    }
}