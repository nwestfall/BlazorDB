using System.Threading.Tasks;

namespace BlazorDB
{
    public interface IBlazorDbFactory
    {
        Task<IndexedDbManager> GetDbManager(string dbName);

        Task<IndexedDbManager> GetDbManager(DbStore dbStore);
    }
}