// ****************************************************************************
// 
// MPEG4 Modifier
// Copyright (C) 2004-2007  Moitah (moitah@yahoo.com)
// 
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
// 
// ****************************************************************************

using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Collections.Generic;

namespace JDP {
	interface IFrameModifier {
		void SetVideoInfo(int width, int height, string fourCC);
		void PreviewStart();
		void PreviewFrame(byte[] data);
		void PreviewDone();
		void ModifyStart();
		void ModifyFrame(byte[] data, bool isKeyFrame);
		void ModifyDone();
	}

	interface IVideoModifier {
		void WriteFrame(byte[] data, bool isKeyFrame);
	}

	static class General {
		public static byte[] ConcatByteArrays(List<byte[]> byteArrays) {
			if (byteArrays.Count == 1) return byteArrays[0];

			int length = 0;
			int offset = 0;
			byte[] newArray;

			foreach (byte[] a in byteArrays) {
				length += a.Length;
			}

			newArray = new byte[length];
			foreach (byte[] a in byteArrays) {
				Array.Copy(a, 0, newArray, offset, a.Length);
				offset += a.Length;
			}

			return newArray;
		}
	}

	static class StructHelper<T> where T : struct {
		private static int _sizeOf;
		private static int[] _map;

		static StructHelper() {
			_sizeOf = Marshal.SizeOf(typeof(T));
			MapStructure();
		}

		public static int SizeOf {
			get {
				return _sizeOf;
			}
		}

		public static unsafe int ToBytes(void* structure, byte[] dst, int dstOffset, bool structIsBigEndian) {
			if ((dst == null) || (dstOffset < 0) || (dstOffset >= dst.Length)) {
				throw new ArgumentException();
			}

			int length = Math.Min(_sizeOf, dst.Length - dstOffset);

			fixed (byte* pDst = &dst[dstOffset]) {
				MemCpy(structure, pDst, length);
				if (BitConverter.IsLittleEndian == structIsBigEndian) {
					ReverseByteOrder(pDst);
				}
			}

			return length;
		}

		public static unsafe int ToBytes(T structure, byte[] dst, int dstOffset, bool structIsBigEndian) {
			GCHandle hStructure = GCHandle.Alloc(structure, GCHandleType.Pinned);
			int length = ToBytes((void*)hStructure.AddrOfPinnedObject(), dst, dstOffset, structIsBigEndian);
			hStructure.Free();
			return length;
		}

		public static unsafe byte[] ToBytes(T structure, bool structIsBigEndian) {
			byte[] bytes = new byte[_sizeOf];
			GCHandle hStructure = GCHandle.Alloc(structure, GCHandleType.Pinned);
			ToBytes((void*)hStructure.AddrOfPinnedObject(), bytes, 0, structIsBigEndian);
			hStructure.Free();
			return bytes;
		}

		public static unsafe int FromBytes(byte[] src, int srcOffset, void* structure, bool structIsBigEndian) {
			if ((src == null) || (srcOffset < 0) || (srcOffset >= src.Length)) {
				throw new ArgumentException();
			}

			int length = Math.Min(_sizeOf, src.Length - srcOffset);

			fixed (byte* pSrc = &src[srcOffset]) {
				MemCpy(pSrc, structure, length);
				if (BitConverter.IsLittleEndian == structIsBigEndian) {
					ReverseByteOrder(structure);
				}
			}

			return length;
		}

		public static unsafe T FromBytes(byte[] src, int srcOffset, bool structIsBigEndian) {
			object structureBoxed = (object)(new T());
			GCHandle hStructure = GCHandle.Alloc(structureBoxed, GCHandleType.Pinned);
			FromBytes(src, srcOffset, (void*)hStructure.AddrOfPinnedObject(), structIsBigEndian);
			hStructure.Free();
			return (T)structureBoxed;
		}

		public static unsafe void MemCpy(void* src, void* dst, int length) {
			byte* pSrc = (byte*)src;
			byte* pDst = (byte*)dst;

			while (length >= 4) {
				*(uint*)pDst = *(uint*)pSrc;
				pDst += 4;
				pSrc += 4;
				length -= 4;
			}
			if (length >= 2) {
				*(ushort*)pDst = *(ushort*)pSrc;
				pDst += 2;
				pSrc += 2;
				length -= 2;
			}
			if (length != 0) {
				*(byte*)pDst = *(byte*)pSrc;
			}
		}

		private static unsafe void ReverseByteOrder(void* structure) {
			byte* p = (byte*)structure;

			for (int i = 0; i < _map.Length; i++) {
				int len = _map[i];

				switch (len) {
					case 4:
						*(uint*)p = ByteOrder.Reverse(*(uint*)p);
						break;
					case 8:
						*(ulong*)p = ByteOrder.Reverse(*(ulong*)p);
						break;
					case 2:
						*(ushort*)p = ByteOrder.Reverse(*(ushort*)p);
						break;
				}

				p += len;
			}
		}

		private static void MapStructure() {
			List<int> map = new List<int>();

			foreach (FieldInfo fi in typeof(T).GetFields()) {
				map.Add(Marshal.SizeOf(fi.FieldType));
			}

			_map = map.ToArray();
		}
	}

	static class ByteOrder {
		public static ulong Reverse(ulong x) {
			return
				((x >> 56) & 0x00000000000000FF) |
				((x >> 40) & 0x000000000000FF00) |
				((x >> 24) & 0x0000000000FF0000) |
				((x >>  8) & 0x00000000FF000000) |
				((x <<  8) & 0x000000FF00000000) |
				((x << 24) & 0x0000FF0000000000) |
				((x << 40) & 0x00FF000000000000) |
				((x << 56) & 0xFF00000000000000);
		}

		public static long Reverse(long x) {
			ulong ux = (ulong)x;
			return (long)(
				((ux >> 56) & 0x00000000000000FF) |
				((ux >> 40) & 0x000000000000FF00) |
				((ux >> 24) & 0x0000000000FF0000) |
				((ux >>  8) & 0x00000000FF000000) |
				((ux <<  8) & 0x000000FF00000000) |
				((ux << 24) & 0x0000FF0000000000) |
				((ux << 40) & 0x00FF000000000000) |
				((ux << 56) & 0xFF00000000000000));
		}

		public static uint Reverse(uint x) {
			return
				((x >> 24) & 0x000000FF) |
				((x >>  8) & 0x0000FF00) |
				((x <<  8) & 0x00FF0000) |
				((x << 24) & 0xFF000000);
		}

		public static int Reverse(int x) {
			uint ux = (uint)x;
			return (int)(
				((ux >> 24) & 0x000000FF) |
				((ux >>  8) & 0x0000FF00) |
				((ux <<  8) & 0x00FF0000) |
				((ux << 24) & 0xFF000000));
		}

		public static ushort Reverse(ushort x) {
			return (ushort)(
				((x >> 8) & 0x00FF) |
				((x << 8) & 0xFF00));
		}

		public static short Reverse(short x) {
			ushort ux = (ushort)x;
			return (short)(
				((ux >> 8) & 0x00FF) |
				((ux << 8) & 0xFF00));
		}
	}

	static class BitConverterLE {
		public static ulong ToUInt64(byte[] value, int startIndex) {
			return
				((ulong)value[startIndex    ]      ) |
				((ulong)value[startIndex + 1] <<  8) |
				((ulong)value[startIndex + 2] << 16) |
				((ulong)value[startIndex + 3] << 24) |
				((ulong)value[startIndex + 4] << 32) |
				((ulong)value[startIndex + 5] << 40) |
				((ulong)value[startIndex + 6] << 48) |
				((ulong)value[startIndex + 7] << 56);
		}

		public static uint ToUInt32(byte[] value, int startIndex) {
			return
				((uint)value[startIndex    ]      ) |
				((uint)value[startIndex + 1] <<  8) |
				((uint)value[startIndex + 2] << 16) |
				((uint)value[startIndex + 3] << 24);
		}

		public static void WriteBytes(ulong value, byte[] dst, int startIndex) {
			dst[startIndex    ] = (byte)(value      );
			dst[startIndex + 1] = (byte)(value >>  8);
			dst[startIndex + 2] = (byte)(value >> 16);
			dst[startIndex + 3] = (byte)(value >> 24);
			dst[startIndex + 4] = (byte)(value >> 32);
			dst[startIndex + 5] = (byte)(value >> 40);
			dst[startIndex + 6] = (byte)(value >> 48);
			dst[startIndex + 7] = (byte)(value >> 56);
		}

		public static void WriteBytes(uint value, byte[] dst, int startIndex) {
			dst[startIndex    ] = (byte)(value      );
			dst[startIndex + 1] = (byte)(value >>  8);
			dst[startIndex + 2] = (byte)(value >> 16);
			dst[startIndex + 3] = (byte)(value >> 24);
		}
	}
}