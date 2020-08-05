using System;
using System.Data;

namespace Dapper
{
	// Token: 0x02000006 RID: 6
	public sealed class DbString : SqlMapper.ICustomQueryParameter
	{
		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000019 RID: 25 RVA: 0x00002414 File Offset: 0x00000614
		// (set) Token: 0x0600001A RID: 26 RVA: 0x0000241B File Offset: 0x0000061B
		public static bool IsAnsiDefault { get; set; }

		// Token: 0x0600001B RID: 27 RVA: 0x00002423 File Offset: 0x00000623
		public DbString()
		{
			this.Length = -1;
			this.IsAnsi = DbString.IsAnsiDefault;
		}

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x0600001C RID: 28 RVA: 0x0000243D File Offset: 0x0000063D
		// (set) Token: 0x0600001D RID: 29 RVA: 0x00002445 File Offset: 0x00000645
		public bool IsAnsi { get; set; }

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x0600001E RID: 30 RVA: 0x0000244E File Offset: 0x0000064E
		// (set) Token: 0x0600001F RID: 31 RVA: 0x00002456 File Offset: 0x00000656
		public bool IsFixedLength { get; set; }

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x06000020 RID: 32 RVA: 0x0000245F File Offset: 0x0000065F
		// (set) Token: 0x06000021 RID: 33 RVA: 0x00002467 File Offset: 0x00000667
		public int Length { get; set; }

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x06000022 RID: 34 RVA: 0x00002470 File Offset: 0x00000670
		// (set) Token: 0x06000023 RID: 35 RVA: 0x00002478 File Offset: 0x00000678
		public string Value { get; set; }

		// Token: 0x06000024 RID: 36 RVA: 0x00002484 File Offset: 0x00000684
		public void AddParameter(IDbCommand command, string name)
		{
			if (this.IsFixedLength && this.Length == -1)
			{
				throw new InvalidOperationException("If specifying IsFixedLength,  a Length must also be specified");
			}
			IDbDataParameter dbDataParameter = command.CreateParameter();
			dbDataParameter.ParameterName = name;
			dbDataParameter.Value = SqlMapper.SanitizeParameterValue(this.Value);
			if (this.Length == -1 && this.Value != null && this.Value.Length <= 4000)
			{
				dbDataParameter.Size = 4000;
			}
			else
			{
				dbDataParameter.Size = this.Length;
			}
			dbDataParameter.DbType = (this.IsAnsi ? (this.IsFixedLength ? DbType.AnsiStringFixedLength : DbType.AnsiString) : (this.IsFixedLength ? DbType.StringFixedLength : DbType.String));
			command.Parameters.Add(dbDataParameter);
		}

		// Token: 0x04000010 RID: 16
		public const int DefaultLength = 4000;
	}
}
