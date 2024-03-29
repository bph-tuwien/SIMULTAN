﻿using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Contains the conversation via ChatItems within a single component.
    /// </summary>
    public class SimChat : INotifyPropertyChanged
    {
        #region PROPERTIES: INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RegisterPropertyChanged(string _propName)
        {
            if (_propName == null)
                return;

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(_propName));
        }

        #endregion

        /// <summary>
        /// Contains the conversation sorted according to the timestamp.
        /// </summary>
        public SortedList<DateTime, SimChatItem> TopItems { get; private set; }

        /// <summary>
        /// Adds the chat item to the conversation. Removal of chat items is *not* possible.
        /// </summary>
        /// <param name="_item">the chat item to be added</param>
        public void AddItem(SimChatItem _item)
        {
            if (_item == null) return;

            this.TopItems.Add(_item.TimeStamp, _item);
            this.RegisterPropertyChanged(nameof(TopItems));
        }

        /// <summary>
        /// Initializes an empty conversation.
        /// </summary>
        public SimChat()
        {
            this.TopItems = new SortedList<DateTime, SimChatItem>();
        }

        /// <summary>
        /// Performs a deep copy of a conversation.
        /// </summary>
        /// <param name="_original">the original conversation</param>
        internal SimChat(SimChat _original)
        {
            if (_original == null)
            {
                this.TopItems = new SortedList<DateTime, SimChatItem>();
            }
            else
            {
                this.TopItems = new SortedList<DateTime, SimChatItem>();
                foreach (var entry in _original.TopItems)
                {
                    SimChatItem copy = new SimChatItem(entry.Value);
                    this.TopItems.Add(entry.Key, copy);
                }
            }
        }

        /// <summary>
        /// Parsing constructor.
        /// </summary>
        /// <param name="_parsed_items">the chat items parsed from a file</param>
        internal SimChat(IEnumerable<SimChatItem> _parsed_items)
        {
            this.TopItems = new SortedList<DateTime, SimChatItem>();
            foreach (SimChatItem item in _parsed_items)
            {
                this.TopItems.Add(item.TimeStamp, item);
            }
        }

        public override string ToString()
        {
            return "Chat: " + this.TopItems.Count.ToString();
        }
    }
}
