using SIMULTAN.Data.Assets;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.SIMLINKS
{
    /// <summary>
    /// Provides methods for serializing <see cref="MultiLink"/> instances into a SIMLINKS file
    /// </summary>
    public static class SimLinksDxfIO
    {
        #region Syntax

        /// <summary>
        /// Syntax for a MultiLink
        /// </summary>
        internal static DXFEntityParserElementBase<MultiLink> LinkEntityElement =
            new DXFEntityParserElement<MultiLink>(ParamStructTypes.MULTI_LINK, ParseMultiLink,
                new DXFEntryParserElement[]
                {
                    new DXFStructArrayEntryParserElement<(int, string)>(ParamStructCommonSaveCode.NUMBER_OF, ParseRepresentation,
                        new DXFEntryParserElement[]
                        {
                            new DXFSingleEntryParserElement<int>(ParamStructCommonSaveCode.COORDS_X),
                            new DXFSingleEntryParserElement<string>(ParamStructCommonSaveCode.COORDS_Y),
                        })
                });

        /// <summary>
        /// Syntax for a multilinks section
        /// </summary>
        internal static DXFSectionParserElement<MultiLink> MultiLinkSectionEntityElement =
            new DXFSectionParserElement<MultiLink>(ParamStructTypes.MULTI_LINK_SECTION,
                new DXFEntityParserElementBase<MultiLink>[]
                {
                    LinkEntityElement
                });

        #endregion

        internal static void Write(IEnumerable<MultiLink> links, FileInfo file, byte[] key)
        {
            if (key == null || key.Length <= 0)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (links == null)
                throw new ArgumentNullException(nameof(links));
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            using (FileStream fs = new FileStream(file.FullName, FileMode.Create))
            {
                Write(links, fs, key);
            }
        }

        internal static void Write(IEnumerable<MultiLink> links, Stream stream, byte[] key)
        {
            if (key == null || key.Length <= 0)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (links == null)
                throw new ArgumentNullException(nameof(links));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            // Create an AES object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                //Write iv
                stream.Write(aesAlg.IV, 0, aesAlg.IV.Length);

                using (CryptoStream csEncrypt = new CryptoStream(stream, encryptor, CryptoStreamMode.Write))
                {
                    using (DXFStreamWriter writer = new DXFStreamWriter(csEncrypt))
                    {
                        Write(links, writer);
                    }
                }
            }
        }

        internal static void Write(IEnumerable<MultiLink> links, DXFStreamWriter writer)
        {
            writer.WriteVersionSection();

            WriteMultiLinkSection(links, writer);

            writer.WriteEOF();
        }

        /// <summary>
        /// Reads encrypted MultiLinks from a file.
        /// </summary>
        /// <param name="file">The file to read from</param>
        /// <param name="key">The encryption key</param>
        /// <param name="info">The parser info</param>
        /// <returns>A list of loaded SimUsers</returns>
        /// <exception cref="ArgumentNullException">If an argument is null</exception>
        internal static List<MultiLink> Read(FileInfo file, byte[] key, DXFParserInfo info)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (key == null || key.Length <= 0)
                throw new ArgumentNullException(nameof(key));
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            try
            {
                using (FileStream fs = new FileStream(file.FullName, FileMode.Open))
                {
                    return Read(fs, key, info);
                }
            }
            // Can happen when there is no version section, so try again without parsing it
            catch (Exception)
            {
                using (FileStream fs = new FileStream(file.FullName, FileMode.Open))
                {
                    info.FileVersion = 11;
                    return Read(fs, key, info, false);
                }
            }
        }

        /// <summary>
        /// Reads encrypted MultiLinks from a stream.
        /// May throw an exception if there is no VersionSection and parseVersionSection is set to true.
        /// If that happens, reopen the stream and call with parseVersionSEction set to false.
        /// </summary>
        /// <param name="stream">The stream to read from</param>
        /// <param name="key">The encryption key</param>
        /// <param name="info">The parser info</param>
        /// <param name="parseVersionSection">Set to false if you do not want to parse the version section. (File versions &lt; 12)</param>
        /// <returns>A list of loaded SimUsers</returns>
        /// <exception cref="ArgumentNullException">If an argument is null</exception>
        internal static List<MultiLink> Read(Stream stream, byte[] key, DXFParserInfo info, bool parseVersionSection = true)
        {
            if (key == null || key.Length <= 0)
                throw new ArgumentNullException(nameof(key));
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            // Create an AES object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;

                byte[] iv = new byte[aesAlg.BlockSize / 8];
                stream.Read(iv, 0, aesAlg.BlockSize / 8);
                aesAlg.IV = iv;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (CryptoStream csEncrypt = new CryptoStream(stream, decryptor, CryptoStreamMode.Read))
                {
                    using (var reader = new DXFStreamReader(csEncrypt))
                    {
                        return Read(reader, info, parseVersionSection);
                    }
                }
            }
        }

        /// <summary>
        /// Reads encrypted MultiLink from a stream.
        /// May throw an exception if there is no VersionSection and parseVersionSection is set to true.
        /// If that happens, reopen the stream and call with parseVersionSEction set to false.
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="info">The parser info</param>
        /// <param name="parseVersionSection">Set to false if you do not want to parse the version section. (File versions &lt; 12)</param>
        /// <returns>A list of loaded SimUsers</returns>
        /// <exception cref="ArgumentNullException">If an argument is null</exception>
        internal static List<MultiLink> Read(DXFStreamReader reader, DXFParserInfo info, bool parseVersionSection = true)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            //Version section
            if (parseVersionSection)
            {
                info = CommonParserElements.VersionSectionElement.Parse(reader, info).First();
            }

            return MultiLinkSectionEntityElement.Parse(reader, info);
        }


        #region Section

        internal static void WriteMultiLinkSection(IEnumerable<MultiLink> links, DXFStreamWriter writer)
        {
            writer.StartSection(ParamStructTypes.MULTI_LINK_SECTION);

            foreach (var link in links)
                WriteMultiLink(link, writer);

            writer.EndSection();
        }

        #endregion

        #region Link

        internal static void WriteMultiLink(MultiLink link, DXFStreamWriter writer)
        {
            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.MULTI_LINK);

            writer.WriteArray(ParamStructCommonSaveCode.NUMBER_OF, link.Representations,
                (item, iwriter) =>
                {
                    iwriter.Write(ParamStructCommonSaveCode.COORDS_X, item.Key);
                    iwriter.Write(ParamStructCommonSaveCode.COORDS_Y, item.Value);
                });
        }

        private static MultiLink ParseMultiLink(DXFParserResultSet data, DXFParserInfo info)
        {
            var representations = data.Get<(int key, string value)[]>(ParamStructCommonSaveCode.NUMBER_OF, new (int, string)[0]);

            try
            {
                var repDict = representations.ToDictionary(x => x.key, x => x.value);

                return new MultiLink(repDict);
            }
            catch (Exception e)
            {
                info.Log(string.Format("Failed to load MultiLink\nException: {2}\nStackTrace:\n{3}",
                    e.Message, e.StackTrace
                    ));
                return null;
            }
        }

        private static (int, string) ParseRepresentation(DXFParserResultSet data, DXFParserInfo info)
        {
            int key = data.Get<int>(ParamStructCommonSaveCode.COORDS_X, 0);
            string value = data.Get<string>(ParamStructCommonSaveCode.COORDS_Y, string.Empty);

            return (key, value);
        }

        #endregion
    }
}
