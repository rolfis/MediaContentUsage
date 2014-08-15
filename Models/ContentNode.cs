using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.MediaContentUsage.Models
{
    public class ContentNode
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Property { get; set; }
        public string Data { get; set; }
    }
}