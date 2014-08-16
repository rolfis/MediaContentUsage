using System;

namespace Chalmers
{
    public class Constants
    {
        /// <summary>
        /// The alias for our custom RelationType
        /// </summary>
        public const string RelationTypeAlias = "relateMediaToContent";

        /// <summary>
        /// Human-readable name for our custom RelationType
        /// </summary>
        public const string RelationTypeName = "Relate Media and Content";

        // http://our.umbraco.org/wiki/reference/api-cheatsheet/relationtypes-and-relations/object-guids-for-creating-relation-types
        /// <summary>
        /// The GUID for Media RelationType
        /// </summary>
        public static Guid RelationTypeMedia = new Guid("B796F64C-1F99-4FFB-B886-4BF4BC011A9C");

        /// <summary>
        /// The GUID for Document RelationType
        /// </summary>
        public static Guid RelationTypeDocument = new Guid("C66BA18E-EAF3-4CFF-8A22-41B16D66A972");
    }
}