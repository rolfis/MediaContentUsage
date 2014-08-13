using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Umbraco.Web.WebApi;
using Umbraco.Web.Editors;
using Umbraco.Core.Persistence;
using Umbraco.Core.Logging;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace CTH.Controllers
{
    public class MediaContentUsage
    {
        public int MediaNodeId { get; set; }
        public bool MediaIsValid { get; set; }
        public bool HasContentUsage { get; set; }
        public List<Content> Content { get; set; }
    }
    public class Content
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Property { get; set; }
        public string Data { get; set; }
    }
    public class Result
    {
        public int contentNodeId { get; set; }
        public int propertyTypeId { get; set; }
        public string nodeName { get; set; }
        public string propertyName { get; set; }
        public string dataCombined { get; set; }
    }

    [Umbraco.Web.Mvc.PluginController("CTH")]
    public class MediaContentUsageController : UmbracoAuthorizedJsonController
    {
        // Default Data Type Ids
        const string DefaultDataTypeIds = "-87,1035,1045";

        public MediaContentUsage GetMediaContentUsage(int id)
        {
            return GetMediaContentUsage(id, "");
        }

        public MediaContentUsage GetMediaContentUsage(int id, string propertyTypes)
        {
            var ret = new MediaContentUsage();
            ret.MediaNodeId = id;
            ret.Content = new List<Content>();
            ret.HasContentUsage = false;

            // Check if this is really a Media Node
            var ms = ApplicationContext.Current.Services.MediaService;

            var media = ms.GetById(id);

            if (media != null)
            {
                ret.MediaIsValid = true;
            }
            else
            {
                ret.MediaIsValid = false;
                return ret;
            }    

            // Default or sanitized Property Types
            string propertyTypesList = SanitizeDataTypesInput(propertyTypes);

            // Debug logging
            LogHelper.Info<MediaContentUsageController>("Searching Data Types: " + propertyTypesList);

            // Magic happens here
            try
            {
                // Connect to the Umbraco DB
                using (var db = ApplicationContext.DatabaseContext.Database)
                {
                    // Debug logging
                    LogHelper.Info<MediaContentUsageController>(String.Format("select pd.contentNodeId,d.text as nodeName,pt.Name as propertyName,isnull(cast(pd.dataInt as nvarchar(100)),'') + ',' + isnull(pd.dataNvarchar,'') + ',' + isnull(cast(pd.dataNtext as nvarchar(max)),'') + ',' as dataCombined from cmsPropertyData pd, cmsdocument d, cmsPropertyType pt where pd.contentNodeId=d.nodeId and pd.propertytypeid=pt.id and pd.versionId=d.versionId and d.published=1 and (isnull(cast(pd.dataInt as nvarchar(100)),'') + ',' + isnull(pd.dataNvarchar,'') + ',' + isnull(cast(pd.dataNtext as nvarchar(max)),'') + ',' like '%,{0},%' or isnull(cast(pd.dataInt as nvarchar(100)),'') + ',' + isnull(pd.dataNvarchar,'') + ',' + isnull(cast(pd.dataNtext as nvarchar(max)),'') + ',' like '%rel=\"{0}\"%') and pd.propertytypeid in (select id from cmsPropertyType where datatypeid in (" + propertyTypesList + "))", id.ToString()));

                    // Get Content nodes (really slow query, I'm clearly not an optimizer)
                    var nodes = db.Query<Result>("select pd.contentNodeId,d.text as nodeName,pt.Name as propertyName,isnull(cast(pd.dataInt as nvarchar(100)),'') + ',' + isnull(pd.dataNvarchar,'') + ',' + isnull(cast(pd.dataNtext as nvarchar(max)),'') + ',' as dataCombined from cmsPropertyData pd, cmsdocument d, cmsPropertyType pt where pd.contentNodeId=d.nodeId and pd.propertytypeid=pt.id and pd.versionId=d.versionId and d.published=1 and (isnull(cast(pd.dataInt as nvarchar(100)),'') + ',' + isnull(pd.dataNvarchar,'') + ',' + isnull(cast(pd.dataNtext as nvarchar(max)),'') + ',' like '%,' + @0 + ',%' or isnull(cast(pd.dataInt as nvarchar(100)),'') + ',' + isnull(pd.dataNvarchar,'') + ',' + isnull(cast(pd.dataNtext as nvarchar(max)),'') + ',' like '%rel=\"' + @0 + '\"%') and pd.propertytypeid in (select id from cmsPropertyType where datatypeid in (" + propertyTypesList + "))",  id.ToString());

                    // Populate JSON data from SQL result
                    foreach (var node in nodes)
                    {
                        var content = new Content();
                        content.Id = node.contentNodeId;
                        content.Name = node.nodeName;
                        content.Property = node.propertyName;
                        content.Data = node.dataCombined;
                        ret.Content.Add(content);
                    }

                    if (ret.Content.Count > 0)
                    {
                        ret.HasContentUsage = true;
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.Error<MediaContentUsageController>(e.Message, e);
            }

            return ret;
        }

        // Check input DataTypes to search for, return default or sanitize user input
        // Richtext Editor (-87), Media Picker (1035), Multiple Media Picker (1045)
        private string SanitizeDataTypesInput (string pt)
        {
            if (String.IsNullOrEmpty(pt))
            {
                return DefaultDataTypeIds;
            }
            else
	        {
                string[] tokens = pt.Split(',');
                List<int> parsedTokens = new List<int>();
                int result;

                foreach (var item in tokens)
                {
                    if (Int32.TryParse(item, out result))
                    {
                        parsedTokens.Add(result);
                    }
                }

                if (parsedTokens.Count > 0)
                {
                    return string.Join(",", parsedTokens.ToArray());
                }
                else
                {
                    return DefaultDataTypeIds;
                }
	        }
        }
    }
}
