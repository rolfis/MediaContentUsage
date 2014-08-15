﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.MediaContentUsage.Models
{
    public class MediaContent
    {
        public int MediaNodeId { get; set; }
        public bool MediaIsValid { get; set; }
        public bool HasContentUsage { get; set; }
        public List<ContentNode> Content { get; set; }
    }
}