#include "stdafx.h"
#include "Base64Helper.h"

#ifndef _BASE64_INCLUDE__H__
#define _BASE64_INCLUDE__H__

// 编码后的长度一般比原文多占1/3的存储空间，请保证base64code有足够的空间


CBase64Helper::CBase64Helper()
{
}


CBase64Helper::~CBase64Helper()
{
}

__inline char GetB64Char(int index)
{
	const char szBase64Table[] = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
	if (index >= 0 && index < 64)
		return szBase64Table[index];

	return '=';
}

// 从双字中取单字节
#define B0(a) (a & 0xFF)
#define B1(a) (a >> 8 & 0xFF)
#define B2(a) (a >> 16 & 0xFF)
#define B3(a) (a >> 24 & 0xFF)

//base64加密
// 编码后的长度一般比原文多占1/3的存储空间，请保证base64code有足够的空间
int CBase64Helper::Base64Encode(char * base64code, const char * src, int src_len)
{
	if (src_len == 0)
		src_len = strlen(src);

	int len = 0;
	unsigned char* psrc = (unsigned char*)src;
	char * p64 = base64code;
	//  int i = 0;
	int i;
	for (i = 0; i < src_len - 3; i += 3)
	{
		unsigned long ulTmp = *(unsigned long*)psrc;
		register int b0 = GetB64Char((B0(ulTmp) >> 2) & 0x3F);
		register int b1 = GetB64Char((B0(ulTmp) << 6 >> 2 | B1(ulTmp) >> 4) & 0x3F);
		register int b2 = GetB64Char((B1(ulTmp) << 4 >> 2 | B2(ulTmp) >> 6) & 0x3F);
		register int b3 = GetB64Char((B2(ulTmp) << 2 >> 2) & 0x3F);
		*((unsigned long*)p64) = b0 | b1 << 8 | b2 << 16 | b3 << 24;
		len += 4;
		p64 += 4;
		psrc += 3;
	}

	// 处理最后余下的不足3字节的饿数据
	if (i < src_len)
	{
		int rest = src_len - i;
		unsigned long ulTmp = 0;
		for (int j = 0; j < rest; ++j)
		{
			*(((unsigned char*)&ulTmp) + j) = *psrc++;
		}

		p64[0] = GetB64Char((B0(ulTmp) >> 2) & 0x3F);
		p64[1] = GetB64Char((B0(ulTmp) << 6 >> 2 | B1(ulTmp) >> 4) & 0x3F);
		p64[2] = rest > 1 ? GetB64Char((B1(ulTmp) << 4 >> 2 | B2(ulTmp) >> 6) & 0x3F) : '=';
		p64[3] = rest > 2 ? GetB64Char((B2(ulTmp) << 2 >> 2) & 0x3F) : '=';
		p64 += 4;
		len += 4;
	}

	*p64 = '\0';

	return len;
}


__inline int GetB64Index(char ch)
{
	int index = -1;
	if (ch >= 'A' && ch <= 'Z')
	{
		index = ch - 'A';
	}
	else if (ch >= 'a' && ch <= 'z')
	{
		index = ch - 'a' + 26;
	}
	else if (ch >= '0' && ch <= '9')
	{
		index = ch - '0' + 52;
	}
	else if (ch == '+')
	{
		index = 62;
	}
	else if (ch == '/')
	{
		index = 63;
	}

	return index;
}

//base64解密
//解码后的长度一般比原文少用占1/4的存储空间，请保证buf有足够的空间
int CBase64Helper::Base64Decode(char * buf, const char * base64code, int src_len)
{
	if (src_len == 0)
		src_len = strlen(base64code);

	int len = 0;
	unsigned char* psrc = (unsigned char*)base64code;
	char * pbuf = buf;
	int i = 0;
	for (i = 0; i < src_len - 4; i += 4)
	{
		unsigned long ulTmp = *(unsigned long*)psrc;

		if (i == 52)
		{
			int iTemp1 = (char)B0(ulTmp);
			int iTemp2 = (char)B1(ulTmp);
			int iTemp3 = (char)B2(ulTmp);
		}
		register int b0 = (GetB64Index((char)B0(ulTmp)) << 2 | GetB64Index((char)B1(ulTmp)) << 2 >> 6) & 0xFF;
		register int b1 = (GetB64Index((char)B1(ulTmp)) << 4 | GetB64Index((char)B2(ulTmp)) << 2 >> 4) & 0xFF;
		register int b2 = (GetB64Index((char)B2(ulTmp)) << 6 | GetB64Index((char)B3(ulTmp)) << 2 >> 2) & 0xFF;

		*((unsigned long*)pbuf) = b0 | b1 << 8 | b2 << 16;
		psrc += 4;
		pbuf += 3;
		len += 3;
	}

	// 处理最后余下的不足4字节的饿数据
	if (i < src_len)
	{
		int rest = src_len - i;
		unsigned long ulTmp = 0;
		for (int j = 0; j < rest; ++j)
		{
			*(((unsigned char*)&ulTmp) + j) = *psrc++;
		}

		register int b0 = (GetB64Index((char)B0(ulTmp)) << 2 | GetB64Index((char)B1(ulTmp)) << 2 >> 6) & 0xFF;
		*pbuf++ = b0;
		len++;

		if ('=' != B1(ulTmp) && '=' != B2(ulTmp))
		{
			register int b1 = (GetB64Index((char)B1(ulTmp)) << 4 | GetB64Index((char)B2(ulTmp)) << 2 >> 4) & 0xFF;
			*pbuf++ = b1;
			len++;
		}

		if ('=' != B2(ulTmp) && '=' != B3(ulTmp))
		{
			register int b2 = (GetB64Index((char)B2(ulTmp)) << 6 | GetB64Index((char)B3(ulTmp)) << 2 >> 2) & 0xFF;
			*pbuf++ = b2;
			len++;
		}

	}

	*pbuf = '\0';

	return len;
}

 void CBase64Helper::encodetribyte(char * in, char * out, int len)
{
	if (len == 0) return;
	int i;
	unsigned char inbuf[3];
	memset(inbuf, 0, sizeof(char) * 3);
	for (i = 0; i<len; i++)
	{
		inbuf[i] = in[i];
	}
	out[0] = _B64_[inbuf[0] >> 2];
	out[1] = _B64_[((inbuf[0] & 0x03) << 4) | ((inbuf[1] & 0xf0) >> 4)];
	out[2] = (len>1 ? _B64_[((inbuf[1] & 0x0f) << 2) | ((inbuf[2] & 0xc0) >> 6)] : '=');
	out[3] = (len>2 ? _B64_[inbuf[2] & 0x3f] : '=');
}

int CBase64Helper::decodetribyte(char * in, char * out)
{
	int i, j, len;
	char dec[4];
	memset(dec, 0, sizeof(char) * 4);
	len = 3;
	if (in[3] == '=') len--;
	if (in[2] == '=') len--;
	for (i = 0; i<64; i++)
	{
		for (j = 0; j<4; j++)
		{
			if (in[j] == _B64_[i]) dec[j] = i;
		}
	}
	out[0] = (dec[0] << 2 | dec[1] >> 4);
	if (len == 1) return 1;
	out[1] = (dec[1] << 4 | dec[2] >> 2);
	if (len == 2) return 2;
	out[2] = (((dec[2] << 6) & 0xc0) | dec[3]);
	return 3;
}

int CBase64Helper::cBase64Encode(char * b64, const  char * input, long stringlen)
{
	if (!b64 || !input || stringlen<0) return 0;
	long slen, imax;
	register  int i, idin, idout;
	int rd, re, len;
	slen = (stringlen) ? stringlen : strlen((char *)input);
	if (slen == 0) return 0;
	rd = slen % 3;
	rd = (rd == 0) ? 3 : rd;
	imax = (slen + (3 - rd)) / 3 - 1;
	for (i = 0; i <= imax; i++)
	{
		idin = i * 3;
		idout = i * 4;
		len = (i == imax) ? rd : 3;
		encodetribyte((char *)&input[idin], &b64[idout], len);
	}
	re = (imax + 1) * 4;
	b64[re] = '\0';
	return re;
}

int CBase64Helper::cBase64Decode(char * output, char * b64, long codelen)
{
	if (!output || !b64 || codelen<0) return 0;
	long slen, imax;
	register  int i, idin, idout;
	int rd, re, len;
	slen = (codelen) ? codelen : strlen((char *)b64);
	if (slen<4) return 0;
	rd = slen % 4;
	if (rd != 0) return 0;
	imax = slen / 4 - 1;
	for (i = 0; i <= imax; i++)
	{
		idin = i * 4;
		idout = i * 3;
		len = decodetribyte((char *)&b64[idin], &output[idout]);
	}
	re = (imax * 3) + len;
	output[re] = '\0';
	return re;
}

CString CBase64Helper::cBase64Encode(CString szDataXml)
{
	int iTextLen = WideCharToMultiByte(CP_ACP, 0, szDataXml, -1, NULL, 0, NULL, NULL);
	char * buffer = (char*)malloc(iTextLen + 1);
	char * base64 = (char*)malloc(4 * iTextLen + 1);
	memset(buffer, 0, iTextLen);
	memset(base64, 0, 4 * iTextLen + 1);
	WideCharToMultiByte(CP_ACP, 0, szDataXml, -1, buffer, iTextLen, NULL, NULL);
	Base64Encode(base64, buffer);
	CString xmlDataEecode(base64);
	free(buffer);
	free(base64);
	buffer = NULL;
	base64 = NULL;
	return xmlDataEecode;
}

CString CBase64Helper::cBase64Decode(CString xmlBase64)
{
	int iTextLen = WideCharToMultiByte(CP_ACP, 0, xmlBase64, -1, NULL, 0, NULL, NULL);
	char * cxmlBase64 = (char*)malloc(4 * iTextLen + 1);
	char * cxmlContent = (char*)malloc(4 * iTextLen + 1);
	memset(cxmlBase64, 0, 4 * iTextLen + 1);
	memset(cxmlContent, 0, 4 * iTextLen + 1);
	WideCharToMultiByte(CP_ACP, 0, xmlBase64, -1, cxmlBase64, 4 * iTextLen + 1, NULL, NULL);
	Base64Decode(cxmlContent, cxmlBase64);
	CString xmlContent(cxmlContent);
	free(cxmlBase64);
	free(cxmlContent);
	cxmlBase64 = NULL;
	cxmlContent = NULL;
	return xmlContent;
}

#endif // #ifndef _BASE64_INCLUDE__H__
