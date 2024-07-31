using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Exchange.GeometryConnectors;
using SIMULTAN.Projects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Stores all <see cref="GeometryModel"/> in the project which are currently loaded.
    /// Makes sure that models are only loaded once.
    /// </summary>
    public class SimGeometryModelCollection : IEnumerable<GeometryModel>, INotifyCollectionChanged
    {
        private class GeometryModelReference
        {
            public GeometryModel Model { get; }
            public int ReferenceCounter { get; set; }

            public GeometryModelReference(GeometryModel model)
            {
                this.Model = model;
                this.ReferenceCounter = 0;
            }
        }

        private Dictionary<ResourceFileEntry, GeometryModelReference> geometryModels = new Dictionary<ResourceFileEntry, GeometryModelReference>();
        private GeometryImporterCache geometryCache = new GeometryImporterCache();
        private Dictionary<ResourceFileEntry, GeometryModel> allLoadedModels = new Dictionary<ResourceFileEntry, GeometryModel>();

        /// <summary>
        /// The project data this collection belongs to
        /// </summary>
        public ProjectData ProjectData { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimGeometryModelCollection"/> class
        /// </summary>
        /// <param name="projectData">The projectData this collection belongs to</param>
        public SimGeometryModelCollection(ProjectData projectData)
        {
            if (projectData == null)
                throw new ArgumentNullException(nameof(projectData));

            this.ProjectData = projectData;
        }

        #region IEnumerable

        /// <inheritdoc />
        public IEnumerator<GeometryModel> GetEnumerator()
        {
            return allLoadedModels.Values.GetEnumerator();
        }
        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return allLoadedModels.Values.GetEnumerator();
        }

        #endregion

        #region INotifyCollectionChanged

        /// <inheritdoc />
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void NotifyCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            this.CollectionChanged?.Invoke(this, args);
        }

        #endregion


        #region Model Store

        /// <summary>
        /// Registers a new GeometryModel to the store
        /// </summary>
        /// <param name="model">The new model</param>
        public void AddGeometryModel(GeometryModel model)
        {
            AddGeometryModels(new[] { model });
        }

        /// <summary>
        /// Registers new GeometryModels to the store.
        /// Preferred as this optimizes connector creation more than adding a single one.
        /// </summary>
        /// <param name="models">The new models</param>

        public void AddGeometryModels(IEnumerable<GeometryModel> models)
        {
            foreach (var model in models)
            {
                if (this.geometryModels.TryGetValue(model.File, out var entry))
                {
                    entry.ReferenceCounter++;
                }
                else
                {
                    var newReference = new GeometryModelReference(model);
                    newReference.ReferenceCounter++;
                    this.geometryModels.Add(model.File, newReference);
                }
            }

            AddModelGraph(models);
        }

        private void AddModelGraph(IEnumerable<GeometryModel> models)
        {
            Dictionary<ResourceFileEntry, GeometryModel> all = new Dictionary<ResourceFileEntry, GeometryModel>();

            var connectors = new List<GeometryModelConnector>();
            foreach (var model in models)
            {
                GetAllReachableModels(model, all, new HashSet<ResourceFileEntry>());

                foreach (var toAdd in all)
                {
                    if (!allLoadedModels.ContainsKey(toAdd.Key))
                    {
                        allLoadedModels.Add(toAdd.Key, toAdd.Value);

                        NotifyCollectionChanged(
                            new NotifyCollectionChangedEventArgs(
                                NotifyCollectionChangedAction.Add,
                                toAdd.Value
                            ));

                        connectors.Add(ProjectData.ComponentGeometryExchange.AddGeometryModel(toAdd.Value));

                        toAdd.Value.LinkedModels.CollectionChanged += this.LinkedModels_CollectionChanged;
                    }
                }
            }

            // initialize all connectors at one to speed it up

            //Performance disabling
            var enableReferencePropagation = ProjectData.Components.EnableReferencePropagation;
            ProjectData.Components.EnableReferencePropagation = false;
            ProjectData.ComponentGeometryExchange.EnableNotifyGeometryInvalidated = false;

            //Check the components to find instances that attach to this geometryModel
            CreateConnectors(connectors.ToDictionary(x => x.GeometryModel.File.Key, x => x), ProjectData.Components);

            ProjectData.ComponentGeometryExchange.EnableNotifyGeometryInvalidated = true;
            ProjectData.ComponentGeometryExchange.NotifyGeometryInvalidated(null);

            // initialize connectors before the rest of the initialization
            foreach (var connector in connectors)
            {
                connector.InitializeConnectors();
            }

            //Invalidate/Recalculate all references
            ProjectData.Components.EnableReferencePropagation = enableReferencePropagation;

            foreach (var connector in connectors)
            {
                connector.Initialize(); //Has to be done after the connector has been added to connectors. Otherwise the offset surfaces can't be calculated
            }
        }

        private void CreateConnectors(Dictionary<int, GeometryModelConnector> connectors, IEnumerable<SimComponent> components)
        {
            foreach (var component in components)
            {
                if (component != null)
                {
                    foreach (var inst in component.Instances)
                    {
                        foreach (var placement in inst.Placements.Where(x => x is SimInstancePlacementGeometry gp && connectors.Keys.Contains(gp.FileId)))
                        {
                            var p = placement as SimInstancePlacementGeometry;
                            connectors[p.FileId].CreateConnector((SimInstancePlacementGeometry)placement, false);
                        }
                    }

                    //Child components
                    CreateConnectors(connectors, component.Components.Select(x => x.Component));
                }
            }
        }

        /// <summary>
        /// Removes a GeometryModel from the store. Only frees the model when this has been the last reference to the model.
        /// </summary>
        /// <param name="model">The GeometryModel that should be removed</param>
        /// <returns>True when the GeometryModel is now deleted, False when some other references to the model exist</returns>

        public bool RemoveGeometryModel(GeometryModel model)
        {

            if (this.geometryModels.TryGetValue(model.File, out var entry))
            {
                if (entry.ReferenceCounter == 1)
                {
                    this.geometryModels.Remove(model.File);

                    RemoveUnusedModels();

                    return true;
                }
                else
                {
                    entry.ReferenceCounter--;
                    return false;
                }
            }
            else
                throw new Exception("Model has never been added");
        }

        private void RemoveUnusedModels()
        {
            Dictionary<ResourceFileEntry, GeometryModel> allInUse = new Dictionary<ResourceFileEntry, GeometryModel>();

            // find all models that are still in used by all root models
            foreach (var refs in geometryModels.Values)
            {
                GetAllReachableModels(refs.Model, allInUse, new HashSet<ResourceFileEntry>());
            }

            // get keys that remain
            var removedKeys = allLoadedModels.Keys.Except(allInUse.Keys).ToList();
            foreach (var removed in removedKeys)
            {
                var removedModel = allLoadedModels[removed];
                allLoadedModels.Remove(removed);

                NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Remove,
                    removedModel
                    ));

                ProjectData.ComponentGeometryExchange.RemoveGeometryModel(removedModel);
                removedModel.LinkedModels.CollectionChanged -= this.LinkedModels_CollectionChanged;
            }

        }

        /// <summary>
        /// Puts all the models that are linked to the root model (that are reachable from that) into the result.
        /// Also includes the root model itself.
        /// </summary>
        /// <param name="rootModel">The root model to</param>
        /// <param name="result">The result</param>
        public static void GetAllReachableModels(GeometryModel rootModel, Dictionary<ResourceFileEntry, GeometryModel> result)
        {
            GetAllReachableModels(rootModel, result, new HashSet<ResourceFileEntry>());
        }

        private static void GetAllReachableModels(GeometryModel rootModel, Dictionary<ResourceFileEntry, GeometryModel> result, HashSet<ResourceFileEntry> processed)
        {
            if (processed.Contains(rootModel.File))
                return;
            processed.Add(rootModel.File);

            if (!result.ContainsKey(rootModel.File))
                result.Add(rootModel.File, rootModel);

            foreach (var linked in rootModel.LinkedModels)
                GetAllReachableModels(linked, result, processed);
        }

        /// <summary>
        /// Tries to find a GeometryModel for a given file
        /// </summary>
        /// <param name="file">The file to search for</param>
        /// <param name="model">Returns the model when one exists</param>
        /// <param name="isOwning">
        /// When set to true, the reference counter for this model is increased by one.
        /// A model requested with isOwning == true has to be freed by calling RemoveGeometryModel
        /// </param>
        /// <returns>True when a model with this file exists, otherwise False</returns>
        public bool TryGetGeometryModel(ResourceFileEntry file, out GeometryModel model, bool isOwning = true)
        {
            model = null;
            if (this.allLoadedModels.TryGetValue(file, out var entry))
            {
                if (isOwning)
                {
                    if (geometryModels.TryGetValue(file, out var rootmodel))
                    {
                        rootmodel.ReferenceCounter++;
                    }
                    else
                    {
                        var newref = new GeometryModelReference(entry);
                        newref.ReferenceCounter++;
                        geometryModels.Add(file, newref);
                    }
                }

                model = entry;
                return true;
            }
            return false;
        }

        private void LinkedModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        AddModelGraph(e.NewItems.OfType<GeometryModel>());
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        if (e.OldItems.OfType<GeometryModel>().Any())
                            RemoveUnusedModels();
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        if (e.OldItems.OfType<GeometryModel>().Any())
                            RemoveUnusedModels();
                        AddModelGraph(e.NewItems.OfType<GeometryModel>());
                    }
                    break;
                default:
                    throw new NotSupportedException("This operation is not supported");
            }
        }

        #endregion

        #region Geometry Cache

        /// <summary>
        /// Tries to get the imported geometry data from the cache.
        /// </summary>
        /// <param name="file">The file that should be checked if it is present in the cache.</param>
        /// <returns>The GeometryImporterResult of the specified file from the cache or null if it was not found.</returns>
        public SimMeshGeometryData TryGetCachedImportedGeometry(FileInfo file)
        {
            return geometryCache.TryGetCachedImportedGeometry(file);
        }

        /// <summary>
        /// Adds or updates an entry in the imported geometry cache.
        /// </summary>
        /// <param name="file">File that was imported.</param>
        /// <param name="geometry">The GeometryImporterResult of the imported geometry.</param>
        public void CacheImportedGeometry(FileInfo file, SimMeshGeometryData geometry)
        {
            geometryCache.CacheImportedGeometry(file, geometry);
        }

        #endregion

        #region Importer Warnings

        /// <summary>
        /// Event handler when an warning is reported while importing meshes.
        /// </summary>
        /// <param name="messages">The error messages.</param>
        public delegate void ImporterWarningEventHandler(IEnumerable<ImportWarningMessage> messages);
        /// <summary>
        /// Event that gets called when errors happen while importing meshes.
        /// </summary>
        public event ImporterWarningEventHandler ImporterWarning;
        /// <summary>
        /// Calls the ImporterWarning Event.
        /// </summary>
        /// <param name="messages">Warning Messages.</param>
        public void OnImporterWarning(IEnumerable<ImportWarningMessage> messages)
        {
            ImporterWarning?.Invoke(messages);
        }

        #endregion
    }
}