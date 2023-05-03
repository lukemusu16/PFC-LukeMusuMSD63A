using Google.Cloud.Firestore;

namespace classApp.Models
{
    [FirestoreData]
    public class Video
    {
        [FirestoreProperty]
        public string Id { get; set; }

        [FirestoreProperty]
        public string Name { get; set; }

        [FirestoreProperty]
        public string Link { get; set; }

        [FirestoreProperty]
        public string flacLink { get; set; }

        [FirestoreProperty]
        public string Owner { get; set; }

        [FirestoreProperty]
        public string Thumbnail { get; set; }

        [FirestoreProperty]
        public Google.Cloud.Firestore.Timestamp DateUploaded { get; set; }

        public System.DateTime DateUploadedDT
        {
            get
            {
                return DateUploaded.ToDateTime();
            }
            set
            {
                DateUploaded = Google.Cloud.Firestore.Timestamp.FromDateTime(value.ToUniversalTime());
            }
        }
    }
}
