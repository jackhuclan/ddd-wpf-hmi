using CommunityToolkit.Mvvm.Messaging;
using Hmi.Application.Messages;

namespace Hmi.Infrastructure.Messaging;

/// <summary>
/// 把领域事件处理器发布的 UI 消息转发到 CommunityToolkit Messenger。
/// 领域处理器只依赖 IUiEventPublisher，不反向依赖 UI 框架。
/// </summary>
public sealed class UiEventPublisher(IMessenger messenger) : IUiEventPublisher
{
    public const string Token = "hmi-ui";

    public void Publish<T>(T message) where T : class => messenger.Send(message, Token);
}
