#pragma once


#ifndef MD5_H 
#define MD5_H 

#include <string> 
#include <fstream> 

/* Type define */
typedef unsigned char byte;
typedef unsigned int uint32;

using std::string;
using std::ifstream;

/* MD5 declaration. */
class MD5 {
public:
	MD5();
	MD5(const void* input, size_t length);
	MD5(const string& str);
	MD5(ifstream& in);
	void update(const void* input, size_t length);
	void update(const string& str);
	void update(ifstream& in);
	const byte* digest();
	string toString();
	void reset();
	string ToMD5(const string& str);

private:
	void update(const byte* input, size_t length);
	void final();
	void transform(const byte block[64]);
	void encode(const uint32* input, byte* output, size_t length);
	void decode(const byte* input, uint32* output, size_t length);
	string bytesToHexString(const byte* input, size_t length);
	MD5(const MD5&);
	MD5& operator=(const MD5&);

private:
	uint32 _state[4];
	uint32 _count[2];
	byte _buffer[64];
	byte _digest[16];
	bool _finished;

	static const byte PADDING[64];
	static const char HEX[16];
	enum { BUFFER_SIZE = 1024 };
};

#endif /*MD5_H*/


class CMD5Helper
{
public:
	CMD5Helper();
	~CMD5Helper();

	CString GetMD5(CString source);
};

