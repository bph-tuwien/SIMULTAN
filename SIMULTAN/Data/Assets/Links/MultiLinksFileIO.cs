using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Assets
{
    /// <summary>
    /// Provides methods for loading / saving multi-links in a DXF-based format in a AES encrypted file.
    /// </summary>
    public static class MultiLinksFileIO
    {
        /// <summary>
        /// Stores the multi-link data into a file. Creates a new file when a non-existing path is given.
		/// Does not create parent folder.
        /// </summary>
        /// <param name="links">a collection of multi-links to be stored</param>
        /// <param name="file">the target file</param>
        /// <param name="key">the key for the AES encryption</param>
        public static void Save(IEnumerable<MultiLink> links, FileInfo file, byte[] key)
        {
            if (key == null || key.Length <= 0)
                throw new ArgumentNullException(nameof(key));

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (FileStream fs = new FileStream(file.FullName, FileMode.Create))
                {
                    //Write iv at the beginning of the file
                    fs.Write(aesAlg.IV, 0, aesAlg.IV.Length);

                    using (CryptoStream csEncrypt = new CryptoStream(fs, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(csEncrypt, Encoding.UTF8))
                        {
                            SaveDXF(sw, links);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Loads and decrypts a multi-link file.
        /// </summary>
        /// <param name="file">the source file</param>
        /// <param name="key">the key for the AES encryption</param>
        /// <returns>a list of the multi-links stored in the file</returns>
        public static List<MultiLink> Load(FileInfo file, byte[] key)
        {
            if (key == null || key.Length <= 0)
                throw new ArgumentNullException(nameof(key));

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;

                using (FileStream fs = new FileStream(file.FullName, FileMode.Open))
                {
                    byte[] iv = new byte[aesAlg.BlockSize / 8];
                    fs.Read(iv, 0, aesAlg.BlockSize / 8);
                    aesAlg.IV = iv;

                    // Create a decryptor to perform the stream transform.
                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    using (CryptoStream csEncrypt = new CryptoStream(fs, decryptor, CryptoStreamMode.Read))
                    {
                        DXFDecoderMultiLinks decoder = new DXFDecoderMultiLinks();
                        decoder.LoadFromFile(csEncrypt);
                        return decoder.ParsedLinks;
                    }
                }
            }
        }

        private static void SaveDXF(StreamWriter sw, IEnumerable<MultiLink> links)
        {
            sw.WriteLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sw.WriteLine(ParamStructTypes.SECTION_START);
            sw.WriteLine(((int)ParamStructCommonSaveCode.ENTITY_NAME).ToString());
            sw.WriteLine(ParamStructTypes.MULTI_LINK_SECTION);

            foreach (MultiLink ml in links)
            {
                ml.AddToExport(sw);
            }

            sw.WriteLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sw.WriteLine(ParamStructTypes.SECTION_END);

            sw.WriteLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sw.WriteLine(ParamStructTypes.EOF);
        }

    }
}
