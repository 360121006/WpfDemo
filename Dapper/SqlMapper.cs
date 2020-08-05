using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Microsoft.SqlServer.Server;

namespace Dapper
{
	public static class SqlMapper
	{
		private static int GetColumnHash(IDataReader reader, int startBound = 0, int length = -1)
		{
			int num = (length < 0) ? reader.FieldCount : (startBound + length);
			int num2 = -37 * startBound + num;
			for (int i = startBound; i < num; i++)
			{
				object name = reader.GetName(i);
				int num3 = -79 * (num2 * 31 + ((name != null) ? name.GetHashCode() : 0));
				Type fieldType = reader.GetFieldType(i);
				num2 = num3 + ((fieldType != null) ? fieldType.GetHashCode() : 0);
			}
			return num2;
		}

		public static event EventHandler QueryCachePurged;

		private static void OnQueryCachePurged()
		{
			EventHandler queryCachePurged = SqlMapper.QueryCachePurged;
			if (queryCachePurged == null)
			{
				return;
			}
			queryCachePurged(null, EventArgs.Empty);
		}

		private static void SetQueryCache(SqlMapper.Identity key, SqlMapper.CacheInfo value)
		{
			if (Interlocked.Increment(ref SqlMapper.collect) == 1000)
			{
				SqlMapper.CollectCacheGarbage();
			}
			SqlMapper._queryCache[key] = value;
		}

		private static void CollectCacheGarbage()
		{
			try
			{
				foreach (KeyValuePair<SqlMapper.Identity, SqlMapper.CacheInfo> keyValuePair in SqlMapper._queryCache)
				{
					if (keyValuePair.Value.GetHitCount() <= 0)
					{
						SqlMapper.CacheInfo cacheInfo;
						SqlMapper._queryCache.TryRemove(keyValuePair.Key, out cacheInfo);
					}
				}
			}
			finally
			{
				Interlocked.Exchange(ref SqlMapper.collect, 0);
			}
		}

		// Token: 0x0600005A RID: 90 RVA: 0x00003C1C File Offset: 0x00001E1C
		private static bool TryGetQueryCache(SqlMapper.Identity key, out SqlMapper.CacheInfo value)
		{
			if (SqlMapper._queryCache.TryGetValue(key, out value))
			{
				value.RecordHit();
				return true;
			}
			value = null;
			return false;
		}

		public static void PurgeQueryCache()
		{
			SqlMapper._queryCache.Clear();
			SqlMapper.TypeDeserializerCache.Purge();
			SqlMapper.OnQueryCachePurged();
		}

		private static void PurgeQueryCacheByType(Type type)
		{
			foreach (KeyValuePair<SqlMapper.Identity, SqlMapper.CacheInfo> keyValuePair in SqlMapper._queryCache)
			{
				if (keyValuePair.Key.type == type)
				{
					SqlMapper.CacheInfo cacheInfo;
					SqlMapper._queryCache.TryRemove(keyValuePair.Key, out cacheInfo);
				}
			}
			SqlMapper.TypeDeserializerCache.Purge(type);
		}

		public static int GetCachedSQLCount()
		{
			return SqlMapper._queryCache.Count;
		}

		public static IEnumerable<Tuple<string, string, int>> GetCachedSQL(int ignoreHitCountAbove = 2147483647)
		{
			IEnumerable<Tuple<string, string, int>> enumerable = from pair in SqlMapper._queryCache
			select Tuple.Create<string, string, int>(pair.Key.connectionString, pair.Key.sql, pair.Value.GetHitCount());
			if (ignoreHitCountAbove < 2147483647)
			{
				enumerable = from tuple in enumerable
				where tuple.Item3 <= ignoreHitCountAbove
				select tuple;
			}
			return enumerable;
		}

		public static IEnumerable<Tuple<int, int>> GetHashCollissions()
		{
			Dictionary<int, int> dictionary = new Dictionary<int, int>();
			foreach (SqlMapper.Identity identity in SqlMapper._queryCache.Keys)
			{
				int num;
				if (!dictionary.TryGetValue(identity.hashCode, out num))
				{
					dictionary.Add(identity.hashCode, 1);
				}
				else
				{
					dictionary[identity.hashCode] = num + 1;
				}
			}
			return from pair in dictionary
			where pair.Value > 1
			select Tuple.Create<int, int>(pair.Key, pair.Value);
		}

		// Token: 0x06000060 RID: 96 RVA: 0x00003E00 File Offset: 0x00002000
		static SqlMapper()
		{
			Dictionary<Type, DbType> dictionary = new Dictionary<Type, DbType>();
			Type typeFromHandle = typeof(byte);
			dictionary[typeFromHandle] = DbType.Byte;
			Type typeFromHandle2 = typeof(sbyte);
			dictionary[typeFromHandle2] = DbType.SByte;
			Type typeFromHandle3 = typeof(short);
			dictionary[typeFromHandle3] = DbType.Int16;
			Type typeFromHandle4 = typeof(ushort);
			dictionary[typeFromHandle4] = DbType.UInt16;
			Type typeFromHandle5 = typeof(int);
			dictionary[typeFromHandle5] = DbType.Int32;
			Type typeFromHandle6 = typeof(uint);
			dictionary[typeFromHandle6] = DbType.UInt32;
			Type typeFromHandle7 = typeof(long);
			dictionary[typeFromHandle7] = DbType.Int64;
			Type typeFromHandle8 = typeof(ulong);
			dictionary[typeFromHandle8] = DbType.UInt64;
			Type typeFromHandle9 = typeof(float);
			dictionary[typeFromHandle9] = DbType.Single;
			Type typeFromHandle10 = typeof(double);
			dictionary[typeFromHandle10] = DbType.Double;
			Type typeFromHandle11 = typeof(decimal);
			dictionary[typeFromHandle11] = DbType.Decimal;
			Type typeFromHandle12 = typeof(bool);
			dictionary[typeFromHandle12] = DbType.Boolean;
			Type typeFromHandle13 = typeof(string);
			dictionary[typeFromHandle13] = DbType.String;
			Type typeFromHandle14 = typeof(char);
			dictionary[typeFromHandle14] = DbType.StringFixedLength;
			Type typeFromHandle15 = typeof(Guid);
			dictionary[typeFromHandle15] = DbType.Guid;
			Type typeFromHandle16 = typeof(DateTime);
			dictionary[typeFromHandle16] = DbType.DateTime;
			Type typeFromHandle17 = typeof(DateTimeOffset);
			dictionary[typeFromHandle17] = DbType.DateTimeOffset;
			Type typeFromHandle18 = typeof(TimeSpan);
			dictionary[typeFromHandle18] = DbType.Time;
			Type typeFromHandle19 = typeof(byte[]);
			dictionary[typeFromHandle19] = DbType.Binary;
			Type typeFromHandle20 = typeof(byte?);
			dictionary[typeFromHandle20] = DbType.Byte;
			Type typeFromHandle21 = typeof(sbyte?);
			dictionary[typeFromHandle21] = DbType.SByte;
			Type typeFromHandle22 = typeof(short?);
			dictionary[typeFromHandle22] = DbType.Int16;
			Type typeFromHandle23 = typeof(ushort?);
			dictionary[typeFromHandle23] = DbType.UInt16;
			Type typeFromHandle24 = typeof(int?);
			dictionary[typeFromHandle24] = DbType.Int32;
			Type typeFromHandle25 = typeof(uint?);
			dictionary[typeFromHandle25] = DbType.UInt32;
			Type typeFromHandle26 = typeof(long?);
			dictionary[typeFromHandle26] = DbType.Int64;
			Type typeFromHandle27 = typeof(ulong?);
			dictionary[typeFromHandle27] = DbType.UInt64;
			Type typeFromHandle28 = typeof(float?);
			dictionary[typeFromHandle28] = DbType.Single;
			Type typeFromHandle29 = typeof(double?);
			dictionary[typeFromHandle29] = DbType.Double;
			Type typeFromHandle30 = typeof(decimal?);
			dictionary[typeFromHandle30] = DbType.Decimal;
			Type typeFromHandle31 = typeof(bool?);
			dictionary[typeFromHandle31] = DbType.Boolean;
			Type typeFromHandle32 = typeof(char?);
			dictionary[typeFromHandle32] = DbType.StringFixedLength;
			Type typeFromHandle33 = typeof(Guid?);
			dictionary[typeFromHandle33] = DbType.Guid;
			Type typeFromHandle34 = typeof(DateTime?);
			dictionary[typeFromHandle34] = DbType.DateTime;
			Type typeFromHandle35 = typeof(DateTimeOffset?);
			dictionary[typeFromHandle35] = DbType.DateTimeOffset;
			Type typeFromHandle36 = typeof(TimeSpan?);
			dictionary[typeFromHandle36] = DbType.Time;
			Type typeFromHandle37 = typeof(object);
			dictionary[typeFromHandle37] = DbType.Object;
			SqlMapper.typeMap = dictionary;
			SqlMapper.ResetTypeHandlers(false);
		}

		// Token: 0x06000061 RID: 97 RVA: 0x00004392 File Offset: 0x00002592
		public static void ResetTypeHandlers()
		{
			SqlMapper.ResetTypeHandlers(true);
		}

		// Token: 0x06000062 RID: 98 RVA: 0x0000439C File Offset: 0x0000259C
		private static void ResetTypeHandlers(bool clone)
		{
			SqlMapper.typeHandlers = new Dictionary<Type, SqlMapper.ITypeHandler>();
			SqlMapper.AddTypeHandlerImpl(typeof(DataTable), new DataTableHandler(), clone);
			try
			{
				SqlMapper.AddSqlDataRecordsTypeHandler(clone);
			}
			catch
			{
			}
			SqlMapper.AddTypeHandlerImpl(typeof(XmlDocument), new XmlDocumentHandler(), clone);
			SqlMapper.AddTypeHandlerImpl(typeof(XDocument), new XDocumentHandler(), clone);
			SqlMapper.AddTypeHandlerImpl(typeof(XElement), new XElementHandler(), clone);
			SqlMapper.allowedCommandBehaviors = (CommandBehavior)(-1);
		}

		// Token: 0x06000063 RID: 99 RVA: 0x00004428 File Offset: 0x00002628
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void AddSqlDataRecordsTypeHandler(bool clone)
		{
			SqlMapper.AddTypeHandlerImpl(typeof(IEnumerable<SqlDataRecord>), new SqlDataRecordHandler(), clone);
		}

		// Token: 0x06000064 RID: 100 RVA: 0x00004440 File Offset: 0x00002640
		public static void AddTypeMap(Type type, DbType dbType)
		{
			Dictionary<Type, DbType> dictionary = SqlMapper.typeMap;
			DbType dbType2;
			if (dictionary.TryGetValue(type, out dbType2) && dbType2 == dbType)
			{
				return;
			}
			Dictionary<Type, DbType> dictionary2 = new Dictionary<Type, DbType>(dictionary);
			dictionary2[type] = dbType;
			SqlMapper.typeMap = dictionary2;
		}

		// Token: 0x06000065 RID: 101 RVA: 0x00004478 File Offset: 0x00002678
		public static void AddTypeHandler(Type type, SqlMapper.ITypeHandler handler)
		{
			SqlMapper.AddTypeHandlerImpl(type, handler, true);
		}

		// Token: 0x06000066 RID: 102 RVA: 0x00004482 File Offset: 0x00002682
		internal static bool HasTypeHandler(Type type)
		{
			return SqlMapper.typeHandlers.ContainsKey(type);
		}

		// Token: 0x06000067 RID: 103 RVA: 0x00004490 File Offset: 0x00002690
		public static void AddTypeHandlerImpl(Type type, SqlMapper.ITypeHandler handler, bool clone)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			Type type2 = null;
			if (type.IsValueType())
			{
				Type underlyingType = Nullable.GetUnderlyingType(type);
				if (underlyingType == null)
				{
					type2 = typeof(Nullable<>).MakeGenericType(new Type[]
					{
						type
					});
				}
				else
				{
					type2 = type;
					type = underlyingType;
				}
			}
			Dictionary<Type, SqlMapper.ITypeHandler> dictionary = SqlMapper.typeHandlers;
			SqlMapper.ITypeHandler typeHandler;
			if (dictionary.TryGetValue(type, out typeHandler) && handler == typeHandler)
			{
				return;
			}
			Dictionary<Type, SqlMapper.ITypeHandler> dictionary2 = clone ? new Dictionary<Type, SqlMapper.ITypeHandler>(dictionary) : dictionary;
			typeof(SqlMapper.TypeHandlerCache<>).MakeGenericType(new Type[]
			{
				type
			}).GetMethod("SetHandler", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[]
			{
				handler
			});
			if (type2 != null)
			{
				typeof(SqlMapper.TypeHandlerCache<>).MakeGenericType(new Type[]
				{
					type2
				}).GetMethod("SetHandler", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[]
				{
					handler
				});
			}
			if (handler == null)
			{
				dictionary2.Remove(type);
				if (type2 != null)
				{
					dictionary2.Remove(type2);
				}
			}
			else
			{
				dictionary2[type] = handler;
				if (type2 != null)
				{
					dictionary2[type2] = handler;
				}
			}
			SqlMapper.typeHandlers = dictionary2;
		}

		// Token: 0x06000068 RID: 104 RVA: 0x000045C4 File Offset: 0x000027C4
		public static void AddTypeHandler<T>(SqlMapper.TypeHandler<T> handler)
		{
			SqlMapper.AddTypeHandlerImpl(typeof(T), handler, true);
		}

		// Token: 0x06000069 RID: 105 RVA: 0x000045D8 File Offset: 0x000027D8
		[Obsolete("This method is for internal use only", false)]
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static DbType GetDbType(object value)
		{
			if (value == null || value is DBNull)
			{
				return DbType.Object;
			}
			SqlMapper.ITypeHandler typeHandler;
			return SqlMapper.LookupDbType(value.GetType(), "n/a", false, out typeHandler);
		}

		// Token: 0x0600006A RID: 106 RVA: 0x00004608 File Offset: 0x00002808
		[Obsolete("This method is for internal use only", false)]
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static DbType LookupDbType(Type type, string name, bool demand, out SqlMapper.ITypeHandler handler)
		{
			handler = null;
			Type underlyingType = Nullable.GetUnderlyingType(type);
			if (underlyingType != null)
			{
				type = underlyingType;
			}
			if (type.IsEnum() && !SqlMapper.typeMap.ContainsKey(type))
			{
				type = Enum.GetUnderlyingType(type);
			}
			DbType result;
			if (SqlMapper.typeMap.TryGetValue(type, out result))
			{
				return result;
			}
			if (type.FullName == "System.Data.Linq.Binary")
			{
				return DbType.Binary;
			}
			if (SqlMapper.typeHandlers.TryGetValue(type, out handler))
			{
				return DbType.Object;
			}
			if (typeof(IEnumerable).IsAssignableFrom(type))
			{
				return (DbType)(-1);
			}
			string fullName = type.FullName;
			if (fullName == "Microsoft.SqlServer.Types.SqlGeography")
			{
				Type type2 = type;
				SqlMapper.ITypeHandler handler2;
				handler = (handler2 = new SqlMapper.UdtTypeHandler("geography"));
				SqlMapper.AddTypeHandler(type2, handler2);
				return DbType.Object;
			}
			if (fullName == "Microsoft.SqlServer.Types.SqlGeometry")
			{
				Type type3 = type;
				SqlMapper.ITypeHandler handler2;
				handler = (handler2 = new SqlMapper.UdtTypeHandler("geometry"));
				SqlMapper.AddTypeHandler(type3, handler2);
				return DbType.Object;
			}
			if (fullName == "Microsoft.SqlServer.Types.SqlHierarchyId")
			{
				Type type4 = type;
				SqlMapper.ITypeHandler handler2;
				handler = (handler2 = new SqlMapper.UdtTypeHandler("hierarchyid"));
				SqlMapper.AddTypeHandler(type4, handler2);
				return DbType.Object;
			}
			if (demand)
			{
				throw new NotSupportedException(string.Format("The member {0} of type {1} cannot be used as a parameter value", name, type.FullName));
			}
			return DbType.Object;
		}

		// Token: 0x0600006B RID: 107 RVA: 0x00004726 File Offset: 0x00002926
		public static List<T> AsList<T>(this IEnumerable<T> source)
		{
			if (source != null && !(source is List<T>))
			{
				return source.ToList<T>();
			}
			return (List<T>)source;
		}

		// Token: 0x0600006C RID: 108 RVA: 0x00004740 File Offset: 0x00002940
		public static int Execute(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
		{
			CommandDefinition commandDefinition = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.Buffered);
			return cnn.ExecuteImpl(ref commandDefinition);
		}

		// Token: 0x0600006D RID: 109 RVA: 0x00004764 File Offset: 0x00002964
		public static int Execute(this IDbConnection cnn, CommandDefinition command)
		{
			return cnn.ExecuteImpl(ref command);
		}

		// Token: 0x0600006E RID: 110 RVA: 0x00004770 File Offset: 0x00002970
		public static object ExecuteScalar(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
		{
			CommandDefinition commandDefinition = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.Buffered);
			return SqlMapper.ExecuteScalarImpl<object>(cnn, ref commandDefinition);
		}

		// Token: 0x0600006F RID: 111 RVA: 0x00004794 File Offset: 0x00002994
		public static T ExecuteScalar<T>(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
		{
			CommandDefinition commandDefinition = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.Buffered);
			return SqlMapper.ExecuteScalarImpl<T>(cnn, ref commandDefinition);
		}

		// Token: 0x06000070 RID: 112 RVA: 0x000047B8 File Offset: 0x000029B8
		public static object ExecuteScalar(this IDbConnection cnn, CommandDefinition command)
		{
			return SqlMapper.ExecuteScalarImpl<object>(cnn, ref command);
		}

		// Token: 0x06000071 RID: 113 RVA: 0x000047C2 File Offset: 0x000029C2
		public static T ExecuteScalar<T>(this IDbConnection cnn, CommandDefinition command)
		{
			return SqlMapper.ExecuteScalarImpl<T>(cnn, ref command);
		}

		// Token: 0x06000072 RID: 114 RVA: 0x000047CC File Offset: 0x000029CC
		private static IEnumerable GetMultiExec(object param)
		{
			if (!(param is IEnumerable) || param is string || param is IEnumerable<KeyValuePair<string, object>> || param is SqlMapper.IDynamicParameters)
			{
				return null;
			}
			return (IEnumerable)param;
		}

		// Token: 0x06000073 RID: 115 RVA: 0x000047F8 File Offset: 0x000029F8
		private static int ExecuteImpl(this IDbConnection cnn, ref CommandDefinition command)
		{
			object parameters = command.Parameters;
			IEnumerable multiExec = SqlMapper.GetMultiExec(parameters);
			SqlMapper.CacheInfo cacheInfo = null;
			if (multiExec != null)
			{
				bool flag = true;
				int num = 0;
				bool flag2 = cnn.State == ConnectionState.Closed;
				try
				{
					if (flag2)
					{
						cnn.Open();
					}
					using (IDbCommand dbCommand = command.SetupCommand(cnn, null))
					{
						string commandText = null;
						foreach (object obj in multiExec)
						{
							if (flag)
							{
								commandText = dbCommand.CommandText;
								flag = false;
								cacheInfo = SqlMapper.GetCacheInfo(new SqlMapper.Identity(command.CommandText, new CommandType?(dbCommand.CommandType), cnn, null, obj.GetType(), null), obj, command.AddToCache);
							}
							else
							{
								dbCommand.CommandText = commandText;
								dbCommand.Parameters.Clear();
							}
							cacheInfo.ParamReader(dbCommand, obj);
							num += dbCommand.ExecuteNonQuery();
						}
					}
					command.OnCompleted();
				}
				finally
				{
					if (flag2)
					{
						cnn.Close();
					}
				}
				return num;
			}
			if (parameters != null)
			{
				cacheInfo = SqlMapper.GetCacheInfo(new SqlMapper.Identity(command.CommandText, command.CommandType, cnn, null, parameters.GetType(), null), parameters, command.AddToCache);
			}
			return SqlMapper.ExecuteCommand(cnn, ref command, (parameters == null) ? null : cacheInfo.ParamReader);
		}

		// Token: 0x06000074 RID: 116 RVA: 0x00004970 File Offset: 0x00002B70
		public static IDataReader ExecuteReader(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
		{
			CommandDefinition commandDefinition = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.Buffered);
			IDbCommand cmd;
			IDataReader reader = SqlMapper.ExecuteReaderImpl(cnn, ref commandDefinition, CommandBehavior.Default, out cmd);
			return new WrappedReader(cmd, reader);
		}

		// Token: 0x06000075 RID: 117 RVA: 0x000049A0 File Offset: 0x00002BA0
		public static IDataReader ExecuteReader(this IDbConnection cnn, CommandDefinition command)
		{
			IDbCommand cmd;
			IDataReader reader = SqlMapper.ExecuteReaderImpl(cnn, ref command, CommandBehavior.Default, out cmd);
			return new WrappedReader(cmd, reader);
		}

		// Token: 0x06000076 RID: 118 RVA: 0x000049C0 File Offset: 0x00002BC0
		public static IDataReader ExecuteReader(this IDbConnection cnn, CommandDefinition command, CommandBehavior commandBehavior)
		{
			IDbCommand cmd;
			IDataReader reader = SqlMapper.ExecuteReaderImpl(cnn, ref command, commandBehavior, out cmd);
			return new WrappedReader(cmd, reader);
		}


		public static IEnumerable<dynamic> Query(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = null, CommandType? commandType = null)
		{
			return cnn.Query<dynamic>(sql, param, transaction, buffered, commandTimeout, commandType);
		}

		// Token: 0x06000078 RID: 120 RVA: 0x000049F1 File Offset: 0x00002BF1
		public static dynamic QueryFirst(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
		{
			return cnn.QueryFirst(sql, param, transaction, commandTimeout, commandType);
		}

		// Token: 0x06000079 RID: 121 RVA: 0x00004A00 File Offset: 0x00002C00
		public static dynamic QueryFirstOrDefault(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
		{
			return cnn.QueryFirstOrDefault(sql, param, transaction, commandTimeout, commandType);
		}

		// Token: 0x0600007A RID: 122 RVA: 0x00004A0F File Offset: 0x00002C0F
		public static dynamic QuerySingle(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
		{
			return cnn.QuerySingle(sql, param, transaction, commandTimeout, commandType);
		}

		// Token: 0x0600007B RID: 123 RVA: 0x00004A1E File Offset: 0x00002C1E
		public static dynamic QuerySingleOrDefault(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
		{
			return cnn.QuerySingleOrDefault(sql, param, transaction, commandTimeout, commandType);
		}

		// Token: 0x0600007C RID: 124 RVA: 0x00004A30 File Offset: 0x00002C30
		public static IEnumerable<T> Query<T>(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = null, CommandType? commandType = null)
		{
			CommandDefinition command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None);
			IEnumerable<T> enumerable = cnn.QueryImpl<T>(command, typeof(T));
			if (!command.Buffered)
			{
				return enumerable;
			}
			return enumerable.ToList<T>();
		}

		// Token: 0x0600007D RID: 125 RVA: 0x00004A78 File Offset: 0x00002C78
		public static T QueryFirst<T>(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
		{
			CommandDefinition commandDefinition = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.None);
			return SqlMapper.QueryRowImpl<T>(cnn, SqlMapper.Row.First, ref commandDefinition, typeof(T));
		}

		// Token: 0x0600007E RID: 126 RVA: 0x00004AA8 File Offset: 0x00002CA8
		public static T QueryFirstOrDefault<T>(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
		{
			CommandDefinition commandDefinition = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.None);
			return SqlMapper.QueryRowImpl<T>(cnn, SqlMapper.Row.FirstOrDefault, ref commandDefinition, typeof(T));
		}

		// Token: 0x0600007F RID: 127 RVA: 0x00004AD8 File Offset: 0x00002CD8
		public static T QuerySingle<T>(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
		{
			CommandDefinition commandDefinition = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.None);
			return SqlMapper.QueryRowImpl<T>(cnn, SqlMapper.Row.Single, ref commandDefinition, typeof(T));
		}

		// Token: 0x06000080 RID: 128 RVA: 0x00004B08 File Offset: 0x00002D08
		public static T QuerySingleOrDefault<T>(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
		{
			CommandDefinition commandDefinition = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.None);
			return SqlMapper.QueryRowImpl<T>(cnn, SqlMapper.Row.SingleOrDefault, ref commandDefinition, typeof(T));
		}

		// Token: 0x06000081 RID: 129 RVA: 0x00004B38 File Offset: 0x00002D38
		public static IEnumerable<object> Query(this IDbConnection cnn, Type type, string sql, object param = null, IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = null, CommandType? commandType = null)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			CommandDefinition command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None);
			IEnumerable<object> enumerable = cnn.QueryImpl<object>(command, type);
			if (!command.Buffered)
			{
				return enumerable;
			}
			return enumerable.ToList<object>();
		}

		// Token: 0x06000082 RID: 130 RVA: 0x00004B8C File Offset: 0x00002D8C
		public static object QueryFirst(this IDbConnection cnn, Type type, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			CommandDefinition commandDefinition = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.None);
			return SqlMapper.QueryRowImpl<object>(cnn, SqlMapper.Row.First, ref commandDefinition, type);
		}

		// Token: 0x06000083 RID: 131 RVA: 0x00004BC8 File Offset: 0x00002DC8
		public static object QueryFirstOrDefault(this IDbConnection cnn, Type type, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			CommandDefinition commandDefinition = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.None);
			return SqlMapper.QueryRowImpl<object>(cnn, SqlMapper.Row.FirstOrDefault, ref commandDefinition, type);
		}

		// Token: 0x06000084 RID: 132 RVA: 0x00004C04 File Offset: 0x00002E04
		public static object QuerySingle(this IDbConnection cnn, Type type, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			CommandDefinition commandDefinition = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.None);
			return SqlMapper.QueryRowImpl<object>(cnn, SqlMapper.Row.Single, ref commandDefinition, type);
		}

		// Token: 0x06000085 RID: 133 RVA: 0x00004C40 File Offset: 0x00002E40
		public static object QuerySingleOrDefault(this IDbConnection cnn, Type type, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			CommandDefinition commandDefinition = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.None);
			return SqlMapper.QueryRowImpl<object>(cnn, SqlMapper.Row.SingleOrDefault, ref commandDefinition, type);
		}

		// Token: 0x06000086 RID: 134 RVA: 0x00004C7C File Offset: 0x00002E7C
		public static IEnumerable<T> Query<T>(this IDbConnection cnn, CommandDefinition command)
		{
			IEnumerable<T> enumerable = cnn.QueryImpl<T>(command, typeof(T));
			if (!command.Buffered)
			{
				return enumerable;
			}
			return enumerable.ToList<T>();
		}

		// Token: 0x06000087 RID: 135 RVA: 0x00004CAE File Offset: 0x00002EAE
		public static T QueryFirst<T>(this IDbConnection cnn, CommandDefinition command)
		{
			return SqlMapper.QueryRowImpl<T>(cnn, SqlMapper.Row.First, ref command, typeof(T));
		}

		// Token: 0x06000088 RID: 136 RVA: 0x00004CC3 File Offset: 0x00002EC3
		public static T QueryFirstOrDefault<T>(this IDbConnection cnn, CommandDefinition command)
		{
			return SqlMapper.QueryRowImpl<T>(cnn, SqlMapper.Row.FirstOrDefault, ref command, typeof(T));
		}

		// Token: 0x06000089 RID: 137 RVA: 0x00004CD8 File Offset: 0x00002ED8
		public static T QuerySingle<T>(this IDbConnection cnn, CommandDefinition command)
		{
			return SqlMapper.QueryRowImpl<T>(cnn, SqlMapper.Row.Single, ref command, typeof(T));
		}

		// Token: 0x0600008A RID: 138 RVA: 0x00004CED File Offset: 0x00002EED
		public static T QuerySingleOrDefault<T>(this IDbConnection cnn, CommandDefinition command)
		{
			return SqlMapper.QueryRowImpl<T>(cnn, SqlMapper.Row.SingleOrDefault, ref command, typeof(T));
		}

		// Token: 0x0600008B RID: 139 RVA: 0x00004D04 File Offset: 0x00002F04
		public static SqlMapper.GridReader QueryMultiple(this IDbConnection cnn, string sql, object param = null, IDbTransaction transaction = null, int? commandTimeout = null, CommandType? commandType = null)
		{
			CommandDefinition commandDefinition = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, CommandFlags.Buffered);
			return cnn.QueryMultipleImpl(ref commandDefinition);
		}

		// Token: 0x0600008C RID: 140 RVA: 0x00004D28 File Offset: 0x00002F28
		public static SqlMapper.GridReader QueryMultiple(this IDbConnection cnn, CommandDefinition command)
		{
			return cnn.QueryMultipleImpl(ref command);
		}

		// Token: 0x0600008D RID: 141 RVA: 0x00004D34 File Offset: 0x00002F34
		private static SqlMapper.GridReader QueryMultipleImpl(this IDbConnection cnn, ref CommandDefinition command)
		{
			object parameters = command.Parameters;
			SqlMapper.Identity identity = new SqlMapper.Identity(command.CommandText, command.CommandType, cnn, typeof(SqlMapper.GridReader), (parameters != null) ? parameters.GetType() : null, null);
			SqlMapper.CacheInfo cacheInfo = SqlMapper.GetCacheInfo(identity, parameters, command.AddToCache);
			IDbCommand dbCommand = null;
			IDataReader dataReader = null;
			bool flag = cnn.State == ConnectionState.Closed;
			SqlMapper.GridReader result;
			try
			{
				if (flag)
				{
					cnn.Open();
				}
				dbCommand = command.SetupCommand(cnn, cacheInfo.ParamReader);
				dataReader = SqlMapper.ExecuteReaderWithFlagsFallback(dbCommand, flag, CommandBehavior.SequentialAccess);
				SqlMapper.GridReader gridReader = new SqlMapper.GridReader(dbCommand, dataReader, identity, command.Parameters as DynamicParameters, command.AddToCache);
				dbCommand = null;
				flag = false;
				result = gridReader;
			}
			catch
			{
				if (dataReader != null)
				{
					if (!dataReader.IsClosed)
					{
						try
						{
							if (dbCommand != null)
							{
								dbCommand.Cancel();
							}
						}
						catch
						{
						}
					}
					dataReader.Dispose();
				}
				if (dbCommand != null)
				{
					dbCommand.Dispose();
				}
				if (flag)
				{
					cnn.Close();
				}
				throw;
			}
			return result;
		}

		// Token: 0x0600008E RID: 142 RVA: 0x00004E30 File Offset: 0x00003030
		private static IDataReader ExecuteReaderWithFlagsFallback(IDbCommand cmd, bool wasClosed, CommandBehavior behavior)
		{
			IDataReader result;
			try
			{
				result = cmd.ExecuteReader(SqlMapper.GetBehavior(wasClosed, behavior));
			}
			catch (ArgumentException ex)
			{
				if (!SqlMapper.DisableCommandBehaviorOptimizations(behavior, ex))
				{
					throw;
				}
				result = cmd.ExecuteReader(SqlMapper.GetBehavior(wasClosed, behavior));
			}
			return result;
		}

		// Token: 0x0600008F RID: 143 RVA: 0x00004E7C File Offset: 0x0000307C
		private static IEnumerable<T> QueryImpl<T>(this IDbConnection cnn, CommandDefinition command, Type effectiveType)
		{
			object parameters = command.Parameters;
			var identity = new Identity(command.CommandText, command.CommandType, cnn, effectiveType, parameters?.GetType(), null);
			CacheInfo cacheInfo = GetCacheInfo(identity, parameters, command.AddToCache);
			IDbCommand cmd = null;
			IDataReader reader = null;
			bool wasClosed = cnn.State == ConnectionState.Closed;
			try
			{
				cmd = command.SetupCommand(cnn, cacheInfo.ParamReader);
				if (wasClosed)
				{
					cnn.Open();
				}
				reader = ExecuteReaderWithFlagsFallback(cmd, wasClosed, CommandBehavior.SingleResult | CommandBehavior.SequentialAccess);
				wasClosed = false;
				DeserializerState deserializerState = cacheInfo.Deserializer;
				int columnHash = GetColumnHash(reader, 0, -1);
				if (deserializerState.Func == null || deserializerState.Hash != columnHash)
				{
					if (reader.FieldCount == 0)
					{
						yield break;
					}
					deserializerState = (cacheInfo.Deserializer = new DeserializerState(columnHash, GetDeserializer(effectiveType, reader, 0, -1, false)));
					if (command.AddToCache)
					{
						SetQueryCache(identity, cacheInfo);
					}
				}
				Func<IDataReader, object> func = deserializerState.Func;
				Type convertToType = Nullable.GetUnderlyingType(effectiveType) ?? effectiveType;
				while (reader.Read())
				{
					object val = func(reader);
					if (val == null || val is T)
					{
						yield return (T)val;
					}
					else
					{
						yield return (T)Convert.ChangeType(val, convertToType, CultureInfo.InvariantCulture);
					}
					val = null;
				}
				while (reader.NextResult())
				{
				}
				reader.Dispose();
				reader = null;
				command.OnCompleted();
				func = null;
				convertToType = null;
			}
			finally
			{
				if (reader != null)
				{
					if (!reader.IsClosed)
					{
						try
						{
							cmd.Cancel();
						}
						catch
						{
						}
					}
					reader.Dispose();
				}
				if (wasClosed)
				{
					cnn.Close();
				}
				IDbCommand dbCommand = cmd;
				if (dbCommand != null)
				{
					dbCommand.Dispose();
				}
			}
			yield break;
			yield break;
		}

		// Token: 0x06000090 RID: 144 RVA: 0x00004E9A File Offset: 0x0000309A
		private static void ThrowMultipleRows(SqlMapper.Row row)
		{
			if (row == SqlMapper.Row.Single)
			{
				SqlMapper.ErrTwoRows.Single<int>();
				return;
			}
			if (row != SqlMapper.Row.SingleOrDefault)
			{
				throw new InvalidOperationException();
			}
			SqlMapper.ErrTwoRows.SingleOrDefault<int>();
		}

		// Token: 0x06000091 RID: 145 RVA: 0x00004EC3 File Offset: 0x000030C3
		private static void ThrowZeroRows(SqlMapper.Row row)
		{
			if (row == SqlMapper.Row.First)
			{
				SqlMapper.ErrZeroRows.First<int>();
				return;
			}
			if (row != SqlMapper.Row.Single)
			{
				throw new InvalidOperationException();
			}
			SqlMapper.ErrZeroRows.Single<int>();
		}

		// Token: 0x06000092 RID: 146 RVA: 0x00004EEC File Offset: 0x000030EC
		private static T QueryRowImpl<T>(IDbConnection cnn, SqlMapper.Row row, ref CommandDefinition command, Type effectiveType)
		{
			object parameters = command.Parameters;
			SqlMapper.Identity identity = new SqlMapper.Identity(command.CommandText, command.CommandType, cnn, effectiveType, (parameters != null) ? parameters.GetType() : null, null);
			SqlMapper.CacheInfo cacheInfo = SqlMapper.GetCacheInfo(identity, parameters, command.AddToCache);
			IDbCommand dbCommand = null;
			IDataReader dataReader = null;
			bool flag = cnn.State == ConnectionState.Closed;
			T result;
			try
			{
				dbCommand = command.SetupCommand(cnn, cacheInfo.ParamReader);
				if (flag)
				{
					cnn.Open();
				}
				dataReader = SqlMapper.ExecuteReaderWithFlagsFallback(dbCommand, flag, ((row & SqlMapper.Row.Single) != SqlMapper.Row.First) ? (CommandBehavior.SingleResult | CommandBehavior.SequentialAccess) : (CommandBehavior.SingleResult | CommandBehavior.SingleRow | CommandBehavior.SequentialAccess));
				flag = false;
				T t = default(T);
				if (dataReader.Read() && dataReader.FieldCount != 0)
				{
					SqlMapper.DeserializerState deserializerState = cacheInfo.Deserializer;
					int columnHash = SqlMapper.GetColumnHash(dataReader, 0, -1);
					if (deserializerState.Func == null || deserializerState.Hash != columnHash)
					{
						deserializerState = (cacheInfo.Deserializer = new SqlMapper.DeserializerState(columnHash, SqlMapper.GetDeserializer(effectiveType, dataReader, 0, -1, false)));
						if (command.AddToCache)
						{
							SqlMapper.SetQueryCache(identity, cacheInfo);
						}
					}
					object obj = deserializerState.Func(dataReader);
					if (obj == null || obj is T)
					{
						t = (T)((object)obj);
					}
					else
					{
						Type conversionType = Nullable.GetUnderlyingType(effectiveType) ?? effectiveType;
						t = (T)((object)Convert.ChangeType(obj, conversionType, CultureInfo.InvariantCulture));
					}
					if ((row & SqlMapper.Row.Single) != SqlMapper.Row.First && dataReader.Read())
					{
						SqlMapper.ThrowMultipleRows(row);
					}
					while (dataReader.Read())
					{
					}
				}
				else if ((row & SqlMapper.Row.FirstOrDefault) == SqlMapper.Row.First)
				{
					SqlMapper.ThrowZeroRows(row);
				}
				while (dataReader.NextResult())
				{
				}
				dataReader.Dispose();
				dataReader = null;
				command.OnCompleted();
				result = t;
			}
			finally
			{
				if (dataReader != null)
				{
					if (!dataReader.IsClosed)
					{
						try
						{
							dbCommand.Cancel();
						}
						catch
						{
						}
					}
					dataReader.Dispose();
				}
				if (flag)
				{
					cnn.Close();
				}
				if (dbCommand != null)
				{
					dbCommand.Dispose();
				}
			}
			return result;
		}

		// Token: 0x06000093 RID: 147 RVA: 0x000050E0 File Offset: 0x000032E0
		public static IEnumerable<TReturn> Query<TFirst, TSecond, TReturn>(this IDbConnection cnn, string sql, Func<TFirst, TSecond, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
		{
			return cnn.MultiMap<TFirst, TSecond, TFirst, TFirst, TFirst, TFirst, TFirst,TReturn>(sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);
		}

		// Token: 0x06000094 RID: 148 RVA: 0x00005100 File Offset: 0x00003300
		public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TReturn>(this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
		{
			return cnn.MultiMap<TFirst, TSecond, TThird, TFirst, TFirst, TFirst, TFirst, TReturn>(sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);
		}

		// Token: 0x06000095 RID: 149 RVA: 0x00005120 File Offset: 0x00003320
		public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TReturn>(this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
		{
			return cnn.MultiMap<TFirst, TSecond, TThird, TFourth, TFirst, TFirst, TFirst, TReturn>(sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);
		}

		// Token: 0x06000096 RID: 150 RVA: 0x00005140 File Offset: 0x00003340
		public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
		{
			return cnn.MultiMap<TFirst, TSecond, TThird, TFourth, TFifth, TFirst, TFirst, TReturn>(sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);
		}

		// Token: 0x06000097 RID: 151 RVA: 0x00005160 File Offset: 0x00003360
		public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
		{
			return cnn.MultiMap<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TFirst, TReturn>(sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);
		}

		// Token: 0x06000098 RID: 152 RVA: 0x00005180 File Offset: 0x00003380
		public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(this IDbConnection cnn, string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
		{
			return cnn.MultiMap<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType);
		}

		// Token: 0x06000099 RID: 153 RVA: 0x000051A0 File Offset: 0x000033A0
		public static IEnumerable<TReturn> Query<TReturn>(this IDbConnection cnn, string sql, Type[] types, Func<object[], TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
		{
			CommandDefinition command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None);
			IEnumerable<TReturn> enumerable = cnn.MultiMapImpl(command, types, map, splitOn, null, null, true);
			if (!buffered)
			{
				return enumerable;
			}
			return enumerable.ToList<TReturn>();
		}

		// Token: 0x0600009A RID: 154 RVA: 0x000051E4 File Offset: 0x000033E4
		private static IEnumerable<TReturn> MultiMap<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(this IDbConnection cnn, string sql, Delegate map, object param, IDbTransaction transaction, bool buffered, string splitOn, int? commandTimeout, CommandType? commandType)
		{
			CommandDefinition command = new CommandDefinition(sql, param, transaction, commandTimeout, commandType, buffered ? CommandFlags.Buffered : CommandFlags.None);
			IEnumerable<TReturn> enumerable = cnn.MultiMapImpl<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(command, map, splitOn, null, null, true);
			if (!buffered)
			{
				return enumerable;
			}
			return enumerable.ToList<TReturn>();
		}

		// Token: 0x0600009B RID: 155 RVA: 0x00005224 File Offset: 0x00003424
		private static IEnumerable<TReturn> MultiMapImpl<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(this IDbConnection cnn, CommandDefinition command, Delegate map, string splitOn, IDataReader reader, SqlMapper.Identity identity, bool finalize)
		{
			object parameters = command.Parameters;
			SqlMapper.Identity identity2;
			if ((identity2 = identity) == null)
			{
				identity2 = new SqlMapper.Identity(command.CommandText, command.CommandType, cnn, typeof(TFirst), parameters?.GetType(), new Type[]
				{
					typeof(TFirst),
					typeof(TSecond),
					typeof(TThird),
					typeof(TFourth),
					typeof(TFifth),
					typeof(TSixth),
					typeof(TSeventh)
				});
			}
			identity = identity2;
			SqlMapper.CacheInfo cacheInfo = SqlMapper.GetCacheInfo(identity, parameters, command.AddToCache);
			IDbCommand ownedCommand = null;
			IDataReader ownedReader = null;
			bool wasClosed = cnn != null && cnn.State == ConnectionState.Closed;
			try
			{
				if (reader == null)
				{
					ownedCommand = command.SetupCommand(cnn, cacheInfo.ParamReader);
					if (wasClosed)
					{
						cnn.Open();
					}
					ownedReader = SqlMapper.ExecuteReaderWithFlagsFallback(ownedCommand, wasClosed, CommandBehavior.SingleResult | CommandBehavior.SequentialAccess);
					reader = ownedReader;
				}
				SqlMapper.DeserializerState deserializerState = default(SqlMapper.DeserializerState);
				int columnHash = SqlMapper.GetColumnHash(reader, 0, -1);
				Func<IDataReader, object>[] otherDeserializers;
				if ((deserializerState = cacheInfo.Deserializer).Func == null || (otherDeserializers = cacheInfo.OtherDeserializers) == null || columnHash != deserializerState.Hash)
				{
					Func<IDataReader, object>[] array = SqlMapper.GenerateDeserializers(new Type[]
					{
						typeof(TFirst),
						typeof(TSecond),
						typeof(TThird),
						typeof(TFourth),
						typeof(TFifth),
						typeof(TSixth),
						typeof(TSeventh)
					}, splitOn, reader);
					deserializerState = (cacheInfo.Deserializer = new SqlMapper.DeserializerState(columnHash, array[0]));
					otherDeserializers = (cacheInfo.OtherDeserializers = array.Skip(1).ToArray<Func<IDataReader, object>>());
					if (command.AddToCache)
					{
						SqlMapper.SetQueryCache(identity, cacheInfo);
					}
				}
				Func<IDataReader, TReturn> mapIt = SqlMapper.GenerateMapper<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(deserializerState.Func, otherDeserializers, map);
				if (mapIt != null)
				{
					while (reader.Read())
					{
						yield return mapIt(reader);
					}
					if (finalize)
					{
						while (reader.NextResult())
						{
						}
						command.OnCompleted();
					}
				}
				mapIt = null;
			}
			finally
			{
				try
				{
					IDataReader dataReader = ownedReader;
                    dataReader?.Dispose();
                }
				finally
				{
					IDbCommand dbCommand = ownedCommand;
                    dbCommand?.Dispose();
                    if (wasClosed)
					{
						cnn.Close();
					}
				}
			}
			yield break;
			yield break;
		}

		// Token: 0x0600009C RID: 156 RVA: 0x00005261 File Offset: 0x00003461
		private static bool DisableCommandBehaviorOptimizations(CommandBehavior behavior, Exception ex)
		{
			if (SqlMapper.allowedCommandBehaviors == (CommandBehavior)(-1) && (behavior & (CommandBehavior.SingleResult | CommandBehavior.SingleRow)) != CommandBehavior.Default && (ex.Message.Contains("SingleResult") || ex.Message.Contains("SingleRow")))
			{
				SqlMapper.allowedCommandBehaviors = ~(CommandBehavior.SingleResult | CommandBehavior.SingleRow);
				return true;
			}
			return false;
		}

		// Token: 0x0600009D RID: 157 RVA: 0x0000529F File Offset: 0x0000349F
		private static CommandBehavior GetBehavior(bool close, CommandBehavior @default)
		{
			return (close ? (@default | CommandBehavior.CloseConnection) : @default) & SqlMapper.allowedCommandBehaviors;
		}

		// Token: 0x0600009E RID: 158 RVA: 0x000052B4 File Offset: 0x000034B4
		private static IEnumerable<TReturn> MultiMapImpl<TReturn>(this IDbConnection cnn, CommandDefinition command, Type[] types, Func<object[], TReturn> map, string splitOn, IDataReader reader, SqlMapper.Identity identity, bool finalize)
		{
			if (types.Length < 1)
			{
				throw new ArgumentException("you must provide at least one type to deserialize");
			}
			object parameters = command.Parameters;
			SqlMapper.Identity identity2;
			if ((identity2 = identity) == null)
			{
				identity2 = new SqlMapper.Identity(command.CommandText, command.CommandType, cnn, types[0], (parameters != null) ? parameters.GetType() : null, types);
			}
			identity = identity2;
			SqlMapper.CacheInfo cacheInfo = SqlMapper.GetCacheInfo(identity, parameters, command.AddToCache);
			IDbCommand ownedCommand = null;
			IDataReader ownedReader = null;
			bool wasClosed = cnn != null && cnn.State == ConnectionState.Closed;
			try
			{
				if (reader == null)
				{
					ownedCommand = command.SetupCommand(cnn, cacheInfo.ParamReader);
					if (wasClosed)
					{
						cnn.Open();
					}
					ownedReader = SqlMapper.ExecuteReaderWithFlagsFallback(ownedCommand, wasClosed, CommandBehavior.SingleResult | CommandBehavior.SequentialAccess);
					reader = ownedReader;
				}
				int columnHash = SqlMapper.GetColumnHash(reader, 0, -1);
				SqlMapper.DeserializerState deserializerState;
				Func<IDataReader, object>[] otherDeserializers;
				if ((deserializerState = cacheInfo.Deserializer).Func == null || (otherDeserializers = cacheInfo.OtherDeserializers) == null || columnHash != deserializerState.Hash)
				{
					Func<IDataReader, object>[] array = SqlMapper.GenerateDeserializers(types, splitOn, reader);
					deserializerState = (cacheInfo.Deserializer = new SqlMapper.DeserializerState(columnHash, array[0]));
					otherDeserializers = (cacheInfo.OtherDeserializers = array.Skip(1).ToArray<Func<IDataReader, object>>());
					SqlMapper.SetQueryCache(identity, cacheInfo);
				}
				Func<IDataReader, TReturn> mapIt = SqlMapper.GenerateMapper<TReturn>(types.Length, deserializerState.Func, otherDeserializers, map);
				if (mapIt != null)
				{
					while (reader.Read())
					{
						yield return mapIt(reader);
					}
					if (finalize)
					{
						while (reader.NextResult())
						{
						}
						command.OnCompleted();
					}
				}
				mapIt = null;
			}
			finally
			{
				try
				{
					IDataReader dataReader = ownedReader;
					if (dataReader != null)
					{
						dataReader.Dispose();
					}
				}
				finally
				{
					IDbCommand dbCommand = ownedCommand;
					if (dbCommand != null)
					{
						dbCommand.Dispose();
					}
					if (wasClosed)
					{
						cnn.Close();
					}
				}
			}
			yield break;
			yield break;
		}

		// Token: 0x0600009F RID: 159 RVA: 0x00005304 File Offset: 0x00003504
		private static Func<IDataReader, TReturn> GenerateMapper<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(Func<IDataReader, object> deserializer, Func<IDataReader, object>[] otherDeserializers, object map)
		{
			switch (otherDeserializers.Length)
			{
			case 1:
				return (IDataReader r) => ((Func<TFirst, TSecond, TReturn>)map)((TFirst)((object)deserializer(r)), (TSecond)((object)otherDeserializers[0](r)));
			case 2:
				return (IDataReader r) => ((Func<TFirst, TSecond, TThird, TReturn>)map)((TFirst)((object)deserializer(r)), (TSecond)((object)otherDeserializers[0](r)), (TThird)((object)otherDeserializers[1](r)));
			case 3:
				return (IDataReader r) => ((Func<TFirst, TSecond, TThird, TFourth, TReturn>)map)((TFirst)((object)deserializer(r)), (TSecond)((object)otherDeserializers[0](r)), (TThird)((object)otherDeserializers[1](r)), (TFourth)((object)otherDeserializers[2](r)));
			case 4:
				return (IDataReader r) => ((Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>)map)((TFirst)((object)deserializer(r)), (TSecond)((object)otherDeserializers[0](r)), (TThird)((object)otherDeserializers[1](r)), (TFourth)((object)otherDeserializers[2](r)), (TFifth)((object)otherDeserializers[3](r)));
			case 5:
				return (IDataReader r) => ((Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>)map)((TFirst)((object)deserializer(r)), (TSecond)((object)otherDeserializers[0](r)), (TThird)((object)otherDeserializers[1](r)), (TFourth)((object)otherDeserializers[2](r)), (TFifth)((object)otherDeserializers[3](r)), (TSixth)((object)otherDeserializers[4](r)));
			case 6:
				return (IDataReader r) => ((Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>)map)((TFirst)((object)deserializer(r)), (TSecond)((object)otherDeserializers[0](r)), (TThird)((object)otherDeserializers[1](r)), (TFourth)((object)otherDeserializers[2](r)), (TFifth)((object)otherDeserializers[3](r)), (TSixth)((object)otherDeserializers[4](r)), (TSeventh)((object)otherDeserializers[5](r)));
			default:
				throw new NotSupportedException();
			}
		}

		// Token: 0x060000A0 RID: 160 RVA: 0x000053AA File Offset: 0x000035AA
		private static Func<IDataReader, TReturn> GenerateMapper<TReturn>(int length, Func<IDataReader, object> deserializer, Func<IDataReader, object>[] otherDeserializers, Func<object[], TReturn> map)
		{
			return delegate(IDataReader r)
			{
				object[] array = new object[length];
				array[0] = deserializer(r);
				for (int i = 1; i < length; i++)
				{
					array[i] = otherDeserializers[i - 1](r);
				}
				return map(array);
			};
		}

		// Token: 0x060000A1 RID: 161 RVA: 0x000053D8 File Offset: 0x000035D8
		private static Func<IDataReader, object>[] GenerateDeserializers(Type[] types, string splitOn, IDataReader reader)
		{
			List<Func<IDataReader, object>> list = new List<Func<IDataReader, object>>();
			string[] array = (from s in splitOn.Split(new char[]
			{
				','
			})
			select s.Trim()).ToArray<string>();
			bool flag = array.Length > 1;
			if (types.First<Type>() == typeof(object))
			{
				bool flag2 = true;
				int num = 0;
				int num2 = 0;
				string splitOn2 = array[num2];
				foreach (Type type in types)
				{
					if (type == typeof(SqlMapper.DontMap))
					{
						break;
					}
					int nextSplitDynamic = SqlMapper.GetNextSplitDynamic(num, splitOn2, reader);
					if (flag && num2 < array.Length - 1)
					{
						splitOn2 = array[++num2];
					}
					list.Add(SqlMapper.GetDeserializer(type, reader, num, nextSplitDynamic - num, !flag2));
					num = nextSplitDynamic;
					flag2 = false;
				}
			}
			else
			{
				int num3 = reader.FieldCount;
				int num4 = array.Length - 1;
				string splitOn3 = array[num4];
				for (int j = types.Length - 1; j >= 0; j--)
				{
					Type type2 = types[j];
					if (!(type2 == typeof(SqlMapper.DontMap)))
					{
						int num5 = 0;
						if (j > 0)
						{
							num5 = SqlMapper.GetNextSplit(num3, splitOn3, reader);
							if (flag && num4 > 0)
							{
								splitOn3 = array[--num4];
							}
						}
						list.Add(SqlMapper.GetDeserializer(type2, reader, num5, num3 - num5, j > 0));
						num3 = num5;
					}
				}
				list.Reverse();
			}
			return list.ToArray();
		}

		// Token: 0x060000A2 RID: 162 RVA: 0x00005564 File Offset: 0x00003764
		private static int GetNextSplitDynamic(int startIdx, string splitOn, IDataReader reader)
		{
			if (startIdx == reader.FieldCount)
			{
				throw SqlMapper.MultiMapException(reader);
			}
			if (splitOn == "*")
			{
				return ++startIdx;
			}
			for (int i = startIdx + 1; i < reader.FieldCount; i++)
			{
				if (string.Equals(splitOn, reader.GetName(i), StringComparison.OrdinalIgnoreCase))
				{
					return i;
				}
			}
			return reader.FieldCount;
		}

		// Token: 0x060000A3 RID: 163 RVA: 0x000055C0 File Offset: 0x000037C0
		private static int GetNextSplit(int startIdx, string splitOn, IDataReader reader)
		{
			if (splitOn == "*")
			{
				return --startIdx;
			}
			for (int i = startIdx - 1; i > 0; i--)
			{
				if (string.Equals(splitOn, reader.GetName(i), StringComparison.OrdinalIgnoreCase))
				{
					return i;
				}
			}
			throw SqlMapper.MultiMapException(reader);
		}

		private static CacheInfo GetCacheInfo(Identity identity, object exampleParameters, bool addToCache)
		{
			CacheInfo cacheInfo;
			if (!TryGetQueryCache(identity, out cacheInfo))
			{
				if (GetMultiExec(exampleParameters) != null)
				{
					throw new InvalidOperationException("An enumerable sequence of parameters (arrays, lists, etc) is not allowed in this context");
				}
				cacheInfo = new SqlMapper.CacheInfo();
				if (identity.parametersType != null)
				{
					Action<IDbCommand, object> action;
					if (exampleParameters is SqlMapper.IDynamicParameters)
					{
						action = delegate(IDbCommand cmd, object obj)
						{
							((SqlMapper.IDynamicParameters)obj).AddParameters(cmd, identity);
						};
					}
					else if (exampleParameters is IEnumerable<KeyValuePair<string, object>>)
					{
						action = delegate(IDbCommand cmd, object obj)
						{
							((SqlMapper.IDynamicParameters)new DynamicParameters(obj)).AddParameters(cmd, identity);
						};
					}
					else
					{
						IList<SqlMapper.LiteralToken> literals = SqlMapper.GetLiteralTokens(identity.sql);
						action = SqlMapper.CreateParamInfoGenerator(identity, false, true, literals);
					}
					if ((identity.commandType == null || identity.commandType == CommandType.Text) && SqlMapper.ShouldPassByPosition(identity.sql))
					{
						Action<IDbCommand, object> tail = action;
						action = delegate(IDbCommand cmd, object obj)
						{
							tail(cmd, obj);
							SqlMapper.PassByPosition(cmd);
						};
					}
					cacheInfo.ParamReader = action;
				}
				if (addToCache)
				{
					SqlMapper.SetQueryCache(identity, cacheInfo);
				}
			}
			return cacheInfo;
		}

		// Token: 0x060000A5 RID: 165 RVA: 0x0000572F File Offset: 0x0000392F
		private static bool ShouldPassByPosition(string sql)
		{
			return sql != null && sql.IndexOf('?') >= 0 && SqlMapper.pseudoPositional.IsMatch(sql);
		}

		// Token: 0x060000A6 RID: 166 RVA: 0x0000574C File Offset: 0x0000394C
		private static void PassByPosition(IDbCommand cmd)
		{
			if (cmd.Parameters.Count == 0)
			{
				return;
			}
			Dictionary<string, IDbDataParameter> parameters = new Dictionary<string, IDbDataParameter>(StringComparer.Ordinal);
			foreach (object obj in cmd.Parameters)
			{
				IDbDataParameter dbDataParameter = (IDbDataParameter)obj;
				if (!string.IsNullOrEmpty(dbDataParameter.ParameterName))
				{
					parameters[dbDataParameter.ParameterName] = dbDataParameter;
				}
			}
			HashSet<string> consumed = new HashSet<string>(StringComparer.Ordinal);
			bool firstMatch = true;
			cmd.CommandText = SqlMapper.pseudoPositional.Replace(cmd.CommandText, delegate(Match match)
			{
				string value = match.Groups[1].Value;
				if (!consumed.Add(value))
				{
					throw new InvalidOperationException("When passing parameters by position, each parameter can only be referenced once");
				}
				IDbDataParameter value2;
				if (parameters.TryGetValue(value, out value2))
				{
					if (firstMatch)
					{
						firstMatch = false;
						cmd.Parameters.Clear();
					}
					cmd.Parameters.Add(value2);
					parameters.Remove(value);
					consumed.Add(value);
					return "?";
				}
				return match.Value;
			});
		}

		// Token: 0x060000A7 RID: 167 RVA: 0x00005838 File Offset: 0x00003A38
		private static Func<IDataReader, object> GetDeserializer(Type type, IDataReader reader, int startBound, int length, bool returnNullIfFirstMissing)
		{
			if (type == typeof(object) || type == typeof(SqlMapper.DapperRow))
			{
				return SqlMapper.GetDapperRowDeserializer(reader, startBound, length, returnNullIfFirstMissing);
			}
			Type type2 = null;
			if (SqlMapper.typeMap.ContainsKey(type) || type.IsEnum() || type.FullName == "System.Data.Linq.Binary" || (type.IsValueType() && (type2 = Nullable.GetUnderlyingType(type)) != null && type2.IsEnum()))
			{
				return SqlMapper.GetStructDeserializer(type, type2 ?? type, startBound);
			}
			SqlMapper.ITypeHandler handler;
			if (SqlMapper.typeHandlers.TryGetValue(type, out handler))
			{
				return SqlMapper.GetHandlerDeserializer(handler, type, startBound);
			}
			return SqlMapper.GetTypeDeserializer(type, reader, startBound, length, returnNullIfFirstMissing);
		}

		// Token: 0x060000A8 RID: 168 RVA: 0x000058EE File Offset: 0x00003AEE
		private static Func<IDataReader, object> GetHandlerDeserializer(SqlMapper.ITypeHandler handler, Type type, int startBound)
		{
			return (IDataReader reader) => handler.Parse(type, reader.GetValue(startBound));
		}

		// Token: 0x060000A9 RID: 169 RVA: 0x00005918 File Offset: 0x00003B18
		private static Exception MultiMapException(IDataRecord reader)
		{
			bool flag = false;
			try
			{
				flag = (reader != null && reader.FieldCount != 0);
			}
			catch
			{
			}
			if (flag)
			{
				return new ArgumentException("When using the multi-mapping APIs ensure you set the splitOn param if you have keys other than Id", "splitOn");
			}
			return new InvalidOperationException("No columns were selected");
		}

		// Token: 0x060000AA RID: 170 RVA: 0x0000596C File Offset: 0x00003B6C
		internal static Func<IDataReader, object> GetDapperRowDeserializer(IDataRecord reader, int startBound, int length, bool returnNullIfFirstMissing)
		{
			int fieldCount = reader.FieldCount;
			if (length == -1)
			{
				length = fieldCount - startBound;
			}
			if (fieldCount <= startBound)
			{
				throw SqlMapper.MultiMapException(reader);
			}
			int effectiveFieldCount = Math.Min(fieldCount - startBound, length);
			SqlMapper.DapperTable table = null;
			return delegate(IDataReader r)
			{
				if (table == null)
				{
					string[] array = new string[effectiveFieldCount];
					for (int i = 0; i < effectiveFieldCount; i++)
					{
						array[i] = r.GetName(i + startBound);
					}
					table = new SqlMapper.DapperTable(array);
				}
				object[] array2 = new object[effectiveFieldCount];
				if (returnNullIfFirstMissing)
				{
					array2[0] = r.GetValue(startBound);
					if (array2[0] is DBNull)
					{
						return null;
					}
				}
				if (startBound == 0)
				{
					for (int j = 0; j < array2.Length; j++)
					{
						object value = r.GetValue(j);
						array2[j] = ((value is DBNull) ? null : value);
					}
				}
				else
				{
					for (int k = returnNullIfFirstMissing ? 1 : 0; k < effectiveFieldCount; k++)
					{
						object value2 = r.GetValue(k + startBound);
						array2[k] = ((value2 is DBNull) ? null : value2);
					}
				}
				return new SqlMapper.DapperRow(table, array2);
			};
		}

		// Token: 0x060000AB RID: 171 RVA: 0x000059DC File Offset: 0x00003BDC
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This method is for internal use only", false)]
		public static char ReadChar(object value)
		{
			if (value == null || value is DBNull)
			{
				throw new ArgumentNullException("value");
			}
			string text = value as string;
			if (text == null || text.Length != 1)
			{
				throw new ArgumentException("A single-character was expected", "value");
			}
			return text[0];
		}

		// Token: 0x060000AC RID: 172 RVA: 0x00005A2C File Offset: 0x00003C2C
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This method is for internal use only", false)]
		public static char? ReadNullableChar(object value)
		{
			if (value == null || value is DBNull)
			{
				return null;
			}
			string text = value as string;
			if (text == null || text.Length != 1)
			{
				throw new ArgumentException("A single-character was expected", "value");
			}
			return new char?(text[0]);
		}

		// Token: 0x060000AD RID: 173 RVA: 0x00005A80 File Offset: 0x00003C80
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This method is for internal use only", true)]
		public static IDbDataParameter FindOrAddParameter(IDataParameterCollection parameters, IDbCommand command, string name)
		{
			IDbDataParameter dbDataParameter;
			if (parameters.Contains(name))
			{
				dbDataParameter = (IDbDataParameter)parameters[name];
			}
			else
			{
				dbDataParameter = command.CreateParameter();
				dbDataParameter.ParameterName = name;
				parameters.Add(dbDataParameter);
			}
			return dbDataParameter;
		}

		// Token: 0x060000AE RID: 174 RVA: 0x00005ABC File Offset: 0x00003CBC
		internal static int GetListPaddingExtraCount(int count)
		{
			switch (count)
			{
			case 0:
			case 1:
			case 2:
			case 3:
			case 4:
			case 5:
				return 0;
			default:
			{
				if (count < 0)
				{
					return 0;
				}
				int num;
				if (count <= 150)
				{
					num = 10;
				}
				else if (count <= 750)
				{
					num = 50;
				}
				else if (count <= 2000)
				{
					num = 100;
				}
				else if (count <= 2070)
				{
					num = 10;
				}
				else
				{
					if (count <= 2100)
					{
						return 0;
					}
					num = 200;
				}
				int num2 = count % num;
				if (num2 != 0)
				{
					return num - num2;
				}
				return 0;
			}
			}
		}

		// Token: 0x060000AF RID: 175 RVA: 0x00005B41 File Offset: 0x00003D41
		private static string GetInListRegex(string name, bool byPosition)
		{
			if (!byPosition)
			{
				return "([?@:]" + Regex.Escape(name) + ")(?!\\w)(\\s+(?i)unknown(?-i))?";
			}
			return "(\\?)" + Regex.Escape(name) + "\\?(?!\\w)(\\s+(?i)unknown(?-i))?";
		}

		// Token: 0x060000B0 RID: 176 RVA: 0x00005B74 File Offset: 0x00003D74
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("This method is for internal use only", false)]
		public static void PackListParameters(IDbCommand command, string namePrefix, object value)
		{
			if (FeatureSupport.Get(command.Connection).Arrays)
			{
				IDbDataParameter dbDataParameter = command.CreateParameter();
				dbDataParameter.Value = SqlMapper.SanitizeParameterValue(value);
				dbDataParameter.ParameterName = namePrefix;
				command.Parameters.Add(dbDataParameter);
				return;
			}
			bool byPosition = SqlMapper.ShouldPassByPosition(command.CommandText);
			IEnumerable enumerable = value as IEnumerable;
			int count = 0;
			bool flag = value is IEnumerable<string>;
			bool flag2 = value is IEnumerable<DbString>;
			DbType dbType = DbType.AnsiString;
			int inListStringSplitCount = SqlMapper.Settings.InListStringSplitCount;
			bool flag3 = inListStringSplitCount >= 0 && SqlMapper.TryStringSplit(ref enumerable, inListStringSplitCount, namePrefix, command, byPosition);
			if (enumerable != null && !flag3)
			{
				object obj = null;
				foreach (object obj2 in enumerable)
				{
					int num = count + 1;
					count = num;
					if (num == 1)
					{
						if (obj2 == null)
						{
							throw new NotSupportedException("The first item in a list-expansion cannot be null");
						}
						if (!flag2)
						{
							SqlMapper.ITypeHandler typeHandler;
							dbType = SqlMapper.LookupDbType(obj2.GetType(), "", true, out typeHandler);
						}
					}
					string text = namePrefix + count.ToString();
					if (flag2 && obj2 is DbString)
					{
						(obj2 as DbString).AddParameter(command, text);
					}
					else
					{
						IDbDataParameter dbDataParameter2 = command.CreateParameter();
						dbDataParameter2.ParameterName = text;
						if (flag)
						{
							dbDataParameter2.Size = 4000;
							if (obj2 != null && ((string)obj2).Length > 4000)
							{
								dbDataParameter2.Size = -1;
							}
						}
						object obj3 = dbDataParameter2.Value = SqlMapper.SanitizeParameterValue(obj2);
						if (obj3 != null && !(obj3 is DBNull))
						{
							obj = obj3;
						}
						if (dbDataParameter2.DbType != dbType)
						{
							dbDataParameter2.DbType = dbType;
						}
						command.Parameters.Add(dbDataParameter2);
					}
				}
				if (SqlMapper.Settings.PadListExpansions && !flag2 && obj != null)
				{
					int listPaddingExtraCount = SqlMapper.GetListPaddingExtraCount(count);
					for (int i = 0; i < listPaddingExtraCount; i++)
					{
						int num = count;
						count = num + 1;
						IDbDataParameter dbDataParameter3 = command.CreateParameter();
						dbDataParameter3.ParameterName = namePrefix + count.ToString();
						if (flag)
						{
							dbDataParameter3.Size = 4000;
						}
						dbDataParameter3.DbType = dbType;
						dbDataParameter3.Value = obj;
						command.Parameters.Add(dbDataParameter3);
					}
				}
			}
			if (!flag3)
			{
				string inListRegex = SqlMapper.GetInListRegex(namePrefix, byPosition);
				if (count == 0)
				{
					command.CommandText = Regex.Replace(command.CommandText, inListRegex, delegate(Match match)
					{
						string value2 = match.Groups[1].Value;
						if (match.Groups[2].Success)
						{
							return match.Value;
						}
						return "(SELECT " + value2 + " WHERE 1 = 0)";
					}, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);
					IDbDataParameter dbDataParameter4 = command.CreateParameter();
					dbDataParameter4.ParameterName = namePrefix;
					dbDataParameter4.Value = DBNull.Value;
					command.Parameters.Add(dbDataParameter4);
					return;
				}
				command.CommandText = Regex.Replace(command.CommandText, inListRegex, delegate(Match match)
				{
					string value2 = match.Groups[1].Value;
					if (match.Groups[2].Success)
					{
						string value3 = match.Groups[2].Value;
						StringBuilder stringBuilder = SqlMapper.GetStringBuilder().Append(value2).Append(1).Append(value3);
						for (int j = 2; j <= count; j++)
						{
							stringBuilder.Append(',').Append(value2).Append(j).Append(value3);
						}
						return stringBuilder.__ToStringRecycle();
					}
					StringBuilder stringBuilder2 = SqlMapper.GetStringBuilder().Append('(').Append(value2);
					if (!byPosition)
					{
						stringBuilder2.Append(1);
					}
					for (int k = 2; k <= count; k++)
					{
						stringBuilder2.Append(',').Append(value2);
						if (!byPosition)
						{
							stringBuilder2.Append(k);
						}
					}
					return stringBuilder2.Append(')').__ToStringRecycle();
				}, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);
			}
		}

		// Token: 0x060000B1 RID: 177 RVA: 0x00005EB4 File Offset: 0x000040B4
		private static bool TryStringSplit(ref IEnumerable list, int splitAt, string namePrefix, IDbCommand command, bool byPosition)
		{
			if (list == null || splitAt < 0)
			{
				return false;
			}
			if (list is IEnumerable<int>)
			{
				return SqlMapper.TryStringSplit<int>(ref list, splitAt, namePrefix, command, "int", byPosition, delegate(StringBuilder sb, int i)
				{
					sb.Append(i.ToString(CultureInfo.InvariantCulture));
				});
			}
			if (list is IEnumerable<long>)
			{
				return SqlMapper.TryStringSplit<long>(ref list, splitAt, namePrefix, command, "bigint", byPosition, delegate(StringBuilder sb, long i)
				{
					sb.Append(i.ToString(CultureInfo.InvariantCulture));
				});
			}
			if (list is IEnumerable<short>)
			{
				return SqlMapper.TryStringSplit<short>(ref list, splitAt, namePrefix, command, "smallint", byPosition, delegate(StringBuilder sb, short i)
				{
					sb.Append(i.ToString(CultureInfo.InvariantCulture));
				});
			}
			if (list is IEnumerable<byte>)
			{
				return SqlMapper.TryStringSplit<byte>(ref list, splitAt, namePrefix, command, "tinyint", byPosition, delegate(StringBuilder sb, byte i)
				{
					sb.Append(i.ToString(CultureInfo.InvariantCulture));
				});
			}
			return false;
		}

		// Token: 0x060000B2 RID: 178 RVA: 0x00005FB0 File Offset: 0x000041B0
		private static bool TryStringSplit<T>(ref IEnumerable list, int splitAt, string namePrefix, IDbCommand command, string colType, bool byPosition, Action<StringBuilder, T> append)
		{
			ICollection<T> collection = list as ICollection<T>;
			if (collection == null)
			{
				collection = ((IEnumerable<T>)list).ToList<T>();
				list = collection;
			}
			if (collection.Count < splitAt)
			{
				return false;
			}
			string varName = null;
			string inListRegex = SqlMapper.GetInListRegex(namePrefix, byPosition);
			string commandText = Regex.Replace(command.CommandText, inListRegex, delegate(Match match)
			{
				string value2 = match.Groups[1].Value;
				if (match.Groups[2].Success)
				{
					return match.Value;
				}
				varName = value2;
				return string.Concat(new string[]
				{
					"(select cast([value] as ",
					colType,
					") from string_split(",
					value2,
					",','))"
				});
			}, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);
			if (varName == null)
			{
				return false;
			}
			command.CommandText = commandText;
			IDbDataParameter dbDataParameter = command.CreateParameter();
			dbDataParameter.ParameterName = namePrefix;
			dbDataParameter.DbType = DbType.AnsiString;
			dbDataParameter.Size = -1;
			string value;
			using (IEnumerator<T> enumerator = collection.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					StringBuilder stringBuilder = SqlMapper.GetStringBuilder();
					append(stringBuilder, enumerator.Current);
					while (enumerator.MoveNext())
					{
						T arg = enumerator.Current;
						append(stringBuilder.Append(','), arg);
					}
					value = stringBuilder.ToString();
				}
				else
				{
					value = "";
				}
			}
			dbDataParameter.Value = value;
			command.Parameters.Add(dbDataParameter);
			return true;
		}

		// Token: 0x060000B3 RID: 179 RVA: 0x000060E4 File Offset: 0x000042E4
		[Obsolete("This method is for internal use only", false)]
		public static object SanitizeParameterValue(object value)
		{
			if (value == null)
			{
				return DBNull.Value;
			}
			if (value is Enum)
			{
				TypeCode typeCode;
				if (value is IConvertible)
				{
					typeCode = ((IConvertible)value).GetTypeCode();
				}
				else
				{
					typeCode = TypeExtensions.GetTypeCode(Enum.GetUnderlyingType(value.GetType()));
				}
				switch (typeCode)
				{
				case TypeCode.SByte:
					return (sbyte)value;
				case TypeCode.Byte:
					return (byte)value;
				case TypeCode.Int16:
					return (short)value;
				case TypeCode.UInt16:
					return (ushort)value;
				case TypeCode.Int32:
					return (int)value;
				case TypeCode.UInt32:
					return (uint)value;
				case TypeCode.Int64:
					return (long)value;
				case TypeCode.UInt64:
					return (ulong)value;
				}
			}
			return value;
		}

		// Token: 0x060000B4 RID: 180 RVA: 0x000061B8 File Offset: 0x000043B8
		private static IEnumerable<PropertyInfo> FilterParameters(IEnumerable<PropertyInfo> parameters, string sql)
		{
			return from p in parameters
			where Regex.IsMatch(sql, "[?@:]" + p.Name + "([^a-z0-9_]+|$)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant)
			select p;
		}

		// Token: 0x060000B5 RID: 181 RVA: 0x000061E4 File Offset: 0x000043E4
		public static void ReplaceLiterals(this SqlMapper.IParameterLookup parameters, IDbCommand command)
		{
			IList<SqlMapper.LiteralToken> list = SqlMapper.GetLiteralTokens(command.CommandText);
			if (list.Count != 0)
			{
				SqlMapper.ReplaceLiterals(parameters, command, list);
			}
		}

		// Token: 0x060000B6 RID: 182 RVA: 0x00006210 File Offset: 0x00004410
		[Obsolete("This method is for internal use only")]
		public static string Format(object value)
		{
			if (value == null)
			{
				return "null";
			}
			switch (TypeExtensions.GetTypeCode(value.GetType()))
			{
			case TypeCode.DBNull:
				return "null";
			case TypeCode.Boolean:
				if (!(bool)value)
				{
					return "0";
				}
				return "1";
			case TypeCode.SByte:
				return ((sbyte)value).ToString(CultureInfo.InvariantCulture);
			case TypeCode.Byte:
				return ((byte)value).ToString(CultureInfo.InvariantCulture);
			case TypeCode.Int16:
				return ((short)value).ToString(CultureInfo.InvariantCulture);
			case TypeCode.UInt16:
				return ((ushort)value).ToString(CultureInfo.InvariantCulture);
			case TypeCode.Int32:
				return ((int)value).ToString(CultureInfo.InvariantCulture);
			case TypeCode.UInt32:
				return ((uint)value).ToString(CultureInfo.InvariantCulture);
			case TypeCode.Int64:
				return ((long)value).ToString(CultureInfo.InvariantCulture);
			case TypeCode.UInt64:
				return ((ulong)value).ToString(CultureInfo.InvariantCulture);
			case TypeCode.Single:
				return ((float)value).ToString(CultureInfo.InvariantCulture);
			case TypeCode.Double:
				return ((double)value).ToString(CultureInfo.InvariantCulture);
			case TypeCode.Decimal:
				return ((decimal)value).ToString(CultureInfo.InvariantCulture);
			}
			IEnumerable multiExec = SqlMapper.GetMultiExec(value);
			if (multiExec == null)
			{
				throw new NotSupportedException(value.GetType().Name);
			}
			StringBuilder stringBuilder = null;
			bool flag = true;
			foreach (object value2 in multiExec)
			{
				if (flag)
				{
					stringBuilder = SqlMapper.GetStringBuilder().Append('(');
					flag = false;
				}
				else
				{
					stringBuilder.Append(',');
				}
				stringBuilder.Append(SqlMapper.Format(value2));
			}
			if (flag)
			{
				return "(select null where 1=0)";
			}
			return stringBuilder.Append(')').__ToStringRecycle();
		}

		// Token: 0x060000B7 RID: 183 RVA: 0x00006428 File Offset: 0x00004628
		internal static void ReplaceLiterals(SqlMapper.IParameterLookup parameters, IDbCommand command, IList<SqlMapper.LiteralToken> tokens)
		{
			string text = command.CommandText;
			foreach (SqlMapper.LiteralToken literalToken in tokens)
			{
				string newValue = SqlMapper.Format(parameters[literalToken.Member]);
				text = text.Replace(literalToken.Token, newValue);
			}
			command.CommandText = text;
		}

		// Token: 0x060000B8 RID: 184 RVA: 0x0000649C File Offset: 0x0000469C
		internal static IList<SqlMapper.LiteralToken> GetLiteralTokens(string sql)
		{
			if (string.IsNullOrEmpty(sql))
			{
				return SqlMapper.LiteralToken.None;
			}
			if (!SqlMapper.literalTokens.IsMatch(sql))
			{
				return SqlMapper.LiteralToken.None;
			}
			MatchCollection matchCollection = SqlMapper.literalTokens.Matches(sql);
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			List<SqlMapper.LiteralToken> list = new List<SqlMapper.LiteralToken>(matchCollection.Count);
			foreach (object obj in matchCollection)
			{
				Match match = (Match)obj;
				string value = match.Value;
				if (hashSet.Add(match.Value))
				{
					list.Add(new SqlMapper.LiteralToken(value, match.Groups[1].Value));
				}
			}
			if (list.Count != 0)
			{
				return list;
			}
			return SqlMapper.LiteralToken.None;
		}

		// Token: 0x060000B9 RID: 185 RVA: 0x00006578 File Offset: 0x00004778
		public static Action<IDbCommand, object> CreateParamInfoGenerator(SqlMapper.Identity identity, bool checkForDuplicates, bool removeUnused)
		{
			return SqlMapper.CreateParamInfoGenerator(identity, checkForDuplicates, removeUnused, SqlMapper.GetLiteralTokens(identity.sql));
		}

		// Token: 0x060000BA RID: 186 RVA: 0x00006590 File Offset: 0x00004790
		internal static Action<IDbCommand, object> CreateParamInfoGenerator(SqlMapper.Identity identity, bool checkForDuplicates, bool removeUnused, IList<SqlMapper.LiteralToken> literals)
		{
			Type parametersType = identity.parametersType;
			bool flag = false;
			if (removeUnused && identity.commandType.GetValueOrDefault(CommandType.Text) == CommandType.Text)
			{
				flag = !SqlMapper.smellsLikeOleDb.IsMatch(identity.sql);
			}
			DynamicMethod dynamicMethod = new DynamicMethod("ParamInfo" + Guid.NewGuid().ToString(), null, new Type[]
			{
				typeof(IDbCommand),
				typeof(object)
			}, parametersType, true);
			ILGenerator ilgenerator = dynamicMethod.GetILGenerator();
			bool flag2 = parametersType.IsValueType();
			bool flag3 = false;
			ilgenerator.Emit(OpCodes.Ldarg_1);
			if (flag2)
			{
				ilgenerator.DeclareLocal(parametersType.MakePointerType());
				ilgenerator.Emit(OpCodes.Unbox, parametersType);
			}
			else
			{
				ilgenerator.DeclareLocal(parametersType);
				ilgenerator.Emit(OpCodes.Castclass, parametersType);
			}
			ilgenerator.Emit(OpCodes.Stloc_0);
			ilgenerator.Emit(OpCodes.Ldarg_0);
			ilgenerator.EmitCall(OpCodes.Callvirt, typeof(IDbCommand).GetProperty("Parameters").GetGetMethod(), null);
			PropertyInfo[] array = (from p in parametersType.GetProperties()
			where p.GetIndexParameters().Length == 0
			select p).ToArray<PropertyInfo>();
			ConstructorInfo[] constructors = parametersType.GetConstructors();
			IEnumerable<PropertyInfo> enumerable = null;
			ParameterInfo[] parameters;
			if (constructors.Length == 1 && array.Length == (parameters = constructors[0].GetParameters()).Length)
			{
				bool flag4 = true;
				for (int i = 0; i < array.Length; i++)
				{
					if (!string.Equals(array[i].Name, parameters[i].Name, StringComparison.OrdinalIgnoreCase))
					{
						flag4 = false;
						break;
					}
				}
				if (flag4)
				{
					enumerable = array;
				}
				else
				{
					Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
					foreach (ParameterInfo parameterInfo in parameters)
					{
						dictionary[parameterInfo.Name] = parameterInfo.Position;
					}
					if (dictionary.Count == array.Length)
					{
						int[] array3 = new int[array.Length];
						flag4 = true;
						for (int k = 0; k < array.Length; k++)
						{
							int num;
							if (!dictionary.TryGetValue(array[k].Name, out num))
							{
								flag4 = false;
								break;
							}
							array3[k] = num;
						}
						if (flag4)
						{
							Array.Sort<int, PropertyInfo>(array3, array);
							enumerable = array;
						}
					}
				}
			}
			if (enumerable == null)
			{
				enumerable = from x in array
				orderby x.Name
				select x;
			}
			if (flag)
			{
				enumerable = SqlMapper.FilterParameters(enumerable, identity.sql);
			}
			OpCode opcode = flag2 ? OpCodes.Call : OpCodes.Callvirt;
			foreach (PropertyInfo propertyInfo in enumerable)
			{
				if (typeof(SqlMapper.ICustomQueryParameter).IsAssignableFrom(propertyInfo.PropertyType))
				{
					ilgenerator.Emit(OpCodes.Ldloc_0);
					ilgenerator.Emit(opcode, propertyInfo.GetGetMethod());
					ilgenerator.Emit(OpCodes.Ldarg_0);
					ilgenerator.Emit(OpCodes.Ldstr, propertyInfo.Name);
					ilgenerator.EmitCall(OpCodes.Callvirt, propertyInfo.PropertyType.GetMethod("AddParameter"), null);
				}
				else
				{
					SqlMapper.ITypeHandler typeHandler;
					DbType dbType = SqlMapper.LookupDbType(propertyInfo.PropertyType, propertyInfo.Name, true, out typeHandler);
					if (dbType == (DbType)(-1))
					{
						ilgenerator.Emit(OpCodes.Ldarg_0);
						ilgenerator.Emit(OpCodes.Ldstr, propertyInfo.Name);
						ilgenerator.Emit(OpCodes.Ldloc_0);
						ilgenerator.Emit(opcode, propertyInfo.GetGetMethod());
						if (propertyInfo.PropertyType.IsValueType())
						{
							ilgenerator.Emit(OpCodes.Box, propertyInfo.PropertyType);
						}
						ilgenerator.EmitCall(OpCodes.Call, typeof(SqlMapper).GetMethod("PackListParameters"), null);
					}
					else
					{
						ilgenerator.Emit(OpCodes.Dup);
						ilgenerator.Emit(OpCodes.Ldarg_0);
						if (checkForDuplicates)
						{
							ilgenerator.Emit(OpCodes.Ldstr, propertyInfo.Name);
							ilgenerator.EmitCall(OpCodes.Call, typeof(SqlMapper).GetMethod("FindOrAddParameter"), null);
						}
						else
						{
							ilgenerator.EmitCall(OpCodes.Callvirt, typeof(IDbCommand).GetMethod("CreateParameter"), null);
							ilgenerator.Emit(OpCodes.Dup);
							ilgenerator.Emit(OpCodes.Ldstr, propertyInfo.Name);
							ilgenerator.EmitCall(OpCodes.Callvirt, typeof(IDataParameter).GetProperty("ParameterName").GetSetMethod(), null);
						}
						if (dbType != DbType.Time && typeHandler == null)
						{
							ilgenerator.Emit(OpCodes.Dup);
							if (dbType == DbType.Object && propertyInfo.PropertyType == typeof(object))
							{
								ilgenerator.Emit(OpCodes.Ldloc_0);
								ilgenerator.Emit(opcode, propertyInfo.GetGetMethod());
								ilgenerator.Emit(OpCodes.Call, typeof(SqlMapper).GetMethod("GetDbType", BindingFlags.Static | BindingFlags.Public));
							}
							else
							{
								SqlMapper.EmitInt32(ilgenerator, (int)dbType);
							}
							ilgenerator.EmitCall(OpCodes.Callvirt, typeof(IDataParameter).GetProperty("DbType").GetSetMethod(), null);
						}
						ilgenerator.Emit(OpCodes.Dup);
						SqlMapper.EmitInt32(ilgenerator, 1);
						ilgenerator.EmitCall(OpCodes.Callvirt, typeof(IDataParameter).GetProperty("Direction").GetSetMethod(), null);
						ilgenerator.Emit(OpCodes.Dup);
						ilgenerator.Emit(OpCodes.Ldloc_0);
						ilgenerator.Emit(opcode, propertyInfo.GetGetMethod());
						bool flag6;
						if (propertyInfo.PropertyType.IsValueType())
						{
							Type type = propertyInfo.PropertyType;
							Type underlyingType = Nullable.GetUnderlyingType(type);
							bool flag5 = false;
							if ((underlyingType ?? type).IsEnum())
							{
								if (underlyingType != null)
								{
									flag6 = (flag5 = true);
								}
								else
								{
									flag6 = false;
									switch (TypeExtensions.GetTypeCode(Enum.GetUnderlyingType(type)))
									{
									case TypeCode.SByte:
										type = typeof(sbyte);
										break;
									case TypeCode.Byte:
										type = typeof(byte);
										break;
									case TypeCode.Int16:
										type = typeof(short);
										break;
									case TypeCode.UInt16:
										type = typeof(ushort);
										break;
									case TypeCode.Int32:
										type = typeof(int);
										break;
									case TypeCode.UInt32:
										type = typeof(uint);
										break;
									case TypeCode.Int64:
										type = typeof(long);
										break;
									case TypeCode.UInt64:
										type = typeof(ulong);
										break;
									}
								}
							}
							else
							{
								flag6 = (underlyingType != null);
							}
							ilgenerator.Emit(OpCodes.Box, type);
							if (flag5)
							{
								flag6 = false;
								ilgenerator.EmitCall(OpCodes.Call, typeof(SqlMapper).GetMethod("SanitizeParameterValue"), null);
							}
						}
						else
						{
							flag6 = true;
						}
						if (flag6)
						{
							if ((dbType == DbType.String || dbType == DbType.AnsiString) && !flag3)
							{
								ilgenerator.DeclareLocal(typeof(int));
								flag3 = true;
							}
							ilgenerator.Emit(OpCodes.Dup);
							Label label = ilgenerator.DefineLabel();
							Label? label2 = (dbType == DbType.String || dbType == DbType.AnsiString) ? new Label?(ilgenerator.DefineLabel()) : null;
							ilgenerator.Emit(OpCodes.Brtrue_S, label);
							ilgenerator.Emit(OpCodes.Pop);
							ilgenerator.Emit(OpCodes.Ldsfld, typeof(DBNull).GetField("Value"));
							if (dbType == DbType.String || dbType == DbType.AnsiString)
							{
								SqlMapper.EmitInt32(ilgenerator, 0);
								ilgenerator.Emit(OpCodes.Stloc_1);
							}
							if (label2 != null)
							{
								ilgenerator.Emit(OpCodes.Br_S, label2.Value);
							}
							ilgenerator.MarkLabel(label);
							if (propertyInfo.PropertyType == typeof(string))
							{
								ilgenerator.Emit(OpCodes.Dup);
								ilgenerator.EmitCall(OpCodes.Callvirt, typeof(string).GetProperty("Length").GetGetMethod(), null);
								SqlMapper.EmitInt32(ilgenerator, 4000);
								ilgenerator.Emit(OpCodes.Cgt);
								Label label3 = ilgenerator.DefineLabel();
								Label label4 = ilgenerator.DefineLabel();
								ilgenerator.Emit(OpCodes.Brtrue_S, label3);
								SqlMapper.EmitInt32(ilgenerator, 4000);
								ilgenerator.Emit(OpCodes.Br_S, label4);
								ilgenerator.MarkLabel(label3);
								SqlMapper.EmitInt32(ilgenerator, -1);
								ilgenerator.MarkLabel(label4);
								ilgenerator.Emit(OpCodes.Stloc_1);
							}
							if (propertyInfo.PropertyType.FullName == "System.Data.Linq.Binary")
							{
								ilgenerator.EmitCall(OpCodes.Callvirt, propertyInfo.PropertyType.GetMethod("ToArray", BindingFlags.Instance | BindingFlags.Public), null);
							}
							if (label2 != null)
							{
								ilgenerator.MarkLabel(label2.Value);
							}
						}
						if (typeHandler != null)
						{
							ilgenerator.Emit(OpCodes.Call, typeof(SqlMapper.TypeHandlerCache<>).MakeGenericType(new Type[]
							{
								propertyInfo.PropertyType
							}).GetMethod("SetValue"));
						}
						else
						{
							ilgenerator.EmitCall(OpCodes.Callvirt, typeof(IDataParameter).GetProperty("Value").GetSetMethod(), null);
						}
						if (propertyInfo.PropertyType == typeof(string))
						{
							Label label5 = ilgenerator.DefineLabel();
							ilgenerator.Emit(OpCodes.Ldloc_1);
							ilgenerator.Emit(OpCodes.Brfalse_S, label5);
							ilgenerator.Emit(OpCodes.Dup);
							ilgenerator.Emit(OpCodes.Ldloc_1);
							ilgenerator.EmitCall(OpCodes.Callvirt, typeof(IDbDataParameter).GetProperty("Size").GetSetMethod(), null);
							ilgenerator.MarkLabel(label5);
						}
						if (checkForDuplicates)
						{
							ilgenerator.Emit(OpCodes.Pop);
						}
						else
						{
							ilgenerator.EmitCall(OpCodes.Callvirt, typeof(IList).GetMethod("Add"), null);
							ilgenerator.Emit(OpCodes.Pop);
						}
					}
				}
			}
			ilgenerator.Emit(OpCodes.Pop);
			if (literals.Count != 0 && array != null)
			{
				ilgenerator.Emit(OpCodes.Ldarg_0);
				ilgenerator.Emit(OpCodes.Ldarg_0);
				PropertyInfo property = typeof(IDbCommand).GetProperty("CommandText");
				ilgenerator.EmitCall(OpCodes.Callvirt, property.GetGetMethod(), null);
				Dictionary<Type, LocalBuilder> dictionary2 = null;
				LocalBuilder localBuilder = null;
				foreach (SqlMapper.LiteralToken literalToken in literals)
				{
					PropertyInfo propertyInfo2 = null;
					PropertyInfo propertyInfo3 = null;
					string member = literalToken.Member;
					for (int l = 0; l < array.Length; l++)
					{
						string name = array[l].Name;
						if (string.Equals(name, member, StringComparison.OrdinalIgnoreCase))
						{
							propertyInfo3 = array[l];
							if (string.Equals(name, member, StringComparison.Ordinal))
							{
								propertyInfo2 = propertyInfo3;
								break;
							}
						}
					}
					PropertyInfo propertyInfo4 = propertyInfo2 ?? propertyInfo3;
					if (propertyInfo4 != null)
					{
						ilgenerator.Emit(OpCodes.Ldstr, literalToken.Token);
						ilgenerator.Emit(OpCodes.Ldloc_0);
						ilgenerator.EmitCall(opcode, propertyInfo4.GetGetMethod(), null);
						Type propertyType = propertyInfo4.PropertyType;
						TypeCode typeCode = TypeExtensions.GetTypeCode(propertyType);
						switch (typeCode)
						{
						case TypeCode.Boolean:
						{
							Label label6 = ilgenerator.DefineLabel();
							Label label7 = ilgenerator.DefineLabel();
							ilgenerator.Emit(OpCodes.Brtrue_S, label6);
							ilgenerator.Emit(OpCodes.Ldstr, "0");
							ilgenerator.Emit(OpCodes.Br_S, label7);
							ilgenerator.MarkLabel(label6);
							ilgenerator.Emit(OpCodes.Ldstr, "1");
							ilgenerator.MarkLabel(label7);
							break;
						}
						case TypeCode.Char:
							goto IL_BFC;
						case TypeCode.SByte:
						case TypeCode.Byte:
						case TypeCode.Int16:
						case TypeCode.UInt16:
						case TypeCode.Int32:
						case TypeCode.UInt32:
						case TypeCode.Int64:
						case TypeCode.UInt64:
						case TypeCode.Single:
						case TypeCode.Double:
						case TypeCode.Decimal:
						{
							MethodInfo toString = SqlMapper.GetToString(typeCode);
							if (localBuilder == null || localBuilder.LocalType != propertyType)
							{
								if (dictionary2 == null)
								{
									dictionary2 = new Dictionary<Type, LocalBuilder>();
									localBuilder = null;
								}
								else if (!dictionary2.TryGetValue(propertyType, out localBuilder))
								{
									localBuilder = null;
								}
								if (localBuilder == null)
								{
									localBuilder = ilgenerator.DeclareLocal(propertyType);
									dictionary2.Add(propertyType, localBuilder);
								}
							}
							ilgenerator.Emit(OpCodes.Stloc, localBuilder);
							ilgenerator.Emit(OpCodes.Ldloca, localBuilder);
							ilgenerator.EmitCall(OpCodes.Call, SqlMapper.InvariantCulture, null);
							ilgenerator.EmitCall(OpCodes.Call, toString, null);
							break;
						}
						default:
							goto IL_BFC;
						}
						IL_C23:
						ilgenerator.EmitCall(OpCodes.Callvirt, SqlMapper.StringReplace, null);
						continue;
						IL_BFC:
						if (propertyType.IsValueType())
						{
							ilgenerator.Emit(OpCodes.Box, propertyType);
						}
						ilgenerator.EmitCall(OpCodes.Call, SqlMapper.format, null);
						goto IL_C23;
					}
				}
				ilgenerator.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
			}
			ilgenerator.Emit(OpCodes.Ret);
			return (Action<IDbCommand, object>)dynamicMethod.CreateDelegate(typeof(Action<IDbCommand, object>));
		}

		// Token: 0x060000BB RID: 187 RVA: 0x00007254 File Offset: 0x00005454
		private static MethodInfo GetToString(TypeCode typeCode)
		{
			MethodInfo result;
			if (!SqlMapper.toStrings.TryGetValue(typeCode, out result))
			{
				return null;
			}
			return result;
		}

		// Token: 0x060000BC RID: 188 RVA: 0x00007274 File Offset: 0x00005474
		private static int ExecuteCommand(IDbConnection cnn, ref CommandDefinition command, Action<IDbCommand, object> paramReader)
		{
			IDbCommand dbCommand = null;
			bool flag = cnn.State == ConnectionState.Closed;
			int result;
			try
			{
				dbCommand = command.SetupCommand(cnn, paramReader);
				if (flag)
				{
					cnn.Open();
				}
				int num = dbCommand.ExecuteNonQuery();
				command.OnCompleted();
				result = num;
			}
			finally
			{
				if (flag)
				{
					cnn.Close();
				}
				if (dbCommand != null)
				{
					dbCommand.Dispose();
				}
			}
			return result;
		}

		// Token: 0x060000BD RID: 189 RVA: 0x000072D4 File Offset: 0x000054D4
		private static T ExecuteScalarImpl<T>(IDbConnection cnn, ref CommandDefinition command)
		{
			Action<IDbCommand, object> paramReader = null;
			object parameters = command.Parameters;
			if (parameters != null)
			{
				paramReader = SqlMapper.GetCacheInfo(new SqlMapper.Identity(command.CommandText, command.CommandType, cnn, null, parameters.GetType(), null), command.Parameters, command.AddToCache).ParamReader;
			}
			IDbCommand dbCommand = null;
			bool flag = cnn.State == ConnectionState.Closed;
			object value;
			try
			{
				dbCommand = command.SetupCommand(cnn, paramReader);
				if (flag)
				{
					cnn.Open();
				}
				value = dbCommand.ExecuteScalar();
				command.OnCompleted();
			}
			finally
			{
				if (flag)
				{
					cnn.Close();
				}
				if (dbCommand != null)
				{
					dbCommand.Dispose();
				}
			}
			return SqlMapper.Parse<T>(value);
		}

		// Token: 0x060000BE RID: 190 RVA: 0x00007378 File Offset: 0x00005578
		private static IDataReader ExecuteReaderImpl(IDbConnection cnn, ref CommandDefinition command, CommandBehavior commandBehavior, out IDbCommand cmd)
		{
			Action<IDbCommand, object> parameterReader = SqlMapper.GetParameterReader(cnn, ref command);
			cmd = null;
			bool flag = cnn.State == ConnectionState.Closed;
			bool flag2 = true;
			IDataReader result;
			try
			{
				cmd = command.SetupCommand(cnn, parameterReader);
				if (flag)
				{
					cnn.Open();
				}
				IDataReader dataReader = SqlMapper.ExecuteReaderWithFlagsFallback(cmd, flag, commandBehavior);
				flag = false;
				flag2 = false;
				result = dataReader;
			}
			finally
			{
				if (flag)
				{
					cnn.Close();
				}
				if (cmd != null && flag2)
				{
					cmd.Dispose();
				}
			}
			return result;
		}

		// Token: 0x060000BF RID: 191 RVA: 0x000073EC File Offset: 0x000055EC
		private static Action<IDbCommand, object> GetParameterReader(IDbConnection cnn, ref CommandDefinition command)
		{
			object parameters = command.Parameters;
			bool multiExec = SqlMapper.GetMultiExec(parameters) != null;
			SqlMapper.CacheInfo cacheInfo = null;
			if (multiExec)
			{
				throw new NotSupportedException("MultiExec is not supported by ExecuteReader");
			}
			if (parameters != null)
			{
				cacheInfo = SqlMapper.GetCacheInfo(new SqlMapper.Identity(command.CommandText, command.CommandType, cnn, null, parameters.GetType(), null), parameters, command.AddToCache);
			}
			if (cacheInfo == null)
			{
				return null;
			}
			return cacheInfo.ParamReader;
		}

		// Token: 0x060000C0 RID: 192 RVA: 0x0000744C File Offset: 0x0000564C
		private static Func<IDataReader, object> GetStructDeserializer(Type type, Type effectiveType, int index)
		{
			if (type == typeof(char))
			{
				return (IDataReader r) => SqlMapper.ReadChar(r.GetValue(index));
			}
			if (type == typeof(char?))
			{
				return (IDataReader r) => SqlMapper.ReadNullableChar(r.GetValue(index));
			}
			if (type.FullName == "System.Data.Linq.Binary")
			{
				return (IDataReader r) => Activator.CreateInstance(type, new object[]
				{
					r.GetValue(index)
				});
			}
			if (effectiveType.IsEnum())
			{
				return delegate(IDataReader r)
				{
					object obj = r.GetValue(index);
					if (obj is float || obj is double || obj is decimal)
					{
						obj = Convert.ChangeType(obj, Enum.GetUnderlyingType(effectiveType), CultureInfo.InvariantCulture);
					}
					if (!(obj is DBNull))
					{
						return Enum.ToObject(effectiveType, obj);
					}
					return null;
				};
			}
			SqlMapper.ITypeHandler handler;
			if (SqlMapper.typeHandlers.TryGetValue(type, out handler))
			{
				return delegate(IDataReader r)
				{
					object value = r.GetValue(index);
					if (!(value is DBNull))
					{
						return handler.Parse(type, value);
					}
					return null;
				};
			}
			return delegate(IDataReader r)
			{
				object value = r.GetValue(index);
				if (!(value is DBNull))
				{
					return value;
				}
				return null;
			};
		}

		// Token: 0x060000C1 RID: 193 RVA: 0x0000752C File Offset: 0x0000572C
		private static T Parse<T>(object value)
		{
			if (value == null || value is DBNull)
			{
				return default(T);
			}
			if (value is T)
			{
				return (T)((object)value);
			}
			Type type = typeof(T);
			type = (Nullable.GetUnderlyingType(type) ?? type);
			if (type.IsEnum())
			{
				if (value is float || value is double || value is decimal)
				{
					value = Convert.ChangeType(value, Enum.GetUnderlyingType(type), CultureInfo.InvariantCulture);
				}
				return (T)((object)Enum.ToObject(type, value));
			}
			SqlMapper.ITypeHandler typeHandler;
			if (SqlMapper.typeHandlers.TryGetValue(type, out typeHandler))
			{
				return (T)((object)typeHandler.Parse(type, value));
			}
			return (T)((object)Convert.ChangeType(value, type, CultureInfo.InvariantCulture));
		}

		// Token: 0x060000C2 RID: 194 RVA: 0x000075E4 File Offset: 0x000057E4
		public static SqlMapper.ITypeMap GetTypeMap(Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			SqlMapper.ITypeMap typeMap = (SqlMapper.ITypeMap)SqlMapper._typeMaps[type];
			if (typeMap == null)
			{
				Hashtable typeMaps = SqlMapper._typeMaps;
				lock (typeMaps)
				{
					typeMap = (SqlMapper.ITypeMap)SqlMapper._typeMaps[type];
					if (typeMap == null)
					{
						typeMap = SqlMapper.TypeMapProvider(type);
						SqlMapper._typeMaps[type] = typeMap;
					}
				}
			}
			return typeMap;
		}

		// Token: 0x060000C3 RID: 195 RVA: 0x00007674 File Offset: 0x00005874
		public static void SetTypeMap(Type type, SqlMapper.ITypeMap map)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			Hashtable typeMaps;
			if (map == null || map is DefaultTypeMap)
			{
				typeMaps = SqlMapper._typeMaps;
				lock (typeMaps)
				{
					SqlMapper._typeMaps.Remove(type);
					goto IL_6E;
				}
			}
			typeMaps = SqlMapper._typeMaps;
			lock (typeMaps)
			{
				SqlMapper._typeMaps[type] = map;
			}
			IL_6E:
			SqlMapper.PurgeQueryCacheByType(type);
		}

		// Token: 0x060000C4 RID: 196 RVA: 0x00007714 File Offset: 0x00005914
		public static Func<IDataReader, object> GetTypeDeserializer(Type type, IDataReader reader, int startBound = 0, int length = -1, bool returnNullIfFirstMissing = false)
		{
			return SqlMapper.TypeDeserializerCache.GetReader(type, reader, startBound, length, returnNullIfFirstMissing);
		}

		// Token: 0x060000C5 RID: 197 RVA: 0x00007724 File Offset: 0x00005924
		private static LocalBuilder GetTempLocal(ILGenerator il, ref Dictionary<Type, LocalBuilder> locals, Type type, bool initAndLoad)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			if (locals == null)
			{
				locals = new Dictionary<Type, LocalBuilder>();
			}
			LocalBuilder localBuilder;
			if (!locals.TryGetValue(type, out localBuilder))
			{
				localBuilder = il.DeclareLocal(type);
				locals.Add(type, localBuilder);
			}
			if (initAndLoad)
			{
				il.Emit(OpCodes.Ldloca, (short)localBuilder.LocalIndex);
				il.Emit(OpCodes.Initobj, type);
				il.Emit(OpCodes.Ldloca, (short)localBuilder.LocalIndex);
				il.Emit(OpCodes.Ldobj, type);
			}
			return localBuilder;
		}

		// Token: 0x060000C6 RID: 198 RVA: 0x000077B0 File Offset: 0x000059B0
		private static Func<IDataReader, object> GetTypeDeserializerImpl(Type type, IDataReader reader, int startBound = 0, int length = -1, bool returnNullIfFirstMissing = false)
		{
			Type type2 = type.IsValueType() ? typeof(object) : type;
			DynamicMethod dynamicMethod = new DynamicMethod("Deserialize" + Guid.NewGuid().ToString(), type2, new Type[]
			{
				typeof(IDataReader)
			}, type, true);
			ILGenerator ilgenerator = dynamicMethod.GetILGenerator();
			ilgenerator.DeclareLocal(typeof(int));
			ilgenerator.DeclareLocal(type);
			ilgenerator.Emit(OpCodes.Ldc_I4_0);
			ilgenerator.Emit(OpCodes.Stloc_0);
			if (length == -1)
			{
				length = reader.FieldCount - startBound;
			}
			if (reader.FieldCount <= startBound)
			{
				throw SqlMapper.MultiMapException(reader);
			}
			string[] names = (from i in Enumerable.Range(startBound, length)
			select reader.GetName(i)).ToArray<string>();
			SqlMapper.ITypeMap typeMap = SqlMapper.GetTypeMap(type);
			int num = startBound;
			ConstructorInfo specializedConstructor = null;
			bool flag = false;
			Dictionary<Type, LocalBuilder> dictionary = null;
			if (type.IsValueType())
			{
				ilgenerator.Emit(OpCodes.Ldloca_S, 1);
				ilgenerator.Emit(OpCodes.Initobj, type);
			}
			else
			{
				Type[] array = new Type[length];
				for (int k = startBound; k < startBound + length; k++)
				{
					array[k - startBound] = reader.GetFieldType(k);
				}
				ConstructorInfo constructorInfo = typeMap.FindExplicitConstructor();
				if (constructorInfo != null)
				{
					foreach (ParameterInfo parameterInfo in constructorInfo.GetParameters())
					{
						if (!parameterInfo.ParameterType.IsValueType())
						{
							ilgenerator.Emit(OpCodes.Ldnull);
						}
						else
						{
							SqlMapper.GetTempLocal(ilgenerator, ref dictionary, parameterInfo.ParameterType, true);
						}
					}
					ilgenerator.Emit(OpCodes.Newobj, constructorInfo);
					ilgenerator.Emit(OpCodes.Stloc_1);
					flag = typeof(ISupportInitialize).IsAssignableFrom(type);
					if (flag)
					{
						ilgenerator.Emit(OpCodes.Ldloc_1);
						ilgenerator.EmitCall(OpCodes.Callvirt, typeof(ISupportInitialize).GetMethod("BeginInit"), null);
					}
				}
				else
				{
					ConstructorInfo constructorInfo2 = typeMap.FindConstructor(names, array);
					if (constructorInfo2 == null)
					{
						string arg = "(" + string.Join(", ", array.Select((Type t, int i) => t.FullName + " " + names[i]).ToArray<string>()) + ")";
						throw new InvalidOperationException(string.Format("A parameterless default constructor or one matching signature {0} is required for {1} materialization", arg, type.FullName));
					}
					if (constructorInfo2.GetParameters().Length == 0)
					{
						ilgenerator.Emit(OpCodes.Newobj, constructorInfo2);
						ilgenerator.Emit(OpCodes.Stloc_1);
						flag = typeof(ISupportInitialize).IsAssignableFrom(type);
						if (flag)
						{
							ilgenerator.Emit(OpCodes.Ldloc_1);
							ilgenerator.EmitCall(OpCodes.Callvirt, typeof(ISupportInitialize).GetMethod("BeginInit"), null);
						}
					}
					else
					{
						specializedConstructor = constructorInfo2;
					}
				}
			}
			ilgenerator.BeginExceptionBlock();
			if (type.IsValueType())
			{
				ilgenerator.Emit(OpCodes.Ldloca_S, 1);
			}
			else if (specializedConstructor == null)
			{
				ilgenerator.Emit(OpCodes.Ldloc_1);
			}
			List<SqlMapper.IMemberMap> list = ((specializedConstructor != null) ? (from n in names
			select typeMap.GetConstructorParameter(specializedConstructor, n)) : names.Select((string n) => typeMap.GetMember(n))).ToList<SqlMapper.IMemberMap>();
			bool flag2 = true;
			Label label = ilgenerator.DefineLabel();
			int num2 = -1;
			int localIndex = ilgenerator.DeclareLocal(typeof(object)).LocalIndex;
			bool applyNullValues = SqlMapper.Settings.ApplyNullValues;
			foreach (SqlMapper.IMemberMap memberMap in list)
			{
				if (memberMap != null)
				{
					if (specializedConstructor == null)
					{
						ilgenerator.Emit(OpCodes.Dup);
					}
					Label label2 = ilgenerator.DefineLabel();
					Label label3 = ilgenerator.DefineLabel();
					ilgenerator.Emit(OpCodes.Ldarg_0);
					SqlMapper.EmitInt32(ilgenerator, num);
					ilgenerator.Emit(OpCodes.Dup);
					ilgenerator.Emit(OpCodes.Stloc_0);
					ilgenerator.Emit(OpCodes.Callvirt, SqlMapper.getItem);
					ilgenerator.Emit(OpCodes.Dup);
					SqlMapper.StoreLocal(ilgenerator, localIndex);
					Type fieldType = reader.GetFieldType(num);
					Type memberType = memberMap.MemberType;
					if (memberType == typeof(char) || memberType == typeof(char?))
					{
						ilgenerator.EmitCall(OpCodes.Call, typeof(SqlMapper).GetMethod((memberType == typeof(char)) ? "ReadChar" : "ReadNullableChar", BindingFlags.Static | BindingFlags.Public), null);
					}
					else
					{
						ilgenerator.Emit(OpCodes.Dup);
						ilgenerator.Emit(OpCodes.Isinst, typeof(DBNull));
						ilgenerator.Emit(OpCodes.Brtrue_S, label2);
						Type underlyingType = Nullable.GetUnderlyingType(memberType);
						Type type3 = (underlyingType != null && underlyingType.IsEnum()) ? underlyingType : memberType;
						if (type3.IsEnum())
						{
							Type underlyingType2 = Enum.GetUnderlyingType(type3);
							if (fieldType == typeof(string))
							{
								if (num2 == -1)
								{
									num2 = ilgenerator.DeclareLocal(typeof(string)).LocalIndex;
								}
								ilgenerator.Emit(OpCodes.Castclass, typeof(string));
								SqlMapper.StoreLocal(ilgenerator, num2);
								ilgenerator.Emit(OpCodes.Ldtoken, type3);
								ilgenerator.EmitCall(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"), null);
								SqlMapper.LoadLocal(ilgenerator, num2);
								ilgenerator.Emit(OpCodes.Ldc_I4_1);
								ilgenerator.EmitCall(OpCodes.Call, SqlMapper.enumParse, null);
								ilgenerator.Emit(OpCodes.Unbox_Any, type3);
							}
							else
							{
								SqlMapper.FlexibleConvertBoxedFromHeadOfStack(ilgenerator, fieldType, type3, underlyingType2);
							}
							if (underlyingType != null)
							{
								ilgenerator.Emit(OpCodes.Newobj, memberType.GetConstructor(new Type[]
								{
									underlyingType
								}));
							}
						}
						else if (memberType.FullName == "System.Data.Linq.Binary")
						{
							ilgenerator.Emit(OpCodes.Unbox_Any, typeof(byte[]));
							ilgenerator.Emit(OpCodes.Newobj, memberType.GetConstructor(new Type[]
							{
								typeof(byte[])
							}));
						}
						else
						{
							TypeCode typeCode = TypeExtensions.GetTypeCode(fieldType);
							TypeCode typeCode2 = TypeExtensions.GetTypeCode(type3);
							bool flag3;
							if ((flag3 = SqlMapper.typeHandlers.ContainsKey(type3)) || fieldType == type3 || typeCode == typeCode2 || typeCode == TypeExtensions.GetTypeCode(underlyingType))
							{
								if (flag3)
								{
									ilgenerator.EmitCall(OpCodes.Call, typeof(SqlMapper.TypeHandlerCache<>).MakeGenericType(new Type[]
									{
										type3
									}).GetMethod("Parse"), null);
								}
								else
								{
									ilgenerator.Emit(OpCodes.Unbox_Any, type3);
								}
							}
							else
							{
								SqlMapper.FlexibleConvertBoxedFromHeadOfStack(ilgenerator, fieldType, underlyingType ?? type3, null);
								if (underlyingType != null)
								{
									ilgenerator.Emit(OpCodes.Newobj, type3.GetConstructor(new Type[]
									{
										underlyingType
									}));
								}
							}
						}
					}
					if (specializedConstructor == null)
					{
						if (memberMap.Property != null)
						{
							ilgenerator.Emit(type.IsValueType() ? OpCodes.Call : OpCodes.Callvirt, DefaultTypeMap.GetPropertySetter(memberMap.Property, type));
						}
						else
						{
							ilgenerator.Emit(OpCodes.Stfld, memberMap.Field);
						}
					}
					ilgenerator.Emit(OpCodes.Br_S, label3);
					ilgenerator.MarkLabel(label2);
					if (specializedConstructor != null)
					{
						ilgenerator.Emit(OpCodes.Pop);
						if (memberMap.MemberType.IsValueType())
						{
							int localIndex2 = ilgenerator.DeclareLocal(memberMap.MemberType).LocalIndex;
							SqlMapper.LoadLocalAddress(ilgenerator, localIndex2);
							ilgenerator.Emit(OpCodes.Initobj, memberMap.MemberType);
							SqlMapper.LoadLocal(ilgenerator, localIndex2);
						}
						else
						{
							ilgenerator.Emit(OpCodes.Ldnull);
						}
					}
					else if (applyNullValues && (!memberType.IsValueType() || Nullable.GetUnderlyingType(memberType) != null))
					{
						ilgenerator.Emit(OpCodes.Pop);
						if (memberType.IsValueType())
						{
							SqlMapper.GetTempLocal(ilgenerator, ref dictionary, memberType, true);
						}
						else
						{
							ilgenerator.Emit(OpCodes.Ldnull);
						}
						if (memberMap.Property != null)
						{
							ilgenerator.Emit(type.IsValueType() ? OpCodes.Call : OpCodes.Callvirt, DefaultTypeMap.GetPropertySetter(memberMap.Property, type));
						}
						else
						{
							ilgenerator.Emit(OpCodes.Stfld, memberMap.Field);
						}
					}
					else
					{
						ilgenerator.Emit(OpCodes.Pop);
						ilgenerator.Emit(OpCodes.Pop);
					}
					if (flag2 && returnNullIfFirstMissing)
					{
						ilgenerator.Emit(OpCodes.Pop);
						ilgenerator.Emit(OpCodes.Ldnull);
						ilgenerator.Emit(OpCodes.Stloc_1);
						ilgenerator.Emit(OpCodes.Br, label);
					}
					ilgenerator.MarkLabel(label3);
				}
				flag2 = false;
				num++;
			}
			if (type.IsValueType())
			{
				ilgenerator.Emit(OpCodes.Pop);
			}
			else
			{
				if (specializedConstructor != null)
				{
					ilgenerator.Emit(OpCodes.Newobj, specializedConstructor);
				}
				ilgenerator.Emit(OpCodes.Stloc_1);
				if (flag)
				{
					ilgenerator.Emit(OpCodes.Ldloc_1);
					ilgenerator.EmitCall(OpCodes.Callvirt, typeof(ISupportInitialize).GetMethod("EndInit"), null);
				}
			}
			ilgenerator.MarkLabel(label);
			ilgenerator.BeginCatchBlock(typeof(Exception));
			ilgenerator.Emit(OpCodes.Ldloc_0);
			ilgenerator.Emit(OpCodes.Ldarg_0);
			SqlMapper.LoadLocal(ilgenerator, localIndex);
			ilgenerator.EmitCall(OpCodes.Call, typeof(SqlMapper).GetMethod("ThrowDataException"), null);
			ilgenerator.EndExceptionBlock();
			ilgenerator.Emit(OpCodes.Ldloc_1);
			if (type.IsValueType())
			{
				ilgenerator.Emit(OpCodes.Box, type);
			}
			ilgenerator.Emit(OpCodes.Ret);
			Type funcType = Expression.GetFuncType(new Type[]
			{
				typeof(IDataReader),
				type2
			});
			return (Func<IDataReader, object>)dynamicMethod.CreateDelegate(funcType);
		}

		// Token: 0x060000C7 RID: 199 RVA: 0x000081F0 File Offset: 0x000063F0
		private static void FlexibleConvertBoxedFromHeadOfStack(ILGenerator il, Type from, Type to, Type via)
		{
			if (from == (via ?? to))
			{
				il.Emit(OpCodes.Unbox_Any, to);
				return;
			}
			MethodInfo @operator;
			if ((@operator = SqlMapper.GetOperator(from, to)) != null)
			{
				il.Emit(OpCodes.Unbox_Any, from);
				il.Emit(OpCodes.Call, @operator);
				return;
			}
			bool flag = false;
			OpCode opcode = default(OpCode);
			switch (TypeExtensions.GetTypeCode(from))
			{
			case TypeCode.Boolean:
			case TypeCode.SByte:
			case TypeCode.Byte:
			case TypeCode.Int16:
			case TypeCode.UInt16:
			case TypeCode.Int32:
			case TypeCode.UInt32:
			case TypeCode.Int64:
			case TypeCode.UInt64:
			case TypeCode.Single:
			case TypeCode.Double:
				flag = true;
				switch (TypeExtensions.GetTypeCode(via ?? to))
				{
				case TypeCode.Boolean:
				case TypeCode.Int32:
					opcode = OpCodes.Conv_Ovf_I4;
					goto IL_12D;
				case TypeCode.SByte:
					opcode = OpCodes.Conv_Ovf_I1;
					goto IL_12D;
				case TypeCode.Byte:
					opcode = OpCodes.Conv_Ovf_I1_Un;
					goto IL_12D;
				case TypeCode.Int16:
					opcode = OpCodes.Conv_Ovf_I2;
					goto IL_12D;
				case TypeCode.UInt16:
					opcode = OpCodes.Conv_Ovf_I2_Un;
					goto IL_12D;
				case TypeCode.UInt32:
					opcode = OpCodes.Conv_Ovf_I4_Un;
					goto IL_12D;
				case TypeCode.Int64:
					opcode = OpCodes.Conv_Ovf_I8;
					goto IL_12D;
				case TypeCode.UInt64:
					opcode = OpCodes.Conv_Ovf_I8_Un;
					goto IL_12D;
				case TypeCode.Single:
					opcode = OpCodes.Conv_R4;
					goto IL_12D;
				case TypeCode.Double:
					opcode = OpCodes.Conv_R8;
					goto IL_12D;
				}
				flag = false;
				break;
			}
			IL_12D:
			if (flag)
			{
				il.Emit(OpCodes.Unbox_Any, from);
				il.Emit(opcode);
				if (to == typeof(bool))
				{
					il.Emit(OpCodes.Ldc_I4_0);
					il.Emit(OpCodes.Ceq);
					il.Emit(OpCodes.Ldc_I4_0);
					il.Emit(OpCodes.Ceq);
					return;
				}
			}
			else
			{
				il.Emit(OpCodes.Ldtoken, via ?? to);
				il.EmitCall(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"), null);
				il.EmitCall(OpCodes.Call, typeof(Convert).GetMethod("ChangeType", new Type[]
				{
					typeof(object),
					typeof(Type)
				}), null);
				il.Emit(OpCodes.Unbox_Any, to);
			}
		}

		// Token: 0x060000C8 RID: 200 RVA: 0x00008400 File Offset: 0x00006600
		private static MethodInfo GetOperator(Type from, Type to)
		{
			if (to == null)
			{
				return null;
			}
			MethodInfo[] methods;
			MethodInfo result;
			MethodInfo[] methods2;
			if ((result = SqlMapper.ResolveOperator(methods = from.GetMethods(BindingFlags.Static | BindingFlags.Public), from, to, "op_Implicit")) == null && (result = SqlMapper.ResolveOperator(methods2 = to.GetMethods(BindingFlags.Static | BindingFlags.Public), from, to, "op_Implicit")) == null)
			{
				result = (SqlMapper.ResolveOperator(methods, from, to, "op_Explicit") ?? SqlMapper.ResolveOperator(methods2, from, to, "op_Explicit"));
			}
			return result;
		}

		// Token: 0x060000C9 RID: 201 RVA: 0x0000846C File Offset: 0x0000666C
		private static MethodInfo ResolveOperator(MethodInfo[] methods, Type from, Type to, string name)
		{
			for (int i = 0; i < methods.Length; i++)
			{
				if (!(methods[i].Name != name) && !(methods[i].ReturnType != to))
				{
					ParameterInfo[] parameters = methods[i].GetParameters();
					if (parameters.Length == 1 && !(parameters[0].ParameterType != from))
					{
						return methods[i];
					}
				}
			}
			return null;
		}

		// Token: 0x060000CA RID: 202 RVA: 0x000084CC File Offset: 0x000066CC
		private static void LoadLocal(ILGenerator il, int index)
		{
			if (index < 0 || index >= 32767)
			{
				throw new ArgumentNullException("index");
			}
			switch (index)
			{
			case 0:
				il.Emit(OpCodes.Ldloc_0);
				return;
			case 1:
				il.Emit(OpCodes.Ldloc_1);
				return;
			case 2:
				il.Emit(OpCodes.Ldloc_2);
				return;
			case 3:
				il.Emit(OpCodes.Ldloc_3);
				return;
			default:
				if (index <= 255)
				{
					il.Emit(OpCodes.Ldloc_S, (byte)index);
					return;
				}
				il.Emit(OpCodes.Ldloc, (short)index);
				return;
			}
		}

		// Token: 0x060000CB RID: 203 RVA: 0x0000855C File Offset: 0x0000675C
		private static void StoreLocal(ILGenerator il, int index)
		{
			if (index < 0 || index >= 32767)
			{
				throw new ArgumentNullException("index");
			}
			switch (index)
			{
			case 0:
				il.Emit(OpCodes.Stloc_0);
				return;
			case 1:
				il.Emit(OpCodes.Stloc_1);
				return;
			case 2:
				il.Emit(OpCodes.Stloc_2);
				return;
			case 3:
				il.Emit(OpCodes.Stloc_3);
				return;
			default:
				if (index <= 255)
				{
					il.Emit(OpCodes.Stloc_S, (byte)index);
					return;
				}
				il.Emit(OpCodes.Stloc, (short)index);
				return;
			}
		}

		// Token: 0x060000CC RID: 204 RVA: 0x000085EB File Offset: 0x000067EB
		private static void LoadLocalAddress(ILGenerator il, int index)
		{
			if (index < 0 || index >= 32767)
			{
				throw new ArgumentNullException("index");
			}
			if (index <= 255)
			{
				il.Emit(OpCodes.Ldloca_S, (byte)index);
				return;
			}
			il.Emit(OpCodes.Ldloca, (short)index);
		}

		// Token: 0x060000CD RID: 205 RVA: 0x00008628 File Offset: 0x00006828
		[Obsolete("This method is for internal use only", false)]
		public static void ThrowDataException(Exception ex, int index, IDataReader reader, object value)
		{
			Exception ex3;
			try
			{
				string arg = "(n/a)";
				string arg2 = "(n/a)";
				if (reader != null && index >= 0 && index < reader.FieldCount)
				{
					arg = reader.GetName(index);
					try
					{
						if (value == null || value is DBNull)
						{
							arg2 = "<null>";
						}
						else
						{
							arg2 = Convert.ToString(value) + " - " + TypeExtensions.GetTypeCode(value.GetType());
						}
					}
					catch (Exception ex2)
					{
						arg2 = ex2.Message;
					}
				}
				ex3 = new DataException(string.Format("Error parsing column {0} ({1}={2})", index, arg, arg2), ex);
			}
			catch
			{
				ex3 = new DataException(ex.Message, ex);
			}
			throw ex3;
		}

		// Token: 0x060000CE RID: 206 RVA: 0x000086E0 File Offset: 0x000068E0
		private static void EmitInt32(ILGenerator il, int value)
		{
			switch (value)
			{
			case -1:
				il.Emit(OpCodes.Ldc_I4_M1);
				return;
			case 0:
				il.Emit(OpCodes.Ldc_I4_0);
				return;
			case 1:
				il.Emit(OpCodes.Ldc_I4_1);
				return;
			case 2:
				il.Emit(OpCodes.Ldc_I4_2);
				return;
			case 3:
				il.Emit(OpCodes.Ldc_I4_3);
				return;
			case 4:
				il.Emit(OpCodes.Ldc_I4_4);
				return;
			case 5:
				il.Emit(OpCodes.Ldc_I4_5);
				return;
			case 6:
				il.Emit(OpCodes.Ldc_I4_6);
				return;
			case 7:
				il.Emit(OpCodes.Ldc_I4_7);
				return;
			case 8:
				il.Emit(OpCodes.Ldc_I4_8);
				return;
			default:
				if (value >= -128 && value <= 127)
				{
					il.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
					return;
				}
				il.Emit(OpCodes.Ldc_I4, value);
				return;
			}
		}

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x060000CF RID: 207 RVA: 0x000087BB File Offset: 0x000069BB
		// (set) Token: 0x060000D0 RID: 208 RVA: 0x000087C2 File Offset: 0x000069C2
		public static IEqualityComparer<string> ConnectionStringComparer
		{
			get
			{
				return SqlMapper.connectionStringComparer;
			}
			set
			{
				SqlMapper.connectionStringComparer = (value ?? StringComparer.Ordinal);
			}
		}

		// Token: 0x060000D1 RID: 209 RVA: 0x000087D3 File Offset: 0x000069D3
		public static SqlMapper.ICustomQueryParameter AsTableValuedParameter(this DataTable table, string typeName = null)
		{
			return new TableValuedParameter(table, typeName);
		}

		// Token: 0x060000D2 RID: 210 RVA: 0x000087DC File Offset: 0x000069DC
		public static void SetTypeName(this DataTable table, string typeName)
		{
			if (table != null)
			{
				if (string.IsNullOrEmpty(typeName))
				{
					table.ExtendedProperties.Remove("dapper:TypeName");
					return;
				}
				table.ExtendedProperties["dapper:TypeName"] = typeName;
			}
		}

		// Token: 0x060000D3 RID: 211 RVA: 0x0000880B File Offset: 0x00006A0B
		public static string GetTypeName(this DataTable table)
		{
			return ((table != null) ? table.ExtendedProperties["dapper:TypeName"] : null) as string;
		}

		// Token: 0x060000D4 RID: 212 RVA: 0x00008828 File Offset: 0x00006A28
		public static SqlMapper.ICustomQueryParameter AsTableValuedParameter(this IEnumerable<SqlDataRecord> list, string typeName = null)
		{
			return new SqlDataRecordListTVPParameter(list, typeName);
		}

		// Token: 0x060000D5 RID: 213 RVA: 0x00008834 File Offset: 0x00006A34
		private static StringBuilder GetStringBuilder()
		{
			StringBuilder stringBuilder = SqlMapper.perThreadStringBuilderCache;
			if (stringBuilder != null)
			{
				SqlMapper.perThreadStringBuilderCache = null;
				stringBuilder.Length = 0;
				return stringBuilder;
			}
			return new StringBuilder();
		}

		// Token: 0x060000D6 RID: 214 RVA: 0x0000885E File Offset: 0x00006A5E
		private static string __ToStringRecycle(this StringBuilder obj)
		{
			if (obj == null)
			{
				return "";
			}
			string result = obj.ToString();
			if (SqlMapper.perThreadStringBuilderCache == null)
			{
				SqlMapper.perThreadStringBuilderCache = obj;
			}
			return result;
		}

		// Token: 0x060000D7 RID: 215 RVA: 0x0000887C File Offset: 0x00006A7C
		public static IEnumerable<T> Parse<T>(this IDataReader reader)
		{
			if (reader.Read())
			{
				Func<IDataReader, object> deser = SqlMapper.GetDeserializer(typeof(T), reader, 0, -1, false);
				do
				{
					yield return (T)((object)deser(reader));
				}
				while (reader.Read());
				deser = null;
			}
			yield break;
		}

		// Token: 0x060000D8 RID: 216 RVA: 0x0000888C File Offset: 0x00006A8C
		public static IEnumerable<object> Parse(this IDataReader reader, Type type)
		{
			if (reader.Read())
			{
				Func<IDataReader, object> deser = SqlMapper.GetDeserializer(type, reader, 0, -1, false);
				do
				{
					yield return deser(reader);
				}
				while (reader.Read());
				deser = null;
			}
			yield break;
		}


		public static IEnumerable<dynamic> Parse(this IDataReader reader)
		{
			if (reader.Read())
			{
				Func<IDataReader, object> deser = SqlMapper.GetDapperRowDeserializer(reader, 0, -1, false);
				do
				{
					yield return deser(reader);
				}
				while (reader.Read());
				deser = null;
			}
			yield break;
		}

		// Token: 0x060000DA RID: 218 RVA: 0x000088B3 File Offset: 0x00006AB3
		public static Func<IDataReader, object> GetRowParser(this IDataReader reader, Type type, int startIndex = 0, int length = -1, bool returnNullIfFirstMissing = false)
		{
			return SqlMapper.GetDeserializer(type, reader, startIndex, length, returnNullIfFirstMissing);
		}

		// Token: 0x060000DB RID: 219 RVA: 0x000088C0 File Offset: 0x00006AC0
		public static Func<IDataReader, T> GetRowParser<T>(this IDataReader reader, Type concreteType = null, int startIndex = 0, int length = -1, bool returnNullIfFirstMissing = false)
		{
			if (concreteType == null)
			{
				concreteType = typeof(T);
			}
			Func<IDataReader, object> func = SqlMapper.GetDeserializer(concreteType, reader, startIndex, length, returnNullIfFirstMissing);
			if (concreteType.IsValueType())
			{
				return (IDataReader _) => (T)((object)func(_));
			}
            return (IDataReader d) => (T)func(d);
            //return (Func<IDataReader, T>)func;
		}

		// Token: 0x0400002A RID: 42
		private static readonly ConcurrentDictionary<SqlMapper.Identity, SqlMapper.CacheInfo> _queryCache = new ConcurrentDictionary<SqlMapper.Identity, SqlMapper.CacheInfo>();

		// Token: 0x0400002B RID: 43
		private const int COLLECT_PER_ITEMS = 1000;

		// Token: 0x0400002C RID: 44
		private const int COLLECT_HIT_COUNT_MIN = 0;

		// Token: 0x0400002D RID: 45
		private static int collect;

		// Token: 0x0400002E RID: 46
		private static Dictionary<Type, DbType> typeMap;

		// Token: 0x0400002F RID: 47
		private static Dictionary<Type, SqlMapper.ITypeHandler> typeHandlers;

		// Token: 0x04000030 RID: 48
		internal const string LinqBinary = "System.Data.Linq.Binary";

		// Token: 0x04000031 RID: 49
		private const string ObsoleteInternalUsageOnly = "This method is for internal use only";

		// Token: 0x04000032 RID: 50
		private static readonly int[] ErrTwoRows = new int[2];

		// Token: 0x04000033 RID: 51
		private static readonly int[] ErrZeroRows = new int[0];

		// Token: 0x04000034 RID: 52
		private const CommandBehavior DefaultAllowedCommandBehaviors = (CommandBehavior)(-1);

		// Token: 0x04000035 RID: 53
		private static CommandBehavior allowedCommandBehaviors = (CommandBehavior)(-1);

		// Token: 0x04000036 RID: 54
		private static readonly Regex smellsLikeOleDb = new Regex("(?<![a-z0-9@_])[?@:](?![a-z0-9@_])", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.CultureInvariant);

		// Token: 0x04000037 RID: 55
		private static readonly Regex literalTokens = new Regex("(?<![a-z0-9_])\\{=([a-z0-9_]+)\\}", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.CultureInvariant);

		// Token: 0x04000038 RID: 56
		private static readonly Regex pseudoPositional = new Regex("\\?([a-z_][a-z0-9_]*)\\?", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

		// Token: 0x04000039 RID: 57
		internal static readonly MethodInfo format = typeof(SqlMapper).GetMethod("Format", BindingFlags.Static | BindingFlags.Public);

		// Token: 0x0400003A RID: 58
		private static readonly Dictionary<TypeCode, MethodInfo> toStrings = new Type[]
		{
			typeof(bool),
			typeof(sbyte),
			typeof(byte),
			typeof(ushort),
			typeof(short),
			typeof(uint),
			typeof(int),
			typeof(ulong),
			typeof(long),
			typeof(float),
			typeof(double),
			typeof(decimal)
		}.ToDictionary((Type x) => TypeExtensions.GetTypeCode(x), (Type x) => x.GetPublicInstanceMethod("ToString", new Type[]
		{
			typeof(IFormatProvider)
		}));

		// Token: 0x0400003B RID: 59
		private static readonly MethodInfo StringReplace = typeof(string).GetPublicInstanceMethod("Replace", new Type[]
		{
			typeof(string),
			typeof(string)
		});

		// Token: 0x0400003C RID: 60
		private static readonly MethodInfo InvariantCulture = typeof(CultureInfo).GetProperty("InvariantCulture", BindingFlags.Static | BindingFlags.Public).GetGetMethod();

		// Token: 0x0400003D RID: 61
		private static readonly MethodInfo enumParse = typeof(Enum).GetMethod("Parse", new Type[]
		{
			typeof(Type),
			typeof(string),
			typeof(bool)
		});

		// Token: 0x0400003E RID: 62
		private static readonly MethodInfo getItem = (from p in typeof(IDataRecord).GetProperties(BindingFlags.Instance | BindingFlags.Public)
		where p.GetIndexParameters().Any<ParameterInfo>() && p.GetIndexParameters()[0].ParameterType == typeof(int)
		select p.GetGetMethod()).First<MethodInfo>();

		// Token: 0x0400003F RID: 63
		public static Func<Type, SqlMapper.ITypeMap> TypeMapProvider = (Type type) => new DefaultTypeMap(type);

		// Token: 0x04000040 RID: 64
		private static readonly Hashtable _typeMaps = new Hashtable();

		// Token: 0x04000041 RID: 65
		private static IEqualityComparer<string> connectionStringComparer = StringComparer.Ordinal;

		// Token: 0x04000042 RID: 66
		private const string DataTableTypeNameKey = "dapper:TypeName";

		// Token: 0x04000043 RID: 67
		[ThreadStatic]
		private static StringBuilder perThreadStringBuilderCache;

		// Token: 0x02000020 RID: 32
		private class CacheInfo
		{
			// Token: 0x17000030 RID: 48
			// (get) Token: 0x06000150 RID: 336 RVA: 0x000090D2 File Offset: 0x000072D2
			// (set) Token: 0x06000151 RID: 337 RVA: 0x000090DA File Offset: 0x000072DA
			public SqlMapper.DeserializerState Deserializer { get; set; }

			// Token: 0x17000031 RID: 49
			// (get) Token: 0x06000152 RID: 338 RVA: 0x000090E3 File Offset: 0x000072E3
			// (set) Token: 0x06000153 RID: 339 RVA: 0x000090EB File Offset: 0x000072EB
			public Func<IDataReader, object>[] OtherDeserializers { get; set; }

			// Token: 0x17000032 RID: 50
			// (get) Token: 0x06000154 RID: 340 RVA: 0x000090F4 File Offset: 0x000072F4
			// (set) Token: 0x06000155 RID: 341 RVA: 0x000090FC File Offset: 0x000072FC
			public Action<IDbCommand, object> ParamReader { get; set; }

			// Token: 0x06000156 RID: 342 RVA: 0x00009105 File Offset: 0x00007305
			public int GetHitCount()
			{
				return Interlocked.CompareExchange(ref this.hitCount, 0, 0);
			}

			// Token: 0x06000157 RID: 343 RVA: 0x00009114 File Offset: 0x00007314
			public void RecordHit()
			{
				Interlocked.Increment(ref this.hitCount);
			}

			// Token: 0x0400006E RID: 110
			private int hitCount;
		}

		// Token: 0x02000021 RID: 33
		[Flags]
		internal enum Row
		{
			// Token: 0x04000070 RID: 112
			First = 0,
			// Token: 0x04000071 RID: 113
			FirstOrDefault = 1,
			// Token: 0x04000072 RID: 114
			Single = 2,
			// Token: 0x04000073 RID: 115
			SingleOrDefault = 3
		}

		// Token: 0x02000022 RID: 34
		private sealed class DapperRow : IDynamicMetaObjectProvider, IDictionary<string, object>, ICollection<KeyValuePair<string, object>>, IEnumerable<KeyValuePair<string, object>>, IEnumerable
		{
			// Token: 0x06000159 RID: 345 RVA: 0x00009122 File Offset: 0x00007322
			public DapperRow(SqlMapper.DapperTable table, object[] values)
			{
				if (table == null)
				{
					throw new ArgumentNullException("table");
				}
				if (values == null)
				{
					throw new ArgumentNullException("values");
				}
				this.table = table;
				this.values = values;
			}

			// Token: 0x17000033 RID: 51
			// (get) Token: 0x0600015A RID: 346 RVA: 0x00009154 File Offset: 0x00007354
			int ICollection<KeyValuePair<string, object>>.Count
			{
				get
				{
					int num = 0;
					for (int i = 0; i < this.values.Length; i++)
					{
						if (!(this.values[i] is SqlMapper.DapperRow.DeadValue))
						{
							num++;
						}
					}
					return num;
				}
			}

			// Token: 0x0600015B RID: 347 RVA: 0x0000918C File Offset: 0x0000738C
			public bool TryGetValue(string name, out object value)
			{
				int num = this.table.IndexOfName(name);
				if (num < 0)
				{
					value = null;
					return false;
				}
				value = ((num < this.values.Length) ? this.values[num] : null);
				if (value is SqlMapper.DapperRow.DeadValue)
				{
					value = null;
					return false;
				}
				return true;
			}

			// Token: 0x0600015C RID: 348 RVA: 0x000091D8 File Offset: 0x000073D8
			public override string ToString()
			{
				StringBuilder stringBuilder = SqlMapper.GetStringBuilder().Append("{DapperRow");
				foreach (KeyValuePair<string, object> keyValuePair in this)
				{
					bool value = keyValuePair.Value != null;
					stringBuilder.Append(", ").Append(keyValuePair.Key);
					if (value)
					{
						stringBuilder.Append(" = '").Append(keyValuePair.Value).Append('\'');
					}
					else
					{
						stringBuilder.Append(" = NULL");
					}
				}
				return stringBuilder.Append('}').__ToStringRecycle();
			}

			// Token: 0x0600015D RID: 349 RVA: 0x00009284 File Offset: 0x00007484
			DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
			{
				return new SqlMapper.DapperRowMetaObject(parameter, BindingRestrictions.Empty, this);
			}

			// Token: 0x0600015E RID: 350 RVA: 0x00009292 File Offset: 0x00007492
			public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
			{
				string[] names = this.table.FieldNames;
				int num;
				for (int i = 0; i < names.Length; i = num + 1)
				{
					object obj = (i < this.values.Length) ? this.values[i] : null;
					if (!(obj is SqlMapper.DapperRow.DeadValue))
					{
						yield return new KeyValuePair<string, object>(names[i], obj);
					}
					num = i;
				}
				yield break;
			}

			// Token: 0x0600015F RID: 351 RVA: 0x000092A1 File Offset: 0x000074A1
			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.GetEnumerator();
			}

			// Token: 0x06000160 RID: 352 RVA: 0x000092A9 File Offset: 0x000074A9
			void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
			{
				((IDictionary<string, object>)this).Add(item.Key, item.Value);
			}

			// Token: 0x06000161 RID: 353 RVA: 0x000092C0 File Offset: 0x000074C0
			void ICollection<KeyValuePair<string, object>>.Clear()
			{
				for (int i = 0; i < this.values.Length; i++)
				{
					this.values[i] = SqlMapper.DapperRow.DeadValue.Default;
				}
			}

			// Token: 0x06000162 RID: 354 RVA: 0x000092F0 File Offset: 0x000074F0
			bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
			{
				object objA;
				return this.TryGetValue(item.Key, out objA) && object.Equals(objA, item.Value);
			}

			// Token: 0x06000163 RID: 355 RVA: 0x00009320 File Offset: 0x00007520
			void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
			{
				foreach (KeyValuePair<string, object> keyValuePair in this)
				{
					array[arrayIndex++] = keyValuePair;
				}
			}

			// Token: 0x06000164 RID: 356 RVA: 0x00009370 File Offset: 0x00007570
			bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
			{
				return ((IDictionary<string, object>)this).Remove(item.Key);
			}

			// Token: 0x17000034 RID: 52
			// (get) Token: 0x06000165 RID: 357 RVA: 0x0000937F File Offset: 0x0000757F
			bool ICollection<KeyValuePair<string, object>>.IsReadOnly
			{
				get
				{
					return false;
				}
			}

			// Token: 0x06000166 RID: 358 RVA: 0x00009384 File Offset: 0x00007584
			bool IDictionary<string, object>.ContainsKey(string key)
			{
				int num = this.table.IndexOfName(key);
				return num >= 0 && num < this.values.Length && !(this.values[num] is SqlMapper.DapperRow.DeadValue);
			}

			// Token: 0x06000167 RID: 359 RVA: 0x000093BF File Offset: 0x000075BF
			void IDictionary<string, object>.Add(string key, object value)
			{
				this.SetValue(key, value, true);
			}

			// Token: 0x06000168 RID: 360 RVA: 0x000093CC File Offset: 0x000075CC
			bool IDictionary<string, object>.Remove(string key)
			{
				int num = this.table.IndexOfName(key);
				if (num < 0 || num >= this.values.Length || this.values[num] is SqlMapper.DapperRow.DeadValue)
				{
					return false;
				}
				this.values[num] = SqlMapper.DapperRow.DeadValue.Default;
				return true;
			}

			// Token: 0x17000035 RID: 53
			object IDictionary<string, object>.this[string key]
			{
				get
				{
					object result;
					this.TryGetValue(key, out result);
					return result;
				}
				set
				{
					this.SetValue(key, value, false);
				}
			}

			// Token: 0x0600016B RID: 363 RVA: 0x00009438 File Offset: 0x00007638
			public object SetValue(string key, object value)
			{
				return this.SetValue(key, value, false);
			}

			// Token: 0x0600016C RID: 364 RVA: 0x00009444 File Offset: 0x00007644
			private object SetValue(string key, object value, bool isAdd)
			{
				if (key == null)
				{
					throw new ArgumentNullException("key");
				}
				int num = this.table.IndexOfName(key);
				if (num < 0)
				{
					num = this.table.AddField(key);
				}
				else if (isAdd && num < this.values.Length && !(this.values[num] is SqlMapper.DapperRow.DeadValue))
				{
					throw new ArgumentException("An item with the same key has already been added", "key");
				}
				int num2 = this.values.Length;
				if (num2 <= num)
				{
					Array.Resize<object>(ref this.values, this.table.FieldCount);
					for (int i = num2; i < this.values.Length; i++)
					{
						this.values[i] = SqlMapper.DapperRow.DeadValue.Default;
					}
				}
				this.values[num] = value;
				return value;
			}

			// Token: 0x17000036 RID: 54
			// (get) Token: 0x0600016D RID: 365 RVA: 0x000094FB File Offset: 0x000076FB
			ICollection<string> IDictionary<string, object>.Keys
			{
				get
				{
					return (from kv in this
					select kv.Key).ToArray<string>();
				}
			}

			// Token: 0x17000037 RID: 55
			// (get) Token: 0x0600016E RID: 366 RVA: 0x00009527 File Offset: 0x00007727
			ICollection<object> IDictionary<string, object>.Values
			{
				get
				{
					return (from kv in this
					select kv.Value).ToArray<object>();
				}
			}

			// Token: 0x04000074 RID: 116
			private readonly SqlMapper.DapperTable table;

			// Token: 0x04000075 RID: 117
			private object[] values;

			// Token: 0x0200004D RID: 77
			private sealed class DeadValue
			{
				// Token: 0x06000250 RID: 592 RVA: 0x0000240C File Offset: 0x0000060C
				private DeadValue()
				{
				}

				// Token: 0x04000118 RID: 280
				public static readonly SqlMapper.DapperRow.DeadValue Default = new SqlMapper.DapperRow.DeadValue();
			}
		}

		// Token: 0x02000023 RID: 35
		private sealed class DapperRowMetaObject : DynamicMetaObject
		{
			// Token: 0x0600016F RID: 367 RVA: 0x00009553 File Offset: 0x00007753
			public DapperRowMetaObject(Expression expression, BindingRestrictions restrictions) : base(expression, restrictions)
			{
			}

			// Token: 0x06000170 RID: 368 RVA: 0x0000955D File Offset: 0x0000775D
			public DapperRowMetaObject(Expression expression, BindingRestrictions restrictions, object value) : base(expression, restrictions, value)
			{
			}

			// Token: 0x06000171 RID: 369 RVA: 0x00009568 File Offset: 0x00007768
			private DynamicMetaObject CallMethod(MethodInfo method, Expression[] parameters)
			{
				return new DynamicMetaObject(Expression.Call(Expression.Convert(base.Expression, base.LimitType), method, parameters), BindingRestrictions.GetTypeRestriction(base.Expression, base.LimitType));
			}

			// Token: 0x06000172 RID: 370 RVA: 0x00009598 File Offset: 0x00007798
			public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
			{
				Expression[] parameters = new Expression[]
				{
					Expression.Constant(binder.Name)
				};
				return this.CallMethod(SqlMapper.DapperRowMetaObject.getValueMethod, parameters);
			}

			// Token: 0x06000173 RID: 371 RVA: 0x000095C8 File Offset: 0x000077C8
			public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
			{
				Expression[] parameters = new Expression[]
				{
					Expression.Constant(binder.Name)
				};
				return this.CallMethod(SqlMapper.DapperRowMetaObject.getValueMethod, parameters);
			}

			// Token: 0x06000174 RID: 372 RVA: 0x000095F8 File Offset: 0x000077F8
			public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
			{
				Expression[] parameters = new Expression[]
				{
					Expression.Constant(binder.Name),
					value.Expression
				};
				return this.CallMethod(SqlMapper.DapperRowMetaObject.setValueMethod, parameters);
			}

			// Token: 0x04000076 RID: 118
			private static readonly MethodInfo getValueMethod = typeof(IDictionary<string, object>).GetProperty("Item").GetGetMethod();

			// Token: 0x04000077 RID: 119
			private static readonly MethodInfo setValueMethod = typeof(SqlMapper.DapperRow).GetMethod("SetValue", new Type[]
			{
				typeof(string),
				typeof(object)
			});
		}

		// Token: 0x02000024 RID: 36
		private sealed class DapperTable
		{
			// Token: 0x17000038 RID: 56
			// (get) Token: 0x06000176 RID: 374 RVA: 0x00009694 File Offset: 0x00007894
			internal string[] FieldNames
			{
				get
				{
					return this.fieldNames;
				}
			}

			// Token: 0x06000177 RID: 375 RVA: 0x0000969C File Offset: 0x0000789C
			public DapperTable(string[] fieldNames)
			{
				if (fieldNames == null)
				{
					throw new ArgumentNullException("fieldNames");
				}
				this.fieldNames = fieldNames;
				this.fieldNameLookup = new Dictionary<string, int>(fieldNames.Length, StringComparer.Ordinal);
				for (int i = fieldNames.Length - 1; i >= 0; i--)
				{
					string text = fieldNames[i];
					if (text != null)
					{
						this.fieldNameLookup[text] = i;
					}
				}
			}

			// Token: 0x06000178 RID: 376 RVA: 0x000096FC File Offset: 0x000078FC
			internal int IndexOfName(string name)
			{
				int result;
				if (name == null || !this.fieldNameLookup.TryGetValue(name, out result))
				{
					return -1;
				}
				return result;
			}

			// Token: 0x06000179 RID: 377 RVA: 0x00009720 File Offset: 0x00007920
			internal int AddField(string name)
			{
				if (name == null)
				{
					throw new ArgumentNullException("name");
				}
				if (this.fieldNameLookup.ContainsKey(name))
				{
					throw new InvalidOperationException("Field already exists: " + name);
				}
				int num = this.fieldNames.Length;
				Array.Resize<string>(ref this.fieldNames, num + 1);
				this.fieldNames[num] = name;
				this.fieldNameLookup[name] = num;
				return num;
			}

			// Token: 0x0600017A RID: 378 RVA: 0x00009788 File Offset: 0x00007988
			internal bool FieldExists(string key)
			{
				return key != null && this.fieldNameLookup.ContainsKey(key);
			}

			// Token: 0x17000039 RID: 57
			// (get) Token: 0x0600017B RID: 379 RVA: 0x0000979B File Offset: 0x0000799B
			public int FieldCount
			{
				get
				{
					return this.fieldNames.Length;
				}
			}

			// Token: 0x04000078 RID: 120
			private string[] fieldNames;

			// Token: 0x04000079 RID: 121
			private readonly Dictionary<string, int> fieldNameLookup;
		}

		// Token: 0x02000025 RID: 37
		private struct DeserializerState
		{
			// Token: 0x0600017C RID: 380 RVA: 0x000097A5 File Offset: 0x000079A5
			public DeserializerState(int hash, Func<IDataReader, object> func)
			{
				this.Hash = hash;
				this.Func = func;
			}

			// Token: 0x0400007A RID: 122
			public readonly int Hash;

			// Token: 0x0400007B RID: 123
			public readonly Func<IDataReader, object> Func;
		}

		// Token: 0x02000026 RID: 38
		private class DontMap
		{
		}

		// Token: 0x02000027 RID: 39
		public class GridReader : IDisposable
		{
			// Token: 0x0600017E RID: 382 RVA: 0x000097B5 File Offset: 0x000079B5
			internal GridReader(IDbCommand command, IDataReader reader, SqlMapper.Identity identity, SqlMapper.IParameterCallbacks callbacks, bool addToCache)
			{
				this.Command = command;
				this.reader = reader;
				this.identity = identity;
				this.callbacks = callbacks;
				this.addToCache = addToCache;
			}

			public IEnumerable<dynamic> Read(bool buffered = true)
			{
				return this.ReadImpl<object>(typeof(SqlMapper.DapperRow), buffered);
			}

			public dynamic ReadFirst()
			{
				return this.ReadRow<object>(typeof(SqlMapper.DapperRow), SqlMapper.Row.First);
			}

			public dynamic ReadFirstOrDefault()
			{
				return this.ReadRow<object>(typeof(SqlMapper.DapperRow), SqlMapper.Row.FirstOrDefault);
			}

			public dynamic ReadSingle()
			{
				return this.ReadRow<object>(typeof(SqlMapper.DapperRow), SqlMapper.Row.Single);
			}

			public dynamic ReadSingleOrDefault()
			{
				return this.ReadRow<object>(typeof(SqlMapper.DapperRow), SqlMapper.Row.SingleOrDefault);
			}

			// Token: 0x06000184 RID: 388 RVA: 0x00009841 File Offset: 0x00007A41
			public IEnumerable<T> Read<T>(bool buffered = true)
			{
				return this.ReadImpl<T>(typeof(T), buffered);
			}

			// Token: 0x06000185 RID: 389 RVA: 0x00009854 File Offset: 0x00007A54
			public T ReadFirst<T>()
			{
				return this.ReadRow<T>(typeof(T), SqlMapper.Row.First);
			}

			// Token: 0x06000186 RID: 390 RVA: 0x00009867 File Offset: 0x00007A67
			public T ReadFirstOrDefault<T>()
			{
				return this.ReadRow<T>(typeof(T), SqlMapper.Row.FirstOrDefault);
			}

			// Token: 0x06000187 RID: 391 RVA: 0x0000987A File Offset: 0x00007A7A
			public T ReadSingle<T>()
			{
				return this.ReadRow<T>(typeof(T), SqlMapper.Row.Single);
			}

			// Token: 0x06000188 RID: 392 RVA: 0x0000988D File Offset: 0x00007A8D
			public T ReadSingleOrDefault<T>()
			{
				return this.ReadRow<T>(typeof(T), SqlMapper.Row.SingleOrDefault);
			}

			// Token: 0x06000189 RID: 393 RVA: 0x000098A0 File Offset: 0x00007AA0
			public IEnumerable<object> Read(Type type, bool buffered = true)
			{
				if (type == null)
				{
					throw new ArgumentNullException("type");
				}
				return this.ReadImpl<object>(type, buffered);
			}

			// Token: 0x0600018A RID: 394 RVA: 0x000098BE File Offset: 0x00007ABE
			public object ReadFirst(Type type)
			{
				if (type == null)
				{
					throw new ArgumentNullException("type");
				}
				return this.ReadRow<object>(type, SqlMapper.Row.First);
			}

			// Token: 0x0600018B RID: 395 RVA: 0x000098DC File Offset: 0x00007ADC
			public object ReadFirstOrDefault(Type type)
			{
				if (type == null)
				{
					throw new ArgumentNullException("type");
				}
				return this.ReadRow<object>(type, SqlMapper.Row.FirstOrDefault);
			}

			// Token: 0x0600018C RID: 396 RVA: 0x000098FA File Offset: 0x00007AFA
			public object ReadSingle(Type type)
			{
				if (type == null)
				{
					throw new ArgumentNullException("type");
				}
				return this.ReadRow<object>(type, SqlMapper.Row.Single);
			}

			// Token: 0x0600018D RID: 397 RVA: 0x00009918 File Offset: 0x00007B18
			public object ReadSingleOrDefault(Type type)
			{
				if (type == null)
				{
					throw new ArgumentNullException("type");
				}
				return this.ReadRow<object>(type, SqlMapper.Row.SingleOrDefault);
			}

			// Token: 0x0600018E RID: 398 RVA: 0x00009938 File Offset: 0x00007B38
			private IEnumerable<T> ReadImpl<T>(Type type, bool buffered)
			{
				if (this.reader == null)
				{
					throw new ObjectDisposedException(base.GetType().FullName, "The reader has been disposed; this can happen after all data has been consumed");
				}
				if (this.IsConsumed)
				{
					throw new InvalidOperationException("Query results must be consumed in the correct order, and each result can only be consumed once");
				}
				SqlMapper.Identity typedIdentity = this.identity.ForGrid(type, this.gridIndex);
				SqlMapper.CacheInfo cacheInfo = SqlMapper.GetCacheInfo(typedIdentity, null, this.addToCache);
				SqlMapper.DeserializerState deserializer = cacheInfo.Deserializer;
				int columnHash = SqlMapper.GetColumnHash(this.reader, 0, -1);
				if (deserializer.Func == null || deserializer.Hash != columnHash)
				{
					deserializer = new SqlMapper.DeserializerState(columnHash, SqlMapper.GetDeserializer(type, this.reader, 0, -1, false));
					cacheInfo.Deserializer = deserializer;
				}
				this.IsConsumed = true;
				IEnumerable<T> enumerable = this.ReadDeferred<T>(this.gridIndex, deserializer.Func, typedIdentity, type);
				if (!buffered)
				{
					return enumerable;
				}
				return enumerable.ToList<T>();
			}

			// Token: 0x0600018F RID: 399 RVA: 0x00009A0C File Offset: 0x00007C0C
			private T ReadRow<T>(Type type, SqlMapper.Row row)
			{
				if (this.reader == null)
				{
					throw new ObjectDisposedException(base.GetType().FullName, "The reader has been disposed; this can happen after all data has been consumed");
				}
				if (this.IsConsumed)
				{
					throw new InvalidOperationException("Query results must be consumed in the correct order, and each result can only be consumed once");
				}
				this.IsConsumed = true;
				T result = default(T);
				if (this.reader.Read() && this.reader.FieldCount != 0)
				{
					SqlMapper.CacheInfo cacheInfo = SqlMapper.GetCacheInfo(this.identity.ForGrid(type, this.gridIndex), null, this.addToCache);
					SqlMapper.DeserializerState deserializer = cacheInfo.Deserializer;
					int columnHash = SqlMapper.GetColumnHash(this.reader, 0, -1);
					if (deserializer.Func == null || deserializer.Hash != columnHash)
					{
						deserializer = new SqlMapper.DeserializerState(columnHash, SqlMapper.GetDeserializer(type, this.reader, 0, -1, false));
						cacheInfo.Deserializer = deserializer;
					}
					object obj = deserializer.Func(this.reader);
					if (obj == null || obj is T)
					{
						result = (T)((object)obj);
					}
					else
					{
						Type conversionType = Nullable.GetUnderlyingType(type) ?? type;
						result = (T)((object)Convert.ChangeType(obj, conversionType, CultureInfo.InvariantCulture));
					}
					if ((row & SqlMapper.Row.Single) != SqlMapper.Row.First && this.reader.Read())
					{
						SqlMapper.ThrowMultipleRows(row);
					}
					while (this.reader.Read())
					{
					}
				}
				else if ((row & SqlMapper.Row.FirstOrDefault) == SqlMapper.Row.First)
				{
					SqlMapper.ThrowZeroRows(row);
				}
				this.NextResult();
				return result;
			}

			// Token: 0x06000190 RID: 400 RVA: 0x00009B60 File Offset: 0x00007D60
			private IEnumerable<TReturn> MultiReadInternal<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(Delegate func, string splitOn)
			{
				SqlMapper.Identity identity = this.identity.ForGrid(typeof(TReturn), new Type[]
				{
					typeof(TFirst),
					typeof(TSecond),
					typeof(TThird),
					typeof(TFourth),
					typeof(TFifth),
					typeof(TSixth),
					typeof(TSeventh)
				}, this.gridIndex);
				try
				{
					foreach (TReturn treturn in SqlMapper.MultiMapImpl<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>((IDbConnection)null, default(CommandDefinition), func, splitOn, this.reader, identity, false))
					{
						yield return treturn;
					}
					IEnumerator<TReturn> enumerator = null;
				}
				finally
				{
					this.NextResult();
				}
				yield break;
				yield break;
			}

			// Token: 0x06000191 RID: 401 RVA: 0x00009B7E File Offset: 0x00007D7E
			private IEnumerable<TReturn> MultiReadInternal<TReturn>(Type[] types, Func<object[], TReturn> map, string splitOn)
			{
				SqlMapper.Identity identity = this.identity.ForGrid(typeof(TReturn), types, this.gridIndex);
				try
				{
					foreach (TReturn treturn in SqlMapper.MultiMapImpl((IDbConnection)null, default(CommandDefinition), types, map, splitOn, this.reader, identity, false))
					{
						yield return treturn;
					}
					IEnumerator<TReturn> enumerator = null;
				}
				finally
				{
					this.NextResult();
				}
				yield break;
				yield break;
			}

			// Token: 0x06000192 RID: 402 RVA: 0x00009BA4 File Offset: 0x00007DA4
			public IEnumerable<TReturn> Read<TFirst, TSecond, TReturn>(Func<TFirst, TSecond, TReturn> func, string splitOn = "id", bool buffered = true)
			{
				IEnumerable<TReturn> enumerable = this.MultiReadInternal<TFirst, TSecond, SqlMapper.DontMap, SqlMapper.DontMap, SqlMapper.DontMap, SqlMapper.DontMap, SqlMapper.DontMap, TReturn>(func, splitOn);
				if (!buffered)
				{
					return enumerable;
				}
				return enumerable.ToList<TReturn>();
			}

			// Token: 0x06000193 RID: 403 RVA: 0x00009BC8 File Offset: 0x00007DC8
			public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TReturn>(Func<TFirst, TSecond, TThird, TReturn> func, string splitOn = "id", bool buffered = true)
			{
				IEnumerable<TReturn> enumerable = this.MultiReadInternal<TFirst, TSecond, TThird, SqlMapper.DontMap, SqlMapper.DontMap, SqlMapper.DontMap, SqlMapper.DontMap, TReturn>(func, splitOn);
				if (!buffered)
				{
					return enumerable;
				}
				return enumerable.ToList<TReturn>();
			}

			// Token: 0x06000194 RID: 404 RVA: 0x00009BEC File Offset: 0x00007DEC
			public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TFourth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TReturn> func, string splitOn = "id", bool buffered = true)
			{
				IEnumerable<TReturn> enumerable = this.MultiReadInternal<TFirst, TSecond, TThird, TFourth, SqlMapper.DontMap, SqlMapper.DontMap, SqlMapper.DontMap, TReturn>(func, splitOn);
				if (!buffered)
				{
					return enumerable;
				}
				return enumerable.ToList<TReturn>();
			}

			// Token: 0x06000195 RID: 405 RVA: 0x00009C10 File Offset: 0x00007E10
			public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> func, string splitOn = "id", bool buffered = true)
			{
				IEnumerable<TReturn> enumerable = this.MultiReadInternal<TFirst, TSecond, TThird, TFourth, TFifth, SqlMapper.DontMap, SqlMapper.DontMap, TReturn>(func, splitOn);
				if (!buffered)
				{
					return enumerable;
				}
				return enumerable.ToList<TReturn>();
			}

			// Token: 0x06000196 RID: 406 RVA: 0x00009C34 File Offset: 0x00007E34
			public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> func, string splitOn = "id", bool buffered = true)
			{
				IEnumerable<TReturn> enumerable = this.MultiReadInternal<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, SqlMapper.DontMap, TReturn>(func, splitOn);
				if (!buffered)
				{
					return enumerable;
				}
				return enumerable.ToList<TReturn>();
			}

			// Token: 0x06000197 RID: 407 RVA: 0x00009C58 File Offset: 0x00007E58
			public IEnumerable<TReturn> Read<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn> func, string splitOn = "id", bool buffered = true)
			{
				IEnumerable<TReturn> enumerable = this.MultiReadInternal<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(func, splitOn);
				if (!buffered)
				{
					return enumerable;
				}
				return enumerable.ToList<TReturn>();
			}

			// Token: 0x06000198 RID: 408 RVA: 0x00009C7C File Offset: 0x00007E7C
			public IEnumerable<TReturn> Read<TReturn>(Type[] types, Func<object[], TReturn> map, string splitOn = "id", bool buffered = true)
			{
				IEnumerable<TReturn> enumerable = this.MultiReadInternal<TReturn>(types, map, splitOn);
				if (!buffered)
				{
					return enumerable;
				}
				return enumerable.ToList<TReturn>();
			}

			// Token: 0x06000199 RID: 409 RVA: 0x00009CA1 File Offset: 0x00007EA1
			private IEnumerable<T> ReadDeferred<T>(int index, Func<IDataReader, object> deserializer, SqlMapper.Identity typedIdentity, Type effectiveType)
			{
				try
				{
					Type convertToType = Nullable.GetUnderlyingType(effectiveType) ?? effectiveType;
					while (index == this.gridIndex && this.reader.Read())
					{
						object val = deserializer(this.reader);
						if (val == null || val is T)
						{
							yield return (T)((object)val);
						}
						else
						{
							yield return (T)((object)Convert.ChangeType(val, convertToType, CultureInfo.InvariantCulture));
						}
						val = null;
					}
					convertToType = null;
				}
				finally
				{
					if (index == this.gridIndex)
					{
						this.NextResult();
					}
				}
				yield break;
				yield break;
			}

			// Token: 0x1700003A RID: 58
			// (get) Token: 0x0600019A RID: 410 RVA: 0x00009CC7 File Offset: 0x00007EC7
			// (set) Token: 0x0600019B RID: 411 RVA: 0x00009CCF File Offset: 0x00007ECF
			public bool IsConsumed { get; private set; }

			// Token: 0x1700003B RID: 59
			// (get) Token: 0x0600019C RID: 412 RVA: 0x00009CD8 File Offset: 0x00007ED8
			// (set) Token: 0x0600019D RID: 413 RVA: 0x00009CE0 File Offset: 0x00007EE0
			public IDbCommand Command { get; set; }

			// Token: 0x0600019E RID: 414 RVA: 0x00009CEC File Offset: 0x00007EEC
			private void NextResult()
			{
				if (this.reader.NextResult())
				{
					this.readCount++;
					this.gridIndex++;
					this.IsConsumed = false;
					return;
				}
				this.reader.Dispose();
				this.reader = null;
				SqlMapper.IParameterCallbacks parameterCallbacks = this.callbacks;
				if (parameterCallbacks != null)
				{
					parameterCallbacks.OnCompleted();
				}
				this.Dispose();
			}

			// Token: 0x0600019F RID: 415 RVA: 0x00009D54 File Offset: 0x00007F54
			public void Dispose()
			{
				if (this.reader != null)
				{
					if (!this.reader.IsClosed)
					{
						IDbCommand command = this.Command;
						if (command != null)
						{
							command.Cancel();
						}
					}
					this.reader.Dispose();
					this.reader = null;
				}
				if (this.Command != null)
				{
					this.Command.Dispose();
					this.Command = null;
				}
			}

			// Token: 0x0400007C RID: 124
			private IDataReader reader;

			// Token: 0x0400007D RID: 125
			private SqlMapper.Identity identity;

			// Token: 0x0400007E RID: 126
			private bool addToCache;

			// Token: 0x0400007F RID: 127
			private int gridIndex;

			// Token: 0x04000080 RID: 128
			private int readCount;

			// Token: 0x04000081 RID: 129
			private SqlMapper.IParameterCallbacks callbacks;
		}

		// Token: 0x02000028 RID: 40
		public interface ICustomQueryParameter
		{
			// Token: 0x060001A0 RID: 416
			void AddParameter(IDbCommand command, string name);
		}

		// Token: 0x02000029 RID: 41
		public class Identity : IEquatable<SqlMapper.Identity>
		{
			// Token: 0x060001A1 RID: 417 RVA: 0x00009DB3 File Offset: 0x00007FB3
			internal SqlMapper.Identity ForGrid(Type primaryType, int gridIndex)
			{
				return new SqlMapper.Identity(this.sql, this.commandType, this.connectionString, primaryType, this.parametersType, null, gridIndex);
			}

			// Token: 0x060001A2 RID: 418 RVA: 0x00009DD5 File Offset: 0x00007FD5
			internal SqlMapper.Identity ForGrid(Type primaryType, Type[] otherTypes, int gridIndex)
			{
				return new SqlMapper.Identity(this.sql, this.commandType, this.connectionString, primaryType, this.parametersType, otherTypes, gridIndex);
			}

			// Token: 0x060001A3 RID: 419 RVA: 0x00009DF7 File Offset: 0x00007FF7
			public SqlMapper.Identity ForDynamicParameters(Type type)
			{
				return new SqlMapper.Identity(this.sql, this.commandType, this.connectionString, this.type, type, null, -1);
			}

			// Token: 0x060001A4 RID: 420 RVA: 0x00009E19 File Offset: 0x00008019
			internal Identity(string sql, CommandType? commandType, IDbConnection connection, Type type, Type parametersType, Type[] otherTypes) : this(sql, commandType, connection.ConnectionString, type, parametersType, otherTypes, 0)
			{
			}

			// Token: 0x060001A5 RID: 421 RVA: 0x00009E30 File Offset: 0x00008030
			private Identity(string sql, CommandType? commandType, string connectionString, Type type, Type parametersType, Type[] otherTypes, int gridIndex)
			{
				this.sql = sql;
				this.commandType = commandType;
				this.connectionString = connectionString;
				this.type = type;
				this.parametersType = parametersType;
				this.gridIndex = gridIndex;
				this.hashCode = 17;
				this.hashCode = this.hashCode * 23 + commandType.GetHashCode();
				this.hashCode = this.hashCode * 23 + gridIndex.GetHashCode();
				this.hashCode = this.hashCode * 23 + ((sql != null) ? sql.GetHashCode() : 0);
				this.hashCode = this.hashCode * 23 + ((type != null) ? type.GetHashCode() : 0);
				if (otherTypes != null)
				{
					foreach (Type type2 in otherTypes)
					{
						this.hashCode = this.hashCode * 23 + ((type2 != null) ? type2.GetHashCode() : 0);
					}
				}
				this.hashCode = this.hashCode * 23 + ((connectionString == null) ? 0 : SqlMapper.connectionStringComparer.GetHashCode(connectionString));
				this.hashCode = this.hashCode * 23 + ((parametersType != null) ? parametersType.GetHashCode() : 0);
			}

			// Token: 0x060001A6 RID: 422 RVA: 0x00009F5A File Offset: 0x0000815A
			public override bool Equals(object obj)
			{
				return this.Equals(obj as SqlMapper.Identity);
			}

			// Token: 0x060001A7 RID: 423 RVA: 0x00009F68 File Offset: 0x00008168
			public override int GetHashCode()
			{
				return this.hashCode;
			}

			// Token: 0x060001A8 RID: 424 RVA: 0x00009F70 File Offset: 0x00008170
			public bool Equals(SqlMapper.Identity other)
			{
				return other != null && this.gridIndex == other.gridIndex && this.type == other.type && this.sql == other.sql && this.commandType == other.commandType && SqlMapper.connectionStringComparer.Equals(this.connectionString, other.connectionString) && this.parametersType == other.parametersType;
			}

			// Token: 0x04000084 RID: 132
			public readonly string sql;

			// Token: 0x04000085 RID: 133
			public readonly CommandType? commandType;

			// Token: 0x04000086 RID: 134
			public readonly int hashCode;

			// Token: 0x04000087 RID: 135
			public readonly int gridIndex;

			// Token: 0x04000088 RID: 136
			public readonly Type type;

			// Token: 0x04000089 RID: 137
			public readonly string connectionString;

			// Token: 0x0400008A RID: 138
			public readonly Type parametersType;
		}

		// Token: 0x0200002A RID: 42
		public interface IDynamicParameters
		{
			// Token: 0x060001A9 RID: 425
			void AddParameters(IDbCommand command, SqlMapper.Identity identity);
		}

		// Token: 0x0200002B RID: 43
		public interface IMemberMap
		{
			// Token: 0x1700003C RID: 60
			// (get) Token: 0x060001AA RID: 426
			string ColumnName { get; }

			// Token: 0x1700003D RID: 61
			// (get) Token: 0x060001AB RID: 427
			Type MemberType { get; }

			// Token: 0x1700003E RID: 62
			// (get) Token: 0x060001AC RID: 428
			PropertyInfo Property { get; }

			// Token: 0x1700003F RID: 63
			// (get) Token: 0x060001AD RID: 429
			FieldInfo Field { get; }

			// Token: 0x17000040 RID: 64
			// (get) Token: 0x060001AE RID: 430
			ParameterInfo Parameter { get; }
		}

		// Token: 0x0200002C RID: 44
		public interface IParameterCallbacks : SqlMapper.IDynamicParameters
		{
			// Token: 0x060001AF RID: 431
			void OnCompleted();
		}

		// Token: 0x0200002D RID: 45
		public interface IParameterLookup : SqlMapper.IDynamicParameters
		{
			// Token: 0x17000041 RID: 65
			object this[string name]
			{
				get;
			}
		}

		// Token: 0x0200002E RID: 46
		public interface ITypeHandler
		{
			// Token: 0x060001B1 RID: 433
			void SetValue(IDbDataParameter parameter, object value);

			// Token: 0x060001B2 RID: 434
			object Parse(Type destinationType, object value);
		}

		// Token: 0x0200002F RID: 47
		public interface ITypeMap
		{
			// Token: 0x060001B3 RID: 435
			ConstructorInfo FindConstructor(string[] names, Type[] types);

			// Token: 0x060001B4 RID: 436
			ConstructorInfo FindExplicitConstructor();

			// Token: 0x060001B5 RID: 437
			SqlMapper.IMemberMap GetConstructorParameter(ConstructorInfo constructor, string columnName);

			// Token: 0x060001B6 RID: 438
			SqlMapper.IMemberMap GetMember(string columnName);
		}

		// Token: 0x02000030 RID: 48
		internal class Link<TKey, TValue> where TKey : class
		{
			// Token: 0x060001B7 RID: 439 RVA: 0x0000A018 File Offset: 0x00008218
			public static bool TryGet(SqlMapper.Link<TKey, TValue> link, TKey key, out TValue value)
			{
				while (link != null)
				{
					if (key == link.Key)
					{
						value = link.Value;
						return true;
					}
					link = link.Tail;
				}
				value = default(TValue);
				return false;
			}

			// Token: 0x060001B8 RID: 440 RVA: 0x0000A050 File Offset: 0x00008250
			public static bool TryAdd(ref SqlMapper.Link<TKey, TValue> head, TKey key, ref TValue value)
			{
				TValue tvalue;
				for (;;)
				{
					SqlMapper.Link<TKey, TValue> link = Interlocked.CompareExchange<SqlMapper.Link<TKey, TValue>>(ref head, null, null);
					if (SqlMapper.Link<TKey, TValue>.TryGet(link, key, out tvalue))
					{
						break;
					}
					SqlMapper.Link<TKey, TValue> value2 = new SqlMapper.Link<TKey, TValue>(key, value, link);
					if (Interlocked.CompareExchange<SqlMapper.Link<TKey, TValue>>(ref head, value2, link) == link)
					{
						return true;
					}
				}
				value = tvalue;
				return false;
			}

			// Token: 0x060001B9 RID: 441 RVA: 0x0000A09B File Offset: 0x0000829B
			private Link(TKey key, TValue value, SqlMapper.Link<TKey, TValue> tail)
			{
				this.Key = key;
				this.Value = value;
				this.Tail = tail;
			}

			// Token: 0x17000042 RID: 66
			// (get) Token: 0x060001BA RID: 442 RVA: 0x0000A0B8 File Offset: 0x000082B8
			public TKey Key { get; }

			// Token: 0x17000043 RID: 67
			// (get) Token: 0x060001BB RID: 443 RVA: 0x0000A0C0 File Offset: 0x000082C0
			public TValue Value { get; }

			// Token: 0x17000044 RID: 68
			// (get) Token: 0x060001BC RID: 444 RVA: 0x0000A0C8 File Offset: 0x000082C8
			public SqlMapper.Link<TKey, TValue> Tail { get; }
		}

		// Token: 0x02000031 RID: 49
		internal struct LiteralToken
		{
			// Token: 0x17000045 RID: 69
			// (get) Token: 0x060001BD RID: 445 RVA: 0x0000A0D0 File Offset: 0x000082D0
			public string Token { get; }

			// Token: 0x17000046 RID: 70
			// (get) Token: 0x060001BE RID: 446 RVA: 0x0000A0D8 File Offset: 0x000082D8
			public string Member { get; }

			// Token: 0x060001BF RID: 447 RVA: 0x0000A0E0 File Offset: 0x000082E0
			internal LiteralToken(string token, string member)
			{
				this.Token = token;
				this.Member = member;
			}

			// Token: 0x04000090 RID: 144
			internal static readonly IList<SqlMapper.LiteralToken> None = new SqlMapper.LiteralToken[0];
		}

		// Token: 0x02000032 RID: 50
		public static class Settings
		{
			// Token: 0x060001C1 RID: 449 RVA: 0x0000A0FD File Offset: 0x000082FD
			static Settings()
			{
				SqlMapper.Settings.SetDefaults();
			}

			// Token: 0x060001C2 RID: 450 RVA: 0x0000A10C File Offset: 0x0000830C
			public static void SetDefaults()
			{
				SqlMapper.Settings.CommandTimeout = null;
				SqlMapper.Settings.ApplyNullValues = false;
			}

			// Token: 0x17000047 RID: 71
			// (get) Token: 0x060001C3 RID: 451 RVA: 0x0000A12D File Offset: 0x0000832D
			// (set) Token: 0x060001C4 RID: 452 RVA: 0x0000A134 File Offset: 0x00008334
			public static int? CommandTimeout { get; set; }

			// Token: 0x17000048 RID: 72
			// (get) Token: 0x060001C5 RID: 453 RVA: 0x0000A13C File Offset: 0x0000833C
			// (set) Token: 0x060001C6 RID: 454 RVA: 0x0000A143 File Offset: 0x00008343
			public static bool ApplyNullValues { get; set; }

			// Token: 0x17000049 RID: 73
			// (get) Token: 0x060001C7 RID: 455 RVA: 0x0000A14B File Offset: 0x0000834B
			// (set) Token: 0x060001C8 RID: 456 RVA: 0x0000A152 File Offset: 0x00008352
			public static bool PadListExpansions { get; set; }

			// Token: 0x1700004A RID: 74
			// (get) Token: 0x060001C9 RID: 457 RVA: 0x0000A15A File Offset: 0x0000835A
			// (set) Token: 0x060001CA RID: 458 RVA: 0x0000A161 File Offset: 0x00008361
			public static int InListStringSplitCount { get; set; } = -1;
		}

		// Token: 0x02000033 RID: 51
		private class TypeDeserializerCache
		{
			// Token: 0x060001CB RID: 459 RVA: 0x0000A169 File Offset: 0x00008369
			private TypeDeserializerCache(Type type)
			{
				this.type = type;
			}

			// Token: 0x060001CC RID: 460 RVA: 0x0000A184 File Offset: 0x00008384
			internal static void Purge(Type type)
			{
				Hashtable obj = SqlMapper.TypeDeserializerCache.byType;
				lock (obj)
				{
					SqlMapper.TypeDeserializerCache.byType.Remove(type);
				}
			}

			// Token: 0x060001CD RID: 461 RVA: 0x0000A1C8 File Offset: 0x000083C8
			internal static void Purge()
			{
				Hashtable obj = SqlMapper.TypeDeserializerCache.byType;
				lock (obj)
				{
					SqlMapper.TypeDeserializerCache.byType.Clear();
				}
			}

			// Token: 0x060001CE RID: 462 RVA: 0x0000A20C File Offset: 0x0000840C
			internal static Func<IDataReader, object> GetReader(Type type, IDataReader reader, int startBound, int length, bool returnNullIfFirstMissing)
			{
				SqlMapper.TypeDeserializerCache typeDeserializerCache = (SqlMapper.TypeDeserializerCache)SqlMapper.TypeDeserializerCache.byType[type];
				if (typeDeserializerCache == null)
				{
					Hashtable obj = SqlMapper.TypeDeserializerCache.byType;
					lock (obj)
					{
						typeDeserializerCache = (SqlMapper.TypeDeserializerCache)SqlMapper.TypeDeserializerCache.byType[type];
						if (typeDeserializerCache == null)
						{
							typeDeserializerCache = (TypeDeserializerCache) (SqlMapper.TypeDeserializerCache.byType[type] = new SqlMapper.TypeDeserializerCache(type));
						}
					}
				}
				return typeDeserializerCache.GetReader(reader, startBound, length, returnNullIfFirstMissing);
			}

			// Token: 0x060001CF RID: 463 RVA: 0x0000A28C File Offset: 0x0000848C
			private Func<IDataReader, object> GetReader(IDataReader reader, int startBound, int length, bool returnNullIfFirstMissing)
			{
				if (length < 0)
				{
					length = reader.FieldCount - startBound;
				}
				int num = SqlMapper.GetColumnHash(reader, startBound, length);
				if (returnNullIfFirstMissing)
				{
					num *= -27;
				}
				SqlMapper.TypeDeserializerCache.DeserializerKey key = new SqlMapper.TypeDeserializerCache.DeserializerKey(num, startBound, length, returnNullIfFirstMissing, reader, false);
				Dictionary<SqlMapper.TypeDeserializerCache.DeserializerKey, Func<IDataReader, object>> obj = this.readers;
				Func<IDataReader, object> typeDeserializerImpl;
				lock (obj)
				{
					if (this.readers.TryGetValue(key, out typeDeserializerImpl))
					{
						return typeDeserializerImpl;
					}
				}
				typeDeserializerImpl = SqlMapper.GetTypeDeserializerImpl(this.type, reader, startBound, length, returnNullIfFirstMissing);
				key = new SqlMapper.TypeDeserializerCache.DeserializerKey(num, startBound, length, returnNullIfFirstMissing, reader, true);
				obj = this.readers;
				Func<IDataReader, object> result;
				lock (obj)
				{
					result = (this.readers[key] = typeDeserializerImpl);
				}
				return result;
			}

			// Token: 0x04000095 RID: 149
			private static readonly Hashtable byType = new Hashtable();

			// Token: 0x04000096 RID: 150
			private readonly Type type;

			// Token: 0x04000097 RID: 151
			private Dictionary<SqlMapper.TypeDeserializerCache.DeserializerKey, Func<IDataReader, object>> readers = new Dictionary<SqlMapper.TypeDeserializerCache.DeserializerKey, Func<IDataReader, object>>();

			// Token: 0x02000053 RID: 83
			private struct DeserializerKey : IEquatable<SqlMapper.TypeDeserializerCache.DeserializerKey>
			{
				// Token: 0x06000279 RID: 633 RVA: 0x0000C628 File Offset: 0x0000A828
				public DeserializerKey(int hashCode, int startBound, int length, bool returnNullIfFirstMissing, IDataReader reader, bool copyDown)
				{
					this.hashCode = hashCode;
					this.startBound = startBound;
					this.length = length;
					this.returnNullIfFirstMissing = returnNullIfFirstMissing;
					if (copyDown)
					{
						this.reader = null;
						this.names = new string[length];
						this.types = new Type[length];
						int i = startBound;
						for (int j = 0; j < length; j++)
						{
							this.names[j] = reader.GetName(i);
							this.types[j] = reader.GetFieldType(i++);
						}
						return;
					}
					this.reader = reader;
					this.names = null;
					this.types = null;
				}

				// Token: 0x0600027A RID: 634 RVA: 0x0000C6BE File Offset: 0x0000A8BE
				public override int GetHashCode()
				{
					return this.hashCode;
				}

				// Token: 0x0600027B RID: 635 RVA: 0x0000C6C8 File Offset: 0x0000A8C8
				public override string ToString()
				{
					if (this.names != null)
					{
						return string.Join(", ", this.names);
					}
					if (this.reader != null)
					{
						StringBuilder stringBuilder = new StringBuilder();
						int num = this.startBound;
						for (int i = 0; i < this.length; i++)
						{
							if (i != 0)
							{
								stringBuilder.Append(", ");
							}
							stringBuilder.Append(this.reader.GetName(num++));
						}
						return stringBuilder.ToString();
					}
					return base.ToString();
				}

				// Token: 0x0600027C RID: 636 RVA: 0x0000C751 File Offset: 0x0000A951
				public override bool Equals(object obj)
				{
					return obj is SqlMapper.TypeDeserializerCache.DeserializerKey && this.Equals((SqlMapper.TypeDeserializerCache.DeserializerKey)obj);
				}

				// Token: 0x0600027D RID: 637 RVA: 0x0000C76C File Offset: 0x0000A96C
				public bool Equals(SqlMapper.TypeDeserializerCache.DeserializerKey other)
				{
					if (this.hashCode != other.hashCode || this.startBound != other.startBound || this.length != other.length || this.returnNullIfFirstMissing != other.returnNullIfFirstMissing)
					{
						return false;
					}
					int i = 0;
					while (i < this.length)
					{
						string[] array = this.names;
						string a;
						if ((a = ((array != null) ? array[i] : null)) == null)
						{
							IDataReader dataReader = this.reader;
							a = ((dataReader != null) ? dataReader.GetName(this.startBound + i) : null);
						}
						string[] array2 = other.names;
						string b;
						if ((b = ((array2 != null) ? array2[i] : null)) == null)
						{
							IDataReader dataReader2 = other.reader;
							b = ((dataReader2 != null) ? dataReader2.GetName(this.startBound + i) : null);
						}
						if (!(a != b))
						{
							Type[] array3 = this.types;
							Type left;
							if ((left = ((array3 != null) ? array3[i] : null)) == null)
							{
								IDataReader dataReader3 = this.reader;
								left = ((dataReader3 != null) ? dataReader3.GetFieldType(this.startBound + i) : null);
							}
							Type[] array4 = other.types;
							Type right;
							if ((right = ((array4 != null) ? array4[i] : null)) == null)
							{
								IDataReader dataReader4 = other.reader;
								right = ((dataReader4 != null) ? dataReader4.GetFieldType(this.startBound + i) : null);
							}
							if (!(left != right))
							{
								i++;
								continue;
							}
						}
						return false;
					}
					return true;
				}

				// Token: 0x04000141 RID: 321
				private readonly int startBound;

				// Token: 0x04000142 RID: 322
				private readonly int length;

				// Token: 0x04000143 RID: 323
				private readonly bool returnNullIfFirstMissing;

				// Token: 0x04000144 RID: 324
				private readonly IDataReader reader;

				// Token: 0x04000145 RID: 325
				private readonly string[] names;

				// Token: 0x04000146 RID: 326
				private readonly Type[] types;

				// Token: 0x04000147 RID: 327
				private readonly int hashCode;
			}
		}

		// Token: 0x02000034 RID: 52
		public abstract class TypeHandler<T> : SqlMapper.ITypeHandler
		{
			// Token: 0x060001D1 RID: 465
			public abstract void SetValue(IDbDataParameter parameter, T value);

			// Token: 0x060001D2 RID: 466
			public abstract T Parse(object value);

			// Token: 0x060001D3 RID: 467 RVA: 0x0000A378 File Offset: 0x00008578
			void SqlMapper.ITypeHandler.SetValue(IDbDataParameter parameter, object value)
			{
				if (value is DBNull)
				{
					parameter.Value = value;
					return;
				}
				this.SetValue(parameter, (T)((object)value));
			}

			// Token: 0x060001D4 RID: 468 RVA: 0x0000A397 File Offset: 0x00008597
			object SqlMapper.ITypeHandler.Parse(Type destinationType, object value)
			{
				return this.Parse(value);
			}
		}

		// Token: 0x02000035 RID: 53
		public abstract class StringTypeHandler<T> : SqlMapper.TypeHandler<T>
		{
			// Token: 0x060001D6 RID: 470
			protected abstract T Parse(string xml);

			// Token: 0x060001D7 RID: 471
			protected abstract string Format(T xml);

			// Token: 0x060001D8 RID: 472 RVA: 0x0000A3A5 File Offset: 0x000085A5
			public override void SetValue(IDbDataParameter parameter, T value)
			{
				parameter.Value = ((value == null) ? (object) DBNull.Value : this.Format(value));
			}

			// Token: 0x060001D9 RID: 473 RVA: 0x0000A3C4 File Offset: 0x000085C4
			public override T Parse(object value)
			{
				if (value == null || value is DBNull)
				{
					return default(T);
				}
				return this.Parse((string)value);
			}
		}

		// Token: 0x02000036 RID: 54
		[Obsolete("This method is for internal use only", false)]
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static class TypeHandlerCache<T>
		{
			// Token: 0x060001DB RID: 475 RVA: 0x0000A3FA File Offset: 0x000085FA
			[Obsolete("This method is for internal use only", true)]
			public static T Parse(object value)
			{
				return (T)((object)SqlMapper.TypeHandlerCache<T>.handler.Parse(typeof(T), value));
			}

			// Token: 0x060001DC RID: 476 RVA: 0x0000A416 File Offset: 0x00008616
			[Obsolete("This method is for internal use only", true)]
			public static void SetValue(IDbDataParameter parameter, object value)
			{
				SqlMapper.TypeHandlerCache<T>.handler.SetValue(parameter, value);
			}

			// Token: 0x060001DD RID: 477 RVA: 0x0000A424 File Offset: 0x00008624
			internal static void SetHandler(SqlMapper.ITypeHandler handler)
			{
				SqlMapper.TypeHandlerCache<T>.handler = handler;
			}

			// Token: 0x04000098 RID: 152
			private static SqlMapper.ITypeHandler handler;
		}

		// Token: 0x02000037 RID: 55
		public class UdtTypeHandler : SqlMapper.ITypeHandler
		{
			// Token: 0x060001DE RID: 478 RVA: 0x0000A42C File Offset: 0x0000862C
			public UdtTypeHandler(string udtTypeName)
			{
				if (string.IsNullOrEmpty(udtTypeName))
				{
					throw new ArgumentException("Cannot be null or empty", udtTypeName);
				}
				this.udtTypeName = udtTypeName;
			}

			// Token: 0x060001DF RID: 479 RVA: 0x0000A44F File Offset: 0x0000864F
			object SqlMapper.ITypeHandler.Parse(Type destinationType, object value)
			{
				if (!(value is DBNull))
				{
					return value;
				}
				return null;
			}

			// Token: 0x060001E0 RID: 480 RVA: 0x0000A45C File Offset: 0x0000865C
			void SqlMapper.ITypeHandler.SetValue(IDbDataParameter parameter, object value)
			{
				parameter.Value = SqlMapper.SanitizeParameterValue(value);
				if (parameter is SqlParameter && !(value is DBNull))
				{
					((SqlParameter)parameter).SqlDbType = SqlDbType.Udt;
					((SqlParameter)parameter).UdtTypeName = this.udtTypeName;
				}
			}

			// Token: 0x04000099 RID: 153
			private readonly string udtTypeName;
		}
	}
}
