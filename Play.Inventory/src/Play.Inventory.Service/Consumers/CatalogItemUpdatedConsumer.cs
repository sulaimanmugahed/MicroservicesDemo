using MassTransit;
using Play.Catalog.Contracts;
using Play.Common;
using Play.Inventory.Service.Entities;

namespace Play.Inventory.Service.Consumers;

public class CatalogItemUpdatedConsumer(IRepository<CatalogItem> repository) : IConsumer<CatalogItemUpdated>
{
	public async Task Consume(ConsumeContext<CatalogItemUpdated> context)
	{
		var message = context.Message;
		var item = await repository.GetAsync(message.Id);
		if (item is null)
		{
			item = new CatalogItem
			{
				Id = message.Id,
				Name = message.Name,
				Description = message.Description,
			};

			await repository.CreateAsync(item);
		}
		else
		{
			item.Name = message.Name;
			item.Description = message.Description;
			await repository.UpdateAsync(item);
		}
	}
}
