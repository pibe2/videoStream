using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VideoStreamingAspMvc.Models
{
    public class Video
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        public string VideoFileName { get; set; }
        public string ImageFileName { get; set; }
        public int Length { get; set; }         // in minutes

        public VideoGenre Genre { get; set; }
    }

    public enum VideoGenre
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