INCLUDE = 
LIBDIR  = 

COMPILERFLAGS = -Wall -Wextra -Wcast-align -Wctor-dtor-privacy -Wswitch-enum -Wshadow -Wcast-qual -Winit-self -Wmissing-include-dirs -Woverloaded-virtual -Wnon-virtual-dtor -Wfloat-equal -O3

CC = g++
CFLAGS = $(COMPILERFLAGS) $(INCLUDE)
LIBRARIES = -lboost_filesystem -lboost_system

.cpp.o:
	$(CC) $(CFLAGS) $(LIBDIR) -c $< -o $@

all: FLVExtractCL

FLVExtractCL : FLVExtractCL.o
	$(CC) $(CFLAGS) -o $@ $(LIBDIR) $^ $(LIBRARIES)

clean:
	rm -f FLVExtractCL
	rm -f FLVExtractCL.o
