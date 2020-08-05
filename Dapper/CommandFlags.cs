using System;

namespace Dapper
{
	// Token: 0x02000003 RID: 3
	[Flags]
	public enum CommandFlags
	{
		// Token: 0x04000009 RID: 9
		None = 0,
		// Token: 0x0400000A RID: 10
		Buffered = 1,
		// Token: 0x0400000B RID: 11
		Pipelined = 2,
		// Token: 0x0400000C RID: 12
		NoCache = 4
	}
}
