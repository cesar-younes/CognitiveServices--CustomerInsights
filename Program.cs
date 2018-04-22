using AForge.Video;
using AForge.Video.DirectShow;
using FaceIdentificationConsole.Helpers;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace FaceIdentificationConsole
{
    class Program
    {
        static VideoCaptureDevice videoSource;

        static void Main(string[] args)
        {
            while(true)
            {
                Console.WriteLine("Get Ready!!!!!!!!!");
                Task.Delay(1000);
                Console.WriteLine("Taking Snapshot!");
                //For this part I'm using the AForge library but this can be changed for other libraries or custom code
                TakeSnapshot();

                Console.ReadKey();
            }
        }

        static void TakeSnapshot()
        {
            //List all available video sources. (That can be webcams as well as tv cards, etc)
            FilterInfoCollection videosources = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            //Check if atleast one video source is available
            if (videosources != null)
            {
                //For example use first video device. You may check if this is your webcam.
                videoSource = new VideoCaptureDevice(videosources[0].MonikerString);

                try
                {
                    //Check if the video device provides a list of supported resolutions
                    if (videoSource.VideoCapabilities.Length > 0)
                    {
                        //The video capabilities are all the possible resolutions the cam can take. I chose one that gives us an image less than 4MB in size
                        videoSource.VideoResolution = videoSource.VideoCapabilities[16];
                    }
                }
                catch { }

                //Create NewFrame event handler
                //(This one triggers every time a new frame/image is captured
                videoSource.NewFrame += new AForge.Video.NewFrameEventHandler(videoSource_NewFrame);

                //Start recording
                videoSource.Start();
            }
        }

        static async void videoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            //Cast the frame as Bitmap object and don't forget to use ".Clone()" otherwise
            //you'll probably get access violation exceptions
            var image = (Bitmap)eventArgs.Frame.Clone();

            //Stop grabbing more frames as we've already caught one to process
            videoSource.SignalToStop();

            string fileName = Guid.NewGuid().ToString() + ".jpg";
            string file = @"C:\IPCamRecord\LCW\" + fileName;
            image.Save(file);

            //Keeping a copy of the image on the cloud because it's easier to work with. IT can be kept on the pc instead
            string imageUrl = await UploadToBlob(file, fileName);

            //Try to see if we've captured this person before
            var person = await FaceIdentificationHelper.DetectPersonAsync(imageUrl);

            if (person != null)
            {
                //If the person has been captured before then we can print their name
                Console.WriteLine("This is " + person.Name);
            }
            else
            {
                //Person not captured before so let's add them to the database
                string s = Guid.NewGuid().ToString();

                //Add person to PersonGroup
                var result = await FaceIdentificationHelper.AddEmptyPersonInGroupAsync(s);

                //Add the captured face to this person
                await FaceIdentificationHelper.AddFaceToPersonAsync(result.PersonId, imageUrl);

                //Train FaceAPI to recognize the person 
                await FaceIdentificationHelper.TrainPersonGroupAsync();

                //Wait while training is completed
                TrainingStatus trainingStatus = null;
                while (true)
                {
                    trainingStatus = await FaceIdentificationHelper.GetTrainingStatus();
                    Console.WriteLine("Training status: " + trainingStatus.Status);
                    if (trainingStatus.Status == Status.Running)
                    {
                        continue;
                    }
                    else if (trainingStatus.Status == Status.Succeeded)
                    {
                        break;
                    }

                    Task.Delay(1000).Wait();
                }
            }
        }

        private static async Task<string> UploadToBlob(string file, string fileName)
        {
            // Create a CloudStorageAccount instance pointing to your storage account.
            CloudStorageAccount storageAccount =
              CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=cognitiveanalyticsstore;AccountKey=1408JVEih7I1aPFTYiilistMYQd7U0okAKPu/buJbXoeCHrfMnzH+E5hY1YwEBCrGb38/4+qw8Qmx/SHujOgjQ==;EndpointSuffix=core.windows.net");

            // Create the CloudBlobClient that is used to call the Blob Service for that storage account.
            CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();

            // Create a container called 'quickstartblobs'. 
            CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference("images");

            // Upload the file 
            CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(fileName);
            cloudBlockBlob.Properties.ContentType = "image/jpg";

            await cloudBlockBlob.UploadFromFileAsync(file);

            return cloudBlockBlob.StorageUri.PrimaryUri.ToString();
        }
    }
}
