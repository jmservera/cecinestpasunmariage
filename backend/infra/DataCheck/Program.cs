using Microsoft.Azure.CognitiveServices.Vision.Face;
using dotenv.net;
using functions.AI;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using System.Text.RegularExpressions;

static async Task<List<DetectedFace>> DetectFaceRecognize(IFaceClient faceClient, Stream stream, string recognition_model)
{
    // Detect faces from image URL. Since only recognizing, use the recognition model 1.
    // We use detection model 3 because we are not retrieving attributes.
    IList<DetectedFace> detectedFaces = await faceClient.Face.DetectWithStreamAsync(stream, recognitionModel: recognition_model, detectionModel: DetectionModel.Detection03, returnFaceAttributes: new List<FaceAttributeType> { FaceAttributeType.QualityForRecognition });
    List<DetectedFace> sufficientQualityFaces = new List<DetectedFace>();
    foreach (DetectedFace detectedFace in detectedFaces)
    {
        var faceQualityForRecognition = detectedFace.FaceAttributes.QualityForRecognition;
        if (faceQualityForRecognition.HasValue && (faceQualityForRecognition.Value >= QualityForRecognition.Medium))
        {
            sufficientQualityFaces.Add(detectedFace);
        }
    }
    Console.WriteLine($"{detectedFaces.Count} face(s) with {sufficientQualityFaces.Count} having sufficient quality for recognition detected from image");

    return sufficientQualityFaces;
}

DotEnv.Load(options: new DotEnvOptions(envFilePaths: ["../../../www/.env"], ignoreExceptions: false));

var key = Environment.GetEnvironmentVariable("VISION_KEY");
var endpoint = Environment.GetEnvironmentVariable("VISION_ENDPOINT");
var client = new FaceClient(new ApiKeyServiceClientCredentials(key)) { Endpoint = endpoint };

var baseDirectory = "/cecidata/";

List<string> images = ["BetJuanma_Crowd.jpg","IsaJuanma.jpg","JuanmaOvi.jpg",
"LucBetCrowd.jpg","LucBet.jpg","LucBet2.jpg","LucBet3.jpg","group_with_Luc.jpg","IsaPerla.jpg","LucBetEdurne.jpg"];

var personGroupId = FaceRecognition.DefaultPersonGroupId;
var recognitionModel = Microsoft.Azure.CognitiveServices.Vision.Face.Models.RecognitionModel.Recognition04;

foreach (var sourceImageFileName in images)
{
    Console.WriteLine($"Check whether image is of sufficient quality for recognition");
    using var stream = new FileStream(Path.Combine(baseDirectory, sourceImageFileName), FileMode.Open);

    List<Guid> sourceFaceIds = new List<Guid>();
    List<DetectedFace> detectedFaces = await DetectFaceRecognize(client, stream, recognitionModel);

    // Add detected faceId to sourceFaceIds.
    foreach (var detectedFace in detectedFaces) { sourceFaceIds.Add(detectedFace.FaceId.Value); }

    // Identify the faces in a person group. 
    var identifyResults = await client.Face.IdentifyAsync(sourceFaceIds, personGroupId);

    foreach (var identifyResult in identifyResults)
    {
        if (identifyResult.Candidates.Count == 0)
        {
            Console.WriteLine($"No person is identified for the face in: {sourceImageFileName} - {identifyResult.FaceId},");
            continue;
        }
        Person person = await client.PersonGroupPerson.GetAsync(personGroupId, identifyResult.Candidates[0].PersonId);
        Console.WriteLine($"Person '{person.Name}' is identified for the face in: {sourceImageFileName} - {identifyResult.FaceId}," +
            $" confidence: {identifyResult.Candidates[0].Confidence}.");

        VerifyResult verifyResult = await client.Face.VerifyFaceToPersonAsync(identifyResult.FaceId, person.PersonId, personGroupId);
        Console.WriteLine($"Verification result: is a match? {verifyResult.IsIdentical}. confidence: {verifyResult.Confidence}");
    }
    Console.WriteLine();
}









