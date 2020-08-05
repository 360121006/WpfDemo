using System;
using System.Data;

namespace Dapper
{
	// Token: 0x02000012 RID: 18
	internal class WrappedReader : IDataReader, IDisposable, IDataRecord, IWrappedDataReader
	{
		// Token: 0x1700001D RID: 29
		// (get) Token: 0x060000EA RID: 234 RVA: 0x00008A7A File Offset: 0x00006C7A
		public IDataReader Reader
		{
			get
			{
				IDataReader dataReader = this.reader;
				if (dataReader == null)
				{
					throw new ObjectDisposedException(base.GetType().Name);
				}
				return dataReader;
			}
		}

		// Token: 0x1700001E RID: 30
		// (get) Token: 0x060000EB RID: 235 RVA: 0x00008A96 File Offset: 0x00006C96
		IDbCommand IWrappedDataReader.Command
		{
			get
			{
				IDbCommand dbCommand = this.cmd;
				if (dbCommand == null)
				{
					throw new ObjectDisposedException(base.GetType().Name);
				}
				return dbCommand;
			}
		}

		// Token: 0x060000EC RID: 236 RVA: 0x00008AB2 File Offset: 0x00006CB2
		public WrappedReader(IDbCommand cmd, IDataReader reader)
		{
			this.cmd = cmd;
			this.reader = reader;
		}

		// Token: 0x060000ED RID: 237 RVA: 0x00008AC8 File Offset: 0x00006CC8
		void IDataReader.Close()
		{
			IDataReader dataReader = this.reader;
			if (dataReader == null)
			{
				return;
			}
			dataReader.Close();
		}

		// Token: 0x1700001F RID: 31
		// (get) Token: 0x060000EE RID: 238 RVA: 0x00008ADA File Offset: 0x00006CDA
		int IDataReader.Depth
		{
			get
			{
				return this.Reader.Depth;
			}
		}

		// Token: 0x060000EF RID: 239 RVA: 0x00008AE7 File Offset: 0x00006CE7
		DataTable IDataReader.GetSchemaTable()
		{
			return this.Reader.GetSchemaTable();
		}

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x060000F0 RID: 240 RVA: 0x00008AF4 File Offset: 0x00006CF4
		bool IDataReader.IsClosed
		{
			get
			{
				IDataReader dataReader = this.reader;
				return dataReader == null || dataReader.IsClosed;
			}
		}

		// Token: 0x060000F1 RID: 241 RVA: 0x00008B07 File Offset: 0x00006D07
		bool IDataReader.NextResult()
		{
			return this.Reader.NextResult();
		}

		// Token: 0x060000F2 RID: 242 RVA: 0x00008B14 File Offset: 0x00006D14
		bool IDataReader.Read()
		{
			return this.Reader.Read();
		}

		// Token: 0x17000021 RID: 33
		// (get) Token: 0x060000F3 RID: 243 RVA: 0x00008B21 File Offset: 0x00006D21
		int IDataReader.RecordsAffected
		{
			get
			{
				return this.Reader.RecordsAffected;
			}
		}

		// Token: 0x060000F4 RID: 244 RVA: 0x00008B30 File Offset: 0x00006D30
		void IDisposable.Dispose()
		{
			IDataReader dataReader = this.reader;
			if (dataReader != null)
			{
				dataReader.Close();
			}
			IDataReader dataReader2 = this.reader;
			if (dataReader2 != null)
			{
				dataReader2.Dispose();
			}
			this.reader = null;
			IDbCommand dbCommand = this.cmd;
			if (dbCommand != null)
			{
				dbCommand.Dispose();
			}
			this.cmd = null;
		}

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x060000F5 RID: 245 RVA: 0x00008B7E File Offset: 0x00006D7E
		int IDataRecord.FieldCount
		{
			get
			{
				return this.Reader.FieldCount;
			}
		}

		// Token: 0x060000F6 RID: 246 RVA: 0x00008B8B File Offset: 0x00006D8B
		bool IDataRecord.GetBoolean(int i)
		{
			return this.Reader.GetBoolean(i);
		}

		// Token: 0x060000F7 RID: 247 RVA: 0x00008B99 File Offset: 0x00006D99
		byte IDataRecord.GetByte(int i)
		{
			return this.Reader.GetByte(i);
		}

		// Token: 0x060000F8 RID: 248 RVA: 0x00008BA7 File Offset: 0x00006DA7
		long IDataRecord.GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
		{
			return this.Reader.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
		}

		// Token: 0x060000F9 RID: 249 RVA: 0x00008BBB File Offset: 0x00006DBB
		char IDataRecord.GetChar(int i)
		{
			return this.Reader.GetChar(i);
		}

		// Token: 0x060000FA RID: 250 RVA: 0x00008BC9 File Offset: 0x00006DC9
		long IDataRecord.GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
		{
			return this.Reader.GetChars(i, fieldoffset, buffer, bufferoffset, length);
		}

		// Token: 0x060000FB RID: 251 RVA: 0x00008BDD File Offset: 0x00006DDD
		IDataReader IDataRecord.GetData(int i)
		{
			return this.Reader.GetData(i);
		}

		// Token: 0x060000FC RID: 252 RVA: 0x00008BEB File Offset: 0x00006DEB
		string IDataRecord.GetDataTypeName(int i)
		{
			return this.Reader.GetDataTypeName(i);
		}

		// Token: 0x060000FD RID: 253 RVA: 0x00008BF9 File Offset: 0x00006DF9
		DateTime IDataRecord.GetDateTime(int i)
		{
			return this.Reader.GetDateTime(i);
		}

		// Token: 0x060000FE RID: 254 RVA: 0x00008C07 File Offset: 0x00006E07
		decimal IDataRecord.GetDecimal(int i)
		{
			return this.Reader.GetDecimal(i);
		}

		// Token: 0x060000FF RID: 255 RVA: 0x00008C15 File Offset: 0x00006E15
		double IDataRecord.GetDouble(int i)
		{
			return this.Reader.GetDouble(i);
		}

		// Token: 0x06000100 RID: 256 RVA: 0x00008C23 File Offset: 0x00006E23
		Type IDataRecord.GetFieldType(int i)
		{
			return this.Reader.GetFieldType(i);
		}

		// Token: 0x06000101 RID: 257 RVA: 0x00008C31 File Offset: 0x00006E31
		float IDataRecord.GetFloat(int i)
		{
			return this.Reader.GetFloat(i);
		}

		// Token: 0x06000102 RID: 258 RVA: 0x00008C3F File Offset: 0x00006E3F
		Guid IDataRecord.GetGuid(int i)
		{
			return this.Reader.GetGuid(i);
		}

		// Token: 0x06000103 RID: 259 RVA: 0x00008C4D File Offset: 0x00006E4D
		short IDataRecord.GetInt16(int i)
		{
			return this.Reader.GetInt16(i);
		}

		// Token: 0x06000104 RID: 260 RVA: 0x00008C5B File Offset: 0x00006E5B
		int IDataRecord.GetInt32(int i)
		{
			return this.Reader.GetInt32(i);
		}

		// Token: 0x06000105 RID: 261 RVA: 0x00008C69 File Offset: 0x00006E69
		long IDataRecord.GetInt64(int i)
		{
			return this.Reader.GetInt64(i);
		}

		// Token: 0x06000106 RID: 262 RVA: 0x00008C77 File Offset: 0x00006E77
		string IDataRecord.GetName(int i)
		{
			return this.Reader.GetName(i);
		}

		// Token: 0x06000107 RID: 263 RVA: 0x00008C85 File Offset: 0x00006E85
		int IDataRecord.GetOrdinal(string name)
		{
			return this.Reader.GetOrdinal(name);
		}

		// Token: 0x06000108 RID: 264 RVA: 0x00008C93 File Offset: 0x00006E93
		string IDataRecord.GetString(int i)
		{
			return this.Reader.GetString(i);
		}

		// Token: 0x06000109 RID: 265 RVA: 0x00008CA1 File Offset: 0x00006EA1
		object IDataRecord.GetValue(int i)
		{
			return this.Reader.GetValue(i);
		}

		// Token: 0x0600010A RID: 266 RVA: 0x00008CAF File Offset: 0x00006EAF
		int IDataRecord.GetValues(object[] values)
		{
			return this.Reader.GetValues(values);
		}

		// Token: 0x0600010B RID: 267 RVA: 0x00008CBD File Offset: 0x00006EBD
		bool IDataRecord.IsDBNull(int i)
		{
			return this.Reader.IsDBNull(i);
		}

		// Token: 0x17000023 RID: 35
		object IDataRecord.this[string name]
		{
			get
			{
				return this.Reader[name];
			}
		}

		// Token: 0x17000024 RID: 36
		object IDataRecord.this[int i]
		{
			get
			{
				return this.Reader[i];
			}
		}

		// Token: 0x04000047 RID: 71
		private IDataReader reader;

		// Token: 0x04000048 RID: 72
		private IDbCommand cmd;
	}
}
