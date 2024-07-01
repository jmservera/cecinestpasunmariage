using Microsoft.Azure.CognitiveServices.Vision.Face;
using dotenv.net;
using functions.AI;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using System.Text.RegularExpressions;

DotEnv.Load(options: new DotEnvOptions(envFilePaths: ["../../../www/.env"], ignoreExceptions: false));

var key = Environment.GetEnvironmentVariable("VISION_KEY");
var endpoint = Environment.GetEnvironmentVariable("VISION_ENDPOINT");
var client = new FaceClient(new ApiKeyServiceClientCredentials(key)) { Endpoint = endpoint };

var baseDirectory = "/cecidata/";
// Create a dictionary for all your images, grouping similar ones under the same key.
var personDictionary = new Dictionary<string, string[]>
    { { "Isa", [ "isa_1.jpg", "isa_2.jpg", "isa_3.jpg", "isa_4.jpg" ] },
            { "Juanma",  ["juanma_1.jpg","juanma_2.jpg" ] },
            { "Luc", ["Luc_1.jpg","Luc_2.jpg","Luc_3.jpg"] },
            { "Bet", ["bet_1.jpg", "bet_2.jpg", "bet_3.jpg", "bet_4.jpg", "bet_5.png" ]}
            //,{ "Perla",  ["perla_1.jpg", "perla_2.jpg" ] }
    };

var personGroupId = FaceRecognition.DefaultPersonGroupId;
var recognitionModel = Microsoft.Azure.CognitiveServices.Vision.Face.Models.RecognitionModel.Recognition04;

var group = await client.PersonGroup.GetAsync(personGroupId);
if (group!=null)
{
    Console.WriteLine($"Delete person group {personGroupId}.");
    await client.PersonGroup.DeleteAsync(personGroupId);
}

Console.WriteLine($"Create a person group ({personGroupId}).");
await client.PersonGroup.CreateAsync(personGroupId, personGroupId, recognitionModel: recognitionModel);

// The similar faces will be grouped into a single person group person.
foreach (var groupedFace in personDictionary.Keys)
{
    // Limit TPS
    await Task.Delay(250);
    Person person = await client.PersonGroupPerson.CreateAsync(personGroupId: personGroupId, name: groupedFace);
    Console.WriteLine($"Create a person group person '{groupedFace}'.");

    // Add face to the person group person.
    foreach (var similarImage in personDictionary[groupedFace])
    {
        Console.WriteLine($"Check whether image is of sufficient quality for recognition");
        using var stream = new FileStream(Path.Combine(baseDirectory, similarImage), FileMode.Open);
        IList<DetectedFace> detectedFaces1 = await client.Face.DetectWithStreamAsync(stream,
            recognitionModel: recognitionModel,
            detectionModel: DetectionModel.Detection03,
            returnFaceAttributes: new List<FaceAttributeType> { FaceAttributeType.QualityForRecognition });
        bool sufficientQuality = true;
        foreach (var face1 in detectedFaces1)
        {
            var faceQualityForRecognition = face1.FaceAttributes.QualityForRecognition;
            //  Only "high" quality images are recommended for person enrollment
            if (faceQualityForRecognition.HasValue && (faceQualityForRecognition.Value != QualityForRecognition.High))
            {
                sufficientQuality = false;
                break;
            }
        }

        if (!sufficientQuality)
        {
            continue;
        }

        // add face to the person group
        Console.WriteLine($"Add face to the person group person({groupedFace}) from image `{similarImage}`");
        
        using var faceStream = new FileStream(Path.Combine(baseDirectory, similarImage), FileMode.Open);
        PersistedFace face = await client.PersonGroupPerson.AddFaceFromStreamAsync(personGroupId, person.PersonId,
            faceStream, similarImage);
    }
}

// Start to train the person group.
Console.WriteLine();
Console.WriteLine($"Train person group {personGroupId}.");
await client.PersonGroup.TrainAsync(personGroupId);

// Wait until the training is completed.
while (true)
{
    await Task.Delay(1000);
    var trainingStatus = await client.PersonGroup.GetTrainingStatusAsync(personGroupId);
    Console.WriteLine($"Training status: {trainingStatus.Status}.");
    if (trainingStatus.Status == TrainingStatusType.Succeeded) { break; }
}
Console.WriteLine();
