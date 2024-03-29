﻿using SIMULTAN.Serializer.DXF;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    /// <summary>
    /// DXF Entry for a entity sequence with missing start tag. This was a bug in pre-version 12 files
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DXFEntitySequenceNoStartEntryParserElementV11<T> : DXFEntryParserElement, IDXFEntitySequenceEntryParserEntry
    {
        private Dictionary<String, DXFEntityParserElementBase<T>> entityElements;
        /// <inheritdoc />
        public IEnumerable<IDXFEntityParserElementBase> Entities => entityElements.Values;

        /// <summary>
        /// Initializes a new instance of the <see cref="DXFEntitySequenceNoStartEntryParserElementV11{T}"/> class
        /// </summary>
        /// <param name="code">The code of this entry</param>
        /// <param name="entityElement">The entity in this collection</param>
        internal DXFEntitySequenceNoStartEntryParserElementV11(ComponentSaveCode code, DXFEntityParserElementBase<T> entityElement)
            : this((int)code, entityElement) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DXFEntitySequenceNoStartEntryParserElementV11{T}"/> class
        /// </summary>
        /// <param name="code">The code of this entry</param>
        /// <param name="entityElement">The entity in this collection</param>
        internal DXFEntitySequenceNoStartEntryParserElementV11(int code, DXFEntityParserElementBase<T> entityElement)
            : this(code, new DXFEntityParserElementBase<T>[] { entityElement })
        { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFEntitySequenceNoStartEntryParserElementV11{T}"/> class
        /// </summary>
        /// <param name="code">The code of this entry</param>
        /// <param name="entityElements">The entities in this collection</param>
        internal DXFEntitySequenceNoStartEntryParserElementV11(int code, IEnumerable<DXFEntityParserElementBase<T>> entityElements)
            : base(code)
        {
            this.entityElements = entityElements.ToDictionary(x => x.EntityName, x => x);
            this.entityElements.Values.ForEach(x => x.Parent = this);
        }

        /// <inheritdoc/>
        internal override object ParseInternal(DXFStreamReader reader, DXFParserInfo info)
        {
            (var key, var value) = reader.GetLast();
            var count = DXFDataConverter<int>.P.FromDXFString(value, info);

            return ParseBody(reader, info, count);
        }

        private object ParseBody(DXFStreamReader reader, DXFParserInfo info, int count)
        {
            T[] resultArray = new T[count];

            if (count > 0)
            {
                //Start TAG is missing
                int key;
                string value;

                //(var key, var value) = reader.Read();
                //if (key != (int)ParamStructCommonSaveCode.ENTITY_START)
                //    throw new Exception(string.Format("Expected Code \"{0}\" but found Code \"{1}\" while parsing Entity Sequence",
                //        ParamStructCommonSaveCode.ENTITY_START, key));
                //if (value != ParamStructTypes.ENTITY_SEQUENCE)
                //    throw new Exception(string.Format("Expected Entity \"{0}\" but found \"{1}\" while parsing Entity Sequence",
                //        ParamStructTypes.ENTITY_SEQUENCE, value));

                //Skip to first element
                reader.Read();

                for (int i = 0; i < count; i++)
                {
                    (key, value) = reader.GetLast();

                    if (key != (int)ParamStructCommonSaveCode.ENTITY_START)
                        throw new Exception(string.Format("Expected Code \"{0}\" but found Code \"{1}\" while parsing Entity Sequence Entry",
                            ParamStructCommonSaveCode.ENTITY_START, key));

                    if (entityElements.TryGetValue(value, out var element))
                    {
                        resultArray[i] = element.Parse(reader, info);
                    }
                    else
                    {
                        throw new Exception(string.Format("Unsupported entity \"{0}\" in sequence.", value));
                    }
                }

                //0 SEQEND
                (key, value) = reader.GetLast();
                if (key != (int)ParamStructCommonSaveCode.ENTITY_START)
                    throw new Exception(string.Format("Expected Code \"{0}\" but found Code \"{1}\" while parsing Entity Sequence",
                        ParamStructCommonSaveCode.ENTITY_START, key));
                if (value != ParamStructTypes.SEQUENCE_END)
                    throw new Exception(string.Format("Expected Entity \"{0}\" but found \"{1}\" while parsing Entity Sequence",
                        ParamStructTypes.SEQUENCE_END, value));

                //0 ENTCTN
                (key, value) = reader.Read();
                if (key != (int)ParamStructCommonSaveCode.ENTITY_START)
                    throw new Exception(string.Format("Expected Code \"{0}\" but found Code \"{1}\" while parsing Entity Sequence",
                        ParamStructCommonSaveCode.ENTITY_START, key));
                if (value != ParamStructTypes.ENTITY_CONTINUE)
                    throw new Exception(string.Format("Expected Entity \"{0}\" but found \"{1}\" while parsing Entity Sequence",
                        ParamStructTypes.ENTITY_CONTINUE, value));
            }

            return resultArray;
        }
    }
}
