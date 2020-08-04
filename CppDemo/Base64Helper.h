#pragma once

static const char _B64_[64] = {
	'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H',
	'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
	'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X',
	'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f',
	'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n',
	'o', 'p', 'q', 'r', 's', 't', 'u', 'v',
	'w', 'x', 'y', 'z', '0', '1', '2', '3',
	'4', '5', '6', '7', '8', '9', '+', '/'
};
class CBase64Helper
{
	void encodetribyte(char * in, char * out, int len);
	int decodetribyte(char * in, char * out);
public:
	CBase64Helper();
	~CBase64Helper();

	int Base64Encode(char * base64code, const char * src, int src_len = 0);
	int Base64Decode(char * buf, const char * base64code, int src_len = 0);

	int cBase64Encode(char * b64, const  char * input, long stringlen = 0);
	int cBase64Decode(char * output, char * b64, long codelen = 0);
	CString cBase64Encode(CString szDataXml);
	CString cBase64Decode(CString xmlBase64);
};

