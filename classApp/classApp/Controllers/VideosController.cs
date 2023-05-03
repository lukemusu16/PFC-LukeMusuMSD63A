using classApp.DataAccess;
using classApp.Models;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using Google.Cloud.Firestore;
using System.IO;

namespace classApp.Controllers
{
    public class VideosController : Controller
    {
        FirestoreVideosRepository fvr;
        ILogger<VideosController> logger;
        PubsubTranscriptionsRepository pstr;

        public VideosController(FirestoreVideosRepository _fvr,
                                ILogger<VideosController> _logger,
                                PubsubTranscriptionsRepository _pstr)
        {
            fvr = _fvr;
            logger = _logger;
            pstr = _pstr;
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public IActionResult Create(Video v, IFormFile file)
        {
            try
            {
                //Will store the link to the ebook in the cloud storage
                if (file != null)
                {
                    
                    var storage = StorageClient.Create();
                    using Stream fileStream = file.OpenReadStream();
                    v.Thumbnail = fvr.GetThumbnailBase64(fileStream);

                    string newFilename = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(file.FileName);

                    logger.LogInformation($"File {file.FileName} has been renamed to {newFilename}");

                    storage.UploadObject("classapp", newFilename, null, fileStream);
                    v.Link = $"https://storage.googleapis.com/classapp/{newFilename}";
                    
                    logger.LogInformation($"File {newFilename} has been uploaded to {v.Link}");

                    v.Id = Guid.NewGuid().ToString();
                    v.Owner = GlobalValues.UserID;
                    v.DateUploaded = Timestamp.FromDateTime(DateTime.UtcNow);

                    // Will store book details to firestore
                    logger.LogInformation($"File {file.FileName} will be saved to db");
                    fvr.AddVideo(GlobalValues.UserID, v, fileStream);
                    logger.LogInformation($"File {file.FileName} has been saved in the database");
                    pstr.PushTranscriptionJob(v);
                    logger.LogInformation($"Video Transcription has been pushed to the queue");
                    TempData["success"] = "Book was added successfully";
                    return RedirectToAction("Index");
                }

            }
            catch (Exception e)
            {
                logger.LogInformation(e, $"File {file.FileName} was either not saved or not saved in the db");
                TempData["error"] = "Book was not added";
                return View();
            }

            return View();

        }

        public IActionResult Index()
        {
            var list = fvr.GetVideos(GlobalValues.UserID).Result;

            return View(list);
        }


        public async Task<IActionResult> Delete(string vidId)
        {
            try
            {
                var video = await fvr.GetVideo(GlobalValues.UserID, vidId);
                string link = video.Link;
                string flacLink = video.flacLink;

                var storage = StorageClient.Create();
                string objectNameLink = System.IO.Path.GetFileName(link);
                string objectNameFlacLink = System.IO.Path.GetFileName(flacLink);

                storage.DeleteObject("classapp", objectNameLink);
                storage.DeleteObject("classapp", objectNameFlacLink);



                await fvr.DeleteVideo(GlobalValues.UserID, vidId);
                TempData["success"] = "Book was deleted successfully";
            }
            catch (Exception e)
            {
                TempData["error"] = "Book was not deleted" + e;
            }


            return RedirectToAction("Index");
        }


        public async Task<IActionResult>DownloadVideo(string vidId)
        {
            try
            {
                var video = await fvr.GetVideo(GlobalValues.UserID, vidId);
                var downloadId = Guid.NewGuid().ToString();

                Download d = new Download();
                d.Id = downloadId;
                d.DateDonwloaded = Timestamp.FromDateTime(DateTime.UtcNow);

                fvr.AddDowload(GlobalValues.UserID, vidId, d);
                logger.LogInformation($"Downloaded added to firestore");

                return new RedirectResult(video.Link);
            }
            catch (Exception e)
            {
                logger.LogInformation(e, $"Downloaded not added to firestore");
                return View();
            }

            
        }

    }
}
