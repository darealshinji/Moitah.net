CXX := g++
_CXXFLAGS = -O2 -Wall -Wextra -fstack-protector-all -D_FORTIFY_SOURCE=2
_LDFLAGS = -Wl,-z,relro
_LIBS = -lboost_filesystem -lboost_system

.cpp.o:
	$(CXX) $(_CXXFLAGS) $(CPPFLAGS) $(CXXFLAGS) -c $< -o $@

all: FLVExtractCL

FLVExtractCL : FLVExtractCL.o
	$(CXX) $(_LDFLAGS) $(LDFLAGS) -o $@ $^ $(_LIBS)

clean:
	rm -f FLVExtractCL
	rm -f FLVExtractCL.o

distclean: clean

