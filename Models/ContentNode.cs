using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Chalmers.Models
{
    public class ContentNode
    {
        public int Id { get; set; }
        public bool Published { get; set; }
        public bool Trashed { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string PathName { get; set; }
        public string Comment { get; set; }
        public string State { get; set; }
    }
}