using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FaceIdentificationConsole.Helpers
{
    static class FaceIdentificationHelper
    {
        private static string SubscriptionKey = "Your Face API Key goes here";
        private static string SubscriptionRegion = "Your Face API Endpint Goes Here";
        private static FaceServiceClient _faceServiceClient = new FaceServiceClient(SubscriptionKey, SubscriptionRegion);

        public static async Task CreatePersonGroupIfNotExistsAsync(string personGroupId, string personGroupName)
        {
            try
            {
                var listOfPersonGroups = await _faceServiceClient.ListPersonGroupsAsync();
                
                if (listOfPersonGroups.Length == 0)
                {
                    await _faceServiceClient.CreateLargePersonGroupAsync(personGroupId, personGroupName);
                }
            }
            catch(Exception e)
            {
                Debug.WriteLine("" + e);
                throw;
            }
        }

        public static async Task<CreatePersonResult> AddEmptyPersonInGroupAsync(string personName)
        {
            try
            {
                var personGroups = await _faceServiceClient.ListPersonGroupsAsync();
                var personGroup = personGroups[0];
                // Define Anna
                return await _faceServiceClient.CreatePersonInLargePersonGroupAsync(
                    // Id of the PersonGroup that the person belonged to
                    personGroup.PersonGroupId,
                    // Name of the person
                    personName
                );
            }
            catch (Exception e)
            {
                Debug.WriteLine("" + e);
                throw;
            }
           
        }

        public static async Task AddFaceToPersonAsync(Guid personId, string imageUrl)
        {
            try
            {
                var personGroups = await _faceServiceClient.ListLargePersonGroupsAsync();
                var personGroup = personGroups[0];
                await _faceServiceClient.AddPersonFaceInLargePersonGroupAsync(personGroup.LargePersonGroupId, personId, imageUrl);
            }
            catch (Exception e)
            {
                Debug.WriteLine("" + e);
                throw;
            }
        }

        public static async Task TrainPersonGroupAsync()
        {
            try
            {
                var personGroups = await _faceServiceClient.ListLargePersonGroupsAsync();
                var personGroup = personGroups[0];
                await _faceServiceClient.TrainLargePersonGroupAsync(personGroup.LargePersonGroupId);
            }
            catch (Exception e)
            {
                Debug.WriteLine("" + e);
                throw;
            }
        }

        public static async Task<Person> DetectPersonAsync(string imageUrl)
        {
            var faces = await _faceServiceClient.DetectAsync(imageUrl);
            var faceIds = faces.Select(face => face.FaceId).ToArray();
            var personGroupList = await _faceServiceClient.ListLargePersonGroupsAsync();
            for (int x=0 ; x <= personGroupList.Length ; x++)
            {
                var results = await _faceServiceClient.IdentifyAsync(personGroupList[x].LargePersonGroupId, faceIds);
                foreach (var identifyResult in results)
                {
                    //Console.WriteLine("Result of face: {0}", identifyResult.FaceId);
                    if (identifyResult.Candidates.Length == 0)
                    {
                        return null;
                    }
                    else
                    {
                        // Get top 1 among all candidates returned
                        var candidateId = identifyResult.Candidates[0].PersonId;
                        return await _faceServiceClient.GetPersonAsync(personGroupList[x].LargePersonGroupId, candidateId);
                    }
                }
            }
            return null;
        }

        public static async Task<Person> DetectPersonFromImageStreamAsync(Stream stream)
        {
            var faces = await _faceServiceClient.DetectAsync(stream);
            var faceIds = faces.Select(face => face.FaceId).ToArray();
            var personGroupList = await _faceServiceClient.ListPersonGroupsAsync();
            for (int x = 0; x <= personGroupList.Length; x++)
            {
                var results = await _faceServiceClient.IdentifyAsync(personGroupList[x].PersonGroupId, faceIds);
                foreach (var identifyResult in results)
                {
                    Console.WriteLine("Result of face: {0}", identifyResult.FaceId);
                    if (identifyResult.Candidates.Length == 0)
                    {
                        return null;
                    }
                    else
                    {
                        // Get top 1 among all candidates returned
                        var candidateId = identifyResult.Candidates[0].PersonId;
                        return await _faceServiceClient.GetPersonAsync(personGroupList[x].PersonGroupId, candidateId);
                    }
                }
            }

            return null;
        }

        public static async Task<TrainingStatus> GetTrainingStatus()
        {
            var personGroups = await _faceServiceClient.ListLargePersonGroupsAsync();
            var personGroup = personGroups[0];
            return await _faceServiceClient.GetLargePersonGroupTrainingStatusAsync(personGroup.LargePersonGroupId);
        }


    }
}
