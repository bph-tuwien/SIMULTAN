using SIMULTAN.Data.Components;
using SIMULTAN.Data.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    internal class DXFUser : DXFEntity
    {
        private string dxf_Name;
        private Guid dxf_Id;
        private byte[] dxf_PasswordHash;
        private byte[] dxf_EncryptionKey;
        private SimUserRole dxf_Role;

        public DXFUser()
        {
            this.dxf_Name = "guest";
            this.dxf_Id = Guid.Empty;
            this.dxf_PasswordHash = SimUsersManager.HashPassword(Encoding.UTF8.GetBytes("guest"));
            this.dxf_Role = SimUserRole.GUEST;
            this.dxf_EncryptionKey = null;
        }

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)UserSaveCode.USER_ID:
                    this.dxf_Id = Guid.ParseExact(this.Decoder.FValue, "N");
                    break;
                case (int)UserSaveCode.USER_NAME:
                    this.dxf_Name = this.Decoder.FValue;
                    break;
                case (int)UserSaveCode.USER_PSW_HASH:
                    this.dxf_PasswordHash = Convert.FromBase64String(this.Decoder.FValue);
                    break;
                case (int)UserSaveCode.USER_ROLE:
                    this.dxf_Role = ComponentUtils.StringToComponentManagerType(this.Decoder.FValue);
                    break;
                case (int)UserSaveCode.USER_ENCRYPTION_KEY:
                    this.dxf_EncryptionKey = Convert.FromBase64String(this.Decoder.FValue);
                    break;
                default:
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    break;
            }
        }

        internal override void OnLoaded()
        {
            base.OnLoaded();

            if (this.Decoder is DXFDecoderUsers)
            {
                (this.Decoder as DXFDecoderUsers).ParsedUsers.Add(new SimUser(this.dxf_Id, this.dxf_Name, this.dxf_PasswordHash, this.dxf_EncryptionKey, this.dxf_Role));
            }
        }
    }
}
