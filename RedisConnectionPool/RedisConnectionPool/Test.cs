using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;


public  class Test
{
    static int _testCount = 10000;

    static int _poolCount = 0;
    static int _singleCount = 0;
    static int _ImmediateCount = 0;

    static private ConnectionMultiplexer _redis;
    static public ConnectionMultiplexer Redis { 
        get { 
            if(_redis == null || _redis.IsConnected == false)
            {
                _redis = ConnectionMultiplexer.Connect(new ConfigurationOptions()
                {
                    SyncTimeout = 5000,
                    EndPoints =
                        {
                            {"localhost", 6379}
                        },
                    AbortOnConnectFail = false // this prevents that error
                });
            }   
            return _redis; 
        } 
    }




    static RedisConnectionPool _pool = new RedisConnectionPool();
    static public void ConnectionPoolTest(int poolSize)
    {
        Console.WriteLine("Create Pool");
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        _pool.CreatePool(poolSize);
        stopwatch.Stop();
        Console.WriteLine("Create Pool done " + stopwatch.Elapsed);

        Console.WriteLine("PoolTest Start");
        stopwatch.Restart();

        for (int i = 0; i < _testCount; i++)
        {
            int num = i;
            Thread thread = new Thread(() => SetUsingConnectionPool(num));
            thread.Start();
        }
        while(_poolCount < _testCount) { }


        stopwatch.Stop();
        Console.WriteLine("PoolTest done " + stopwatch.Elapsed);
    }

    static public async Task SetUsingConnectionPool(int i)
    {

        ConnectionMultiplexer redis = await _pool.DequeueConnectionAsync();
        IDatabase db = redis.GetDatabase();
        await db.SortedSetAddAsync("pool", new RedisValue(i.ToString()), i);
        _pool.EnqueueConnection(redis);
        Interlocked.Increment(ref _poolCount);
    }

    static public void SingleConnectionTest()
    {
        Console.WriteLine("SingleTest Start");
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();


        for (int i = 0; i < _testCount; i++)
        {
            int num = i;
            Thread thread = new Thread(() => SetUsingSingleConnection(num));
            thread.Start();
        }
        while (_singleCount < _testCount) { }


        stopwatch.Stop();
        Console.WriteLine("SingleTest done " + stopwatch.Elapsed);
    }
    static public async Task SetUsingSingleConnection(int i)
    {
        await Redis.GetDatabase().SortedSetAddAsync("single", new RedisValue(i.ToString()), i);

        Interlocked.Increment(ref _singleCount);

    }


    static public void ImmediateConnectionTest()
    {
        Console.WriteLine("ImmediateTest Start");
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();


        for (int i = 0; i < _testCount; i++)
        {
            int num = i;
            Thread thread = new Thread(() => SetUsingImmediateConnection(num));
            thread.Start();
        }
        while (_ImmediateCount < _testCount) { }


        stopwatch.Stop();
        Console.WriteLine("ImmediateTest done " + stopwatch.Elapsed);
    }

    static public async Task SetUsingImmediateConnection(int i)
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

        await redis.GetDatabase().SortedSetAddAsync("imme", new RedisValue(i.ToString()), i);

        Interlocked.Increment(ref _ImmediateCount);

    }
}

