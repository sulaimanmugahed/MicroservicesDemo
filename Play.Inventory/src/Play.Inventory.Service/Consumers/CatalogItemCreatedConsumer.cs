using MassTransit;
using Play.Catalog.Contracts;
using Play.Common;
using Play.Inventory.Service.Entities;

namespace Play.Inventory.Service.Consumers;

public class CatalogItemCreatedConsumer(IRepository<CatalogItem> repository) : IConsumer<CatalogItemCreated>
{
	public async Task Consume(ConsumeContext<CatalogItemCreated> context)
	{
		var message = context.Message;
		var item = await repository.GetAsync(message.Id);
		if(item is not null)
		{
			return;
		}

		item = new CatalogItem
		{
			Id = message.Id,
			Name = message.Name,
			Description = message.Description,
		};

		await repository.CreateAsync(item);
	}
}
