#pragma once

#include <map>
#include <vector>
#include <list>
#include <string>
#include <limits>
#include <fstream>
#include <boost/lexical_cast.hpp>
#include <boost/format.hpp>
#include <boost/filesystem.hpp>
#include <boost/foreach.hpp>
#include <boost/scoped_ptr.hpp>

#include "General.hpp"
#include "WAVFile.hpp"


namespace JDP {

  

struct FractionUInt32
{
  uint32_t N;
  uint32_t D;

  FractionUInt32(void * = NULL)
   : N(std::numeric_limits<uint32_t>::max()), D(std::numeric_limits<uint32_t>::max())
  {}
  
  FractionUInt32(uint32_t n, uint32_t d)
   : N(n), D(d)
  {}
    
  operator bool () const
  {
    return N != std::numeric_limits<uint32_t>::max() || D != std::numeric_limits<uint32_t>::max();
  }

  double ToDouble() const
  {
    return double(N) / double(D);
  }

  void Reduce()
  {
    uint32_t gcd = GCD(N, D);
    N /= gcd;
    D /= gcd;
  }

  std::string ToString() const
  {
    return ToString(true);
  }

  std::string ToString(bool full) const
  {
    if (full)
      return boost::lexical_cast<std::string>(ToDouble()) + " (" + boost::lexical_cast<std::string>(N) + "/" + boost::lexical_cast<std::string>(D) + ")";
    else
      return (boost::format("%.4f") % ToDouble()).str();
  }
  
  private:
  uint32_t GCD(uint32_t a, uint32_t b)
  {
    uint32_t r;

    while (b != 0)
    {
      r = a % b;
      a = b;
      b = r;
    }

    return a;
  }  
};

  
  
struct IAudioWriter
{
  std::string Path;
  IAudioWriter(std::string const & path) : Path(path) {}
  virtual void WriteChunk(std::vector<byte> const &, uint32_t) {};
  virtual void Finish() {};  
  virtual ~IAudioWriter() {};
};


struct IVideoWriter
{
  std::string Path;
  IVideoWriter(std::string const & path) : Path(path) {}
  virtual void WriteChunk(std::vector<byte> const &, uint32_t, int) {};
  virtual void Finish(FractionUInt32) {};
  virtual ~IVideoWriter() {};
};


struct DummyAudioWriter : public IAudioWriter
{
  DummyAudioWriter() : IAudioWriter("") {}
  virtual void WriteChunk(std::vector<byte> const &, uint32_t) {}
  virtual void Finish() {}
};

struct DummyVideoWriter : public IVideoWriter
{
  DummyVideoWriter() : IVideoWriter("") {}
  virtual void WriteChunk(std::vector<byte> const &, uint32_t, int) {}
  virtual void Finish(FractionUInt32) {}
};



struct MP3Writer : public IAudioWriter
{
  std::string _path;
  std::fstream _fs;
  std::vector<std::string> & _warnings;
  std::vector<std::vector<byte> > _chunkBuffer;
  std::vector<uint32_t> _frameOffsets;
  uint32_t _totalFrameLength;
  bool _isVBR;
  bool _delayWrite;
  bool _hasVBRHeader;
  bool _writeVBRHeader;
  int _firstBitRate;
  int _mpegVersion;
  int _sampleRate;
  int _channelMode;
  uint32_t _firstFrameHeader;

  MP3Writer(std::string const & path, std::vector<std::string> & warnings)
   : IAudioWriter(path),
     _path(path),
     _warnings(warnings),
     _delayWrite(true)
  {
    _fs.open(path.c_str(), std::ios_base::out | std::ios_base::binary);
  }

  virtual void WriteChunk(std::vector<byte> const & chunk, uint32_t)
  {
    _chunkBuffer.push_back(chunk);
    ParseMP3Frames(chunk);
    if (_delayWrite && _totalFrameLength >= 65536)
    {
      _delayWrite = false;
    }
    if (!_delayWrite)
    {
      Flush();
    }
  }

  virtual void Finish()
  {
    Flush();
    if (_writeVBRHeader)
    {
      _fs.seekp(0, std::ios_base::beg);
      WriteVBRHeader(false);
    }
    _fs.close();
  }

  void Flush()
  {
    BOOST_FOREACH(std::vector<byte> const & chunk, _chunkBuffer)
    {
      Stream::Write(_fs, chunk);
    }
    _chunkBuffer.clear();
  }

  private:
  void ParseMP3Frames(std::vector<byte> const & buff)
  {
    int MPEG1BitRate[] = { 0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0 };
    int MPEG2XBitRate[] = { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0 };
    int MPEG1SampleRate[] = { 44100, 48000, 32000, 0 };
    int MPEG20SampleRate[] = { 22050, 24000, 16000, 0 };
    int MPEG25SampleRate[] = { 11025, 12000, 8000, 0 };

    int offset = 0;
    int length = buff.size();

    while (length >= 4)
    {
      uint64_t header;
      int mpegVersion, layer, bitRate, sampleRate, padding, channelMode;
      int frameLen;

      header = (uint64_t)BitConverterBE::ToUInt32(buff, offset) << 32;
      if (BitHelper::Read(header, 11) != 0x7FF)
      {
        break;
      }
      mpegVersion = BitHelper::Read(header, 2);
      layer = BitHelper::Read(header, 2);
      BitHelper::Read(header, 1);
      bitRate = BitHelper::Read(header, 4);
      sampleRate = BitHelper::Read(header, 2);
      padding = BitHelper::Read(header, 1);
      BitHelper::Read(header, 1);
      channelMode = BitHelper::Read(header, 2);

      if ((mpegVersion == 1) || (layer != 1) || (bitRate == 0) || (bitRate == 15) || (sampleRate == 3))
      {
        break;
      }

      bitRate = ((mpegVersion == 3) ? MPEG1BitRate[bitRate] : MPEG2XBitRate[bitRate]) * 1000;

      if (mpegVersion == 3)
        sampleRate = MPEG1SampleRate[sampleRate];
      else if (mpegVersion == 2)
        sampleRate = MPEG20SampleRate[sampleRate];
      else
        sampleRate = MPEG25SampleRate[sampleRate];

      frameLen = GetFrameLength(mpegVersion, bitRate, sampleRate, padding);
      if (frameLen > length)
      {
        break;
      }

      bool isVBRHeaderFrame = false;
      if (_frameOffsets.size() == 0)
      {
        // Check for an existing VBR header just to be safe (I haven't seen any in FLVs)
        int o = offset + GetFrameDataOffset(mpegVersion, channelMode);
        if (BitConverterBE::ToUInt32(buff, o) == 0x58696E67)
        { // "Xing"
          isVBRHeaderFrame = true;
          _delayWrite = false;
          _hasVBRHeader = true;
        }
      }

      if (isVBRHeaderFrame)
      {
      }
      else if (_firstBitRate == 0)
      {
        _firstBitRate = bitRate;
        _mpegVersion = mpegVersion;
        _sampleRate = sampleRate;
        _channelMode = channelMode;
        _firstFrameHeader = BitConverterBE::ToUInt32(buff, offset);
      }
      else if (!_isVBR && (bitRate != _firstBitRate))
      {
        _isVBR = true;
        if (_hasVBRHeader)
        {
        }
        else if (_delayWrite)
        {
          WriteVBRHeader(true);
          _writeVBRHeader = true;
          _delayWrite = false;
        }
        else
        {
          _warnings.push_back("Detected VBR too late, cannot add VBR header.");
        }
      }

      _frameOffsets.push_back(_totalFrameLength + (uint32_t)offset);

      offset += frameLen;
      length -= frameLen;
    }

    _totalFrameLength += (uint32_t)buff.size();
  }

  void WriteVBRHeader(bool isPlaceholder)
  {
    std::vector<byte> buff(GetFrameLength(_mpegVersion, 64000, _sampleRate, 0));
    if (!isPlaceholder)
    {
      uint32_t header = _firstFrameHeader;
      int dataOffset = GetFrameDataOffset(_mpegVersion, _channelMode);
      header &= 0xFFFF0DFF; // Clear bitrate and padding fields
      header |= 0x00010000; // Set protection bit (indicates that CRC is NOT present)
      header |= (uint32_t)((_mpegVersion == 3) ? 5 : 8) << 12; // 64 kbit/sec
      General::CopyBytes(buff, 0, BitConverterBE::GetBytes(header));
      General::CopyBytes(buff, dataOffset, BitConverterBE::GetBytes((uint32_t)0x58696E67)); // "Xing"
      General::CopyBytes(buff, dataOffset + 4, BitConverterBE::GetBytes((uint32_t)0x7)); // Flags
      General::CopyBytes(buff, dataOffset + 8, BitConverterBE::GetBytes((uint32_t)_frameOffsets.size())); // Frame count
      General::CopyBytes(buff, dataOffset + 12, BitConverterBE::GetBytes((uint32_t)_totalFrameLength)); // File length
      for (int i = 0; i < 100; i++)
      {
        int frameIndex = (int)((i / 100.0) * _frameOffsets.size());
        buff[dataOffset + 16 + i] = (byte)((_frameOffsets[frameIndex] / (double)_totalFrameLength) * 256.0);
      }
    }
    Stream::Write(_fs, buff);
  }

  int GetFrameLength(int mpegVersion, int bitRate, int sampleRate, int padding)
  {
    return ((mpegVersion == 3) ? 144 : 72) * bitRate / sampleRate + padding;
  }

  int GetFrameDataOffset(int mpegVersion, int channelMode)
  {
    return 4 + ((mpegVersion == 3) ?
      ((channelMode == 3) ? 17 : 32) :
      ((channelMode == 3) ?  9 : 17));
  }
};


struct OggPacket
{
  uint64_t GranulePosition;
  std::vector<byte> Data;
  
  OggPacket() : GranulePosition(0), Data() {}
  OggPacket(uint64_t const & gpos, std::vector<byte> const & data)
   : GranulePosition(gpos),
     Data(data)
  {}
};


struct SpeexWriter : public IAudioWriter
{
  static std::string const _vendorString;
  static uint32_t const _sampleRate = 16000;
  static uint32_t const _msPerFrame = 20;
  static uint32_t const _samplesPerFrame = _sampleRate / (1000 / _msPerFrame);
  static int const _targetPageDataSize = 4096;

  std::fstream _fs;
  int _serialNumber;
  std::vector<OggPacket> _packetList;
  int _packetListDataSize;
  std::vector<byte> _pageBuff;
  int _pageBuffOffset;
  uint32_t _pageSequenceNumber;
  uint64_t _granulePosition;

  SpeexWriter(std::string const & path, int serialNumber)
   : IAudioWriter(path),
     _serialNumber(serialNumber),
     _packetListDataSize(0),
     _pageBuffOffset(0),
     _pageSequenceNumber(2), // First audio packet
     _granulePosition(0)
  {
    _fs.open(path.c_str(), std::ios_base::out | std::ios_base::binary);
    _fs.seekp((28 + 80) + (28 + 8 + _vendorString.size()), std::ios_base::beg); // Speex header + Vorbis comment    
    _pageBuff.resize(27 + 255 + _targetPageDataSize + 254); // Header + max segment table + target data size + extra segment    
  }

  virtual void WriteChunk(std::vector<byte> const & chunk, uint32_t)
  {
    int subModeSizes[] = { 0, 43, 119, 160, 220, 300, 364, 492, 79 };
    int wideBandSizes[] = { 0, 36, 112, 192, 352 };
    int inBandSignalSizes[] = { 1, 1, 4, 4, 4, 4, 4, 4, 8, 8, 16, 16, 32, 32, 64, 64 };
    int frameStart = -1;
    int frameEnd = 0;
    int offset = 0;
    int length = chunk.size() * 8;
    int x;

    while (length - offset >= 5)
    {
      x = BitHelper::Read(chunk, offset, 1);
      if (x != 0)
      {
        // wideband frame
        x = BitHelper::Read(chunk, offset, 3);
        if (x < 1 || x > 4) goto Error;
        offset += wideBandSizes[x] - 4;
      }
      else
      {
        x = BitHelper::Read(chunk, offset, 4);
        if (x >= 1 && x <= 8)
        {
          // narrowband frame
          if (frameStart != -1)
          {
            WriteFramePacket(chunk, frameStart, frameEnd);
          }
          frameStart = frameEnd;
          offset += subModeSizes[x] - 5;
        }
        else if (x == 15)
        {
          // terminator
          break;
        }
        else if (x == 14)
        {
          // in-band signal
          if (length - offset < 4) goto Error;
          x = BitHelper::Read(chunk, offset, 4);
          offset += inBandSignalSizes[x];
        }
        else if (x == 13)
        {
          // custom in-band signal
          if (length - offset < 5) goto Error;
          x = BitHelper::Read(chunk, offset, 5);
          offset += x * 8;
        }
        else goto Error;
      }
      frameEnd = offset;
    }
    if (offset > length) goto Error;

    if (frameStart != -1)
    {
      WriteFramePacket(chunk, frameStart, frameEnd);
    }

    return;

  Error:
    throw std::runtime_error("Invalid Speex data.");
  }

  virtual void Finish()
  {
    WritePage();
    FlushPage(true);
    _fs.seekp(0, std::ios_base::beg);
    _pageSequenceNumber = 0;
    _granulePosition = 0;
    WriteSpeexHeaderPacket();
    WriteVorbisCommentPacket();
    FlushPage(false);
    _fs.close();
  }

  private:
  void WriteFramePacket(std::vector<byte> const & data, int startBit, int endBit)
  {
    int lengthBits = endBit - startBit;
    std::vector<byte> frame = BitHelper::CopyBlock(data, startBit, lengthBits);
    if (lengthBits % 8 != 0)
    {
      frame[frame.size() - 1] |= (byte)(0xFF >> ((lengthBits % 8) + 1)); // padding
    }
    AddPacket(frame, _samplesPerFrame, true);
  }

  void WriteSpeexHeaderPacket()
  {
    std::vector<byte> data(80);
    General::CopyBytes(data, 0, Encoding::ASCII::GetBytes("Speex   ")); // speex_string
    General::CopyBytes(data, 8, Encoding::ASCII::GetBytes("unknown")); // speex_version
    data[28] = 1; // speex_version_id
    data[32] = 80; // header_size
    General::CopyBytes(data, 36, BitConverterLE::GetBytes((uint32_t)_sampleRate)); // rate
    data[40] = 1; // mode (e.g. narrowband, wideband)
    data[44] = 4; // mode_bitstream_version
    data[48] = 1; // nb_channels
    General::CopyBytes(data, 52, BitConverterLE::GetBytes(((uint32_t)-1))); // bitrate
    General::CopyBytes(data, 56, BitConverterLE::GetBytes((uint32_t)_samplesPerFrame)); // frame_size
    data[60] = 0; // vbr
    data[64] = 1; // frames_per_packet
    AddPacket(data, 0, false);
  }

  void WriteVorbisCommentPacket()
  {
    std::vector<byte> vendorStringBytes = Encoding::ASCII::GetBytes(_vendorString);
    std::vector<byte> data(8 + vendorStringBytes.size());
    data[0] = (byte)vendorStringBytes.size();
    General::CopyBytes(data, 4, vendorStringBytes);
    AddPacket(data, 0, false);
  }

  void AddPacket(std::vector<byte> const & data, uint32_t sampleLength, bool delayWrite)
  {    
    if (data.size() >= 255)
    {
      throw std::runtime_error("Packet exceeds maximum size.");
    }
    _granulePosition += sampleLength;
    _packetList.push_back(OggPacket(_granulePosition, data));    
    _packetListDataSize += data.size();
    if (!delayWrite || (_packetListDataSize >= _targetPageDataSize) || (_packetList.size() == 255))
    {
      WritePage();
    }
  }

  void WritePage()
  {
    if (_packetList.size() == 0) return;
    FlushPage(false);
    WriteToPage(BitConverterBE::GetBytes(0x4F676753U), 0, 4); // "OggS"
    WriteToPage((byte)0); // Stream structure version
    WriteToPage((byte)((_pageSequenceNumber == 0) ? 0x02 : 0)); // Page flags
    WriteToPage((uint64_t)_packetList[_packetList.size() - 1].GranulePosition); // Position in samples
    WriteToPage((uint32_t)_serialNumber); // Stream serial number
    WriteToPage((uint32_t)_pageSequenceNumber); // Page sequence number
    WriteToPage((uint32_t)0); // Checksum
    WriteToPage((byte)_packetList.size()); // Page segment count
    
    BOOST_FOREACH(OggPacket const & packet, _packetList)
    {
      WriteToPage((byte)packet.Data.size());
    }
    BOOST_FOREACH(OggPacket const & packet, _packetList)
    {
      WriteToPage(packet.Data, 0, packet.Data.size());
    }
    _packetList.clear();
    _packetListDataSize = 0;
    _pageSequenceNumber++;
  }

  void FlushPage(bool isLastPage)
  {
    if (_pageBuffOffset == 0) return;
    if (isLastPage) _pageBuff[5] |= 0x04;
    uint32_t crc = OggCRC::Calculate(_pageBuff, 0, _pageBuffOffset);
    General::CopyBytes(_pageBuff, 22, BitConverterLE::GetBytes(crc));
    Stream::Write(_fs, _pageBuff, 0, _pageBuffOffset);
    _pageBuffOffset = 0;
  }

  void WriteToPage(std::vector<byte> const & data, int offset, int length)
  {
    memcpy(&_pageBuff[0] + _pageBuffOffset, &data[0] + offset, length);
    _pageBuffOffset += length;
  }

  void WriteToPage(byte data)
  {
    std::vector<byte> tmp(1);
    tmp[0] = data;
    WriteToPage(tmp, 0, 1);
  }

  void WriteToPage(uint32_t data)
  {
    WriteToPage(BitConverterLE::GetBytes(data), 0, 4);
  }

  void WriteToPage(uint64_t data)
  {
    WriteToPage(BitConverterLE::GetBytes(data), 0, 8);
  }

};





struct AACWriter : public IAudioWriter
{
  std::fstream _fs;
  int _aacProfile;
  int _sampleRateIndex;
  int _channelConfig;

  AACWriter(std::string const & path)
   : IAudioWriter(path)
  {
    _fs.open(path.c_str(), std::ios_base::out | std::ios_base::binary);
  }

  virtual void WriteChunk(std::vector<byte> const & chunk, uint32_t)
  {
    if (chunk.size() < 1) return;

    if (chunk[0] == 0)
    { // Header
      if (chunk.size() < 3) return;

      uint64_t bits = (uint64_t)BitConverterBE::ToUInt16(chunk, 1) << 48;

      _aacProfile = BitHelper::Read(bits, 5) - 1;
      _sampleRateIndex = BitHelper::Read(bits, 4);
      _channelConfig = BitHelper::Read(bits, 4);

      if ((_aacProfile < 0) || (_aacProfile > 3))
        throw std::runtime_error("Unsupported AAC profile.");
      if (_sampleRateIndex > 12)
        throw std::runtime_error("Invalid AAC sample rate index.");
      if (_channelConfig > 6)
        throw std::runtime_error("Invalid AAC channel configuration.");
    }
    else
    { // Audio data
      int dataSize = chunk.size() - 1;
      uint64_t bits = 0;

      // Reference: WriteADTSHeader from FAAC's bitstream.c

      BitHelper::Write(bits, 12, 0xFFF);
      BitHelper::Write(bits,  1, 0);
      BitHelper::Write(bits,  2, 0);
      BitHelper::Write(bits,  1, 1);
      BitHelper::Write(bits,  2, _aacProfile);
      BitHelper::Write(bits,  4, _sampleRateIndex);
      BitHelper::Write(bits,  1, 0);
      BitHelper::Write(bits,  3, _channelConfig);
      BitHelper::Write(bits,  1, 0);
      BitHelper::Write(bits,  1, 0);
      BitHelper::Write(bits,  1, 0);
      BitHelper::Write(bits,  1, 0);
      BitHelper::Write(bits, 13, 7 + dataSize);
      BitHelper::Write(bits, 11, 0x7FF);
      BitHelper::Write(bits,  2, 0);

      std::vector<byte> tmp = BitConverterBE::GetBytes(bits);
      Stream::Write(_fs, tmp, 1, 7);
      Stream::Write(_fs, chunk, 1, dataSize);
    }
  }

  virtual void Finish()
  {
    _fs.close();
  }

};



struct RawH264Writer : public IVideoWriter
{
  static std::vector<byte> const _startCode;

  std::fstream _fs;
  int _nalLengthSize;

  RawH264Writer(std::string const & path)
   : IVideoWriter(path)
  {
    _fs.open(path.c_str(), std::ios_base::out | std::ios_base::binary);
  }

  virtual void WriteChunk(std::vector<byte> const & chunk, uint32_t, int)
  {
    if (chunk.size() < 4) return;

    // Reference: decode_frame from libavcodec's h264.c

    if (chunk[0] == 0)
    { // Headers
      if (chunk.size() < 10) return;

      size_t offset;
      int spsCount, ppsCount;

      offset = 8;
      _nalLengthSize = (chunk[offset++] & 0x03) + 1;
      spsCount = chunk[offset++] & 0x1F;
      ppsCount = -1;

      while (offset <= chunk.size() - 2)
      {
        if ((spsCount == 0) && (ppsCount == -1))
        {
          ppsCount = chunk[offset++];
          continue;
        }

        if (spsCount > 0) spsCount--;
        else if (ppsCount > 0) ppsCount--;
        else break;

        int len = (int)BitConverterBE::ToUInt16(chunk, offset);
        offset += 2;
        if (offset + len > chunk.size()) break;
        Stream::Write(_fs, _startCode);
        Stream::Write(_fs, chunk, offset, len);
        offset += len;
      }
    }
    else
    { // Video data
      size_t offset = 4;

      if (_nalLengthSize != 2)
      {
        _nalLengthSize = 4;
      }

      while (offset <= chunk.size() - _nalLengthSize)
      {
        int len = (_nalLengthSize == 2) ?
          (int)BitConverterBE::ToUInt16(chunk, offset) :
          (int)BitConverterBE::ToUInt32(chunk, offset);
        offset += _nalLengthSize;
        if (offset + len > chunk.size()) break;
        Stream::Write(_fs, _startCode);
        Stream::Write(_fs, chunk, offset, len);
        offset += len;
      }
    }
  }

  virtual void Finish(FractionUInt32)
  {
    _fs.close();
  }

};



struct WAVWriter : public IAudioWriter
{
  boost::scoped_ptr<WAVTools::WAVWriter> _wr;
  int blockAlign;

  WAVWriter(std::string const & path, int bitsPerSample, int channelCount, int sampleRate)
   : IAudioWriter(path),
     _wr(new WAVTools::WAVWriter(path, bitsPerSample, channelCount, sampleRate)),
     blockAlign((bitsPerSample / 8) * channelCount)
  {}

  virtual void WriteChunk(std::vector<byte> const & chunk, uint32_t)
  {
    _wr->Write(&chunk[0], chunk.size() / blockAlign);
  }

  virtual void Finish()
  {
    _wr->Close();
  }

};



struct AVIWriter : public IVideoWriter
{
  std::fstream _bw;
  int _codecID;
  int _width, _height, _frameCount;
  uint32_t _moviDataSize, _indexChunkSize;
  std::vector<uint32_t> _index;
  bool _isAlphaWriter;
  boost::scoped_ptr<AVIWriter> _alphaWriter;
  std::vector<std::string> & _warnings;

  // Chunk:          Off:  Len:
  //
  // RIFF AVI          0    12
  //   LIST hdrl      12    12
  //     avih         24    64
  //     LIST strl    88    12
  //       strh      100    64
  //       strf      164    48
  //   LIST movi     212    12
  //     (frames)    224   ???
  //   idx1          ???   ???
  
  private:
  void WriteFourCC(std::string const & fourCC)
  {
    std::vector<byte> bytes = Encoding::ASCII::GetBytes(fourCC);
    if (bytes.size() != 4)
    {
      throw std::runtime_error("Invalid FourCC length.");
    }
    Stream::Write(_bw, bytes);
  }
  
  std::string CodecFourCC() const
  {
    if (_codecID == 2)
    {
      return "FLV1";
    }
    if ((_codecID == 4) || (_codecID == 5))
    {
      return "VP6F";
    }
    return "NULL";
  }

  public:
  AVIWriter(std::string const & path, int codecID, std::vector<std::string> & warnings, bool isAlphaWriter = false)
   : IVideoWriter(path),
     _codecID(codecID),
     _isAlphaWriter(isAlphaWriter),
     _alphaWriter(NULL),
     _warnings(warnings)
  {
    if ((codecID != 2) && (codecID != 4) && (codecID != 5))
    {
      throw std::runtime_error("Unsupported video codec.");
    }

    _bw.open(path.c_str(), std::ios_base::out | std::ios_base::binary);

    if ((codecID == 5) && !_isAlphaWriter)
    {
      _alphaWriter.reset(new AVIWriter(path.substr(0, path.length() - 4) + ".alpha.avi", codecID, warnings, true));
    }

    WriteFourCC("RIFF");
    Binary::Write(_bw, (uint32_t)0); // chunk size
    WriteFourCC("AVI ");

    WriteFourCC("LIST");
    Binary::Write(_bw, (uint32_t)192);
    WriteFourCC("hdrl");

    WriteFourCC("avih");
    Binary::Write(_bw, (uint32_t)56);
    Binary::Write(_bw, (uint32_t)0);
    Binary::Write(_bw, (uint32_t)0);
    Binary::Write(_bw, (uint32_t)0);
    Binary::Write(_bw, (uint32_t)0x10);
    Binary::Write(_bw, (uint32_t)0); // frame count
    Binary::Write(_bw, (uint32_t)0);
    Binary::Write(_bw, (uint32_t)1);
    Binary::Write(_bw, (uint32_t)0);
    Binary::Write(_bw, (uint32_t)0); // width
    Binary::Write(_bw, (uint32_t)0); // height
    Binary::Write(_bw, (uint32_t)0);
    Binary::Write(_bw, (uint32_t)0);
    Binary::Write(_bw, (uint32_t)0);
    Binary::Write(_bw, (uint32_t)0);

    WriteFourCC("LIST");
    Binary::Write(_bw, (uint32_t)116);
    WriteFourCC("strl");

    WriteFourCC("strh");
    Binary::Write(_bw, (uint32_t)56);
    WriteFourCC("vids");
    WriteFourCC(CodecFourCC());
    Binary::Write(_bw, (uint32_t)0);
    Binary::Write(_bw, (uint32_t)0);
    Binary::Write(_bw, (uint32_t)0);
    Binary::Write(_bw, (uint32_t)0); // frame rate denominator
    Binary::Write(_bw, (uint32_t)0); // frame rate numerator
    Binary::Write(_bw, (uint32_t)0);
    Binary::Write(_bw, (uint32_t)0); // frame count
    Binary::Write(_bw, (uint32_t)0);
    Binary::Write(_bw, (int32_t)-1);
    Binary::Write(_bw, (uint32_t)0);
    Binary::Write(_bw, (uint16_t)0);
    Binary::Write(_bw, (uint16_t)0);
    Binary::Write(_bw, (uint16_t)0); // width
    Binary::Write(_bw, (uint16_t)0); // height
    
    WriteFourCC("strf");
    Binary::Write(_bw, (uint32_t)40);
    Binary::Write(_bw, (uint32_t)40);
    Binary::Write(_bw, (uint32_t)0); // width
    Binary::Write(_bw, (uint32_t)0); // height
    Binary::Write(_bw, (uint16_t)1);
    Binary::Write(_bw, (uint16_t)24);
    
    WriteFourCC(CodecFourCC());
    Binary::Write(_bw, (uint32_t)0); // biSizeImage
    Binary::Write(_bw, (uint32_t)0);
    Binary::Write(_bw, (uint32_t)0);
    Binary::Write(_bw, (uint32_t)0);
    Binary::Write(_bw, (uint32_t)0);
    
    WriteFourCC("LIST");
    Binary::Write(_bw, (uint32_t)0); // chunk size
    WriteFourCC("movi");
  }

  virtual void WriteChunk(std::vector<byte> const & chunk, uint32_t timeStamp, int frameType)
  {
    int offset, len;

    offset = 0;
    len = chunk.size();
    if (_codecID == 4)
    {
      offset = 1;
      len -= 1;
    }
    if (_codecID == 5)
    {
      offset = 4;
      if (len >= 4)
      {
        int alphaOffset = (int)BitConverterBE::ToUInt32(chunk, 0) & 0xFFFFFF;
        if (!_isAlphaWriter)
        {
          len = alphaOffset;
        }
        else
        {
          offset += alphaOffset;
          len -= offset;
        }
      }
      else
      {
        len = 0;
      }
    }
    len = std::max(len, 0);
    len = std::min(size_t(len), chunk.size() - offset);

    _index.push_back((frameType == 1) ? (uint32_t)0x10 : (uint32_t)0);
    _index.push_back(_moviDataSize + 4);
    _index.push_back((uint32_t)len);

    if ((_width == 0) && (_height == 0))
    {
      GetFrameSize(chunk);
    }

    WriteFourCC("00dc");
    Binary::Write(_bw, len);
    Stream::Write(_bw, chunk, offset, len);
    
    if ((len % 2) != 0)
    {
      Binary::Write(_bw, (byte)0);
      len++;
    }
    _moviDataSize += (uint32_t)len + 8;
    _frameCount++;

    if (_alphaWriter)
    {
      _alphaWriter->WriteChunk(chunk, timeStamp, frameType);
    }
  }

  private:
  void GetFrameSize(std::vector<byte> const & chunk)
  {
    if (_codecID == 2)
    {
      // Reference: flv_h263_decode_picture_header from libavcodec's h263.c

      if (chunk.size() < 10) return;

      if ((chunk[0] != 0) || (chunk[1] != 0))
      {
        return;
      }

      uint64_t x = BitConverterBE::ToUInt64(chunk, 2);
      int format;

      if (BitHelper::Read(x, 1) != 1)
      {
        return;
      }
      BitHelper::Read(x, 5);
      BitHelper::Read(x, 8);

      format = BitHelper::Read(x, 3);
      switch (format)
      {
        case 0:
          _width = BitHelper::Read(x, 8);
          _height = BitHelper::Read(x, 8);
          break;
        case 1:
          _width = BitHelper::Read(x, 16);
          _height = BitHelper::Read(x, 16);
          break;
        case 2:
          _width = 352;
          _height = 288;
          break;
        case 3:
          _width = 176;
          _height = 144;
          break;
        case 4:
          _width = 128;
          _height = 96;
          break;
        case 5:
          _width = 320;
          _height = 240;
          break;
        case 6:
          _width = 160;
          _height = 120;
          break;
        default:
          return;
      }
    }
    else if ((_codecID == 4) || (_codecID == 5))
    {
      // Reference: vp6_parse_header from libavcodec's vp6.c

      int skip = (_codecID == 4) ? 1 : 4;
      if (int(chunk.size()) < (skip + 8)) return;
      uint64_t x = BitConverterBE::ToUInt64(chunk, skip);

      int deltaFrameFlag = BitHelper::Read(x, 1);
      int separatedCoeffFlag = BitHelper::Read(x, 1);
      int filterHeader = BitHelper::Read(x, 2);

      if (deltaFrameFlag != 0)
      {
        return;
      }
      if ((separatedCoeffFlag != 0) || (filterHeader == 0))
      {
        BitHelper::Read(x, 16);
      }

      _height = BitHelper::Read(x, 8) * 16;
      _width = BitHelper::Read(x, 8) * 16;

      // chunk[0] contains the width and height (4 bits each, respectively) that should
      // be cropped off during playback, which will be non-zero if the encoder padded
      // the frames to a macroblock boundary.  But if you use this adjusted size in the
      // AVI header, DirectShow seems to ignore it, and it can cause stride or chroma
      // alignment problems with VFW if the width/height aren't multiples of 4.
      if (!_isAlphaWriter)
      {
        int cropX = chunk[0] >> 4;
        int cropY = chunk[0] & 0x0F;
        if (((cropX != 0) || (cropY != 0)) && !_isAlphaWriter)
        {
          _warnings.push_back((boost::format("Suggested cropping: %d pixels from right, %d pixels from bottom.") % cropX % cropY).str());
        }
      }
    }
  }

  void WriteIndexChunk()
  {
    uint32_t indexDataSize = (uint32_t)_frameCount * 16;

    WriteFourCC("idx1");
    Binary::Write(_bw, indexDataSize);

    for (int i = 0; i < _frameCount; i++)
    {
      WriteFourCC("00dc");
      Binary::Write(_bw, _index[(i * 3) + 0]);
      Binary::Write(_bw, _index[(i * 3) + 1]);
      Binary::Write(_bw, _index[(i * 3) + 2]);
    }

    _indexChunkSize = indexDataSize + 8;
  }

  public:
  virtual void Finish(FractionUInt32 averageFrameRate)
  {
    WriteIndexChunk();

    _bw.seekp(4, std::ios_base::beg);
    Binary::Write(_bw, (uint32_t)(224 + _moviDataSize + _indexChunkSize - 8));
    
    _bw.seekp(24 + 8, std::ios_base::beg);
    Binary::Write(_bw, (uint32_t)0);
    _bw.seekp(12, std::ios_base::cur);
    Binary::Write(_bw, (uint32_t)_frameCount);
    _bw.seekp(12, std::ios_base::cur);
    Binary::Write(_bw, (uint32_t)_width);
    Binary::Write(_bw, (uint32_t)_height);
    
    _bw.seekp(100 + 28, std::ios_base::beg);
    Binary::Write(_bw, (uint32_t)averageFrameRate.D);
    Binary::Write(_bw, (uint32_t)averageFrameRate.N);
    _bw.seekp(4, std::ios_base::cur);
    Binary::Write(_bw, (uint32_t)_frameCount);
    _bw.seekp(16, std::ios_base::cur);
    Binary::Write(_bw, (uint16_t)_width);
    Binary::Write(_bw, (uint16_t)_height);

    _bw.seekp(164 + 12, std::ios_base::beg);
    Binary::Write(_bw, (uint32_t)_width);
    Binary::Write(_bw, (uint32_t)_height);
    _bw.seekp(8, std::ios_base::cur);
    Binary::Write(_bw, (uint32_t)(_width * _height * 6));

    _bw.seekp(212 + 4, std::ios_base::beg);
    Binary::Write(_bw, (uint32_t)(_moviDataSize + 4));

    _bw.close();

    if (_alphaWriter)
    {
      _alphaWriter->Finish(averageFrameRate);
      _alphaWriter.reset();
    }
  }

};



struct TimeCodeWriter
{
  std::string Path;
  std::fstream _sw;

  TimeCodeWriter(std::string const & path)
   : Path(path)
  {
    if (!path.empty())
    {
      _sw.open(path.c_str(), std::ios_base::out);
      _sw << "# timecode format v2" << std::endl;
    }
  }

  void Write(uint32_t timeStamp)
  {
    if (_sw.is_open())
      _sw << boost::lexical_cast<std::string>(timeStamp) << std::endl;
  }

  void Finish()
  {
    _sw.close();
  }

};




struct FLVFile
{
  static std::vector<std::string> const _outputExtensions;

  std::string _inputPath, _outputDirectory, _outputPathBase;
  OverwriteDelegate _overwrite;
  std::fstream _fs;
  int64_t _fileOffset, _fileLength;
  boost::scoped_ptr<IAudioWriter> _audioWriter;
  boost::scoped_ptr<IVideoWriter> _videoWriter;
  boost::scoped_ptr<TimeCodeWriter> _timeCodeWriter;
  std::vector<uint32_t> _videoTimeStamps;
  bool _extractAudio, _extractVideo, _extractTimeCodes;
  bool _extractedAudio, _extractedVideo, _extractedTimeCodes;
  FractionUInt32 _averageFrameRate, _trueFrameRate;
  std::vector<std::string> _warnings;

  FLVFile(std::string const & path)
   : _inputPath(path),
     _outputDirectory(boost::filesystem::path(path).parent_path().string()),
     _fileOffset(0),
     _fileLength(boost::filesystem::file_size(path)),
     _audioWriter(NULL),
     _videoWriter(NULL),
     _timeCodeWriter(NULL)
  {
    _fs.open(path.c_str(), std::ios_base::in | std::ios_base::binary);
  }

  void Dispose()
  {
    if (_fs.is_open())
      _fs.close();

    CloseOutput(NULL, true);
  }

  void Close()
  {
    Dispose();
  }

  std::string & OutputDirectory() { return _outputDirectory; }
  FractionUInt32 const & AverageFrameRate() const { return _averageFrameRate; }
  FractionUInt32 const & TrueFrameRate() const { return _trueFrameRate; }
  std::vector<std::string> const & Warnings() const { return _warnings; }

  bool ExtractedAudio() const { return _extractedAudio; }
  bool ExtractedVideo() const { return _extractedVideo; }
  bool ExtractedTimeCodes() const { return _extractedTimeCodes; }

  void ExtractStreams(bool extractAudio, bool extractVideo, bool extractTimeCodes, OverwriteDelegate const & overwrite)
  {
    uint32_t dataOffset;
    _outputPathBase = (boost::filesystem::path(_outputDirectory) / boost::filesystem::path(_inputPath).stem()).string();
    _overwrite = overwrite;
    _extractAudio = extractAudio;
    _extractVideo = extractVideo;
    _extractTimeCodes = extractTimeCodes;
    _videoTimeStamps.clear();

    Seek(0);
    if (_fileLength < 4 || ReadUInt32() != 0x464C5601)
    {
      if (_fileLength >= 8 && ReadUInt32() == 0x66747970)
        throw std::runtime_error("This is an MP4 file. YAMB or MP4Box can be used to extract streams.");
      else
        throw std::runtime_error("This isn't an FLV file.");
    }
    
    if (std::find(_outputExtensions.begin(), _outputExtensions.end(), boost::filesystem::path(_inputPath).extension()) != _outputExtensions.end())
    {
      // Can't have the same extension as files we output
      throw std::runtime_error("Please change the extension of this FLV file.");
    }

    if (!boost::filesystem::is_directory(_outputDirectory))
      throw std::runtime_error("Output directory doesn't exist.");

    dataOffset = ReadUInt32();

    Seek(dataOffset);

    while (_fileOffset < _fileLength)
    {
      if (!ReadTag()) break;
      if ((_fileLength - _fileOffset) < 4) break;
    }

    _averageFrameRate = CalculateAverageFrameRate();
    _trueFrameRate = CalculateTrueFrameRate();

    CloseOutput(_averageFrameRate, false);
  }

  void CloseOutput(FractionUInt32 averageFrameRate, bool disposing)
  {
    if (_videoWriter)
    {
      _videoWriter->Finish(averageFrameRate ? averageFrameRate : FractionUInt32(25, 1));
      if (disposing && !_videoWriter->Path.empty())
      {
        boost::filesystem::remove(_videoWriter->Path);
      }
      _videoWriter.reset();
    }
    if (_audioWriter)
    {
      _audioWriter->Finish();
      if (disposing && !_audioWriter->Path.empty())
      {
        boost::filesystem::remove(_audioWriter->Path);
      }
      _audioWriter.reset();
    }
    if (_timeCodeWriter)
    {
      _timeCodeWriter->Finish();
      if (disposing && !_timeCodeWriter->Path.empty())
      {
        boost::filesystem::remove(_timeCodeWriter->Path);
      }
      _timeCodeWriter.reset();
    }
  }

  private:  
  IAudioWriter * GetAudioWriter(uint32_t mediaInfo)
  {
    uint32_t format = mediaInfo >> 4;
    uint32_t rate = (mediaInfo >> 2) & 0x3;
    uint32_t bits = (mediaInfo >> 1) & 0x1;
    uint32_t chans = mediaInfo & 0x1;
    std::string path;

    if ((format == 2) || (format == 14))
    { // MP3
      path = _outputPathBase + ".mp3";
      if (!CanWriteTo(path)) return new DummyAudioWriter;
      return new MP3Writer(path, _warnings);
    }
    else if ((format == 0) || (format == 3))
    { // PCM
      int sampleRate = 0;
      switch (rate) {
        case 0: sampleRate =  5512; break;
        case 1: sampleRate = 11025; break;
        case 2: sampleRate = 22050; break;
        case 3: sampleRate = 44100; break;
      }
      path = _outputPathBase + ".wav";
      if (!CanWriteTo(path)) return new DummyAudioWriter;
      if (format == 0) {
        _warnings.push_back("PCM byte order unspecified, assuming little endian.");
      }
      return new WAVWriter(path, (bits == 1) ? 16 : 8,
        (chans == 1) ? 2 : 1, sampleRate);
    }
    else if (format == 10)
    { // AAC
      path = _outputPathBase + ".aac";
      if (!CanWriteTo(path)) return new DummyAudioWriter;
      return new AACWriter(path);
    }
    else if (format == 11)
    { // Speex
      path = _outputPathBase + ".spx";
      if (!CanWriteTo(path)) return new DummyAudioWriter;
      return new SpeexWriter(path, (int)(_fileLength & 0xFFFFFFFF));
    }
    else
    {
      std::string typeStr;

      if (format == 1)
        typeStr = "ADPCM";
      else if ((format == 4) || (format == 5) || (format == 6))
        typeStr = "Nellymoser";
      else
        typeStr = "format=" + boost::lexical_cast<std::string>(format);

      _warnings.push_back("Unable to extract audio (" + typeStr + " is unsupported).");

      return new DummyAudioWriter;
    }
  }

  IVideoWriter * GetVideoWriter(uint32_t mediaInfo)
  {
    uint32_t codecID = mediaInfo & 0x0F;
    std::string path;

    if ((codecID == 2) || (codecID == 4) || (codecID == 5))
    {
      path = _outputPathBase + ".avi";
      if (!CanWriteTo(path)) return new DummyVideoWriter();
      return new AVIWriter(path, (int)codecID, _warnings);
    }
    else if (codecID == 7)
    {
      path = _outputPathBase + ".264";
      if (!CanWriteTo(path)) return new DummyVideoWriter();
      return new RawH264Writer(path);
    }
    else
    {
      std::string typeStr;

      if (codecID == 3)
        typeStr = "Screen";
      else if (codecID == 6)
        typeStr = "Screen2";
      else
        typeStr = "codecID=" + boost::lexical_cast<std::string>(codecID);

      _warnings.push_back("Unable to extract video (" + typeStr + " is unsupported).");

      return new DummyVideoWriter();
    }
  }
  
  
  bool ReadTag()
  {
    uint32_t tagType, dataSize, timeStamp, mediaInfo;
    std::vector<byte> data;

    if ((_fileLength - _fileOffset) < 11)
    {
      return false;
    }

    // Read tag header
    tagType = ReadUInt8();
    dataSize = ReadUInt24();
    timeStamp = ReadUInt24();
    timeStamp |= ReadUInt8() << 24;

    // Read tag data
    if (dataSize == 0)
    {
      return true;
    }
    if ((_fileLength - _fileOffset) < dataSize)
    {
      return false;
    }
    mediaInfo = ReadUInt8();
    dataSize -= 1;
    data = ReadBytes((int)dataSize);

    if (tagType == 0x8)
    {  // Audio
      if (!_audioWriter)
      {
        _audioWriter.reset(_extractAudio ? GetAudioWriter(mediaInfo) : new DummyAudioWriter);
        _extractedAudio = typeid(*_audioWriter) != typeid(DummyAudioWriter);
      }
      _audioWriter->WriteChunk(data, timeStamp);
    }
    else if ((tagType == 0x9) && ((mediaInfo >> 4) != 5))
    { // Video
      if (_videoWriter == NULL)
      {
        _videoWriter.reset(_extractVideo ? GetVideoWriter(mediaInfo) : new DummyVideoWriter());
        _extractedVideo = typeid(*_videoWriter) != typeid(DummyVideoWriter);
      }
      if (_timeCodeWriter == NULL)
      {
        std::string path = _outputPathBase + ".txt";
        _timeCodeWriter.reset(new TimeCodeWriter((_extractTimeCodes && CanWriteTo(path)) ? path : ""));
        _extractedTimeCodes = _extractTimeCodes;
      }
      _videoTimeStamps.push_back(timeStamp);
      _videoWriter->WriteChunk(data, timeStamp, (int)((mediaInfo & 0xF0) >> 4));
      _timeCodeWriter->Write(timeStamp);
    }

    return true;
  }
  

  bool CanWriteTo(std::string const & path)
  {
    if(boost::filesystem::exists(path))
      return _overwrite(path);
    
    return true;
  }

  FractionUInt32 CalculateAverageFrameRate()
  {
    FractionUInt32 frameRate;
    int frameCount = _videoTimeStamps.size();

    if (frameCount > 1)
    {
      frameRate.N = (uint32_t)(frameCount - 1) * 1000;
      frameRate.D = _videoTimeStamps[frameCount - 1] - _videoTimeStamps[0];
      frameRate.Reduce();
      return frameRate;
    }
    else
    {
      return NULL;
    }
  }

  FractionUInt32 CalculateTrueFrameRate()
  {
    typedef std::map<uint32_t,uint32_t> DeltaMapType;
    FractionUInt32 frameRate;
    DeltaMapType deltaCount;
    size_t i, threshold;
    uint32_t delta, count, minDelta;

    // Calculate the distance between the timestamps, count how many times each delta appears
    for (i = 1; i < _videoTimeStamps.size(); i++)
    {
      int deltaS = (int)((int64_t)_videoTimeStamps[i] - (int64_t)_videoTimeStamps[i - 1]);

      if (deltaS <= 0) continue;
      delta = (uint32_t)deltaS;

      deltaCount[delta] += 1;
    }

    threshold = _videoTimeStamps.size() / 10;
    minDelta = std::numeric_limits<uint32_t>::max();

    // Find the smallest delta that made up at least 10% of the frames (grouping in delta+1
    // because of rounding, e.g. a NTSC video will have deltas of 33 and 34 ms)    
    BOOST_FOREACH(DeltaMapType::value_type const & deltaItem, deltaCount)
    {
      delta = deltaItem.first;
      count = deltaItem.second;

      if (deltaCount.find(delta + 1) != deltaCount.end())
      {
        count += deltaCount[delta + 1];
      }

      if ((count >= threshold) && (delta < minDelta))
      {
        minDelta = delta;
      }
    }

    // Calculate the frame rate based on the smallest delta, and delta+1 if present
    if (minDelta != std::numeric_limits<uint32_t>::max())
    {
      uint32_t totalTime, totalFrames;

      count = deltaCount[minDelta];
      totalTime = minDelta * count;
      totalFrames = count;

      if (deltaCount.find(minDelta + 1) != deltaCount.end())
      {
        count = deltaCount[minDelta + 1];
        totalTime += (minDelta + 1) * count;
        totalFrames += count;
      }

      if (totalTime != 0)
      {
        frameRate.N = totalFrames * 1000;
        frameRate.D = totalTime;
        frameRate.Reduce();
        return frameRate;
      }
    }

    // Unable to calculate frame rate
    return NULL;
  }

  void Seek(int64_t const & offset)
  {
    _fs.seekg(offset, std::ios_base::beg);
    _fileOffset = offset;
  }

  uint32_t ReadUInt8()
  {
    _fileOffset += 1;
    char c = 0;
    _fs.get(c);
    return uint32_t(static_cast<byte>(c));
  }

  uint32_t ReadUInt24()
  {
    byte x[4] =  {0, 0, 0, 0};    
    Stream::Read(_fs, x, 1, 3);
    _fileOffset += 3;
    return BitConverterBE::ToUInt32(x, 0);
  }

  uint32_t ReadUInt32()
  {
    byte x[4] =  {0, 0, 0, 0};
    Stream::Read(_fs, x, 0, 4);
    _fileOffset += 4;
    return BitConverterBE::ToUInt32(x, 0);
  }

  std::vector<byte> ReadBytes(int length)
  {
    std::vector<byte> buff(length);
    Stream::Read(_fs, buff, length);
    _fileOffset += length;
    return buff;
  }
};

  
  
  
  
  

} // end namespace JDP
