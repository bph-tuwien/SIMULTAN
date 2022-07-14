using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.CODXF
{
    /// <summary>
    /// Provides methods for serializing user-defined component lists in a component file
    /// </summary>
    internal static class ComponentDxfIOUserLists
    {
        #region Syntax User List

        /// <summary>
        /// Syntax for a <see cref="SimUserComponentList"/>
        /// </summary>
        internal static DXFEntityParserElementBase<SimUserComponentList> UserListEntityElement =
            new DXFENTCTNEntityParserElementV11<SimUserComponentList>(
                new DXFEntityParserElement<SimUserComponentList>(ParamStructTypes.USER_LIST,
                    (data, info) => ParseUserComponentList(data, info),
                    new DXFEntryParserElement[]
                    {
                        new DXFSingleEntryParserElement<string>(UserComponentListSaveCode.NAME),
                        new DXFStructArrayEntryParserElement<SimId>(UserComponentListSaveCode.ROOT_COMPONENTS, ParseComponentId,
                            new DXFEntryParserElement[]
                            {
                                new DXFSingleEntryParserElement<long>(ParamStructCommonSaveCode.ENTITY_LOCAL_ID),
                                new DXFSingleEntryParserElement<Guid>(ParamStructCommonSaveCode.ENTITY_GLOBAL_ID),
                            }) { MinVersion = 12 },
                        new DXFArrayEntryParserElement<string>(UserComponentListSaveCode.ROOT_COMPONENTS, ParamStructCommonSaveCode.ENTITY_LOCAL_ID)
                            { MaxVersion = 11 }
                    }));

        /// <summary>
        /// Syntax for the user list section
        /// </summary>
        internal static DXFSectionParserElement<SimUserComponentList> UserListSectionElement =
            new DXFSectionParserElement<SimUserComponentList>(ParamStructTypes.USERCOMPONENTLIST_SECTION,
                new DXFEntityParserElementBase<SimUserComponentList>[]
                {
                    UserListEntityElement
                });

        /// <summary>
        /// Sytax for the user list section in pre-version 12 files
        /// </summary>
        internal static DXFSectionParserElement<SimUserComponentList> UserListSectionElementV11 =
            new DXFSectionParserElement<SimUserComponentList>(ParamStructTypes.ENTITY_SECTION,
                new DXFEntityParserElementBase<SimUserComponentList>[]
                {
                    UserListEntityElement
                });

        #endregion

        /// <summary>
        /// Writes a user list section to the DXF stream
        /// </summary>
        /// <param name="userLists">The user-defined lists to serialize</param>
        /// <param name="writer">The writer into which the data should be written</param>
        internal static void WriteUserListsSection(IEnumerable<SimUserComponentList> userLists, DXFStreamWriter writer)
        {
            writer.StartSection(ParamStructTypes.USERCOMPONENTLIST_SECTION);

            foreach (var ucl in userLists)
                WriteUserList(ucl, writer);

            writer.EndSection();
        }
        /// <summary>
        /// Reads a user list section. The results are stored in <see cref="DXFParserInfo.ProjectData"/>
        /// </summary>
        /// <param name="reader">The DXF reader to read from</param>
        /// <param name="info">Info for the parser</param>
        internal static void ReadUserListsSection(DXFStreamReader reader, DXFParserInfo info)
        {
            List<SimUserComponentList> lists = null;

            if (info.FileVersion >= 12)
                lists = UserListSectionElement.Parse(reader, info);
            else
                lists = UserListSectionElementV11.Parse(reader, info);

            foreach (var list in lists)
                info.ProjectData.UserComponentLists.Add(list);
        }


        /// <summary>
        /// Writes a user list to the DXF stream
        /// </summary>
        /// <param name="userList">The user list to serialize</param>
        /// <param name="writer">The writer into which the data should be written</param>
        internal static void WriteUserList(SimUserComponentList userList, DXFStreamWriter writer)
        {
            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.USER_LIST);
            writer.Write(ParamStructCommonSaveCode.CLASS_NAME, typeof(SimUserComponentList));

            writer.Write(UserComponentListSaveCode.NAME, userList.Name);
            writer.WriteArray(UserComponentListSaveCode.ROOT_COMPONENTS, userList.RootComponents, (comp, iwriter) =>
            {
                iwriter.Write(ParamStructCommonSaveCode.ENTITY_LOCAL_ID, comp.Id.LocalId);
                iwriter.Write(ParamStructCommonSaveCode.ENTITY_GLOBAL_ID, Guid.Empty);
            });
        }

        private static SimUserComponentList ParseUserComponentList(DXFParserResultSet data, DXFParserInfo info)
        {
            var name = data.Get<string>(UserComponentListSaveCode.NAME, string.Empty);
            SimId[] rootComponentIds = null;
            
            if (info.FileVersion >= 12)
                rootComponentIds = data.Get<SimId[]>(UserComponentListSaveCode.ROOT_COMPONENTS, new SimId[] { });
            else
            {
                var stringIds = data.Get<string[]>(UserComponentListSaveCode.ROOT_COMPONENTS, new string[] { });
                rootComponentIds = new SimId[stringIds.Length];
                for (int i = 0; i < stringIds.Length; i++)
                {
                    var parsed = SimObjectId.FromString(stringIds[i]);
                    rootComponentIds[i] = new SimId(parsed.global != Guid.Empty ? parsed.global : info.GlobalId, parsed.local);
                }
            }

            try
            {
                var rootComponents = rootComponentIds.Select(x => info.ProjectData.IdGenerator.GetById<SimComponent>(x)).Where(x => x != null);
                return new SimUserComponentList(name, rootComponents);
            }
            catch (Exception e)
            {
                info.Log(string.Format("Failed to load User Component List with Name=\"{0}\"\nException: {2}\nStackTrace:\n{3}",
                    name, e.Message, e.StackTrace
                    ));
            }

            return null;
        }

        private static SimId ParseComponentId(DXFParserResultSet data, DXFParserInfo info)
        {
            var id = data.GetSimId(ParamStructCommonSaveCode.ENTITY_GLOBAL_ID, ParamStructCommonSaveCode.ENTITY_LOCAL_ID, info.GlobalId);
            return id;
        }
    }
}
