using System.Text.Json;

namespace functions.Test;
public class MessageDeserializationTest
{
    static readonly string message = "{\"update_id\":736683371,\"message\":{ \"message_id\":129,\"from\":{ \"id\":511116634,\"is_bot\":false,\"first_name\":\"Juan Manuel\",\"last_name\":\"Servera Bondroit\",\"username\":\"jmservera\",\"language_code\":\"en\"},\"chat\":{ \"id\":511116634,\"first_name\":\"Juan Manuel\",\"last_name\":\"Servera Bondroit\",\"username\":\"jmservera\",\"type\":\"private\"},\"date\":1728993776,\"text\":\"hi\"}}";

    [Fact]
    public void DeserializeMessageWithNewstonsoftTest()
    {
        var update = Newtonsoft.Json.JsonConvert.DeserializeObject<Telegram.Bot.Types.Update>(message);
        Assert.NotNull(update);
        Assert.NotNull(update.Message);
        Assert.Equal(736683371, update.Id);
        Assert.Equal(129, update.Message.MessageId);
    }

}