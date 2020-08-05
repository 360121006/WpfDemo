using System;
using System.Xml.Linq;

namespace Dapper
{
	// Token: 0x02000015 RID: 21
	internal sealed class XDocumentHandler : XmlTypeHandler<XDocument>
	{
		// Token: 0x06000113 RID: 275 RVA: 0x00008D1F File Offset: 0x00006F1F
		protected override XDocument Parse(string xml)
		{
			return XDocument.Parse(xml);
		}

		// Token: 0x06000114 RID: 276 RVA: 0x00008D27 File Offset: 0x00006F27
		protected override string Format(XDocument xml)
		{
			return xml.ToString();
		}
	}
}
