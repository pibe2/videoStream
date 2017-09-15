using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VideoStreamingAspMvc.Models;

namespace VideoStreamingAspMvc.Controllers
{
    public class VideosController : Controller
    {

        private ApplicationDbContext _dbContext;

        public VideosController()
        {
            _dbContext = new ApplicationDbContext();
        }

        // GET: Videos
        public ActionResult Index()
        {
            List<Video> videos = _dbContext.Videos.ToList();
            return View(videos);
        }

        public ActionResult New()
        {
            return View();
        }

        [HttpPost]
        public ActionResult SubmitNew(Video video)
        {
            _dbContext.Videos.Add(video);
            _dbContext.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id) {
            var video = _dbContext.Videos.SingleOrDefault(v => v.id == id); // linq-library db query

            if (video == null)
                return HttpNotFound(String.Format("Video with id {0} not found", id));  // video doesn't exist in db

            return View(video);
        }

        [HttpPost]
        public ActionResult SubmitEdit(Video editInfo)
        {
            Video videoInDb = _dbContext.Videos.SingleOrDefault(v => v.id == editInfo.id);  // linq-library db query

            if (videoInDb == null)
                return HttpNotFound(String.Format("Video with id {0} not found", editInfo.id));  // video doesn't exist in db

            videoInDb.title = editInfo.title;
            videoInDb.description = editInfo.description;
            videoInDb.genre = editInfo.genre;
            _dbContext.SaveChanges();

            return RedirectToAction("Index");
        }

        public ActionResult ConfirmDelete(int id)
        {
            Video videoInDb = _dbContext.Videos.SingleOrDefault(v => v.id == id);  // linq-library db query

            if (videoInDb == null)
                return HttpNotFound(String.Format("Video with id {0} not found", id));  // video doesn't exist in db

            return View(videoInDb);
        }

        [HttpPost]
        public ActionResult SubmitDelete(int id)
        {
            Video videoInDb = _dbContext.Videos.SingleOrDefault(v => v.id == id);  // linq-library db query

            if (videoInDb == null)
                return HttpNotFound(String.Format("Video with id {0} not found", id));  // video doesn't exist in db

            _dbContext.Videos.Remove(videoInDb);
            _dbContext.SaveChanges();

            return RedirectToAction("Index");
        }

    }
}