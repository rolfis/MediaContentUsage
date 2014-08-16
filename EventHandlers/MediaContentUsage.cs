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
using Chalmers.MediaContentUsage;

namespace Chalmers.MediaContentUsage
{
    public class EventHandler : ApplicationEventHandler
    {
        /// <summary>
        /// Binds to different events in Umbraco to handle relationship mappings
        /// </summary>
        public EventHandler()
        {
            ContentService.Published += AddMediaUsage;
            ContentService.UnPublished += RemoveMediaUsage;
            ContentService.Deleted += RemoveMediaUsage;

            /* this service or event is a lie, 7.1.4 code don't seem to trigger the events */
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

            // Create RelationType if it's missing
            if (relationService.GetRelationTypeByAlias(Constants.RelationTypeAlias) == null)
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
                LogHelper.Debug<EventHandler>(String.Format("item: {0}", item.Alias));

                if (item.Alias == Constants.RelationTypeAlias)
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

            // Create new RelationType
            var relationType = new RelationType(Constants.RelationTypeDocument, Constants.RelationTypeMedia, Constants.RelationTypeAlias, Constants.RelationTypeName);
            relationType.IsBidirectional = true;
            relationService.Save(relationType);

            LogHelper.Info<EventHandler>(String.Format("Created RelationType '{0}' with alias '{1}'", Constants.RelationTypeName, Constants.RelationTypeAlias));
        }

        /// <summary>
        /// Adds relation between Content and Media when Content is published
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void AddMediaUsage(IPublishingStrategy sender, PublishEventArgs<IContent> args)
        {
            // RelationService
            IRelationService rs = ApplicationContext.Current.Services.RelationService;

            // RelationType
            IRelationType relationType = rs.GetRelationTypeByAlias(Constants.RelationTypeAlias);

            // Published Documents
            foreach (var contentNode in args.PublishedEntities)
            {
                // Remove current relations
                RemoveAllMediaRelationsForContent(contentNode.Id);

                // Relate found Media to this Content
                foreach (var mediaNodeId in FindMedia(contentNode.Id))
                {
                    Relation relation = new Relation(mediaNodeId, contentNode.Id, relationType);
                    rs.Save(relation);

                    LogHelper.Debug<EventHandler>(String.Format("Saved relation: ParentId {0} ChildId {1}", relation.ParentId, relation.ChildId));
                }
            }
        }

        /// <summary>
        /// Finds all published Content for this site and adds relations to Media
        /// </summary>
        private void AddMediaUsageForAllContent()
        {
            // RelationService
            IRelationService rs = ApplicationContext.Current.Services.RelationService;

            // RelationType
            IRelationType relationType = rs.GetRelationTypeByAlias(Constants.RelationTypeAlias);

            // Remove existing relations
            if (rs.HasRelations(relationType))
            {
                rs.DeleteRelationsOfType(relationType);
            }

            // Relate Media in all Published Documents
            foreach (var contentNodeId in FindAllContent())
            {
                foreach (var mediaNodeId in FindMedia(contentNodeId))
                {
                    Relation relation = new Relation(mediaNodeId, contentNodeId, relationType);
                    rs.Save(relation);

                    LogHelper.Debug<EventHandler>(String.Format("Saved relation: ParentId {0} ChildId {1}", relation.ParentId, relation.ChildId));
                }
            }
        }

        /// <summary>
        /// Removes relations to Media when Content is unpublished
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void RemoveMediaUsage(IPublishingStrategy sender, PublishEventArgs<IContent> args)
        {
            foreach (var contentNode in args.PublishedEntities)
            {
                RemoveAllMediaRelationsForContent(contentNode.Id);
            }
        }

        /// <summary>
        /// Removes relations to Content when Media is deleted
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void RemoveMediaUsage(IContentService sender, DeleteEventArgs<IContent> args)
        {
            foreach (var mediaNode in args.DeletedEntities)
            {
                RemoveAllContentRelationsForMedia(mediaNode.Id);
            }
        }

        /// <summary>
        /// Removes all Media relations for a Content node
        /// </summary>
        /// <param name="contentNodeId"></param>
        private void RemoveAllMediaRelationsForContent(int contentNodeId)
        {
            // RelationService
            IRelationService rs = ApplicationContext.Current.Services.RelationService;

            // Content is child, query by child id
            foreach (var relation in rs.GetByChildId(contentNodeId))
            {
                rs.Delete(relation);

                LogHelper.Debug<EventHandler>(String.Format("Deleted relation: ParentId {0} ChildId {1}", relation.ParentId, relation.ChildId));
            }
        }

        /// <summary>
        /// Removes all Content relations for a Media node
        /// </summary>
        /// <param name="mediaNodeId"></param>
        private void RemoveAllContentRelationsForMedia(int mediaNodeId)
        {
            // RelationService
            IRelationService rs = ApplicationContext.Current.Services.RelationService;

            // Content is child, query by child id
            foreach (var relation in rs.GetByParentId(mediaNodeId))
            {
                rs.Delete(relation);

                LogHelper.Debug<EventHandler>(String.Format("Deleted relation: ParentId {0} ChildId {1}", relation.ParentId, relation.ChildId));
            }
        }

        /// <summary>
        /// Finds Media node ids for a Content node
        /// </summary>
        /// <param name="contentNodeId"></param>
        /// <returns></returns>
        private List<int> FindMedia(int contentNodeId)
        {
            // Default Data Type ids (TODO: make this dynamic)
            // string propertyTypesList = "-87,1035,1045";
            string propertyTypesList = "-87,1035,1045,2100,2120";

            // List of combined Property Data (should be only one)
            List<string> combinedPropertyData = new List<string>();

            // List of all found Media node ids
            List<int> mediaNodeIds = new List<int>();

            try
            {
                // Connect to the Umbraco DB
                using (var db = ApplicationContext.Current.DatabaseContext.Database)
                {
                    // Combine the Content Property Data into a comma separated string
                    foreach (var node in db.Query<ContentPropertiesResult>("select pd.contentNodeId,d.text as nodeName,pt.Name as propertyName,isnull(cast(pd.dataInt as nvarchar(100)),'') + ',' + isnull(pd.dataNvarchar,'') + ',' + isnull(cast(pd.dataNtext as nvarchar(max)),'') + ',' as dataCombined from cmsPropertyData pd, cmsdocument d, cmsPropertyType pt where pd.contentNodeId=d.nodeId and pd.propertytypeid=pt.id and pd.versionId=d.versionId and d.published=1 and pd.contentNodeId=@0 and pd.propertytypeid in (select id from cmsPropertyType where datatypeid in (" + propertyTypesList + "))", contentNodeId))
                    {
                        combinedPropertyData.Add(node.dataCombined);
                    }
                }

                // Discover Media in the combined PropertyData
                foreach (var item in combinedPropertyData)
                {
                    foreach (var mediaNodeId in Parser.GetMediaNodesFromString(item))
                    {
                        mediaNodeIds.Add(mediaNodeId);
                    }

                }
            }
            catch (Exception e)
            {
                LogHelper.Error<EventHandler>(e.Message, e);
            }

            if (mediaNodeIds.Count > 0)
            {
                LogHelper.Info<EventHandler>(String.Format("Media found for Content with id '{0}': {1}", contentNodeId, String.Join(",", mediaNodeIds.Distinct().ToArray())));
            }

            // Return distinct values
            return mediaNodeIds.Distinct().ToList();
        }

        /// <summary>
        /// Finds all published Content
        /// </summary>
        /// <returns>List of node ids</returns>
        private List<int> FindAllContent()
        {
            // List of all found Content node ids
            List<int> contentNodeIds = new List<int>();

            LogHelper.Info<EventHandler>(String.Format("Searching for all published Content..."));

            try
            {
                // Connect to the Umbraco DB
                using (var db = ApplicationContext.Current.DatabaseContext.Database)
                {
                    // Find all published Content
                    foreach (var node in db.Query<ContentResult>("select d.nodeId as Id, d.text as Name from cmsDocument d where d.published=1"))
                    {
                        contentNodeIds.Add(node.Id);
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.Error<EventHandler>(e.Message, e);
            }

            LogHelper.Info<EventHandler>(String.Format("Content found: {0}", String.Join(",", contentNodeIds.ToArray())));

            return contentNodeIds;
        }
    }
}