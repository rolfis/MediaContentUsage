using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Logging;

namespace Chalmers.Helpers
{
    public class Parser
    {
        /// <summary>
        /// Parses a string for Media node ids, based on comma delimited Property Data
        /// </summary>
        /// <param name="property"></param>
        /// <returns>List of Media node ids</returns>
        public static List<int> GetMediaNodesFromString(string property)
        {
            List<int> parsedTokens = new List<int>();

            int result;
            Udi udiResult;

            string[] tokens = property.Split(',');

            LogHelper.Debug<Parser>(String.Format("Incoming propertyString: {0}", property));
            LogHelper.Debug<Parser>(String.Format("Tokens to TryParse(): {0}", tokens.Length));

            // Find integers (possible Media node ids)
            foreach (var item in tokens)
            {
                // Try and parse as integer
                if (Int32.TryParse(item, out result))
                {
                    // Media for real
                    if (IsMedia(result))
                    {
                        parsedTokens.Add(result);
                    }
                }
            }

            // Look for TinyMCE properties
            if (property.Contains("rel=\""))
            {
                LogHelper.Debug<Parser>(String.Format("Property contains rel attribute"));

                foreach (Match m in Regex.Matches(property, @"rel=.(\d+)"))
                {
                    // Try and parse as integer
                    if (Int32.TryParse(m.Value.Substring(5, m.Value.Length - 5), out result))
                    {
                        // Media for real
                        if (IsMedia(result))
                        {
                            parsedTokens.Add(result);
                        }
                    }
                }
            }

            // data-udi="umb://media/1aeaf66774a34256b8e44b1edec1bb32"
            // Look for UDI properties
            if (property.Contains("data-udi=\"umb://media"))
            {
                foreach (Match m in Regex.Matches(property, "data-udi=\"(?<Identifier>[^\"]+)"))
                {
                    LogHelper.Info<Parser>(String.Format("Match in data-udi attribute: {0}", m.Groups["Identifier"].Value));

                    // Try and parse as UDI
                    if (GuidUdi.TryParse(m.Groups["Identifier"].Value, out GuidUdi udi))
                    {
                        // Media for real
                        if (IsMedia(udi))
                        {
                            parsedTokens.Add(MediaId(udi));
                        }
                    }
                }
            }

            // Return distinct values
            return parsedTokens.Distinct().ToList();
        }

        /// <summary>
        /// Checks if a Node id is Media
        /// </summary>
        /// <param name="mediaNodeId"></param>
        /// <returns>True if Media, otherwise false</returns>
        public static bool IsMedia(int mediaNodeId)
        {
            // MediaService
            var ms = ApplicationContext.Current.Services.MediaService;

            // Load Media
            IMedia mediaItem = ms.GetById(mediaNodeId);

            if (mediaItem == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Checks if a Node id is Media
        /// </summary>
        /// <param name="identifier">GuidUdi</param>
        /// <returns>True if Media, otherwise false</returns>
        public static bool IsMedia(GuidUdi identifier)
        {
            // MediaService
            var ms = ApplicationContext.Current.Services.MediaService;

            // Load Media
            IMedia mediaItem = ms.GetById(identifier.Guid);

            if (mediaItem == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Get Media integer id from a GuidUdi
        /// </summary>
        /// <param name="identifier">GuidUdi</param>
        /// <returns>Media id</returns>
        public static int MediaId(GuidUdi identifier)
        {
            // MediaService
            var ms = ApplicationContext.Current.Services.MediaService;

            // Load Media
            IMedia mediaItem = ms.GetById(identifier.Guid);

            return mediaItem.Id;
        }
    }
}