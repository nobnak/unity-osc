using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RingBuffer<T> : IEnumerable<T> {
	private int _length = 0;
	private T[] _ring;
	private int _front = 0;
	private int _mask;
	
	public RingBuffer(int capasity) {
		capasity = Pot(capasity);
		_ring = new T[capasity];
		_mask = capasity - 1;
	}
	
	public int Pot(int n) {
		var t = 0;
		for (n--; n != 0; n >>= 1)
			t = (t << 1) + 1;
		return t + 1;
	}
	
	public T this[int i]{
		get {
			lock (this) {
				return _ring[(i + _front) & _mask];
			}
		}
		set {
			lock (this) {
				_ring[(i + _front) & _mask] = value;
			}
		}
	}
	
	public void Add(T one) {
		lock (this) {
			_ring[_front] = one;
			_front = (_front + 1) & _mask;
			_length = _length >= _ring.Length ? _length : _length + 1;
		}
	}

	#region IEnumerable[T] implementation
	public IEnumerator<T> GetEnumerator() {
		lock (this) {
			for (var i = 0; i < _length; i++)
				yield return _ring[(_front - i) & _mask];
		}
	}
	#endregion

	#region IEnumerable implementation
	IEnumerator IEnumerable.GetEnumerator() {
		throw new System.NotImplementedException();
	}
	#endregion
}
