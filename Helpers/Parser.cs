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

            string[] tokens = property.Split(',');

            LogHelper.Debug<Parser>(String.Format("Incoming propertyString: {0}", property));
            LogHelper.Debug<Parser>(String.Format("Tokens to TryParse(): {0}", tokens.Length));

            // Find integers (possible Media node ids)
            foreach (var item in tokens)
            {
                // Try and parse as integer
                if (Int32.TryParse(item, out int result))
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

                foreach (Match m in Regex.Matches(property, @"rel=.(?<Identifier>\d+)"))
                {
                    // Try and parse as integer
                    if (Int32.TryParse(m.Groups["Identifier"].Value, out int result))
                    {
                        // Media for real
                        if (IsMedia(result))
                        {
                            parsedTokens.Add(result);
                        }
                    }
                }
            }

            // Look for UDI properties
            if (property.Contains("umb://media"))
            {
                foreach (Match m in Regex.Matches(property, "umb://media/([^,|$|\"]+)"))
                {
                    LogHelper.Info<Parser>(String.Format("Match in data-udi attribute: {0}", m.Value));

                    // Try and parse as UDI
                    if (GuidUdi.TryParse(m.Value, out GuidUdi udi))
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