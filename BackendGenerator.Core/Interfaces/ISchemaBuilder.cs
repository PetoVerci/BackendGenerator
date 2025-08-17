using BackendGenerator.Core.Models.hyml;
using BackendGenerator.Core.Models.Schema;

namespace BackendGenerator.Core.Interfaces
{
    public interface ISchemaBuilder
    {
        TableRegistry Build(HymlModel m);
    }
}
