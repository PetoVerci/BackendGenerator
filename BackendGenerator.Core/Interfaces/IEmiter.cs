using BackendGenerator.Core.Models.hyml;
using BackendGenerator.Core.Models.Schema;
using FluentResults;

namespace BackendGenerator.Core.Interfaces;

public interface IEmiter
{
    string Emit(TableRegistry tableRegistry);
}
