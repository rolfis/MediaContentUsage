using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Umbraco.Core.Publishing;
using Umbraco.Core.Services;
using Umbraco.Core.Logging;
using Chalmers.MediaContentUsage.Models;
using Chalmers.MediaContentUsage.Helpers;

namespace Chalmers.MediaContentUsage
{
    public class EventHandler : ApplicationEventHandler
    {
        // Bind to events
        public EventHandler()
        {
            ContentService.Published += AddMediaUsage;
            ContentService.UnPublished += RemoveMediaUsage;
            ContentService.Deleted += RemoveMediaUsage;
            /* RelationService.DeletedRelationType += RelationService_DeletedRelationType; */
        }

        /// <summary>
        /// Application Started
        /// </summary>
        /// <param name="app"></param>
        /// <param name="ctx"></param>
        protected override void ApplicationStarted(UmbracoApplicationBase app, ApplicationContext ctx)
        {
            // RelationService
            var relationService = ctx.Services.RelationService;

            // RelationType is missing
            if (relationService.GetRelationTypeByAlias("relateMediaToContent") == null)
	        {
                CreateRelationType();
                AddMediaUsageForAllContent();
            }
        }

        /// <summary>
        /// RelationType needed in Umbraco deleted, re-create and re-index all Content Medias
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RelationService_DeletedRelationType(IRelationService sender, DeleteEventArgs<IRelationType> e)
        {
            LogHelper.Info<EventHandler>(String.Format("RelationService_DeletedRelationType"));

            foreach (var item in e.DeletedEntities)
            {
                LogHelper.Info<EventHandler>(String.Format("item: {0}", item.Alias));

                if (item.Alias == "relateMediaToContent")
                {
                    CreateRelationType();
                    AddMediaUsageForAllContent();
                }
            }
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates the RelationType needed in Umbraco to store Media-Content relations
        /// </summary>
        private void CreateRelationType()
        {
            // RelationService
            var relationService = ApplicationContext.Current.Services.RelationService;

            // RelationType alias and name
            const string RelationTypeAlias = "relateMediaToContent";
            const string RelationTypeName = "Relate Media and Content";

            // http://our.umbraco.org/wiki/reference/api-cheatsheet/relationtypes-and-relations/object-guids-for-creating-relation-types
            Guid RelationTypeMedia = new Guid("B796F64C-1F99-4FFB-B886-4BF4BC011A9C");
            Guid RelationTypeDocument = new Guid("C66BA18E-EAF3-4CFF-8A22-41B16D66A972");

            var relationType = new RelationType(RelationTypeDocument, RelationTypeMedia, RelationTypeAlias, RelationTypeName);
            relationType.IsBidirectional = true;

            LogHelper.Info<EventHandler>(String.Format("Creating RelationType '{0}' with alias '{1}'", RelationTypeName, RelationTypeAlias));

            relationService.Save(relationType);
        }

        // Content is published, find Media relations
        private void AddMediaUsage(IPublishingStrategy sender, PublishEventArgs<IContent> args)
        {
            // ContentService
            IContentService cs = ApplicationContext.Current.Services.ContentService;

            // RelationService
            IRelationService rs = ApplicationContext.Current.Services.RelationService;

            // RelationType
            IRelationType relationType = rs.GetRelationTypeByAlias("relateMediaToContent");

            // Published Documents
            foreach (var contentNode in args.PublishedEntities)
            {
                // LogHelper.Info<EventHandler>(String.Format("NodeId {0} published...", contentNode.Id));

                // Find Media node ids for this Content node
                List<int> mediaNodeIds = FindMedia(contentNode.Id);

                // Remove current relations
                RemoveAllMediaRelationsForContent(contentNode.Id);

                // Relate found Media to this Content
                foreach (var mediaNodeId in mediaNodeIds)
                {
                    Relation relation = new Relation(mediaNodeId, contentNode.Id, relationType);
                    LogHelper.Info<EventHandler>(String.Format("Saving relation with ParentId {0} and ChildId {1}", relation.ParentId, relation.ChildId));
                    rs.Save(relation);
                }
            }
        }

        // Find Media relations for all published Content nodes
        private void AddMediaUsageForAllContent()
        {
            // ContentService
            IContentService cs = ApplicationContext.Current.Services.ContentService;

            // RelationService
            IRelationService rs = ApplicationContext.Current.Services.RelationService;

            // RelationType
            IRelationType relationType = rs.GetRelationTypeByAlias("relateMediaToContent");

            // Published Documents
            foreach (var contentNodeId in FindAllContent())
            {
                // Find Media node ids for this Content node
                List<int> mediaNodeIds = FindMedia(contentNodeId);

                // Remove current relations
                RemoveAllMediaRelationsForContent(contentNodeId);

                // Relate found Media to this Content
                foreach (var mediaNodeId in mediaNodeIds)
                {
                    Relation relation = new Relation(mediaNodeId, contentNodeId, relationType);
                    LogHelper.Info<EventHandler>(String.Format("Saving relation with ParentId {0} and ChildId {1}", relation.ParentId, relation.ChildId));
                    rs.Save(relation);
                }
            }
        }
        // Content is unpublished, remove all Media relations
        private void RemoveMediaUsage(IPublishingStrategy sender, PublishEventArgs<IContent> args)
        {
            foreach (var contentNode in args.PublishedEntities)
            {
                RemoveAllMediaRelationsForContent(contentNode.Id);
            }
        }

        // Media is deleted, remove all Content relations
        private void RemoveMediaUsage(IContentService sender, DeleteEventArgs<IContent> args)
        {
            foreach (var mediaNode in args.DeletedEntities)
            {
                RemoveAllContentRelationsForMedia(mediaNode.Id);
            }
        }

        // Remove all Media relations for a Content node
        private void RemoveAllMediaRelationsForContent(int contentNodeId)
        {
            List<IRelation> relations = new List<IRelation>();

            // RelationService
            IRelationService rs = ApplicationContext.Current.Services.RelationService;

            // Content is child, query by child id
            foreach (var relation in rs.GetByChildId(contentNodeId))
            {
                LogHelper.Info<EventHandler>(String.Format("Deleting relation with ParentId {0} and ChildId {1}", relation.ParentId, relation.ChildId));
                rs.Delete(relation);
            }
        }

        // Remove all Content relations for a Media node
        private void RemoveAllContentRelationsForMedia(int mediaNodeId)
        {
            List<IRelation> relations = new List<IRelation>();

            // RelationService
            IRelationService rs = ApplicationContext.Current.Services.RelationService;

            // Content is child, query by child id
            foreach (var relation in rs.GetByParentId(mediaNodeId))
            {
                LogHelper.Info<EventHandler>(String.Format("Deleting relation with ParentId {0} and ChildId {1}", relation.ParentId, relation.ChildId));
                rs.Delete(relation);
            }
        }

        // Find Media nodes for a Content node
        private List<int> FindMedia(int contentNodeId)
        {
            List<int> mediaNodeIds = new List<int>();

            // Default Data Type ids (TODO: make this dynamic)
            // string propertyTypesList = "-87,1035,1045";
            string propertyTypesList = "-87,1035,1045,2100,2120";

            // DataTypeService
            // IDataTypeService ds  = ApplicationContext.Current.Services.DataTypeService;

            LogHelper.Info<EventHandler>(String.Format("Searching Content with id '{0}' for Media...", contentNodeId));

            try
            {
                // Connect to the Umbraco DB
                using (var db = ApplicationContext.Current.DatabaseContext.Database)
                {
                    // Get Properties for this Content node
                    var nodes = db.Query<ContentPropertiesResult>("select pd.contentNodeId,d.text as nodeName,pt.Name as propertyName,isnull(cast(pd.dataInt as nvarchar(100)),'') + ',' + isnull(pd.dataNvarchar,'') + ',' + isnull(cast(pd.dataNtext as nvarchar(max)),'') + ',' as dataCombined from cmsPropertyData pd, cmsdocument d, cmsPropertyType pt where pd.contentNodeId=d.nodeId and pd.propertytypeid=pt.id and pd.versionId=d.versionId and d.published=1 and pd.contentNodeId=@0 and pd.propertytypeid in (select id from cmsPropertyType where datatypeid in (" + propertyTypesList + "))", contentNodeId);

                    foreach (var node in nodes)
                    {
                        // Discover Media in the combined PropertyData string
                        List<int> foundNodes = Parser.GetMediaNodesFromString(node.dataCombined);

                        foreach (var item in foundNodes)
                        {
                            mediaNodeIds.Add(item);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.Error<EventHandler>(e.Message, e);
            }

            // Return distinct values
            List<int> distinctMediaNodeIds = mediaNodeIds.Distinct().ToList();

            LogHelper.Info<EventHandler>(String.Format("Found Media: {0}", String.Join(",", distinctMediaNodeIds.ToArray())));

            return distinctMediaNodeIds;
        }

        /// <summary>
        /// Finds all published Content
        /// </summary>
        /// <returns>List of node ids</returns>
        private List<int> FindAllContent()
        {
            List<int> contentNodeIds = new List<int>();

            LogHelper.Info<EventHandler>(String.Format("Searching for all published Content..."));

            try
            {
                // Connect to the Umbraco DB
                using (var db = ApplicationContext.Current.DatabaseContext.Database)
                {
                    // Get Properties for this Content node
                    var nodes = db.Query<ContentResult>("select d.nodeId as Id, d.text as Name from cmsDocument d where d.published=1");

                    foreach (var node in nodes)
                    {
                        contentNodeIds.Add(node.Id);
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.Error<EventHandler>(e.Message, e);
            }

            LogHelper.Info<EventHandler>(String.Format("Found Content: {0}", String.Join(",", contentNodeIds.ToArray())));

            return contentNodeIds;
        }
    }
}