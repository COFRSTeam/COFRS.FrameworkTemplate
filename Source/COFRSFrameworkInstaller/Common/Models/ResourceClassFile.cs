namespace COFRS.Template.Common.Models
{
    public class ResourceClassFile
	{
		public string ClassName { get; set; }
		public string FileName { get; set; }
		public string EntityClass { get; set; }
		public string ClassNamespace { get; set; }

		public override string ToString()
		{
			return ClassName;
		}
	}
}
