using Assimp;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.JSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace SIMULTAN.Serializer.XMI
{
    /// <summary>
    /// Exporter for the XMI format. Either exports the whole project or just a selected network with all attached data
    /// </summary>
    public class XMIExporter
    {
        private static Dictionary<Type, Func<object, string>> primitiveTypes = new Dictionary<Type, Func<object, string>>()
        {
            { typeof(int), x => ((int)x).ToString() },
            { typeof(uint), x => ((uint)x).ToString() },
            { typeof(long), x => ((long)x).ToString() },
            { typeof(ulong), x => ((ulong)x).ToString() },

            { typeof(string), x => (string)x },

            { typeof(double), x => ((double)x).ToString(CultureInfo.InvariantCulture) },

            { typeof(bool), x => ((bool)x) ? "true" : "false" },
        };

        /// <summary>
        /// Exports the whole project to the XMI format
        /// </summary>
        /// <param name="projectData">The data to export</param>
        /// <param name="fileToSave">Location of the target file. Has to have the .xmi extension</param>
        public static void Export(ProjectData projectData, FileInfo fileToSave)
        {
            if (fileToSave.Extension != ".xmi")
                throw new ArgumentException("not a .xmi file");

            using (StreamWriter sw = new StreamWriter(fileToSave.FullName))
            {
                Export(projectData, sw);
            }
        }

        /// <summary>
        /// Exports some networks with their attached data to the XMI format
        /// </summary>
        /// <param name="projectData">The data to export</param>
        /// <param name="networks">The networks to export</param>
        /// <param name="fileToSave">Location of the target file. Has to have the .xmi extension</param>
        public static void Export(ProjectData projectData, IEnumerable<SimNetwork> networks, FileInfo fileToSave)
        {
            if (fileToSave.Extension != ".xmi")
                throw new ArgumentException("not a .xmi file");

            using (StreamWriter sw = new StreamWriter(fileToSave.FullName))
            {
                Export(projectData, networks, sw);
            }
        }

        /// <summary>
        /// Exports the whole project to the XMI format
        /// </summary>
        /// <param name="projectData">The data to export</param>
        /// <param name="writer">The text writer into which the XMI data should be written</param>
        public static void Export(ProjectData projectData, TextWriter writer)
        {
            var serializableProjectData = new ProjectSerializable(projectData);
            Serialize(writer, serializableProjectData);
        }
        /// <summary>
        /// Exports some networks with their attached data to the XMI format
        /// </summary>
        /// <param name="projectData">The data to export</param>
        /// <param name="networks">The networks to export</param>
        /// <param name="writer">The text writer into which the XMI data should be written</param>
        public static void Export(ProjectData projectData, IEnumerable<SimNetwork> networks, TextWriter writer)
        {
            var serializableNetworks = new List<SimNetworkSerializable>();
            var serializableComponents = new List<SimComponentSerializable>();
            var componentsToExport = new List<SimComponent>();

            foreach (var network in networks)
            {
                serializableNetworks.Add(new SimNetworkSerializable(network));
                var components = SimNetworkSerializable.GetComponentInstances(network);
                foreach (var item in components)
                {
                    if (!componentsToExport.Contains(item))
                    {
                        componentsToExport.Add(item);
                    }
                }
            }
            foreach (var item in componentsToExport)
            {
                serializableComponents.Add(new SimComponentSerializable(item));
            }

            var serializableProjectData = new ProjectSerializable(projectData, serializableComponents, serializableNetworks);
            Serialize(writer, serializableProjectData);
        }

        private static void Serialize(TextWriter writer, ProjectSerializable serializableProjectData)
        {
            XmlWriterSettings settings = new XmlWriterSettings()
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                IndentChars = "\t",
                NewLineOnAttributes = false,
            };

            using (XmlWriter xw = XmlWriter.Create(writer, settings))
            {


                string simNS = "http://tuwien.ac.at/simultan";
                string xmiNS = "http://www.omg.org/XMI";

                xw.WriteStartDocument();

                //Header
                xw.WriteStartElement("", "Simultan", simNS);
                xw.WriteAttributeString("xmi", "version", xmiNS, "2.0");
                xw.WriteAttributeString("xmlns", "xmi", null, xmiNS);
                xw.WriteAttributeString("xmlns", simNS);

                Serialize(xw, serializableProjectData);

                xw.WriteEndElement();
                xw.WriteEndDocument();
            }
        }

        private static void Serialize(XmlWriter writer, object obj)
        {
            var attributes = GetAllCustomAttributes(obj);

            var jsonPolymorphicAttribute = attributes.FirstOrDefault(x => x is JsonPolymorphicAttribute) as JsonPolymorphicAttribute;
            if (jsonPolymorphicAttribute != null)
            {
                var derivedTypeAttribute = attributes.FirstOrDefault(x => x is JsonDerivedTypeAttribute ta && ta.DerivedType == obj.GetType())
                    as JsonDerivedTypeAttribute;
                if (derivedTypeAttribute != null)
                {
                    writer.WriteStartElement(jsonPolymorphicAttribute.TypeDiscriminatorPropertyName);
                    writer.WriteValue(derivedTypeAttribute.TypeDiscriminator);
                    writer.WriteFullEndElement();
                }
            }


            foreach (var property in obj.GetType().GetProperties())
            {
                var propValue = property.GetValue(obj);
                var propName = char.ToLower(property.Name[0]) + property.Name.Substring(1);

                Type enumerableInterface = null;

                if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() ==
                    typeof(IEnumerable<>))
                    enumerableInterface = property.PropertyType;
                else
                    enumerableInterface = property.PropertyType.GetInterfaces().FirstOrDefault(x =>
                                    x.IsGenericType &&
                                    x.GetGenericTypeDefinition() == typeof(IEnumerable<>));

                if (enumerableInterface != null && property.PropertyType != typeof(string))
                {
                    foreach (var item in (IEnumerable)propValue)
                    {
                        SerializeItem(writer, propName, item, enumerableInterface.GenericTypeArguments[0]);
                    }
                }
                else
                {
                    SerializeItem(writer, propName, propValue, property.PropertyType);
                }
            }
        }

        private static void SerializeItem(XmlWriter writer, string tagName, object item, Type itemType)
        {
            writer.WriteStartElement(tagName);

            if (primitiveTypes.TryGetValue(itemType, out var primitiveSerializer))
            {
                if (item != null)
                    writer.WriteValue(primitiveSerializer(item));
            }
            else if (item != null)
                Serialize(writer, item);

            writer.WriteFullEndElement();
        }

        private static List<object> GetAllCustomAttributes(object obj)
        {
            List<object> attributes = new List<object>();

            var type = obj.GetType();
            while (type != null)
            {
                attributes.AddRange(type.GetCustomAttributes(false));
                type = type.BaseType;
            }

            return attributes;
        }
    }
}
