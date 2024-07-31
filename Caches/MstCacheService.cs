using System;
using System.Collections.Generic;
using System.Linq;
using MstCaches;
using NPOI.OpenXmlFormats.Shared;
using SqlSugar;
using Umbraco.Core.Collections;

namespace MstSopService.Caches
{
    /// <summary>
    /// 缓存类
    /// </summary>
    public class MstCacheService : ICacheService
    {
        private static readonly string ConstantMaohao = ":";
        private static string _preKey = "MstContractService";
        public static string PreKey
        {
            get => _preKey; set
            {
                value = value.TrimEnd(':');
                _preKey = value + ConstantMaohao;
            }
        }

        private static MstCache _cache;

        public MstCache Cache()
        {
            Init();
            return _cache;
        }

        public static void Init()
        {
            if (_cache == null)
            {
                _cache = new MstCache();
            }
        }

        public MstCacheService()
        {
            PreKey = _preKey;
            this.DefaultCacheDurationInSeconds = 30 * 60 * 24;
        }

        private string GetKey(string key)
        {
            return key.StartsWith(_preKey) ? key : _preKey + key;
        }


        public int DefaultCacheDurationInSeconds { get; set; }

        public void Add<V>(string key, V value)
        {
            Init();
            key = GetKey(key);
            _cache.Set(key, value);
        }

        public void Add<V>(string key, V value, int cacheDurationInSeconds)
        {
            if (cacheDurationInSeconds == int.MaxValue)
            {
                cacheDurationInSeconds = DefaultCacheDurationInSeconds;
            }
            key = GetKey(key);
            _cache.Set(key, value, cacheDurationInSeconds);
        }

        public bool ContainsKey<V>(string key)
        {
            key = GetKey(key);
            return _cache.Exist(key);
        }

        public V Get<V>(string key)
        {
            Init();
            key = GetKey(key);
            var value = _cache.CSRedis.Get<V>(key);
            return value;
        }

        public IEnumerable<string> GetAllKey<V>()
        {
            var keys = _cache.CSRedis.Keys($"{_preKey}*");
            return keys;
        }

        public V GetOrCreate<V>(string cacheKey, Func<V> create, int cacheDurationInSeconds = int.MaxValue)
        {
            if (cacheDurationInSeconds == int.MaxValue)
            {
                cacheDurationInSeconds = DefaultCacheDurationInSeconds;
            }

            if (ContainsKey<V>(cacheKey))
            {
                return Get<V>(cacheKey);
            }
            else
            {
                var result = create.Invoke();
                Add<V>(cacheKey, result, cacheDurationInSeconds);
                return result;
            }
        }

        public void Remove<V>(string key)
        {
            key = GetKey(key);
            long i = _cache.CSRedis.Del(key);
        }

        /// <summary>
        /// 向有序集合添加一个或多个成员
        /// </summary>
        /// <param name="key"></param>
        /// <param name="scoreMembers"></param>
        /// <returns></returns>
        public long ZAdd(string key, params (decimal, object)[] scoreMembers)
        {
            //Init();
            key = GetKey(key);
            var value = _cache.CSRedis.ZAdd(key, scoreMembers);
            return value;
        }
        /// <summary>
        /// 获取有序集合数据(高到低)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <returns></returns>
        public string[] ZRevRange(string key, long start , long stop)
        {
            //Init();
            key = GetKey (key);
            var value = _cache.CSRedis.ZRevRange(key, start, stop);
            return value;
        }
        /// <summary>
        /// 获取有序集合数据(低到高)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <returns></returns>
        public string[] ZRange(string key, long start, long stop)
        {
            //Init();
            key = GetKey(key);
            var value = _cache.CSRedis.ZRange(key, start, stop);
            return value;
        }
        /// <summary>
        /// 向有序集合移除一个或多个成员
        /// </summary>
        /// <param name="key"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        public long ZRange(string key, params string[] member)
        {
            //Init();
            key = GetKey(key);
            var value = _cache.CSRedis.ZRem(key, member);
            return value;
        }
        /// <summary>
        /// 向无序集合添加数据
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="key"></param>
        /// <param name="values"></param>
        public long SAdd(string key, object values)
        {
            //Init();
            //key = GetKey(key);
            var value= _cache.CSRedis.SAdd(key, values);
            return value;
        }

    }
}
