using System.Reflection;

namespace SearchDescriptor
{
    public class SearchPart
    {
        public PropertyInfo Property { get; set; }
        public Operand Operand { get; set; }
        public object Value { get; set; }
        public override string ToString() => $"{Property.Name } {Operand} \"{Value}\"";
    }
}
