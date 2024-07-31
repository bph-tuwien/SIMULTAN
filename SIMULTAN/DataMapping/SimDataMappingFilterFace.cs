using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.Taxonomy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SIMULTAN.DataMapping
{
    /// <summary>
    /// Filterable properties for a <see cref="Face"/>
    /// </summary>
    public enum SimDataMappingFaceFilterProperties
    {
        /// <summary>
        /// The name of the Face.
        /// Supports string or Regex
        /// </summary>
        Name = 0,
        /// <summary>
        /// Filters based on the type of face.
        /// Supports <see cref="SimDataMappingFaceType"/>
        /// </summary>
        FaceType = 1,
        /// <summary>
        /// Filters based on the key of the resource file the face is stored in
        /// </summary>
        FileKey = 2, //Special handling outside of the filter to prevent geometry model loading when not necessary
        /// <summary>
        /// Filters based on the tags of the resource files (see <see cref="ResourceEntry.Tags"/>
        /// Supports <see cref="SimTaxonomyEntryReference"/>
        /// </summary>
        FileTags = 3,
    }

    /// <summary>
    /// Filter value for the <see cref="SimDataMappingFaceFilterProperties.FaceType"/> property
    /// </summary>
    public enum SimDataMappingFaceType
    {
        /// <summary>
        /// The face is a Wall
        /// </summary>
        Wall = 0,
        /// <summary>
        /// The face is a Floor face. See <see cref="FaceAlgorithms.IsFloor(PFace)"/>.
        /// </summary>
        Floor = 1,
        /// <summary>
        /// The face is a Floor face. See <see cref="FaceAlgorithms.IsCeiling(PFace)"/>.
        /// </summary>
        Ceiling = 2,
        /// <summary>
        /// The face is either a Floor face or a Ceiling face. Opposite of <see cref="Wall"/>.
        /// </summary>
        FloorOrCeiling = 3,
    }

    /// <summary>
    /// Filter for a <see cref="Face"/>. Used by the <see cref="SimDataMappingRuleFace"/>
    /// </summary>
    public class SimDataMappingFilterFace : SimDataMappingFilterBase<SimDataMappingFaceFilterProperties>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimDataMappingFilterFace"/> class
        /// </summary>
        /// <param name="property">The property to filter for</param>
        /// <param name="value">The value the property is compared to. See description of <see cref="SimDataMappingFaceFilterProperties"/>
        /// to see which value types are supported</param>
        public SimDataMappingFilterFace(SimDataMappingFaceFilterProperties property, object value)
            : base(property, value) { }

        /// <summary>
        /// Returns True when the filtered object matches the filter
        /// </summary>
        /// <param name="face">The object the filter should be applied to</param>
        /// <returns>True when the filter matches the object, otherwise False</returns>
        public bool Match(Face face)
        {
            switch (this.Property)
            {
                case SimDataMappingFaceFilterProperties.Name:
                    if (this.Value is string sname)
                        return face.Name == sname;
                    else if (this.Value is Regex rname)
                        return rname.IsMatch(face.Name);
                    else
                        throw new NotSupportedException("Unsupported value type");
                case SimDataMappingFaceFilterProperties.FaceType:
                    if (this.Value is SimDataMappingFaceType ftenum)
                    {
                        switch (ftenum)
                        {
                            case SimDataMappingFaceType.Wall:
                                return !FaceAlgorithms.IsFloorOrCeiling(face);
                            case SimDataMappingFaceType.Floor:
                                return FaceAlgorithms.IsFloor(face.Normal);
                            case SimDataMappingFaceType.Ceiling:
                                return FaceAlgorithms.IsCeiling(face.Normal);
                            case SimDataMappingFaceType.FloorOrCeiling:
                                return FaceAlgorithms.IsFloorOrCeiling(face);
                            default:
                                throw new NotImplementedException("Unsupported enum value");
                        }
                    }
                    else
                        throw new NotSupportedException("Unsupported value type");
                case SimDataMappingFaceFilterProperties.FileKey:
                    return true; //Has already been handled before arriving here
                case SimDataMappingFaceFilterProperties.FileTags:
                    return true; //Has already been handled before arriving here
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Creates a deep copy of the filter. Uses the <see cref="SimDataMappingFilterBase{TPropertyEnum}.CloneFilterValue(object)"/> method to clone the filter value.
        /// </summary>
        /// <returns>A deep copy of the filter</returns>
        public SimDataMappingFilterFace Clone()
        {
            return new SimDataMappingFilterFace(this.Property, CloneFilterValue(this.Value));
        }
    }
}
