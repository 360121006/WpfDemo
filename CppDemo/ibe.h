#ifndef IBE_H
#define IBE_H
/****************************************************************************/
/*                  master是存储主密钥的缓冲区                              */
/*                         ______                                           */
/*                        |master|                                          */
/*                        |______|                                          */
/* 其中：master是生成的系统主密钥                                           */
/* 说明：master的长度为QBYTES，QBYTES=QBITS/8，                             */
/* 其中QBITS是基点阶的位数                                                  */
/****************************************************************************/
/****************************************************************************/
/*               ibcParameter是存贮公共参数的缓冲区                         */
/*                    _____________________                                 */
/*                   |pubX|pubY|pairX|pairY|                                */
/*                   |____|____|_____|_____|                                */
/* 其中：(pubX, pubY)为公共参数点；(pairX, pairY)为e(P,Ppub)计算的配对值    */
/* 椭圆曲线参数为固定的值，所以这儿这儿不用预存了                           */
/* 说明：ibcParameter的长度为4*PBYTES，PBYTES=PBITS/8                       */
/*                                                                          */
/****************************************************************************/ 

/****************************************************************************/
/*               privateKey是存贮用户私钥的缓冲区                           */
/*                    _________________                                     */
/*                   |privateX|privateY|                                    */
/*                   |________|________|                                    */
/* 其中：(privateX, privateY)是生成的用户私钥                               */
/* 说明：privateKey的长度为2*PBYTES，PBYTES=PBITS/8，                       */
/* 其中PBITS是有限域位数                                                    */
/****************************************************************************/
//
//  
//
#define PBITS 1024
#define QBITS 256
#define PBYTES (PBITS/8)
#define QBYTES (QBITS/8)
//#define SIMPLE
#define byte unsigned char
#ifndef HASH_LEN
#define HASH_LEN 32
#endif// HASH_LEN

#define SESSIONKEYLENGTH (2*PBYTES+HASH_LEN)
//////////国密申报指定参数//////////


/*	执行加密运算之字符串加密 
 *	输入参数：
 *		plaintext，需要加密的明文信息；
 *      plainLength，明文长度；
 *      identityMsg，用户身份标识字串；
 *      ibcParameter，公共参数缓冲区。
 *	输出参数：
 *		ciphertext，加密后的密文信息；
 *      cipherLength，密文长度，实际上cipherLength=plainLength+SESSIONKEYLENGTH=plainLength+2*SINGLEKEYLENGTH+HASH_LEN
 *	返回值：
 *		正确返回SUCCESS；标识串长度出错返回STR_LEN_ERROR；公共参数解析错误，返回COMMON_PARSE_ERROR,配对运算出错，返回ALG_CAL_ERROR。
 *	功能：
 *		执行IBC核心算法的第三步加密运算之字符串加密，根据用户的身份标识和系统公共参数，进行加密运算。
 */
int EncString(byte* ciphertext, int *cipherLength, const byte* plaintext, const int plainLength, const char* identityMsg, const char *ibcParameter);

/*	执行解密运算之字符串解密 
 *	输入参数：
 *		ciphertext，需要解密的密文信息；
 *      cipherLength，密文长度；
 *      privateKey，用户私钥缓冲区；
 *      identityMsg，用户的身份信息；
 *      ibcParameter，公共参数缓冲区。
 *	输出参数：
 *		plaintext，解密得到的明文串；
 *      plainLength，明文串长度，实际上plainLength=cipherLength-SESSIONKEYLENGTH= cipherLength-[2*SINGLEKEYLENGTH+HASH_LEN]
 *	返回值：
 *		正确返回SUCCESS；公共参数解析错误返回COMMON_PARSE_ERROR，私钥解析错误返回KEY_PARSE_ERROR；运算错误返回ALG_CAL_ERROR。
 *	功能：
 *		执行IBC核心算法的第四步解密运算之字符串解密，根据用户的私钥和系统公共参数，进行解密运算。
 */
int DecString(byte* plaintext, int *plainLength, const byte* ciphertext, const int cipherLength, const char *privateKey, const char* identityMsg, const char *ibcParameter);

/*	执行加密运算之文件加密 
 *	输入参数：
 *      cipher_keyDir，密文文件需要存放的目录路径；
 *		plainFile，需要加密文件的绝对路径；
 *      identityMsg，用户身份标识字串；
 *      ibcParameter，系统公共参数缓冲区。
 *	输出参数：
 *		void
 *  输出：
        在指定路径生成的密文文件，默认文件名为：原文件名_cipherFile.ibe
 *	返回值：
 *		正确返回SUCCESS；公共参数解析错误返回COMMON_PARSE_ERROR，私钥解析错误返回KEY_PARSE_ERROR；运算错误返回ALG_CAL_ERROR；字符串长度错误返回STR_LEN_ERROR，内存分配错误返回MEM_ALLOC_ERROR,文件操作错误返回FILE_OPT_ERROR
 *	功能：
 *		执行IBC核心算法的第三步加密运算之文件加密，根据用户的身份标识和系统公共参数，进行加密运算。
 */
int EncFile(const char* cipherFileDir, const char* plainFile, const char* identityMsg, const char *ibcParameter);

/*	执行解密运算之文件解密 
 *	输入参数：
 *		decFile，解密后的明文文件的绝对路径；
 *      cipherFile，需要解密的密文文件绝对路径；
 *      privateKey，用户私钥缓冲区；
 *      identityMsg，用户的身份信息 
 *      ibcParameter，系统公共参数缓冲区。
 *	输出参数：
 *		void
 *  输出：
 *      按decFile的绝对路径生成对应的明文文件
 *	返回值：
 *		正确返回SUCCESS；公共参数解析错误返回COMMON_PARSE_ERROR，私钥解析错误返回KEY_PARSE_ERROR；运算错误返回ALG_CAL_ERROR；字符串长度错误返回STR_LEN_ERROR，内存分配错误返回MEM_ALLOC_ERROR,文件操作错误返回FILE_OPT_ERROR。
 *	功能：
 *		执行IBC核心算法的第四步解密运算之文件解密，根据用户的私钥文件和系统公共参数，进行解密运算。
 */
int DecFile(const char* decFile, const char* cipherFile, const char *privateKey,const char* identityMsg, const char *ibcParameter);

/*	执行对字符串的签名运算 
 *	输入参数：
 *		plaintext，需要签名的字符串；
 *      plainLength，串长；
 *      privateKey，用户私钥缓冲区；
 *      ibcParameter，系统公共参数缓冲区。
 *	输出参数：
 *		signature，签名信息,长度为3*SINGLEKEYLENGTH。
 *	返回值：
 *		正确返回SUCCESS；公共参数解析错误返回COMMON_PARSE_ERROR，私钥解析错误返回KEY_PARSE_ERROR；运算错误返回ALG_CAL_ERROR。
 *	功能：
 *		执行IBC核心算法的第五步签名运算之字串签名，根据系统公共参数和用户私钥，进行签名运算。
 */
int SigString(byte* signature, const byte* plaintext, const int plainLength, const char *privateKey, const char *ibcParameter);

/*	执行对字串签名的验证运算 
 *	输入参数：
 *      signature，签名信息；
 *		plaintext，需要验证签名信息的字符串；
 *      plainLength，串长；
 *      identityMsg，用户身份标识字串；
 *      ibcParameter，系统公共参数缓冲区。
 *	输出参数：
 *		void。
 *	返回值：
 *		返回SUCCESS表示验证通过；返回FAIL表示验证失败；返回STR_LEN_ERROR表示串长错误；返回COMMON_PARSE_ERROR表明公钥解析错误，返回ALG_CAL_ERROR表明算法运算错误。
 *	功能：
 *		执行IBC核心算法的第六步验证运算之字串签名验证运算，根据系统公共参数和用户身份标识，进行验证运算。
 */
int VerifyString(const byte* signature,  const int plainLength, const byte* plaintext,const char* identityMsg, const char *ibcParameter);

/*	执行对文件的签名运算 
 *	输入参数：
 *		fileName，需要签名文件的绝对路径；
 *      privateKey，用户私钥缓冲区；
 *      ibcParameter，系统公共参数缓冲区。
 *	输出参数：
 *		签名信息signature。
 *	返回值：
 *		正确返回SUCCESS；公共参数解析错误返回COMMON_PARSE_ERROR，私钥解析错误返回KEY_PARSE_ERROR；运算错误返回ALG_CAL_ERROR。
 *	功能：
 *		执行IBC核心算法的第五步签名运算之文件签名，根据系统公共参数和用户私钥，进行签名运算。
 */
int SigFile(byte* signature, const char *fileName, const char *privateKey, const char *ibcParameter);

/*	执行对文件签名的验证运算 
 *	输入参数：
 *		signature，签名信息；
 *		fileName，需要签名文件的绝对路径；
 *      identityMsg，用户身份标识字串；
 *      ibcParameter，系统公共参数缓冲区。 
 *	输出参数：
 *		void。
 *	返回值：
 *		返回SUCCESS表示验证通过；返回FAIL表示验证失败；返回STR_LEN_ERROR表示串长错误；返回COMMON_PARSE_ERROR表明公钥解析错误，返回ALG_CAL_ERROR表明算法运算错误。
 *	功能：
 *		执行IBC核心算法的第六步验证运算之文件签名验证运算，根据系统公共参数和用户身份标识，进行验证运算。
 */
int VerifyFile(const byte* signature,const char* fileName,const char* identityMsg, const char *ibcParameter);

/*	发送方执行密钥交换的第一步
 *	输入参数：
 *		recvIdentityMsg，接收方的身份信息；
 *		ibcParameter，系统公共参数缓冲区；
 *	输出参数：
 *      send1RandNum，所产生的随机数，长度为SINGLEKEYLENGTH  
 *		send1Msg，发送方第一步要发送给接收方的信息，长度为2*SINGLEKEYLENGTH
 *	返回值：
 *		返回SUCCESS表示成功；返回COMMON_PARSE_ERROR表明公钥解析错误。
 *	功能：
 *		密钥交换的第一步。
 */
int keyExchangeSend1(char *send1RandNum, char *send1Msg, const char* recvIdentityMsg, const char *ibcParameter);

/*	接收方执行密钥交换的第一步
 *	输入参数：
 *		sendIdentityMsg，发送方的身份信息；
 *      send1Msg，发送在第一步keyExchangeSend1中计算并传过来的结果
 *      recvIdntityMsg, 接收方身份信息；
 *      recvPrivateKey，接收方的私钥缓冲区；
 *		ibcParameter，系统公共参数缓冲区；
 *      optional，是否计算可选项验证的变量SB；
 *      klen，接收方计算共享密钥的长度。
 *	输出参数：
 *		skRecv，接收方计算的共享密钥；
 *      hashBuf，计算出来要在最后一步验证的哈希值，长度为HASH_LEN
 *      recv1Msg，接收方计算出来需要发送给发送方的信息。如果optional=false,recv1Msg=RB，长度为2*SINGLEKEYLENGTH
 *                                                      如果optional=true,recv1Msg=RB||SB，长度为2*SINGLEKEYLENGTH+HASH_LEN
 *	返回值：
 *		返回SUCCESS表示成功；返回COMMON_PARSE_ERROR表明公钥解析错误; ALG_CAL_ERROR,配对计算错误。
 *	功能：
 *		密钥交换的第二步。
 */
int keyExchangeRecv1(char *skRecv, char *hashBuf, char *recv1Msg, const char* sendIdentityMsg, char *send1Msg, const char *recvIdntityMsg, const char *recvPrivateKey, const char *ibcParameter, bool optional, const int klen);

/*	发送方执行密钥交换的第二步
 *	输入参数：
 *		sendIdentityMsg，发送方的身份信息；
 *      sendPrivateKey，发送方私钥缓冲区；
 *      send1RandNum，发送方在第一步的随机数ra；
 *      send1Msg，发送在第一步keyExchangeSend1中计算并传过来的结果
 *      recvIdntityMsg, 接收方身份信息；
 *      recv1Msg，接收方在keyExchangeRecv1步骤计算出来的发送给发送方的缓冲区；
 *		ibcParameter，系统公共参数缓冲区；
 *      optional，是否计算可选项验证的变量SB；
 *      klen，接收方计算共享密钥的长度。
 *	输出参数：
 *		skSend，发送方计算的共享密钥；
 *      send2Msg，接收方计算出来需要发送给发送方的信息。如果optional=false,send2Msg 为0
 *                                                      如果optional=true,send2Msg为一哈希值，长度为HASH_LEN
 *	返回值：
 *		返回SUCCESS表示成功；返回COMMON_PARSE_ERROR表明公钥解析错误; ALG_CAL_ERROR,配对计算错误；FAIL表示失败。
 *	功能：
 *		密钥交换的第三步。
 */
int keyExchangeSend2(char *skSend, char *send2Msg, const char* sendIdentityMsg, const char *sendPrivateKey, const char *send1RandNum, const char *send1Msg, const char *recvIdntityMsg, const char *recv1Msg, const char *ibcParameter, bool optional, const int klen);

/*	接收方执行密钥交换的第二步
 *	输入参数：
 *		hashBuf，接收方在keyExchangeRecv1步骤中计算出来的哈希值；
 *      send2Msg，发送方在keyExchangeSend2步骤中计算并发送过来的哈希值
 *	输出参数：void
 *	返回值：
 *		返回SUCCESS表示成功；其它表示验证失败。
 *	功能：
 *		密钥交换的第四步。
 */
int keyExchangeRecv2(const char *hashBuf, const char *send2Msg);
#endif
