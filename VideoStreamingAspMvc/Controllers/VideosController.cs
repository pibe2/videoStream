using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VideoStreamingAspMvc.Models;
using System.IO;
using System.Diagnostics;

namespace VideoStreamingAspMvc.Controllers
{
    public class VideoUploadControl
    {
        public bool isMergeControl { get; set; }
        public string fileName { get; set; }
        public int numChunks { get; set; }
        public int videoId { get; set; }
    }

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

            return RedirectToAction("UploadVideo", new { id = video.Id });
        }


        public ActionResult UploadVideo(int id)
        {
            Video video = _dbContext.Videos.SingleOrDefault(v => v.Id == id); // linq-library db query

            if (video == null)
                return HttpNotFound(String.Format("Video with id {0} not found", id));  // video doesn't exist in db

            return View(video);
        }

        [HttpPost]
        public JsonResult UploadControl(VideoUploadControl fileInfo)
        {
            string chunksDirPath, mergeDirPath;
            getVideoDirPaths(fileInfo.fileName, fileInfo.videoId, out chunksDirPath, out mergeDirPath);

            if (fileInfo.isMergeControl) {  // all file chunks uploaded; time to merge and process
                string mergedFilePath;
                if (mergeFileChunks(fileInfo, chunksDirPath, mergeDirPath, out mergedFilePath))
                {
                    processVideo(mergedFilePath, fileInfo);
                    return Json(String.Format("server: merge successful upload of file {0}, video {1}", fileInfo.fileName, fileInfo.videoId));
                }
                else
                    return Json(String.Format("server: error for merging upload of file {0}, video {1}", fileInfo.fileName, fileInfo.videoId));
            }
            else // file chunks about to be uploaded create upload directory
            {
                if (Directory.Exists(chunksDirPath))
                    Directory.Delete(chunksDirPath, true);  // TODO: do we really want this
                Directory.CreateDirectory(chunksDirPath);
                return Json(String.Format("server: start upload control message for upload of file {0}, video {1}", fileInfo.fileName, fileInfo.videoId));
            }
        }

        [HttpPost]
        public JsonResult UploadVideoChunkAjax()
        {
            HttpPostedFileBase chunk = Request.Files[0];

            string videoFileName;
            int id, chunkCount, chunkNo;
            if (!ExtractInfoFromFileChunkName(chunk.FileName, out videoFileName, out id, out chunkCount, out chunkNo))
                return Json(String.Format("file chunk has invalid name"));

            if (chunk == null || chunk.ContentLength == 0)
                return Json(String.Format("no file uploaded"));

            Video video = _dbContext.Videos.SingleOrDefault(v => v.Id == id);
            if (video == null)
                return Json(String.Format("Video with id {0} not found", id));

            string chunksDirPath, mergeDirPath;
            getVideoDirPaths(videoFileName, id, out chunksDirPath, out mergeDirPath);

            string chunkName = getChunkName(chunkNo, chunkCount);
            string chunkPath = Path.Combine(chunksDirPath, chunkName);
            chunk.SaveAs(chunkPath);
            return Json(String.Format("server: file {0} uploaded successfully for video {1}", chunkName, id));
        }

        private bool mergeFileChunks(VideoUploadControl info, string chunksDirPath, string mergeDirPath, out string mergedFilePath) {
            string[] chunkPaths = new string[info.numChunks];
            mergedFilePath = "";
            for (int i = 0; i < info.numChunks; ++i)
            {
                string chunkName = getChunkName(i + 1, info.numChunks);
                string chunkPath = Path.Combine(chunksDirPath, chunkName);
                if (!System.IO.File.Exists(chunkPath))
                    return false;
                chunkPaths[i] = chunkPath;
            }

            /*
            if (Directory.Exists(mergeDirPath))
                Directory.Delete(mergeDirPath, true);  // TODO: do we really want this
            Directory.CreateDirectory(mergeDirPath);
            */
            if (!Directory.Exists(mergeDirPath))
                Directory.CreateDirectory(mergeDirPath);

            mergedFilePath = Path.Combine(mergeDirPath, info.fileName);
            try
            {
                using (FileStream mergeStream = new FileStream(mergedFilePath, FileMode.Create))
                    for (int i = 0; i < info.numChunks; ++i)
                    {
                        using (FileStream chunkStream = new FileStream(chunkPaths[i], FileMode.Open))
                            chunkStream.CopyTo(mergeStream);
                    }
            }
            catch (IOException e)
            {
                throw e;
            }
            return true;
        }

        // to do this async?
        private void processVideo(string videoPath, VideoUploadControl info) {
            Video video = _dbContext.Videos.SingleOrDefault(v => v.Id == info.videoId); // linq-library db query
            if (video == null)
                return;

            string processedDirPath, processedDirUrl;
            getSourceUrlDir(info.fileName, info.videoId, out processedDirPath, out processedDirUrl);
            string videoBaseName = Path.GetFileNameWithoutExtension(info.fileName);
            string fileExtension = ".mp4";
            string processedVideoPath = Path.Combine(processedDirPath, videoBaseName + fileExtension);

            if (System.IO.File.Exists(processedVideoPath))
            {
                System.IO.File.Delete(processedVideoPath);
            }

            // TODO video processing with ffmpeg (extract thumbnail) async
            try
            {
                Directory.CreateDirectory(processedDirPath);
                Process proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        WorkingDirectory = processedDirPath,
                        FileName = "C:\\ffmpeg\\bin\\ffmpeg.exe",
                        Arguments = string.Format("-i {0} -c:v libx264 {1}", videoPath, processedVideoPath),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                    }
                };
                proc.Start();
                proc.BeginErrorReadLine();
                proc.BeginOutputReadLine();

                proc.OutputDataReceived += new DataReceivedEventHandler(ffmpegStdOut);
                proc.ErrorDataReceived += new DataReceivedEventHandler(ffmpegStdErr);

                proc.WaitForExit();
                /*
                while (!proc.StandardOutput.EndOfStream || !proc.StandardError.EndOfStream)
                {
                    if (!proc.StandardOutput.EndOfStream)
                    {
                        string line = proc.StandardOutput.ReadToEnd();
                        Console.WriteLine("stdout: " + line);
                        Debug.WriteLine(line);
                    }
                    if (!proc.StandardError.EndOfStream)
                    {
                        string line = proc.StandardError.ReadToEnd();
                        Console.WriteLine("stderr: " + line);
                        Debug.WriteLine(line);
                    }
                }

                // TODO: Check that path.combine does what we want here
                video.VideoFileName = Path.Combine(processedDirUrl, "vid.mp4");
                video.ImageFileName = Path.Combine(processedDirUrl, "img.jpg");
                video.Length = 20;  // TODO
                _dbContext.SaveChanges();
                */
            }
            catch (Exception e) {
                Debug.WriteLine("```````````````````````Exception Caught````````````````````````````````\r\n{0}", e);
            }
        }

        private void ffmpegStdOut(object sendingProcess, DataReceivedEventArgs outLine)
        {
            Debug.WriteLine("aaa");
        }

        private void ffmpegStdErr(object sendingProcess, DataReceivedEventArgs outLine)
        {
            Debug.WriteLine("aaa");
        }

        private void getVideoDirPaths(string fileName, int videoId, out string chunksDirPath , out string mergeDirPath)
        {
            string baseFileName = Path.GetFileName(fileName);

            string storagePath = Server.MapPath("~/Storage/" + videoId + "_" + baseFileName);
            chunksDirPath = Path.Combine(storagePath, "chunks");
            mergeDirPath = Path.Combine(storagePath, "merge");
        }

        private void getSourceUrlDir(string fileName, int videoId, out string processedDirPath, out string processedDirUrl) {
            string baseFileName = Path.GetFileName(fileName);
            processedDirPath = Server.MapPath("~/Storage/" + videoId + "_" + baseFileName + "/processed/");
            processedDirUrl = "/Storage/" + videoId + "_" + baseFileName + "/processed/";
        }

        private string getChunkName(int chunkNo, int chunkCount)
        {
            return chunkNo + "-" + chunkCount; 
        }

        private bool ExtractInfoFromFileChunkName(string chunkName, out string fileName, out int videoId, out int chunkCount, out int chunkNo) {
            chunkNo = chunkCount = videoId = -1;
            fileName = "";

            char[] delimiter = { '-' };
            string[] chunkNameTokens = chunkName.Split(delimiter, 4);

            if (chunkNameTokens.Count() < 4)
                return false;

            if (!int.TryParse(chunkNameTokens[0], out videoId))
                return false;
            if (!int.TryParse(chunkNameTokens[1], out chunkCount))
                return false;
            if (!int.TryParse(chunkNameTokens[2], out chunkNo))
                return false;

            fileName = chunkNameTokens[3];
            return true;
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

            return RedirectToAction("UploadVideo", new { id = editInfo.Id });
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