using classApp.Models;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace classApp.DataAccess
{
    public class RedisCacheMenusRepository
    {
        IDatabase myCacheDb;
        public RedisCacheMenusRepository(string connectionString)
        {
            var cn = ConnectionMultiplexer.Connect(connectionString);
            myCacheDb = cn.GetDatabase();
        }

        public async void AddMenu(Menu m)
        {
            var list = await GetMenus();
            list.Add(m);
            string menus = JsonConvert.SerializeObject(list);
            await myCacheDb.StringSetAsync("menus", menus);
        }

        public async Task<List<Menu>> GetMenus()
        {
            string menus = await myCacheDb.StringGetAsync("menus");
            
            var list = JsonConvert.DeserializeObject<List<Menu>>(menus);
            return list;
        }
    }
}
