﻿using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;

namespace BeerBot.BeerApi.Dal
{
    internal class InMemoryRepository<T> : IRepository<T>
    {
        private readonly T[] _entities;
        private readonly ReadOnlyDictionary<int, T> _entitiesDictionary;

        protected InMemoryRepository(string resourceName, Func<T, int> idSelector)
        {
            _entities = LoadEntities(resourceName, idSelector);
            _entitiesDictionary = new ReadOnlyDictionary<int, T>(_entities.ToDictionary(idSelector));
        }

        public IEnumerable<T> Get()
        {
            return _entities;
        }

        public T GetById(int id)
        {
            return _entitiesDictionary[id];
        }

        private static T[] LoadEntities(string resourceName, Func<T, int> idSelector)
        {
            using (var stream = typeof(InMemoryRepository<>).GetTypeInfo().Assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                var csv = new CsvReader(reader);
                csv.Configuration.BadDataFound = null;
                return csv.GetRecords<T>().OrderBy(idSelector).ToArray();
            }
        }
    }
}