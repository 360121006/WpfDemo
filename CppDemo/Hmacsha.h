#pragma once
class CHmacsha
{
public:
	CHmacsha();
	~CHmacsha();
	CString bytestohexstring(char * bytes, int bytelength);
	void truncate(char * d1, char * d2, int len);
	void hmac_sha256(const char * k, int lk, const char * d, int ld, char * out, int * t);
	void hmac_sha1(const char * k, int lk, const char * d, int ld, char * out, int * t);
	void sha256(const char * d, int ld, char * out, int * t);
	void sha1(const char * d, int ld, char * out, int * t);
};

