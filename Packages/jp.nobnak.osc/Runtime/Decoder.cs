// Osc.cs - A minimal OSC receiver implementation for Unity.
// https://github.com/keijiro/unity-osc
using System;
using System.Net;
using System.Text;

namespace Osc2 {
    using MessageQueue = System.Collections.Generic.Queue<Message>;

    /// <summary>LRU cache for strings keyed by byte sequence. Reduces allocation by skipping GetString on cache hit.</summary>
    public class StringCache {
        struct Entry {
            public byte[] key;
            public string value;
        }
        readonly Entry[] _entries;
        readonly int _maxKeyLength;
        int _next;

        /// <param name="capacity">Number of cache entries</param>
        /// <param name="maxKeyLength">Strings longer than this (bytes) are not cached. 0 = no limit</param>
        public StringCache(int capacity = 64, int maxKeyLength = 0) {
            _entries = new Entry[capacity];
            _maxKeyLength = maxKeyLength;
        }

        public string GetOrAdd(ReadOnlySpan<byte> key) {
            if (_maxKeyLength > 0 && key.Length > _maxKeyLength)
                return Encoding.UTF8.GetString(key);
            for (var i = 0; i < _entries.Length; i++) {
                var e = _entries[i];
                if (e.key != null && e.key.Length == key.Length && key.SequenceEqual(e.key))
                    return e.value;
            }
            var s = Encoding.UTF8.GetString(key);
            var idx = _next++ % _entries.Length;
            _entries[idx] = new Entry { key = key.ToArray(), value = s };
            return s;
        }
    }

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
        readonly StringCache _pathCache;
        readonly StringCache _typeTagCache;
        readonly StringCache _stringArgCache;
        #endregion
        
        #region Public members
        public int MessageCount {
            get { return messageBuffer.Count; }
        }
        
        /// <summary>No caching (backward compatible)</summary>
		public Parser() {
            messageBuffer = new MessageQueue();
            _pathCache = _typeTagCache = _stringArgCache = null;
        }
        /// <summary>Caching enabled. path:64, typeTag:16, arg:128 (max 64 bytes)</summary>
        public Parser(bool useCaching) : this(useCaching ? 64 : 0, useCaching ? 16 : 0, useCaching ? 128 : 0, 64) { }
        /// <summary>Specify capacity for each cache. 0 = disabled. argMaxKeyLength limits string arg cache (bytes)</summary>
        public Parser(int pathCapacity, int typeTagCapacity, int argCapacity, int argMaxKeyLength = 64) {
            messageBuffer = new MessageQueue();
            _pathCache = pathCapacity > 0 ? new StringCache(pathCapacity) : null;
            _typeTagCache = typeTagCapacity > 0 ? new StringCache(typeTagCapacity) : null;
            _stringArgCache = argCapacity > 0 ? new StringCache(argCapacity, argMaxKeyLength) : null;
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
            var path = ReadString(data, ref readPoint, _pathCache);
            
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
            
            var types = ReadString(data, ref readPoint, _typeTagCache);
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
                    msgdata[i] = ReadString(data, ref readPoint, _stringArgCache);
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
        
		string ReadString(Span<byte> data, ref int start) {
            var count = 0;
            while (data[start + count] != 0)
                count++;
            var s = Encoding.UTF8.GetString(data.Slice(start, count));
            start += (count + 4) & ~3;
            return s;
        }
        string ReadString(Span<byte> data, ref int start, StringCache cache) {
            var count = 0;
            while (data[start + count] != 0)
                count++;
            var slice = data.Slice(start, count);
            var s = cache != null ? cache.GetOrAdd(slice) : Encoding.UTF8.GetString(slice);
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