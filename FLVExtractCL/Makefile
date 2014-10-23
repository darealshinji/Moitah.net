COMPILERFLAGS = -Wall -Wextra -Wcast-align -Wctor-dtor-privacy -Wswitch-enum -Wshadow -Wcast-qual -Winit-self -Wmissing-include-dirs -Woverloaded-virtual -Wnon-virtual-dtor -Wfloat-equal -O3

CXX = g++
LIBRARIES = -lboost_filesystem -lboost_system

.cpp.o:
	$(CXX) $(CXXFLAGS) $(CPPFLAGS) $(COMPILERFLAGS) -c $< -o $@

all: FLVExtractCL

FLVExtractCL : FLVExtractCL.o
	$(CXX) $(LDFLAGS) -o $@ $^ $(LIBRARIES)

clean:
	rm -f flvextract
	rm -f FLVExtractCL.o
