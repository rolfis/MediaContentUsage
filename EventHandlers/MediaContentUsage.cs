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
using Chalmers.Models;
using Chalmers.Helpers;
using Chalmers;

namespace Chalmers
{
    public class MediaContentUsage : ApplicationEventHandler
    {
        /// <summary>
        /// Binds to different events in Umbraco to handle relationship mappings
        /// </summary>
        public MediaContentUsage()
        {
            // Watch the Published event, if UnPublished we don't care as Published or Trashed on the
            // Content node will be used instead when reading from the RelationService in the API Controller.
            // When Content or Media Recycle bin is emptied, Umbraco takes care of the relations cleanup.
            ContentService.Published += ContentService_Published;

            // This event should trigger re-generation of RelationType and re-index, but event seems to not fire
            RelationService.DeletedRelationType += RelationService_DeletedRelationType;
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
            LogHelper.Info<MediaContentUsage>(String.Format("RelationService_DeletedRelationType"));

            foreach (var item in e.DeletedEntities)
            {
                LogHelper.Debug<MediaContentUsage>(String.Format("item: {0}", item.Alias));

                if (item.Alias == Constants.RelationTypeAlias)
                {
                    CreateRelationType();
                    AddMediaUsageForAllContent();
                }
            }
        }

        /// <summary>
        /// Creates the RelationType needed in Umbraco to store Media-Content relations
        /// </summary>
        private void CreateRelationType()
        {
            // RelationService
            IRelationService rs = ApplicationContext.Current.Services.RelationService;

            // Create new RelationType
            var relationType = new RelationType(Constants.RelationTypeDocument, Constants.RelationTypeMedia, Constants.RelationTypeAlias, Constants.RelationTypeName);
            relationType.IsBidirectional = true;
            rs.Save(relationType);

            LogHelper.Info<MediaContentUsage>(String.Format("Created RelationType '{0}' with alias '{1}'", Constants.RelationTypeName, Constants.RelationTypeAlias));
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

            // Remove all existing relations
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

                    LogHelper.Debug<MediaContentUsage>(String.Format("Saved relation: ParentId {0} ChildId {1}", relation.ParentId, relation.ChildId));
                }
            }
        }

        /// <summary>
        /// Adds relation between Content and Media when Content is published
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ContentService_Published(IPublishingStrategy sender, PublishEventArgs<IContent> e)
        {
            // RelationService
            IRelationService rs = ApplicationContext.Current.Services.RelationService;

            // ContentService
            IContentService cs = ApplicationContext.Current.Services.ContentService;

            // RelationType
            IRelationType relationType = rs.GetRelationTypeByAlias(Constants.RelationTypeAlias);

            // Published Documents
            foreach (var contentNode in e.PublishedEntities)
            {
                // Content is child, query by child and RelationType
                var relations = rs.GetByChild(cs.GetById(contentNode.Id), Constants.RelationTypeAlias);

                // Remove current relations
                if (relations.Count() > 0)
                {
                    LogHelper.Info<MediaContentUsage>(String.Format("Removing all Media relations for published Content with id '{0}'", contentNode.Id));

                    foreach (var relation in relations)
                    {
                        rs.Delete(relation);

                        LogHelper.Debug<MediaContentUsage>(String.Format("Deleted relation: ParentId {0} ChildId {1}", relation.ParentId, relation.ChildId));
                    }
                }

                // Relate found Media to this Content
                foreach (var mediaNodeId in FindMedia(contentNode.Id))
                {
                    Relation relation = new Relation(mediaNodeId, contentNode.Id, relationType);
                    rs.Save(relation);

                    LogHelper.Debug<MediaContentUsage>(String.Format("Saved relation: ParentId {0} ChildId {1}", relation.ParentId, relation.ChildId));
                }
            }
        }

        /// <summary>
        /// Finds Media node ids for a Content node
        /// </summary>
        /// <param name="contentNodeId"></param>
        /// <returns></returns>
        private List<int> FindMedia(int contentNodeId)
        {
            // Property DataTypes to search for Media
            string configurationPropertyTypesList =  System.Configuration.ConfigurationManager.AppSettings[Constants.ConfigurationKeyDataTypes];
            string propertyTypesList = String.IsNullOrEmpty(configurationPropertyTypesList) ? Constants.DefaultDataTypes : configurationPropertyTypesList;

            LogHelper.Debug<MediaContentUsage>(String.Format("Property DataTypes: {0}", propertyTypesList));

            // List of combined Property Data (should be only one)
            List<string> combinedPropertyData = new List<string>();

            // List of all found Media node ids
            List<int> mediaNodeIds = new List<int>();

            try
            {
                // Connect to the Umbraco DB
                using (var db = ApplicationContext.Current.DatabaseContext.Database)
                {
                    // Combine the Property Data into a comma separated string from published Content node
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
                LogHelper.Error<MediaContentUsage>(e.Message, e);
            }

            if (mediaNodeIds.Count > 0)
            {
                LogHelper.Info<MediaContentUsage>(String.Format("Media found for Content with id '{0}': {1}", contentNodeId, String.Join(",", mediaNodeIds.Distinct().ToArray())));
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

            LogHelper.Info<MediaContentUsage>(String.Format("Searching for all published Content..."));

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
                LogHelper.Error<MediaContentUsage>(e.Message, e);
            }

            LogHelper.Info<MediaContentUsage>(String.Format("Content found: {0} nodes", contentNodeIds.Count()));

            return contentNodeIds;
        }
    }
}