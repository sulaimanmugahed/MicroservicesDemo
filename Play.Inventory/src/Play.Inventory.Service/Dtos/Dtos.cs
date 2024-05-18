namespace Play.Inventory.Service.Dtos;

public record GrantItemsDto(Guid UserId,Guid CatalogItemId,int Quantity);
public record InventoryItemDto(Guid CatalogItemId,string Name ,string Description, int Quantity,DateTimeOffset AcquireDate);

public record CatalogItemDto(Guid Id, string Name, string Description);