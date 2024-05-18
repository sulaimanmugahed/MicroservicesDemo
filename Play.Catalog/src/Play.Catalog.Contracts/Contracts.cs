using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Play.Catalog.Contracts;
public record CatalogItemCreated(Guid Id,string Name,string Description);
public record CatalogItemUpdated(Guid Id,string Name,string Description);
public record CatalogItemDeleted(Guid Id);

