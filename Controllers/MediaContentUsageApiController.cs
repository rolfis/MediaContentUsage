using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Umbraco.Web.Editors;
using Umbraco.Web.WebApi;
using Umbraco.Core.Logging;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Chalmers.Models;
using Chalmers;

namespace Chalmers.Controllers
{
    [Umbraco.Web.Mvc.PluginController("Chalmers")]
    public class MediaContentUsageApiController : UmbracoAuthorizedJsonController
    {
        /// <summary>
        /// Returns the Content referenced for a Media node
        /// </summary>
        /// <param name="id">Media node id</param>
        /// <returns>JSON</returns>
        public MediaContent GetMediaContentUsage(int id)
        {
            var mc = new MediaContent();

            mc.MediaNodeId = id;
            mc.HasContentUsage = false;
            mc.Content = new List<ContentNode>();

            // RelationService
            var rs = ApplicationContext.Current.Services.RelationService;

            // ContentService
            var cs = ApplicationContext.Current.Services.ContentService;

            // MediaService
            var ms = ApplicationContext.Current.Services.MediaService;

            // Media is parent, get relations of our type
            var relations = rs.GetByParent(ms.GetById(id), Constants.RelationTypeAlias);

            // Check for relations
            if (relations.Count() > 0)
            {
                mc.HasContentUsage = true;

                foreach (var relation in relations)
                {
                    var cn = new ContentNode();

                    var csNode = cs.GetById(relation.ChildId);

                    cn.Id = csNode.Id;
                    cn.Name = csNode.Name;
                    cn.Path = csNode.Path;
                    cn.Published = csNode.Published;
                    cn.PathName = NodePathAsNameString(csNode.Id, csNode.Path);
                    cn.Comment = relation.Comment;

                    mc.Content.Add(cn);
                }
            }

            return mc;
        }

        /// <summary>
        /// Return a path to content as string with node names
        /// </summary>
        /// <param name="n">Node Id</param>
        /// <param name="p">Path</param>
        /// <returns></returns>
        private string NodePathAsNameString(int n, string p)
        {
            // ContentService
            var cs = ApplicationContext.Current.Services.ContentService;

            int r;
            string[] tokens = p.Split(',');
            List<string> path = new List<string>();

            foreach (var token in tokens)
            {
                if (Int32.TryParse(token, out r))
                {
                    if (r != -1 && r!= n)
                    {
                        path.Add(cs.GetById(r).Name);
                    }
                }
            }

            return string.Join(" > ", path);
        }
    }
}