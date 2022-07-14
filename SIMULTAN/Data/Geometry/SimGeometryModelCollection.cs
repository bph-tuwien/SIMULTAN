using SIMULTAN.Data.Assets;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.Geometry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            return geometryModels.Values.Select(x => x.Model).GetEnumerator();
        }
        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return geometryModels.Values.Select(x => x.Model).GetEnumerator();
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
            if (this.geometryModels.TryGetValue(model.File, out var entry))
            {
                entry.ReferenceCounter++;
            }
            else
            {
                var newReference = new GeometryModelReference(model);
                newReference.ReferenceCounter++;

                this.geometryModels.Add(model.File, newReference);
                NotifyCollectionChanged(
                    new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Add,
                        model
                    ));

                ProjectData.ComponentGeometryExchange.AddGeometryModel(model);

                //Linked models
                foreach (var linkedModel in model.LinkedModels)
                    AddGeometryModel(linkedModel);

                model.LinkedModels.CollectionChanged += this.LinkedModels_CollectionChanged;
            }
        }

        /// <summary>
        /// Removes a GeometryModel from the store. Only frees the model when this has been the last reference to the model.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool RemoveGeometryModel(GeometryModel model)
        {
            if (this.geometryModels.TryGetValue(model.File, out var entry))
            {
                foreach (var linkedModel in model.LinkedModels)
                    RemoveGeometryModel(linkedModel);

                if (entry.ReferenceCounter == 1)
                {
                    this.geometryModels.Remove(model.File);
                    NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Remove,
                        model
                        ));

                    ProjectData.ComponentGeometryExchange.RemoveGeometryModel(model);
                    model.LinkedModels.CollectionChanged -= this.LinkedModels_CollectionChanged;

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

        /// <summary>
        /// Tries to find a GeometryModel with a given id
        /// </summary>
        /// <param name="id">The id to search for</param>
        /// <param name="model">Returns the model when one exists</param>
        /// <param name="isOwning">
        /// When set to true, the reference counter for this model is increased by one.
        /// A model requested with isOwning == true has to be freed by calling RemoveGeometryModel
        /// </param>
        /// <returns>True when a model with this id exists, otherwise False</returns>
        public bool TryGetGeometryModel(Guid id, out GeometryModel model, bool isOwning = true)
        {
            foreach (var entry in this.geometryModels.Values)
            {
                if (entry.Model.Id == id)
                {
                    if (isOwning)
                        entry.ReferenceCounter++;

                    model = entry.Model;
                    return true;
                }
            }

            model = null;
            return false;
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
            if (this.geometryModels.TryGetValue(file, out var entry))
            {
                if (isOwning)
                    entry.ReferenceCounter++;

                model = entry.Model;
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
                        foreach (var model in e.NewItems.OfType<GeometryModel>())
                            AddGeometryModel(model);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        foreach (var model in e.OldItems.OfType<GeometryModel>())
                            RemoveGeometryModel(model);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        foreach (var model in e.OldItems.OfType<GeometryModel>())
                            RemoveGeometryModel(model);
                        foreach (var model in e.NewItems.OfType<GeometryModel>())
                            AddGeometryModel(model);
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