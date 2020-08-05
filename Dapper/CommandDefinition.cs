using System;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;

namespace Dapper
{
	// Token: 0x02000002 RID: 2
	public struct CommandDefinition
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		internal static CommandDefinition ForCallback(object parameters)
		{
			if (parameters is DynamicParameters)
			{
				return new CommandDefinition(parameters);
			}
			return default(CommandDefinition);
		}

		// Token: 0x06000002 RID: 2 RVA: 0x00002075 File Offset: 0x00000275
		internal void OnCompleted()
		{
			SqlMapper.IParameterCallbacks parameterCallbacks = this.Parameters as SqlMapper.IParameterCallbacks;
			if (parameterCallbacks == null)
			{
				return;
			}
			parameterCallbacks.OnCompleted();
		}

		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000003 RID: 3 RVA: 0x0000208C File Offset: 0x0000028C
		public string CommandText { get; }

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000004 RID: 4 RVA: 0x00002094 File Offset: 0x00000294
		public object Parameters { get; }

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000005 RID: 5 RVA: 0x0000209C File Offset: 0x0000029C
		public IDbTransaction Transaction { get; }

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000006 RID: 6 RVA: 0x000020A4 File Offset: 0x000002A4
		public int? CommandTimeout { get; }

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000007 RID: 7 RVA: 0x000020AC File Offset: 0x000002AC
		public CommandType? CommandType { get; }

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000008 RID: 8 RVA: 0x000020B4 File Offset: 0x000002B4
		public bool Buffered
		{
			get
			{
				return (this.Flags & CommandFlags.Buffered) > CommandFlags.None;
			}
		}

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000009 RID: 9 RVA: 0x000020C1 File Offset: 0x000002C1
		internal bool AddToCache
		{
			get
			{
				return (this.Flags & CommandFlags.NoCache) == CommandFlags.None;
			}
		}

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x0600000A RID: 10 RVA: 0x000020CE File Offset: 0x000002CE
		public CommandFlags Flags { get; }

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x0600000B RID: 11 RVA: 0x000020D6 File Offset: 0x000002D6
		public bool Pipelined
		{
			get
			{
				return (this.Flags & CommandFlags.Pipelined) > CommandFlags.None;
			}
		}

		// Token: 0x0600000C RID: 12 RVA: 0x000020E3 File Offset: 0x000002E3
		public CommandDefinition(string commandText, object parameters = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null, CommandFlags flags = CommandFlags.Buffered)
		{
			this.CommandText = commandText;
			this.Parameters = parameters;
			this.Transaction = transaction;
			this.CommandTimeout = commandTimeout;
			this.CommandType = commandType;
			this.Flags = flags;
		}

		// Token: 0x0600000D RID: 13 RVA: 0x00002112 File Offset: 0x00000312
		private CommandDefinition(object parameters)
		{
			this = default(CommandDefinition);
			this.Parameters = parameters;
		}

		// Token: 0x0600000E RID: 14 RVA: 0x00002124 File Offset: 0x00000324
		internal IDbCommand SetupCommand(IDbConnection cnn, Action<IDbCommand, object> paramReader)
		{
			IDbCommand dbCommand = cnn.CreateCommand();
			Action<IDbCommand> init = CommandDefinition.GetInit(dbCommand.GetType());
			if (init != null)
			{
				init(dbCommand);
			}
			if (this.Transaction != null)
			{
				dbCommand.Transaction = this.Transaction;
			}
			dbCommand.CommandText = this.CommandText;
			if (this.CommandTimeout != null)
			{
				dbCommand.CommandTimeout = this.CommandTimeout.Value;
			}
			else if (SqlMapper.Settings.CommandTimeout != null)
			{
				dbCommand.CommandTimeout = SqlMapper.Settings.CommandTimeout.Value;
			}
			if (this.CommandType != null)
			{
				dbCommand.CommandType = this.CommandType.Value;
			}
			if (paramReader != null)
			{
				paramReader(dbCommand, this.Parameters);
			}
			return dbCommand;
		}

		// Token: 0x0600000F RID: 15 RVA: 0x000021EC File Offset: 0x000003EC
		private static Action<IDbCommand> GetInit(Type commandType)
		{
			if (commandType == null)
			{
				return null;
			}
			Action<IDbCommand> result;
			if (SqlMapper.Link<Type, Action<IDbCommand>>.TryGet(CommandDefinition.commandInitCache, commandType, out result))
			{
				return result;
			}
			MethodInfo basicPropertySetter = CommandDefinition.GetBasicPropertySetter(commandType, "BindByName", typeof(bool));
			MethodInfo basicPropertySetter2 = CommandDefinition.GetBasicPropertySetter(commandType, "InitialLONGFetchSize", typeof(int));
			result = null;
			if (basicPropertySetter != null || basicPropertySetter2 != null)
			{
				DynamicMethod dynamicMethod = new DynamicMethod(commandType.Name + "_init", null, new Type[]
				{
					typeof(IDbCommand)
				});
				ILGenerator ilgenerator = dynamicMethod.GetILGenerator();
				if (basicPropertySetter != null)
				{
					ilgenerator.Emit(OpCodes.Ldarg_0);
					ilgenerator.Emit(OpCodes.Castclass, commandType);
					ilgenerator.Emit(OpCodes.Ldc_I4_1);
					ilgenerator.EmitCall(OpCodes.Callvirt, basicPropertySetter, null);
				}
				if (basicPropertySetter2 != null)
				{
					ilgenerator.Emit(OpCodes.Ldarg_0);
					ilgenerator.Emit(OpCodes.Castclass, commandType);
					ilgenerator.Emit(OpCodes.Ldc_I4_M1);
					ilgenerator.EmitCall(OpCodes.Callvirt, basicPropertySetter2, null);
				}
				ilgenerator.Emit(OpCodes.Ret);
				result = (Action<IDbCommand>)dynamicMethod.CreateDelegate(typeof(Action<IDbCommand>));
			}
			SqlMapper.Link<Type, Action<IDbCommand>>.TryAdd(ref CommandDefinition.commandInitCache, commandType, ref result);
			return result;
		}

		// Token: 0x06000010 RID: 16 RVA: 0x00002328 File Offset: 0x00000528
		private static MethodInfo GetBasicPropertySetter(Type declaringType, string name, Type expectedType)
		{
			PropertyInfo property = declaringType.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
			if (property != null && property.CanWrite && property.PropertyType == expectedType && property.GetIndexParameters().Length == 0)
			{
				return property.GetSetMethod();
			}
			return null;
		}

		// Token: 0x04000007 RID: 7
		private static SqlMapper.Link<Type, Action<IDbCommand>> commandInitCache;
	}
}
