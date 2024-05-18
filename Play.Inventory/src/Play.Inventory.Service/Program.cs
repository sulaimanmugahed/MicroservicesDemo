using Microsoft.AspNetCore.Mvc;
using Play.Common;
using Play.Common.MassTransit;
using Play.Common.MongoDb;
using Play.Inventory.Service;
using Play.Inventory.Service.Clients;
using Play.Inventory.Service.Dtos;
using Play.Inventory.Service.Entities;
using Polly;
using Polly.Timeout;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddMongo()
	.AddMongoRepository<InventoryItem>("inventoryitems")
	.AddMongoRepository<CatalogItem>("catalogitems")
	.AddMassTransitWithRabbitMQ();


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("inventory/{userId}", async (Guid userId, [FromServices] IRepository<InventoryItem> inventoryItemRepository, [FromServices] IRepository<CatalogItem> catalogItemRepository) =>
{
	var inventoryItemEntities = await inventoryItemRepository.GetAllAsync(item => item.UserId == userId);

	var itemIds = inventoryItemEntities.Select(item => item.CatalogItemId);

	var catalogItemEntities = await catalogItemRepository.GetAllAsync(item=> itemIds.Contains(item.Id));

	var inventoryItemDtos = inventoryItemEntities.Select(inventoryItem =>
	{
		var categoryItem = catalogItemEntities.Single(x => x.Id == inventoryItem.CatalogItemId);
		return inventoryItem.AsDto(categoryItem.Name, categoryItem.Description);
	});

	return Results.Ok(inventoryItemDtos);
});


app.MapPost("inventory", async (GrantItemsDto grantItemsDto, [FromServices] IRepository<InventoryItem> repository) =>
{
	var inventoryItem = await repository.GetAsync(
		item => item.UserId == grantItemsDto.UserId && item.CatalogItemId == grantItemsDto.CatalogItemId);

	if (inventoryItem is null)
	{
		inventoryItem = new InventoryItem
		{
			CatalogItemId = grantItemsDto.CatalogItemId,
			UserId = grantItemsDto.UserId,
			Quanity = grantItemsDto.Quantity,
			AcquiredDate = DateTimeOffset.UtcNow
		};

		await repository.CreateAsync(inventoryItem);
	}
	else
	{
		inventoryItem.Quanity = grantItemsDto.Quantity;
		await repository.UpdateAsync(inventoryItem);
	}

	return Results.Ok();

});

app.Run();

static void AddCatalogClient(WebApplicationBuilder builder)
{
	Random jitterer = new Random();

	builder.Services.AddHttpClient<CatalogClient>((client) =>
	{
		client.BaseAddress = new Uri("https://localhost:5001");
	})
	.AddTransientHttpErrorPolicy(policybuilder => policybuilder.Or<TimeoutRejectedException>().WaitAndRetryAsync(
		5,
		retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
		+ TimeSpan.FromMilliseconds(jitterer.Next(0, 1000)),
		onRetry: (outcome, timespan, retryAttempt) =>
		{
			var serviceProvider = builder.Services.BuildServiceProvider();
			serviceProvider.GetService<ILogger<CatalogClient>>()?
			.LogWarning($"delay for {timespan.TotalSeconds} Seconds, then Making retry {retryAttempt}");
		}
		))
	.AddTransientHttpErrorPolicy(policybuilder => policybuilder.Or<TimeoutRejectedException>().CircuitBreakerAsync(
		3,
		TimeSpan.FromSeconds(15),
		onBreak: (outcome, timespan) =>
		{
			var serviceProvider = builder.Services.BuildServiceProvider();
			serviceProvider.GetService<ILogger<CatalogClient>>()?
			.LogWarning($"Opening the circuit for {timespan.TotalSeconds} Seconds..");
		},
		onReset: () =>
		{
			var serviceProvider = builder.Services.BuildServiceProvider();
			serviceProvider.GetService<ILogger<CatalogClient>>()?
			.LogWarning($"Closing the circuit");
		}
	  ))
	  .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(1));
}