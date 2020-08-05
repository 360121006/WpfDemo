using System;
using System.Xml.Linq;

namespace Dapper
{
	// Token: 0x02000016 RID: 22
	internal sealed class XElementHandler : XmlTypeHandler<XElement>
	{
		// Token: 0x06000116 RID: 278 RVA: 0x00008D37 File Offset: 0x00006F37
		protected override XElement Parse(string xml)
		{
			return XElement.Parse(xml);
		}

		// Token: 0x06000117 RID: 279 RVA: 0x00008D27 File Offset: 0x00006F27
		protected override string Format(XElement xml)
		{
			return xml.ToString();
		}
	}
}
