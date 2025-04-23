// Osc.cs - A minimal OSC receiver implementation for Unity.
// https://github.com/keijiro/unity-osc
using System;
using System.Net;
using System.Text;

namespace Osc2 {
    using MessageQueue = System.Collections.Generic.Queue<Message>;

    public struct Capsule {
        public Message message;
        public IPEndPoint local;
        public IPEndPoint remote;

        public Capsule(Message message, IPEndPoint local, IPEndPoint remote) {
            this.message = message;
            this.local = local;
            this.remote = remote;
        }

        public override string ToString() {
            return $"{nameof(Capsule)}: {remote}->{local}\n{message}";
        }
    }
    public struct Message {
        public string path;
        public object[] data;
        
		public override string ToString () {
			var buf = new StringBuilder();
            buf.AppendFormat("{0}", path);
            for (var i = 0; i < data.Length; i++) {
                var v = data[i];
                buf.Append($", {v} ({v.GetType().Name})");
            }
            return buf.ToString();
        }
    }
    
	public class Parser {
        #region General private members
        MessageQueue messageBuffer;
        #endregion
        
        #region Public members
        public int MessageCount {
            get { return messageBuffer.Count; }
        }
        
		public Parser () {
            messageBuffer = new MessageQueue ();
        }        
		public Message PopMessage () {
            return messageBuffer.Dequeue ();
        }        
		public void FeedData (Span<byte> data) {
            var readPoint = 0;
            ReadMessage (data, ref readPoint);
        }
        #endregion
        
        #region Private methods
		void ReadMessage (Span<byte> data, ref int readPoint) {
            var path = ReadString (data, ref readPoint);
            
            if (path == "#bundle") {
                ReadInt64 (data, ref readPoint);
                
                while (true) {
                    if (readPoint >= data.Length)
                        return;
                    var peek = data[readPoint];
                    if (peek == '/' || peek == '#') {
                        ReadMessage (data, ref readPoint);
                        return;
                    }
                    var bundleEnd = readPoint + ReadInt32 (data, ref readPoint);
                    while (readPoint < bundleEnd)
                        ReadMessage (data, ref readPoint);
                }
            }
            
            var temp = new Message ();
            temp.path = path;
            
            var types = ReadString (data, ref readPoint);
			var msgdata = new object[(types.Length > 0 ? types.Length - 1 : 0)];

			for (var i = 0; i < types.Length - 1; i++) {
				switch (types[i + 1]) {
					case 'f':
                    msgdata[i] = ReadFloat32(data, ref readPoint);
						break;
					case 'i':
                    msgdata[i] = ReadInt32(data, ref readPoint);
						break;
					case 's':
                    msgdata[i] = ReadString(data, ref readPoint);
						break;
					case 'b':
                    msgdata[i] = ReadBlob(data, ref readPoint);
						break;
				}
			}
			temp.data = msgdata;

			messageBuffer.Enqueue (temp);
        }
        
		float ReadFloat32 (Span<byte> data, ref int start) {
            var union32 = new Encoder.Union32();
            union32.Unpack(data, start);
            start += 4;
            return union32.floatdata;
        }
        
        int ReadInt32 (Span<byte> data, ref int start) {
            var union32 = new Encoder.Union32();
            union32.Unpack(data, start);
            start += 4;
            return union32.intdata;
        }
        
		long ReadInt64 (Span<byte> data, ref int start) {
			var union64 = new Encoder.Union64 ();
			union64.Unpack (data, start);
            start += 8;
			return union64.intdata;
        }
        
		string ReadString (Span<byte> data, ref int start) {
            var count = 0;
            while (data[start + count] != 0)
                count++;
            var s = System.Text.Encoding.UTF8.GetString (data.Slice(start, count));
            start += (count + 4) & ~3;
            return s;
        }
        
		byte[] ReadBlob (Span<byte> data, ref int start) {
            var length = ReadInt32 (data, ref start);
            var temp = data.Slice(start, length).ToArray();
            start += (length + 3) & ~3;
            return temp;
        }
        #endregion
    }
}