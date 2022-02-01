using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SIMULTAN.Serializer.DXF
{
    #region DXF_Entity
    /// <summary>
    /// Base class for all serialization based on the DXF Standard.
    /// </summary>
    internal class DXFEntity
    {
        #region CLASS MEMBERS
        /// <summary>
        /// The name of the entity. Determines the subtype.
        /// </summary>
        public string ENT_Name { get; protected set; }
        /// <summary>
        /// Optional entry. Application specific, gives the qualified name of the class.
        /// </summary>
        public string ENT_ClassName { get; protected set; }
        /// <summary>
        /// For objects with a global location of type Guid.
        /// </summary>
        public Guid ENT_LOCATION { get; protected set; }
        /// <summary>
        /// For objects carrying ids of type Int64.
        /// </summary>
        public long ENT_ID { get; protected set; }
        /// <summary>
        /// For dictionary keys. Each key is saved with its value.
        /// </summary>
        public string ENT_KEY { get; protected set; }
        /// <summary>
        /// Indicates if the serialized entity contains other ones.
        /// </summary>
        public bool ENT_HasEntites { get; protected set; }
        /// <summary>
        /// The decoder attached to the entity and responsible for its de-serialization.
        /// </summary>
        internal DXFDecoder Decoder { get; set; }

        #endregion

        #region .CTOR
        /// <summary>
        /// Initializes the entity with default settings.
        /// </summary>
        public DXFEntity()
        {
            this.ENT_Name = null;
            this.ENT_ClassName = null;
            this.ENT_ID = -1;
            this.ENT_HasEntites = false;
        }

        #endregion

        #region METHODS: Entity parsing
        /// <summary>
        /// Performs one parsing step. If the entity is complex, it reads its content and prepares it for parsing.
        /// </summary>
        public void ParseNext()
        {
            // start parsing next entity
            this.ReadProperties();
            // if it contains entities itself, parse them next
            if (this.ENT_HasEntites)
                this.ReadEntities();
        }
        /// <summary>
        /// Main method for traversing complex entities and creating the corresponding hierarchy.
        /// </summary>
        protected void ReadEntities()
        {
            DXFEntity e;
            do
            {
                if (this.Decoder.FValue == ParamStructTypes.EOF)
                {
                    // end of file
                    this.Decoder.ReleaseRessources();
                    return;
                }
                e = this.Decoder.CreateEntity();
                if (e == null)
                {
                    // reached end of complex entity
                    this.Decoder.Next();
                    break;
                }
                if (e is DXFContinue)
                {
                    // carry on parsing the same entity
                    this.ParseNext();
                    break;
                }
                e.ParseNext();
                if (e.GetType().IsSubclassOf(typeof(DXFEntity)))
                {
                    // complete parsing
                    e.OnLoaded();
                    // add to list of entities of this entity
                    this.AddEntity(e);
                }
            }
            while (this.Decoder.HasNext());
        }

        #endregion

        #region METHODS: Property parsing
        /// <summary>
        /// Traverses the properties of the entity.
        /// </summary>
        protected void ReadProperties()
        {
            while (this.Decoder.HasNext())
            {
                this.Decoder.Next();
                switch (this.Decoder.FCode)
                {
                    case (int)ParamStructCommonSaveCode.ENTITY_START:
                        // reached next entity
                        return;
                    default:
                        // otherwise continue parsing
                        this.ReadPoperty();
                        break;
                }
            }
        }

        /// <summary>
        /// Parses each property of an entity according to ints numerical code.
        /// </summary>
        public virtual void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ParamStructCommonSaveCode.CLASS_NAME:
                    this.ENT_ClassName = this.Decoder.FValue;
                    break;
                case (int)ParamStructCommonSaveCode.ENTITY_LOCATION:
                    this.ENT_LOCATION = new Guid(this.Decoder.FValue);
                    break;
                case (int)ParamStructCommonSaveCode.ENTITY_ID:
                    this.ENT_ID = this.Decoder.LongValue();
                    break;
                case (int)ParamStructCommonSaveCode.ENTITY_KEY:
                    this.ENT_KEY = this.Decoder.FValue;
                    break;
            }
        }

        #endregion

        #region METHODS: For Subtypes

        /// <summary>
        /// For post-parsing processing.
        /// </summary>
        internal virtual void OnLoaded() { }
        /// <summary>
        /// Adds entities to a complex entity.
        /// </summary>
        /// <param name="_e">the entity to add</param>
        /// <returns>true if the adding was successful, false otherwise</returns>
        internal virtual bool AddEntity(DXFEntity _e)
        {
            return false;
        }
        /// <summary>
        /// Adds entities to a complex entity in a second, deferred pass.
        /// </summary>
        internal virtual void AddDeferredEntities()
        { }

        #endregion

    }
    #endregion

    #region DXF_Dummy_Entity
    /// <summary>
    /// A dummy for testing.
    /// </summary>
    internal class DXFDummy : DXFEntity
    {
        /// <summary>
        /// Initializes the dummy with default values.
        /// </summary>
        public DXFDummy()
        {
            this.ENT_Name = null;
            this.ENT_ClassName = null;
            this.ENT_ID = -1;
            this.ENT_HasEntites = false;
        }
        /// <summary>
        /// Initializes a named dummy.
        /// </summary>
        /// <param name="_name">the name of the dummy</param>
        public DXFDummy(string _name)
        {
            this.ENT_Name = _name;
            this.ENT_ClassName = null;
            this.ENT_ID = -1;
            this.ENT_HasEntites = false;
        }
        /// <inheritdoc/>
        public override string ToString()
        {
            string dxfS = base.ToString();
            if (this.ENT_Name != null)
                dxfS += "[" + this.ENT_Name + "]";

            return dxfS;
        }
    }

    #endregion

    #region DXF_CONTINUE
    /// <summary>
    /// A virtual entity meant to signify the end of a sequence of entities contained in another enitiy.
    /// </summary>
    internal class DXFContinue : DXFEntity
    {
        /// <summary>
        /// Initializes the continue entity with default values.
        /// </summary>
        public DXFContinue()
        {
            this.ENT_Name = ParamStructTypes.ENTITY_CONTINUE;
            this.ENT_ClassName = null;
            this.ENT_ID = -1;
            this.ENT_HasEntites = false;
        }
    }
    #endregion
}