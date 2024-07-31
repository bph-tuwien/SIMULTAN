using SIMULTAN.Data.Users;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace SIMULTAN.Serializer.SIMUSER
{
    /// <summary>
    /// Class to read and write SimUser (.simuser) files.
    /// </summary>
    internal static class SimUserDxfIO
    {
        #region Syntax

        private static DXFEntityParserElement<SimUser> userEntitiyElement =
            new DXFEntityParserElement<SimUser>(ParamStructTypes.USER, ParseSimUser,
                new DXFEntryParserElement[]
                {
                    new DXFSingleEntryParserElement<Guid>(UserSaveCode.USER_ID),
                    new DXFSingleEntryParserElement<String>(UserSaveCode.USER_NAME),
                    new DXFBase64SingleEntryParserElement(UserSaveCode.USER_PSW_HASH),
                    new DXFSingleEntryParserElement<SimUserRole>(UserSaveCode.USER_ROLE),
                    new DXFBase64SingleEntryParserElement(UserSaveCode.USER_ENCRYPTION_KEY),
                });


        private static DXFSectionParserElement<SimUser> usersSection =
            new DXFSectionParserElement<SimUser>(ParamStructTypes.USER_SECTION, new DXFEntityParserElement<SimUser>[]
            {
                userEntitiyElement,
            });

        #endregion

        #region Read/Write

        /// <summary>
        /// Reads encrypted users from a file.
        /// </summary>
        /// <param name="file">The file to read from</param>
        /// <param name="key">The encryption key</param>
        /// <param name="info">The parser info</param>
        /// <returns>A list of loaded SimUsers</returns>
        /// <exception cref="ArgumentNullException">If an argument is null</exception>
        internal static List<SimUser> Read(FileInfo file, byte[] key, DXFParserInfo info)
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
        /// Reads encrypted users from a stream.
        /// May throw an exception if there is no VersionSection and parseVersionSection is set to true.
        /// If that happens, reopen the stream and call with parseVersionSEction set to false.
        /// </summary>
        /// <param name="stream">The stream to read from</param>
        /// <param name="key">The encryption key</param>
        /// <param name="info">The parser info</param>
        /// <param name="parseVersionSection">Set to false if you do not want to parse the version section. (File versions &lt; 12)</param>
        /// <returns>A list of loaded SimUsers</returns>
        /// <exception cref="ArgumentNullException">If an argument is null</exception>
        internal static List<SimUser> Read(Stream stream, byte[] key, DXFParserInfo info, bool parseVersionSection = true)
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
        /// Reads encrypted users from a stream.
        /// May throw an exception if there is no VersionSection and parseVersionSection is set to true.
        /// If that happens, reopen the stream and call with parseVersionSEction set to false.
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="info">The parser info</param>
        /// <param name="parseVersionSection">Set to false if you do not want to parse the version section. (File versions &lt; 12)</param>
        /// <returns>A list of loaded SimUsers</returns>
        /// <exception cref="ArgumentNullException">If an argument is null</exception>
        internal static List<SimUser> Read(DXFStreamReader reader, DXFParserInfo info, bool parseVersionSection = true)
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

            return usersSection.Parse(reader, info);
        }

        /// <summary>
        /// Writes a list of users to an encrypted file.
        /// </summary>
        /// <param name="users">The users</param>
        /// <param name="file">The file</param>
        /// <param name="key">The encryption key</param>
        /// <exception cref="ArgumentNullException">If an argument is null</exception>
        internal static void Write(IEnumerable<SimUser> users, FileInfo file, byte[] key)
        {
            if (key == null || key.Length <= 0)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (users == null)
                throw new ArgumentNullException(nameof(users));
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            using (FileStream fs = new FileStream(file.FullName, FileMode.Create))
            {
                Write(users, fs, key);
            }
        }

        /// <summary>
        /// Writes a list of users to an encrypted stream.
        /// </summary>
        /// <param name="users">The users</param>
        /// <param name="stream">The stream to write to</param>
        /// <param name="key">The encryption key</param>
        /// <exception cref="ArgumentNullException">If an argument is null</exception>
        internal static void Write(IEnumerable<SimUser> users, Stream stream, byte[] key)
        {
            if (key == null || key.Length <= 0)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (users == null)
                throw new ArgumentNullException(nameof(users));
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
                        Write(users, writer);
                    }
                }
            }
        }

        /// <summary>
        /// Writes a list of users to a writer.
        /// </summary>
        /// <param name="users">The users</param>
        /// <param name="writer">The writer</param>
        /// <exception cref="ArgumentNullException">If an argument is null</exception>
        internal static void Write(IEnumerable<SimUser> users, DXFStreamWriter writer)
        {
            if (users == null)
                throw new ArgumentNullException(nameof(users));
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            writer.WriteVersionSection();

            writer.StartSection(ParamStructTypes.USER_SECTION, users.Count());

            foreach (var user in users)
            {
                WriteUser(user, writer);
            }

            writer.EndSection();

            writer.WriteEOF();

        }

        internal static void WriteUser(SimUser user, DXFStreamWriter writer)
        {
            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.USER);
            writer.Write(UserSaveCode.USER_ID, user.Id);
            writer.Write(UserSaveCode.USER_NAME, user.Name);
            writer.WriteBase64(UserSaveCode.USER_PSW_HASH, user.PasswordHash);
            writer.Write(UserSaveCode.USER_ROLE, user.Role);
            writer.WriteBase64(UserSaveCode.USER_ENCRYPTION_KEY, user.EncryptedEncryptionKey);
        }
        #endregion

        #region Parsing

        private static SimUser ParseSimUser(DXFParserResultSet result, DXFParserInfo info)
        {
            var id = result.Get<Guid>(UserSaveCode.USER_ID, Guid.Empty);
            var name = result.Get<string>(UserSaveCode.USER_NAME, string.Empty);
            var pwHash = result.Get<byte[]>(UserSaveCode.USER_PSW_HASH, null);
            var role = result.Get<SimUserRole>(UserSaveCode.USER_ROLE, SimUserRole.GUEST);
            var encKey = result.Get<byte[]>(UserSaveCode.USER_ENCRYPTION_KEY, null);

            return new SimUser(id, name, pwHash, encKey, role);
        }

        #endregion

    }
}
