using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nito.AsyncEx;
using StackExchange.Redis;


public class RedisConnectionPool
{
    static ConcurrentQueue<ConnectionMultiplexer> pool = new ConcurrentQueue<ConnectionMultiplexer>();

    static public void CreatePool(int poolSize)
    {
        for (int i = 0; i < poolSize; i++)
        {
            Thread thread = new Thread(ConnectRedisClient);
            thread.Start();
        }
    }
    static public void ConnectRedisClient()
    {
        ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(new ConfigurationOptions()
        {
            SyncTimeout = 5000,
            EndPoints =
                {
                    {"localhost", 6379}
                },
            AbortOnConnectFail = false // this prevents that error
                   
        });
        //var obj = new PooledRedisConnection(redis);
        pool.Enqueue(redis);
        
    }
    static private readonly AsyncLock s_lock = new AsyncLock();
    static public async Task<ConnectionMultiplexer> DequeueConnectionAsync()
    {
        ConnectionMultiplexer connection ;
        using (await s_lock.LockAsync())
        {
            while(pool.Count <= 0)
            {
            }
            pool.TryDequeue(out connection);
        }
        return connection;
    }
    static public void EnqueueConnection(ConnectionMultiplexer connection)
    {
        pool.Enqueue(connection);
    }
}

/*
 * 
 * PriorityQueue<ConnectionMultiplexer, ConnectionMultiplexer> pq =
                new PriorityQueue<ConnectionMultiplexer, ConnectionMultiplexer>(new ConnectionMultiplexerComparer());
    public class PooledRedisConnection
    {
        public ConnectionMultiplexer _redis;
        public int pending { get; set; }
        public PooledRedisConnection(ConnectionMultiplexer redis)
        {
            _redis = redis;
            pending = 0;

        }
    }
    



    public class PooledComparer : IComparer<PooledRedisConnection>
    {
        public int Compare(PooledRedisConnection x, PooledRedisConnection y)
        {
            return (y.pending).CompareTo(x.pending);
        }
    }
    public class ConnectionMultiplexerComparer : IComparer<ConnectionMultiplexer>
    {
        public int Compare(ConnectionMultiplexer x, ConnectionMultiplexer y)
        {
            return (y.GetCounters().TotalOutstanding).CompareTo(x.GetCounters().TotalOutstanding);
        }
    }

*/
