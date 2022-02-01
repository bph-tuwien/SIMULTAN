using SIMULTAN;
using SIMULTAN.Data.Users;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Utils.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Projects.Serializers
{
    /// <summary>
    /// Provides methods for loading/saving user data to a DXF based and AES encrypted file
    /// </summary>
    public static class UserFileIO
    {
        /// <summary>
        /// Stores the user data into a file. Creates a new file when a non-existing path is given.
        /// Does not create parent folder
        /// </summary>
        /// <param name="users">A list of users that should be stored</param>
        /// <param name="file">Path to the file</param>
        /// <param name="key">The key for the AES encryption. May use ProjectIO.ENCR_KEY</param>
        public static void Save(IEnumerable<SimUser> users, FileInfo file, byte[] key)
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
                    //Write iv
                    fs.Write(aesAlg.IV, 0, aesAlg.IV.Length);

                    using (CryptoStream csEncrypt = new CryptoStream(fs, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(csEncrypt, Encoding.UTF8))
                        {
                            SaveDXF(sw, users);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Loads and decrypts a user file
        /// </summary>
        /// <param name="file">The file</param>
        /// <param name="key">The key for the AES encryption. May use ProjectIO.ENCR_KEY</param>
        /// <returns>A list of users that were stored in the file</returns>
        public static List<SimUser> Load(FileInfo file, byte[] key)
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
                        DXFDecoderUsers decoder = new DXFDecoderUsers();
                        decoder.LoadFromFile(csEncrypt);
                        return decoder.ParsedUsers;
                    }
                }
            }
        }


        private static void SaveDXF(StreamWriter sw, IEnumerable<SimUser> users)
        {
            sw.WriteLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sw.WriteLine(ParamStructTypes.SECTION_START);
            sw.WriteLine(((int)ParamStructCommonSaveCode.ENTITY_NAME).ToString());
            sw.WriteLine(ParamStructTypes.USER_SECTION);

            foreach (SimUser u in users)
            {
                u.AddToExport(sw);
            }

            sw.WriteLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sw.WriteLine(ParamStructTypes.SECTION_END);

            sw.WriteLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sw.WriteLine(ParamStructTypes.EOF);
        }
    }
}
