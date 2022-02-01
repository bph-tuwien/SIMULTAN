using SIMULTAN.Data.Components;
using SIMULTAN.Data.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    /// <summary>
    /// wrapper class for chat items
    /// </summary>
    internal class DXFChatItem : DXFEntityContainer
    {
        #region CLASS MEMBERS

        private SimChatItemType dxf_Type;
        private SimUserRole dxf_Author;
        private string dxf_VotingRegistration_Address;
        private string dxf_VotingRegistration_Password;

        private string dxf_GitCommitKey;
        private DateTime dxf_TimeStamp;

        private string dxf_Message;
        private SimChatItemState dxf_State;

        private List<SimUserRole> dxf_ExpectsReacionsFrom;
        private int dxf_nr_ExpectsReacionsFrom;

        private List<SimChatItem> dxf_children;
        private int dxf_nr_children;

        internal SimChatItem dxf_parsed;
        #endregion

        public DXFChatItem()
        {
            this.dxf_Type = SimChatItemType.QUESTION;
            this.dxf_Author = SimUserRole.ADMINISTRATOR;
            this.dxf_VotingRegistration_Address = string.Empty;
            this.dxf_VotingRegistration_Password = string.Empty;

            this.dxf_GitCommitKey = string.Empty;
            this.dxf_TimeStamp = DateTime.Now;

            this.dxf_Message = string.Empty;
            this.dxf_State = SimChatItemState.OPEN;

            this.dxf_ExpectsReacionsFrom = new List<SimUserRole>();
            this.dxf_nr_ExpectsReacionsFrom = 0;

            this.dxf_children = new List<SimChatItem>();
            this.dxf_nr_children = 0;
        }

        #region OVERRIDES: Read Property

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ChatItemSaveCode.TYPE:
                    SimChatItemType t;
                    bool success_t = Enum.TryParse<SimChatItemType>(this.Decoder.FValue, out t);
                    if (success_t)
                        this.dxf_Type = t;
                    break;
                case (int)ChatItemSaveCode.AUTHOR:
                    this.dxf_Author = ComponentUtils.StringToComponentManagerType(this.Decoder.FValue);
                    break;
                case (int)ChatItemSaveCode.VR_ADDRESS:
                    this.dxf_VotingRegistration_Address = this.Decoder.FValue;
                    break;
                case (int)ChatItemSaveCode.VR_PASSWORD:
                    this.dxf_VotingRegistration_Password = this.Decoder.FValue;
                    break;
                case (int)ChatItemSaveCode.GIT_COMMIT:
                    this.dxf_GitCommitKey = this.Decoder.FValue;
                    break;
                case (int)ChatItemSaveCode.TIMESTAMP:
                    DateTime dt_tmp = DateTime.Now;
                    bool dt_p_success = DateTime.TryParse(this.Decoder.FValue, ParamStructTypes.DT_FORMATTER, System.Globalization.DateTimeStyles.None, out dt_tmp);
                    if (dt_p_success)
                        this.dxf_TimeStamp = dt_tmp;
                    break;
                case (int)ChatItemSaveCode.MESSAGE:
                    this.dxf_Message = this.Decoder.FValue;
                    break;
                case (int)ChatItemSaveCode.STATE:
                    SimChatItemState s;
                    bool success_s = Enum.TryParse<SimChatItemState>(this.Decoder.FValue, out s);
                    if (success_s)
                        this.dxf_State = s;
                    break;
                case (int)ChatItemSaveCode.EXPECTED_REACTIONS_FROM:
                    this.dxf_nr_ExpectsReacionsFrom = this.Decoder.IntValue();
                    break;
                case (int)ChatItemSaveCode.CHILDREN:
                    this.dxf_nr_children = this.Decoder.IntValue();
                    break;
                case (int)ParamStructCommonSaveCode.X_VALUE:
                    if (this.dxf_nr_ExpectsReacionsFrom > this.dxf_ExpectsReacionsFrom.Count)
                    {
                        SimUserRole user = ComponentUtils.StringToComponentManagerType(this.Decoder.FValue);
                        this.dxf_ExpectsReacionsFrom.Add(user);
                    }
                    break;
                default:
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    break;
            }
        }

        #endregion

        #region OVERRIDES: Adding Entities

        internal override bool AddEntity(DXFEntity _e)
        {
            if (_e == null) return false;
            bool add_successful = false;

            DXFComponentSubContainer container = _e as DXFComponentSubContainer;
            if (container != null)
            {
                add_successful = true;
                foreach (DXFEntity sE in container.EC_Entities)
                {
                    DXFChatItem chat_item = sE as DXFChatItem;
                    if (chat_item != null && chat_item.dxf_parsed != null &&
                        this.dxf_nr_children > this.dxf_children.Count)
                    {
                        // take the parsed chat item                       
                        this.dxf_children.Add(chat_item.dxf_parsed);
                        add_successful &= true;
                    }
                }
            }

            return add_successful;
        }

        #endregion

        #region OVERRIDES: Post-Processing

        internal override void OnLoaded()
        {
            this.dxf_parsed = new SimChatItem(this.dxf_Type, this.dxf_Author, this.dxf_VotingRegistration_Address, this.dxf_VotingRegistration_Password,
                                           this.dxf_GitCommitKey, this.dxf_TimeStamp, this.dxf_Message, this.dxf_State,
                                           this.dxf_ExpectsReacionsFrom, this.dxf_children);
        }

        #endregion
    }
}
