using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

namespace DataUI {
	public class ReflectiveCopy<SRC, DST> {
		[System.Flags]
		public enum FieldClassEnum { 
			Primitive = 1, Enum = 1 << 1, String = 1 << 2, Struct = 1  << 3, Class = 1 << 4,
			Any = Primitive | Enum | String | Struct | Class
		}

        public const BindingFlags FIELD_BINDING_ATTR = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;

        readonly SRC _src;
        readonly DST _dst;
        readonly List<Tuple<FieldInfo, FieldInfo>> _pairs;

        public ReflectiveCopy(SRC src, DST dst) : this(src, dst, FIELD_BINDING_ATTR) {
        }
        public ReflectiveCopy(SRC src, DST dst, BindingFlags bflags) {
            this._src = src;
            this._dst = dst;

            var srcFields = typeof(SRC).GetFields (bflags);
            var dstFields = typeof(DST).GetFields (bflags);
            _pairs = new List<Tuple<FieldInfo, FieldInfo>> ();
            for (var i = 0; i < srcFields.Length; i++) {
                var sf = srcFields [i];
                for (var j = 0; j < dstFields.Length; j++) {
                    var df = dstFields [j];
                    if (sf.Name == df.Name) {
                        if (sf.FieldType == df.FieldType)
                            _pairs.Add (new Tuple<FieldInfo, FieldInfo> (sf, df));
                        break;
                    }
                }
            }
        }
        public void Sync() {
            for (var i = 0; i < _pairs.Count; i++) {
                try {
                    var pair = _pairs [i];
                    pair.t.SetValue (_dst, pair.s.GetValue (_src));
                } catch (System.Exception e) {
                    Debug.Log(e);
                }
            }
        }

        public struct Tuple<S, T> {
            public readonly S s;
            public readonly T t;

            public Tuple(S s, T t) {
                this.s = s;
                this.t = t;
            }
        }
	}
}