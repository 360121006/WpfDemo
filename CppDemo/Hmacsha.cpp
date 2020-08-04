#include "stdafx.h"
#include "Hmacsha.h"
#include "sha256.h"
#include "sha1.h"

CHmacsha::CHmacsha()
{
}


CHmacsha::~CHmacsha()
{
}

CString CHmacsha::bytestohexstring(char* bytes, int bytelength)
{
	string str("");
	string str2("0123456789abcdef");
	for (int i = 0; i<bytelength; i++) {
		int b;
		b = 0x0f & (bytes[i] >> 4);
		char s1 = str2.at(b);
		str.append(1, str2.at(b));
		b = 0x0f & bytes[i];
		str.append(1, str2.at(b));
		char s2 = str2.at(b);
	}
	return CString(str.c_str());
}

void CHmacsha::truncate(char* d1, char* d2,int len)
{
	int i;
	for (i = 0; i < len; i++)
	{
		d2[i] = d1[i];
	}
}


void CHmacsha::hmac_sha256(const char* k, int lk,const char* d, int ld, char* out, int* t)
{
	SHA256Context ictx, octx;
	char isha[SHA256_HASH_SIZE], osha[SHA256_HASH_SIZE];
	char key[SHA256_HASH_SIZE];
	char buf[SHA_BLOCKSIZE];
	int i;

	if (lk > SHA_BLOCKSIZE) {

		SHA256Context tctx;

		SHA256Init(&tctx);
		SHA256Update(&tctx, k, lk);
		SHA256Final(&tctx, (uint8_t*)key);

		k = key;
		lk = SHA256_HASH_SIZE;
	}

	/**** Inner Digest ****/

	SHA256Init(&ictx);

	/* Pad the key for inner digest */
	for (i = 0; i < lk; ++i) buf[i] = k[i] ^ 0x36;
	for (i = lk; i < SHA_BLOCKSIZE; ++i) buf[i] = 0x36;

	SHA256Update(&ictx, buf, SHA_BLOCKSIZE);
	SHA256Update(&ictx, d, ld);

	SHA256Final(&ictx, (uint8_t*)isha);

	/**** Outter Digest ****/

	SHA256Init(&octx);

	/* Pad the key for outter digest */

	for (i = 0; i < lk; ++i) buf[i] = k[i] ^ 0x5C;
	for (i = lk; i < SHA_BLOCKSIZE; ++i) buf[i] = 0x5C;

	SHA256Update(&octx, buf, SHA_BLOCKSIZE);
	SHA256Update(&octx, isha, SHA256_HASH_SIZE);
	SHA256Final(&octx, (uint8_t*)osha);

	/* truncate and print the results */
	*t = *t > SHA256_HASH_SIZE ? SHA256_HASH_SIZE : *t;
	truncate(osha, out, *t);
}


void CHmacsha::sha256(const char* d, int ld, char* out, int* t)
{
	SHA256Context ictx;
	char isha[SHA256_HASH_SIZE];

	/**** Inner Digest ****/
	SHA256Init(&ictx);
	SHA256Update(&ictx, d, ld);
	SHA256Final(&ictx, (uint8_t*)isha);

	*t = *t > SHA256_HASH_SIZE ? SHA256_HASH_SIZE : *t;
	truncate(isha, out, *t);
}

void CHmacsha::sha1(const char* d, int ld, char* out, int* t)
{
	SHA1_CTX ictx;
	char isha[SHA_DIGESTSIZE];

	/**** Inner Digest ****/
	SHA1Init(&ictx);
	SHA1Update(&ictx, (unsigned char*)d, ld);
	SHA1Final((unsigned char*)isha, &ictx);

	*t = *t > SHA_DIGESTSIZE ? SHA_DIGESTSIZE : *t;
	truncate(isha, out, *t);
}

void CHmacsha::hmac_sha1(const char* k, int lk, const char* d, int ld, char* out, int* t)
{
	SHA1_CTX ictx, octx;
	char isha[SHA_DIGESTSIZE], osha[SHA_DIGESTSIZE];
	char key[SHA_DIGESTSIZE];
	char buf[SHA_BLOCKSIZE];
	int i;

	if (lk > SHA_BLOCKSIZE)
	{
		SHA1_CTX tctx;

		SHA1Init(&tctx);
		SHA1Update(&tctx, (unsigned char*)k, lk);
		SHA1Final((unsigned char*)key, &tctx);

		k = key;
		lk = SHA_DIGESTSIZE;
	}

	/**** Inner Digest ****/

	SHA1Init(&ictx);

	/* Pad the key for inner digest */
	for (i = 0; i < lk; ++i) buf[i] = k[i] ^ 0x36;
	for (i = lk; i < SHA_BLOCKSIZE; ++i) buf[i] = 0x36;

	SHA1Update(&ictx, (unsigned char*)buf, SHA_BLOCKSIZE);
	SHA1Update(&ictx, (unsigned char*)d, ld);
	SHA1Final((unsigned char*)isha, &ictx);

	/**** Outter Digest ****/

	SHA1Init(&octx);

	/* Pad the key for outter digest */

	for (i = 0; i < lk; ++i) buf[i] = k[i] ^ 0x5C;
	for (i = lk; i < SHA_BLOCKSIZE; ++i) buf[i] = 0x5C;

	SHA1Update(&octx, (unsigned char*)buf, SHA_BLOCKSIZE);
	SHA1Update(&octx, (unsigned char*)isha, SHA_DIGESTSIZE);
	SHA1Final((unsigned char*)osha, &octx);

	/* truncate and print the results */
	*t = *t > SHA_DIGESTSIZE ? SHA_DIGESTSIZE : *t;
	truncate(osha, out, *t);
}
