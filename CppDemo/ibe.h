#ifndef IBE_H
#define IBE_H
/****************************************************************************/
/*                  master�Ǵ洢����Կ�Ļ�����                              */
/*                         ______                                           */
/*                        |master|                                          */
/*                        |______|                                          */
/* ���У�master�����ɵ�ϵͳ����Կ                                           */
/* ˵����master�ĳ���ΪQBYTES��QBYTES=QBITS/8��                             */
/* ����QBITS�ǻ���׵�λ��                                                  */
/****************************************************************************/
/****************************************************************************/
/*               ibcParameter�Ǵ������������Ļ�����                         */
/*                    _____________________                                 */
/*                   |pubX|pubY|pairX|pairY|                                */
/*                   |____|____|_____|_____|                                */
/* ���У�(pubX, pubY)Ϊ���������㣻(pairX, pairY)Ϊe(P,Ppub)��������ֵ    */
/* ��Բ���߲���Ϊ�̶���ֵ����������������Ԥ����                           */
/* ˵����ibcParameter�ĳ���Ϊ4*PBYTES��PBYTES=PBITS/8                       */
/*                                                                          */
/****************************************************************************/ 

/****************************************************************************/
/*               privateKey�Ǵ����û�˽Կ�Ļ�����                           */
/*                    _________________                                     */
/*                   |privateX|privateY|                                    */
/*                   |________|________|                                    */
/* ���У�(privateX, privateY)�����ɵ��û�˽Կ                               */
/* ˵����privateKey�ĳ���Ϊ2*PBYTES��PBYTES=PBITS/8��                       */
/* ����PBITS��������λ��                                                    */
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
//////////�����걨ָ������//////////


/*	ִ�м�������֮�ַ������� 
 *	���������
 *		plaintext����Ҫ���ܵ�������Ϣ��
 *      plainLength�����ĳ��ȣ�
 *      identityMsg���û���ݱ�ʶ�ִ���
 *      ibcParameter������������������
 *	���������
 *		ciphertext�����ܺ��������Ϣ��
 *      cipherLength�����ĳ��ȣ�ʵ����cipherLength=plainLength+SESSIONKEYLENGTH=plainLength+2*SINGLEKEYLENGTH+HASH_LEN
 *	����ֵ��
 *		��ȷ����SUCCESS����ʶ�����ȳ�����STR_LEN_ERROR�����������������󣬷���COMMON_PARSE_ERROR,��������������ALG_CAL_ERROR��
 *	���ܣ�
 *		ִ��IBC�����㷨�ĵ�������������֮�ַ������ܣ������û�����ݱ�ʶ��ϵͳ�������������м������㡣
 */
int EncString(byte* ciphertext, int *cipherLength, const byte* plaintext, const int plainLength, const char* identityMsg, const char *ibcParameter);

/*	ִ�н�������֮�ַ������� 
 *	���������
 *		ciphertext����Ҫ���ܵ�������Ϣ��
 *      cipherLength�����ĳ��ȣ�
 *      privateKey���û�˽Կ��������
 *      identityMsg���û��������Ϣ��
 *      ibcParameter������������������
 *	���������
 *		plaintext�����ܵõ������Ĵ���
 *      plainLength�����Ĵ����ȣ�ʵ����plainLength=cipherLength-SESSIONKEYLENGTH= cipherLength-[2*SINGLEKEYLENGTH+HASH_LEN]
 *	����ֵ��
 *		��ȷ����SUCCESS�����������������󷵻�COMMON_PARSE_ERROR��˽Կ�������󷵻�KEY_PARSE_ERROR��������󷵻�ALG_CAL_ERROR��
 *	���ܣ�
 *		ִ��IBC�����㷨�ĵ��Ĳ���������֮�ַ������ܣ������û���˽Կ��ϵͳ�������������н������㡣
 */
int DecString(byte* plaintext, int *plainLength, const byte* ciphertext, const int cipherLength, const char *privateKey, const char* identityMsg, const char *ibcParameter);

/*	ִ�м�������֮�ļ����� 
 *	���������
 *      cipher_keyDir�������ļ���Ҫ��ŵ�Ŀ¼·����
 *		plainFile����Ҫ�����ļ��ľ���·����
 *      identityMsg���û���ݱ�ʶ�ִ���
 *      ibcParameter��ϵͳ����������������
 *	���������
 *		void
 *  �����
        ��ָ��·�����ɵ������ļ���Ĭ���ļ���Ϊ��ԭ�ļ���_cipherFile.ibe
 *	����ֵ��
 *		��ȷ����SUCCESS�����������������󷵻�COMMON_PARSE_ERROR��˽Կ�������󷵻�KEY_PARSE_ERROR��������󷵻�ALG_CAL_ERROR���ַ������ȴ��󷵻�STR_LEN_ERROR���ڴ������󷵻�MEM_ALLOC_ERROR,�ļ��������󷵻�FILE_OPT_ERROR
 *	���ܣ�
 *		ִ��IBC�����㷨�ĵ�������������֮�ļ����ܣ������û�����ݱ�ʶ��ϵͳ�������������м������㡣
 */
int EncFile(const char* cipherFileDir, const char* plainFile, const char* identityMsg, const char *ibcParameter);

/*	ִ�н�������֮�ļ����� 
 *	���������
 *		decFile�����ܺ�������ļ��ľ���·����
 *      cipherFile����Ҫ���ܵ������ļ�����·����
 *      privateKey���û�˽Կ��������
 *      identityMsg���û��������Ϣ 
 *      ibcParameter��ϵͳ����������������
 *	���������
 *		void
 *  �����
 *      ��decFile�ľ���·�����ɶ�Ӧ�������ļ�
 *	����ֵ��
 *		��ȷ����SUCCESS�����������������󷵻�COMMON_PARSE_ERROR��˽Կ�������󷵻�KEY_PARSE_ERROR��������󷵻�ALG_CAL_ERROR���ַ������ȴ��󷵻�STR_LEN_ERROR���ڴ������󷵻�MEM_ALLOC_ERROR,�ļ��������󷵻�FILE_OPT_ERROR��
 *	���ܣ�
 *		ִ��IBC�����㷨�ĵ��Ĳ���������֮�ļ����ܣ������û���˽Կ�ļ���ϵͳ�������������н������㡣
 */
int DecFile(const char* decFile, const char* cipherFile, const char *privateKey,const char* identityMsg, const char *ibcParameter);

/*	ִ�ж��ַ�����ǩ������ 
 *	���������
 *		plaintext����Ҫǩ�����ַ�����
 *      plainLength��������
 *      privateKey���û�˽Կ��������
 *      ibcParameter��ϵͳ����������������
 *	���������
 *		signature��ǩ����Ϣ,����Ϊ3*SINGLEKEYLENGTH��
 *	����ֵ��
 *		��ȷ����SUCCESS�����������������󷵻�COMMON_PARSE_ERROR��˽Կ�������󷵻�KEY_PARSE_ERROR��������󷵻�ALG_CAL_ERROR��
 *	���ܣ�
 *		ִ��IBC�����㷨�ĵ��岽ǩ������֮�ִ�ǩ��������ϵͳ�����������û�˽Կ������ǩ�����㡣
 */
int SigString(byte* signature, const byte* plaintext, const int plainLength, const char *privateKey, const char *ibcParameter);

/*	ִ�ж��ִ�ǩ������֤���� 
 *	���������
 *      signature��ǩ����Ϣ��
 *		plaintext����Ҫ��֤ǩ����Ϣ���ַ�����
 *      plainLength��������
 *      identityMsg���û���ݱ�ʶ�ִ���
 *      ibcParameter��ϵͳ����������������
 *	���������
 *		void��
 *	����ֵ��
 *		����SUCCESS��ʾ��֤ͨ��������FAIL��ʾ��֤ʧ�ܣ�����STR_LEN_ERROR��ʾ�������󣻷���COMMON_PARSE_ERROR������Կ�������󣬷���ALG_CAL_ERROR�����㷨�������
 *	���ܣ�
 *		ִ��IBC�����㷨�ĵ�������֤����֮�ִ�ǩ����֤���㣬����ϵͳ�����������û���ݱ�ʶ��������֤���㡣
 */
int VerifyString(const byte* signature,  const int plainLength, const byte* plaintext,const char* identityMsg, const char *ibcParameter);

/*	ִ�ж��ļ���ǩ������ 
 *	���������
 *		fileName����Ҫǩ���ļ��ľ���·����
 *      privateKey���û�˽Կ��������
 *      ibcParameter��ϵͳ����������������
 *	���������
 *		ǩ����Ϣsignature��
 *	����ֵ��
 *		��ȷ����SUCCESS�����������������󷵻�COMMON_PARSE_ERROR��˽Կ�������󷵻�KEY_PARSE_ERROR��������󷵻�ALG_CAL_ERROR��
 *	���ܣ�
 *		ִ��IBC�����㷨�ĵ��岽ǩ������֮�ļ�ǩ��������ϵͳ�����������û�˽Կ������ǩ�����㡣
 */
int SigFile(byte* signature, const char *fileName, const char *privateKey, const char *ibcParameter);

/*	ִ�ж��ļ�ǩ������֤���� 
 *	���������
 *		signature��ǩ����Ϣ��
 *		fileName����Ҫǩ���ļ��ľ���·����
 *      identityMsg���û���ݱ�ʶ�ִ���
 *      ibcParameter��ϵͳ���������������� 
 *	���������
 *		void��
 *	����ֵ��
 *		����SUCCESS��ʾ��֤ͨ��������FAIL��ʾ��֤ʧ�ܣ�����STR_LEN_ERROR��ʾ�������󣻷���COMMON_PARSE_ERROR������Կ�������󣬷���ALG_CAL_ERROR�����㷨�������
 *	���ܣ�
 *		ִ��IBC�����㷨�ĵ�������֤����֮�ļ�ǩ����֤���㣬����ϵͳ�����������û���ݱ�ʶ��������֤���㡣
 */
int VerifyFile(const byte* signature,const char* fileName,const char* identityMsg, const char *ibcParameter);

/*	���ͷ�ִ����Կ�����ĵ�һ��
 *	���������
 *		recvIdentityMsg�����շ��������Ϣ��
 *		ibcParameter��ϵͳ����������������
 *	���������
 *      send1RandNum���������������������ΪSINGLEKEYLENGTH  
 *		send1Msg�����ͷ���һ��Ҫ���͸����շ�����Ϣ������Ϊ2*SINGLEKEYLENGTH
 *	����ֵ��
 *		����SUCCESS��ʾ�ɹ�������COMMON_PARSE_ERROR������Կ��������
 *	���ܣ�
 *		��Կ�����ĵ�һ����
 */
int keyExchangeSend1(char *send1RandNum, char *send1Msg, const char* recvIdentityMsg, const char *ibcParameter);

/*	���շ�ִ����Կ�����ĵ�һ��
 *	���������
 *		sendIdentityMsg�����ͷ��������Ϣ��
 *      send1Msg�������ڵ�һ��keyExchangeSend1�м��㲢�������Ľ��
 *      recvIdntityMsg, ���շ������Ϣ��
 *      recvPrivateKey�����շ���˽Կ��������
 *		ibcParameter��ϵͳ����������������
 *      optional���Ƿ�����ѡ����֤�ı���SB��
 *      klen�����շ����㹲����Կ�ĳ��ȡ�
 *	���������
 *		skRecv�����շ�����Ĺ�����Կ��
 *      hashBuf���������Ҫ�����һ����֤�Ĺ�ϣֵ������ΪHASH_LEN
 *      recv1Msg�����շ����������Ҫ���͸����ͷ�����Ϣ�����optional=false,recv1Msg=RB������Ϊ2*SINGLEKEYLENGTH
 *                                                      ���optional=true,recv1Msg=RB||SB������Ϊ2*SINGLEKEYLENGTH+HASH_LEN
 *	����ֵ��
 *		����SUCCESS��ʾ�ɹ�������COMMON_PARSE_ERROR������Կ��������; ALG_CAL_ERROR,��Լ������
 *	���ܣ�
 *		��Կ�����ĵڶ�����
 */
int keyExchangeRecv1(char *skRecv, char *hashBuf, char *recv1Msg, const char* sendIdentityMsg, char *send1Msg, const char *recvIdntityMsg, const char *recvPrivateKey, const char *ibcParameter, bool optional, const int klen);

/*	���ͷ�ִ����Կ�����ĵڶ���
 *	���������
 *		sendIdentityMsg�����ͷ��������Ϣ��
 *      sendPrivateKey�����ͷ�˽Կ��������
 *      send1RandNum�����ͷ��ڵ�һ���������ra��
 *      send1Msg�������ڵ�һ��keyExchangeSend1�м��㲢�������Ľ��
 *      recvIdntityMsg, ���շ������Ϣ��
 *      recv1Msg�����շ���keyExchangeRecv1�����������ķ��͸����ͷ��Ļ�������
 *		ibcParameter��ϵͳ����������������
 *      optional���Ƿ�����ѡ����֤�ı���SB��
 *      klen�����շ����㹲����Կ�ĳ��ȡ�
 *	���������
 *		skSend�����ͷ�����Ĺ�����Կ��
 *      send2Msg�����շ����������Ҫ���͸����ͷ�����Ϣ�����optional=false,send2Msg Ϊ0
 *                                                      ���optional=true,send2MsgΪһ��ϣֵ������ΪHASH_LEN
 *	����ֵ��
 *		����SUCCESS��ʾ�ɹ�������COMMON_PARSE_ERROR������Կ��������; ALG_CAL_ERROR,��Լ������FAIL��ʾʧ�ܡ�
 *	���ܣ�
 *		��Կ�����ĵ�������
 */
int keyExchangeSend2(char *skSend, char *send2Msg, const char* sendIdentityMsg, const char *sendPrivateKey, const char *send1RandNum, const char *send1Msg, const char *recvIdntityMsg, const char *recv1Msg, const char *ibcParameter, bool optional, const int klen);

/*	���շ�ִ����Կ�����ĵڶ���
 *	���������
 *		hashBuf�����շ���keyExchangeRecv1�����м�������Ĺ�ϣֵ��
 *      send2Msg�����ͷ���keyExchangeSend2�����м��㲢���͹����Ĺ�ϣֵ
 *	���������void
 *	����ֵ��
 *		����SUCCESS��ʾ�ɹ���������ʾ��֤ʧ�ܡ�
 *	���ܣ�
 *		��Կ�����ĵ��Ĳ���
 */
int keyExchangeRecv2(const char *hashBuf, const char *send2Msg);
#endif
