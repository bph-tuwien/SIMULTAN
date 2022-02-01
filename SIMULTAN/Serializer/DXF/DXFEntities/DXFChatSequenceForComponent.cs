using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    /// <summary>
    /// wrapper class for a chat sequence of a single component
    /// </summary>
    internal class DXFChatSequenceForComponent : DXFEntityContainer
    {
        #region CLASS MEMBERS

        private List<SimChatItem> dxf_TopItems;
        private long dxf_owner_id;
        internal SimChat dxf_parsed;

        #endregion

        public DXFChatSequenceForComponent()
        {
            this.dxf_TopItems = new List<SimChatItem>();
            this.dxf_owner_id = -1L;
        }

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
                    if (chat_item != null && chat_item.dxf_parsed != null)
                    {
                        // take the parsed chat item                       
                        this.dxf_TopItems.Add(chat_item.dxf_parsed);
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
            if (this.dxf_TopItems.Count == 0 || this.Decoder.ProjectData.Components.Count == 0) return;

            long id = -1L;
            bool id_success = long.TryParse(this.ENT_KEY, out id);
            if (id_success)
                this.dxf_owner_id = id;
            this.dxf_parsed = new SimChat(this.dxf_TopItems);

            // attach to the correct component
            if (this.Decoder.AttachChatToComponent)
            {
                SimComponent owner = this.Decoder.ProjectData.IdGenerator
                    .GetById<SimComponent>(new SimId(this.dxf_owner_id));
                if (owner != null)
                {
                    owner.Conversation = this.dxf_parsed;
                }
            }
            else
            {
                this.Decoder.for_ChatMerging.Add(this.dxf_owner_id, this.dxf_parsed);
            }
        }

        #endregion
    }
}
