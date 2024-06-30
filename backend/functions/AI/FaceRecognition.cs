using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Extensions.Logging;

namespace functions.AI
{
    public class FaceRecognition(ILogger logger, IFaceClient client)
    {
        public const string DefaultPersonGroupId = "f250b46c-4f01-41be-8300-82b596311b36";

        // From your Face subscription in the Azure portal, get your subscription key and endpoint.
        readonly ILogger _logger = logger;
        readonly IFaceClient _client=client;


        // Detect faces from image url for recognition purposes. This is a helper method for other functions in this quickstart.
        // Parameter `returnFaceId` of `DetectWithUrlAsync` must be set to `true` (by default) for recognition purposes.
        // Parameter `FaceAttributes` is set to include the QualityForRecognition attribute. 
        // Recognition model must be set to recognition_03 or recognition_04 as a result.
        // Result faces with insufficient quality for recognition are filtered out. 
        // The field `faceId` in returned `DetectedFace`s will be used in Face - Face - Verify and Face - Identify.
        // It will expire 24 hours after the detection call.
        private async Task<List<DetectedFace>> DetectFaceRecognize(Stream stream, string recognition_model)
        {
            // Detect faces from image URL. Since only recognizing, use the recognition model 1.
            // We use detection model 3 because we are not retrieving attributes.
            IList<DetectedFace> detectedFaces = await _client.Face.DetectWithStreamAsync(stream, recognitionModel: recognition_model, detectionModel: DetectionModel.Detection03, returnFaceAttributes: [FaceAttributeType.QualityForRecognition]);
            List<DetectedFace> sufficientQualityFaces = [];
            foreach (DetectedFace detectedFace in detectedFaces){
                var faceQualityForRecognition = detectedFace.FaceAttributes.QualityForRecognition;
                if (faceQualityForRecognition.HasValue && (faceQualityForRecognition.Value >= QualityForRecognition.Medium)){
                    sufficientQualityFaces.Add(detectedFace);
                }
            }
            _logger.LogInformation("{count} face(s) with {countQuality} having sufficient quality for recognition detected from image.",detectedFaces.Count,sufficientQualityFaces.Count);

            return sufficientQualityFaces;
        }

        /*
         * IDENTIFY FACES
         * To identify faces, you need to create and define a person group.
         * The Identify operation takes one or several face IDs from DetectedFace or PersistedFace and a PersonGroup and returns 
         * a list of Person objects that each face might belong to. Returned Person objects are wrapped as Candidate objects, 
         * which have a prediction confidence value.
         */
        public async Task<IReadOnlyList<string>> IdentifyInPersonGroup(Stream stream, string recognitionModel)
        {
            Console.WriteLine("========IDENTIFY FACES========");
            Console.WriteLine();

            // // Create a dictionary for all your images, grouping similar ones under the same key.
            // Dictionary<string, string[]> personDictionary =
            //     new Dictionary<string, string[]>
            //         { { "Family1-Dad", new[] { "Family1-Dad1.jpg", "Family1-Dad2.jpg" } },
            //           { "Family1-Mom", new[] { "Family1-Mom1.jpg", "Family1-Mom2.jpg" } },
            //           { "Family1-Son", new[] { "Family1-Son1.jpg", "Family1-Son2.jpg" } },
            //           { "Family1-Daughter", new[] { "Family1-Daughter1.jpg", "Family1-Daughter2.jpg" } },
            //           { "Family2-Lady", new[] { "Family2-Lady1.jpg", "Family2-Lady2.jpg" } },
            //           { "Family2-Man", new[] { "Family2-Man1.jpg", "Family2-Man2.jpg" } }
            //         };
            // // A group photo that includes some of the persons you seek to identify from your dictionary.
            // string sourceImageFileName = "identification1.jpg";

            // // Create a person group. 
            // Console.WriteLine($"Create a person group ({personGroupId}).");
            // await _client.PersonGroup.CreateAsync(personGroupId, personGroupId, recognitionModel: recognitionModel);
            // // The similar faces will be grouped into a single person group person.
            // foreach (var groupedFace in personDictionary.Keys)
            // {
            //     // Limit TPS
            //     await Task.Delay(250);
            //     Person person = await _client.PersonGroupPerson.CreateAsync(personGroupId: personGroupId, name: groupedFace);
            //     Console.WriteLine($"Create a person group person '{groupedFace}'.");

            //     // Add face to the person group person.
            //     foreach (var similarImage in personDictionary[groupedFace])
            //     {
            //         Console.WriteLine($"Check whether image is of sufficient quality for recognition");
            //         IList<DetectedFace> detectedFaces1 = await _client.Face.DetectWithUrlAsync($"{url}{similarImage}", 
            //             recognitionModel: recognitionModel, 
            //             detectionModel: DetectionModel.Detection03,
            //             returnFaceAttributes: new List<FaceAttributeType> { FaceAttributeType.QualityForRecognition });
            //         bool sufficientQuality = true;
            //         foreach (var face1 in detectedFaces1)
            //         {
            //             var faceQualityForRecognition = face1.FaceAttributes.QualityForRecognition;
            //             //  Only "high" quality images are recommended for person enrollment
            //             if (faceQualityForRecognition.HasValue && (faceQualityForRecognition.Value != QualityForRecognition.High)){
            //                 sufficientQuality = false;
            //                 break;
            //             }
            //         }

            //         if (!sufficientQuality){
            //             continue;
            //         }

            //         // add face to the person group
            //         Console.WriteLine($"Add face to the person group person({groupedFace}) from image `{similarImage}`");
            //         PersistedFace face = await _client.PersonGroupPerson.AddFaceFromUrlAsync(personGroupId, person.PersonId,
            //             $"{url}{similarImage}", similarImage);
            //     }
            // }

            // // Start to train the person group.
            // Console.WriteLine();
            // Console.WriteLine($"Train person group {personGroupId}.");
            // await _client.PersonGroup.TrainAsync(personGroupId);

            // // Wait until the training is completed.
            // while (true)
            // {
            //     await Task.Delay(1000);
            //     var trainingStatus = await _client.PersonGroup.GetTrainingStatusAsync(personGroupId);
            //     Console.WriteLine($"Training status: {trainingStatus.Status}.");
            //     if (trainingStatus.Status == TrainingStatusType.Succeeded) { break; }
            // }
            // Console.WriteLine();

            // Detect faces from source image url.
            List<DetectedFace> detectedFaces = await DetectFaceRecognize(stream, recognitionModel);
            List<Guid> sourceFaceIds = detectedFaces.Where(face => face.FaceId != null).Select(face => face.FaceId!.Value).ToList();



            
            // Identify the faces in a person group. 
            var identifyResults = await _client.Face.IdentifyAsync(sourceFaceIds, DefaultPersonGroupId);
            List<string> personNames = [];
            foreach (var identifyResult in identifyResults)
            {
                if (identifyResult.Candidates.Count==0) {
                    _logger.LogInformation("No person is identified for the face in: {FaceId}, ",identifyResult.FaceId);
                    continue;
                }
                Person person = await _client.PersonGroupPerson.GetAsync(DefaultPersonGroupId, identifyResult.Candidates[0].PersonId);
                _logger.LogInformation("Person '{name}' is identified for the face in: {FaceId}, confidence: {confidence}.", person.Name, identifyResult.FaceId, identifyResult.Candidates[0].Confidence);

                VerifyResult verifyResult = await _client.Face.VerifyFaceToPersonAsync(identifyResult.FaceId, person.PersonId, DefaultPersonGroupId);
                _logger.LogInformation("Verification result: is a match? {isIdentical}. confidence: {confidence}",verifyResult.IsIdentical,verifyResult.Confidence);
                if(verifyResult.IsIdentical)
                {
                    personNames.Add(person.Name);
                }
            }
            return personNames;
        }
    }
}