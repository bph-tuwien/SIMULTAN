using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Projects;
using SIMULTAN.Utils.Files;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

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
        public static int SimGeoVersion => 10;

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
        /// Stores the model to a file
        /// </summary>
        /// <param name="model">The model to store</param>
        /// <param name="file">FileInfo of the target file. The file gets overridden without conformation!!</param>
        /// <param name="mode">Format in which the file should be written</param>
        public static bool Save(GeometryModel model, ResourceFileEntry file, WriteMode mode)
        {
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
                    valid = valid && Save(lm, lm.File, mode);
            }
            catch (IOException e)
            {
                valid = false;
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
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
            Dictionary<ulong, Guid> idToGuid = new Dictionary<ulong, Guid>();

            var model = LoadWithoutCheck(geometryFile, projectData, idToGuid, offsetAlg, errors);
            ConvertIdBasedReferences(model, idToGuid, errors);

            return model;
        }

        private static GeometryModel LoadWithoutCheck(ResourceFileEntry geometryFile,
            ProjectData projectData, Dictionary<ulong, Guid> idToGuid, OffsetAlgorithm offsetAlg, List<SimGeoIOError> errors)
        {
            if (!geometryFile.Exists)
                throw new FileNotFoundException(geometryFile.CurrentFullPath);

            if (projectData.GeometryModels.TryGetGeometryModel(geometryFile, out var existingModel, false))
            {
                //Model is already loaded
                return existingModel;
            }
            else
            {
                try
                {
                    var encoding = GetEncoding(geometryFile.CurrentFullPath);

                    GeometryModel model = null;
                    List<FileInfo> linkedModels = new List<FileInfo>();

                    using (FileStream fs = new FileStream(geometryFile.CurrentFullPath, FileMode.Open))
                    {
                        using (StreamReader sr = new StreamReader(fs, encoding))
                        {
                            var formatIdent = (char)sr.Read();
                            int row = 1, column = 2;
                            if (formatIdent == 'T')
                                model = LoadPlaintext(sr, geometryFile, linkedModels, projectData, idToGuid, ref row, ref column, offsetAlg);
                            else
                                throw new IOException("Unknown format identifier");
                        }
                    }

                    if (model != null)
                    {
                        foreach (var file in linkedModels)
                        {
                            if (!projectData.AssetManager.IsValidResourcePath(file, false))
                            {
                                errors.Add(new SimGeoIOError(SimGeoIOErrorReason.InvalidLinkedModel, new object[]
                                {
                                    file.FullName
                                }));
                            }
                            else
                            {
                                var resource = projectData.AssetManager.GetResource(file);

                                if (resource == null)
                                {
                                    int key = projectData.AssetManager.AddResourceEntry(file);
                                    resource = (ResourceFileEntry)projectData.AssetManager.GetResource(key);
                                }

                                var linkedModel = LoadWithoutCheck(resource, projectData, idToGuid, offsetAlg, errors);
                                model.LinkedModels.Add(linkedModel);
                            }
                        }
                    }

                    return model;
                }
                catch (Exception e) when (!Debugger.IsAttached)
                {
                    throw e;
                }
            }
        }

        #region Write

        private static void SavePlaintext(StreamWriter sw, GeometryModel model)
        {
            //HEADER
            sw.Write('T');
            WriteNumberPlaintext<Int32>(sw, SimGeoIO.SimGeoVersion);
            WriteGuidPlaintext(sw, model.Id);
            WriteNumberPlaintext<UInt64>(sw, (UInt64)model.Permissions.ModelPermissions);
            WriteNumberPlaintext<UInt64>(sw, (UInt64)model.Permissions.GeometryPermissions);
            WriteNumberPlaintext<UInt64>(sw, (UInt64)model.Permissions.LayerPermissions);

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
                WriteGeoRefPlainText(sw, r);

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
        private static void WriteGeometryReferencePlaintext(StreamWriter sw, GeometryReference reference)
        {
            WriteBoolPlaintext(sw, reference != null); //IsValid
            if (reference != null)
            {
                WriteGuidPlaintext(sw, reference.ModelID);
                WriteNumberPlaintext<UInt64>(sw, reference.GeometryID);
                WriteStringPlaintext(sw, reference.Name);
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
            if (geo.Id == 16548)
                Console.WriteLine("here");

            WriteNumberPlaintext<UInt64>(sw, geo.Id);
            WriteStringPlaintext(sw, geo.Name);
            WriteNumberPlaintext<UInt64>(sw, geo.Layer.Id);
            WriteBoolPlaintext(sw, geo.IsVisible);
            WriteGeometryReferencePlaintext(sw, geo.Parent);
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

        private static void WriteGeoRefPlainText(StreamWriter sw, GeoReference reference)
        {
            WriteNumberPlaintext<ulong>(sw, reference.Vertex.Id);
            WriteNumberPlaintext<double>(sw, reference.ReferencePoint.X);
            WriteNumberPlaintext<double>(sw, reference.ReferencePoint.Y);
            WriteNumberPlaintext<double>(sw, reference.ReferencePoint.Z);
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
            var fileInfo = new FileInfo(sourceModel.File.CurrentFullPath);

            var relativePath = FileSystemNavigation.GetRelativePath(fileInfo.Directory.FullName, linkedModel.File.CurrentFullPath);
            WriteStringPlaintext(sw, relativePath);
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
            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
            if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
            if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return Encoding.UTF32;
            return Encoding.Default;
        }

        private static GeometryModel LoadPlaintext(StreamReader stream, ResourceFileEntry file,
            List<FileInfo> linkedModels, ProjectData projectData, Dictionary<ulong, Guid> idToGuid,
            ref int row, ref int column, OffsetAlgorithm offsetAlg)
        {
            //Parse header
            int versionNumber = ReadNumber<Int32>(stream, ref row, ref column, "Version Number");

            Guid id = Guid.NewGuid();
            if (versionNumber >= 8)
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

            UInt64 nextGeoId = 0;
            if (versionNumber >= 10)
                nextGeoId = ReadNumber<UInt64>(stream, ref row, ref column, "Next Geometry ID");

            string name = ReadString(stream, ref row, ref column, "Model Name");
            bool isVisible = ReadBool(stream, ref row, ref column, "Model IsVisible");

            GeometryModelData modelData = new GeometryModelData(nextGeoId);
            modelData.OffsetModel.Generator.Algorithm = offsetAlg;
            modelData.StartBatchOperation();

            //Layer
            Dictionary<ulong, Layer> layers = new Dictionary<ulong, Layer>();
            for (int i = 0; i < layerCount; ++i)
                ReadLayer(stream, modelData, layers, ref row, ref column);

            Dictionary<ulong, BaseGeometry> geometries = new Dictionary<ulong, BaseGeometry>();
            for (int i = 0; i < vertexCount; ++i)
                ReadVertex(stream, modelData, layers, geometries, projectData, versionNumber, ref row, ref column);

            for (int i = 0; i < edgeCount; ++i)
                ReadEdge(stream, modelData, layers, geometries, projectData, versionNumber, ref row, ref column);

            for (int i = 0; i < edgeLoopCount; ++i)
                ReadEdgeLoop(stream, modelData, layers, geometries, projectData, versionNumber, ref row, ref column);

            for (int i = 0; i < polylineCount; ++i)
                ReadPolyline(stream, modelData, layers, geometries, projectData, versionNumber, ref row, ref column);

            for (int i = 0; i < faceCount; ++i)
                ReadFace(stream, modelData, layers, geometries, projectData, versionNumber, ref row, ref column);

            for (int i = 0; i < volumeCount; ++i)
                ReadVolume(stream, modelData, layers, geometries, projectData, versionNumber, ref row, ref column);

            for (int i = 0; i < proxyCount; ++i)
                ReadProxyGeometry(stream, modelData, layers, geometries, projectData, versionNumber, ref row, ref column);

            modelData.EndBatchOperation();

            var geometryModel = new GeometryModel(id, name, file, permission, modelData);

            for (int i = 0; i < geoRefCount; ++i)
                ReadGeoRef(stream, modelData, geometries, versionNumber, ref row, ref column);

            for (int i = 0; i < linkedModelCount; ++i)
                linkedModels.Add(ReadLinkedModel(stream, geometryModel, ref row, ref column));

            if (modelData.EdgeLoops.Any(x => x.Faces.Count == 0))
                Console.WriteLine("Error: Found unreferenced edge loop");

            return geometryModel;
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

            color.Color = Color.FromArgb(a, r, g, b);
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
        private static GeometryReference ReadGeometryReference(StreamReader sr, ProjectData projectData, int versionNumber,
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
                    return new GeometryReference(modelGuid, geomId, name, null, projectData.GeometryModels);
                else if (versionNumber >= 5 && versionNumber < 8)
                    return new IdBasedGeometryReference(modelId, geomId, name, null, projectData.GeometryModels);
                else
                    return null; //Cannot happen. Would throw an Exception at start of the method
            }
            else
                return null;
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
                Console.WriteLine(string.Format("Two layers with same id found:\nLayer \"{0}\" and Layer \"{1}\".\n\nAssigned new Id {2} to layer \"{1}\"",
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

        private static (ulong id, string name, ulong layer, bool isVisible, GeometryReference parent) ReadBaseGeometryStart(StreamReader sr,
            ProjectData modelStore, int versionNumber, ref int row, ref int column)
        {
            var id = ReadNumber<UInt64>(sr, ref row, ref column, "Geometry ID");
            var name = ReadString(sr, ref row, ref column, "Geometry Name");
            var layer = ReadNumber<UInt64>(sr, ref row, ref column, "Geometry Layer ID");
            var isVisible = ReadBool(sr, ref row, ref column, "Geometry IsVisible");

            GeometryReference parent = null;
            if (versionNumber >= 5)
            {
                parent = ReadGeometryReference(sr, modelStore, versionNumber, ref row, ref column, "Geometry Parent");
            }

            return (
                id, name, layer, isVisible, parent
                );
        }
        private static void ReadVertex(StreamReader sr, GeometryModelData model, Dictionary<ulong, Layer> layers, Dictionary<ulong, BaseGeometry> geometries,
            ProjectData modelStore, int versionNumber, ref int row, ref int column)
        {
            var streamPos = sr.BaseStream.Position;
            var bg = ReadBaseGeometryStart(sr, modelStore, versionNumber, ref row, ref column);

            if (!layers.ContainsKey(bg.layer))
            {
                throw new Exception(String.Format("An error occurred during loading at position {1}:\n" +
                    "Layer with Id {0} not found",
                    bg.layer, streamPos));
            }

            double x = ReadNumber<double>(sr, ref row, ref column, "Vertex Position.X");
            double y = ReadNumber<double>(sr, ref row, ref column, "Vertex Position.Y");
            double z = ReadNumber<double>(sr, ref row, ref column, "Vertex Position.Z");

            Vertex v = new Vertex(bg.id, layers[bg.layer], bg.name, new Point3D(x, y, z))
            {
                IsVisible = bg.isVisible,
                Parent = bg.parent,
            };
            ReadColor(sr, v.Color, ref row, ref column, "Vertex Color");

            geometries.Add(v.Id, v);

        }
        private static void ReadEdge(StreamReader sr, GeometryModelData model, Dictionary<ulong, Layer> layers, Dictionary<ulong, BaseGeometry> geometries,
            ProjectData modelStore, int versionNumber, ref int row, ref int column)
        {
            var streamPos = sr.BaseStream.Position;

            var bg = ReadBaseGeometryStart(sr, modelStore, versionNumber, ref row, ref column);
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
                Parent = bg.parent,
            };
            ReadColor(sr, e.Color, ref row, ref column, "Edge Color");

            geometries.Add(e.Id, e);
        }
        private static void ReadEdgeLoop(StreamReader sr, GeometryModelData model, Dictionary<ulong, Layer> layers, Dictionary<ulong, BaseGeometry> geometries,
            ProjectData modelStore, int versionNumber, ref int row, ref int column)
        {
            var streamPos = sr.BaseStream.Position;
            var bg = ReadBaseGeometryStart(sr, modelStore, versionNumber, ref row, ref column);

            if (!layers.ContainsKey(bg.layer))
            {
                Layer destroyedLayer = new Layer(bg.layer, model, "Repaired layer");
                model.Layers.Add(destroyedLayer);
                layers.Add(bg.layer, destroyedLayer);
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

            EdgeLoop loop = new EdgeLoop(bg.id, layers[bg.layer], bg.name, edges)
            {
                IsVisible = bg.isVisible,
                Parent = bg.parent,
            };
            ReadColor(sr, loop.Color, ref row, ref column, "EdgeLoop Color");

            geometries.Add(loop.Id, loop);
        }
        private static void ReadPolyline(StreamReader sr, GeometryModelData model, Dictionary<ulong, Layer> layers, Dictionary<ulong, BaseGeometry> geometries,
            ProjectData modelStore, int versionNumber, ref int row, ref int column)
        {
            var streamPos = sr.BaseStream.Position;
            var bg = ReadBaseGeometryStart(sr, modelStore, versionNumber, ref row, ref column);

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
                Parent = bg.parent,
            };
            ReadColor(sr, pl.Color, ref row, ref column, "Polyline Color");

            geometries.Add(pl.Id, pl);
        }
        private static void ReadFace(StreamReader sr, GeometryModelData model, Dictionary<ulong, Layer> layers, Dictionary<ulong, BaseGeometry> geometries,
            ProjectData modelStore, int versionNumber, ref int row, ref int column)
        {
            var streamPos = sr.BaseStream.Position;
            var bg = ReadBaseGeometryStart(sr, modelStore, versionNumber, ref row, ref column);

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

            GeometricOrientation orient = ReadOrientation(sr, ref row, ref column, "Face Orientation");
            Face f = new Face(bg.id, layers[bg.layer], bg.name, (EdgeLoop)geometries[boundaryId], orient, holes)
            {
                IsVisible = bg.isVisible,
                Parent = bg.parent,
            };
            ReadColor(sr, f.Color, ref row, ref column, "Face Color");

            geometries.Add(f.Id, f);
        }
        private static void ReadVolume(StreamReader sr, GeometryModelData model, Dictionary<ulong, Layer> layers, Dictionary<ulong, BaseGeometry> geometries,
            ProjectData modelStore, int versionNumber, ref int row, ref int column)
        {
            var streamPos = sr.BaseStream.Position;
            var bg = ReadBaseGeometryStart(sr, modelStore, versionNumber, ref row, ref column);

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

            Volume v = new Volume(bg.id, layers[bg.layer], bg.name, faces)
            {
                IsVisible = bg.isVisible,
                Parent = bg.parent,
            };
            ReadColor(sr, v.Color, ref row, ref column, "Volume Color");

            geometries.Add(v.Id, v);
        }
        private static void ReadProxyGeometry(StreamReader sr, GeometryModelData model, Dictionary<ulong, Layer> layers, Dictionary<ulong, BaseGeometry> geometries,
            ProjectData modelStore, int versionNumber, ref int row, ref int column)
        {
            var streamPos = sr.BaseStream.Position;
            var bg = ReadBaseGeometryStart(sr, modelStore, versionNumber, ref row, ref column);

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
            Vector3D size = new Vector3D(
                ReadNumber<double>(sr, ref row, ref column, "Size X"),
                ReadNumber<double>(sr, ref row, ref column, "Size Y"),
                ReadNumber<double>(sr, ref row, ref column, "Size Z")
                );

            Quaternion rotation = Quaternion.Identity;
            if (versionNumber >= 9)
            {
                rotation = new Quaternion(
                    ReadNumber<double>(sr, ref row, ref column, "Rotation X"),
                    ReadNumber<double>(sr, ref row, ref column, "Rotation Y"),
                    ReadNumber<double>(sr, ref row, ref column, "Rotation Z"),
                    ReadNumber<double>(sr, ref row, ref column, "Rotation W")
                    );
            }

            //Positions
            var positions = ReadList<Point3D>(sr,
                (StreamReader lsr, ref int lrow, ref int lcolumn) =>
                {
                    return new Point3D(
                        ReadNumber<double>(sr, ref lrow, ref lcolumn, "Proxy Position X"),
                        ReadNumber<double>(sr, ref lrow, ref lcolumn, "Proxy Position Y"),
                        ReadNumber<double>(sr, ref lrow, ref lcolumn, "Proxy Position Z")
                        );
                }
                , ref row, ref column, "Proxy Positions");

            //Normals
            var normals = ReadList<Vector3D>(sr,
                (StreamReader lsr, ref int lrow, ref int lcolumn) =>
                {
                    return new Vector3D(
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
                Parent = bg.parent,
            };
            ReadColor(sr, proxy.Color, ref row, ref column, "Proxy Color");

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
                    model.GeoReferences.Add(new GeoReference(vertex, new Point3D(x, y, z)));
            }
        }

        private static FileInfo ReadLinkedModel(StreamReader sr, GeometryModel model, ref int row, ref int column)
        {
            var path = ReadString(sr, ref row, ref column, "Linked Model Path");
            return new FileInfo(Path.Combine(Path.GetDirectoryName(model.File.CurrentFullPath), path));
        }

        private static void ConvertIdBasedReferences(GeometryModel model, Dictionary<ulong, Guid> idToGuid, List<SimGeoIOError> errors)
        {
            if (idToGuid.Count == 0) //When there is no model that had an Id, checking for references is not necessary
                return;

            foreach (var subModel in model.LinkedModels)
            {
                ConvertIdBasedReferences(subModel, idToGuid, errors);
            }

            foreach (var geo in model.Geometry.Geometries)
            {
                if (geo.Parent is IdBasedGeometryReference idRef)
                {
                    var convertedReference = idRef.ToGeometryReference(idToGuid);

                    if (!convertedReference.success)
                    {
                        errors.Add(new SimGeoIOError(SimGeoIOErrorReason.ReferenceConvertFailed,
                            new object[] { geo.Name }));
                    }

                    geo.Parent = convertedReference.reference;
                }
            }
        }

        #endregion
    }
}
