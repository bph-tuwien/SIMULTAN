using SIMULTAN.Data.Components;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Users
{
    /// <summary>
    /// Stores information about a user
    /// </summary>
    public class SimUser : INotifyPropertyChanged
    {
        /// <summary>
        /// The unique GUID of this user
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// The name of the user. Currently used for both, login and display name
        /// </summary>
        public string Name
        {
            get { return name; }
            set
            {
                if (name != value)
                {
                    var old_value = name;
                    name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                }
            }
        }
        private string name;

        /// <summary>
        /// The hashed password. For security reasons the password is never stored in non-hashed form
        /// </summary>
        public byte[] PasswordHash
        {
            get { return passwordHash; }
            set
            {
                passwordHash = value;
            }
        }
        private byte[] passwordHash;

        /// <summary>
        /// The project encryption key, encrypted by the password of the user
        /// </summary>
        public byte[] EncryptedEncryptionKey { get { return encryptedEncryptionKey; } set { encryptedEncryptionKey = value; } }
        private byte[] encryptedEncryptionKey;

        /// <summary>
        /// Stores the role of this user. Only admin has special hardcoded functionality. 
        /// All other roles influence access rights to components
        /// </summary>
        public SimUserRole Role
        {
            get { return role; }
            set
            {
                if (role != value)
                {
                    var old_value = role;
                    role = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Role)));
                }
            }
        }
        private SimUserRole role;

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Initializes a new instance of the SimUser class
        /// </summary>
        /// <param name="id">Guid for this user (has to be unique)</param>
        /// <param name="name">The users name</param>
        /// <param name="passwordHash">Hash of the password. Can be created by using the UsersManager.HashPassword methods</param>
        /// <param name="encryptedEncryptionKey">The project encryption key, encrypted with the users password</param>
        /// <param name="role">Role of this user</param>
        public SimUser(Guid id, string name, byte[] passwordHash, byte[] encryptedEncryptionKey, SimUserRole role)
        {
            this.Id = id;
            this.name = name;
            this.PasswordHash = passwordHash;
            this.role = role;
            this.encryptedEncryptionKey = encryptedEncryptionKey;
        }

        /// <summary>
        /// Returns a default user with name "admin" and password "admin" (role = ADMINISTRATOR)
        /// </summary>
        [Obsolete("Should only be used in the old UI")]
        public static SimUser DefaultUser
        {
            get
            {
                //Generate encryption key for project
                byte[] key = new byte[32];
                new RNGCryptoServiceProvider().GetBytes(key);

                return new SimUser(Guid.NewGuid(),
                    "admin",
                    SimUsersManager.HashPassword(SimUser.DefaultUserPassword),
                    SimUsersManager.EncryptEncryptionKey(key, SimUser.DefaultUserPassword),
                    SimUserRole.ADMINISTRATOR);
            }
        }

        /// <summary>
        /// Returns the default password of the default user. Use this if you need to authenticate the default user right after creating them
        /// </summary>
        public static SecureString DefaultUserPassword
        {
            get
            {
                SecureString passwd = new SecureString();
                passwd.AppendChar('a');
                passwd.AppendChar('d');
                passwd.AppendChar('m');
                passwd.AppendChar('i');
                passwd.AppendChar('n');
                //passwd.MakeReadOnly();
                return passwd;
            }
        }

        /// <summary>
        /// Writes this user to a DXF format
        /// </summary>
        /// <param name="sw">The target stream</param>
        public void AddToExport(StreamWriter sw)
        {
            sw.WriteLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            sw.WriteLine(ParamStructTypes.USER);                                    // USER

            sw.WriteLine(((int)UserSaveCode.USER_ID).ToString());
            sw.WriteLine(this.Id.ToString("N"));

            sw.WriteLine(((int)UserSaveCode.USER_NAME).ToString());
            sw.WriteLine(this.Name);

            sw.WriteLine(((int)UserSaveCode.USER_PSW_HASH).ToString());
            sw.WriteLine(Convert.ToBase64String(this.PasswordHash));

            sw.WriteLine(((int)UserSaveCode.USER_ROLE).ToString());
            sw.WriteLine(ComponentUtils.ComponentManagerTypeToLetter(this.Role));

            sw.WriteLine(((int)UserSaveCode.USER_ENCRYPTION_KEY).ToString());
            sw.WriteLine(Convert.ToBase64String(this.EncryptedEncryptionKey));
        }
    }
}
