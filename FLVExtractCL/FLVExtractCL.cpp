// ****************************************************************************
// 
// FLV Extract
// Copyright (C) 2006-2012  J.D. Purcell (moitah@yahoo.com)
//                          C++ port by TheProphet (theprophet@wanadoo.fr)
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

#include "FLVFile.hpp"
#include <iostream>

#include "static_init.cxx"


bool autoOverwrite = false;

bool can_overwrite(std::string const & path)
{
  if(autoOverwrite)
    return true;
  
  std::cout << "The file " << path << " already exists ; do you want to overwrite it ? [y/n] ";
  char c = 'n';
  std::cin >> c;
  return (c == 'y' || c == 'Y');
}


int main(int argc, char * argv[])
{
  int argCount = argc;
  int argIndex = 1;
  bool extractVideo = false;
  bool extractAudio = false;
  bool extractTimeCodes = false;
  std::string outputDirectory = "";
  std::string inputPath = "";

  std::cout << "FLV Extract CL v1.6.2 (C++ version by TheProphet)" << std::endl;
  std::cout << "Copyright 2006-2011 J.D. Purcell" << std::endl;
  std::cout << "http://www.moitah.net/" << std::endl;
  std::cout << std::endl;

  try
  {
    while (argIndex < argCount)
    {
      if(std::string(argv[argIndex]) == "-v")
        extractVideo = true;
      else if(std::string(argv[argIndex]) == "-a")
        extractAudio = true;
      else if(std::string(argv[argIndex]) == "-t")
        extractTimeCodes = true;
      else if(std::string(argv[argIndex]) == "-o")
        autoOverwrite = true;
      else if(std::string(argv[argIndex]) == "-d")
        outputDirectory = argv[++argIndex];
      else
        goto BreakArgLoop;
      
      argIndex++;
    }
  BreakArgLoop:

    if (argIndex != (argCount - 1))
      throw std::runtime_error("Incorrect command line parameters.");

    inputPath = argv[argIndex];
  }
  catch(...)
  {
    std::cout << "Arguments: [switches] source_path" << std::endl;
    std::cout << std::endl;
    std::cout << "Switches:" << std::endl;
    std::cout << "  -v         Extract video." << std::endl;
    std::cout << "  -a         Extract audio." << std::endl;
    std::cout << "  -t         Extract timecodes." << std::endl;
    std::cout << "  -o         Overwrite output files without prompting." << std::endl;
    std::cout << "  -d <dir>   Output directory.  If not specified, output files will be written" << std::endl;
    std::cout << "             in the same directory as the source file." << std::endl;
    return 1;
  }

  try
  {
    using namespace JDP;
    FLVFile flvFile(boost::filesystem::system_complete(inputPath).string());
    
    if (!outputDirectory.empty())
      flvFile.OutputDirectory() = boost::filesystem::system_complete(outputDirectory).string();

    flvFile.ExtractStreams(extractAudio, extractVideo, extractTimeCodes, can_overwrite);
    if (flvFile.TrueFrameRate() || flvFile.AverageFrameRate())
    {
      if (flvFile.TrueFrameRate())
        std::cout << "True Frame Rate: " << flvFile.TrueFrameRate().ToString() << std::endl;

      if (flvFile.AverageFrameRate())
        std::cout << "Average Frame Rate: " << flvFile.AverageFrameRate().ToString() << std::endl;
      
      std::cout << std::endl;
    }
    if (flvFile.Warnings().size())
    {
      BOOST_FOREACH(std::string const & warning, flvFile.Warnings())
      {
        std::cout << "Warning: " << warning << std::endl;
      }
      std::cout << std::endl;
    }
  }
  catch (std::exception const & e)
  {
    std::cout << "Error: " << e.what() << std::endl;
    return 1;
  }

  std::cout << "Finished." << std::endl;
  return 0;
 }
  
  