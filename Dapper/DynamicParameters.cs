using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace Dapper
{
	// Token: 0x02000008 RID: 8
	public class DynamicParameters : SqlMapper.IDynamicParameters, SqlMapper.IParameterLookup, SqlMapper.IParameterCallbacks
	{
		// Token: 0x17000011 RID: 17
		object SqlMapper.IParameterLookup.this[string member]
		{
			get
			{
				DynamicParameters.ParamInfo paramInfo;
				if (!this.parameters.TryGetValue(member, out paramInfo))
				{
					return null;
				}
				return paramInfo.Value;
			}
		}

		// Token: 0x06000031 RID: 49 RVA: 0x00002ACD File Offset: 0x00000CCD
		public DynamicParameters()
		{
			this.RemoveUnused = true;
		}

		// Token: 0x06000032 RID: 50 RVA: 0x00002AE7 File Offset: 0x00000CE7
		public DynamicParameters(object template)
		{
			this.RemoveUnused = true;
			this.AddDynamicParams(template);
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00002B08 File Offset: 0x00000D08
		public void AddDynamicParams(object param)
		{
			if (param != null)
			{
				DynamicParameters dynamicParameters = param as DynamicParameters;
				if (dynamicParameters == null)
				{
					IEnumerable<KeyValuePair<string, object>> enumerable = param as IEnumerable<KeyValuePair<string, object>>;
					if (enumerable == null)
					{
						this.templates = (this.templates ?? new List<object>());
						this.templates.Add(param);
						return;
					}
					using (IEnumerator<KeyValuePair<string, object>> enumerator = enumerable.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							KeyValuePair<string, object> keyValuePair = enumerator.Current;
							this.Add(keyValuePair.Key, keyValuePair.Value, null, null, null);
						}
						return;
					}
				}
				if (dynamicParameters.parameters != null)
				{
					foreach (KeyValuePair<string, DynamicParameters.ParamInfo> keyValuePair2 in dynamicParameters.parameters)
					{
						this.parameters.Add(keyValuePair2.Key, keyValuePair2.Value);
					}
				}
				if (dynamicParameters.templates != null)
				{
					this.templates = (this.templates ?? new List<object>());
					foreach (object item in dynamicParameters.templates)
					{
						this.templates.Add(item);
					}
				}
			}
		}

		// Token: 0x06000034 RID: 52 RVA: 0x00002C88 File Offset: 0x00000E88
		public void Add(string name, object value, DbType? dbType, ParameterDirection? direction, int? size)
		{
			this.parameters[DynamicParameters.Clean(name)] = new DynamicParameters.ParamInfo
			{
				Name = name,
				Value = value,
				ParameterDirection = (direction ?? ParameterDirection.Input),
				DbType = dbType,
				Size = size
			};
		}

		// Token: 0x06000035 RID: 53 RVA: 0x00002CE4 File Offset: 0x00000EE4
		public void Add(string name, object value = null, DbType? dbType = null, ParameterDirection? direction = null, int? size = null, byte? precision = null, byte? scale = null)
		{
			this.parameters[DynamicParameters.Clean(name)] = new DynamicParameters.ParamInfo
			{
				Name = name,
				Value = value,
				ParameterDirection = (direction ?? ParameterDirection.Input),
				DbType = dbType,
				Size = size,
				Precision = precision,
				Scale = scale
			};
		}

		// Token: 0x06000036 RID: 54 RVA: 0x00002D50 File Offset: 0x00000F50
		private static string Clean(string name)
		{
			if (!string.IsNullOrEmpty(name))
			{
				char c = name[0];
				if (c == ':' || c == '?' || c == '@')
				{
					return name.Substring(1);
				}
			}
			return name;
		}

		// Token: 0x06000037 RID: 55 RVA: 0x00002D85 File Offset: 0x00000F85
		void SqlMapper.IDynamicParameters.AddParameters(IDbCommand command, SqlMapper.Identity identity)
		{
			this.AddParameters(command, identity);
		}

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x06000038 RID: 56 RVA: 0x00002D8F File Offset: 0x00000F8F
		// (set) Token: 0x06000039 RID: 57 RVA: 0x00002D97 File Offset: 0x00000F97
		public bool RemoveUnused { get; set; }

		// Token: 0x0600003A RID: 58 RVA: 0x00002DA0 File Offset: 0x00000FA0
		protected void AddParameters(IDbCommand command, SqlMapper.Identity identity)
		{
			IList<SqlMapper.LiteralToken> literalTokens = SqlMapper.GetLiteralTokens(identity.sql);
			if (this.templates != null)
			{
				foreach (object obj in this.templates)
				{
					SqlMapper.Identity identity2 = identity.ForDynamicParameters(obj.GetType());
					Dictionary<SqlMapper.Identity, Action<IDbCommand, object>> obj2 = DynamicParameters.paramReaderCache;
					Action<IDbCommand, object> action;
					lock (obj2)
					{
						if (!DynamicParameters.paramReaderCache.TryGetValue(identity2, out action))
						{
							action = SqlMapper.CreateParamInfoGenerator(identity2, true, this.RemoveUnused, literalTokens);
							DynamicParameters.paramReaderCache[identity2] = action;
						}
					}
					action(command, obj);
				}
				foreach (object obj3 in command.Parameters)
				{
					IDbDataParameter dbDataParameter = (IDbDataParameter)obj3;
					if (!this.parameters.ContainsKey(dbDataParameter.ParameterName))
					{
						this.parameters.Add(dbDataParameter.ParameterName, new DynamicParameters.ParamInfo
						{
							AttachedParam = dbDataParameter,
							CameFromTemplate = true,
							DbType = new DbType?(dbDataParameter.DbType),
							Name = dbDataParameter.ParameterName,
							ParameterDirection = dbDataParameter.Direction,
							Size = new int?(dbDataParameter.Size),
							Value = dbDataParameter.Value
						});
					}
				}
				List<Action> list = this.outputCallbacks;
				if (list != null)
				{
					foreach (Action action2 in list)
					{
						action2();
					}
				}
			}
			foreach (DynamicParameters.ParamInfo paramInfo in this.parameters.Values)
			{
				if (!paramInfo.CameFromTemplate)
				{
					DbType? dbType = paramInfo.DbType;
					object value = paramInfo.Value;
					string text = DynamicParameters.Clean(paramInfo.Name);
					bool flag2 = value is SqlMapper.ICustomQueryParameter;
					SqlMapper.ITypeHandler typeHandler = null;
					if (dbType == null && value != null && !flag2)
					{
						dbType = new DbType?(SqlMapper.LookupDbType(value.GetType(), text, true, out typeHandler));
					}
					if (flag2)
					{
						((SqlMapper.ICustomQueryParameter)value).AddParameter(command, text);
					}
					else if (dbType == (DbType)(-1))
					{
						SqlMapper.PackListParameters(command, text, value);
					}
					else
					{
						bool flag3 = !command.Parameters.Contains(text);
						IDbDataParameter dbDataParameter2;
						if (flag3)
						{
							dbDataParameter2 = command.CreateParameter();
							dbDataParameter2.ParameterName = text;
						}
						else
						{
							dbDataParameter2 = (IDbDataParameter)command.Parameters[text];
						}
						dbDataParameter2.Direction = paramInfo.ParameterDirection;
						if (typeHandler == null)
						{
							dbDataParameter2.Value = SqlMapper.SanitizeParameterValue(value);
							if (dbType != null && dbDataParameter2.DbType != dbType)
							{
								dbDataParameter2.DbType = dbType.Value;
							}
							string text2 = value as string;
							if (text2 != null && text2.Length <= 4000)
							{
								dbDataParameter2.Size = 4000;
							}
							if (paramInfo.Size != null)
							{
								dbDataParameter2.Size = paramInfo.Size.Value;
							}
							if (paramInfo.Precision != null)
							{
								dbDataParameter2.Precision = paramInfo.Precision.Value;
							}
							if (paramInfo.Scale != null)
							{
								dbDataParameter2.Scale = paramInfo.Scale.Value;
							}
						}
						else
						{
							if (dbType != null)
							{
								dbDataParameter2.DbType = dbType.Value;
							}
							if (paramInfo.Size != null)
							{
								dbDataParameter2.Size = paramInfo.Size.Value;
							}
							if (paramInfo.Precision != null)
							{
								dbDataParameter2.Precision = paramInfo.Precision.Value;
							}
							if (paramInfo.Scale != null)
							{
								dbDataParameter2.Scale = paramInfo.Scale.Value;
							}
							typeHandler.SetValue(dbDataParameter2, value ?? DBNull.Value);
						}
						if (flag3)
						{
							command.Parameters.Add(dbDataParameter2);
						}
						paramInfo.AttachedParam = dbDataParameter2;
					}
				}
			}
			if (literalTokens.Count != 0)
			{
				SqlMapper.ReplaceLiterals(this, command, literalTokens);
			}
		}

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x0600003B RID: 59 RVA: 0x000032D0 File Offset: 0x000014D0
		public IEnumerable<string> ParameterNames
		{
			get
			{
				return from p in this.parameters
				select p.Key;
			}
		}

		// Token: 0x0600003C RID: 60 RVA: 0x000032FC File Offset: 0x000014FC
		public T Get<T>(string name)
		{
			DynamicParameters.ParamInfo paramInfo = this.parameters[DynamicParameters.Clean(name)];
			IDbDataParameter attachedParam = paramInfo.AttachedParam;
			object obj = (attachedParam == null) ? paramInfo.Value : attachedParam.Value;
			if (obj != DBNull.Value)
			{
				return (T)((object)obj);
			}
			if (default(T) != null)
			{
				throw new ApplicationException("Attempting to cast a DBNull to a non nullable type! Note that out/return parameters will not have updated values until the data stream completes (after the 'foreach' for Query(..., buffered: false), or after the GridReader has been disposed for QueryMultiple)");
			}
			return default(T);
		}

		// Token: 0x0600003D RID: 61 RVA: 0x00003368 File Offset: 0x00001568
		public DynamicParameters Output<T>(T target, Expression<Func<T, object>> expression, DbType? dbType = null, int? size = null)
		{
			string failMessage = "Expression must be a property/field chain off of a(n) {0} instance";
			failMessage = string.Format(failMessage, typeof(T).Name);
			Action action = delegate()
			{
				throw new InvalidOperationException(failMessage);
			};
			MemberExpression lastMemberAccess = expression.Body as MemberExpression;
			if (lastMemberAccess == null || (!(lastMemberAccess.Member is PropertyInfo) && !(lastMemberAccess.Member is FieldInfo)))
			{
				if (expression.Body.NodeType == ExpressionType.Convert && expression.Body.Type == typeof(object) && ((UnaryExpression)expression.Body).Operand is MemberExpression)
				{
					lastMemberAccess = (MemberExpression)((UnaryExpression)expression.Body).Operand;
				}
				else
				{
					action();
				}
			}
			MemberExpression memberExpression = lastMemberAccess;
			List<string> list = new List<string>();
			List<MemberExpression> list2 = new List<MemberExpression>();
			do
			{
				list.Insert(0, (memberExpression != null) ? memberExpression.Member.Name : null);
				list2.Insert(0, memberExpression);
				ParameterExpression parameterExpression = ((memberExpression != null) ? memberExpression.Expression : null) as ParameterExpression;
				memberExpression = (((memberExpression != null) ? memberExpression.Expression : null) as MemberExpression);
				if (parameterExpression != null && parameterExpression.Type == typeof(T))
				{
					break;
				}
				if (memberExpression == null || (!(memberExpression.Member is PropertyInfo) && !(memberExpression.Member is FieldInfo)))
				{
					action();
				}
			}
			while (memberExpression != null);
			string dynamicParamName = string.Join(string.Empty, list.ToArray());
			string key = string.Join("|", list.ToArray());
			Hashtable cache = DynamicParameters.CachedOutputSetters<T>.Cache;
			Action<object, DynamicParameters> setter = (Action<object, DynamicParameters>)cache[key];
			if (setter == null)
			{
				DynamicMethod dynamicMethod = new DynamicMethod("ExpressionParam" + Guid.NewGuid().ToString(), null, new Type[]
				{
					typeof(object),
					base.GetType()
				}, true);
				ILGenerator ilgenerator = dynamicMethod.GetILGenerator();
				ilgenerator.Emit(OpCodes.Ldarg_0);
				ilgenerator.Emit(OpCodes.Castclass, typeof(T));
				for (int i = 0; i < list2.Count - 1; i++)
				{
					MemberInfo member = list2[0].Member;
					if (member is PropertyInfo)
					{
						MethodInfo getMethod = ((PropertyInfo)member).GetGetMethod(true);
						ilgenerator.Emit(OpCodes.Callvirt, getMethod);
					}
					else
					{
						ilgenerator.Emit(OpCodes.Ldfld, (FieldInfo)member);
					}
				}
				MethodInfo meth = base.GetType().GetMethod("Get", new Type[]
				{
					typeof(string)
				}).MakeGenericMethod(new Type[]
				{
					lastMemberAccess.Type
				});
				ilgenerator.Emit(OpCodes.Ldarg_1);
				ilgenerator.Emit(OpCodes.Ldstr, dynamicParamName);
				ilgenerator.Emit(OpCodes.Callvirt, meth);
				MemberInfo member2 = lastMemberAccess.Member;
				if (member2 is PropertyInfo)
				{
					MethodInfo setMethod = ((PropertyInfo)member2).GetSetMethod(true);
					ilgenerator.Emit(OpCodes.Callvirt, setMethod);
				}
				else
				{
					ilgenerator.Emit(OpCodes.Stfld, (FieldInfo)member2);
				}
				ilgenerator.Emit(OpCodes.Ret);
				setter = (Action<object, DynamicParameters>)dynamicMethod.CreateDelegate(typeof(Action<object, DynamicParameters>));
				Hashtable obj = cache;
				lock (obj)
				{
					cache[key] = setter;
				}
			}
			List<Action> list3;
			if ((list3 = this.outputCallbacks) == null)
			{
				list3 = (this.outputCallbacks = new List<Action>());
			}
			list3.Add(delegate
			{
				MemberExpression lastMemberAccess1 = lastMemberAccess;
				Type type = (lastMemberAccess1 != null) ? lastMemberAccess1.Type : null;
				int num = (size == null && type == typeof(string)) ? 4000 : (size ?? 0);
				DynamicParameters.ParamInfo paramInfo;
				if (this.parameters.TryGetValue(dynamicParamName, out paramInfo))
				{
					paramInfo.ParameterDirection = (paramInfo.AttachedParam.Direction = ParameterDirection.InputOutput);
					if (paramInfo.AttachedParam.Size == 0)
					{
						paramInfo.Size = new int?(paramInfo.AttachedParam.Size = num);
					}
				}
				else
				{
					SqlMapper.ITypeHandler typeHandler;
					dbType = ((dbType == null) ? new DbType?(SqlMapper.LookupDbType(type, (type != null) ? type.Name : null, true, out typeHandler)) : dbType);
					this.Add(dynamicParamName, expression.Compile()(target), null, new ParameterDirection?(ParameterDirection.InputOutput), new int?(num));
				}
				paramInfo = this.parameters[dynamicParamName];
				paramInfo.OutputCallback = setter;
				paramInfo.OutputTarget = target;
			});
			return this;
		}

		// Token: 0x0600003E RID: 62 RVA: 0x00003798 File Offset: 0x00001998
		void SqlMapper.IParameterCallbacks.OnCompleted()
		{
			foreach (DynamicParameters.ParamInfo paramInfo in from p in this.parameters
			select p.Value)
			{
				Action<object, DynamicParameters> outputCallback = paramInfo.OutputCallback;
				if (outputCallback != null)
				{
					outputCallback(paramInfo.OutputTarget, this);
				}
			}
		}

		// Token: 0x04000019 RID: 25
		internal const DbType EnumerableMultiParameter = (DbType)(-1);

		// Token: 0x0400001A RID: 26
		private static Dictionary<SqlMapper.Identity, Action<IDbCommand, object>> paramReaderCache = new Dictionary<SqlMapper.Identity, Action<IDbCommand, object>>();

		// Token: 0x0400001B RID: 27
		private Dictionary<string, DynamicParameters.ParamInfo> parameters = new Dictionary<string, DynamicParameters.ParamInfo>();

		// Token: 0x0400001C RID: 28
		private List<object> templates;

		// Token: 0x0400001E RID: 30
		private List<Action> outputCallbacks;

		// Token: 0x0200001C RID: 28
		internal static class CachedOutputSetters<T>
		{
			// Token: 0x04000053 RID: 83
			public static readonly Hashtable Cache = new Hashtable();
		}

		// Token: 0x0200001D RID: 29
		private sealed class ParamInfo
		{
			// Token: 0x17000025 RID: 37
			// (get) Token: 0x06000132 RID: 306 RVA: 0x00008E85 File Offset: 0x00007085
			// (set) Token: 0x06000133 RID: 307 RVA: 0x00008E8D File Offset: 0x0000708D
			public string Name { get; set; }

			// Token: 0x17000026 RID: 38
			// (get) Token: 0x06000134 RID: 308 RVA: 0x00008E96 File Offset: 0x00007096
			// (set) Token: 0x06000135 RID: 309 RVA: 0x00008E9E File Offset: 0x0000709E
			public object Value { get; set; }

			// Token: 0x17000027 RID: 39
			// (get) Token: 0x06000136 RID: 310 RVA: 0x00008EA7 File Offset: 0x000070A7
			// (set) Token: 0x06000137 RID: 311 RVA: 0x00008EAF File Offset: 0x000070AF
			public ParameterDirection ParameterDirection { get; set; }

			// Token: 0x17000028 RID: 40
			// (get) Token: 0x06000138 RID: 312 RVA: 0x00008EB8 File Offset: 0x000070B8
			// (set) Token: 0x06000139 RID: 313 RVA: 0x00008EC0 File Offset: 0x000070C0
			public DbType? DbType { get; set; }

			// Token: 0x17000029 RID: 41
			// (get) Token: 0x0600013A RID: 314 RVA: 0x00008EC9 File Offset: 0x000070C9
			// (set) Token: 0x0600013B RID: 315 RVA: 0x00008ED1 File Offset: 0x000070D1
			public int? Size { get; set; }

			// Token: 0x1700002A RID: 42
			// (get) Token: 0x0600013C RID: 316 RVA: 0x00008EDA File Offset: 0x000070DA
			// (set) Token: 0x0600013D RID: 317 RVA: 0x00008EE2 File Offset: 0x000070E2
			public IDbDataParameter AttachedParam { get; set; }

			// Token: 0x1700002B RID: 43
			// (get) Token: 0x0600013E RID: 318 RVA: 0x00008EEB File Offset: 0x000070EB
			// (set) Token: 0x0600013F RID: 319 RVA: 0x00008EF3 File Offset: 0x000070F3
			internal Action<object, DynamicParameters> OutputCallback { get; set; }

			// Token: 0x1700002C RID: 44
			// (get) Token: 0x06000140 RID: 320 RVA: 0x00008EFC File Offset: 0x000070FC
			// (set) Token: 0x06000141 RID: 321 RVA: 0x00008F04 File Offset: 0x00007104
			internal object OutputTarget { get; set; }

			// Token: 0x1700002D RID: 45
			// (get) Token: 0x06000142 RID: 322 RVA: 0x00008F0D File Offset: 0x0000710D
			// (set) Token: 0x06000143 RID: 323 RVA: 0x00008F15 File Offset: 0x00007115
			internal bool CameFromTemplate { get; set; }

			// Token: 0x1700002E RID: 46
			// (get) Token: 0x06000144 RID: 324 RVA: 0x00008F1E File Offset: 0x0000711E
			// (set) Token: 0x06000145 RID: 325 RVA: 0x00008F26 File Offset: 0x00007126
			public byte? Precision { get; set; }

			// Token: 0x1700002F RID: 47
			// (get) Token: 0x06000146 RID: 326 RVA: 0x00008F2F File Offset: 0x0000712F
			// (set) Token: 0x06000147 RID: 327 RVA: 0x00008F37 File Offset: 0x00007137
			public byte? Scale { get; set; }
		}
	}
}
