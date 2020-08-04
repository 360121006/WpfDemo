#pragma once
#ifndef _AES_H_
#define _AES_H_

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#if defined(_MSC_VER) && !defined(OPENSSL_SYS_WINCE)
# define SWAP(x) (_lrotl(x, 8) & 0x00ff00ff | _lrotr(x, 8) & 0xff00ff00)
# define GETU32(p) SWAP(*((u32 *)(p)))
# define PUTU32(ct, st) { *((u32 *)(ct)) = SWAP((st)); }
#else
# define GETU32(pt) (((u32)(pt)[0] << 24) ^ ((u32)(pt)[1] << 16) ^ ((u32)(pt)[2] <<  8) ^ ((u32)(pt)[3]))
# define PUTU32(ct, st) { (ct)[0] = (u8)((st) >> 24); (ct)[1] = (u8)((st) >> 16); (ct)[2] = (u8)((st) >>  8); (ct)[3] = (u8)(st); }
#endif

typedef unsigned long u32;
typedef unsigned short u16;
typedef unsigned char u8;

#define MAXKC   (256/32)
#define MAXKB   (256/8)
#define MAXNR   14

#ifdef OPENSSL_NO_AES
#error AES is disabled.
#endif

#define AES_ENCRYPT	1
#define AES_DECRYPT	0

/* Because array size can't be a const in C, the following two are macros.
Both sizes are in bytes. */
#define AES_MAXNR 14
#define AES_BLOCK_SIZE 16

struct aes_key_st {
	unsigned long rd_key[4 * (AES_MAXNR + 1)];
	int rounds;
};
typedef struct aes_key_st AES_KEY;

class CAESHelper
{
public:
	CAESHelper(unsigned char* key);

	~CAESHelper();

	const char *AES_options(void);

	int AES_set_encrypt_key(const unsigned char *userKey, const int bits,
		AES_KEY *key);
	int AES_set_decrypt_key(const unsigned char *userKey, const int bits,
		AES_KEY *key);

	void AES_encrypt(const unsigned char *in, unsigned char *out,
		const AES_KEY *key);
	void AES_decrypt(const unsigned char *in, unsigned char *out,
		const AES_KEY *key);

	void AES_ecb_encrypt(const unsigned char *in, unsigned char *out,
		const AES_KEY *key, const int enc, const unsigned long length);
	void AES_cbc_encrypt(const unsigned char *in, unsigned char *out,
		const unsigned long length, const AES_KEY *key,
		unsigned char *ivec, const int enc);
	void AES_cfb128_encrypt(const unsigned char *in, unsigned char *out,
		const unsigned long length, const AES_KEY *key,
		unsigned char *ivec, int *num, const int enc);
	void AES_ofb128_encrypt(const unsigned char *in, unsigned char *out,
		const unsigned long length, const AES_KEY *key,
		unsigned char *ivec, int *num);
	void AES_ctr128_encrypt(const unsigned char *in, unsigned char *out,
		const unsigned long length, const AES_KEY *key,
		unsigned char ivec[AES_BLOCK_SIZE],
		unsigned char ecount_buf[AES_BLOCK_SIZE],
		unsigned int *num);
	int aes_encrypt(char* in, char* key, char* out);//, int olen)
		int aes_decrypt(char* in, char* key, char* out);

		void CipherStr(const char *input, char *output);
		//Ω‚√‹
		void InvCipherStr(char *inut, char *output);
		void InvCipher(char *inut, char *output);
		void Cipher(char *input, char *output);
		CString CombinedEncode(CString pbPlainText);
		CString CombinedDecode(CString pbCipherText);
private:
	unsigned char* Cipher(unsigned char* input);

	unsigned char* InvCipher(unsigned char* input);
	void* Cipher(void* input, int length = 0);
	void* InvCipher(void* input, int length);

	unsigned char Sbox[256];
	unsigned char InvSbox[256];
	unsigned char w[11][4][4];
	void KeyExpansion(unsigned char* key, unsigned char w[][4][4]);
	unsigned char FFmul(unsigned char a, unsigned char b);
	void SubBytes(unsigned char state[][4]);
	void ShiftRows(unsigned char state[][4]);
	void MixColumns(unsigned char state[][4]);
	void AddRoundKey(unsigned char state[][4], unsigned char k[][4]);
	void InvSubBytes(unsigned char state[][4]);
	void InvShiftRows(unsigned char state[][4]);
	void InvMixColumns(unsigned char state[][4]);
	int strToHex(const char *ch, char *hex);
	int hexToStr(const char *hex, char *ch);
	int ascillToValue(const char ch);
	char valueToHexCh(const int value);
	int getUCharLen(const unsigned char *uch);
	int strToUChar(const char *ch, unsigned char *uch);
	int ucharToStr(const unsigned char *uch, char *ch, int nLen);
	int ucharToHex(const unsigned char *uch, char *hex, int nLen);
	int hexToUChar(const char *hex, unsigned char *uch);
	
	
};

#undef FULL_UNROLL

#endif

