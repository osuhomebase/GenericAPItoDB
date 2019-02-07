using System;

namespace Api2db
{
    public interface IDynamicTableColumn
    {
        string Name { get; }
        Type ValueType { get; }
        object DefaultValue { get; }
    }
}
