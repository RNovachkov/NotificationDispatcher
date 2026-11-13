using APNCore;
using FCMCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NotificationCore;
using NotificationDispatcher;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency-injection extensions for registering the unified notification sender.
/// </summary>
public static class NotificationServiceCollectionExtensions
{
    /// <summary>
    /// Registers an <see cref="INotificationSender"/> that routes to whichever provider clients are
    /// present in the container. Call this when you register the providers yourself (for example with
    /// <c>AddApns(...)</c> and/or <c>AddFcm(...)</c>); the dispatcher resolves <see cref="IApnClient"/>
    /// and <see cref="IFcmClient"/> optionally, so registering only one is fine.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
    public static IServiceCollection AddNotifications(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<INotificationSender>(sp => new NotificationDispatcher.NotificationDispatcher(
            sp.GetService<IApnClient>(),
            sp.GetService<IFcmClient>()));

        return services;
    }

    /// <summary>
    /// Registers both the provider(s) and the unified <see cref="INotificationSender"/> in one call.
    /// Configure the platforms your app targets through the builder; each one you configure is wired up
    /// (via the underlying <c>AddApns</c> / <c>AddFcm</c>) and the dispatcher routes to it automatically.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configure">Selects and configures the providers (<c>UseApns</c>, <c>UseFcm</c>).</param>
    /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
    /// <exception cref="ArgumentException">When the builder configured no provider.</exception>
    public static IServiceCollection AddNotifications(this IServiceCollection services, Action<NotificationBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new NotificationBuilder();
        configure(builder);

        if (builder.ApnsConfigure is null && builder.FcmConfigure is null)
            throw new ArgumentException(
                "AddNotifications configured no provider: call UseApns and/or UseFcm on the builder.",
                nameof(configure));

        if (builder.ApnsConfigure is not null)
            services.AddApns(builder.ApnsConfigure);

        if (builder.FcmConfigure is not null)
            services.AddFcm(builder.FcmConfigure);

        return services.AddNotifications();
    }
}
