using System;

namespace HousingFunctions
{
    public interface IDynamicTableColumn
    {
        string Name { get; }
        Type ValueType { get; }
        object DefaultValue { get; }
    }
}
