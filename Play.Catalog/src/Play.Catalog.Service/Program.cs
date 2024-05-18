using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Play.Catalog.Contracts;
using Play.Catalog.Service;
using Play.Catalog.Service.Dtos;
using Play.Catalog.Service.Entities;
using Play.Common;
using Play.Common.MassTransit;
using Play.Common.MongoDb;
using Play.Common.Settings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var serviceSettings = builder.Configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();

builder.Services.AddMongo()
	.AddMongoRepository<Item>("items")
	.AddMassTransitWithRabbitMQ();

//builder.Services.AddMassTransitHostedService();

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



app.MapGet("Items", async ([FromServices] IRepository<Item> itemRepository) =>
{
	
	var items = await itemRepository.GetAllAsync();

	return Results.Ok(items.Select(x => x.AsDto()));
});




app.MapGet("Items/{id:guid}", async ([FromRoute] Guid id, [FromServices] IRepository<Item> itemRepository) =>
{
	var item = await itemRepository.GetAsync(id);

	if (item is null)
	{
		return Results.NotFound();
	}

	return Results.Ok(item.AsDto());

})
.WithName("GetById");

app.MapPost("Items", async (CreateItemDto createItemDto, [FromServices] IRepository<Item> itemRepository,IPublishEndpoint publisher) =>
{
	var item = new Item
	{
		Name = createItemDto.Name,
		Description = createItemDto.Description,
		Price = createItemDto.Price,
		CreatedDate = DateTimeOffset.UtcNow
	};
	await itemRepository.CreateAsync(item);

	await publisher.Publish(new CatalogItemCreated(item.Id,item.Name,item.Description));

	return Results.CreatedAtRoute("GetById", new { item.Id }, item);
});




app.MapPut("Items/{id}", async (Guid id, UpdateItemDto updateItemDto, [FromServices] IRepository<Item> itemRepository, IPublishEndpoint publisher) =>
{
	var existItem = await itemRepository.GetAsync(id);
	if (existItem is null)
		return Results.NotFound();

	existItem.Name = updateItemDto.Name;
	existItem.Description = updateItemDto.Description;
	existItem.Price = updateItemDto.Price;

	await itemRepository.UpdateAsync(existItem);

	await publisher.Publish(new CatalogItemUpdated(existItem.Id, existItem.Name, existItem.Description));


	return Results.NoContent();
});




app.MapDelete("Items/{id:guid}", async ([FromRoute] Guid id, [FromServices] IRepository<Item> itemRepository, IPublishEndpoint publisher) =>
{
	var existItem = await itemRepository.GetAsync(id);
	if (existItem is null)
		return Results.NotFound();

	await itemRepository.DeleteAsync(existItem.Id);

	await publisher.Publish(new CatalogItemDeleted(existItem.Id));

	return Results.NoContent();

});




app.Run();



