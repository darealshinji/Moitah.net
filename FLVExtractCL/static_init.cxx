#pragma once

#include <boost/assign/list_of.hpp>

bool JDP::OggCRC::inited = false;
uint32_t JDP::OggCRC::_lut[256] = {0};

std::vector<std::string> const JDP::FLVFile::_outputExtensions = boost::assign::list_of(".avi")(".mp3")(".264")(".aac")(".spx")(".txt");

std::string const JDP::SpeexWriter::_vendorString = "FLV Extract";

std::vector<byte> const JDP::RawH264Writer::_startCode = boost::assign::list_of(0)(0)(0)(1);

