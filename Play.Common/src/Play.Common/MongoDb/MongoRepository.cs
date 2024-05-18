using MongoDB.Driver;
using System.Linq.Expressions;

namespace Play.Common.MongoDb;

public class MongoRepository<T> : IRepository<T> where T : IEntity
{

	private readonly IMongoCollection<T> _dbCollection;
	private readonly FilterDefinitionBuilder<T> _filterBuilder = Builders<T>.Filter;


	public MongoRepository(IMongoDatabase database,string collectionName)
	{
		_dbCollection = database.GetCollection<T>(collectionName);
	}


	public async Task<IReadOnlyCollection<T>> GetAllAsync()
	{
		return await _dbCollection.Find(_filterBuilder.Empty).ToListAsync();
	}

	public async Task<IReadOnlyCollection<T>> GetAllAsync(Expression<Func<T, bool>> filter)
	{
		return await _dbCollection.Find(filter).ToListAsync();
	}



	public async Task<T> GetAsync(Guid id)
	{
		FilterDefinition<T> filter = _filterBuilder.Eq(x => x.Id, id);
		return await _dbCollection.Find(filter).FirstOrDefaultAsync();

	}

	public async Task<T> GetAsync(Expression<Func<T, bool>> filter)
	{
		return await _dbCollection.Find(filter).FirstOrDefaultAsync();

	}

	public async Task CreateAsync(T item)
	{
		if (item is null)
		{
			throw new ArgumentNullException(nameof(item));
		}

		await _dbCollection.InsertOneAsync(item);
	}


	public async Task UpdateAsync(T item)
	{
		if (item is null)
		{
			throw new ArgumentNullException(nameof(item));
		}
		FilterDefinition<T> filter = _filterBuilder.Eq(x => x.Id, item.Id);
		await _dbCollection.ReplaceOneAsync(filter, item);

	}


	public async Task DeleteAsync(Guid id)
	{
		FilterDefinition<T> filter = _filterBuilder.Eq(x => x.Id, id);
		await _dbCollection.DeleteOneAsync(filter);
	}

}
