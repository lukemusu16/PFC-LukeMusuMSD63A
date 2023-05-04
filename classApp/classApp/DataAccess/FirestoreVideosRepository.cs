using classApp.Models;
using Google.Api.Gax.Rest;
using Google.Cloud.Firestore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Security.Claims;
using System.IO;
using NReco.VideoConverter;
using System.Drawing;
using Microsoft.Extensions.Logging;
using classApp.Controllers;
using static System.Net.Mime.MediaTypeNames;
using Xabe.FFmpeg;

namespace classApp.DataAccess
{
    public class FirestoreVideosRepository
    {
        FirestoreDb db;

        string userId;
        public FirestoreVideosRepository(string project)
        {
            db = FirestoreDb.Create(project);
        }

        public async void AddVideo(string userId, Video v, Stream fileStream)
        {
            DocumentReference docRef = db.Collection("Users").Document(userId).Collection("Videos").Document(v.Id);
            await docRef.SetAsync(v);
        }

        public async void AddDowload(string userId, string vidId, Download d)
        { 
            DocumentReference docRef = db.Collection("Users")
                                         .Document(userId)
                                         .Collection("Videos")
                                         .Document(vidId)
                                         .Collection("Downloads").Document(d.Id);

            await docRef.SetAsync(d);

        }

        public async Task<List<Video>> GetVideos(string userId)
        {
            List<Video> videos = new List<Video>();


            Query allVidsQuery = db.Collection("Users").Document(userId).Collection("Videos");
            QuerySnapshot allVidsQuerySnapshot = await allVidsQuery.GetSnapshotAsync();

            foreach (DocumentSnapshot docSnap in allVidsQuerySnapshot.Documents)
            {
                Video video = docSnap.ConvertTo<Video>();
                videos.Add(video);
            }

            return videos;
        }

        public async Task<Video> GetVideo(string userId, string id)
        {
            List<Video> videos = new List<Video>();

            Query allVidsQuery = db.Collection("Users").Document(userId).Collection("Videos").WhereEqualTo("Id", id);
            QuerySnapshot allVidsQuerySnapshot = await allVidsQuery.GetSnapshotAsync();

            foreach (DocumentSnapshot docSnap in allVidsQuerySnapshot.Documents)
            {
                Video video = docSnap.ConvertTo<Video>();
                videos.Add(video);
            }

            return videos.FirstOrDefault();
        }

        public async Task DeleteVideo(string userId, string vidId)
        {
            List<Video> videos = new List<Video>();

            Query allVidsQuery = db.Collection("Users").Document(userId).Collection("Videos").WhereEqualTo("Id", vidId);
            QuerySnapshot allVidsQuerySnapshot = await allVidsQuery.GetSnapshotAsync();

            string id = allVidsQuerySnapshot.Documents[0].Id;

            DocumentReference docRef = db.Collection("Users").Document(userId).Collection("Videos").Document(id);
            await docRef.DeleteAsync();
        }

        public string GetThumbnailBase64(Stream imageStream)
        {
            System.Drawing.Image image = System.Drawing.Image.FromStream(imageStream);

            MemoryStream m = new MemoryStream();
            image.Save(m, image.RawFormat);
            byte[] bytes = m.ToArray();

            string base64String = Convert.ToBase64String(bytes);

            m.Dispose();

            return base64String;
        }
    }
}
