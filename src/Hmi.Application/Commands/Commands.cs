using Hmi.Domain.Alarms;
using MediatR;

namespace Hmi.Application.Commands;

public sealed record CreateWorkOrderCommand(string? Code) : IRequest<string>;

public sealed record AssignWorkOrderCommand(string Code, Guid DeviceId, string? RecipeCode) : IRequest<string>;

public sealed record StartWorkOrderCommand(string Code) : IRequest<string>;

public sealed record CompleteWorkOrderCommand(string Code) : IRequest<string>;

public sealed record ReloadConfigCommand : IRequest<string>;

public sealed record SwitchRecipeCommand(string Code) : IRequest<string>;

public sealed record AcknowledgeAlarmCommand(Guid Id) : IRequest<string>;

public sealed record ClearAlarmCommand(Guid Id) : IRequest<string>;

public sealed record AlarmQuery(
    string Level,
    string State,
    DateTime? From,
    DateTime? To,
    int PageIndex,
    int PageSize);

public sealed record AlarmQueryResult(
    IReadOnlyList<Alarm> Items,
    int TotalCount,
    int PageCount,
    int PageIndex);

public sealed record QueryAlarmsQuery(AlarmQuery Query) : IRequest<AlarmQueryResult>;
