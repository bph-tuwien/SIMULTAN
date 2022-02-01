using SIMULTAN.Data.Components;
using SIMULTAN.Data.Users;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    #region ENUMS

    // Warning: String names are serialized
    public enum SimChatItemType
    {
        /// <summary>
        /// A question that can be answered by the users determined by the author.
        /// </summary>
        QUESTION,
        /// <summary>
        /// An answer to a question. Can be delivered by users determined by the author of the question.
        /// </summary>
        ANSWER,
        /// <summary>
        /// Positive reaction to the answer to a question. The author of the question and the reacting user are the same.
        /// This type of ChatItem cannot have any more reactions.
        /// </summary>
        ANSWER_ACCEPT,
        /// <summary>
        /// Negative reaction to the answer to a question. The author of the question and the reacting user are the same.
        /// This type of ChatItem cannot have any more reactions.
        /// </summary>
        ANSWER_REJECT,
        /// <summary>
        /// A voting session that can be recorded on a block-chain. The voting session closes automatically
        /// as soon as all voters have placed a vote.
        /// </summary>
        VOTING_SESSION,
        /// <summary>
        /// A positive vote as a child to voting session.
        /// </summary>
        VOTE_ACCEPT,
        /// <summary>
        /// A negative vote as a child to voting session.
        /// </summary>
        VOTE_REJECT
    }

    //Warning: String names are serialized
    public enum SimChatItemState
    {
        OPEN,
        CLOSED
    }
    #endregion

    /// <summary>
    /// Represents a single unit of communication within one component. Is part of a Chat.
    /// </summary>
    public class SimChatItem : INotifyPropertyChanged
    {
        #region STATIC: Dispatchers

        /// <summary>
        /// Dispatcher method for creating a new top-level ChatItem of type QUESTION or VOTING_SESSION.
        /// </summary>
        /// <param name="_author">the calling user</param>
        /// <param name="_commit_key">the GIT commit key of the currently opened project</param>
        /// <param name="_message">the textual content</param>
        /// <param name="_is_question">true = poses a question, false = set up a voting session</param>
        /// <param name="_expects_reactions_from">the users that can / are expected to react</param>
        /// <returns>the newly created chat item, never Null</returns>
        public static SimChatItem CreateNew(SimUserRole _author, string _commit_key, string _message, bool _is_question,
                                    List<SimUserRole> _expects_reactions_from)
        {
            if (_is_question)
                return OpenQuestion(_author, _commit_key, _message, _expects_reactions_from);
            else
                return OpenVotingSession(_author, _commit_key, _message, _expects_reactions_from);
        }

        /// <summary>
        /// Checks if the given user '_author' can react to the given chat item.
        /// </summary>
        /// <param name="_item">the given chat item</param>
        /// <param name="_author">the calling user attempting a reaction to the chat item</param>
        /// <returns>true, if the user can react to the chat itme, false otherwise</returns>
        public static bool CanReactTo(SimChatItem _item, SimUserRole _author)
        {
            if (_item == null) return false;
            switch (_item.Type)
            {
                case SimChatItemType.QUESTION:
                    return SimChatItem.CanAnswerQuestion(_item, _author);
                case SimChatItemType.ANSWER:
                    return SimChatItem.CanReactToAnswer(_item, _author);
                case SimChatItemType.ANSWER_ACCEPT:
                case SimChatItemType.ANSWER_REJECT:
                    return false;
                case SimChatItemType.VOTING_SESSION:
                    return SimChatItem.CanVoteInSession(_item, _author);
                case SimChatItemType.VOTE_ACCEPT:
                case SimChatItemType.VOTE_REJECT:
                    return false;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Creates a chat item of the appropriate type as a response to the given chat item by the calling user.
        /// </summary>
        /// <param name="_item">the given chat item</param>
        /// <param name="_author">the calling user reacting to the given item</param>
        /// <param name="_commit_key">the GIT commit key of the currently open project by the calling user</param>
        /// <param name="_message">the reaction as text</param>
        /// <param name="_positive">true = positive reaction, false = negative, no value = neutral</param>
        /// <returns>the created reaction chat item</returns>
        public static SimChatItem ReactTo(SimChatItem _item, SimUserRole _author, string _commit_key, string _message, bool? _positive)
        {
            if (_item == null) return null;
            switch (_item.Type)
            {
                case SimChatItemType.QUESTION:
                    return SimChatItem.AnswerQuestion(_item, _author, _commit_key, _message);
                case SimChatItemType.ANSWER:
                    return SimChatItem.ReactToAnswer(_item, _author, _commit_key, _message, _positive);
                case SimChatItemType.ANSWER_ACCEPT:
                case SimChatItemType.ANSWER_REJECT:
                    return null;
                case SimChatItemType.VOTING_SESSION:
                    if (_positive.HasValue)
                        return SimChatItem.PlaceVote(_item, _author, _commit_key, _message, _positive.Value);
                    else
                        return null;
                case SimChatItemType.VOTE_ACCEPT:
                case SimChatItemType.VOTE_REJECT:
                    return null;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Checks if the given chat item can be closed for discussion.
        /// </summary>
        /// <param name="_item">the chat item</param>
        /// <param name="_author">the calling user attempting to close the chat item</param>
        /// <returns>true if closing is possible, false otherwise</returns>
        public static bool CanClose(SimChatItem _item, SimUserRole _author)
        {
            if (_item == null) return false;
            if (_item.Type == SimChatItemType.QUESTION)
                return SimChatItem.CanCloseQuestion(_item, _author);
            else if (_item.Type == SimChatItemType.ANSWER)
                return (_item.state != SimChatItemState.CLOSED && _item.ExpectsReacionsFrom.Contains(_author));
            else
                return false;
        }

        #endregion

        #region STATIC: Question

        /// <summary>
        /// Creates a ChatItem that poses a question that can be answered by other users. It can be closed
        /// by the creator at any time.
        /// </summary>
        /// <param name="_author">the user asking the question</param>
        /// <param name="_commit_key">the git commit key (hash) to which the question refers</param>
        /// <param name="_question_text">the question itself</param>
        /// <param name="_expects_reactions_from">the users that can answer the question</param>
        /// <returns>a ChatItem of type QUESTION or Null</returns>
        internal static SimChatItem OpenQuestion(SimUserRole _author, string _commit_key,
                                              string _question_text, List<SimUserRole> _expects_reactions_from)
        {
            return new SimChatItem(SimChatItemType.QUESTION, _author, _commit_key, _question_text, _expects_reactions_from);
        }

        /// <summary>
        /// Checks if the user can answer the question posed by the given ChatItem.
        /// Answering is not possible if the ChatItem is not a question, if the question is closed,
        /// or if the user is not among the users expected to answer.
        /// </summary>
        /// <param name="_question">the ChatItem posing the question</param>
        /// <param name="_answering_author">the user requesting to answer the question</param>
        /// <returns>true, if the given user can answer the question, false otherwise</returns>
        internal static bool CanAnswerQuestion(SimChatItem _question, SimUserRole _answering_author)
        {
            if (_question == null) return false;
            if (_question.Type != SimChatItemType.QUESTION || _question.state == SimChatItemState.CLOSED) return false;
            if (!_question.ExpectsReacionsFrom.Contains(_answering_author)) return false;

            return true;
        }

        /// <summary>
        /// Creates a ChatItem of type ANSWER as a child to the given ChatItem of type QUESTION, if 
        /// the aswering user is allowed to answer this question.
        /// </summary>
        /// <param name="_question">the question that is being answered</param>
        /// <param name="_answering_author">the user providing the answer</param>
        /// <param name="_commit_key">the GIT commit key of the currently opened project by the author</param>
        /// <param name="_answer_text">the actual answer</param>
        /// <returns>the created ChatItem of type ANSWER or Null</returns>
        internal static SimChatItem AnswerQuestion(SimChatItem _question, SimUserRole _answering_author, string _commit_key, string _answer_text)
        {
            if (!SimChatItem.CanAnswerQuestion(_question, _answering_author)) return null;
            var answer = new SimChatItem(SimChatItemType.ANSWER, _answering_author, _question.GitCommitKey, _answer_text, new List<SimUserRole> { _question.Author }, _question);
            return answer;
        }

        /// <summary>
        /// Checks if the reacting user can actually place a reaction to the given answer.
        /// This should generally be the author of the question that received the given answer.
        /// </summary>
        /// <param name="_answer">the answer to which the user reacts</param>
        /// <param name="_reacting_author">the reacting user</param>
        /// <returns>true if the user can react to the answer, false otherwise</returns>
        internal static bool CanReactToAnswer(SimChatItem _answer, SimUserRole _reacting_author)
        {
            if (_answer == null) return false;
            if (_answer.Type != SimChatItemType.ANSWER || _answer.state == SimChatItemState.CLOSED) return false;
            if (!_answer.ExpectsReacionsFrom.Contains(_reacting_author)) return false;

            return true;
        }

        /// <summary>
        /// Creates a ChatItem of type ANSWER, ANSWER_ACCEPT or ANSWER_REJECT as a reply / reaction
        /// to the given answer. Generally, the user expected to react is the one that posed the question
        /// which received the answer.
        /// </summary>
        /// <param name="_answer">the answer to which the user reacts</param>
        /// <param name="_reacting_author">the reacting user</param>
        /// <param name="_commit_key">the GIT commit key of the project currently opened by the author</param>
        /// <param name="_reaction_text">the actual reaction</param>
        /// <param name="_positive">Null = answer, true = positive reaction, false = negative reaction</param>
        /// <returns></returns>
        internal static SimChatItem ReactToAnswer(SimChatItem _answer, SimUserRole _reacting_author, string _commit_key, string _reaction_text, bool? _positive)
        {
            if (!SimChatItem.CanReactToAnswer(_answer, _reacting_author)) return null;

            SimChatItemType t = SimChatItemType.ANSWER;
            List<SimUserRole> expected_reactions = new List<SimUserRole> { _answer.Author };
            if (_positive.HasValue)
            {
                expected_reactions = new List<SimUserRole>();
                if (_positive.Value)
                    t = SimChatItemType.ANSWER_ACCEPT;
                else
                    t = SimChatItemType.ANSWER_REJECT;
            }

            var reaction = new SimChatItem(t, _reacting_author, _answer.GitCommitKey, _reaction_text, expected_reactions, _answer);
            return reaction;
        }

        /// <summary>
        /// Checks if the question can be declared as answered or closed by the given user. 
        /// A question can be closed only once. A closing is propagated to all child ChatItems.
        /// </summary>
        /// <param name="_question">the question ChatItem</param>
        /// <param name="_closing_user">the user attempting to close the question</param>
        /// <returns>true, if the user can close the question, false otherwise</returns>
        public static bool CanCloseQuestion(SimChatItem _question, SimUserRole _closing_user)
        {
            if (_question == null) return false;
            if (_question.Type != SimChatItemType.QUESTION || _question.state == SimChatItemState.CLOSED) return false;
            if (_question.Author != _closing_user) return false;

            return true;
        }

        /// <summary>
        /// Declares a question ChatItem as answered or closed.
        /// A question can be closed only once. A closing is propagated to all child ChatItems.
        /// </summary>
        /// <param name="_question">the question ChatItem that is being closed</param>
        /// <param name="_closing_user">the user closing the question</param>
        /// <param name="_commit_key">the GIT commit key of the project currently opened by the user</param>
        public static void CloseQuestion(SimChatItem _question, SimUserRole _closing_user, string _commit_key)
        {
            if (SimChatItem.CanCloseQuestion(_question, _closing_user))
                _question.State = SimChatItemState.CLOSED;
        }

        #endregion

        #region STATIC: Voting (attachable to a block-chain)

        /// <summary>
        /// Creates a ChatItem of type VOTING_SESSION that opens up voting on the given subject.
        /// The users required to vote can do so only once. This ChatItem is automatically declared 
        /// closed when each voter has placed a vote in a child ChatItem.
        /// </summary>
        /// <param name="_author">the user creating and managing the voting session</param>
        /// <param name="_commit_key">the Git commit key the vote is about</param>
        /// <param name="_voting_subject">the subject of the voting session</param>
        /// <param name="_requires_votes_from">the users with voting rights</param>
        /// <returns>the ChatItem of type VOTING SESSION</returns>
        internal static SimChatItem OpenVotingSession(SimUserRole _author, string _commit_key,
                                                 string _voting_subject, List<SimUserRole> _requires_votes_from)
        {
            return new SimChatItem(SimChatItemType.VOTING_SESSION, _author, _commit_key, _voting_subject, _requires_votes_from);
        }

        /// <summary>
        /// Checks if the given user can vote in the voting session.
        /// </summary>
        /// <param name="_voting_session">the voting session in which to vote</param>
        /// <param name="_voter">the user attempting a vote</param>
        /// <returns>true if the given voter can vote, false otherwise</returns>
        internal static bool CanVoteInSession(SimChatItem _voting_session, SimUserRole _voter)
        {
            if (_voting_session == null) return false;
            if (_voting_session.Type != SimChatItemType.VOTING_SESSION || _voting_session.state == SimChatItemState.CLOSED) return false;
            if (!_voting_session.ExpectsReacionsFrom.Contains(_voter)) return false;

            // prevent double votes
            List<SimUserRole> voted_users = _voting_session.children.Select(x => x.Author).ToList();
            if (voted_users.Contains(_voter)) return false;

            return true;
        }

        /// <summary>
        /// Creates a closed ChatItem of type VOTE_ACCEPT or VOTE_REJECT. No reaction it is possible.
        /// After all voters declared by the voting session have placed a vote, the voting session is automatically closed.
        /// </summary>
        /// <param name="_voting_session">the voting session in which to vote</param>
        /// <param name="_voter">the voter</param>
        /// <param name="_commit_key">the GIT commit key of the project currently opened by the voter</param>
        /// <param name="_voting_motivation">additional information to justify the voter'S choice</param>
        /// <param name="_accept">true = positive vote, false = negative vote</param>
        /// <returns>a ChatItem of type VOTE_ACCEPT or VOTE_REJECT, or Null</returns>
        internal static SimChatItem PlaceVote(SimChatItem _voting_session, SimUserRole _voter, string _commit_key, string _voting_motivation, bool _accept)
        {
            if (!SimChatItem.CanVoteInSession(_voting_session, _voter)) return null;

            SimChatItemType t = SimChatItemType.VOTE_ACCEPT;
            if (!_accept)
                t = SimChatItemType.VOTE_REJECT;

            SimChatItem vote_record = new SimChatItem(t, _voter, _voting_session.GitCommitKey, _voting_motivation, new List<SimUserRole>(), _voting_session);

            // check if the voting session should be closed
            List<SimUserRole> voted_users = _voting_session.children.Select(x => x.Author).ToList();
            bool close = _voting_session.ExpectsReacionsFrom.Select(x => voted_users.Contains(x)).Aggregate((x, y) => x && y);
            if (close)
                _voting_session.State = SimChatItemState.CLOSED;

            return vote_record;
        }

        #endregion

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

        #region PROPERTIES: General

        /// <summary>
        /// The type of the ChatItem: QUESTION, ANSWER, ANSWER_ACCEPT, ANSWER_REJECT, VOTING_SESSION, VOTE_ACCEPT, VOTE_REJECT.
        /// </summary>
        public SimChatItemType Type { get; private set; }
        /// <summary>
        /// The user that creates and owns the chat item. Generally, it is the user with writing access 
        /// to the component to which this chat item is attached.
        /// </summary>
        public SimUserRole Author { get; private set; }
        /// <summary>
        /// Block-Chain specific: account public key for setting up of voting sessions and for voting.
        /// </summary>
        public string VotingRegistration_Address { get; private set; }
        /// <summary>
        /// Block-Chain specific: account private key for setting up of voting sessions and for voting.
        /// </summary>
        private SecureString VotingRegistration_Password;

        /// <summary>
        /// The Git commit key of the currently loaded version of the component record.
        /// </summary>
        public string GitCommitKey { get; private set; }
        /// <summary>
        /// The creation date and time of this chat item.
        /// </summary>
        public DateTime TimeStamp { get; private set; }
        /// <summary>
        /// The textual message of the chat item.
        /// </summary>
        public string Message { get; private set; }


        private SimChatItemState state;
        /// <summary>
        /// The state of the chat item. It can be either OPEN o CLOSED.
        /// The CLOSED state is propagated down to all children.
        /// </summary>
        public SimChatItemState State
        {
            get { return this.state; }
            private set
            {
                if (this.state != value)
                {
                    var old_value = this.state;
                    this.state = value;
                    if (this.state == SimChatItemState.CLOSED)
                    {
                        if (this.children != null)
                        {
                            foreach (SimChatItem child in this.children)
                            {
                                child.State = SimChatItemState.CLOSED;
                            }
                        }
                    }
                    this.RegisterPropertyChanged(nameof(State));
                }
            }
        }

        #endregion

        #region PROPERTIES: Reaction

        /// <summary>
        /// The list of users that can react to this chat item.
        /// </summary>
        public List<SimUserRole> ExpectsReacionsFrom { get; private set; }
        /// <summary>
        /// Derived: a string representation of the users that are expected to or can react to the chat item.
        /// </summary>
        public string ExpectsReactionsFromString
        {
            get
            {
                if (this.ExpectsReacionsFrom.Count == 0)
                    return string.Empty;
                return this.ExpectsReacionsFrom.Select(x => ComponentUtils.ComponentManagerTypeToLetter(x)).Aggregate((x, y) => x + y);
            }
        }

        private SimChatItem reaction_to;
        /// <summary>
        /// The parent chat item, to which this one is a reaction.
        /// </summary>
        public SimChatItem ReactionTo
        {
            get { return this.reaction_to; }
            private set
            {
                if (this.reaction_to != null)
                {
                    this.reaction_to.children.Remove(this);
                    this.reaction_to.RegisterPropertyChanged(nameof(Children));
                }
                this.reaction_to = value;
                if (this.reaction_to != null)
                {
                    this.reaction_to.children.Add(this);
                    this.reaction_to.RegisterPropertyChanged(nameof(Children));
                }
            }
        }

        private List<SimChatItem> children;
        /// <summary>
        /// Derived: the child chat items that are reactions to this one.
        /// </summary>
        public List<SimChatItem> Children
        {
            get
            {
                if (this.children != null)
                    return new List<SimChatItem>(this.children);
                else
                    return new List<SimChatItem>();
            }
        }

        #endregion

        #region .CTOR

        private SimChatItem()
        {
            this.Type = SimChatItemType.QUESTION;
            this.Author = SimUserRole.ADMINISTRATOR;
            this.VotingRegistration_Address = ChatUtils.GetFixedAddressFor(this.Author);
            this.VotingRegistration_Password = ChatUtils.GetFixedPasswordFor(this.Author);

            this.GitCommitKey = string.Empty;
            this.TimeStamp = DateTime.Now;
            this.Message = string.Empty;

            this.state = SimChatItemState.OPEN;

            this.ExpectsReacionsFrom = new List<SimUserRole> { SimUserRole.ADMINISTRATOR };
            this.ReactionTo = null;
            this.children = new List<SimChatItem>();
        }

        private SimChatItem(SimChatItemType _type, SimUserRole _author, string _commit_key, string _message,
                        List<SimUserRole> _expects_reactions_from, SimChatItem _reaction_to = null)
        {
            this.Type = _type;
            this.Author = _author;
            this.VotingRegistration_Address = ChatUtils.GetFixedAddressFor(_author);
            this.VotingRegistration_Password = ChatUtils.GetFixedPasswordFor(_author);

            this.GitCommitKey = _commit_key;
            this.TimeStamp = DateTime.Now;
            this.Message = _message;

            this.state = SimChatItemState.OPEN;

            this.ExpectsReacionsFrom = new List<SimUserRole>(_expects_reactions_from);
            if (this.ExpectsReacionsFrom.Count == 0)
                this.state = SimChatItemState.CLOSED;
            this.ReactionTo = _reaction_to;
            this.children = new List<SimChatItem>();
        }

        #endregion

        #region COPY .CTOR

        internal SimChatItem(SimChatItem _original)
        {
            this.Type = _original.Type;
            this.Author = _original.Author;
            this.VotingRegistration_Address = ChatUtils.GetFixedAddressFor(_original.Author);
            this.VotingRegistration_Password = ChatUtils.GetFixedPasswordFor(_original.Author);

            this.GitCommitKey = _original.GitCommitKey;
            this.TimeStamp = _original.TimeStamp;

            this.Message = _original.Message;
            this.state = _original.state;

            this.ExpectsReacionsFrom = new List<SimUserRole>(_original.ExpectsReacionsFrom);

            this.children = new List<SimChatItem>();
            foreach (SimChatItem child in _original.children)
            {
                SimChatItem child_copy = new SimChatItem(child);
                this.children.Add(child_copy);
                child_copy.ReactionTo = this;
            }
        }

        #endregion

        #region PARSING .CTOR

        internal SimChatItem(SimChatItemType _type, SimUserRole _author, string _voting_registration_address, string _voting_registration_password,
                           string _commit_key, DateTime _timestamp, string _message, SimChatItemState _state,
                           List<SimUserRole> _expects_reactions_from, IEnumerable<SimChatItem> _children)
        {
            this.Type = _type;
            this.Author = _author;
            this.VotingRegistration_Address = _voting_registration_address;
            this.VotingRegistration_Password = ChatUtils.StringToSecureString(_voting_registration_password);

            this.GitCommitKey = _commit_key;
            this.TimeStamp = _timestamp;

            this.Message = _message;
            this.state = _state;

            this.ExpectsReacionsFrom = _expects_reactions_from;
            this.children = new List<SimChatItem>();
            foreach (SimChatItem child in _children)
            {
                child.ReactionTo = this; // the setter fills in the this.children list
            }
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            string offset = (this.GetDepth() == 0) ? string.Empty : Enumerable.Repeat("\t", this.GetDepth()).Aggregate((x, y) => x + y);
            string rep = offset + this.TimeStamp.ToString() + " ";
            rep += ComponentUtils.ComponentManagerTypeToLetter(this.Author) + ": ";
            rep += this.Type.ToString() + " ";
            rep += this.Message + " ";
            rep += (this.state == SimChatItemState.CLOSED) ? "x" : "o";

            rep += Environment.NewLine;
            foreach (SimChatItem child in this.Children)
            {
                rep += child.ToString();
            }

            return rep;
        }

        public void AddToExport(ref StringBuilder _sb, string _key = null)
        {
            if (_sb == null) return;

            // head
            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.CHAT_ITEM);                               // CHAT_ITEM

            if (!string.IsNullOrEmpty(_key))
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_KEY).ToString());
                _sb.AppendLine(_key);
            }

            _sb.AppendLine(((int)ParamStructCommonSaveCode.CLASS_NAME).ToString());
            _sb.AppendLine(this.GetType().ToString());

            // body
            _sb.AppendLine(((int)ChatItemSaveCode.TYPE).ToString());
            _sb.AppendLine(this.Type.ToString());

            _sb.AppendLine(((int)ChatItemSaveCode.AUTHOR).ToString());
            _sb.AppendLine(ComponentUtils.ComponentManagerTypeToLetter(this.Author));

            _sb.AppendLine(((int)ChatItemSaveCode.VR_ADDRESS).ToString());
            _sb.AppendLine(this.VotingRegistration_Address);

            _sb.AppendLine(((int)ChatItemSaveCode.VR_PASSWORD).ToString());
            _sb.AppendLine(ChatUtils.SecureStringToString(this.VotingRegistration_Password));

            _sb.AppendLine(((int)ChatItemSaveCode.GIT_COMMIT).ToString());
            _sb.AppendLine(this.GitCommitKey);

            _sb.AppendLine(((int)ChatItemSaveCode.TIMESTAMP).ToString());
            _sb.AppendLine(this.TimeStamp.ToString(ParamStructTypes.DT_FORMATTER));

            _sb.AppendLine(((int)ChatItemSaveCode.MESSAGE).ToString());
            _sb.AppendLine(this.Message);

            _sb.AppendLine(((int)ChatItemSaveCode.STATE).ToString());
            _sb.AppendLine(this.state.ToString());

            _sb.AppendLine(((int)ChatItemSaveCode.EXPECTED_REACTIONS_FROM).ToString());
            _sb.AppendLine(this.ExpectsReacionsFrom.Count.ToString());

            if (this.ExpectsReacionsFrom.Count > 0)
            {
                foreach (var user in this.ExpectsReacionsFrom)
                {
                    _sb.AppendLine(((int)ParamStructCommonSaveCode.X_VALUE).ToString());
                    _sb.AppendLine(ComponentUtils.ComponentManagerTypeToLetter(user));
                }
            }

            _sb.AppendLine(((int)ChatItemSaveCode.CHILDREN).ToString());
            _sb.AppendLine(this.children.Count.ToString());

            if (this.children.Count > 0)
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.ENTITY_SEQUENCE);                         // ENTSEQ

                foreach (SimChatItem child in this.children)
                {
                    child.AddToExport(ref _sb);
                }

                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.SEQUENCE_END);                            // SEQEND
                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.ENTITY_CONTINUE);                         // ENTCTN

            }

            // signify end of complex entity
            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.SEQUENCE_END);                            // SEQEND
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Finds how deep in the chat the item lies. Top-level items have depth 0.
        /// </summary>
        /// <returns>the 0-based depth of the chat item measured from its top-level ancestor</returns>
        public int GetDepth()
        {
            if (this.ReactionTo == null)
                return 0;
            else
                return this.ReactionTo.GetDepth() + 1;
        }

        #endregion

        #region METHODS: Merging

        /// <summary>
        /// Assumes that the two items aligned in the CorrespondingChatItemPair instance
        /// are at least comparable: same type, author, timestamp and git_commit key.
        /// It turns the alignment, produced by DistrbutedChat.CompareAndAlign, into 
        /// a single chat item tree.
        /// </summary>
        /// <param name="_pair">the aligned chat item trees</param>
        /// <returns>the union of the two chat items into a single chat item tree</returns>
        internal static SimChatItem Combine(CorrespondingChatItemPair _pair)
        {
            SimChatItem combined = new SimChatItem();
            if (_pair.Item1 != null && _pair.Item2 != null)
            {
                // initialize the comparable part
                combined.Type = _pair.Item1.Type;
                combined.Author = _pair.Item1.Author;
                combined.VotingRegistration_Address = ChatUtils.GetFixedAddressFor(combined.Author);
                combined.VotingRegistration_Password = ChatUtils.GetFixedPasswordFor(combined.Author);
                combined.TimeStamp = _pair.Item1.TimeStamp;
                combined.GitCommitKey = _pair.Item1.GitCommitKey;
                combined.ExpectsReacionsFrom = new List<SimUserRole>(_pair.Item1.ExpectsReacionsFrom);

                // combine the state
                if (_pair.Item1.state == SimChatItemState.CLOSED || _pair.Item2.state == SimChatItemState.CLOSED)
                    combined.state = SimChatItemState.CLOSED;

                // combine the messages
                if (_pair.Item1.Message == _pair.Item2.Message)
                    combined.Message = _pair.Item1.Message;
                else
                    combined.Message = "Version 1: [" + _pair.Item1.Message + "] Version 2: [" + _pair.Item2.Message + "]";
            }
            else
            {
                SimChatItem single = (_pair.Item1 == null) ? _pair.Item2 : _pair.Item1;
                combined.Type = single.Type;
                combined.Author = single.Author;
                combined.VotingRegistration_Address = ChatUtils.GetFixedAddressFor(combined.Author);
                combined.VotingRegistration_Password = ChatUtils.GetFixedPasswordFor(combined.Author);
                combined.TimeStamp = single.TimeStamp;
                combined.GitCommitKey = single.GitCommitKey;
                combined.ExpectsReacionsFrom = new List<SimUserRole>(single.ExpectsReacionsFrom);
                combined.state = single.state;
                combined.Message = single.Message;
            }

            // combine the children
            foreach (CorrespondingChatItemPair child_pair in _pair.Children)
            {
                SimChatItem combined_child = SimChatItem.Combine(child_pair);
                combined.children.Add(combined_child);
                combined_child.reaction_to = combined;
                if (combined.state == SimChatItemState.CLOSED)
                    combined_child.state = SimChatItemState.CLOSED;
            }
            combined.children = combined.children.OrderBy(x => x.TimeStamp).ToList();

            return combined;
        }

        #endregion

        #region STATIC: Test Data for Merging Method

        internal static SimChatItem CreateTestSample01(string _version = "A")
        {
            SimChatItem question = new SimChatItem(SimChatItemType.QUESTION, SimUserRole.ARCHITECTURE, "abcd", "What does the ARC do?",
                    new List<SimUserRole> { SimUserRole.BUILDING_PHYSICS, SimUserRole.MEP_HVAC, SimUserRole.FIRE_SAFETY }, null);
            question.TimeStamp = new DateTime(2018, 12, 30, 06, 45, 15);

            if (_version == "A")
            {
                // Brach 1
                SimChatItem answer_1 = new SimChatItem(SimChatItemType.ANSWER, SimUserRole.BUILDING_PHYSICS, "abcd", "How should I know?!",
                        new List<SimUserRole> { SimUserRole.ARCHITECTURE, SimUserRole.MEP_HVAC }, question);
                answer_1.TimeStamp = new DateTime(2018, 12, 30, 07, 12, 36);

                SimChatItem answer_11 = new SimChatItem(SimChatItemType.ANSWER, SimUserRole.MEP_HVAC, "abcd", "Be nice - ARC has a lot to do and no time to do it!",
                        new List<SimUserRole> { SimUserRole.ARCHITECTURE }, answer_1);
                answer_11.TimeStamp = new DateTime(2018, 12, 30, 07, 24, 59);

                SimChatItem answer_12 = new SimChatItem(SimChatItemType.ANSWER, SimUserRole.ARCHITECTURE, "abcd", "Do you know what you are supposed to do?",
                    new List<SimUserRole> { SimUserRole.BUILDING_PHYSICS, SimUserRole.MEP_HVAC }, answer_1);
                answer_12.TimeStamp = new DateTime(2018, 12, 30, 07, 25, 07);

                SimChatItem answer_11r = new SimChatItem(SimChatItemType.ANSWER_ACCEPT, SimUserRole.ARCHITECTURE, "abcd", "Yes, thank you!",
                    new List<SimUserRole> { SimUserRole.ARCHITECTURE }, answer_11);
                answer_11r.TimeStamp = new DateTime(2018, 12, 30, 07, 26, 18);

                // Branch 2
                SimChatItem answer_2 = new SimChatItem(SimChatItemType.ANSWER, SimUserRole.MEP_HVAC, "abcd", "Make pretty pictures :)",
                    new List<SimUserRole> { SimUserRole.ARCHITECTURE }, question);
                answer_2.TimeStamp = new DateTime(2018, 12, 30, 07, 17, 04);

                // Branch 3
                SimChatItem answer_3 = new SimChatItem(SimChatItemType.ANSWER, SimUserRole.FIRE_SAFETY, "abcd", "I depend on your work...",
                    new List<SimUserRole> { SimUserRole.ARCHITECTURE, SimUserRole.BUILDING_DEVELOPER }, question);
                answer_3.TimeStamp = new DateTime(2018, 12, 30, 08, 10, 15);

                SimChatItem answer_3r = new SimChatItem(SimChatItemType.ANSWER_REJECT, SimUserRole.BUILDING_DEVELOPER, "abcd", "Every one is responsible for their own work!",
                        new List<SimUserRole> { SimUserRole.ARCHITECTURE, SimUserRole.FIRE_SAFETY }, answer_3);
                answer_3r.TimeStamp = new DateTime(2018, 12, 30, 08, 21, 14);
            }
            else if (_version == "B")
            {
                // Brach 1
                SimChatItem answer_1 = new SimChatItem(SimChatItemType.ANSWER, SimUserRole.BUILDING_PHYSICS, "abcd", "How should I know?!",
                        new List<SimUserRole> { SimUserRole.ARCHITECTURE, SimUserRole.MEP_HVAC }, question);
                answer_1.TimeStamp = new DateTime(2018, 12, 30, 07, 12, 36);

                SimChatItem answer_11 = new SimChatItem(SimChatItemType.ANSWER, SimUserRole.MEP_HVAC, "abcd", "Be nice - ARC has a lot to do and no time to do it!",
                        new List<SimUserRole> { SimUserRole.ARCHITECTURE }, answer_1);
                answer_11.TimeStamp = new DateTime(2018, 12, 30, 07, 24, 59);

                SimChatItem answer_12 = new SimChatItem(SimChatItemType.ANSWER, SimUserRole.ARCHITECTURE, "abcd", "Do you know what you are supposed to do?",
                    new List<SimUserRole> { SimUserRole.BUILDING_PHYSICS, SimUserRole.MEP_HVAC }, answer_1);
                answer_12.TimeStamp = new DateTime(2018, 12, 30, 07, 25, 07);

                SimChatItem answer_11r = new SimChatItem(SimChatItemType.ANSWER_ACCEPT, SimUserRole.ARCHITECTURE, "abcd", "Yes, thank you!",
                    new List<SimUserRole> { SimUserRole.ARCHITECTURE }, answer_11);
                answer_11r.TimeStamp = new DateTime(2018, 12, 30, 07, 26, 18);

                // Branch 2
                SimChatItem answer_2 = new SimChatItem(SimChatItemType.ANSWER, SimUserRole.MEP_HVAC, "abcd", "Make pretty pictures :)",
                    new List<SimUserRole> { SimUserRole.ARCHITECTURE }, question);
                answer_2.TimeStamp = new DateTime(2018, 12, 30, 07, 17, 04);
                answer_2.state = SimChatItemState.CLOSED;
            }
            else if (_version == "C")
            {
                // Brach 1
                SimChatItem answer_1 = new SimChatItem(SimChatItemType.ANSWER, SimUserRole.BUILDING_PHYSICS, "abcd", "How should I know?!",
                        new List<SimUserRole> { SimUserRole.ARCHITECTURE, SimUserRole.MEP_HVAC }, question);
                answer_1.TimeStamp = new DateTime(2018, 12, 30, 07, 12, 36);

                SimChatItem answer_11 = new SimChatItem(SimChatItemType.ANSWER, SimUserRole.MEP_HVAC, "abcd", "Be nice - ARC has a lot to do and no time to do it!",
                        new List<SimUserRole> { SimUserRole.ARCHITECTURE }, answer_1);
                answer_11.TimeStamp = new DateTime(2018, 12, 30, 07, 24, 59);

                // Branch 2

                // Branch 3
                SimChatItem answer_3 = new SimChatItem(SimChatItemType.ANSWER, SimUserRole.FIRE_SAFETY, "abcd", "I depend on your work...",
                    new List<SimUserRole> { SimUserRole.ARCHITECTURE, SimUserRole.BUILDING_DEVELOPER }, question);
                answer_3.TimeStamp = new DateTime(2018, 12, 30, 08, 10, 15);
            }
            else if (_version == "D")
            {
                // Branch 3
                SimChatItem answer_3 = new SimChatItem(SimChatItemType.ANSWER, SimUserRole.FIRE_SAFETY, "abcd", "I depend on your work...",
                    new List<SimUserRole> { SimUserRole.ARCHITECTURE, SimUserRole.BUILDING_DEVELOPER }, question);
                answer_3.TimeStamp = new DateTime(2018, 12, 30, 08, 10, 15);
                answer_3.state = SimChatItemState.CLOSED;
            }

            return question;
        }

        internal static SimChatItem CreateTestSample02(string _version = "A")
        {
            SimChatItem voting_session = new SimChatItem(SimChatItemType.VOTING_SESSION, SimUserRole.ARCHITECTURE, "xyz", "Do I make the prettiest pictures?",
                    new List<SimUserRole> { SimUserRole.BUILDING_PHYSICS, SimUserRole.MEP_HVAC }, null);
            voting_session.TimeStamp = new DateTime(2018, 12, 30, 11, 05, 32);

            if (_version == "A")
            {
                SimChatItem vote_1 = new SimChatItem(SimChatItemType.VOTE_ACCEPT, SimUserRole.BUILDING_PHYSICS, "xyz", "Who else could it be?!",
                new List<SimUserRole> { SimUserRole.ARCHITECTURE }, voting_session);
                vote_1.TimeStamp = new DateTime(2018, 12, 30, 11, 07, 18);

                SimChatItem vote_2 = new SimChatItem(SimChatItemType.VOTE_ACCEPT, SimUserRole.MEP_HVAC, "xyz", "I will agree to anything if we get to work afterwards!",
                    new List<SimUserRole> { SimUserRole.ARCHITECTURE }, voting_session);
                vote_2.TimeStamp = new DateTime(2018, 12, 30, 11, 09, 33);
            }
            else if (_version == "B")
            {
                SimChatItem vote_1 = new SimChatItem(SimChatItemType.VOTE_ACCEPT, SimUserRole.BUILDING_PHYSICS, "xyz", "Who else could it be?!",
               new List<SimUserRole> { SimUserRole.ARCHITECTURE }, voting_session);
                vote_1.TimeStamp = new DateTime(2018, 12, 30, 11, 07, 18);
            }
            else if (_version == "C")
            {
                SimChatItem vote_2 = new SimChatItem(SimChatItemType.VOTE_ACCEPT, SimUserRole.MEP_HVAC, "xyz", "I will agree to anything if we get to work afterwards!",
                    new List<SimUserRole> { SimUserRole.ARCHITECTURE }, voting_session);
                vote_2.TimeStamp = new DateTime(2018, 12, 30, 11, 09, 33);
            }

            return voting_session;
        }

        public static SimChat CreateTestConversationA()
        {
            SimChat conv = new SimChat();
            conv.AddItem(SimChatItem.CreateTestSample01("A"));
            conv.AddItem(SimChatItem.CreateTestSample02("A"));
            return conv;
        }

        public static SimChat CreateTestConversationB()
        {
            SimChat conv = new SimChat();
            conv.AddItem(SimChatItem.CreateTestSample01("B"));
            conv.AddItem(SimChatItem.CreateTestSample02("B"));
            return conv;
        }

        public static SimChat CreateTestConversationC()
        {
            SimChat conv = new SimChat();
            conv.AddItem(SimChatItem.CreateTestSample01("C"));
            conv.AddItem(SimChatItem.CreateTestSample02("C"));
            return conv;
        }

        public static SimChat CreateTestConversationD()
        {
            SimChat conv = new SimChat();
            conv.AddItem(SimChatItem.CreateTestSample01("D"));
            return conv;
        }



        #endregion
    }


}
