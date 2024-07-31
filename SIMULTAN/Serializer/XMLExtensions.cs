using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SIMULTAN.Serializer
{
    /// <summary>
    /// Contains extensions methods for XML classes
    /// </summary>
    public static class XMLIOExtensions
    {
        /// <summary>
        /// Writes a Xml element containing "<key>value</key>"
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sw"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void WriteKeyValue<T>(this XmlWriter sw, string key, T value)
        {
            sw.WriteStartElement(key);
            sw.WriteValue(value);
            sw.WriteEndElement();
        }

        /// <summary>
        /// Loads an xml node and converts the content
        /// </summary>
        /// <param name="node">The XMLElement onto which the xpath is applied</param>
        /// <param name="xpath">The xpath expression</param>
        /// <param name="converter">Convert to convert the content string</param>
        /// <returns>True when the xpath has selected an element, False otherwise</returns>
        public static bool LoadInnerText(this XmlElement node, String xpath, Action<String> converter)
        {
            var s = LoadString(node, xpath);
            if (s != null)
            {
                try
                {
                    converter(s);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Failed to convert setting {0}, Reason: {1}", xpath, e.Message);
                }
                return true;
            }
            return false;
        }
        private static String LoadString(XmlElement node, String xpath)
        {
            var element = node.SelectSingleNode(xpath) as XmlElement;

            if (element == null)
                return null;
            else
                return element.InnerText;
        }
    }
}
