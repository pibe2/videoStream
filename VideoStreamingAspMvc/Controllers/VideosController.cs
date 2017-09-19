﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VideoStreamingAspMvc.Models;
using System.IO;
using System.Diagnostics;

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
            HttpPostedFileBase videoFile = Request.Files[0];

            if (videoFile == null || videoFile.ContentLength <= 0)
                // TODO: HANDLE BETTER
                return HttpNotFound("no file uploaded");


            // TODO video processing with ffmpeg (extract thumbnail)

            string videoFileName = Path.GetFileName(videoFile.FileName);
            string videoFilePath = Path.Combine(Server.MapPath("~/Storage/"), videoFileName);
            videoFile.SaveAs(videoFilePath);

            try
            {
                Process proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "C:/ffmpeg/bin/ffmpeg.exe",
                        Arguments = "-help",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                proc.Start();
                Debug.WriteLine("---------------------success----------------------");
                while (!proc.StandardOutput.EndOfStream)
                {
                    string line = proc.StandardOutput.ReadLine();
                    Console.WriteLine(line);
                    Debug.WriteLine(line);
                }
            }
            catch (Exception e) {
                Debug.WriteLine("```````````````````````Exception Caught````````````````````````````````\r\n{0}", e);
            }

            video.VideoFileName = videoFileName;
            video.ImageFileName = Path.GetFileNameWithoutExtension(videoFileName) + ".jpg";
            video.Length = 20;

            _dbContext.Videos.Add(video);
            _dbContext.SaveChanges();

            return RedirectToAction("Index");
        }


        public JsonResult UploadVideo()
        {
            return Json("server: file uploaded successfully");
        }

        public ActionResult Edit(int id) {
            var video = _dbContext.Videos.SingleOrDefault(v => v.Id == id); // linq-library db query

            if (video == null)
                return HttpNotFound(String.Format("Video with id {0} not found", id));  // video doesn't exist in db

            return View(video);
        }

        [HttpPost]
        public ActionResult SubmitEdit(Video editInfo)
        {
            Video videoInDb = _dbContext.Videos.SingleOrDefault(v => v.Id == editInfo.Id);  // linq-library db query

            if (videoInDb == null)
                return HttpNotFound(String.Format("Video with id {0} not found", editInfo.Id));  // video doesn't exist in db

            videoInDb.Title = editInfo.Title;
            videoInDb.Description = editInfo.Description;
            videoInDb.Genre = editInfo.Genre;
            _dbContext.SaveChanges();

            return RedirectToAction("Index");
        }

        public ActionResult ConfirmDelete(int id)
        {
            Video videoInDb = _dbContext.Videos.SingleOrDefault(v => v.Id == id);  // linq-library db query

            if (videoInDb == null)
                return HttpNotFound(String.Format("Video with id {0} not found", id));  // video doesn't exist in db

            return View(videoInDb);
        }

        [HttpPost]
        public ActionResult SubmitDelete(int id)
        {
            Video videoInDb = _dbContext.Videos.SingleOrDefault(v => v.Id == id);  // linq-library db query

            if (videoInDb == null)
                return HttpNotFound(String.Format("Video with id {0} not found", id));  // video doesn't exist in db

            _dbContext.Videos.Remove(videoInDb);
            _dbContext.SaveChanges();

            return RedirectToAction("Index");
        }

        /** TODO: code to open saved file
        public ActionResult ShowVideos(Video editInfo)
        {
            FileStream SavedFile = new FileStream(Server.MapPath("~/Storage/" + editInfo.FilePath), FileMode.Open);
            return View(editInfo);

        }
        **/

    }
}