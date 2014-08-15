using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.MediaContentUsage.Models
{
    public class ContentPropertiesResult
    {
        public int contentNodeId { get; set; }
        public int propertyTypeId { get; set; }
        public string nodeName { get; set; }
        public string propertyName { get; set; }
        public string dataCombined { get; set; }
    }
}