import os
import flask
import moviepy.editor
from google.cloud import speech, storage, pubsub_v1
import functions_framework
import tempfile
import base64
import datetime
import pysrt

import firebase_admin
from firebase_admin import credentials
from firebase_admin import firestore

firebase_admin.initialize_app()


@functions_framework.http
def convert_video(context):
    subscriber = pubsub_v1.SubscriberClient()
    subscription_path = subscriber.subscription_path("cloudcomputingclass-377007", "transcriptions-sub")

    def callback(message: pubsub_v1.subscriber.message.Message) -> None:
        link = message.attributes.get("Link")
        owner = message.attributes.get("Owner")
        vidId = message.attributes.get("VidID")
        tempdir = tempfile.gettempdir()
        print(link, owner, vidId, tempdir)
        link = link.split("/")[4]
        print(link)
        linkNoExt = link[:-4]
        print(linkNoExt)

        storageClient = storage.Client()

        bucket = storageClient.bucket("classapp")

        blob = bucket.blob(link)
        blob.download_to_filename(tempdir + "/" + link)
        print("Downloaded storage object to local file")

        uploadBlob = linkNoExt + "_audio.flac"
        video = moviepy.editor.VideoFileClip(tempdir + "/" + link)
        audio = video.audio
        audio.write_audiofile(tempdir + "/" + uploadBlob, codec='flac')
        video.close()
        print("Converted vid to audio")

        blob = bucket.blob(uploadBlob)
        blob.upload_from_filename(tempdir + "/" + uploadBlob)
        print("Uploaded the audio file")

        db = firestore.client()

        gs_uri = "gs://classapp/"
        flacLink = gs_uri + uploadBlob

        doc_ref = db.collection("Users").document(owner).collection("Videos").document(vidId)
        doc_ref.update({u'flacLink': flacLink})

        blobLink = uploadBlob
        blobNoExt = blobLink[:-5]

        speechClient = speech.SpeechClient()

        audio = speech.RecognitionAudio(uri=flacLink)
        config = speech.RecognitionConfig(
            encoding=speech.RecognitionConfig.AudioEncoding.FLAC,
            sample_rate_hertz=44100,
            language_code="en-US",
            audio_channel_count=2
        )

        response = speechClient.recognize(config=config, audio=audio)

        with open(tempdir + "/" + blobNoExt + ".srt", 'w') as file:
            for i, result in enumerate(response.results):
                file.write(f"{i + 1}\n")

                start_time = None
                end_time = None
                for word in result.alternatives[0].words:
                    if start_time is None or word.start_time < start_time:
                        start_time = word.start_time

                    if end_time is None or word.end_time > end_time:
                        end_time = word.end_time

                file.write(f"{start_time} --> {end_time}\n")
                file.write(f"{result.alternatives[0].transcript}\n\n")

        file.close()

        blob = bucket.blob(blobNoExt + ".srt")
        blob.upload_from_filename(tempdir + "/" + blobNoExt + ".srt")

        print("SRT file generated and uploaded")

        blob = bucket.blob(blobNoExt + ".flac")
        blob.delete()

        os.remove(tempdir + "/" + blobNoExt + ".flac")
        os.remove(tempdir + "/" + link)
        os.remove(tempdir + "/" + blobNoExt + ".srt")

        doc_ref = db.collection("Users").document(owner).collection("Videos").document(vidId)
        doc_ref.update({u'flacLink': u"https://storage.googleapis.com/classapp/" + blobNoExt + ".srt"})

        message.ack()

    streaming_pull_future = subscriber.subscribe(subscription_path, callback=callback)

    with subscriber:
        try:
            # When `timeout` is not set, result() will block indefinitely,
            # unless an exception is encountered first.
            streaming_pull_future.result(timeout=5)
        except TimeoutError:
            streaming_pull_future.cancel()  # Trigger the shutdown.
            streaming_pull_future.result()  # Block until the shutdown is complete.

    return "Process Done"


def timedelta_to_subrip_time(td):
    total_seconds = int(td.total_seconds())
    hours, remainder = divmod(total_seconds, 3600)
    minutes, seconds = divmod(remainder, 60)
    milliseconds = int(td.microseconds / 1000)
    return pysrt.SubRipTime(hours, minutes, seconds, milliseconds)
