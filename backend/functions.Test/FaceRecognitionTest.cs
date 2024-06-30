using dotenv.net;
using functions.AI;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace functions.Test;

public class FaceRecognitionTest : IAsyncLifetime
{
    readonly IFaceClient _client;
    readonly Dictionary<string, string[]> _personDictionary;
    //initialize the tests
    public FaceRecognitionTest()
    {
        DotEnv.Load(options: new DotEnvOptions(envFilePaths: ["../../../../../www/.env"]));
        
        var key = Environment.GetEnvironmentVariable("VISION_KEY");
        var endpoint = Environment.GetEnvironmentVariable("VISION_ENDPOINT");
        _client = new FaceClient(new ApiKeyServiceClientCredentials(key)) { Endpoint = endpoint };


        // Create a dictionary for all your images, grouping similar ones under the same key.
        _personDictionary =
            new Dictionary<string, string[]>
                { { "Isa", new[] { "Family1-Dad1.jpg", "Family1-Dad2.jpg" } },
                      { "Juanma", new[] { "Family1-Mom1.jpg", "Family1-Mom2.jpg" } },
                      { "Luc", new[] { "Family1-Son1.jpg", "Family1-Son2.jpg" } },
                      { "Bet", new[] { "Family1-Daughter1.jpg", "Family1-Daughter2.jpg" } },
                      { "Perla", new[] { "Family2-Lady1.jpg", "Family2-Lady2.jpg" } }
                };
        // // A group photo that includes some of the persons you seek to identify from your dictionary.
        // string sourceImageFileName = "identification1.jpg";


    }

    //Generate the groups
    public async Task InitializeAsync()
    {
        // Create a person group. 
        var personGroupId = FaceRecognition.DefaultPersonGroupId;
        var recognitionModel = Microsoft.Azure.CognitiveServices.Vision.Face.Models.RecognitionModel.Recognition04;

        await _client.PersonGroup.CreateAsync(personGroupId, personGroupId, recognitionModel: recognitionModel);
        // The similar faces will be grouped into a single person group person.
        foreach (var groupedFace in _personDictionary.Keys)
        {
            // Limit TPS
            await Task.Delay(250);
            Person person = await _client.PersonGroupPerson.CreateAsync(personGroupId: personGroupId, name: groupedFace);
            Console.WriteLine($"Create a person group person '{groupedFace}'.");

            // Add face to the person group person.
            foreach (var similarImage in _personDictionary[groupedFace])
            {
                Console.WriteLine($"Check whether image is of sufficient quality for recognition");
                using var stream = new FileStream($"{similarImage}", FileMode.Open);
                IList<DetectedFace> detectedFaces1 = await _client.Face.DetectWithStreamAsync(stream,
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
                stream.Position = 0;
                PersistedFace face = await _client.PersonGroupPerson.AddFaceFromStreamAsync(personGroupId, person.PersonId,
                    stream, similarImage);
            }
        }

        // Start to train the person group.
        Console.WriteLine();
        Console.WriteLine($"Train person group {personGroupId}.");
        await _client.PersonGroup.TrainAsync(personGroupId);

        // Wait until the training is completed.
        while (true)
        {
            await Task.Delay(1000);
            var trainingStatus = await _client.PersonGroup.GetTrainingStatusAsync(personGroupId);
            Console.WriteLine($"Training status: {trainingStatus.Status}.");
            if (trainingStatus.Status == TrainingStatusType.Succeeded) { break; }
        }
        Console.WriteLine();

    }

    public Task DisposeAsync() => Task.CompletedTask;


    [Fact]
    public void Test1()
    {

    }
}