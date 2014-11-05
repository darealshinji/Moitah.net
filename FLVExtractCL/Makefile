CXX := g++
BUILDFLAGS += -O2 -D_FORTIFY_SOURCE=2 -Wall -Wextra -fstack-protector \
	--param=ssp-buffer-size=4 -Wformat -Werror=format-security
LINKERFLAGS = -Wl,-Bsymbolic-functions -Wl,-z,relro
LIBRARIES = -lboost_filesystem -lboost_system

.cpp.o:
	$(CXX) $(BUILDFLAGS) $(CPPFLAGS) $(CXXFLAGS) -c $< -o $@

all: FLVExtractCL

FLVExtractCL : FLVExtractCL.o
	$(CXX) $(LINKERFLAGS) $(LDFLAGS) -o $@ $^ $(LIBRARIES)

clean:
	rm -f flvextract
	rm -f FLVExtractCL.o
