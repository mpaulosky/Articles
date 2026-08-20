using System.Reflection;

using FluentValidation;

namespace Web.MyMediator;

public static class ServiceCollectionExtensions
{
	/// <summary>
	///     Registers <see cref="IMediator" /> and scans <paramref name="assembly" /> for request handlers,
	///     notification handlers, validators, and application-defined pipeline behaviors.
	/// </summary>
	/// <remarks>
	///     Built-in behaviors under <see cref="Web.MyMediator.Behaviors" /> are intentionally excluded from the
	///     pipeline-behavior scan so they stay opt-in; register them explicitly (see Program.cs) rather than have
	///     every request silently pick them up.
	/// </remarks>
	public static IServiceCollection AddMyMediator(this IServiceCollection services, Assembly assembly)
	{
		services.AddScoped<IMediator, PipelineMediator>();

		var types = assembly
			.GetTypes()
			.Where(type => type is { IsAbstract: false, IsInterface: false })
			.ToList();

		RegisterImplementations(services, types, typeof(IRequestHandler<,>));
		RegisterImplementations(services, types, typeof(INotificationHandler<>));
		RegisterImplementations(services, types, typeof(IValidator<>));
		RegisterImplementations(services, types, typeof(IPipelineBehavior<,>), skipNamespace: BuiltInBehaviorsNamespace);

		return services;
	}

	private static readonly string BuiltInBehaviorsNamespace = $"{typeof(ServiceCollectionExtensions).Namespace}.Behaviors";

	private static void RegisterImplementations(
		IServiceCollection services,
		IEnumerable<Type> types,
		Type openInterfaceType,
		string? skipNamespace = null)
	{
		foreach (var implementationType in types)
		{
			if (skipNamespace is not null && implementationType.Namespace == skipNamespace)
			{
				continue;
			}

			var matchingInterfaces = implementationType.GetInterfaces()
				.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openInterfaceType);

			foreach (var matchingInterface in matchingInterfaces)
			{
				services.AddScoped(matchingInterface, implementationType);
			}
		}
	}
}
