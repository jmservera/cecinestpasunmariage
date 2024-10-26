using System;
using System.Text;
using Xunit;
using functions.Identity;

namespace functions.Test
{
    public class ChatUserCipherTest
    {
        private const string chatId = "chat123";
        private const string userName = "user123";

        private const string EncryptionKey = "your-encryption-key-1234"; // Ensure this key is 16, 24, or 32 bytes long
        private const string fakeButCompliantIV = "uHYiCunR+dyJoBO9mSINsA==";
        private const string unrelatedEncryptedUser = "jEfVXe5cXdk8Hz7RPHh8kqAIlyo2x6a6UZxFaBYrbGz2cyQ=";

        private const string oldEncryptedUser = "Kv5iitlqbKi/3jtlfBwxuot8qw7ifdmI1/upvXuSxZAjg0Y=";
        private const string oldIV = "Z2YdNXA4qUWSH7kOWLDALA==";

        [Fact]
        public void Encrypt_ShouldEncryptUser()
        {
            var user = new ChatUser
            {
                ChatId = chatId,
                UserId = userName
            };

            var encryptedUser = ChatUser.TimeSeal(user, EncryptionKey);

            Assert.NotNull(encryptedUser.Hash);
            Assert.NotNull(encryptedUser.IV);
            Assert.NotEqual(user.ChatId, encryptedUser.Hash);
            Assert.NotEqual(user.UserId, encryptedUser.Hash);
            Assert.NotEqual(user.IV, encryptedUser.IV);
            Assert.NotEqual(user.Hash, encryptedUser.Hash);
        }

        [Fact]
        public void CheckValidity_ShouldThrowExceptionForInvalidDate()
        {
            var user = new ChatUser
            {
                ChatId = chatId,
                UserId = userName,
                Hash = unrelatedEncryptedUser,
                IV = fakeButCompliantIV
            };

            Assert.Throws<InvalidOperationException>(() => user.CheckSealValidity(EncryptionKey));
        }

        [Fact]
        public void CheckValidity_ShouldThrowExceptionForExpiredDate()
        {
            // Valid User but old date
            var encryptedUser = new ChatUser
            {
                ChatId = chatId,
                UserId = userName,
                Hash = oldEncryptedUser,
                IV = oldIV
            };

            Assert.Throws<InvalidOperationException>(() => encryptedUser.CheckSealValidity(EncryptionKey, -1));
        }

        [Fact]
        public void CheckValidity_ShouldPassForValidUser()
        {
            var user = new ChatUser
            {
                ChatId = chatId,
                UserId = userName
            };

            var encryptedUser = ChatUser.TimeSeal(user, EncryptionKey);

            Exception ex = Record.Exception(() => encryptedUser.CheckSealValidity(EncryptionKey));
            Assert.Null(ex);
        }
    }
}