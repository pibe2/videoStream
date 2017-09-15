using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VideoStreamingAspMvc.Models
{
    public class Video
    {
        public int id { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public Genre genre { get; set; }

    }

    public enum Genre
    {
        Comedy = 1,
        Horror,
        Scifi,
        Romance,
        Documentary,
        Action,
        Fantasy,
        Sport,
        Kids
    }
}