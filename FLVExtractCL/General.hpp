#pragma once

#include <cstring>
#include <vector>
#include <string>
#include <fstream>
#include <stdint.h>

#include <boost/function.hpp>

typedef unsigned char byte;
typedef boost::function<bool (std::string const &)> OverwriteDelegate;



namespace Encoding
{
  namespace ASCII
  {
    std::vector<byte> GetBytes(std::string const & s)
    {
      return std::vector<byte>(s.c_str(), s.c_str()+s.length());
    }
  }
}


namespace Stream
{
  void Write(std::fstream & fs, std::vector<byte> const & buff)
  {
    fs.write(reinterpret_cast<char const *>(&buff[0]), buff.size());
  }
  
  void Write(std::fstream & fs, std::vector<byte> const & buff, size_t offset, size_t len)
  {
    fs.write(reinterpret_cast<char const *>(&buff[0]+offset), len);
  }
  
  void Write(std::fstream & fs, byte const * buff, size_t offset, size_t len)
  {
    fs.write(reinterpret_cast<char const *>(buff+offset), len);
  }
  
  
  void Read(std::fstream & fs, byte * buff, size_t offset, size_t len)
  {
    fs.read(reinterpret_cast<char *>(buff+offset), len);
  }
  
  void Read(std::fstream & fs, std::vector<byte> & buff, size_t len)
  {
    if(buff.size() != len)
      buff.resize(len);
    fs.read(reinterpret_cast<char *>(&buff[0]), len);
  }
  
}


namespace Binary
{
  void Write(std::fstream & fs, uint8_t v)
  {
    fs.write(reinterpret_cast<char const *>(&v), 1);
  }
  
  void Write(std::fstream & fs, int8_t v)
  {
    fs.write(reinterpret_cast<char const *>(&v), 1);
  }
  
  void Write(std::fstream & fs, uint16_t v)
  {
    fs.write(reinterpret_cast<char const *>(&v), 2);
  }
  
  void Write(std::fstream & fs, int16_t v)
  {
    fs.write(reinterpret_cast<char const *>(&v), 2);
  }
  
  void Write(std::fstream & fs, uint32_t v)
  {
    fs.write(reinterpret_cast<char const *>(&v), 4);
  }
  
  void Write(std::fstream & fs, int32_t v)
  {
    fs.write(reinterpret_cast<char const *>(&v), 4);
  }
  
  void Write(std::fstream & fs, uint64_t const & v)
  {
    fs.write(reinterpret_cast<char const *>(&v), 8);
  }
  
  void Write(std::fstream & fs, int64_t const & v)
  {
    fs.write(reinterpret_cast<char const *>(&v), 8);
  }
}


namespace JDP {
  
  namespace General {
    void CopyBytes(std::vector<byte> & dst, int dstOffset, std::vector<byte> const & src)
    {
      memcpy(reinterpret_cast<void*>(&dst[0] + dstOffset), reinterpret_cast<void const*>(&src[0]), src.size());
    }
  
  }

	struct BitHelper
  {
		static int Read(uint64_t & x, int length)
    {
			int r = (int)(x >> (64 - length));
			x <<= length;
			return r;
		}

		static int Read(std::vector<byte> const & bytes, int & offset, int length)
    {
			int startByte = offset / 8;
			int endByte = (offset + length - 1) / 8;
			int skipBits = offset % 8;
			uint64_t bits = 0;
			for (int i = 0; i <= std::min(endByte - startByte, 7); i++)
      {
				bits |= (uint64_t)bytes[startByte + i] << (56 - (i * 8));
			}
			if (skipBits != 0) Read(bits, skipBits);
			offset += length;
			return Read(bits, length);
		}

		static void Write(uint64_t & x, int length, int value)
    {
			uint64_t mask = 0xFFFFFFFFFFFFFFFF >> (64 - length);
			x = (x << length) | ((uint64_t)value & mask);
		}

		static std::vector<byte> CopyBlock(std::vector<byte> const & bytes, int offset, int length)
    {
			int startByte = offset / 8;
			int endByte = (offset + length - 1) / 8;
			int shiftA = offset % 8;
			int shiftB = 8 - shiftA;
			std::vector<byte> dst((length + 7) / 8);
			if (shiftA == 0)
      {
        memcpy(reinterpret_cast<void*>(&dst[0]), reinterpret_cast<void const*>(&bytes[0] + startByte), dst.size());
			}
			else
      {
				int i;
				for (i = 0; i < endByte - startByte; i++)
        {
					dst[i] = (byte)((bytes[startByte + i] << shiftA) | (bytes[startByte + i + 1] >> shiftB));
				}
				if (size_t(i) < dst.size())
        {
					dst[i] = (byte)(bytes[startByte + i] << shiftA);
				}
			}
			dst[dst.size() - 1] &= (byte)(0xFF << ((dst.size() * 8) - length));
			return dst;
		}
	};

	struct BitConverterBE
  {
		static uint64_t ToUInt64(std::vector<byte> const & value, int startIndex)
    {
			return
				((uint64_t)value[startIndex    ] << 56) |
				((uint64_t)value[startIndex + 1] << 48) |
				((uint64_t)value[startIndex + 2] << 40) |
				((uint64_t)value[startIndex + 3] << 32) |
				((uint64_t)value[startIndex + 4] << 24) |
				((uint64_t)value[startIndex + 5] << 16) |
				((uint64_t)value[startIndex + 6] <<  8) |
				((uint64_t)value[startIndex + 7]      );
		}

		static uint32_t ToUInt32(std::vector<byte> const & value, int startIndex)
    {
			return
				((uint32_t)value[startIndex    ] << 24) |
				((uint32_t)value[startIndex + 1] << 16) |
				((uint32_t)value[startIndex + 2] <<  8) |
				((uint32_t)value[startIndex + 3]      );
		}
    
    static uint32_t ToUInt32(byte const * value, int startIndex)
    {
			return
				((uint32_t)value[startIndex    ] << 24) |
				((uint32_t)value[startIndex + 1] << 16) |
				((uint32_t)value[startIndex + 2] <<  8) |
				((uint32_t)value[startIndex + 3]      );
		}

		static uint16_t ToUInt16(std::vector<byte> const & value, int startIndex)
    {
			return (uint16_t)(
				(value[startIndex    ] <<  8) |
				(value[startIndex + 1]      ));
		}

		static std::vector<byte> GetBytes(uint64_t const & value)
    {
			std::vector<byte> buff(8);
			buff[0] = (byte)(value >> 56);
			buff[1] = (byte)(value >> 48);
			buff[2] = (byte)(value >> 40);
			buff[3] = (byte)(value >> 32);
			buff[4] = (byte)(value >> 24);
			buff[5] = (byte)(value >> 16);
			buff[6] = (byte)(value >>  8);
			buff[7] = (byte)(value      );
			return buff;
		}

		static std::vector<byte> GetBytes(uint32_t value)
    {
			std::vector<byte> buff(4);
			buff[0] = (byte)(value >> 24);
			buff[1] = (byte)(value >> 16);
			buff[2] = (byte)(value >>  8);
			buff[3] = (byte)(value      );
			return buff;
		}

		static std::vector<byte> GetBytes(uint16_t value)
    {
			std::vector<byte> buff(2);
			buff[0] = (byte)(value >>  8);
			buff[1] = (byte)(value      );
			return buff;
		}
	};

	struct BitConverterLE
  {
		static std::vector<byte> GetBytes(uint64_t const & value)
    {
			std::vector<byte> buff(8);
			buff[0] = (byte)(value      );
			buff[1] = (byte)(value >>  8);
			buff[2] = (byte)(value >> 16);
			buff[3] = (byte)(value >> 24);
			buff[4] = (byte)(value >> 32);
			buff[5] = (byte)(value >> 40);
			buff[6] = (byte)(value >> 48);
			buff[7] = (byte)(value >> 56);
			return buff;
		}

		static std::vector<byte> GetBytes(uint32_t value)
    {
			std::vector<byte> buff(4);
			buff[0] = (byte)(value      );
			buff[1] = (byte)(value >>  8);
			buff[2] = (byte)(value >> 16);
			buff[3] = (byte)(value >> 24);
			return buff;
		}

		static std::vector<byte> GetBytes(uint16_t value)
    {
			std::vector<byte> buff(2);
			buff[0] = (byte)(value      );
			buff[1] = (byte)(value >>  8);
			return buff;
		}
	};

	struct OggCRC
  {
		static uint32_t _lut[256];
    static bool inited;

		static void init()
    {
			for (uint32_t i = 0; i < 256; i++)
      {
				uint32_t x = i << 24;
				for (uint32_t j = 0; j < 8; j++)
        {
					x = ((x & 0x80000000U) != 0) ? ((x << 1) ^ 0x04C11DB7) : (x << 1);
				}
				_lut[i] = x;
			}
		}

		static uint32_t Calculate(std::vector<byte>const & buff, int offset, int length)
    {
      if(!inited)
        init();
      
			uint32_t crc = 0;
			for (int i = 0; i < length; i++)
      {
				crc = _lut[((crc >> 24) ^ buff[offset + i]) & 0xFF] ^ (crc << 8);
			}
			return crc;
		}
	};
  
}
