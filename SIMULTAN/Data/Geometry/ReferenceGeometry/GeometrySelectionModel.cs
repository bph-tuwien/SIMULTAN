using SIMULTAN;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Event args for the ActiveChanged event
    /// </summary>
    public class ActiveGeometryChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The old active geometry
        /// </summary>
        public BaseGeometry OldValue { get; private set; }
        /// <summary>
        /// The new active geometry
        /// </summary>
        public BaseGeometry NewValue { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ActiveGeometryChangedEventArgs class
        /// </summary>
        /// <param name="oldValue">The old active geometry</param>
        /// <param name="newValue">The new active geometry</param>
        public ActiveGeometryChangedEventArgs(BaseGeometry oldValue, BaseGeometry newValue)
        {
            this.OldValue = oldValue;
            this.NewValue = newValue;
        }
    }

    /// <summary>
    /// Provides methods to select geometries
    /// 
    /// Usage Hints: If possible use the Select/Deselect methods with lists of objects.
    /// Do not iterate over a geometry list and call Select on each element since this will issue a SelectionChanged Event per Select-call.
    /// SelectionChanged is issued whenever a Select/Deselect operation has finished (once per operation).
    /// </summary>
    public class GeometrySelectionModel
    {
        /// <summary>
        /// A list of all selected geometries
        /// </summary>
        public IReadOnlyCollection<BaseGeometry> SelectedGeometry { get { return selectedGeometry; } }
        private HashSet<BaseGeometry> selectedGeometry;

        /// <summary>
        /// Gets or sets the active geometry
        /// </summary>
        public BaseGeometry ActiveGeometry
        {
            get
            {
                return activeGeometry;
            }
            private set
            {
                var oldVal = activeGeometry;
                activeGeometry = value;
                ActiveChanged?.Invoke(this, new ActiveGeometryChangedEventArgs(oldVal, value));
            }
        }
        private BaseGeometry activeGeometry;

        private ObservableCollection<GeometryModel> models;

        #region SelectionChanged

        /// <summary>
        /// Enumeration for reasons why the selection has changed
        /// </summary>
        public enum SelectionChangedReason
        {
            /// <summary>
            /// The user has changed the selection
            /// </summary>
            User,
            /// <summary>
            /// The selection has not really changed by the undo-operation has exchanged geometry
            /// </summary>
            UndoRedo
        }

        /// <summary>
        /// Event args for the SelectionChanged event
        /// </summary>
        public class SelectionChangedEventArgs : EventArgs
        {
            /// <summary>
            /// Initializes a new instance of the SelectionChangedEventArgs class
            /// </summary>
            /// <param name="reason">The reason for the selection change</param>
            /// <param name="added">A list of all additionally selected objects (may be null)</param>
            /// <param name="removed">A list of all deselected objects (may be null)</param>
            public SelectionChangedEventArgs(SelectionChangedReason reason, IEnumerable<BaseGeometry> added, IEnumerable<BaseGeometry> removed)
            {
                this.Reason = reason;
                this.Added = added;
                this.Removed = removed;
            }

            /// <summary>
            /// The reason for the selection change
            /// </summary>
            public SelectionChangedReason Reason { get; private set; }
            /// <summary>
            /// A list of all additionally selected objects (may be null)
            /// </summary>
            public IEnumerable<BaseGeometry> Added { get; private set; }
            /// <summary>
            /// A list of all deselected objects (may be null)
            /// </summary>
            public IEnumerable<BaseGeometry> Removed { get; private set; }
        }
        /// <summary>
        /// EventHandler for the SelectionChanged event
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The arguments for this event</param>
        public delegate void SelectionChangedEventHandler(object sender, SelectionChangedEventArgs args);
        /// <summary>
        /// Invoked whenever the selection changes
        /// </summary>
        public event SelectionChangedEventHandler SelectionChanged;

        #endregion

        /// <summary>
        /// Initializes a new instance of the GeometrySelectionModel class
        /// </summary>
        public GeometrySelectionModel(ObservableCollection<GeometryModel> models)
        {
            if (models == null)
                throw new ArgumentNullException(nameof(models));

            models.CollectionChanged += Models_CollectionChanged;

            this.models = models;
            this.selectedGeometry = new HashSet<BaseGeometry>();

            foreach (var model in models)
            {
                model.Replaced += this.GeometryModel_Replaced;
                ConnectEvents(model.Geometry);
            }
        }


        private void GeometryModel_Replaced(object sender, GeometryModelReplacedEventArgs e)
        {
            List<BaseGeometry> oldItems = new List<BaseGeometry>();
            List<BaseGeometry> newItems = new List<BaseGeometry>();

            var geometriesToReplace = this.selectedGeometry.Where(x => x.ModelGeometry == e.OldGeometry).ToList();

            foreach (var geo in geometriesToReplace)
                this.selectedGeometry.Remove(geo);

            var replaceGeometries = geometriesToReplace.Select(x => e.NewGeometry.GeometryFromId(x.Id)).Where(x => x != null);
            foreach (var geo in replaceGeometries)
                this.selectedGeometry.Add(geo);

            oldItems.AddRange(geometriesToReplace);
            newItems.AddRange(replaceGeometries);

            //Replace active geometry
            if (this.activeGeometry != null && this.activeGeometry.ModelGeometry == e.OldGeometry)
                this.ActiveGeometry = e.NewGeometry.GeometryFromId(activeGeometry.Id);

            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(SelectionChangedReason.UndoRedo, newItems, oldItems));
        }

        private void Models_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var item in e.OldItems)
                    DisconnectEvents((GeometryModel)item);

                //Remove old elements
                MakeConsistent();
            }
            else if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                    ConnectEvents((GeometryModel)item);
            }
            else
                throw new NotSupportedException("Operation not supported");
        }

        private void Geometry_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                List<BaseGeometry> deselect = new List<BaseGeometry>();
                foreach (var i in e.OldItems)
                {
                    if (this.selectedGeometry.Contains(i))
                        deselect.Add(i as BaseGeometry);
                }

                if (deselect.Count > 0)
                    Deselect(deselect);
            }
        }

        /// <summary>
        /// Event handler delegate for the ActiveChanged event
        /// </summary>
        /// <param name="sender">The sending object</param>
        /// <param name="args">The event arguments</param>
        public delegate void ActiveGeometryChangedEventHandler(object sender, ActiveGeometryChangedEventArgs args);
        /// <summary>
        /// Invoked when the active geometry has changed
        /// </summary>
        public event ActiveGeometryChangedEventHandler ActiveChanged;


        /// <summary>
        /// Selects a BaseGeometry
        /// </summary>
        /// <param name="geometry">The geometry</param>
        /// <param name="clearSelection">When true, the list of selected objects is cleared</param>
        public void Select(BaseGeometry geometry, bool clearSelection)
        {
            Select(new BaseGeometry[] { geometry }, clearSelection);
        }

        /// <summary>
        /// Selects a number of geometries
        /// </summary>
        /// <param name="geometry">The geometries to select</param>
        /// <param name="clearSelection">When set to true, the SelectionModel is cleared before selecting the new geometries</param>
        public void Select(IEnumerable<BaseGeometry> geometry, bool clearSelection)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));

            IEnumerable<BaseGeometry> deselected = null;
            List<BaseGeometry> newSelectedGeometry = geometry.Where(x => x.ModelGeometry.Model.Permissions.GeometryPermissions.HasFlag(GeometryOperationPermissions.Select)).ToList();

            if (newSelectedGeometry.Count > 0)
            {
                if (clearSelection && selectedGeometry.Any())
                {
                    deselected = selectedGeometry.Where(x => !newSelectedGeometry.Contains(x)).ToList();
                    selectedGeometry.Clear();
                }

                ActiveGeometry = newSelectedGeometry.First();
                foreach (var g in newSelectedGeometry)
                    if (!selectedGeometry.Contains(g))
                        selectedGeometry.Add(g);
            }
            else
            {
                if (clearSelection && selectedGeometry.Any())
                {
                    deselected = selectedGeometry.ToList();
                    selectedGeometry.Clear();
                }
                newSelectedGeometry = null;
            }

            if (selectedGeometry.Count == 0)
            {
                ActiveGeometry = null;
            }

            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(SelectionChangedReason.User, newSelectedGeometry, deselected));
        }

        /// <summary>
        /// Selects everything inside a GeometryModel
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="clearSelection">When set to true, the selection is cleared</param>
        public void Select(GeometryModelData model, bool clearSelection)
        {
            List<BaseGeometry> selectGeometry = new List<BaseGeometry>(model.Vertices);
            selectGeometry.AddRange(model.Edges);
            selectGeometry.AddRange(model.Faces);
            selectGeometry.AddRange(model.Volumes);

            Select(selectGeometry, clearSelection);
        }

        /// <summary>
        /// Unselects a Geometry
        /// </summary>
        /// <param name="geometry">The geometry</param>
        public void Deselect(BaseGeometry geometry)
        {
            Deselect(new BaseGeometry[] { geometry });
        }
        /// <summary>
        /// Deselects everything inside a GeometryModel
        /// </summary>
        /// <param name="model">The model</param>
        public void Deselect(GeometryModelData model)
        {
            ActiveGeometry = null;

            var deselect = this.selectedGeometry.Where(x => x.ModelGeometry == model).ToList();
            this.selectedGeometry.RemoveWhere(x => x.ModelGeometry == model);

            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(SelectionChangedReason.User, null, deselect));
        }
        /// <summary>
        /// Deselects all elements from a list
        /// </summary>
        /// <param name="geometry">List of geometries to deselect</param>
        public void Deselect(IEnumerable<BaseGeometry> geometry)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));

            foreach (var g in geometry)
                selectedGeometry.Remove(g);

            if (geometry.Contains(ActiveGeometry))
                ActiveGeometry = selectedGeometry.LastOrDefault();

            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(SelectionChangedReason.User, null, geometry));
        }

        /// <summary>
        /// Toggles the selection state for a Geometry
        /// </summary>
        /// <param name="geometry">The geometry</param>
        public void ToggleSelection(BaseGeometry geometry)
        {
            if (IsSelected(geometry))
                Deselect(geometry);
            else
                Select(geometry, false);
        }



        /// <summary>
        /// Determines whether a geometry is selected or not
        /// </summary>
        /// <param name="geometry">The geometry</param>
        /// <returns>True when the BaseGeometry is selected, otherwise False</returns>
        public bool IsSelected(BaseGeometry geometry)
        {
            return selectedGeometry.Contains(geometry);
        }
        /// <summary>
        /// Determines whether a GeometryModel is selected
        /// </summary>
        /// <param name="model">The model</param>
        /// <returns>True when everything inside the model is selected, False otherwise</returns>
        public bool IsSelected(GeometryModelData model)
        {
            return model.Vertices.All(x => IsSelected(x)) &&
                model.Edges.All(x => IsSelected(x)) &&
                model.Faces.All(x => IsSelected(x)) &&
                model.Volumes.All(x => IsSelected(x));
        }

        /// <summary>
        /// Clears the selection list
        /// </summary>
        public void Clear()
        {
            if (this.selectedGeometry.Count > 0)
            {
                var oldSelection = this.selectedGeometry.ToList();

                ActiveGeometry = null;
                selectedGeometry.Clear();

                SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(SelectionChangedReason.User, null, oldSelection));
            }
        }

        /// <summary>
        /// Ensures that the SelectionModel only contains valid geometry
        /// </summary>
        public void MakeConsistent()
        {
            List<BaseGeometry> deselect = new List<BaseGeometry>();

            foreach (var sel in selectedGeometry.ToList())
                if (!sel.ModelGeometry.ContainsGeometry(sel) || !models.Any(x => x.Geometry == sel.ModelGeometry))
                    deselect.Add(sel);

            if (deselect.Count > 0)
                Deselect(deselect);
        }


        private void ConnectEvents(GeometryModel model)
        {
            model.Replaced += GeometryModel_Replaced;
            ConnectEvents(model.Geometry);
        }
        private void ConnectEvents(GeometryModelData model)
        {
            model.Vertices.CollectionChanged += Geometry_CollectionChanged;
            model.Edges.CollectionChanged += Geometry_CollectionChanged;
            model.EdgeLoops.CollectionChanged += Geometry_CollectionChanged;
            model.Faces.CollectionChanged += Geometry_CollectionChanged;
            model.Volumes.CollectionChanged += Geometry_CollectionChanged;
        }

        private void DisconnectEvents(GeometryModel model)
        {
            model.Replaced -= GeometryModel_Replaced;
            DisconnectEvents(model.Geometry);
        }
        private void DisconnectEvents(GeometryModelData model)
        {
            model.Vertices.CollectionChanged -= Geometry_CollectionChanged;
            model.Edges.CollectionChanged -= Geometry_CollectionChanged;
            model.EdgeLoops.CollectionChanged -= Geometry_CollectionChanged;
            model.Faces.CollectionChanged -= Geometry_CollectionChanged;
            model.Volumes.CollectionChanged -= Geometry_CollectionChanged;
        }
    }
}

