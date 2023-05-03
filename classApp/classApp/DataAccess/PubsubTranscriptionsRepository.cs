using classApp.Models;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Newtonsoft.Json;

namespace classApp.DataAccess
{
    public class PubsubTranscriptionsRepository
    {
        TopicName topicName;
        public PubsubTranscriptionsRepository(string project)
        {
            //get the queue to add messages to it
            topicName = TopicName.FromProjectTopic(project, "transcriptions");

            if (topicName == null)
            {
                var p = PublisherServiceApiClient.Create();
                var t = p.CreateTopic("transcriptions");
                topicName = t.TopicName;
            }
        }

        public async void PushTranscriptionJob(Video v)
        {
            PublisherClient publisher = await PublisherClient.CreateAsync(topicName);
            var video = JsonConvert.SerializeObject(v);

            var pubsubMessage = new PubsubMessage
            {
                Data = ByteString.CopyFromUtf8(video),

                Attributes =
                {
                    { "priority", "low"},
                    { "Link", v.Link },
                    { "Owner", v.Owner },
                    { "VidID", v.Id }
                }
            };
            string message = await publisher.PublishAsync(pubsubMessage);
        }
    }
}
