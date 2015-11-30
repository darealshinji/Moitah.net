# Linux/gmake makefile

BIN = FLVExtractCL

CXX = g++
BUILDFLAGS = -O2 -Wall -Wextra -fstack-protector-all -D_FORTIFY_SOURCE=2 $(CXXFLAGS) -Wl,-z,relro $(LDFLAGS)
LIBS = -lboost_filesystem -lboost_system


all: $(BIN)

$(BIN): $(BIN).cpp
	$(CXX) $(BUILDFLAGS) -o $@ $^ $(LIBS)

clean:
	rm -f $(BIN)

distclean: clean

