using Google.Cloud.Firestore;

namespace classApp.Models
{
    [FirestoreData]
    public class Download
    {
        [FirestoreProperty]
        public string Id { get; set; }

        [FirestoreProperty]
        public Google.Cloud.Firestore.Timestamp DateDonwloaded { get; set; }

        public System.DateTime DateDownloadedDT
        {
            get
            {
                return DateDonwloaded.ToDateTime();
            }
            set
            {
                DateDonwloaded = Google.Cloud.Firestore.Timestamp.FromDateTime(value.ToUniversalTime());
            }
        }
    }
}
