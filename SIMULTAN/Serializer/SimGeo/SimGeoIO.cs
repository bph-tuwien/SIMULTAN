using SIMULTAN.Data;
using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Data.ValueMappings;
using SIMULTAN.Projects;
using SIMULTAN.Utils;
using SIMULTAN.Utils.BackgroundWork;
using SIMULTAN.Utils.Collections;
using SIMULTAN.Utils.Files;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SIMULTAN.Serializer.SimGeo
{
    /// <summary>
    /// Different errors that can happen during an import
    /// </summary>
    public enum SimGeoIOErrorReason
    {
        /// <summary>
        /// A linked resource could not be found or is not part of the working directory
        /// </summary>
        InvalidLinkedModel,
        /// <summary>
        /// Happens when during converting references the target couldn't be found
        /// </summary>
        ReferenceConvertFailed,
        /// <summary>
        /// Happens when multiple GeometryModels in the project had the same Guid
        /// </summary>
        ModelWithSameId,
    }

    /// <summary>
    /// An error message happening during import
    /// </summary>
    public class SimGeoIOError
    {
        /// <summary>
        /// The error reason
        /// </summary>
        public SimGeoIOErrorReason Reason { get; }
        /// <summary>
        /// Additional data for the reason
        /// </summary>
        public object[] Data { get; }

        /// <summary>
        /// Initializes a new instance of the SimGeoIOError class
        /// </summary>
        /// <param name="reason">The error reason</param>
        /// <param name="data">Additional data for the reason</param>
        public SimGeoIOError(SimGeoIOErrorReason reason, object[] data)
        {
            this.Reason = reason;
            this.Data = data;
        }
    }


    /// <summary>
    /// Provides methods for accessing simgeo format files. Currently only the Plaintext version is supported
    /// </summary>
    public class SimGeoIO
    {
        /// <summary>
        /// The current version of the SimGeo Format
        /// </summary>
        public static int SimGeoVersion => 16;

        /// <summary>
        /// Describes which format should be written
        /// </summary>
        public enum WriteMode
        {
            /// <summary>
            /// Plaintext format
            /// </summary>
            Plaintext
        }

        /// <summary>
        /// Used for migrating old parents (previously GeometryReference)
        /// </summary>
        private struct ParentMigrationData
        {
            public bool IsLegacy;
            public ulong LegacyId;
            public Guid ModelId;
            public ulong GeometryId;

            public static ParentMigrationData Empty = new ParentMigrationData(false, 0, Guid.Empty, 0);

            public ParentMigrationData(bool isLegacy, ulong legacyId, Guid modelId, ulong geometryId)
            {
                this.IsLegacy = isLegacy;
                this.LegacyId = legacyId;
                this.ModelId = modelId;
                this.GeometryId = geometryId;
            }

            public ParentMigrationData(ulong legacyId, ulong geometryId) : this(true, legacyId, Guid.Empty, geometryId)
            { }

            public ParentMigrationData(Guid modelId, ulong geometryId) : this(false, 0, modelId, geometryId)
            { }
        }

        private static readonly string LegacyGeometryTaxonomyName = "Legacy Geometry";
        private static readonly string LegacyGeometryTaxonomyDescription = "Taxonomy for legacy geometry";
        private static readonly string LegacyGeometryTaxonomyKey = "legacy geometry";
        private static readonly string LegacyGeometryParentName = "Legacy Parent";
        private static readonly string LegacyGeometryParentDescription = "Tag for the legacy parent geometry relation";
        private static readonly string LegacyGeometryParentKey = "legacy parent";

        /// <summary>
        /// Returns the legacy taxonomy entry tag used to migrate old geometry parents to <see cref="SimGeometryRelation"/>.
        /// Also create the legacy taxonomy and entry if it does not yet exist.
        /// </summary>
        /// <param name="projectData">The project data. Taxonomies already need to be loaded.</param>
        /// <returns>The legacy parent taxonomy entry to be used for <see cref="SimGeometryRelation"/> migration.</returns>
        public static SimTaxonomyEntry GetLegacyParentTaxonomyEntry(ProjectData projectData)
        {
            var entry = projectData.Taxonomies.FindEntry(LegacyGeometryTaxonomyKey, LegacyGeometryParentKey);
            if (entry == null)
            {
                var tax = new SimTaxonomy(LegacyGeometryTaxonomyKey, LegacyGeometryTaxonomyName, LegacyGeometryTaxonomyDescription);
                entry = new SimTaxonomyEntry(LegacyGeometryParentKey, LegacyGeometryParentName, LegacyGeometryParentDescription);
                tax.Entries.Add(entry);
                projectData.Taxonomies.Add(tax);
            }
            return entry;
        }

        /// <summary>
        /// Stores the model to a file
        /// </summary>
        /// <param name="model">The model to store</param>
        /// <param name="file">FileInfo of the target file. The file gets overridden without confirmation!!</param>
        /// <param name="mode">Format in which the file should be written</param>
        public static bool Save(GeometryModel model, ResourceFileEntry file, WriteMode mode)
        {
            return Save(model, file, mode, new HashSet<ResourceFileEntry>());
        }

        private static bool Save(GeometryModel model, ResourceFileEntry file, WriteMode mode, HashSet<ResourceFileEntry> savedFiles)
        {
            if (savedFiles.Contains(file))
                return true;
            savedFiles.Add(file);

            //Make sure the model is consistent
            GeometryModelAlgorithms.CheckConsistency(model.Geometry);

            bool valid = true;
            try
            {
                var directoryPath = Path.GetDirectoryName(file.CurrentFullPath);

                if (!Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                using (StreamWriter sw = new StreamWriter(file.CurrentFullPath, false, Encoding.Unicode))
                {
                    if (mode == WriteMode.Plaintext)
                        SavePlaintext(sw, model);
                }

                foreach (var lm in model.LinkedModels)
                    valid = valid && Save(lm, lm.File, mode, savedFiles);
            }
            catch (IOException e)
            {
                valid = false;
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
            }

            return valid;
        }


        /// <summary>
        /// Reads a model from a file
        /// </summary>
        /// <param name="geometryFile">The file</param>
        /// <param name="projectData">The model store in which the GeometryModels should be loaded</param>
        /// <param name="errors">A list to which error messages are added</param>
        /// <param name="offsetAlg">Defines how offset surfaces should be generated after loading</param>
        /// <returns>The geometry model</returns>
        public static GeometryModel Load(ResourceFileEntry geometryFile, ProjectData projectData,
            List<SimGeoIOError> errors,
            OffsetAlgorithm offsetAlg = OffsetAlgorithm.Full)
        {
            //Translates pre-8 ids to Guids
            var idToGuid = new Dictionary<ulong, Guid>();
            var modelLookup = new Dictionary<Guid, GeometryModel>();
            var legacyParentRelations = new List<(SimBaseGeometryReference source, ParentMigrationData target)>();
            var loadedModels = new Dictionary<ResourceFileEntry, GeometryModel>();

            var model = LoadWithoutCheck(geometryFile, projectData, idToGuid, offsetAlg, errors, modelLookup, legacyParentRelations, loadedModels);
            ConvertIdBasedReferencesAndRelation(projectData, modelLookup, idToGuid, errors, legacyParentRelations);

            return model;
        }

        /// <summary>
        /// Migrates geometry files and their linked files after they were imported with their relations.
        /// Uses the <paramref name="importedFilesLookup"/> to find changed file names (duplicate) for the old linked files migration and
        /// the <paramref name="importedKeysLookup"/> to find changed resource ids of the imported linked files.
        /// Basically opens the files, migrates them, adds them to the open project and saves them again.
        /// Manually remove them afterwards if they should not be kept in memory.
        /// </summary>
        /// <param name="geometryFiles">The geometry file to import</param>
        /// <param name="importedKeysLookup">Lookup from linked file resource id before import to id after import</param>
        /// <param name="importedFilesLookup">Lookup from filenames before import and after import (might have changed if file already existed)</param>
        /// <param name="projectData">The model store in which the GeometryModels should be loaded</param>
        /// <param name="errors">A list to which error messages are added</param>
        /// <param name="offsetAlg">Defines how offset surfaces should be generated after loading</param>
        public static void MigrateAfterImport(IEnumerable<ResourceFileEntry> geometryFiles, ProjectData projectData,
            List<SimGeoIOError> errors,
            Dictionary<int, int> importedKeysLookup, Dictionary<string, string> importedFilesLookup,
            OffsetAlgorithm offsetAlg = OffsetAlgorithm.Disabled)
        {
            var migrateData = new List<(ResourceFileEntry file, GeometryModel model, Dictionary<ulong, Guid> idToGuid)>();
            var modelLookup = new Dictionary<Guid, GeometryModel>();
            var legacyParentRelations = new List<(SimBaseGeometryReference source, ParentMigrationData target)>();

            // load all the models
            foreach (var geometryFile in geometryFiles)
            {
                //Translates pre-8 ids to Guids
                var idToGuid = new Dictionary<ulong, Guid>();
                var loadedModels = new Dictionary<ResourceFileEntry, GeometryModel>();
                // load with lookup to migrate
                var model = LoadWithoutCheck(geometryFile, projectData, idToGuid, offsetAlg, errors, modelLookup, legacyParentRelations, loadedModels, importedKeysLookup, importedFilesLookup);
                migrateData.Add((geometryFile, model, idToGuid));
                // add to project
                projectData.GeometryModels.AddGeometryModel(model);
            }

            // perform the geometry relations migration, need to have all the models loaded beforehand
            foreach (var data in migrateData)
            {
                var model = data.model;
                var idToGuid = data.idToGuid;
                ConvertIdBasedReferencesAndRelation(projectData, modelLookup, idToGuid, errors, legacyParentRelations);

                // save again
                Save(model, data.file, WriteMode.Plaintext);
            }
        }

        private static GeometryModel LoadWithoutCheck(ResourceFileEntry geometryFile,
            ProjectData projectData, Dictionary<ulong, Guid> idToGuid, OffsetAlgorithm offsetAlg, List<SimGeoIOError> errors,
            Dictionary<Guid, GeometryModel> modelLookup,
            List<(SimBaseGeometryReference, ParentMigrationData)> legacyParentRelations,
            Dictionary<ResourceFileEntry, GeometryModel> loadedModels,
            Dictionary<int, int> importedKeysLookup = null, Dictionary<string, string> importedFilesLookup = null)
        {
            if (!geometryFile.Exists)
                throw new FileNotFoundException(geometryFile.CurrentFullPath);

            if (loadedModels.TryGetValue(geometryFile, out var loaded))
            {
                return loaded;
            }
            else if (projectData.GeometryModels.TryGetGeometryModel(geometryFile, out var existingModel, false))
            {
                // if offset algorithm was disabled, enable if changed
                // happens when geo model is opened after the siteplanner
                if (existingModel.Geometry.OffsetModel.Generator.Algorithm == OffsetAlgorithm.Disabled &&
                    existingModel.Geometry.OffsetModel.Generator.Algorithm != offsetAlg)
                {
                    // update the offset surfaces if the algorithm changed
                    existingModel.Geometry.OffsetModel.Generator.Algorithm = offsetAlg;
                    existingModel.Geometry.OffsetModel.Generator.Update();
                }
                return existingModel;
            }
            else
            {
                var encoding = GetEncoding(geometryFile.CurrentFullPath);

                GeometryModel model = null;
                Guid oldId = Guid.Empty;
                int versionNumber = -1;
                var linkedModels = new List<Int32>();

                using (FileStream fs = new FileStream(geometryFile.CurrentFullPath, FileMode.Open))
                {
                    using (StreamReader sr = new StreamReader(fs, encoding))
                    {
                        var formatIdent = (char)sr.Read();
                        int row = 1, column = 2;
                        if (formatIdent == 'T')
                            (model, oldId, versionNumber) = LoadPlaintext(sr, geometryFile, linkedModels, projectData, idToGuid, ref row, ref column, offsetAlg, legacyParentRelations, importedFilesLookup);
                        else
                            throw new IOException("Unknown format identifier");
                    }
                }

                if (model != null)
                {
                    CheckLayerIdsForDuplicates(model);

                    loadedModels.Add(model.File, model);
                    foreach (var tmpFileId in linkedModels)
                    {
                        int fileId = tmpFileId;
                        // lookup migrated ids, only when file was imported to project, only if the file already contained ids for the linked files (>=v12)
                        if (versionNumber >= 12)
                        {
                            if (importedKeysLookup != null && !importedKeysLookup.TryGetValue(tmpFileId, out fileId))
                            {
                                errors.Add(new SimGeoIOError(SimGeoIOErrorReason.InvalidLinkedModel, new object[]
                                {
                                tmpFileId.ToString()
                                }));
                                continue;
                            }
                        }
                        // error while migrating from version < 12 (filenames instead of Ids)
                        if (fileId < 0)
                        {
                            errors.Add(new SimGeoIOError(SimGeoIOErrorReason.InvalidLinkedModel, new object[]
                            {
                                fileId.ToString()
                            }));
                            continue;
                        }

                        var resource = projectData.AssetManager.GetResource(fileId);

                        if (resource == null || !(resource is ResourceFileEntry rfe))
                        {
                            errors.Add(new SimGeoIOError(SimGeoIOErrorReason.InvalidLinkedModel, new object[]
                            {
                                    fileId.ToString()
                            }));
                        }
                        else
                        {
                            var linkedModel = LoadWithoutCheck((ResourceFileEntry)resource, projectData, idToGuid, offsetAlg, errors, modelLookup, legacyParentRelations, loadedModels);
                            model.LinkedModels.Add(linkedModel);
                        }
                    }
                }

                if (versionNumber < 12)
                {
                    if (modelLookup.TryGetValue(oldId, out var otherGM))
                    {
                        //Error, two models with same Id in project
                        errors.Add(new SimGeoIOError(SimGeoIOErrorReason.ModelWithSameId, new object[]
                        {
                            otherGM.File.Name,
                            geometryFile.Name
                        }));
                    }
                    else
                        modelLookup.Add(oldId, model);
                }
                return model;
            }
        }

        /// <summary>
        /// Checks if the layers have the same Id as some geometry and give them new Ids if that happens.
        /// Sometimes the case in older models.
        /// </summary>
        /// <param name="model">the model</param>
        private static void CheckLayerIdsForDuplicates(GeometryModel model)
        {
            var queue = new Queue<Layer>(model.Geometry.Layers);
            while (queue.Any())
            {
                var layer = queue.Dequeue();
                // check if geometry with same id as layer exists
                // Due to yet unknown reasons older files could have layers with IDs of other geometries which causes errors when deleting the layer
                if (model.Geometry.GeometryFromId(layer.Id) != null)
                {
                    // Because the layer id has a private setter (as it should) and all other paths would be completely recreating the layer,
                    // we decided on updating the id via reflection as this is save here and has the least negative long term effects and keeps the API intact.
                    var oldLayerId = layer.Id;
                    var prop = typeof(Layer).GetProperty(nameof(Layer.Id), BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                    prop.SetValue(layer, model.Geometry.GetFreeId());
                    Debug.WriteLine($"Layer ({layer.Name}) Id duplicate found in geometries, migrating from Id '{oldLayerId}' to '{layer.Id}'");
                }
                layer.Layers.ForEach(x => queue.Enqueue(x));
            }
        }

        #region Write

        private static void SavePlaintext(StreamWriter sw, GeometryModel model)
        {
            //HEADER
            sw.Write('T');
            WriteNumberPlaintext<Int32>(sw, SimGeoIO.SimGeoVersion);
            WriteNumberPlaintext<UInt64>(sw, (UInt64)model.Permissions.ModelPermissions);
            WriteNumberPlaintext<UInt64>(sw, (UInt64)model.Permissions.GeometryPermissions);
            WriteNumberPlaintext<UInt64>(sw, (UInt64)model.Permissions.LayerPermissions);

            WriteNumberPlaintext<double>(sw, model.CleanupTolerance);

            WriteNumberPlaintext<Int32>(sw, model.Geometry.Layers.Sum(l => CountLayer(l)));
            WriteNumberPlaintext<Int32>(sw, model.Geometry.Vertices.Count);
            WriteNumberPlaintext<Int32>(sw, model.Geometry.Edges.Count);
            WriteNumberPlaintext<Int32>(sw, model.Geometry.EdgeLoops.Count);
            WriteNumberPlaintext<Int32>(sw, model.Geometry.Polylines.Count);
            WriteNumberPlaintext<Int32>(sw, model.Geometry.Faces.Count);
            WriteNumberPlaintext<Int32>(sw, model.Geometry.Volumes.Count);
            WriteNumberPlaintext<Int32>(sw, model.LinkedModels.Count);
            WriteNumberPlaintext<Int32>(sw, model.Geometry.ProxyGeometries.Count);
            WriteNumberPlaintext<Int32>(sw, model.Geometry.GeoReferences.Count);
            WriteNumberPlaintext<Int32>(sw, model.ValueMappings.Count);
            WriteNumberPlaintext<UInt64>(sw, model.Geometry.GetFreeId(false)); // free id is the next usable one, we need to remember that
            sw.WriteLine();

            //CONTENT
            WriteStringPlaintext(sw, model.Name);
            WriteBoolPlaintext(sw, model.Geometry.IsVisible);
            sw.WriteLine();

            //Layer
            foreach (var l in model.Geometry.Layers)
                WriteLayerPlaintext(sw, l);

            //Geometry
            foreach (var v in model.Geometry.Vertices)
                WriteVertexPlaintext(sw, v);
            foreach (var e in model.Geometry.Edges)
                WriteEdgePlaintext(sw, e);
            foreach (var l in model.Geometry.EdgeLoops)
                WriteEdgeLoopPlaintext(sw, l);
            foreach (var pl in model.Geometry.Polylines)
                WritePolylinePlaintext(sw, pl);
            foreach (var f in model.Geometry.Faces)
                WriteFacePlaintext(sw, f);
            foreach (var v in model.Geometry.Volumes)
                WriteVolumePlaintext(sw, v);
            foreach (var p in model.Geometry.ProxyGeometries)
                WriteProxyGeometryPlaintext(sw, p);
            foreach (var r in model.Geometry.GeoReferences)
                WriteGeoRefPlaintext(sw, r);
            foreach (var vm in model.ValueMappings)
                WriteValueMappingIdPlaintext(sw, vm);
            WriteValueMappingIdPlaintext(sw, model.ActiveValueMapping);
            sw.WriteLine();

            //Linked Models
            foreach (var m in model.LinkedModels)
                WriteLinkedModelPlaintext(sw, m, model);
        }

        private static void WriteStringPlaintext(StreamWriter sw, string str)
        {
            sw.Write("{0:D};{1}", (System.Int32)str.Length, str);
        }
        private static void WriteNumberPlaintext<T>(StreamWriter sw, T number) where T : IConvertible
        {
            sw.Write("{0};", number.ToString(CultureInfo.InvariantCulture));
        }
        private static void WriteBoolPlaintext(StreamWriter sw, bool b)
        {
            sw.Write(b ? "1" : "0");
        }
        private static void WriteColorPlaintext(StreamWriter sw, DerivedColor c)
        {
            WriteNumberPlaintext<Byte>(sw, c.LocalColor.R);
            WriteNumberPlaintext<Byte>(sw, c.LocalColor.G);
            WriteNumberPlaintext<Byte>(sw, c.LocalColor.B);
            WriteNumberPlaintext<Byte>(sw, c.LocalColor.A);
            WriteBoolPlaintext(sw, c.IsFromParent);
        }
        private static void WriteOrientationPlaintext(StreamWriter sw, GeometricOrientation orientation)
        {
            switch (orientation)
            {
                case GeometricOrientation.Undefined:
                    WriteNumberPlaintext<Byte>(sw, 0);
                    break;
                case GeometricOrientation.Forward:
                    WriteNumberPlaintext<Byte>(sw, 1);
                    break;
                case GeometricOrientation.Backward:
                    WriteNumberPlaintext<Byte>(sw, 2);
                    break;
            }
        }
        private static void WriteGuidPlaintext(StreamWriter sw, Guid guid)
        {
            WriteStringPlaintext(sw, guid.ToString("N"));
        }

        private static void WriteListPlaintext<T>(StreamWriter sw, List<T> list, Action<StreamWriter, T> elementWriteAction)
        {
            WriteNumberPlaintext<Int32>(sw, list.Count);
            foreach (var element in list)
                elementWriteAction(sw, element);
        }

        private static int CountLayer(Layer l)
        {
            return 1 + l.Layers.Sum(x => CountLayer(x));
        }
        private static void WriteLayerPlaintext(StreamWriter sw, Layer layer)
        {
            WriteNumberPlaintext<UInt64>(sw, layer.Id);

            if (layer.Parent != null)
                WriteNumberPlaintext<UInt64>(sw, layer.Parent.Id);
            else
                sw.Write(";");

            WriteStringPlaintext(sw, layer.Name);
            WriteBoolPlaintext(sw, layer.IsVisible);
            WriteColorPlaintext(sw, layer.Color);
            sw.WriteLine();

            foreach (var l in layer.Layers)
                WriteLayerPlaintext(sw, l);
        }

        private static void WriteBaseGeometryPlaintext(StreamWriter sw, BaseGeometry geo)
        {
            WriteNumberPlaintext<UInt64>(sw, geo.Id);
            WriteStringPlaintext(sw, geo.Name);
            WriteNumberPlaintext<UInt64>(sw, geo.Layer.Id);
            WriteBoolPlaintext(sw, geo.IsVisible);
        }
        private static void WriteVertexPlaintext(StreamWriter sw, Vertex vertex)
        {
            WriteBaseGeometryPlaintext(sw, vertex);
            WriteNumberPlaintext<Double>(sw, vertex.Position.X);
            WriteNumberPlaintext<Double>(sw, vertex.Position.Y);
            WriteNumberPlaintext<Double>(sw, vertex.Position.Z);
            WriteColorPlaintext(sw, vertex.Color);
            sw.WriteLine();
        }
        private static void WriteEdgePlaintext(StreamWriter sw, Edge e)
        {
            WriteBaseGeometryPlaintext(sw, e);
            WriteNumberPlaintext<UInt64>(sw, e.Vertices[0].Id);
            WriteNumberPlaintext<UInt64>(sw, e.Vertices[1].Id);
            WriteColorPlaintext(sw, e.Color);
            sw.WriteLine();
        }
        private static void WriteEdgeLoopPlaintext(StreamWriter sw, EdgeLoop loop)
        {
            WriteBaseGeometryPlaintext(sw, loop);
            WriteNumberPlaintext<UInt64>(sw, loop.BaseEdge.Id);
            WriteOrientationPlaintext(sw, loop.BaseEdgeOrientation);
            WriteNumberPlaintext<Int32>(sw, loop.Edges.Count);

            foreach (var e in loop.Edges)
                WriteNumberPlaintext<UInt64>(sw, e.Edge.Id);

            WriteColorPlaintext(sw, loop.Color);
            sw.WriteLine();
        }
        private static void WritePolylinePlaintext(StreamWriter sw, Polyline loop)
        {
            WriteBaseGeometryPlaintext(sw, loop);
            WriteNumberPlaintext<Int32>(sw, loop.Edges.Count);

            foreach (var e in loop.Edges)
                WriteNumberPlaintext<UInt64>(sw, e.Edge.Id);

            WriteColorPlaintext(sw, loop.Color);
            sw.WriteLine();
        }
        private static void WriteFacePlaintext(StreamWriter sw, Face face)
        {
            WriteBaseGeometryPlaintext(sw, face);

            WriteNumberPlaintext<UInt64>(sw, face.Boundary.Id);

            WriteNumberPlaintext<Int32>(sw, face.Holes.Count);
            foreach (var h in face.Holes)
                WriteNumberPlaintext<UInt64>(sw, h.Id);

            WriteOrientationPlaintext(sw, face.Orientation);
            WriteColorPlaintext(sw, face.Color);
            sw.WriteLine();
        }
        private static void WriteVolumePlaintext(StreamWriter sw, Volume volume)
        {
            WriteBaseGeometryPlaintext(sw, volume);

            WriteNumberPlaintext<Int32>(sw, volume.Faces.Count);
            foreach (var f in volume.Faces)
                WriteNumberPlaintext<UInt64>(sw, f.Face.Id);

            WriteColorPlaintext(sw, volume.Color);
            sw.WriteLine();
        }

        private static void WriteGeoRefPlaintext(StreamWriter sw, GeoReference reference)
        {
            WriteNumberPlaintext<ulong>(sw, reference.Vertex.Id);
            WriteNumberPlaintext<double>(sw, reference.ReferencePoint.X);
            WriteNumberPlaintext<double>(sw, reference.ReferencePoint.Y);
            WriteNumberPlaintext<double>(sw, reference.ReferencePoint.Z);
        }

        private static void WriteValueMappingIdPlaintext(StreamWriter sw, SimValueMapping vm)
        {
            if (vm != null)
                WriteNumberPlaintext<Int64>(sw, vm.Id.LocalId);
            else
                WriteNumberPlaintext<Int64>(sw, 0L);
        }

        private static void WriteProxyGeometryPlaintext(StreamWriter sw, ProxyGeometry proxy)
        {
            WriteBaseGeometryPlaintext(sw, proxy);

            //Vertex
            WriteNumberPlaintext<UInt64>(sw, proxy.Vertex.Id);

            //Modelmatrix
            WriteNumberPlaintext<double>(sw, proxy.Size.X);
            WriteNumberPlaintext<double>(sw, proxy.Size.Y);
            WriteNumberPlaintext<double>(sw, proxy.Size.Z);

            WriteNumberPlaintext<double>(sw, proxy.Rotation.X);
            WriteNumberPlaintext<double>(sw, proxy.Rotation.Y);
            WriteNumberPlaintext<double>(sw, proxy.Rotation.Z);
            WriteNumberPlaintext<double>(sw, proxy.Rotation.W);

            //Positions
            WriteListPlaintext(sw, proxy.Positions, (w, x) =>
            {
                WriteNumberPlaintext<double>(w, x.X);
                WriteNumberPlaintext<double>(w, x.Y);
                WriteNumberPlaintext<double>(w, x.Z);
            });

            //Normals
            WriteListPlaintext(sw, proxy.Normals, (w, x) =>
            {
                WriteNumberPlaintext<double>(w, x.X);
                WriteNumberPlaintext<double>(w, x.Y);
                WriteNumberPlaintext<double>(w, x.Z);
            });

            //Indices
            WriteListPlaintext(sw, proxy.Indices, WriteNumberPlaintext<Int32>);

            WriteColorPlaintext(sw, proxy.Color);
            sw.WriteLine();
        }

        private static void WriteLinkedModelPlaintext(StreamWriter sw, GeometryModel linkedModel, GeometryModel sourceModel)
        {
            WriteNumberPlaintext<Int32>(sw, linkedModel.File.Key);
        }

        #endregion

        #region Read

        /// <summary>
        /// Determines a text file's encoding by analyzing its byte order mark (BOM).
        /// Defaults to ASCII when detection of the text file's endianness fails.
        /// From https://stackoverflow.com/questions/3825390/effective-way-to-find-any-files-encoding
        /// </summary>
        /// <param name="filename">The text file to analyze.</param>
        /// <returns>The detected encoding.</returns>
        private static Encoding GetEncoding(string filename)
        {
            // Read the BOM
            var bom = new byte[4];
            using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                file.Read(bom, 0, 4);
            }

            // Analyze the BOM
#pragma warning disable SYSLIB0001 // Type or member is obsolete
            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
#pragma warning restore SYSLIB0001 // Type or member is obsolete
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
            if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
            if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return Encoding.UTF32;
            return Encoding.Default;
        }

        private static (GeometryModel model, Guid oldId, int versionNumber) LoadPlaintext(StreamReader stream, ResourceFileEntry file,
            List<Int32> linkedModels, ProjectData projectData, Dictionary<ulong, Guid> idToGuid,
            ref int row, ref int column, OffsetAlgorithm offsetAlg,
            List<(SimBaseGeometryReference, ParentMigrationData)> legacyParentRelations,
            Dictionary<string, string> importPathLookup)
        {
            //Parse header
            int versionNumber = ReadNumber<Int32>(stream, ref row, ref column, "Version Number");

            Guid id = Guid.NewGuid();
            if (versionNumber >= 8 && versionNumber < 12)
            {
                id = ReadGuid(stream, ref row, ref column, "Model ID");
            }
            else if (versionNumber >= 5 && versionNumber < 8)
            {
                ulong ulongId = ReadNumber<UInt64>(stream, ref row, ref column, "Model ID");
                if (ulongId != ulong.MaxValue)
                    idToGuid.Add(ulongId, id);
            }

            OperationPermission permission = OperationPermission.DefaultWallModelPermissions;
            if (versionNumber >= 6)
            {
                permission = new OperationPermission(
                    (GeometryModelOperationPermissions)ReadNumber<UInt64>(stream, ref row, ref column, "Model Permission"),
                    (GeometryOperationPermissions)ReadNumber<UInt64>(stream, ref row, ref column, "Geometry Permission"),
                    (LayerOperationPermissions)ReadNumber<UInt64>(stream, ref row, ref column, "Layer Permission")
                    );
            }

            double tolerance = 0.05;
            if (versionNumber >= 13)
                tolerance = ReadNumber<double>(stream, ref row, ref column, "Tolerance");

            Int32 layerCount = ReadNumber<Int32>(stream, ref row, ref column, "Layer Count");
            Int32 vertexCount = ReadNumber<Int32>(stream, ref row, ref column, "Vertex Count");
            Int32 edgeCount = ReadNumber<Int32>(stream, ref row, ref column, "Edge Count");
            Int32 edgeLoopCount = ReadNumber<Int32>(stream, ref row, ref column, "Edge Loop Count");

            Int32 polylineCount = 0;
            if (versionNumber >= 3)
                polylineCount = ReadNumber<Int32>(stream, ref row, ref column, "Polyline Count");

            Int32 faceCount = ReadNumber<Int32>(stream, ref row, ref column, "Face Count");
            Int32 volumeCount = ReadNumber<Int32>(stream, ref row, ref column, "Volume Count");

            Int32 linkedModelCount = 0;
            if (versionNumber >= 2)
                linkedModelCount = ReadNumber<Int32>(stream, ref row, ref column, "Linked Model Count");

            Int32 proxyCount = 0;
            if (versionNumber >= 4)
                proxyCount = ReadNumber<Int32>(stream, ref row, ref column, "Proxy Geometry Count");

            Int32 geoRefCount = 0;
            if (versionNumber >= 7)
                geoRefCount = ReadNumber<Int32>(stream, ref row, ref column, "GeoRef Count");

            Int32 valueMappingCount = 0;
            if (versionNumber >= 11)
                valueMappingCount = ReadNumber<Int32>(stream, ref row, ref column, "ValueMappingCount");

            UInt64 nextGeoId = 0;
            if (versionNumber >= 10)
                nextGeoId = ReadNumber<UInt64>(stream, ref row, ref column, "Next Geometry ID");

            string name = ReadString(stream, ref row, ref column, "Model Name");
            bool isVisible = ReadBool(stream, ref row, ref column, "Model IsVisible");

            GeometryModelData modelData = new GeometryModelData(nextGeoId, projectData.DispatcherTimerFactory);
            modelData.OffsetModel.Generator.Algorithm = offsetAlg;
            modelData.StartBatchOperation();

            //Layer
            Dictionary<ulong, Layer> layers = new Dictionary<ulong, Layer>();
            for (int i = 0; i < layerCount; ++i)
                ReadLayer(stream, modelData, layers, ref row, ref column);

            Dictionary<ulong, BaseGeometry> geometries = new Dictionary<ulong, BaseGeometry>();
            for (int i = 0; i < vertexCount; ++i)
                ReadVertex(stream, modelData, layers, geometries, projectData, versionNumber, ref row, ref column, file.Key, legacyParentRelations);

            for (int i = 0; i < edgeCount; ++i)
                ReadEdge(stream, modelData, layers, geometries, projectData, versionNumber, ref row, ref column, file.Key, legacyParentRelations);

            MultiDictionary<ulong, EdgeLoop> edgeLoopMergeTable = new MultiDictionary<ulong, EdgeLoop>();
            for (int i = 0; i < edgeLoopCount; ++i)
                ReadEdgeLoop(stream, modelData, layers, geometries, projectData, versionNumber, ref row, ref column, file.Key, legacyParentRelations,
                    edgeLoopMergeTable);

            for (int i = 0; i < polylineCount; ++i)
                ReadPolyline(stream, modelData, layers, geometries, projectData, versionNumber, ref row, ref column, file.Key, legacyParentRelations);

            for (int i = 0; i < faceCount; ++i)
                ReadFace(stream, modelData, layers, geometries, projectData, versionNumber, ref row, ref column, file.Key, legacyParentRelations);

            for (int i = 0; i < volumeCount; ++i)
                ReadVolume(stream, modelData, layers, geometries, projectData, versionNumber, ref row, ref column, file.Key, legacyParentRelations);

            for (int i = 0; i < proxyCount; ++i)
                ReadProxyGeometry(stream, modelData, layers, geometries, projectData, versionNumber, ref row, ref column, file.Key, legacyParentRelations);

            modelData.EndBatchOperation();

            var geometryModel = new GeometryModel(name, file, permission, modelData);
            geometryModel.CleanupTolerance = tolerance;

            for (int i = 0; i < geoRefCount; ++i)
                ReadGeoRef(stream, modelData, geometries, versionNumber, ref row, ref column);

            for (int i = 0; i < valueMappingCount; ++i)
                ReadValueMapping(stream, geometryModel, projectData, versionNumber, ref row, ref column);

            if (versionNumber >= 11)
            {
                long activeValueMappingId = ReadNumber<Int64>(stream, ref row, ref column, "Active ColorMapping Id");
                if (activeValueMappingId > 0)
                {
                    geometryModel.ActiveValueMapping = geometryModel.ValueMappings.FirstOrDefault(x => x.Id.LocalId == activeValueMappingId);
                }
            }

            for (int i = 0; i < linkedModelCount; ++i)
                linkedModels.Add(ReadLinkedModel(stream, geometryModel, projectData, versionNumber, ref row, ref column, importPathLookup));

            if (modelData.EdgeLoops.Any(x => x.Faces.Count == 0))
                Debug.WriteLine("Error: Found unreferenced edge loop");

            return (geometryModel, id, versionNumber);
        }

        private static string ReadToDelimiter(StreamReader sr, ref int row, ref int column, string description)
        {
            StringBuilder str = new StringBuilder();

            int intVal = sr.Read();
            column++;
            char val = (char)intVal;
            while (val != ';')
            {
                if (intVal == -1)
                    throw new IOException(String.Format("Unexpected end of file while reading {0}", description));
                else if (val == '\n')
                {
                    column = 1;
                    row++;
                }
                else if (val != '\r')
                    str.Append(val);

                intVal = sr.Read();
                column++;
                val = (char)intVal;
            }

            return str.ToString();
        }
        private static T ReadNumber<T>(StreamReader sr, ref int row, ref int column, string description) where T : IConvertible
        {
            var read = ReadToDelimiter(sr, ref row, ref column, description);

            if (read.Length == 0)
                throw new FormatException(String.Format("Failed to read {0}: Expected number around row {1}, column {2}", description, row, column));

            return (T)Convert.ChangeType(read, typeof(T), CultureInfo.InvariantCulture);
        }
        private static T? ReadNumberOrEmpty<T>(StreamReader sr, ref int row, ref int column, string description)
            where T : struct, IConvertible
        {
            string str = ReadToDelimiter(sr, ref row, ref column, description);
            if (str == "")
                return null;
            return (T?)Convert.ChangeType(str, typeof(T), CultureInfo.InvariantCulture);
        }
        private static string ReadString(StreamReader sr, ref int row, ref int column, string description)
        {
            Int32 length = ReadNumber<Int32>(sr, ref row, ref column, String.Format("String length of {0}", description));
            char[] buffer = new char[length];
            int readCount = sr.ReadBlock(buffer, 0, length);

            if (readCount < length)
                throw new IOException(String.Format("Unexpected end of file while reading {0}", description));

            column += length;
            row += buffer.Count(x => x == '\n');



            return new String(buffer);
        }
        private static bool ReadBool(StreamReader sr, ref int row, ref int column, string description)
        {
            int b = sr.Read();
            column++;

            while (((char)b == '\n' || (char)b == '\r') && b != -1)
            {
                if ((char)b == '\n')
                {
                    column = 1;
                    row++;
                }

                b = sr.Read();
                column++;
            }

            if (b == -1)
                throw new IOException(String.Format("Unexpected end of file while reading {0}", description));

            if ((char)b == '0')
                return false;
            else if ((char)b == '1')
                return true;
            else
                throw new IOException(String.Format("Failed to parse bool {0} at row {1}, column {2}", description, row, column));
        }
        private static void ReadColor(StreamReader sr, DerivedColor color, ref int row, ref int column, string description)
        {
            byte r = ReadNumber<byte>(sr, ref row, ref column, String.Format("{0} - Red", description));
            byte g = ReadNumber<byte>(sr, ref row, ref column, String.Format("{0} - Green", description));
            byte b = ReadNumber<byte>(sr, ref row, ref column, String.Format("{0} - Blue", description));
            byte a = ReadNumber<byte>(sr, ref row, ref column, String.Format("{0} - Alpha", description));
            bool fromParent = ReadBool(sr, ref row, ref column, String.Format("{0} - FromParent", description));

            color.Color = SimColor.FromArgb(a, r, g, b);
            color.IsFromParent = fromParent;
        }
        private static GeometricOrientation ReadOrientation(StreamReader sr, ref int row, ref int column, string description)
        {
            byte b = ReadNumber<byte>(sr, ref row, ref column, description);
            switch (b)
            {
                case 1:
                    return GeometricOrientation.Forward;
                case 2:
                    return GeometricOrientation.Backward;
                case 0:
                    return GeometricOrientation.Undefined;
                default:
                    throw new IOException(String.Format("Failed to parse orientation {0} at row {1}, column {2}", description, row, column));
            }
        }
        private static ParentMigrationData ReadGeometryReference(StreamReader sr, ProjectData projectData, int versionNumber,
            ref int row, ref int column, string description)
        {
            if (versionNumber < 5)
                throw new NotSupportedException(string.Format("SimGeo Format Version {0} does not support Geometry References", versionNumber));

            var isValid = ReadBool(sr, ref row, ref column, description + ".IsValid");
            if (isValid)
            {
                Guid modelGuid = Guid.Empty;
                ulong modelId = ulong.MaxValue;
                if (versionNumber >= 8)
                {
                    modelGuid = ReadGuid(sr, ref row, ref column, description + ".ModelGUID");
                }
                else if (versionNumber >= 5 && versionNumber < 8)
                {
                    modelId = ReadNumber<UInt64>(sr, ref row, ref column, description + ".ModelID");
                }

                ulong geomId = ReadNumber<UInt64>(sr, ref row, ref column, description + ".GeometryID");
                string name = ReadString(sr, ref row, ref column, description + ".Name");

                if (versionNumber >= 8)
                    return new ParentMigrationData(modelGuid, geomId);
                else if (versionNumber >= 5 && versionNumber < 8)
                    return new ParentMigrationData(modelId, geomId);
                else
                    return ParentMigrationData.Empty; //Cannot happen. Would throw an Exception at start of the method
            }
            else
                return ParentMigrationData.Empty;
        }
        private static Guid ReadGuid(StreamReader sr, ref int row, ref int column, string description)
        {
            string guidString = ReadString(sr, ref row, ref column, description);
            if (Guid.TryParseExact(guidString, "N", out var guid))
                return guid;
            else
                throw new FormatException(String.Format("Failed to parse Guid {0}: Wrong Guid format around row {1}, column {2}", description, row, column));

        }

        private delegate T ListReadDelegate<T>(StreamReader sr, ref int row, ref int column);
        private static List<T> ReadList<T>(StreamReader sr, ListReadDelegate<T> elementReadFunc, ref int row, ref int column, string description)
        {
            List<T> list = new List<T>();

            var count = ReadNumber<Int32>(sr, ref row, ref column, description + " - Count");

            for (int i = 0; i < count; ++i)
                list.Add(elementReadFunc(sr, ref row, ref column));

            return list;
        }


        private static void ReadLayer(StreamReader sr, GeometryModelData model, Dictionary<ulong, Layer> layers, ref int row, ref int column)
        {
            var streamPos = sr.BaseStream.Position;

            UInt64 id = ReadNumber<UInt64>(sr, ref row, ref column, "Layer ID");
            UInt64? parentId = ReadNumberOrEmpty<UInt64>(sr, ref row, ref column, "Layer ParentID");
            string name = ReadString(sr, ref row, ref column, "Layer Name");

            //This code is a fix for an unknown bug where two layers had the same Idea.
            if (layers.ContainsKey(id))
            {
                var oldId = id;
                id = 99999999;
                Debug.WriteLine(string.Format("Two layers with same id found:\nLayer \"{0}\" and Layer \"{1}\".\n\nAssigned new Id {2} to layer \"{1}\"",
                    layers[oldId].Name, name, id));
            }

            Layer layer = new Layer(id, model, name);
            layer.IsVisible = ReadBool(sr, ref row, ref column, "Layer IsVisible");

            //Attach to parent
            if (parentId != null)
            {
                if (layers.ContainsKey(parentId.Value))
                    layers[parentId.Value].Layers.Add(layer);
                else
                    throw new Exception(String.Format("An error occurred during loading at position {1}:\n" +
                        "Parent Layer with Id {0} not found",
                        parentId,
                        streamPos));
            }
            else
                model.Layers.Add(layer);

            ReadColor(sr, layer.Color, ref row, ref column, "Layer Color");

            layers[layer.Id] = layer;
        }

        private static (ulong id, string name, ulong layer, bool isVisible, ParentMigrationData parent) ReadBaseGeometryStart(StreamReader sr,
            ProjectData modelStore, int versionNumber, ref int row, ref int column, int fileId,
            List<(SimBaseGeometryReference, ParentMigrationData)> legacyParentRelations)
        {
            var id = ReadNumber<UInt64>(sr, ref row, ref column, "Geometry ID");
            var name = ReadString(sr, ref row, ref column, "Geometry Name");
            var layer = ReadNumber<UInt64>(sr, ref row, ref column, "Geometry Layer ID");
            var isVisible = ReadBool(sr, ref row, ref column, "Geometry IsVisible");

            ParentMigrationData parent = ParentMigrationData.Empty;
            if (versionNumber >= 5 && versionNumber < 12)
            {
                parent = ReadGeometryReference(sr, modelStore, versionNumber, ref row, ref column, "Geometry Parent");
                if (!parent.Equals(ParentMigrationData.Empty))
                {
                    var source = new SimBaseGeometryReference(modelStore.Owner.GlobalID, fileId, id);
                    legacyParentRelations.Add((source, parent));
                }
            }

            return (id, name, layer, isVisible, parent);
        }
        private static void ReadVertex(StreamReader sr, GeometryModelData model, Dictionary<ulong, Layer> layers, Dictionary<ulong, BaseGeometry> geometries,
            ProjectData modelStore, int versionNumber, ref int row, ref int column, int fileId,
            List<(SimBaseGeometryReference, ParentMigrationData)> legacyParentRelations)
        {
            var streamPos = sr.BaseStream.Position;
            var bg = ReadBaseGeometryStart(sr, modelStore, versionNumber, ref row, ref column, fileId, legacyParentRelations);

            if (!layers.ContainsKey(bg.layer))
            {
                throw new Exception(String.Format("An error occurred during loading at position {1}:\n" +
                    "Layer with Id {0} not found",
                    bg.layer, streamPos));
            }

            double x = ReadNumber<double>(sr, ref row, ref column, "Vertex Position.X");
            double y = ReadNumber<double>(sr, ref row, ref column, "Vertex Position.Y");
            double z = ReadNumber<double>(sr, ref row, ref column, "Vertex Position.Z");

            Vertex v = new Vertex(bg.id, layers[bg.layer], bg.name, new SimPoint3D(x, y, z))
            {
                IsVisible = bg.isVisible,
            };
            ReadColor(sr, v.Color, ref row, ref column, "Vertex Color");

            geometries.Add(v.Id, v);

        }
        private static void ReadEdge(StreamReader sr, GeometryModelData model, Dictionary<ulong, Layer> layers, Dictionary<ulong, BaseGeometry> geometries,
            ProjectData modelStore, int versionNumber, ref int row, ref int column, int fileId,
            List<(SimBaseGeometryReference, ParentMigrationData)> legacyParentRelations)
        {
            var streamPos = sr.BaseStream.Position;

            var bg = ReadBaseGeometryStart(sr, modelStore, versionNumber, ref row, ref column, fileId, legacyParentRelations);
            if (!layers.ContainsKey(bg.layer))
            {
                throw new Exception(String.Format("An error occurred during loading at position {1}:\n" +
                    "Layer with Id {0} not found",
                    bg.layer, streamPos));
            }

            var v1 = ReadNumber<UInt64>(sr, ref row, ref column, "Edge Vertex 1");
            if (!geometries.ContainsKey(v1) || !(geometries[v1] is Vertex))
            {
                throw new Exception(String.Format("An error occurred during loading at position {1}:\n" +
                    "Vertex with Id {0} not found",
                    v1, streamPos));
            }

            var v2 = ReadNumber<UInt64>(sr, ref row, ref column, "Edge Vertex 2");
            if (!geometries.ContainsKey(v2) || !(geometries[v2] is Vertex))
            {
                throw new Exception(String.Format("An error occurred during loading at position {1}:\n" +
                    "Vertex with Id {0} not found",
                    v2, streamPos));
            }

            Edge e = new Edge(bg.id, layers[bg.layer], bg.name, new Vertex[] { (Vertex)geometries[v1], (Vertex)geometries[v2] })
            {
                IsVisible = bg.isVisible,
            };
            ReadColor(sr, e.Color, ref row, ref column, "Edge Color");

            geometries.Add(e.Id, e);
        }
        private static void ReadEdgeLoop(StreamReader sr, GeometryModelData model, Dictionary<ulong, Layer> layers, Dictionary<ulong, BaseGeometry> geometries,
            ProjectData modelStore, int versionNumber, ref int row, ref int column, int fileId,
            List<(SimBaseGeometryReference, ParentMigrationData)> legacyParentRelations,
            MultiDictionary<ulong, EdgeLoop> edgeLoopMergeTable)
        {
            var streamPos = sr.BaseStream.Position;
            var bg = ReadBaseGeometryStart(sr, modelStore, versionNumber, ref row, ref column, fileId, legacyParentRelations);

            if (!layers.ContainsKey(bg.layer))
            {
                Layer destroyedLayer = new Layer(bg.layer, model, "Repaired layer");
                model.Layers.Add(destroyedLayer);
                layers.Add(bg.layer, destroyedLayer);
            }

            ulong baseEdgeId = 0;
            GeometricOrientation baseEdgeOrientation = GeometricOrientation.Undefined;
            if (versionNumber >= 16)
            {
                baseEdgeId = ReadNumber<UInt64>(sr, ref row, ref column, "EdgeLoop - BaseEdge ID");
                baseEdgeOrientation = ReadOrientation(sr, ref row, ref column, "EdgeLoop - BaseEdge Orientation");
            }

            var edgeCount = ReadNumber<Int32>(sr, ref row, ref column, "EdgeLoop - Edge Count");
            List<Edge> edges = new List<Edge>();
            edges.Capacity = edgeCount;
            for (int i = 0; i < edgeCount; ++i)
            {
                ulong eid = ReadNumber<UInt64>(sr, ref row, ref column, "EdgeLoop - Edge ID");
                if (!geometries.ContainsKey(eid) || !(geometries[eid] is Edge))
                {
                    throw new Exception(String.Format("An error occurred during loading at position {1}:\n" +
                    "Edge with Id {0} not found",
                    eid, streamPos));
                }

                edges.Add((Edge)geometries[eid]);
            }

            //Check if an EdgeLoop with the same edges exists
            var hash = EdgeListHash(edges);
            if (edgeLoopMergeTable.TryGetValues(hash, out var loopCandidates))
            {
                var edgesHashSet = edges.ToHashSet();

                var matchingLoop = loopCandidates.FirstOrDefault(x => x.Edges.Count == edges.Count &&
                    x.Edges.All(pe => edgesHashSet.Contains(pe.Edge)));
                //Register existing loop with the second Id
                if (matchingLoop != null)
                {
                    geometries.Add(bg.id, matchingLoop);

                    //Skip over color
                    DerivedColor skipColor = new DerivedColor(SimColors.Red);
                    ReadColor(sr, skipColor, ref row, ref column, "EdgeLoop Color");

                    return;
                }
            }

            Edge baseEdge = null;
            if (versionNumber < 16)
            {
                baseEdge = edges[0];
                baseEdgeOrientation = GeometricOrientation.Forward;
            }
            else
                baseEdge = (Edge)geometries[baseEdgeId];

            EdgeLoop loop = new EdgeLoop(bg.id, layers[bg.layer], bg.name, edges, baseEdge, baseEdgeOrientation)
            {
                IsVisible = bg.isVisible,
            };
            ReadColor(sr, loop.Color, ref row, ref column, "EdgeLoop Color");

            edgeLoopMergeTable.Add(hash, loop);
            geometries.Add(loop.Id, loop);
        }
        private static void ReadPolyline(StreamReader sr, GeometryModelData model, Dictionary<ulong, Layer> layers, Dictionary<ulong, BaseGeometry> geometries,
            ProjectData modelStore, int versionNumber, ref int row, ref int column, int fileId,
            List<(SimBaseGeometryReference, ParentMigrationData)> legacyParentRelations)
        {
            var streamPos = sr.BaseStream.Position;
            var bg = ReadBaseGeometryStart(sr, modelStore, versionNumber, ref row, ref column, fileId, legacyParentRelations);

            if (!layers.ContainsKey(bg.layer))
            {
                throw new Exception(String.Format("An error occurred during loading at position {1}:\n" +
                    "Layer with Id {0} not found",
                    bg.layer, streamPos));
            }

            var edgeCount = ReadNumber<Int32>(sr, ref row, ref column, "Polyline - Edge Count");
            List<Edge> edges = new List<Edge>();
            edges.Capacity = edgeCount;
            for (int i = 0; i < edgeCount; ++i)
            {
                ulong eid = ReadNumber<UInt64>(sr, ref row, ref column, "Polyline - Edge ID");
                if (!geometries.ContainsKey(eid) || !(geometries[eid] is Edge))
                {
                    throw new Exception(String.Format("An error occurred during loading at position {1}:\n" +
                        "Edge with Id {0} not found",
                        eid, streamPos));
                }

                edges.Add((Edge)geometries[eid]);
            }

            Polyline pl = new Polyline(bg.id, layers[bg.layer], bg.name, edges)
            {
                IsVisible = bg.isVisible,
            };
            ReadColor(sr, pl.Color, ref row, ref column, "Polyline Color");

            geometries.Add(pl.Id, pl);
        }
        private static void ReadFace(StreamReader sr, GeometryModelData model, Dictionary<ulong, Layer> layers, Dictionary<ulong, BaseGeometry> geometries,
            ProjectData modelStore, int versionNumber, ref int row, ref int column, int fileId,
            List<(SimBaseGeometryReference, ParentMigrationData)> legacyParentRelations)
        {
            var streamPos = sr.BaseStream.Position;
            var bg = ReadBaseGeometryStart(sr, modelStore, versionNumber, ref row, ref column, fileId, legacyParentRelations);

            if (!layers.ContainsKey(bg.layer))
            {
                throw new Exception(String.Format("An error occurred during loading at position {1}:\n" +
                    "Layer with Id {0} not found",
                    bg.layer, streamPos));
            }

            ulong boundaryId = ReadNumber<UInt64>(sr, ref row, ref column, "Face Boundary ID");
            if (!geometries.ContainsKey(boundaryId) || !(geometries[boundaryId] is EdgeLoop))
            {
                throw new Exception(String.Format("An error occurred during loading at position {1}:\n" +
                    "EdgeLoop with Id {0} not found",
                    boundaryId, streamPos));
            }

            Edge baseEdge = null;
            if (versionNumber >= 15 && versionNumber < 16) //BaseEdge was added in 15, moved to EdgeLoop in 16
            {
                var baseEdgeId = ReadNumber<UInt64>(sr, ref row, ref column, "Face BaseEdge ID");
                baseEdge = (Edge)geometries[baseEdgeId];
            }

            int holeCount = ReadNumber<Int32>(sr, ref row, ref column, "Face Hole Count");
            List<EdgeLoop> holes = new List<EdgeLoop>();
            holes.Capacity = holeCount;
            for (int i = 0; i < holeCount; ++i)
            {
                ulong hid = ReadNumber<UInt64>(sr, ref row, ref column, "Face Hole ID");
                if (!geometries.ContainsKey(hid) || !(geometries[hid] is EdgeLoop))
                {
                    throw new Exception(String.Format("An error occurred during loading at position {1}:\n" +
                        "EdgeLoop with Id {0} not found",
                        hid, streamPos));
                }

                holes.Add((EdgeLoop)geometries[hid]);
            }

            var boundary = (EdgeLoop)geometries[boundaryId];
            if (versionNumber >= 15 && versionNumber < 16) //BaseEdge was added in 15, moved to EdgeLoop in 16
            {
                var originalOrientation = boundary.Edges.First(x => x.Edge == baseEdge).Orientation;
                boundary.BaseEdge = baseEdge;
                boundary.BaseEdgeOrientation = originalOrientation;
            }

            GeometricOrientation orient = ReadOrientation(sr, ref row, ref column, "Face Orientation");
            Face f = new Face(bg.id, layers[bg.layer], bg.name, boundary, orient, holes)
            {
                IsVisible = bg.isVisible,
            };
            ReadColor(sr, f.Color, ref row, ref column, "Face Color");

            geometries.Add(f.Id, f);
        }
        private static void ReadVolume(StreamReader sr, GeometryModelData model, Dictionary<ulong, Layer> layers, Dictionary<ulong, BaseGeometry> geometries,
            ProjectData modelStore, int versionNumber, ref int row, ref int column, int fileId,
            List<(SimBaseGeometryReference, ParentMigrationData)> legacyParentRelations)
        {
            var streamPos = sr.BaseStream.Position;
            var bg = ReadBaseGeometryStart(sr, modelStore, versionNumber, ref row, ref column, fileId, legacyParentRelations);

            if (!layers.ContainsKey(bg.layer))
            {
                throw new Exception(String.Format("An error occurred during loading at position {1}:\n" +
                    "Layer with Id {0} not found",
                    bg.layer, streamPos));
            }

            int faceCount = ReadNumber<Int32>(sr, ref row, ref column, "Volume Face Count");
            List<Face> faces = new List<Face>();
            faces.Capacity = faceCount;
            for (int i = 0; i < faceCount; ++i)
            {
                ulong fid = ReadNumber<UInt64>(sr, ref row, ref column, "Volume Face ID");
                if (!geometries.ContainsKey(fid) || !(geometries[fid] is Face))
                {
                    throw new Exception(String.Format("An error occurred during loading at position {1}:\n" +
                        "Face with Id {0} not found",
                        fid, streamPos));
                }

                faces.Add((Face)geometries[fid]);
            }

            // Migrate holes. Add all hole faces to the volume too. Was added in version 14
            if (versionNumber < 14)
            {
                var holeFaces = new List<Face>();
                foreach (var face in faces)
                {
                    foreach (var hole in face.Holes)
                    {
                        var holeFace = hole.Faces.Find(x => x != face);
                        if (holeFace != null)
                            holeFaces.Add(holeFace);
                    }
                }

                faces.AddRange(holeFaces);

                // remove duplicates
                faces = faces.Distinct().ToList();
            }

            Volume v = new Volume(bg.id, layers[bg.layer], bg.name, faces)
            {
                IsVisible = bg.isVisible,
            };
            ReadColor(sr, v.Color, ref row, ref column, "Volume Color");

            geometries.Add(v.Id, v);
        }
        private static void ReadProxyGeometry(StreamReader sr, GeometryModelData model, Dictionary<ulong, Layer> layers, Dictionary<ulong, BaseGeometry> geometries,
            ProjectData modelStore, int versionNumber, ref int row, ref int column, int fileId,
            List<(SimBaseGeometryReference, ParentMigrationData)> legacyParentRelations)
        {
            var streamPos = sr.BaseStream.Position;
            var bg = ReadBaseGeometryStart(sr, modelStore, versionNumber, ref row, ref column, fileId, legacyParentRelations);

            if (!layers.ContainsKey(bg.layer))
            {
                throw new Exception(String.Format("An error occurred during loading at position {1}:\n" +
                    "Layer with Id {0} not found",
                    bg.layer, streamPos));
            }

            var vertexId = ReadNumber<UInt64>(sr, ref row, ref column, "Proxy Vertex ID");
            Vertex vertex = null;
            if (geometries.ContainsKey(vertexId) && geometries[vertexId] is Vertex)
            {
                vertex = (Vertex)geometries[vertexId];
            }
            else
            {
                throw new Exception(String.Format("An error occurred during loading at position {1}:\n" +
                        "Vertex with Id {0} not found",
                        vertexId, streamPos));
            }

            //Size
            SimVector3D size = new SimVector3D(
                ReadNumber<double>(sr, ref row, ref column, "Size X"),
                ReadNumber<double>(sr, ref row, ref column, "Size Y"),
                ReadNumber<double>(sr, ref row, ref column, "Size Z")
                );

            SimQuaternion rotation = SimQuaternion.Identity;
            if (versionNumber >= 9)
            {
                rotation = new SimQuaternion(
                    ReadNumber<double>(sr, ref row, ref column, "Rotation X"),
                    ReadNumber<double>(sr, ref row, ref column, "Rotation Y"),
                    ReadNumber<double>(sr, ref row, ref column, "Rotation Z"),
                    ReadNumber<double>(sr, ref row, ref column, "Rotation W")
                    );
            }

            //Positions
            var positions = ReadList<SimPoint3D>(sr,
                (StreamReader lsr, ref int lrow, ref int lcolumn) =>
                {
                    return new SimPoint3D(
                        ReadNumber<double>(sr, ref lrow, ref lcolumn, "Proxy Position X"),
                        ReadNumber<double>(sr, ref lrow, ref lcolumn, "Proxy Position Y"),
                        ReadNumber<double>(sr, ref lrow, ref lcolumn, "Proxy Position Z")
                        );
                }
                , ref row, ref column, "Proxy Positions");

            //Normals
            var normals = ReadList<SimVector3D>(sr,
                (StreamReader lsr, ref int lrow, ref int lcolumn) =>
                {
                    return new SimVector3D(
                        ReadNumber<double>(sr, ref lrow, ref lcolumn, "Proxy Normals X"),
                        ReadNumber<double>(sr, ref lrow, ref lcolumn, "Proxy Normals Y"),
                        ReadNumber<double>(sr, ref lrow, ref lcolumn, "Proxy Normals Z")
                        );
                }
                , ref row, ref column, "Proxy Normals");

            //Indices
            var indices = ReadList<Int32>(sr,
                (StreamReader lsr, ref int lrow, ref int lcolumn) =>
                {
                    return ReadNumber<Int32>(sr, ref lrow, ref lcolumn, "Proxy Index");
                }
                , ref row, ref column, "Proxy Indices");

            ProxyGeometry proxy = new ProxyGeometry(layers[bg.layer], bg.name, vertex)
            {
                IsVisible = bg.isVisible,
                Size = size,
                Rotation = rotation,
                Positions = positions,
                Normals = normals,
                Indices = indices,
            };

            DerivedColor x = new DerivedColor(SimColors.Red);
            ReadColor(sr, x, ref row, ref column, "Proxy Color");

            geometries.Add(proxy.Id, proxy);
        }

        private static void ReadGeoRef(StreamReader sr, GeometryModelData model, Dictionary<ulong, BaseGeometry> geometries,
            int versionNumber, ref int row, ref int column)
        {
            ulong vertexID = ReadNumber<ulong>(sr, ref row, ref column, "GeoRef ID");
            double x = ReadNumber<double>(sr, ref row, ref column, "GeoRef X");
            double y = ReadNumber<double>(sr, ref row, ref column, "GeoRef Y");
            double z = ReadNumber<double>(sr, ref row, ref column, "GeoRef Z");

            if (geometries.TryGetValue(vertexID, out var element))
            {
                var vertex = element as Vertex;
                if (vertex != null)
                    model.GeoReferences.Add(new GeoReference(vertex, new SimPoint3D(x, y, z)));
            }
        }

        private static void ReadValueMapping(StreamReader sr, GeometryModel model, ProjectData projectData,
            int versionNumber, ref int row, ref int column)
        {
            long valueMappingId = ReadNumber<long>(sr, ref row, ref column, "Value Mappign ID");

            var mapping = projectData.IdGenerator.GetById<SimValueMapping>(new SimId(
                projectData.ValueMappings.CalledFromLocation, valueMappingId
                ));
            if (mapping != null)
                model.ValueMappings.Add(mapping);
        }
        private static Int32 ReadLinkedModel(StreamReader sr, GeometryModel model, ProjectData projectData, int versionNumber, ref int row, ref int column,
            Dictionary<string, string> importPathLookup)
        {
            // migrate from file path to resource Id
            if (versionNumber < 12)
            {
                var path = ReadString(sr, ref row, ref column, "Linked Model Path");
                if (importPathLookup != null && !importPathLookup.TryGetValue(path, out path))
                {
                    return -1; // could not find in lookup
                }
                var file = new FileInfo(Path.Combine(Path.GetDirectoryName(model.File.CurrentFullPath), path));
                var resource = projectData.AssetManager.GetResource(file);

                if (resource == null) // resource not found
                    return -1;

                return resource.Key;
            }
            return ReadNumber<Int32>(sr, ref row, ref column, "Linked Model File ID");
        }
        private static (Guid guid, bool success) LegacyIdToGuid(ulong oldId, Dictionary<ulong, Guid> idToGuid)
        {
            bool success = true;

            if (!idToGuid.TryGetValue(oldId, out var guid))
            {
                guid = Guid.Empty;
                success = false;
            }
            return ((guid, success));
        }

        private static void ConvertIdBasedReferencesAndRelation(ProjectData projectData, Dictionary<Guid, GeometryModel> modelLookup, Dictionary<ulong, Guid> idToGuid, List<SimGeoIOError> errors,
            List<(SimBaseGeometryReference source, ParentMigrationData target)> legacyParentRelations)
        {
            SimTaxonomyEntry legacyParentTaxEntry = null;
            for (int i = 0; i < legacyParentRelations.Count; i++)
            {
                (var source, var parent) = legacyParentRelations[i];
                if (legacyParentTaxEntry == null)
                    legacyParentTaxEntry = GetLegacyParentTaxonomyEntry(projectData);

                // convert old Ids to GUIDs
                if (parent.IsLegacy)
                {
                    var convertedReference = LegacyIdToGuid(parent.LegacyId, idToGuid);

                    if (!convertedReference.success)
                    {
                        errors.Add(new SimGeoIOError(SimGeoIOErrorReason.ReferenceConvertFailed, new object[] { source }));
                    }

                    parent = new ParentMigrationData(convertedReference.guid, parent.GeometryId);
                }

                // find parent and add geometry relation
                if (modelLookup.TryGetValue(parent.ModelId, out var model))
                {
                    var target = new SimBaseGeometryReference(source.ProjectId, model.File.Key, parent.GeometryId);
                    var relation = new SimGeometryRelation(new SimTaxonomyEntryReference(legacyParentTaxEntry), source, target);
                    projectData.GeometryRelations.Add(relation);
                }
                else
                {
                    errors.Add(new SimGeoIOError(SimGeoIOErrorReason.ReferenceConvertFailed, new object[] { source }));
                }
            }
        }


        private static ulong EdgeListHash(List<Edge> edges)
        {
            ulong hash = (ulong)edges.Count;

            foreach (var edge in edges)
                hash += unchecked(hash * 314159 + edge.Id);

            return hash;
        }

        #endregion
    }
}
