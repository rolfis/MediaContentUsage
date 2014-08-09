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
        public bool HasContentUsage { get; set; }
        public List<Content> Content { get; set; }
    }
    public class Content
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Property { get; set; }
    }
    public class DbContent
    {
        public int contentNodeId { get; set; }
        public int propertyTypeId { get; set; }
        public string nodeName { get; set; }
        public string propertyName { get; set; }
        public int dataInt { get; set; }
        public string dataNvarchar { get; set; }
        public string dataNtext { get; set; }
    }

    [Umbraco.Web.Mvc.PluginController("CTH")]
    public class MediaContentUsageController : UmbracoAuthorizedJsonController
    {
        public MediaContentUsage GetMediaContentUsage(int id)
        {
            var ret = new MediaContentUsage();
            ret.MediaNodeId = id;
            ret.Content = new List<Content>();
            ret.HasContentUsage = false;

            try
            {
                // Connect to the Umbraco DB
                var db = ApplicationContext.DatabaseContext.Database;

                // Get Content nodes
                // TODO: datatypeid values should be parameterized in GetMediaContentUsage call
                // TODO: ',' || dataInt || ',' || dataNvarchar || ',' || dataNtext || ',',
                //       then search for '%,@1,%' and 'rel="@1"' to get a better match
                var nodes = db.Query<DbContent>("select pd.contentNodeId,pd.propertyTypeId,d.text as nodeName,pt.Name as propertyName,pd.dataInt,pd.dataNvarchar,pd.dataNtext from cmsPropertyData pd, cmsdocument d, cmsPropertyType pt where pd.contentNodeId=d.nodeId and pd.propertytypeid=pt.id and pd.versionId=d.versionId and d.published=1 and (pd.dataInt=@0 or pd.dataNvarchar like '%' + @1 + '%' or pd.dataNtext like '%' + @1 + '%') and pd.propertytypeid in (select id from cmsPropertyType where datatypeid in (-87,1068,2100,1035,1045))", id, id.ToString());

                // Iterate
                foreach (var node in nodes)
                {
                    var content = new Content();
                    content.Id = node.contentNodeId;
                    content.Name = node.nodeName;
                    content.Property = node.propertyName;
                    ret.Content.Add(content);
                    ret.HasContentUsage = true;
                }
            }
            catch (Exception e)
            {
                LogHelper.Error<MediaContentUsageController>(e.Message, e);
            }

            return ret;
        }
    }
}