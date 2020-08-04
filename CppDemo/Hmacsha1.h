#pragma once

class CHmacsha1
{
public:
	char * id;
	char * key;
	CHmacsha1();
	~CHmacsha1();

	int EncodeString(CString szPlaintext, CString& szCiphertext, int& len);
	int DecodeString(CString& szPlaintext, CString szCiphertext, int len);
	int SignatureString(CString& signature, CString szText);
	int VerifySignatureString(CString signature, CString szText);
};

