using System;
using System.Threading.Tasks;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;

namespace app.Services;

public interface IDBRService
{
    Task<T> QueryAsync<T>(Func<QueryApi, Task<T>> action);
}
