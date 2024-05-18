using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Play.Common.Settings;


namespace Play.Common.MongoDb;

public static class Extensions
{
	public static IServiceCollection AddMongo(this IServiceCollection services)
	{
		BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
		BsonSerializer.RegisterSerializer(new DateTimeOffsetSerializer(BsonType.String));

		services.AddSingleton((serviceProvider) =>
		{
			var configration = serviceProvider.GetService<IConfiguration>();
			var serviceSettings = configration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
			var mongoDbSettings = configration.GetSection(nameof(MongoDbSettings)).Get<MongoDbSettings>();
			var mongoClient = new MongoClient(mongoDbSettings.ConnectionString);
			return mongoClient.GetDatabase(serviceSettings.ServiceName);
		});


		return services;
	}

	public static IServiceCollection AddMongoRepository<T>(this IServiceCollection services, string collectionName) where T : IEntity
	{
		services.AddSingleton<IRepository<T>>((serviceProvider) =>
		{
			var database = serviceProvider.GetService<IMongoDatabase>();
			return new MongoRepository<T>(database, collectionName);
		});


		return services;

	}



}
