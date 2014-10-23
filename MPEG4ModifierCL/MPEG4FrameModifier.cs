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
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace JDP {
	class MPEG4FrameModifier : IFrameModifier {
		private IVideoModifier _videoModifier;
		private int _frameWidth, _frameHeight, _frameIndex;
		private string _fourCC;
		private VOLInfo _lastVOL;
		private List<VOLInfo> _volList;
		private List<VOPInfo> _vopList;
		private List<MPEG4UserData> _userDataList;
		private MPEG4PAR _parInfo;
		private bool _foundUD, _containsBVOPs, _isPacked, _newIsPacked, _tff;
		private long _timeBase, _prevTimeBase, _prevTS1, _prevTS2;
		private byte[] _shiftVOP;
		private int _delayedWriteDrops;
		private List<byte[]> _queuedFrame;
		private bool _queuedIsKeyFrame, _isFirstVOP;
		private VOPInfo _backRefVOP;

		private static readonly int[] _zigzag_scan = {
			 0,  1,  8, 16,  9,  2,  3, 10,
			17, 24, 32, 25, 18, 11,  4,  5,
			12, 19, 26, 33, 40, 48, 41, 34,
			27, 20, 13,  6,  7, 14, 21, 28,
			35, 42, 49, 56, 57, 50, 43, 36,
			29, 22, 15, 23, 30, 37, 44, 51,
			58, 59, 52, 45, 38, 31, 39, 46,
			53, 60, 61, 54, 47, 55, 62, 63
		};

		private static readonly int[] _sprite_traj_len_code = {
			0x000, 0x002, 0x003, 0x004, 0x005, 0x006, 0x00E, 0x01E,
			0x03E, 0x07E, 0x0FE, 0x1FE, 0x3FE, 0x7FE, 0xFFE
		};

		private static readonly int[] _sprite_traj_len_bits = {
			2, 3, 3, 3, 3, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12
		};

		private static readonly string[] _ar_desc = {
			"Invalid aspect_ratio setting",
			"Square pixels",
			"4:3 PAL pixel shape",
			"4:3 NTSC pixel shape",
			"16:9 PAL pixel shape",
			"16:9 NTSC pixel shape",
			"Invalid aspect_ratio setting",
			"Invalid aspect_ratio setting",
			"Invalid aspect_ratio setting",
			"Invalid aspect_ratio setting",
			"Invalid aspect_ratio setting",
			"Invalid aspect_ratio setting",
			"Invalid aspect_ratio setting",
			"Invalid aspect_ratio setting",
			"Invalid aspect_ratio setting",
			"Custom pixel shape"
		};

		private static readonly string[] _vop_type_desc = {
			"I-VOP", "P-VOP", "B-VOP", "S-VOP", "N-VOP", "N-VOP(D)"
		};

		public MPEG4FrameModifier() {
		}

		public IVideoModifier VideoModifier {
			set {
				_videoModifier = value;
			}
		}

		public int FrameWidth {
			get {
				return _frameWidth;
			}
		}

		public int FrameHeight {
			get {
				return _frameHeight;
			}
		}

		public MPEG4PAR PARInfo {
			get {
				return _parInfo;
			}
			set {
				_parInfo = value;
			}
		}

		public List<MPEG4UserData> UserDataList {
			get {
				return _userDataList;
			}
			set {
				_userDataList = value;
			}
		}

		public bool IsPacked {
			get {
				return _isPacked;
			}
		}

		public bool NewIsPacked {
			get {
				return _newIsPacked;
			}
			set {
				_newIsPacked = value;
			}
		}

		public bool IsInterlaced {
			get {
				return _volList[0].interlaced;
			}
		}

		public bool TopFieldFirst {
			get {
				return _tff;
			}
			set {
				_tff = value;
			}
		}

		public bool ContainsBVOPs {
			get {
				return _containsBVOPs;
			}
		}

		public void SetVideoInfo(int width, int height, string fourCC) {
			_frameWidth = width;
			_frameHeight = height;
			_fourCC = fourCC;
		}

		private void AnalyzePackedBitstream() {
			bool packed, shifting;
			int i, prevFrame, vopPosInFrame;
			long backRefTS;

			packed = false;
			shifting = false;
			prevFrame = -1;
			vopPosInFrame = 0;
			backRefTS = -1;

			for (i = 0; i < _vopList.Count; i++) {
				VOPInfo vop = _vopList[i];

				vopPosInFrame = (vop.frame_num == prevFrame) ? vopPosInFrame + 1 : 1;
				if (vopPosInFrame > 2) {
					throw new Exception("Too many VOPs in a frame.");
				}

				if (vopPosInFrame == 1) {
					if (vop.is_reference_vop) {
						shifting = false;
						backRefTS = vop.disp_time_ticks;
					}
					else if (shifting && !vop.coded && (vop.disp_time_ticks == backRefTS)) {
						shifting = false;
						vop.vop_type = VOPType.N_VOP_D;
						_vopList[i] = vop;
					}
				}
				else {
					packed = true;
					shifting = true;
				}

				prevFrame = vop.frame_num;
			}

			_isPacked = packed;
			_newIsPacked = packed;
		}

		public void PreviewStart() {
			_frameIndex = 0;
			_prevTimeBase = 0;
			_timeBase = 0;
			_prevTS1 = -1;
			_prevTS2 = -1;
			_volList = new List<VOLInfo>();
			_vopList = new List<VOPInfo>();
			_userDataList = new List<MPEG4UserData>();
			_foundUD = false;
			_containsBVOPs = false;
			_isPacked = false;
			_tff = false;
		}

		public void PreviewFrame(byte[] data) {
			if ((data.Length == 0) || ((data.Length == 1) && (data[0] == 0x7F))) {
				// Ignore drop/delay frames
			}
			else {
				MPEG4ChunkReader mp4Chunks;
				uint startcode;
				byte[] chunk;
				bool loadedUD = false;

				try {
					mp4Chunks = new MPEG4ChunkReader(data);
				}
				catch (NotMPEG4Exception ex) {
					throw new Exception(ex.Message + "  Codec: " + _fourCC + ".");
				}

				for (int i = 0; i < mp4Chunks.Count; i++) {
					startcode = mp4Chunks.StartCode(i);
					chunk = mp4Chunks.ReadChunk(i);

					if ((startcode & 0xFFFFFFF0) == (uint)StartCode.VideoObjectLayer) {
						VOLInfo vol;

						ParseVOL(chunk, false, out vol);
						_lastVOL = vol;
						_volList.Add(vol);
					}
					else if (startcode == (uint)StartCode.VideoObjectPlane) {
						VOPInfo vop;

						if (_volList.Count == 0) {
							throw new Exception("No VOL found before first VOP, make sure the video starts with a keyframe.");
						}

						ParseVOP(chunk, false, out vop);
						vop.frame_num = _frameIndex;
						_vopList.Add(vop);
						if (vop.vop_type == VOPType.B_VOP) {
							_containsBVOPs = true;
						}
					}
					else if (startcode == (uint)StartCode.UserData) {
						if (!_foundUD) {
							MPEG4UserData ud = new MPEG4UserData();
							ud.UserDataWithSC = chunk;
							_userDataList.Add(ud);
							loadedUD = true;
						}
					}
				}

				if (loadedUD) {
					_foundUD = true;
				}
			}

			_frameIndex++;
		}

		public void PreviewDone() {
			if ((_vopList.Count == 0) || (_volList.Count == 0)) {
				throw new Exception("At least one VOL/VOP must be present.");
			}

			VOLInfo firstVOL = _volList[0];
			VOPInfo firstVOP = _vopList[0];

			_parInfo.Type = (PARType)firstVOL.aspect_ratio;
			_parInfo.Width = firstVOL.par_width;
			_parInfo.Height = firstVOL.par_height;

			_tff = firstVOP.top_field_first;

			AnalyzePackedBitstream();
		}

		public void ModifyStart() {
			_prevTimeBase = 0;
			_timeBase = 0;
			_prevTS1 = -1;
			_prevTS2 = -1;
			_shiftVOP = null;
			_isFirstVOP = true;
			_delayedWriteDrops = 0;
			_queuedFrame = new List<byte[]>();
			_queuedIsKeyFrame = false;
			_frameIndex = 0;
		}

		public void ModifyFrame(byte[] data, bool isKeyFrame) {
			List<byte[]> thisFrame = new List<byte[]>();
			bool suppressWrite = false;

			if ((data.Length == 1) && (data[0] == 0x7F)) {
				// This is a delay frame.  It'd be nice to remove it, but that causes synch
				// problems on files that were encoded in segments and joined together if each
				// segment has its own set of delay frames.
				thisFrame.Add(data);
				isKeyFrame = false;
			}
			else if (data.Length == 0) {
				// This is a drop frame
				if (!_isPacked && _newIsPacked) {
					// There is a 1 frame delay, if this frame is written now it would end up in the
					// wrong place.  Make a note of it and let the packing code handle it.
					suppressWrite = true;
					_delayedWriteDrops++;
				}
				else {
					thisFrame.Add(data);
					isKeyFrame = false;
				}
			}
			else {
				MPEG4ChunkReader mp4Chunks = new MPEG4ChunkReader(data);
				uint startcode;
				byte[] chunk;
				int vopPosInFrame = 0;

				for (int i = 0; i < mp4Chunks.Count; i++) {
					startcode = mp4Chunks.StartCode(i);
					chunk = mp4Chunks.ReadChunk(i);

					if ((startcode & 0xFFFFFFF0) == (uint)StartCode.VideoObjectLayer) {
						VOLInfo vol;

						thisFrame.Add(ParseVOL(chunk, true, out vol));
						_lastVOL = vol;

						// Write new userdata after VOL
						foreach (MPEG4UserData ud in _userDataList) {
							thisFrame.Add(ud.UserDataWithSC);
						}
					}
					else if (startcode == (uint)StartCode.VideoObjectPlane) {
						VOPInfo vop;

						vopPosInFrame++;
						chunk = ParseVOP(chunk, true, out vop);
						if (vopPosInFrame == 1) {
							// This might not be the VOP to be written if the file is being unpacked, but
							// I-VOPs are never shifted so the keyframe flag will be correct
							isKeyFrame = (vop.vop_type == VOPType.I_VOP);
						}

						if (_isPacked && !_newIsPacked) {
							if (vopPosInFrame == 1) {
								if (vop.is_reference_vop) {
									thisFrame.Add(chunk);
									_shiftVOP = null;
									_backRefVOP = vop;
								}
								else {
									if (_shiftVOP != null) {
										thisFrame.Add(_shiftVOP);
										_shiftVOP = (!vop.coded && (vop.disp_time_ticks ==
											_backRefVOP.disp_time_ticks)) ? null : chunk;
									}
									else {
										thisFrame.Add(chunk);
									}
								}
							}
							else {
								_shiftVOP = chunk;
							}
						}
						else if (!_isPacked && _newIsPacked) {
							suppressWrite = true;
							if (vop.is_reference_vop || (!vop.coded && vop.disp_time_ticks > _backRefVOP.disp_time_ticks)) {
								if ((_queuedFrame.Count == 0) && !_isFirstVOP) {
									_queuedFrame.Add(GenerateDummyNVOP(_backRefVOP.coding_type,
										_backRefVOP.time_inc, _lastVOL.time_inc_bits));
								}
								_backRefVOP = vop;
								thisFrame.Add(chunk);
							}
							else {
								// Ignore dummy N-VOPs (shouldn't be any since the source file isn't packed)
								if (vop.coded || (vop.disp_time_ticks != _backRefVOP.disp_time_ticks)) {
									_queuedFrame.Add(chunk);
								}
							}
							if (_queuedFrame.Count != 0) {
								_videoModifier.WriteFrame(General.ConcatByteArrays(_queuedFrame), _queuedIsKeyFrame);
							}
							while (_delayedWriteDrops > 0) {
								_videoModifier.WriteFrame(new byte[0], false);
								_delayedWriteDrops--;
							}
							_isFirstVOP = false;
							_queuedFrame = thisFrame;
							_queuedIsKeyFrame = (vop.vop_type == VOPType.I_VOP);
							thisFrame = new List<byte[]>();
						}
						else {
							thisFrame.Add(chunk);
						}
					}
					else if (startcode == (uint)StartCode.UserData) {
						// Ignore original userdata
					}
					else {
						thisFrame.Add(chunk);
					}
				}
			}

			if (!suppressWrite) {
				_videoModifier.WriteFrame(General.ConcatByteArrays(thisFrame), isKeyFrame);
			}
		}

		public void ModifyDone() {
			if (!_isPacked && _newIsPacked) {
				if ((_queuedFrame.Count == 0) && !_isFirstVOP) {
					_queuedFrame.Add(GenerateDummyNVOP(_backRefVOP.coding_type,
						_backRefVOP.time_inc, _lastVOL.time_inc_bits));
				}
				if (_queuedFrame.Count != 0) {
					_videoModifier.WriteFrame(General.ConcatByteArrays(_queuedFrame), _queuedIsKeyFrame);
				}
				while (_delayedWriteDrops > 0) {
					_videoModifier.WriteFrame(new byte[0], false);
					_delayedWriteDrops--;
				}
			}
		}

		private byte[] ParseVOP(byte[] data, bool modify, out VOPInfo vop) {
			BitStream bits, bitsOut;

			vop = new VOPInfo();

			bits = new BitStream(data);
			if (modify) {
				bitsOut = new BitStream(data.Length);
				bits.CopyDest = bitsOut;
			}
			else {
				bitsOut = null;
			}

			bits.Copy(32);
			vop.coding_type = bits.Copy(2);

			while (bits.Copy(1) == 1) {
				vop.modulo_time_base++;
			}
			bits.Copy(1);

			vop.time_inc = bits.Copy(_lastVOL.time_inc_bits);
			bits.Copy(1);

			vop.coded = ToBool(bits.Copy(1));

			vop.vop_type = vop.coded ? (VOPType)vop.coding_type : VOPType.N_VOP;
			vop.is_reference_vop = vop.coded && (vop.vop_type != VOPType.B_VOP);

			// Calculate display time
			if (vop.is_reference_vop) {
				_prevTimeBase = _timeBase;
				_timeBase += vop.modulo_time_base;
				vop.disp_time_ticks = (_timeBase * _lastVOL.time_inc_res) + vop.time_inc;

				_prevTS2 = (_prevTS1 == -1) ? -1 : _prevTS1 % _lastVOL.time_inc_res;
				_prevTS1 = (vop.modulo_time_base * _lastVOL.time_inc_res) + vop.time_inc;
			}
			else {
				long timeBase;
				if (vop.vop_type == VOPType.B_VOP) {
					timeBase = _prevTimeBase;
				}
				else {
					long thisTS = (vop.modulo_time_base * _lastVOL.time_inc_res) + vop.time_inc;
					if (!_lastVOL.low_delay && (_prevTS2 != -1) &&
						(((_prevTS1 - _prevTS2) * 2) < _lastVOL.time_inc_res) &&
						(thisTS > _prevTS2) && (thisTS < _prevTS1))
					{
						timeBase = _prevTimeBase;
					}
					else {
						timeBase = _timeBase;
					}
				}
				vop.disp_time_ticks = ((timeBase + vop.modulo_time_base) *
					_lastVOL.time_inc_res) + vop.time_inc;
			}

			if (vop.coded) {
				if (_lastVOL.newpred_enable) {
					int vop_id_bits = Math.Min(_lastVOL.time_inc_bits + 3, 15);

					bits.Copy(vop_id_bits);
					if (bits.Copy(1) == 1) {
						bits.Copy(vop_id_bits);
					}
					bits.Copy(1);
				}

				if ( (_lastVOL.shape != (uint)VOLShape.BinaryOnly) &&
					( (vop.coding_type == (uint)VOPType.P_VOP) ||
					( (vop.coding_type == (uint)VOPType.S_VOP) && (_lastVOL.sprite_enable == (uint)Sprite.GMC) ) ) )
				{
					bits.Copy(1);
				}

				if ( _lastVOL.reduced_resolution_enable &&
					(_lastVOL.shape == (uint)VOLShape.Rectangular) &&
					( (vop.coding_type == (uint)VOPType.P_VOP) || (vop.coding_type == (uint)VOPType.I_VOP) ) )
				{

					bits.Copy(1);
				}

				if (_lastVOL.shape != (uint)VOLShape.Rectangular) {
					if ( !( (_lastVOL.sprite_enable == (uint)Sprite.Static) &&
						(vop.coding_type == (uint)VOPType.I_VOP) ) )
					{
						// 56 bits
						bits.Copy(32);
						bits.Copy(24);
					}
					bits.Copy(1);
					if (bits.Copy(1) == 1) {
						bits.Copy(8);
					}
				}

				if (_lastVOL.shape != (uint)VOLShape.BinaryOnly) {
					bits.Copy(3);
					if (_lastVOL.interlaced) {
						vop.top_field_first = ToBool(bits.Read(1));
						if (modify) {
							bitsOut.Write( (_tff ? 1U : 0U) , 1);
						}
						bits.Copy(1);
					}
				}

				if ( (_lastVOL.sprite_enable != (uint)Sprite.None) &&
					(vop.coding_type == (uint)VOPType.S_VOP) )
				{
					vop.warping_points_used = 0;

					for (int i = 1; i <= _lastVOL.sprite_warping_points; i++) {
						bool x_used = CopyWarpingCode(bits);
						bool y_used = CopyWarpingCode(bits);

						if (x_used || y_used) {
							vop.warping_points_used = (uint)i;
						}
					}
				}
			}

			if (modify) {
				bits.CopyRemaining();
			}

			return modify ? bitsOut.GetBytes() : null;
		}

		private byte[] ParseVOL(byte[] data, bool modify, out VOLInfo vol) {
			BitStream bits, bitsOut;

			vol = new VOLInfo();

			bits = new BitStream(data);
			if (modify) {
				bitsOut = new BitStream(data.Length + 16);
				bits.CopyDest = bitsOut;
			}
			else {
				bitsOut = null;
			}

			bits.Copy(32);
			bits.Copy(9);

			if (bits.Copy(1) == 1) {
				vol.ver_id = bits.Copy(4);
				bits.Copy(3);
			}
			else {
				vol.ver_id = 1;
			}

			// Read PAR info
			vol.aspect_ratio = bits.Read(4);
			if (vol.aspect_ratio == (uint)PARType.Custom) {
				vol.par_width  = bits.Read(8);
				vol.par_height = bits.Read(8);
			}

			// Write new PAR info
			if (modify) {
				bitsOut.Write((uint)_parInfo.Type, 4);
				if (_parInfo.Type == PARType.Custom) {
					bitsOut.Write(_parInfo.Width, 8);
					bitsOut.Write(_parInfo.Height, 8);
				}
			}

			vol.low_delay = false;
			if (bits.Copy(1) == 1) {
				bits.Copy(2);
				vol.low_delay = ToBool(bits.Copy(1));
				if (bits.Copy(1) == 1) {
					// 79 bits
					bits.Copy(32);
					bits.Copy(32);
					bits.Copy(15);
				}
			}

			vol.shape = bits.Copy(2);
			if ((vol.ver_id != 1) && (vol.shape == (uint)VOLShape.Grayscale)) {
				bits.Copy(4);
			}
			bits.Copy(1);

			vol.time_inc_res = bits.Copy(16);
			vol.time_inc_bits = Math.Max(BitsUsed(vol.time_inc_res - 1), 1);
			bits.Copy(1);

			if (bits.Copy(1) == 1) {
				bits.Copy(vol.time_inc_bits);
			}

			if (vol.shape != (uint)VOLShape.BinaryOnly) {
				if (vol.shape == (uint)VOLShape.Rectangular) {
					bits.Copy(29);
				}

				vol.interlaced = ToBool(bits.Copy(1));
				bits.Copy(1);

				vol.sprite_enable = bits.Copy( vol.ver_id == 1 ? 1 : 2 );
				if (vol.sprite_enable != (uint)Sprite.None) {
					if (vol.sprite_enable == (uint)Sprite.Static) {
						// 56 bits
						bits.Copy(32);
						bits.Copy(24);
					}

					vol.sprite_warping_points = bits.Copy(6);
					if (vol.sprite_warping_points > 4) {
						throw new Exception("Invalid VOL (too many warping points).");
					}

					bits.Copy(3);
					if (vol.sprite_enable == (uint)Sprite.Static) {
						bits.Copy(1);
					}
				}

				if ((vol.ver_id != 1) && (vol.shape != (uint)VOLShape.Rectangular)) {
					bits.Copy(1);
				}

				if (bits.Copy(1) == 1) {
					bits.Copy(8);
				}

				if (vol.shape == (uint)VOLShape.Grayscale) {
					bits.Copy(3);
				}

				vol.mpeg_quant = ToBool(bits.Copy(1));
				if (vol.mpeg_quant) {
					vol.load_intra_quant_mat = ToBool(bits.Copy(1));
					if (vol.load_intra_quant_mat) {
						vol.intra_quant_mat = CopyQuantMatrix(bits);
					}

					vol.load_inter_quant_mat = ToBool(bits.Copy(1));
					if (vol.load_inter_quant_mat) {
						vol.inter_quant_mat = CopyQuantMatrix(bits);
					}

					if (vol.shape == (uint)VOLShape.Grayscale) {
						throw new Exception("Grayscale matrix isn't supported.");
					}
				}

				if (vol.ver_id != 1) {
					vol.quarterpel = ToBool(bits.Copy(1));
				}

				if (bits.Copy(1) == 0) {
					throw new Exception("Complexity estimation isn't supported.");
				}
				bits.Copy(1);

				if (bits.Copy(1) == 1) {
					bits.Copy(1);
				}

				if (vol.ver_id != 1) {
					vol.newpred_enable = ToBool(bits.Copy(1));
					if (vol.newpred_enable) {
						bits.Copy(3);
					}
					vol.reduced_resolution_enable = ToBool(bits.Copy(1));
				}

				if (bits.Copy(1) == 1) {
					throw new Exception("Scalability isn't supported.");
				}
			}
			else {
				if (vol.ver_id != 1) {
					if (bits.Copy(1) == 1) {
						throw new Exception("Scalability isn't supported.");
					}
				}
				bits.Copy(1);
			}

			// Check padding (padding should be present even if the data already ends on
			// a byte boundary, but if there's no padding we will just ignore it)
			if ((bits.Remaining < 0) || (bits.Remaining > 8)) {
				throw new Exception("Invalid VOL.");
			}
			bits.Copy(bits.Remaining);

			return modify ? bitsOut.GetBytes() : null;
		}

		private byte[] GenerateDummyNVOP(uint coding_type, uint time_inc, int time_inc_bits) {
			BitStream bits = new BitStream(7);
			int over;

			bits.Write((uint)StartCode.VideoObjectPlane, 32);
			bits.Write(coding_type, 2);
			bits.Write(0, 1); // modulo_time_base
			bits.Write(1, 1); // marker
			bits.Write(time_inc, time_inc_bits);
			bits.Write(1, 1); // marker
			bits.Write(0, 1); // vop_coded

			over = bits.Position % 8;
			bits.Write(0x7FU >> over, 8 - over);

			return bits.GetBytes();
		}

		private uint[] CopyQuantMatrix(BitStream bits) {
			uint[] qm = new uint[64];
			uint last = 0;
			uint coef;
			int i;

			for (i = 0; i < 64; i++) {
				coef = bits.Copy(8);
				if (coef == 0) break;

				last = coef;
				qm[_zigzag_scan[i]] = coef;
			}

			while (i < 64) {
				qm[_zigzag_scan[i++]] = last;
			}

			return qm;
		}

		private bool CopyWarpingCode(BitStream bits) {
			for (int i = 0; i < _sprite_traj_len_code.Length; i++) {
				if (bits.Peek(_sprite_traj_len_bits[i]) == _sprite_traj_len_code[i]) {
					bits.Copy(_sprite_traj_len_bits[i]);
					bits.Copy(i);
					bits.Copy(1);

					return (i != 0);
				}
			}

			throw new Exception("Invalid VOP (unable to find sprite trajectory length VLC).");
		}

		public string GenerateStats() {
			VOLInfo vol = _volList[0];
			VOPInfo vop = _vopList[0];
			StringBuilder sb = new StringBuilder();
			string newline = Environment.NewLine;

			// Packed bitstream
			sb.AppendFormat("Packed bitstream:  {0}", _isPacked ? "Yes" : "No");
			sb.Append(newline);

			// QPel
			sb.AppendFormat("QPel:              {0}", vol.quarterpel ? "Yes" : "No");
			sb.Append(newline);

			// GMC
			sb.Append("GMC:               ");
			if (vol.sprite_enable == (uint)Sprite.GMC) {
				sb.AppendFormat("Yes ({0} warp point{1})", vol.sprite_warping_points,
					vol.sprite_warping_points == 1 ? String.Empty : "s");
			}
			else {
				sb.Append("No");
			}
			sb.Append(newline);

			// Interlaced
			sb.AppendFormat("Interlaced:        {0}", vol.interlaced ? "Yes" : "No");
			if (vol.interlaced) {
				sb.AppendFormat(" ({0} field first)", vop.top_field_first ? "top" : "bottom");
			}
			sb.Append(newline);

			// Aspect ratio
			sb.AppendFormat("Aspect ratio:      {0}", _ar_desc[vol.aspect_ratio]);
			if (vol.aspect_ratio == (uint)PARType.Custom) {
				sb.AppendFormat(" ({0}:{1} = {2:0.00000})", vol.par_width, vol.par_height,
					(double)vol.par_width / (double)vol.par_height);
			}
			sb.Append(newline);

			// Quant type
			sb.Append("Quant type:        ");
			if (vol.mpeg_quant) {
				sb.Append( (vol.load_intra_quant_mat || vol.load_inter_quant_mat) ?
					"MPEG Custom" : "MPEG");
			}
			else {
				sb.Append("H.263");
			}
			sb.Append(newline);

			// FourCC
			sb.AppendFormat("FourCC:            {0}", _fourCC);
			sb.Append(newline);

			// User data
			if (_userDataList.Count != 0) {
				sb.AppendFormat("User data:         {0}", _userDataList[0]);
				sb.Append(newline);
			}
			for (int i = 1; i < _userDataList.Count; i++) {
				sb.AppendFormat("                   {0}", _userDataList[i]);
				sb.Append(newline);
			}

			// Custom intra matrix
			if (vol.load_intra_quant_mat) {
				sb.AppendFormat("{0}Custom intra matrix:{0}", newline);
				DumpCustomMatrix(sb, vol.intra_quant_mat, 4);
			}

			// Custom inter matrix
			if (vol.load_inter_quant_mat) {
				sb.AppendFormat("{0}Custom inter matrix:{0}", newline);
				DumpCustomMatrix(sb, vol.inter_quant_mat, 4);
			}

			// VOP stats
			sb.Append(newline);
			DumpVOPStats(sb);

			return sb.ToString();
		}

		private void DumpCustomMatrix(StringBuilder sb, uint[] qm, int numSpaces) {
			string spaces = new string(' ', numSpaces);

			for (int o = 0; o < 64; o += 8) {
				sb.Append(spaces);
				for (int x = 0; x < 8; x++) {
					sb.AppendFormat("{0,3}{1}", qm[o + x], (x < 7) ? " " : String.Empty);
				}
				sb.Append(Environment.NewLine);
			}
		}

		private void DumpVOPStats(StringBuilder sb) {
			int i;
			VOPInfo vop;
			int totalVOPs;
			int[] vopTypeCount = new int[6];
			int[] wpCount = new int[5];
			int svopCount;

			for (i = 0; i < _vopList.Count; i++) {
				vop = _vopList[i];

				vopTypeCount[ (int)vop.vop_type ]++;

				if (vop.vop_type == VOPType.S_VOP) {
					wpCount[vop.warping_points_used]++;
				}
			}

			// Stats don't include dummy N-VOPs
			totalVOPs = _vopList.Count - vopTypeCount[5];
			for (i = 0; i < 5; i++) {
				sb.AppendFormat("{0}s: {1} ({2:0.00}%){3}", _vop_type_desc[i], vopTypeCount[i],
					((double)vopTypeCount[i] / (double)totalVOPs) * 100, Environment.NewLine);
			}

			// B-VOP stats
			if (vopTypeCount[(int)VOPType.B_VOP] != 0) {
				sb.Append(Environment.NewLine);
				DumpBVOPStats(sb);
			}

			// Warp point stats
			svopCount = vopTypeCount[ (int)VOPType.S_VOP ];
			if (svopCount != 0) {
				sb.AppendFormat("{0}Warp points used:{0}", Environment.NewLine);
				for (i = 0; i <= 4; i++) {
					if (wpCount[i] != 0) {
						sb.AppendFormat("    {0}: {1:0.00}%{2}", i,
							((double)wpCount[i] / (double)svopCount) * 100, Environment.NewLine);
					}
				}
			}
		}

		private void DumpBVOPStats(StringBuilder sb) {
			int i;
			int n = 0;
			int max = 0;
			int runs = 0;
			int vopListCount = _vopList.Count;
			Dictionary<int, int> conBVOPs = new Dictionary<int, int>();
			VOPInfo vop;
			int count;

			for (i = 0; i <= vopListCount; i++) {
				vop = (i < vopListCount) ? _vopList[i] : new VOPInfo();

				if ((i == vopListCount) || (vop.vop_type != VOPType.B_VOP)) {
					if (n != 0) {
						if (!conBVOPs.TryGetValue(n, out count)) {
							count = 0;
						}
						conBVOPs[n] = count + 1;
						if (n > max) {
							max = n;
						}
						runs++;
						n = 0;
					}
				}
				else {
					n++;
				}
			}

			if (max > 0) {
				sb.AppendFormat("Max consecutive B-VOPs:   {0}{1}", max, Environment.NewLine);
				if (max > 1) {
					for (n = 1; n <= max; n++) {
						if (!conBVOPs.TryGetValue(n, out count)) {
							count = 0;
						}

						sb.AppendFormat("    {0} consec: {1:0.00}%{2}", n, ((double)count / (double)runs) * 100.0,
							Environment.NewLine);
					}
				}
			}
		}

		public void DumpFrameList(StreamWriter sw) {
			int i;
			int prevFrame = -1;
			long timeIncRes = (long)_volList[0].time_inc_res;
			VOPInfo vop;
			string frameDesc, timeDesc;
			TimeSpan dispTime;

			for (i = 0; i < _vopList.Count; i++) {
				vop = _vopList[i];
				frameDesc = (vop.frame_num != prevFrame) ? String.Format("{0,6}:", vop.frame_num) :
					new string(' ', 7);
				dispTime = new TimeSpan((vop.disp_time_ticks * TimeSpan.TicksPerSecond) / timeIncRes);
				timeDesc = String.Format("{0}:{1:00}:{2:00}.{3:000}", (dispTime.Days * 24) + dispTime.Hours,
					dispTime.Minutes, dispTime.Seconds, dispTime.Milliseconds);
				sw.WriteLine("{0} {1,-8} ({2})", frameDesc, _vop_type_desc[ (int)vop.vop_type ],
					timeDesc);

				prevFrame = vop.frame_num;
			}
		}

		public List<MPEG4UserData> SuggestedUserData() {
			List<MPEG4UserData> udList = new List<MPEG4UserData>(_userDataList);
			MPEG4UserData ud = new MPEG4UserData();
			string udStr;
			bool isXvid = false;
			int i;

			for (i = 0; i < udList.Count; i++) {
				udStr = udList[i].ToString();

				if ((udStr.Length >= 4) && (udStr.Substring(0, 4).ToLower() == "xvid")) {
					isXvid = true;
					break;
				}
			}

			for (i = 0; i < udList.Count; i++) {
				udStr = udList[i].ToString();

				if ((udStr.Length >= 4) && (udStr.Substring(0, 4).ToLower() == "divx")) {
					if (isXvid) {
						// XviD adds a fake DivX userdata string, remove it
						udList.RemoveAt(i);
						i--;
					}
					else {
						// DivX puts a 'p' at the end of its userdata if the file is packed
						if (Char.IsLetter(udStr, udStr.Length - 1)) {
							udStr = udStr.Substring(0, udStr.Length - 1);
						}
						ud.SetString(_newIsPacked ? udStr + "p" : udStr);
						udList[i] = ud;
					}
				}
			}

			if (isXvid && _newIsPacked) {
				// Add or re-add fake DivX userdata string
				ud.SetString("DivX503b1393p");
				udList.Insert(0, ud);
			}

			return udList;
		}

		private int BitsUsed(uint x) {
			int numBits = 0;

			while (x != 0) {
				x >>= 1;
				numBits++;
			}

			return numBits;
		}

		private bool ToBool(uint x) {
			return (x != 0);
		}
	}

	class BitStream {
		private byte[] _data;
		private int _pos, _byteLength;
		private BitStream _copyDest;

		public BitStream(byte[] data) {
			_pos = 0;
			_byteLength = data.Length;
			_data = new byte[(_byteLength + 7) & ~(int)3];
			Array.Copy(data, _data, _byteLength);
		}

		public BitStream(int byteLength) {
			_byteLength = byteLength;
			_data = new byte[(_byteLength + 7) & ~(int)3];
		}

		public BitStream CopyDest {
			get {
				return _copyDest;
			}
			set {
				_copyDest = value;
			}
		}

		public byte[] ByteBuffer {
			get {
				return _data;
			}
		}

		public int Position {
			get {
				return _pos;
			}
			set {
				_pos = value;
			}
		}

		public int Length {
			get {
				return _byteLength * 8;
			}
		}

		public int Remaining {
			get {
				return (_byteLength * 8) - _pos;
			}
		}

		public void Skip(int length) {
			_pos += length;
		}

		public uint Peek(int length) {
			int bytePos = (_pos / 32) * 4;
			int s = _pos - (bytePos * 8);
			int e = 32 - (s + length);
			uint bufA = ReadUInt32(bytePos);

			if (e >= 0) {
				return (bufA << s) >> (s + e);
			}
			else {
				uint bufB = ReadUInt32(bytePos + 4);
				return ((bufA << s) >> (s + e)) | (bufB >> (e + 32));
			}
		}

		public uint Read(int length) {
			uint x = Peek(length);
			_pos += length;
			return x;
		}

		public void Write(uint bits, int length) {
			int bytePos = (_pos / 32) * 4;
			int s = _pos - (bytePos * 8);
			int e = 32 - (s + length);
			int zeros = 32 - length;
			uint bufA = ReadUInt32(bytePos);

			bits <<= zeros;

			if (e >= 0) {
				bufA &= ~((0xFFFFFFFF << zeros) >> s);
				bufA |= bits >> s;

				WriteUInt32(bytePos, bufA);
			}
			else {
				uint bufB = ReadUInt32(bytePos + 4);
				int aLength = (32 - s);

				bufA &= 0xFFFFFFFF << aLength;
				bufA |= bits >> s;
				bufB &= 0xFFFFFFFF >> -e;
				bufB |= bits << aLength;

				WriteUInt32(bytePos, bufA);
				WriteUInt32(bytePos + 4, bufB);
			}

			_pos += length;
		}

		public uint Copy(int length) {
			uint bits = Read(length);
			if (_copyDest != null) {
				_copyDest.Write(bits, length);
			}
			return bits;
		}

		public void CopyRemaining() {
			if (Remaining > 32) {
				int srcMod = Position % 8;
				int dstMod = _copyDest.Position % 8;

				if (srcMod == dstMod) {
					if (srcMod != 0) {
						Copy(8 - srcMod);
					}
					int bitsLeft = Remaining;
					Buffer.BlockCopy(ByteBuffer, Position / 8,
						_copyDest.ByteBuffer, _copyDest.Position / 8, bitsLeft / 8);
					_copyDest.Skip(bitsLeft);
					return;
				}
			}

			while (Remaining > 0) {
				Copy(Math.Min(Remaining, 32));
			}
		}

		public byte[] GetBytes() {
			byte[] tmp = new byte[(_pos + 7) / 8];
			Array.Copy(_data, tmp, tmp.Length);
			return tmp;
		}

		private uint ReadUInt32(int pos) {
			uint x;

			try {
				x = ((uint)_data[pos    ] << 24) |
					((uint)_data[pos + 1] << 16) |
					((uint)_data[pos + 2] <<  8) |
					((uint)_data[pos + 3]      );
			}
			catch (IndexOutOfRangeException) {
				throw new Exception("Attempted to read past the end of a bitstream.");
			}

			return x;
		}

		private void WriteUInt32(int pos, uint x) {
			try {
				_data[pos    ] = (byte)(x >> 24);
				_data[pos + 1] = (byte)(x >> 16);
				_data[pos + 2] = (byte)(x >>  8);
				_data[pos + 3] = (byte)(x      );
			}
			catch (IndexOutOfRangeException) {
				throw new Exception("Attempted to write past the end of a bitstream.");
			}
		}
	}

	class MPEG4ChunkReader {
		private byte[] _data;
		private int[,] _chunkInfo;

		public unsafe MPEG4ChunkReader(byte[] data) {
			List<int> scList = new List<int>();
			uint startCodeSignal = 0x00000100;
			uint startCodeMask = 0xFFFFFF00;

			if (BitConverter.IsLittleEndian) {
				startCodeSignal = ByteOrder.Reverse(startCodeSignal);
				startCodeMask = ByteOrder.Reverse(startCodeMask);
			}

			_data = data;

			fixed (byte* dataFixed = data) {
				byte* pData = dataFixed;
				byte* pMax = pData + (data.Length - 4);

				while (pData <= pMax) {
					if ((((uint*)pData)[0] & startCodeMask) == startCodeSignal) {
						scList.Add((int)(pData - dataFixed));
						pData += 4;
					}
					else {
						pData++;
					}
				}
			}
			scList.Add(data.Length);

			if (scList[0] != 0) {
				throw new NotMPEG4Exception("This is not a valid MPEG-4 video (startcode not found at beginning of frame).");
			}

			_chunkInfo = new int[scList.Count - 1, 2];
			for (int i = 0; i < scList.Count - 1; i++) {
				_chunkInfo[i, 0] = scList[i];
				_chunkInfo[i, 1] = scList[i + 1] - scList[i];
			}
		}

		public int Count {
			get {
				return _chunkInfo.GetLength(0);
			}
		}

		public uint StartCode(int index) {
			int chunkOffset = _chunkInfo[index, 0];
			return 0x00000100 | (uint)_data[chunkOffset + 3];
		}

		public byte[] ReadChunk(int index) {
			int chunkOffset = _chunkInfo[index, 0];
			int chunkLength = _chunkInfo[index, 1];
			byte[] chunk = new byte[chunkLength];

			Buffer.BlockCopy(_data, chunkOffset, chunk, 0, chunkLength);

			return chunk;
		}

		public int ChunkLength(int index) {
			return _chunkInfo[index, 1];
		}
	}

	struct MPEG4UserData {
		private byte[] _userData;

		public byte[] UserData {
			get {
				return _userData;
			}
			set {
				_userData = value;
			}
		}

		public byte[] UserDataWithSC {
			get {
				byte[] withSC = new byte[_userData.Length + 4];

				withSC[0] = 0x00;
				withSC[1] = 0x00;
				withSC[2] = 0x01;
				withSC[3] = 0xB2;
				Array.Copy(_userData, 0, withSC, 4, _userData.Length);

				return withSC;
			}
			set {
				byte[] withSC = value;

				_userData = new byte[withSC.Length - 4];
				Array.Copy(withSC, 4, _userData, 0, _userData.Length);
			}
		}

		public void SetString(string userDataStr) {
			_userData = Encoding.ASCII.GetBytes(userDataStr);
		}

		public override string ToString() {
			return Encoding.ASCII.GetString(_userData);
		}
	}

	struct MPEG4PAR {
		public PARType Type;
		public uint Width;
		public uint Height;

		public void SetCustomPAR(double par) {
			BestByteFraction(par, out Width, out Height);
			if ((Width != 0) && (Height != 0)) {
				Type = PARType.Custom;
			}
			else {
				Type = PARType.VGA_1_1;
			}
		}

		private void BestByteFraction(double x, out uint numerator, out uint denominator) {
			uint n, d, nBest, dBest, div;
			double err, errBest;

			nBest = 0;
			dBest = 0;
			errBest = Double.PositiveInfinity;
			if (x < 0) x = 0;

			for (d = 1; d <= 255; d++) {
				n = Convert.ToUInt32((double)d * x);
				if (n <= 255) {
					err = Math.Abs( ((double)n / (double)d) - x );
					if (err < errBest) {
						nBest = n;
						dBest = d;
						errBest = err;
					}
				}
			}

			div = GCD(nBest, dBest);
			numerator = nBest / div;
			denominator = dBest / div;
		}

		private uint GCD(uint a, uint b) {
			uint r;

			while (b != 0) {
				r = a % b;
				a = b;
				b = r;
			}

			return a;
		}
	}

	class NotMPEG4Exception : Exception {
		public NotMPEG4Exception(string message) : base(message) {
		}
	}

	enum StartCode : uint {
		VideoObjectLayer = 0x00000120,
		UserData         = 0x000001B2,
		VideoObjectPlane = 0x000001B6
	}

	enum PARType : uint {
		VGA_1_1   = 1,
		PAL_4_3   = 2,
		NTSC_4_3  = 3,
		PAL_16_9  = 4,
		NTSC_16_9 = 5,
		Custom    = 15
	}

	enum VOPType : uint {
		I_VOP   = 0,
		P_VOP   = 1,
		B_VOP   = 2,
		S_VOP   = 3,
		N_VOP   = 4,
		N_VOP_D = 5
	}

	enum VOLShape : uint {
		Rectangular = 0,
		Binary      = 1,
		BinaryOnly  = 2,
		Grayscale   = 3
	}

	enum Sprite : uint {
		None   = 0,
		Static = 1,
		GMC    = 2
	}

	[StructLayout(LayoutKind.Auto)]
	struct VOLInfo {
		public uint ver_id;
		public uint aspect_ratio;
		public uint par_width;
		public uint par_height;
		public bool low_delay;
		public uint shape;
		public uint time_inc_res;
		public int  time_inc_bits;
		public bool interlaced;
		public uint sprite_enable;
		public uint sprite_warping_points;
		public bool mpeg_quant;
		public bool load_intra_quant_mat;
		public bool load_inter_quant_mat;
		public uint[] intra_quant_mat;
		public uint[] inter_quant_mat;
		public bool quarterpel;
		public bool newpred_enable;
		public bool reduced_resolution_enable;
	}

	[StructLayout(LayoutKind.Auto)]
	struct VOPInfo {
		public int frame_num;
		public uint coding_type;
		public bool coded;
		public VOPType vop_type;
		public bool is_reference_vop;
		public uint modulo_time_base;
		public uint time_inc;
		public long disp_time_ticks;
		public bool top_field_first;
		public uint warping_points_used;
	}
}