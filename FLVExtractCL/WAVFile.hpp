#pragma once

#include "General.hpp"

namespace WAVTools {
  
struct WAVWriter
{
  std::fstream _bw;
  bool _canSeek;
  bool _wroteHeaders;
  int _bitsPerSample, _channelCount, _sampleRate, _blockAlign;
  int64_t _finalSampleLen, _sampleLen;

  WAVWriter(std::string const & path, int bitsPerSample, int channelCount, int sampleRate)
  {
    _bitsPerSample = bitsPerSample;
    _channelCount = channelCount;
    _sampleRate = sampleRate;
    _blockAlign = _channelCount * ((_bitsPerSample + 7) / 8);

    _bw.open(path.c_str(), std::ios_base::out | std::ios_base::binary);
    _canSeek = true;
  }

  private:
  void WriteHeaders()
  {
    uint32_t const fccRIFF = 0x46464952;
    uint32_t const fccWAVE = 0x45564157;
    uint32_t const fccFormat = 0x20746D66;
    uint32_t const fccData = 0x61746164;

    uint32_t dataChunkSize = GetDataChunkSize(_finalSampleLen);

    Binary::Write(_bw, fccRIFF);
    Binary::Write(_bw, (uint32_t)(dataChunkSize + (dataChunkSize & 1) + 36));
    Binary::Write(_bw, fccWAVE);
    Binary::Write(_bw, fccFormat);
    Binary::Write(_bw, (uint32_t)16);
    Binary::Write(_bw, (uint16_t)1);
    Binary::Write(_bw, (uint16_t)_channelCount);
    Binary::Write(_bw, (uint32_t)_sampleRate);
    Binary::Write(_bw, (uint32_t)(_sampleRate*_blockAlign));
    Binary::Write(_bw, (uint16_t)_blockAlign);
    Binary::Write(_bw, (uint16_t)_bitsPerSample);
    Binary::Write(_bw, fccData);
    Binary::Write(_bw, dataChunkSize);
  }

  uint32_t GetDataChunkSize(uint64_t sampleCount)
  {
    const uint64_t maxFileSize = 0x7FFFFFFEL;
    uint64_t dataSize = sampleCount * _blockAlign;
    if ((dataSize + 44) > maxFileSize)
    {
      dataSize = ((maxFileSize - 44) / _blockAlign) * _blockAlign;
    }
    return (uint32_t)dataSize;
  }

  public:
  void Close()
  {
    if (((_sampleLen * _blockAlign) & 1) == 1)
    {
      char tmp8 = 0;
      _bw.write(&tmp8, 1);
    }

    if (_sampleLen != _finalSampleLen)
    {
      if (_canSeek)
      {
        uint32_t dataChunkSize = GetDataChunkSize(_sampleLen);
        _bw.seekp(4, std::ios_base::beg);
        Binary::Write(_bw, (uint32_t)(dataChunkSize + (dataChunkSize & 1) + 36));
        _bw.seekp(40, std::ios_base::beg);
        Binary::Write(_bw, dataChunkSize);
      }
      else
      {
        throw std::runtime_error("Samples written differs from the expected sample count.");
      }
    }

    _bw.close();
  }

  uint64_t Position() { return _sampleLen; }
  void FinalSampleCount(uint64_t val) { _finalSampleLen = val; }

  void Write(byte const * buff, int sampleCount)
  {
    if (sampleCount <= 0) return;

    if (!_wroteHeaders)
    {
      WriteHeaders();
      _wroteHeaders = true;
    }

    Stream::Write(_bw, buff, 0, sampleCount * _blockAlign);
    _sampleLen += sampleCount;
  }
};
  
}

