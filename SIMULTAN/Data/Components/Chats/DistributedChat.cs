using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Records the result of comparing two chat items.
    /// </summary>
    [Flags]
    public enum ChatItemComparisonResult
    {
        /// <summary>
        /// The items are not even comparable - e.g., they have a different type or author.
        /// </summary>
        DIFFERENT = 0,
        /// <summary>
        /// The items are at the same depth in their respective chat tree,
        /// they are of the same type, have the same author, timestamp and git commit key.
        /// </summary>
        COMPARABLE = 1,
        /// <summary>
        /// Both items are either closed or open.
        /// </summary>
        SAME_STATE = 2,
        /// <summary>
        /// Both items carry the same message.
        /// </summary>
        SAME_MESSAGE = 4,
        /// <summary>
        /// The intended correspondents of both chat items are the same.
        /// </summary>
        SAME_RECEPIENTS = 8,
        /// <summary>
        /// Both chat items have the same number of children.
        /// </summary>
        SAME_NR_OF_CHILDREN = 16,
        /// <summary>
        /// The chat items have at least one child each whose ChatItemComparisonResult has all flags set.
        /// </summary>
        SOME_CHILDREN_SAME = 32,
        /// <summary>
        /// The chat items have identical children (apart from the reference).
        /// </summary>
        ALL_CHILDREN_SAME = 64
    }

    /// <summary>
    /// Aligns two chat items during comparison.
    /// </summary>
    internal class CorrespondingChatItemPair
    {
        /// <summary>
        /// 1st chat item. Can be Null.
        /// </summary>
        public SimChatItem Item1 { get; private set; }
        /// <summary>
        /// 2nd chat item. Can be Null.
        /// </summary>
        public SimChatItem Item2 { get; private set; }
        /// <summary>
        /// The common time stamp.
        /// </summary>
        public DateTime TimeStamp { get; private set; }
        /// <summary>
        /// The result of comparing the two items.
        /// </summary>
        public ChatItemComparisonResult ComparisonResult { get; private set; }
        /// <summary>
        /// True, if neither chat item is Null; False otherwise.
        /// </summary>
        public bool Full { get { return this.Item1 != null && this.Item2 != null; } }
        /// <summary>
        /// Alignment of the children of the chat items held by this instance.
        /// </summary>
        public List<CorrespondingChatItemPair> Children { get; private set; }

        /// <summary>
        /// Initializes an instance of the CorrespondingChatItemPair class.
        /// </summary>
        /// <param name="_item_1">the 1st chat item</param>
        /// <param name="_item_2">the 2nd chat item</param>
        public CorrespondingChatItemPair(SimChatItem _item_1, SimChatItem _item_2)
        {
            this.Item1 = _item_1;
            this.Item2 = _item_2;
            this.TimeStamp = (_item_1 != null) ? _item_1.TimeStamp : _item_2.TimeStamp;
            this.ComparisonResult = DistributedChat.CompareSimple(_item_1, _item_2);

            this.Children = new List<CorrespondingChatItemPair>();
        }
    }

    /// <summary>
    /// Container for static methods for merging two different versions of a chat containing chat item trees.
    /// </summary>
    internal static class DistributedChat
    {
        /// <summary>
        /// Merges two different version of the same conversation (or chat). Used when loading
        /// a project saved in a distributed way for versioning via GIT. As a conversation can only
        /// add chat items, but not delete or edit them, the result is the union of the two.
        /// </summary>
        /// <param name="_version_1">first version of the conversation</param>
        /// <param name="_version_2">second version of the conversation</param>
        /// <returns>the merged conversation</returns>
        internal static SimChat MergeConversationVersions(SimChat _version_1, SimChat _version_2)
        {
            if (_version_1 == null && _version_2 == null)
            {
                return null;
            }
            else if (_version_1 == null)
            {
                return new SimChat(_version_2);
            }
            else if (_version_2 == null)
            {
                return new SimChat(_version_1);
            }
            else
            {
                SimChat merged = new SimChat();
                List<SimChatItem> combined = new List<SimChatItem>();
                SortedList<DateTime, Tuple<SimChatItem, SimChatItem>> aligned = DistributedChat.Align(_version_1.TopItems, _version_2.TopItems);
                foreach (var entry in aligned)
                {
                    SimChatItem i1 = entry.Value.Item1;
                    SimChatItem i2 = entry.Value.Item2;
                    if (i1 == null && i2 != null)
                    {
                        combined.Add(i2);
                    }
                    else if (i1 != null && i2 == null)
                    {
                        combined.Add(i1);
                    }
                    else if (i1 != null && i2 != null)
                    {
                        ChatItemComparisonResult comp_12 = DistributedChat.Compare(i1, i2);
                        if (DistributedChat.IsFullMatch(comp_12))
                        {
                            combined.Add(i1);
                        }
                        else if (DistributedChat.IsFullMatchExceptState(comp_12))
                        {
                            if (i1.State == SimChatItemState.CLOSED)
                                combined.Add(i1);
                            else
                                combined.Add(i2);
                        }
                        else if (DistributedChat.IsFullMatchExceptStateOrChildren(comp_12))
                        {
                            // combine
                            CorrespondingChatItemPair pair = DistributedChat.CompareAndAlign(i1, i2);
                            SimChatItem pair_combined = SimChatItem.Combine(pair);
                            combined.Add(pair_combined);
                        }
                        else
                        {
                            // add both as they are distinct
                            combined.Add(i1);
                            combined.Add(i2);
                        }
                    }
                }
                merged = new SimChat(combined);
                return merged;
            }

        }

        /// <summary>
        /// Shallow comparison of two chat items - w/o considering their children,
        /// just their number.
        /// </summary>
        /// <param name="_item_1">st chat item</param>
        /// <param name="_item_2">2nd chat item</param>
        /// <returns>the result of the comparison</returns>
        internal static ChatItemComparisonResult CompareSimple(SimChatItem _item_1, SimChatItem _item_2)
        {
            // test comparability
            ChatItemComparisonResult result = ChatItemComparisonResult.DIFFERENT;
            if (AreComparable(_item_1, _item_2))
                result |= ChatItemComparisonResult.COMPARABLE;
            if (!result.HasFlag(ChatItemComparisonResult.COMPARABLE)) return result;

            // test STATE
            if (_item_1.State == _item_2.State)
                result |= ChatItemComparisonResult.SAME_STATE;

            // test MESSAGE
            if (_item_1.Message == _item_2.Message)
                result |= ChatItemComparisonResult.SAME_MESSAGE;

            // test RECEPIENTS
            bool same_recepients = (_item_1.ExpectsReacionsFrom.Count == _item_2.ExpectsReacionsFrom.Count);
            same_recepients &= (_item_1.ExpectsReacionsFrom.TrueForAll(x => _item_2.ExpectsReacionsFrom.Contains(x)));
            if (same_recepients)
                result |= ChatItemComparisonResult.SAME_RECEPIENTS;

            // test NR of CHILDREN
            if (_item_1.Children.Count == _item_2.Children.Count)
                result |= ChatItemComparisonResult.SAME_NR_OF_CHILDREN;

            return result;
        }

        /// <summary>
        /// Deep comparison of two chat items, including their children.
        /// </summary>
        /// <param name="_item_1">1st chat item</param>
        /// <param name="_item_2">2nd chat item</param>
        /// <returns></returns>
        internal static ChatItemComparisonResult Compare(SimChatItem _item_1, SimChatItem _item_2)
        {
            // test comparability
            ChatItemComparisonResult result = ChatItemComparisonResult.DIFFERENT;
            if (AreComparable(_item_1, _item_2))
                result |= ChatItemComparisonResult.COMPARABLE;
            if (!result.HasFlag(ChatItemComparisonResult.COMPARABLE)) return result;

            // test STATE
            if (_item_1.State == _item_2.State)
                result |= ChatItemComparisonResult.SAME_STATE;

            // test MESSAGE
            if (_item_1.Message == _item_2.Message)
                result |= ChatItemComparisonResult.SAME_MESSAGE;

            // test RECEPIENTS
            bool same_recepients = (_item_1.ExpectsReacionsFrom.Count == _item_2.ExpectsReacionsFrom.Count);
            same_recepients &= (_item_1.ExpectsReacionsFrom.TrueForAll(x => _item_2.ExpectsReacionsFrom.Contains(x)));
            if (same_recepients)
                result |= ChatItemComparisonResult.SAME_RECEPIENTS;

            // test NR of CHILDREN
            if (_item_1.Children.Count == _item_2.Children.Count)
                result |= ChatItemComparisonResult.SAME_NR_OF_CHILDREN;

            // test CHILDREN
            if (_item_1.Children.Count == 0)
            {
                result |= ChatItemComparisonResult.SOME_CHILDREN_SAME;
                result |= ChatItemComparisonResult.ALL_CHILDREN_SAME;
                return result;
            }

            foreach (SimChatItem child_1 in _item_1.Children)
            {
                foreach (SimChatItem child_2 in _item_2.Children)
                {
                    ChatItemComparisonResult child_comp_result = DistributedChat.Compare(child_1, child_2);
                    if (child_comp_result.HasFlag(ChatItemComparisonResult.ALL_CHILDREN_SAME))
                        result |= ChatItemComparisonResult.SOME_CHILDREN_SAME;
                    else
                        result &= ~ChatItemComparisonResult.ALL_CHILDREN_SAME;
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if the two chat items are at the same depth in their respective chat tree,
        /// if they are of the same type, have the same author, timestamp and git commit key.
        /// If either chat item is Null, the result is False.
        /// </summary>
        /// <param name="_item_1">1st item</param>
        /// <param name="_item_2">2nd item</param>
        /// <returns>true, if comparable, false otherwise</returns>
        internal static bool AreComparable(SimChatItem _item_1, SimChatItem _item_2)
        {
            bool are_comparable = (_item_1 != null && _item_2 != null);
            if (!are_comparable) return false;

            are_comparable &= (_item_1.GetDepth() == _item_2.GetDepth());
            are_comparable &= (_item_1.Type == _item_2.Type);
            are_comparable &= (_item_1.Author == _item_2.Author);
            are_comparable &= (_item_1.TimeStamp == _item_2.TimeStamp);
            are_comparable &= (_item_1.GitCommitKey == _item_2.GitCommitKey);

            return are_comparable;
        }

        /// <summary>
        /// Checks if the comparison result for two chat items has all flags set.
        /// </summary>
        /// <param name="_result">the result to be examined</param>
        /// <returns>true, if each flag is set, false otherwise</returns>
        private static bool IsFullMatch(ChatItemComparisonResult _result)
        {
            bool is_match = _result.HasFlag(ChatItemComparisonResult.COMPARABLE);
            is_match &= _result.HasFlag(ChatItemComparisonResult.SAME_STATE);
            is_match &= _result.HasFlag(ChatItemComparisonResult.SAME_MESSAGE);
            is_match &= _result.HasFlag(ChatItemComparisonResult.SAME_RECEPIENTS);
            is_match &= _result.HasFlag(ChatItemComparisonResult.SAME_NR_OF_CHILDREN);
            is_match &= _result.HasFlag(ChatItemComparisonResult.SOME_CHILDREN_SAME);
            is_match &= _result.HasFlag(ChatItemComparisonResult.ALL_CHILDREN_SAME);
            return is_match;
        }

        /// <summary>
        /// Checks if the comparison result for the two chat items shows
        /// a match except for the state.
        /// </summary>
        /// <param name="_result">the result to be examined</param>
        /// <returns>true, if each flag is set except fot the state; false otherwise</returns>
        private static bool IsFullMatchExceptState(ChatItemComparisonResult _result)
        {
            bool is_match = _result.HasFlag(ChatItemComparisonResult.COMPARABLE);
            is_match &= !_result.HasFlag(ChatItemComparisonResult.SAME_STATE);
            is_match &= _result.HasFlag(ChatItemComparisonResult.SAME_MESSAGE);
            is_match &= _result.HasFlag(ChatItemComparisonResult.SAME_RECEPIENTS);
            is_match &= _result.HasFlag(ChatItemComparisonResult.SAME_NR_OF_CHILDREN);
            is_match &= _result.HasFlag(ChatItemComparisonResult.SOME_CHILDREN_SAME);
            is_match &= _result.HasFlag(ChatItemComparisonResult.ALL_CHILDREN_SAME);
            return is_match;
        }

        /// <summary>
        /// Checks if the comparison result for the two chat items shows
        /// a match except either the state or the children.
        /// </summary>
        /// <param name="_result">the result to be examined</param>
        /// <returns>true, if the conditions are met; flase otherwise</returns>
        private static bool IsFullMatchExceptStateOrChildren(ChatItemComparisonResult _result)
        {
            bool is_match = _result.HasFlag(ChatItemComparisonResult.COMPARABLE);
            is_match &= _result.HasFlag(ChatItemComparisonResult.SAME_MESSAGE);
            is_match &= _result.HasFlag(ChatItemComparisonResult.SAME_RECEPIENTS);

            is_match &= (!_result.HasFlag(ChatItemComparisonResult.SAME_STATE) ||
                         !_result.HasFlag(ChatItemComparisonResult.SAME_NR_OF_CHILDREN) ||
                         !_result.HasFlag(ChatItemComparisonResult.SOME_CHILDREN_SAME) ||
                         !_result.HasFlag(ChatItemComparisonResult.ALL_CHILDREN_SAME));
            return is_match;
        }

        /// <summary>
        /// Aligns the roots of chat item trees according to timestamp.
        /// </summary>
        /// <param name="_list_1">1st list of chat items sorted by time stamp</param>
        /// <param name="_list_2">2nd list of chat items sorted by time stamp</param>
        /// <returns>the alignment of the two lists</returns>
        internal static SortedList<DateTime, Tuple<SimChatItem, SimChatItem>> Align(SortedList<DateTime, SimChatItem> _list_1, SortedList<DateTime, SimChatItem> _list_2)
        {
            List<DateTime> all_timestamps = new List<DateTime>();
            all_timestamps.AddRange(_list_1.Select(x => x.Key).Distinct());
            all_timestamps.AddRange(_list_2.Select(x => x.Key).Distinct());
            all_timestamps = all_timestamps.Distinct().ToList();
            all_timestamps.Sort();

            SortedList<DateTime, Tuple<SimChatItem, SimChatItem>> aligned = new SortedList<DateTime, Tuple<SimChatItem, SimChatItem>>();
            foreach (DateTime ts in all_timestamps)
            {
                SimChatItem in_1 = _list_1.Where(x => x.Key == ts).Select(x => x.Value).FirstOrDefault();
                SimChatItem in_2 = _list_2.Where(x => x.Key == ts).Select(x => x.Value).FirstOrDefault();
                if (in_1 != null || in_2 != null)
                    aligned.Add(ts, new Tuple<SimChatItem, SimChatItem>(in_1, in_2));
            }

            return aligned;
        }

        /// <summary>
        /// Compares and aligns two chat trees. If the roots are not comparable, it returns Null.
        /// </summary>
        /// <param name="_item_1">1st chat item</param>
        /// <param name="_item_2">2nd chat item</param>
        /// <returns>a tree of CorrespojndingChatItemPair instances, or Null</returns>
        internal static CorrespondingChatItemPair CompareAndAlign(SimChatItem _item_1, SimChatItem _item_2)
        {
            if (!DistributedChat.AreComparable(_item_1, _item_2)) return null;

            CorrespondingChatItemPair aligned = new CorrespondingChatItemPair(_item_1, _item_2);
            if (_item_1.Children.Count == 0 && _item_2.Children.Count == 0) return aligned;

            // handle the children
            List<bool> children_1_found_corresponding = Enumerable.Repeat(false, _item_1.Children.Count).ToList();
            List<bool> children_2_found_corresponding = Enumerable.Repeat(false, _item_2.Children.Count).ToList();
            for (int i = 0; i < _item_1.Children.Count; i++)
            {
                if (children_1_found_corresponding[i]) continue;
                SimChatItem child_1 = _item_1.Children[i];
                for (int j = 0; j < _item_2.Children.Count; j++)
                {
                    if (children_2_found_corresponding[j]) continue;
                    SimChatItem child_2 = _item_2.Children[j];
                    CorrespondingChatItemPair aligned_children = DistributedChat.CompareAndAlign(child_1, child_2);
                    if (aligned_children != null)
                    {
                        aligned.Children.Add(aligned_children);
                        children_1_found_corresponding[i] = true;
                        children_2_found_corresponding[j] = true;
                    }
                }
            }
            for (int i = 0; i < _item_1.Children.Count; i++)
            {
                if (!children_1_found_corresponding[i])
                {
                    CorrespondingChatItemPair single_1 = DistributedChat.CreateHierarchy(_item_1.Children[i], null);
                    aligned.Children.Add(single_1);
                }
            }
            for (int j = 0; j < _item_2.Children.Count; j++)
            {
                if (!children_2_found_corresponding[j])
                {
                    CorrespondingChatItemPair single_2 = DistributedChat.CreateHierarchy(null, _item_2.Children[j]);
                    aligned.Children.Add(single_2);
                }
            }

            return aligned;
        }

        /// <summary>
        /// Creates a CorrespondingChatItemPair tree when one of the chat itmes is Null.
        /// If both are Null, returns Null.
        /// </summary>
        /// <param name="_item_1">1st chat itme, could be Null</param>
        /// <param name="_item_2">2nd chat item, could be Null</param>
        /// <returns>a tree of not full CorrespojndingChatItemPair instances, or Null</returns>
        internal static CorrespondingChatItemPair CreateHierarchy(SimChatItem _item_1, SimChatItem _item_2)
        {
            if (_item_1 == null && _item_2 == null)
                return null;
            else if (_item_1 != null && _item_2 != null)
                return CompareAndAlign(_item_1, _item_2);
            else if (_item_1 != null && _item_2 == null)
            {
                CorrespondingChatItemPair single_1 = new CorrespondingChatItemPair(_item_1, null);
                foreach (SimChatItem child in _item_1.Children)
                {
                    CorrespondingChatItemPair single_1_child = DistributedChat.CreateHierarchy(child, null);
                    single_1.Children.Add(single_1_child);
                }
                return single_1;
            }
            else
            {
                CorrespondingChatItemPair single_2 = new CorrespondingChatItemPair(null, _item_2);
                foreach (SimChatItem child in _item_2.Children)
                {
                    CorrespondingChatItemPair single_2_child = DistributedChat.CreateHierarchy(null, child);
                    single_2.Children.Add(single_2_child);
                }
                return single_2;
            }
        }
    }
}
