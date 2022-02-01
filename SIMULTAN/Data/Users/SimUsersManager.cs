using SIMULTAN;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Users
{
    /// <summary>
    /// Manager/Factor for user data.
    /// Handles current user, user creation and authentication
    /// </summary>
    public class SimUsersManager : INotifyPropertyChanged
    {
        private static readonly long PASSWORD_SALT_LENGTH = 16;

        /// <summary>
        /// Contains all users in the project
        /// </summary>
        public ObservableCollection<SimUser> Users { get; private set; }

        /// <summary>
        /// Stores the currently logged in user
        /// </summary>
        public SimUser CurrentUser
        {
            get { return currentUser; }
            set { currentUser = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentUser))); }
        }
        private SimUser currentUser;

        /// <summary>
        /// Stores the per-project encryption key. 
        /// Use this key for storing information that may only be accessed after beeing logged in
        /// </summary>
        public byte[] EncryptionKey
        {
            get { return encryptionKey; }
            set
            {
                encryptionKey = value;
                //Console.Write("Key: ");
                //foreach (var x in value)
                //	Console.Write("{0:x2}", x);
                //Console.WriteLine();
            }
        }
        private byte[] encryptionKey = null;

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Initializes a new instance of the UsersManager class
        /// </summary>
        public SimUsersManager()
        {
            Users = new ObservableCollection<SimUser>();
            CurrentUser = null;
        }

        /// <summary>
        /// Removes all users from the list
        /// </summary>
        public void Clear()
        {
            this.Users.Clear();
            this.CurrentUser = null;
        }

        /// <summary>
        /// Tests whether a user with given name/password combination exists
        /// </summary>
        /// <param name="name">The name of the user</param>
        /// <param name="password">The password of the user</param>
        /// <returns>The user when username/password match an existing user, Null when no such user exists. 
        /// When user is not Null, encryption key contains the encrypted project encryption key</returns>
        public (SimUser user, byte[] encryptionKey) Authenticate(string name, SecureString password)
        {
            IntPtr bstr = IntPtr.Zero;
            char[] passwdChar = new char[password.Length];
            byte[] passwdByte = null;
            SimUser user = null;
            byte[] encryptionKey = null;

            try
            {

                //Convert to byte[]
                bstr = Marshal.SecureStringToBSTR(password);
                Marshal.Copy(bstr, passwdChar, 0, password.Length);
                passwdByte = Encoding.UTF8.GetBytes(passwdChar);

                //Authenticate
                user = Authenticate(name, passwdByte);
                if (user != null)
                    encryptionKey = DecryptEncryptionKey(user.EncryptedEncryptionKey, passwdByte);
            }
            finally
            {
                //Override memory and free
                if (bstr != IntPtr.Zero)
                    Marshal.ZeroFreeBSTR(bstr);
                Array.Clear(passwdChar, 0, passwdChar.Length);
                if (passwdByte != null)
                    Array.Clear(passwdByte, 0, passwdByte.Length);
                password.Dispose();
            }

            return (user, encryptionKey);
        }
        /// <summary>
        /// Tests whether a user with given name/password combination exists
        /// </summary>
        /// <param name="name">The name of the user</param>
        /// <param name="password">The password of the user</param>
        /// <param name="isHashedPassword">When set to true, password is treated as a password hash. Otherwise, password gets hashed</param>
        /// <returns>The user when username/password match an existing user, Null when no such user exists</returns>
        public SimUser Authenticate(string name, byte[] password, bool isHashedPassword = false)
        {
            if (password == null)
                throw new ArgumentNullException(nameof(password));

            foreach (var user in Users.Where(x => x.Name == name))
            {
                if (Authenticate(user, password, isHashedPassword))
                    return user;
            }

            return null;
        }
        /// <summary>
        /// Tests whether a password matches an user
        /// </summary>
        /// <param name="user">The user</param>
        /// <param name="password">The password of the user</param>
        /// <returns>True when the password matches the user, otherwise False</returns>
        public bool Authenticate(SimUser user, SecureString password)
        {
            IntPtr bstr = IntPtr.Zero;
            char[] passwdChar = new char[password.Length];
            byte[] passwdByte = null;
            bool success = false;

            try
            {
                //Convert to byte[]
                bstr = Marshal.SecureStringToBSTR(password);
                Marshal.Copy(bstr, passwdChar, 0, password.Length);
                passwdByte = Encoding.UTF8.GetBytes(passwdChar);

                //Authenticate
                success = Authenticate(user, passwdByte);
            }
            finally
            {
                //Override memory and free
                if (bstr != IntPtr.Zero)
                    Marshal.ZeroFreeBSTR(bstr);
                Array.Clear(passwdChar, 0, passwdChar.Length);
                if (passwdByte != null)
                    Array.Clear(passwdByte, 0, passwdByte.Length);
                password.Dispose();
            }

            return success;
        }
        /// <summary>
        /// Tests whether a password matches an user
        /// </summary>
        /// <param name="user">The user</param>
        /// <param name="password">The password of the user</param>
        /// <param name="isHashedPassword">When true, password stores a password hash. When false, the hash is calculated from the password</param>
        /// <returns>True when the password matches the user, otherwise False</returns>
        public bool Authenticate(SimUser user, byte[] password, bool isHashedPassword = false)
        {
            byte[] salt = new byte[16];
            Array.Copy(user.PasswordHash, 0, salt, 0, 16);

            byte[] currentPasswordHash;
            if (isHashedPassword)
                currentPasswordHash = password;
            else
                currentPasswordHash = HashPassword(password, salt);

            for (int i = 16; i < 36; ++i)
                if (user.PasswordHash[i] != currentPasswordHash[i])
                    return false;

            return true;
        }

        /// <summary>
        /// Calculates the hash of a password
        /// </summary>
        /// <param name="password">The password</param>
        /// <returns>The hashed password</returns>
        public static byte[] HashPassword(SecureString password)
        {
            IntPtr bstr = IntPtr.Zero;
            char[] passwdChar = new char[password.Length];
            byte[] passwdByte = null;
            byte[] hash = null;

            try
            {
                //Convert to byte[]
                bstr = Marshal.SecureStringToBSTR(password);
                Marshal.Copy(bstr, passwdChar, 0, password.Length);
                passwdByte = Encoding.UTF8.GetBytes(passwdChar);

                //Authenticate
                hash = HashPassword(passwdByte);
            }
            finally
            {
                //Override memory and free
                if (bstr != IntPtr.Zero)
                    Marshal.ZeroFreeBSTR(bstr);
                Array.Clear(passwdChar, 0, passwdChar.Length);
                if (passwdByte != null)
                    Array.Clear(passwdByte, 0, passwdByte.Length);
                password.Dispose();
            }

            return hash;
        }
        /// <summary>
        /// Calculates the hash of a password
        /// </summary>
        /// <param name="password">The password</param>
        /// <returns>The hashed password</returns>
        public static byte[] HashPassword(byte[] password)
        {
            byte[] salt;
            new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);
            return HashPassword(password, salt);
        }
        private static byte[] HashPassword(byte[] password, byte[] salt)
        {
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
            byte[] hash = pbkdf2.GetBytes(20);

            byte[] hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);

            return hashBytes;
        }


        /// <summary>
        /// Encrypts an encryption key with a user password derived hash
        /// </summary>
        /// <param name="unencryptedKey">The unencryted key</param>
        /// <param name="password">Password of the user</param>
        /// <returns>
        /// The encrypted key. Contains: 
        /// Bytes                         | Usage
        /// PASSWORD_SALT_LENGTH          | IV of the password hashing method
        /// aesAlg.BlockSize / 8          | IV of the AES algorithm
        /// Everything else				  | Encrypted key
        /// </returns>
        public static byte[] EncryptEncryptionKey(byte[] unencryptedKey, SecureString password)
        {
            IntPtr bstr = IntPtr.Zero;
            char[] passwdChar = new char[password.Length];
            byte[] passwdByte = null;

            byte[] encryptedKey = null;

            try
            {
                //Convert to byte[]
                bstr = Marshal.SecureStringToBSTR(password);
                Marshal.Copy(bstr, passwdChar, 0, password.Length);
                passwdByte = Encoding.UTF8.GetBytes(passwdChar);

                encryptedKey = EncryptEncryptionKey(unencryptedKey, passwdByte);
            }
            finally
            {
                //Override memory and free
                if (bstr != IntPtr.Zero)
                    Marshal.ZeroFreeBSTR(bstr);
                Array.Clear(passwdChar, 0, passwdChar.Length);
                if (passwdByte != null)
                    Array.Clear(passwdByte, 0, passwdByte.Length);
            }

            return encryptedKey;
        }

        /// <summary>
        /// Encrypts an encryption key with a user password derived hash
        /// </summary>
        /// <param name="unencryptedKey">The unencryted key</param>
        /// <param name="password">Password of the user</param>
        /// <returns>
        /// The encrypted key. Contains: 
        /// Bytes                         | Usage
        /// PASSWORD_SALT_LENGTH          | IV of the password hashing method
        /// aesAlg.BlockSize / 8          | IV of the AES algorithm
        /// Everything else				  | Encrypted key
        /// </returns>
        public static byte[] EncryptEncryptionKey(byte[] unencryptedKey, byte[] password)
        {
            byte[] encryptedKey = null;

            //Generate salt for hashing
            var passwordSalt = new byte[PASSWORD_SALT_LENGTH];
            new RNGCryptoServiceProvider().GetBytes(passwordSalt);

            //Generate 32bit key from password
            byte[] key = KeyFromPassword(password, passwordSalt);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;

                // Create an encryptor
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                byte[] keywithoutIV = encryptor.TransformFinalBlock(unencryptedKey, 0, unencryptedKey.Length);
                encryptedKey = new byte[keywithoutIV.Length + aesAlg.IV.Length + passwordSalt.Length];
                Array.Copy(passwordSalt, encryptedKey, PASSWORD_SALT_LENGTH);
                Array.Copy(aesAlg.IV, 0, encryptedKey, PASSWORD_SALT_LENGTH, aesAlg.IV.Length);
                Array.Copy(keywithoutIV, 0, encryptedKey, PASSWORD_SALT_LENGTH + aesAlg.IV.Length, keywithoutIV.Length);
            }

            return encryptedKey;
        }

        /// <summary>
        /// Decrypts an encryption key with a user password derived hash
        /// </summary>
        /// <param name="encryptedKey">
        /// The encryted key. Has to contain:
        /// Bytes                         | Usage
        /// PASSWORD_SALT_LENGTH          | IV of the password hashing method
        /// aesAlg.BlockSize / 8          | IV of the AES algorithm
        /// Everything else				  | Encrypted key
        /// </param>
        /// <param name="password">Password of the user</param>
        /// <returns>The decrypted key</returns>
        public static byte[] DecryptEncryptionKey(byte[] encryptedKey, byte[] password)
        {
            byte[] decryptedKey = null;

            //Read password salt from key
            byte[] salt = new byte[PASSWORD_SALT_LENGTH];
            Array.Copy(encryptedKey, salt, salt.Length);

            //Generate 32bit key from password
            byte[] key = KeyFromPassword(password, salt);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;

                byte[] iv = new byte[aesAlg.BlockSize / 8];
                Array.Copy(encryptedKey, PASSWORD_SALT_LENGTH, iv, 0, iv.Length);
                aesAlg.IV = iv;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                byte[] data = new byte[encryptedKey.Length - iv.Length - PASSWORD_SALT_LENGTH];
                Array.Copy(encryptedKey, PASSWORD_SALT_LENGTH + iv.Length, data, 0, data.Length);
                decryptedKey = decryptor.TransformFinalBlock(data, 0, data.Length);
            }

            return decryptedKey;
        }

        private static byte[] KeyFromPassword(byte[] password, byte[] salt)
        {
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
            byte[] hash = pbkdf2.GetBytes(32);
            return hash;
        }
    }
}
