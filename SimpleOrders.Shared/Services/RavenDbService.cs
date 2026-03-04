using Microsoft.Extensions.Configuration;
using Raven.Client.Documents;
using Raven.Client.Exceptions;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace SimpleOrders.Shared.Services;

public class RavenDbService : IDisposable
{
    public DocumentStore DocumentStore { get; }

    public RavenDbService(IConfiguration configuration)
    {
        var ravenDbSettings = configuration.GetSection("RavenDB").Get<RavenDbSettings>();

        DocumentStore = new DocumentStore
        {
            Urls = ravenDbSettings?.Urls.ToArray(),
            Database = ravenDbSettings?.Database
        };

        DocumentStore.Initialize();

        try
        {
            DocumentStore.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord("SimpleOrders")));
        }
        catch (ConcurrencyException)
        {
            Console.WriteLine("RavenDB: Banco de dados SimpleOrders já foi criado.");
        }
    }

    public void Dispose()
    {
        DocumentStore.Dispose();
    }
}