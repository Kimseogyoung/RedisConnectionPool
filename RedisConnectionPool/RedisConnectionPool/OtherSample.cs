using StackExchange.Redis.MultiplexerPool;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedisConnectionPoolConsoleApp
{
    class Sample
    {

        public static async Task RunExampleAsync()
        {
            const string cRedisConnectionConfiguration = "localhost:6379,allowAdmin=true,abortConnect=false,ssl=false";
            var poolSize = 10;
            _connectionPool = ConnectionMultiplexerPoolFactory.Create(
                 poolSize: poolSize,
                  configurationOptions: new ConfigurationOptions()

                  {
                      SyncTimeout = 5000,
                      EndPoints =
                                 {
                        { "localhost", 6379}
                     },
                      AbortOnConnectFail = false // this prevents that error
                  },
                 connectionSelectionStrategy: ConnectionSelectionStrategy.RoundRobin);

            _connectionsErrorCount = new int[poolSize];

            for (var i = 0; i < 100; i++)
            {
                var key = $"KEY_{i}";
                var value = $"KEY_{i}";
                await QueryRedisAsync(async db => await db.StringSetAsync(key, value));
            }

            for (var i = 0; i < 100; i++)
            {
                var key = $"KEY_{i}";
                var value = await QueryRedisAsync(async db => await db.StringGetAsync(key));

                Console.WriteLine($"Key: '{key}' Value: '{value}'");
            }
        }

        private static async Task<TResult> QueryRedisAsync<TResult>(Func<IDatabase, Task<TResult>> op)
        {
            var connection = await _connectionPool.GetAsync();

            Console.WriteLine($"Connection '{connection.ConnectionIndex}' established at {connection.ConnectionTimeUtc}");

            try
            {
                return await op(connection.Connection.GetDatabase());
            }
            catch (RedisConnectionException)
            {
                _connectionsErrorCount[connection.ConnectionIndex]++;

                if (_connectionsErrorCount[connection.ConnectionIndex] < 3)
                {
                    throw;
                }

                // Decide when to reconnect based on your own custom logic
                Console.WriteLine($"Re-establishing connection on index '{connection.ConnectionIndex}'");

                await connection.ReconnectAsync();

                return await op(connection.Connection.GetDatabase());
            }
        }

        private static IConnectionMultiplexerPool _connectionPool;
        private static int[] _connectionsErrorCount;
    }
}