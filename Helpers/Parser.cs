using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Web;
using Umbraco.Core.Logging;

namespace Chalmers.MediaContentUsage.Helpers
{
    public class Parser
    {
        // Check input DataTypes to search for, return default or sanitize user input
        // Richtext Editor (-87), Media Picker (1035), Multiple Media Picker (1045)
        public static List<int> GetMediaNodesFromString(string property)
        {
            List<int> parsedTokens = new List<int>();
            int result;

            string[] tokens = property.Split(',');

            LogHelper.Info<Parser>(String.Format("Incoming propertyString: {0}", property));

            // Find integers (possible Media node ids)
            foreach (var item in tokens)
            {
                if (Int32.TryParse(item, out result))
                {
                    // TODO: Make sure it's a Media node before adding
                    parsedTokens.Add(result);
                }
            }

            // Look for TinyMCE properties
            if (property.Contains("rel=\""))
            {
                LogHelper.Info<Parser>(String.Format("Property contains rel attribute"));

                foreach (Match m in Regex.Matches(property, @"rel=.(\d+)"))
                {
                    /* LogHelper.Info<Parser>("Match: " + m.Value);
                    LogHelper.Info<Parser>("Edited: " + m.Value.Substring(5, m.Value.Length - 5)); */

                    if (Int32.TryParse(m.Value.Substring(5, m.Value.Length - 5), out result))
                    {
                        // TODO: Make sure it's a Media node before adding
                        parsedTokens.Add(result);
                    }
                }
            }

            // Return distinct values
            List<int> distinctTokens = parsedTokens.Distinct().ToList();

            return distinctTokens;
        }
    }
}