using System.Collections.Generic;
using System.Net;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Osc2 {
	public class Encoder {
		public string Path { get; protected set; }
		public LinkedList<IParam> Params { get; protected set; }

        #region converter
        public static implicit operator byte[](Encoder encoder) {
            return encoder.Encode();
        }
        #endregion

        public Encoder (string path) {
			Path = path;
			Params = new LinkedList<IParam> ();
		}

		public Encoder Add (int content) {
			Params.AddLast (new Int32Param (content));
			return this;
		}
		public Encoder Add (float content) { 
			Params.AddLast (new Float32Param (content));
			return this;
		}
		public Encoder Add (string content) { 
			Params.AddLast (new StringParam (content));
			return this;
		}
		public Encoder Add (byte[] content) { 
			Params.AddLast (new BlobParam (content, 0, content.Length));
			return this;
		}
		public Encoder Add (byte[] content, int offset, int length) { 
			Params.AddLast (new BlobParam (content, offset, length));
			return this;
		}
		public Encoder Add (int seconds, int fraction) {
			Params.AddLast (new TimeParam (seconds, fraction));
			return this;
		}

		public byte[] Encode () {
			var lenAddress = (Path.Length + 4) & ~3;
			var lenTags = (Params.Count + 5) & ~3;
			var lenDatas = 0;
			foreach (var p in Params)
				lenDatas += p.Length;
			var bytedata = new byte[lenAddress + lenTags + lenDatas];

			var offset = 0;
			Encoding.UTF8.GetBytes (Path, 0, Path.Length, bytedata, offset);
			offset += lenAddress;

			bytedata [offset] = (byte)',';
			var addOffset = 0;
			foreach (var p in Params)
				bytedata [offset + ++addOffset] = p.Tag;
			offset += lenTags;

			foreach (var p in Params) {
				p.Assign (bytedata, offset);
				offset += p.Length;
			}

			return bytedata;
		}

		public static byte[] ReverseBytes (byte[] inArray) {
			byte temp;
			int highCtr = inArray.Length - 1;

			for (int ctr = 0; ctr < inArray.Length / 2; ctr++) {
				temp = inArray [ctr];
				inArray [ctr] = inArray [highCtr];
				inArray [highCtr] = temp;
				highCtr -= 1;
			}
			return inArray;
		}

		public interface IParam {
			byte Tag { get; }
			int Length { get; }
			void Assign (byte[] output, int offset);
		}

		public class Int32Param : IParam {
			private Union32 _union;

			public Int32Param (int intdata) { _union = new Union32 (){ intdata = intdata }; }

			#region IParam implementation
			public byte Tag { get { return (byte)'i'; } }
			public int Length { get { return 4; } }
			public void Assign (byte[] output, int offset) { _union.Pack (output, offset); }
			#endregion
		}

		public class Float32Param : IParam {
			private Union32 _union;

			public Float32Param (float floatdata) { _union = new Union32 (){ floatdata = floatdata }; }

			#region IParam implementation
			public byte Tag { get { return (byte)'f'; } }
			public int Length { get { return 4; } }
			public void Assign (byte[] output, int offset) { _union.Pack (output, offset); }
			#endregion
		}

		public class StringParam : IParam {
			private string _stringdata;

			public StringParam (string stringdata) { _stringdata = stringdata; }

			#region IParam implementation
			public byte Tag { get { return (byte)'s'; } }
			public int Length { get { return (Encoding.UTF8.GetByteCount (_stringdata) + 4) & ~3; } }
			public void Assign (byte[] output, int offset) {
				Encoding.UTF8.GetBytes (_stringdata, 0, _stringdata.Length, output, offset);
			}
			#endregion
		}

		public class BlobParam : IParam {
			private byte[] _bytedata;
			private int _offset;
			private Union32 _length;

			public BlobParam (byte[] bytedata, int offset, int length) {
				_bytedata = bytedata;
				_offset = offset;
				_length = new Union32 (){ intdata = length };
			}

			#region IParam implementation
			public byte Tag { get { return (byte)'b'; } }
			public int Length { get { return (_length.intdata + 7) & ~3; } }
			public void Assign (byte[] output, int outputOffset) {
				_length.Pack (output, outputOffset);
				outputOffset += 4;
				Buffer.BlockCopy (_bytedata, _offset, output, outputOffset, _length.intdata);
			}
			#endregion
		}

		public class TimeParam : IParam {
			private Union32 _seconds;
			private Union32 _frac;
			
			public TimeParam (int seconds, int frac) {
				_seconds = new Union32 (){ intdata = seconds };
				_frac = new Union32 (){ intdata = frac };
			}
			
			#region IParam implementation
			public byte Tag { get { return (byte)'t'; } }
			public int Length { get { return 8; } }
			public void Assign (byte[] output, int offset) {
				_seconds.Pack (output, offset);
				_frac.Pack (output, offset + 4);
			}
			#endregion
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct Union32 {
			[FieldOffset(0)]
			public int intdata;
			[FieldOffset(0)]
			public float floatdata;
			[FieldOffset(0)]
			public byte Byte0;
			[FieldOffset(1)]
			public byte Byte1;
			[FieldOffset(2)]
			public byte Byte2;
			[FieldOffset(3)]
			public byte Byte3;

			public void Pack (Span<byte> output, int offset) {
				var inc = BitConverter.IsLittleEndian ? -1 : 1;
				offset += BitConverter.IsLittleEndian ? 3 : 0;
				output [offset] = Byte0; offset += inc;
				output [offset] = Byte1; offset += inc;
				output [offset] = Byte2; offset += inc;
				output [offset] = Byte3; offset += inc;
			}
			
			public void Unpack(Span<byte> input, int offset) {
				var inc = BitConverter.IsLittleEndian ? -1 : 1;
				offset += BitConverter.IsLittleEndian ? 3 : 0;
				Byte0 = input [offset]; offset += inc;
				Byte1 = input [offset]; offset += inc;
				Byte2 = input [offset]; offset += inc;
				Byte3 = input [offset]; offset += inc;
			}
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct Union64 {
			[FieldOffset(0)]
			public long intdata;
			[FieldOffset(0)]
			public byte Byte0;
			[FieldOffset(1)]
			public byte Byte1;
			[FieldOffset(2)]
			public byte Byte2;
			[FieldOffset(3)]
			public byte Byte3;
			[FieldOffset(4)]
			public byte Byte4;
			[FieldOffset(5)]
			public byte Byte5;
			[FieldOffset(6)]
			public byte Byte6;
			[FieldOffset(7)]
			public byte Byte7;

			public void Pack (Span<byte> output, int offset) {
				var inc = BitConverter.IsLittleEndian ? -1 : 1;
				offset += BitConverter.IsLittleEndian ? 7 : 0;
				output [offset] = Byte0; offset += inc;
				output [offset] = Byte1; offset += inc;
				output [offset] = Byte2; offset += inc;
				output [offset] = Byte3; offset += inc;
				output [offset] = Byte4; offset += inc;
				output [offset] = Byte5; offset += inc;
				output [offset] = Byte6; offset += inc;
				output [offset] = Byte7; offset += inc;
			}

			public void Unpack (Span<byte> input, int offset) {
				var inc = BitConverter.IsLittleEndian ? -1 : 1;
				offset += BitConverter.IsLittleEndian ? 7 : 0;
				Byte0 = input [offset]; offset += inc;
				Byte1 = input [offset]; offset += inc;
				Byte2 = input [offset]; offset += inc;
				Byte3 = input [offset]; offset += inc;
				Byte4 = input [offset]; offset += inc;
				Byte5 = input [offset]; offset += inc;
				Byte6 = input [offset]; offset += inc;
				Byte7 = input [offset]; offset += inc;
			}
		}
	}
}