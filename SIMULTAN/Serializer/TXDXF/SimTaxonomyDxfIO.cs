using SIMULTAN.Data;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SIMULTAN.Serializer.TXDXF
{
    /// <summary>
    /// DXF IO helpers for the <see cref="SimTaxonomy"/> and <see cref="SimTaxonomyEntry"/>.
    /// </summary>
    public class SimTaxonomyDxfIO
    {

        private static string TAXONOMY_ENTRY_IDENTIFIER = "TaxonomyEntry";

        #region Syntax

        private static DXFComplexEntityParserElement<SimTaxonomy> taxonomyParserElement =
            new DXFComplexEntityParserElement<SimTaxonomy>(new DXFEntityParserElement<SimTaxonomy>(ParamStructTypes.TAXONOMY,
                (r, i) => ParseTaxonomy(r, i, false),
                new DXFEntryParserElement[]
            {
                new DXFSingleEntryParserElement<Guid>(ParamStructCommonSaveCode.ENTITY_GLOBAL_ID),
                new DXFSingleEntryParserElement<long>(ParamStructCommonSaveCode.ENTITY_LOCAL_ID),
                new DXFSingleEntryParserElement<string>(TaxonomySaveCode.TAXONOMY_KEY),
                new DXFSingleEntryParserElement<string>(ParamStructCommonSaveCode.ENTITY_NAME) {MaxVersion = 22},
                new DXFMultiLineTextElement(TaxonomySaveCode.TAXONOMY_DESCRIPTION) {MaxVersion = 22},
                new DXFSingleEntryParserElement<bool>(TaxonomySaveCode.TAXONOMY_IS_READONLY) { IsOptional = true },
                new DXFSingleEntryParserElement<bool>(TaxonomySaveCode.TAXONOMY_IS_DELETABLE) { IsOptional = true },
                new DXFArrayEntryParserElement<CultureInfo>(TaxonomySaveCode.TAXONOMY_SUPPORTED_LANGUAGES, TaxonomySaveCode.TAXONOMY_LANGUAGE) {MinVersion = 23},
                new DXFStructArrayEntryParserElement<SimTaxonomyLocalizationEntry>(TaxonomySaveCode.TAXONOMY_LOCALIZATIONS, ParseLocalizationEntry,
                    new DXFEntryParserElement[]
                    {
                        new DXFSingleEntryParserElement<CultureInfo>(TaxonomySaveCode.TAXONOMY_LOCALIZATION_CULTURE),
                        new DXFSingleEntryParserElement<string>(TaxonomySaveCode.TAXONOMY_LOCALIZATION_NAME),
                        new DXFMultiLineTextElement(TaxonomySaveCode.TAXONOMY_LOCALIZATION_DESCRIPTION),
                    }) {MinVersion = 23},
                new DXFEntitySequenceEntryParserElement<SimTaxonomyEntry>(TaxonomySaveCode.TAXONOMY_ENTRIES,
                    new DXFComplexEntityParserElement<SimTaxonomyEntry>(new DXFEntityParserElement<SimTaxonomyEntry>(ParamStructTypes.TAXONOMY_ENTRY,
                        (re, inf) => ParseTaxonomyEntry(re, inf, false),
                        new DXFEntryParserElement[]
                        {
                            new DXFSingleEntryParserElement<string>(TaxonomySaveCode.TAXONOMY_ENTRY_KEY),
                            new DXFSingleEntryParserElement<long>(ParamStructCommonSaveCode.ENTITY_LOCAL_ID),
                            new DXFSingleEntryParserElement<string>(ParamStructCommonSaveCode.ENTITY_NAME) {MaxVersion = 22},
                            new DXFMultiLineTextElement(TaxonomySaveCode.TAXONOMY_DESCRIPTION) {MaxVersion = 22},
                            new DXFStructArrayEntryParserElement<SimTaxonomyLocalizationEntry>(TaxonomySaveCode.TAXONOMY_LOCALIZATIONS, ParseLocalizationEntry,
                                new DXFEntryParserElement[]
                                {
                                    new DXFSingleEntryParserElement<CultureInfo>(TaxonomySaveCode.TAXONOMY_LOCALIZATION_CULTURE),
                                    new DXFSingleEntryParserElement<string>(TaxonomySaveCode.TAXONOMY_LOCALIZATION_NAME),
                                    new DXFMultiLineTextElement(TaxonomySaveCode.TAXONOMY_LOCALIZATION_DESCRIPTION),
                                }) {MinVersion = 23},
                            new DXFEntitySequenceEntryParserElement<SimTaxonomyEntry>(TaxonomySaveCode.TAXONOMY_ENTRIES,
                                new DXFRecursiveEntityParserElement<SimTaxonomyEntry>(ParamStructTypes.TAXONOMY_ENTRY, TAXONOMY_ENTRY_IDENTIFIER))
                        })){Identifier = TAXONOMY_ENTRY_IDENTIFIER})
            }));


        private static DXFSectionParserElement<SimTaxonomy> taxonomiesSection =
            new DXFSectionParserElement<SimTaxonomy>(ParamStructTypes.TAXONOMY_SECTION, new DXFEntityParserElementBase<SimTaxonomy>[]
            {
                taxonomyParserElement
            });

        private static DXFComplexEntityParserElement<SimTaxonomy> taxonomyExportParserElement =
            new DXFComplexEntityParserElement<SimTaxonomy>(new DXFEntityParserElement<SimTaxonomy>(ParamStructTypes.TAXONOMY,
                (r, i) => ParseTaxonomy(r, i, true),
                new DXFEntryParserElement[]
            {
                new DXFSingleEntryParserElement<string>(ParamStructCommonSaveCode.ENTITY_NAME) {MaxVersion=22},
                new DXFSingleEntryParserElement<string>(TaxonomySaveCode.TAXONOMY_KEY),
                new DXFMultiLineTextElement(TaxonomySaveCode.TAXONOMY_DESCRIPTION) { MaxVersion = 22 },
                new DXFSingleEntryParserElement<bool>(TaxonomySaveCode.TAXONOMY_IS_READONLY) { IsOptional = true },
                new DXFSingleEntryParserElement<bool>(TaxonomySaveCode.TAXONOMY_IS_DELETABLE) { IsOptional = true },
                new DXFArrayEntryParserElement<CultureInfo>(TaxonomySaveCode.TAXONOMY_SUPPORTED_LANGUAGES, TaxonomySaveCode.TAXONOMY_LANGUAGE) {MinVersion = 23},
                new DXFStructArrayEntryParserElement<SimTaxonomyLocalizationEntry>(TaxonomySaveCode.TAXONOMY_LOCALIZATIONS, ParseLocalizationEntry,
                    new DXFEntryParserElement[]
                    {
                        new DXFSingleEntryParserElement<CultureInfo>(TaxonomySaveCode.TAXONOMY_LOCALIZATION_CULTURE),
                        new DXFSingleEntryParserElement<string>(TaxonomySaveCode.TAXONOMY_LOCALIZATION_NAME),
                        new DXFMultiLineTextElement(TaxonomySaveCode.TAXONOMY_LOCALIZATION_DESCRIPTION),
                    }) {MinVersion = 23},
                new DXFEntitySequenceEntryParserElement<SimTaxonomyEntry>(TaxonomySaveCode.TAXONOMY_ENTRIES,
                    new DXFComplexEntityParserElement<SimTaxonomyEntry>(new DXFEntityParserElement<SimTaxonomyEntry>(ParamStructTypes.TAXONOMY_ENTRY,
                        (r, i) => ParseTaxonomyEntry(r, i, true),
                        new DXFEntryParserElement[]
                        {
                            new DXFSingleEntryParserElement<string>(TaxonomySaveCode.TAXONOMY_ENTRY_KEY),
                            new DXFSingleEntryParserElement<string>(ParamStructCommonSaveCode.ENTITY_NAME) { MaxVersion = 22 },
                            new DXFMultiLineTextElement(TaxonomySaveCode.TAXONOMY_DESCRIPTION) { MaxVersion = 22 },
                            new DXFStructArrayEntryParserElement<SimTaxonomyLocalizationEntry>(TaxonomySaveCode.TAXONOMY_LOCALIZATIONS, ParseLocalizationEntry,
                                new DXFEntryParserElement[]
                                {
                                    new DXFSingleEntryParserElement<CultureInfo>(TaxonomySaveCode.TAXONOMY_LOCALIZATION_CULTURE),
                                    new DXFSingleEntryParserElement<string>(TaxonomySaveCode.TAXONOMY_LOCALIZATION_NAME),
                                    new DXFMultiLineTextElement(TaxonomySaveCode.TAXONOMY_LOCALIZATION_DESCRIPTION),
                                }) {MinVersion = 23},
                            new DXFEntitySequenceEntryParserElement<SimTaxonomyEntry>(TaxonomySaveCode.TAXONOMY_ENTRIES,
                                new DXFRecursiveEntityParserElement<SimTaxonomyEntry>(ParamStructTypes.TAXONOMY_ENTRY, TAXONOMY_ENTRY_IDENTIFIER))
                        })){Identifier = TAXONOMY_ENTRY_IDENTIFIER})
            }));


        private static DXFSectionParserElement<SimTaxonomy> taxonomiesExportSection =
            new DXFSectionParserElement<SimTaxonomy>(ParamStructTypes.TAXONOMY_SECTION, new DXFEntityParserElementBase<SimTaxonomy>[]
            {
                taxonomyExportParserElement
            });

        #endregion

        #region Parsing
        private static SimTaxonomyLocalizationEntry ParseLocalizationEntry(DXFParserResultSet result, DXFParserInfo parserInfo)
        {
            var culture = result.Get<CultureInfo>(TaxonomySaveCode.TAXONOMY_LOCALIZATION_CULTURE, null);
            var name = result.Get<string>(TaxonomySaveCode.TAXONOMY_LOCALIZATION_NAME, "");
            var descritpion = result.Get<string>(TaxonomySaveCode.TAXONOMY_LOCALIZATION_DESCRIPTION, "");

            return new SimTaxonomyLocalizationEntry(culture, name, descritpion);
        }

        private static SimTaxonomyEntry ParseTaxonomyEntry(DXFParserResultSet result, DXFParserInfo info, bool isImport)
        {
            var key = result.Get<string>(TaxonomySaveCode.TAXONOMY_ENTRY_KEY, "");
            var name = result.Get<string>(ParamStructCommonSaveCode.ENTITY_NAME, "");
            var description = result.Get<string>(TaxonomySaveCode.TAXONOMY_DESCRIPTION, "");
            var localization = result.Get<SimTaxonomyLocalizationEntry[]>(TaxonomySaveCode.TAXONOMY_LOCALIZATIONS, new SimTaxonomyLocalizationEntry[] { });

            var entries = result.Get<SimTaxonomyEntry[]>(TaxonomySaveCode.TAXONOMY_ENTRIES, new SimTaxonomyEntry[] { });

            if (string.IsNullOrEmpty(key))
            {
                key = Guid.NewGuid().ToString("N");
                info.Log(string.Format("Taxonomy entry has an empty key, changing it to {0}", key));
            }

            SimTaxonomyEntry taxEntry;
            if (!isImport)
            {
                var localId = result.Get<long>(ParamStructCommonSaveCode.ENTITY_LOCAL_ID, 0);
                var id = new SimId(Guid.Empty, localId);
                taxEntry = new SimTaxonomyEntry(id, key);
            }
            else
            {
                taxEntry = new SimTaxonomyEntry(key);
            }


            if (info.FileVersion < 23)
            {
                // Localization migration
                taxEntry.Localization.AddLanguage(CultureInfo.InvariantCulture);
                var loc = new SimTaxonomyLocalizationEntry(CultureInfo.InvariantCulture, name, description);
                taxEntry.Localization.SetLanguage(loc);
            }

            foreach (var locEntry in localization)
            {
                taxEntry.Localization.AddLanguage(locEntry.Culture);
                taxEntry.Localization.SetLanguage(locEntry);
            }

            foreach (var entry in entries)
            {
                taxEntry.Children.Add(entry);
            }

            return taxEntry;
        }

        private static SimTaxonomy ParseTaxonomy(DXFParserResultSet result, DXFParserInfo info, bool isImport)
        {
            var key = result.Get<string>(TaxonomySaveCode.TAXONOMY_KEY, "");
            var name = result.Get<string>(ParamStructCommonSaveCode.ENTITY_NAME, "");
            var description = result.Get<string>(TaxonomySaveCode.TAXONOMY_DESCRIPTION, "");
            var isReadonly = result.Get<bool>(TaxonomySaveCode.TAXONOMY_IS_READONLY, false);
            var isDeletable = result.Get<bool>(TaxonomySaveCode.TAXONOMY_IS_DELETABLE, true);
            var languages = result.Get<CultureInfo[]>(TaxonomySaveCode.TAXONOMY_SUPPORTED_LANGUAGES, new CultureInfo[] { });
            var localization = result.Get<SimTaxonomyLocalizationEntry[]>(TaxonomySaveCode.TAXONOMY_LOCALIZATIONS, new SimTaxonomyLocalizationEntry[] { });

            var entries = result.Get<SimTaxonomyEntry[]>(TaxonomySaveCode.TAXONOMY_ENTRIES, new SimTaxonomyEntry[] { });

            SimTaxonomy taxonomy;
            if (!isImport)
            {
                var id = result.GetSimId(ParamStructCommonSaveCode.ENTITY_GLOBAL_ID, ParamStructCommonSaveCode.ENTITY_LOCAL_ID, info.GlobalId);
                taxonomy = new SimTaxonomy(id) { Key = key, IsDeletable = isDeletable };
            }
            else
            {
                taxonomy = new SimTaxonomy() { Key = key, IsDeletable = isDeletable };
            }

            if (info.FileVersion < 23)
            {
                // Localization migration
                taxonomy.Languages.Add(CultureInfo.InvariantCulture);
                taxonomy.Localization.SetLanguage(CultureInfo.InvariantCulture, name, description);
            }

            foreach (var lang in languages)
            {
                if (!taxonomy.Languages.Contains(lang))
                    taxonomy.Languages.Add(lang);
            }

            foreach (var locEntry in localization)
            {
                taxonomy.Localization.AddLanguage(locEntry.Culture);
                taxonomy.Localization.SetLanguage(locEntry);
            }

            foreach (var entry in entries)
            {
                taxonomy.Entries.Add(entry);
            }

            // set at the end, cause otherwise entries cannot be added
            taxonomy.IsReadonly = isReadonly;

            return taxonomy;
        }

        #endregion

        #region Read/Write

        /// <summary>
        /// Reads a taxonomy file and parses the contents into the taxonomies of the project.
        /// </summary>
        /// <param name="file">The taxonomy file to read.</param>
        /// <param name="info">The parser info</param>
        public static void Read(FileInfo file, DXFParserInfo info)
        {
            info.CurrentFile = file;
            using (var fs = file.OpenRead())
            {
                if (fs.Length == 0)
                    return;
                using (var reader = new DXFStreamReader(fs))
                {
                    Read(reader, info);
                }
            }
        }

        /// <summary>
        /// Used to import a taxonomy file into an existing project.
        /// </summary>
        /// <param name="file">The file to read from.</param>
        /// <param name="info">The parser info with the project to import into.</param>
        public static void Import(FileInfo file, DXFParserInfo info)
        {
            info.CurrentFile = file;
            using (var fs = file.OpenRead())
            {
                if (fs.Length == 0)
                    return;
                using (var reader = new DXFStreamReader(fs))
                {
                    Import(reader, info);
                }
            }
        }

        /// <summary>
        /// Reads taxonomies from a provided reader and parses the contents into the taxonomies of the project.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <param name="parserInfo">The parser info</param>
        public static void Read(DXFStreamReader reader, DXFParserInfo parserInfo)
        {
            Read(reader, parserInfo, false);
        }

        /// <summary>
        /// Used to import a taxonomy file into an existing project.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <param name="parserInfo">The parser info with the project to import into.</param>
        public static void Import(DXFStreamReader reader, DXFParserInfo parserInfo)
        {
            Read(reader, parserInfo, true);
        }

        private static void Read(DXFStreamReader reader, DXFParserInfo parserInfo, bool isImport)
        {
            //Version section
            if (CommonParserElements.VersionSectionElement.IsParsable(reader, parserInfo))
            {
                parserInfo = CommonParserElements.VersionSectionElement.Parse(reader, parserInfo).First();
            }

            //Data section
            List<SimTaxonomy> taxonomies;
            if (!isImport)
            {
                taxonomies = taxonomiesSection.Parse(reader, parserInfo);
            }
            else
            {
                taxonomies = taxonomiesExportSection.Parse(reader, parserInfo);
            }


            parserInfo.ProjectData.Taxonomies.StartLoading();

            foreach (var taxonomy in taxonomies)
            {
                if (taxonomy != null)
                    parserInfo.ProjectData.Taxonomies.Add(taxonomy);
            }

            parserInfo.ProjectData.Taxonomies.StopLoading();

            //EOF
            EOFParserElement.Element.Parse(reader);
        }

        /// <summary>
        /// Writes taxonomies to a provided file.
        /// </summary>
        /// <param name="file">The file to write the taxonomies to.</param>
        /// <param name="taxonomies">The taxonomies to write.</param>
        /// <param name="projectData">The project data these taxonomies belong to.</param>
        public static void Write(FileInfo file, IEnumerable<SimTaxonomy> taxonomies, ExtendedProjectData projectData)
        {
            using (var fs = file.Open(FileMode.Create, FileAccess.Write))
            {
                using (var writer = new DXFStreamWriter(fs))
                {
                    Write(writer, taxonomies, projectData);
                }
            }
        }

        /// <summary>
        /// Used to export taxonomies. Does not include ids.
        /// </summary>
        /// <param name="file">The file to export to</param>
        /// <param name="taxonomies">The taxonomies to export</param>
        /// <param name="projectData">The project data where they originate from</param>
        public static void Export(FileInfo file, IEnumerable<SimTaxonomy> taxonomies, ExtendedProjectData projectData)
        {
            using (var fs = file.Open(FileMode.Create, FileAccess.Write))
            {
                using (var writer = new DXFStreamWriter(fs))
                {
                    Export(writer, taxonomies, projectData);
                }
            }
        }

        /// <summary>
        /// Writes taxonomies to a provided writer.
        /// </summary>
        /// <param name="writer">The writer to write the taxonomies to.</param>
        /// <param name="taxonomies">The taxonomies to write.</param>
        /// <param name="projectData">The project data these taxonomies belong to.</param>
        public static void Write(DXFStreamWriter writer, IEnumerable<SimTaxonomy> taxonomies, ExtendedProjectData projectData)
        {
            Write(writer, taxonomies, projectData, false);
        }

        /// <summary>
        /// Used to export taxonomies. Does not include ids.
        /// </summary>
        /// <param name="writer">The writer</param>
        /// <param name="taxonomies">The taxonomies to export</param>
        /// <param name="projectData">The project data where they originate from</param>
        public static void Export(DXFStreamWriter writer, IEnumerable<SimTaxonomy> taxonomies, ExtendedProjectData projectData)
        {
            Write(writer, taxonomies, projectData, true);
        }

        private static void Write(DXFStreamWriter writer, IEnumerable<SimTaxonomy> taxonomies, ExtendedProjectData projectData, bool isExport)
        {
            writer.WriteVersionSection();


            writer.StartSection(ParamStructTypes.TAXONOMY_SECTION, -1);

            foreach (var taxonomy in taxonomies)
            {
                WriteTaxonomy(writer, taxonomy, projectData, isExport);
            }

            writer.EndSection();

            writer.WriteEOF();
        }

        private static void WriteLocalization(SimTaxonomyLocalizationEntry entry, DXFStreamWriter writer)
        {
            writer.Write(TaxonomySaveCode.TAXONOMY_LOCALIZATION_CULTURE, entry.Culture);
            writer.Write(TaxonomySaveCode.TAXONOMY_LOCALIZATION_NAME, entry.Name);
            writer.WriteMultilineText(TaxonomySaveCode.TAXONOMY_LOCALIZATION_DESCRIPTION, entry.Description);
        }

        private static void WriteTaxonomy(DXFStreamWriter writer, SimTaxonomy taxonomy, ExtendedProjectData projectData, bool isExport)
        {
            writer.StartComplexEntity();

            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.TAXONOMY);

            if (!isExport)
            {
                // ID
                writer.WriteGlobalId(ParamStructCommonSaveCode.ENTITY_GLOBAL_ID, taxonomy.Id.GlobalId, taxonomy.Factory.CalledFromLocation.GlobalID);
                writer.Write(ParamStructCommonSaveCode.ENTITY_LOCAL_ID, taxonomy.Id.LocalId);
            }

            writer.Write(TaxonomySaveCode.TAXONOMY_KEY, taxonomy.Key);
            writer.Write(TaxonomySaveCode.TAXONOMY_IS_READONLY, taxonomy.IsReadonly);
            writer.Write(TaxonomySaveCode.TAXONOMY_IS_DELETABLE, taxonomy.IsDeletable);
            writer.WriteArray(TaxonomySaveCode.TAXONOMY_SUPPORTED_LANGUAGES, taxonomy.Languages, (e, w) =>
            {
                w.Write(TaxonomySaveCode.TAXONOMY_LANGUAGE, e);
            });
            writer.WriteArray(TaxonomySaveCode.TAXONOMY_LOCALIZATIONS, taxonomy.Localization.Entries.Values, WriteLocalization);

            // Child Entries
            writer.WriteEntitySequence(TaxonomySaveCode.TAXONOMY_ENTRIES, taxonomy.Entries, (e, w) => WriteTaxonomyEntry(e, w, isExport));

            writer.EndComplexEntity();
        }

        private static void WriteTaxonomyEntry(SimTaxonomyEntry entry, DXFStreamWriter writer, bool isExport)
        {
            writer.StartComplexEntity();

            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.TAXONOMY_ENTRY);

            writer.Write(TaxonomySaveCode.TAXONOMY_ENTRY_KEY, entry.Key);

            if (!isExport)
            {
                // ID
                writer.Write(ParamStructCommonSaveCode.ENTITY_LOCAL_ID, entry.Id.LocalId);
            }

            writer.WriteArray(TaxonomySaveCode.TAXONOMY_LOCALIZATIONS, entry.Localization.Entries.Values, WriteLocalization);

            // Child Entries
            writer.WriteEntitySequence(TaxonomySaveCode.TAXONOMY_ENTRIES, entry.Children, (e, w) => WriteTaxonomyEntry(e, w, isExport));

            writer.EndComplexEntity();
        }

        #endregion


    }
}
