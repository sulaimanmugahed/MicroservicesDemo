using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Play.Common.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Play.Common.MassTransit;
public static class Extensions
{
	public static IServiceCollection AddMassTransitWithRabbitMQ(this IServiceCollection services)
	{
		services.AddMassTransit(options =>
		{

			options.AddConsumers(Assembly.GetEntryAssembly());
			options.UsingRabbitMq((context, configurator) =>
			{
				var configration = context.GetService<IConfiguration>();
				var serviceSettings = configration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
				var rabbitMQSettings = configration.GetSection(nameof(RabbitMQSettings)).Get<RabbitMQSettings>();
				configurator.Host(rabbitMQSettings.Host);
				configurator.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter(serviceSettings.ServiceName, false));
				configurator.UseMessageRetry(retryConfig =>
				{
					retryConfig.Interval(3,TimeSpan.FromSeconds(5));
				});
			});
		});

		return services;
	}
}
