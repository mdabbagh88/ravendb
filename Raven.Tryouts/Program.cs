using System;
using System.Collections.Generic;
using System.Linq;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Listeners;
using Raven.Client.Shard;
using Raven.Json.Linq;

namespace Raven.Tryouts
{
    public class Customer
    {
        public string Region;
        public string Id;
    }

    public class Invoice
    {
        public string Customer;
        public int Usage;
    }
    public class Program
    {
        private static void Main()
        {
            var shards = new Dictionary<string, IDocumentStore>
            {
                {"_", new DocumentStore {Url = "http://localhost:8080", DefaultDatabase = "Shop"}}, //existing data
                {"ME", new DocumentStore {Url = "http://localhost:8080", DefaultDatabase = "Shop_ME"}},
                {"US", new DocumentStore {Url = "http://localhost:8080", DefaultDatabase = "Shop_US"}},
            };

            Func<string, string> potentialShardToShardId = val =>
            {
                var start = val.IndexOf('/');
                if (start == -1)
                    return val;
                var potentialShardId = val.Substring(0, start);
                if (shards.ContainsKey(potentialShardId))
                    return potentialShardId;
                // this is probably an old id, let us use it.
                return "_";

            };
            Func<string, string> regionToShardId = region =>
            {
                switch (region)
                {
                    case "Middle East":
                        return "ME";
                    case "USA":
                    case "United States":
                    case "US":
                        return "US";
                    default:
                        return "_";
                }
            };
            var shardStrategy = new ShardStrategy(shards)
                .ShardingOn<Customer, string>(c => c.Region, potentialShardToShardId, regionToShardId)
                .ShardingOn<Invoice, string>(x => x.Customer, potentialShardToShardId, regionToShardId); 

            var defaultModifyDocumentId = shardStrategy.ModifyDocumentId;
            shardStrategy.ModifyDocumentId = (convention, shardId, documentId) =>
            {
                if(shardId == "_")
                    return documentId;

                return defaultModifyDocumentId(convention, shardId, documentId);
            };

            var documentStore = new ShardedDocumentStore(shardStrategy);
            documentStore.RegisterListener(new AddShardIdToMetadataStoreListener());
            documentStore.Initialize();
            using (var s = documentStore.OpenSession())
            {
                var invoices = s.Query<Invoice>()
                    .Where(x => x.Customer == "customers/1")
                    .ToList();

                foreach (var i in invoices)
                {
                    i.Usage++;
                }
                s.SaveChanges();
            }
        }
    }

    public class AddShardIdToMetadataStoreListener : IDocumentStoreListener
    {
        public bool BeforeStore(string key, object entityInstance, RavenJObject metadata, RavenJObject original)
        {
            if (metadata.ContainsKey(Constants.RavenShardId) == false)
            {
                metadata[Constants.RavenShardId] = "_";// the default shard id
            }
            return false;
        }

        public void AfterStore(string key, object entityInstance, RavenJObject metadata)
        {
        }
    }
}