using System.Text;
using NUnit.Framework;

namespace Osc2.Tests {
    public class OscAlignmentTests {
        static int AlignString(int contentBytes) => (contentBytes + 4) & ~3;
        static int AlignBlobData(int length) => (length + 3) & ~3;

        static int ReadOscInt32(byte[] data, int offset) {
            var u = new Encoder.Union32();
            u.Unpack(data, offset);
            return u.intdata;
        }

        static Message ParseSingle(byte[] data) {
            var parser = new Parser();
            parser.FeedData(data);
            Assert.That(parser.MessageCount, Is.EqualTo(1));
            return parser.PopMessage();
        }

        static int PathFieldSize(string path) => AlignString(Encoding.UTF8.GetByteCount(path));
        static int TypeTagFieldSize(int paramCount) => AlignString(paramCount + 2);

        [Test]
        public void Utf8Path_EncodeDecode_RoundTrip() {
            var path = "/日本/テスト";
            var msg = ParseSingle(new Encoder(path).Add(42).Encode());
            Assert.That(msg.path, Is.EqualTo(path));
            Assert.That(msg.data[0], Is.EqualTo(42));
        }

        [TestCase(0, 4)]
        [TestCase(1, 4)]
        [TestCase(2, 4)]
        [TestCase(3, 4)]
        [TestCase(4, 8)]
        [TestCase(5, 8)]
        public void StringArg_PaddingSize(int charLen, int expectedPaddedBytes) {
            var s = new string('x', charLen);
            var contentBytes = Encoding.UTF8.GetByteCount(s);
            Assert.That(AlignString(contentBytes), Is.EqualTo(expectedPaddedBytes));

            var pathLen = PathFieldSize("/t");
            var tagLen = TypeTagFieldSize(1);
            var data = new Encoder("/t").Add(s).Encode();
            var argOffset = pathLen + tagLen;

            if (charLen > 0)
                Assert.That(data[argOffset], Is.EqualTo((byte)'x'));
            Assert.That(data[argOffset + contentBytes], Is.EqualTo(0));
            for (var i = 1; i < expectedPaddedBytes - contentBytes; i++)
                Assert.That(data[argOffset + contentBytes + i], Is.EqualTo(0));
            Assert.That(data.Length, Is.EqualTo(pathLen + tagLen + expectedPaddedBytes));
        }

        [Test]
        public void Utf8StringArg_PaddingUsesByteCount() {
            var s = "日本";
            var contentBytes = Encoding.UTF8.GetByteCount(s);
            Assert.That(contentBytes, Is.EqualTo(6));
            Assert.That(AlignString(contentBytes), Is.EqualTo(8));

            var pathLen = PathFieldSize("/t");
            var tagLen = TypeTagFieldSize(1);
            var data = new Encoder("/t").Add(s).Encode();
            var argOffset = pathLen + tagLen;
            Assert.That(Encoding.UTF8.GetString(data, argOffset, contentBytes), Is.EqualTo(s));
            Assert.That(data[argOffset + contentBytes], Is.EqualTo(0));
        }

        [TestCase(0, 4)]
        [TestCase(1, 8)]
        [TestCase(3, 8)]
        [TestCase(4, 8)]
        [TestCase(5, 12)]
        public void BlobArg_PaddingSize(int blobLen, int expectedArgBytes) {
            var blob = new byte[blobLen];
            for (var i = 0; i < blobLen; i++)
                blob[i] = (byte)(i + 1);

            Assert.That(4 + AlignBlobData(blobLen), Is.EqualTo(expectedArgBytes));

            var pathLen = PathFieldSize("/t");
            var tagLen = TypeTagFieldSize(1);
            var data = new Encoder("/t").Add(blob).Encode();
            var argOffset = pathLen + tagLen;

            Assert.That(ReadOscInt32(data, argOffset), Is.EqualTo(blobLen));
            for (var i = 0; i < blobLen; i++)
                Assert.That(data[argOffset + 4 + i], Is.EqualTo(blob[i]));
            for (var i = blobLen; i < AlignBlobData(blobLen); i++)
                Assert.That(data[argOffset + 4 + i], Is.EqualTo(0));
            Assert.That(data.Length, Is.EqualTo(pathLen + tagLen + expectedArgBytes));
        }

        [Test]
        public void Utf8Path_FieldSizeMatchesEncodedLayout() {
            var path = "/日本";
            var pathBytes = Encoding.UTF8.GetByteCount(path);
            var pathLen = AlignString(pathBytes);
            var data = new Encoder(path).Add(1).Encode();

            Assert.That(pathLen, Is.EqualTo(8));
            Assert.That(Encoding.UTF8.GetString(data, 0, pathBytes), Is.EqualTo(path));
            Assert.That(data[pathBytes], Is.EqualTo(0));
            Assert.That(data[pathLen - 1], Is.EqualTo(0));
            Assert.That(data[pathLen], Is.EqualTo((byte)','));
        }

        [Test]
        public void MixedArgs_RoundTrip() {
            var blob = new byte[] { 1, 2, 3 };
            var msg = ParseSingle(new Encoder("/mix")
                .Add("")
                .Add("a")
                .Add("abcd")
                .Add(blob)
                .Add(7)
                .Encode());

            Assert.That(msg.path, Is.EqualTo("/mix"));
            Assert.That(msg.data.Length, Is.EqualTo(5));
            Assert.That(msg.data[0], Is.EqualTo(""));
            Assert.That(msg.data[1], Is.EqualTo("a"));
            Assert.That(msg.data[2], Is.EqualTo("abcd"));
            CollectionAssert.AreEqual(blob, (byte[])msg.data[3]);
            Assert.That(msg.data[4], Is.EqualTo(7));
        }
    }
}
