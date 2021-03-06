﻿// -----------------------------------------------------------------------
//  <copyright file="RDBQA_7.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------
namespace Raven.Tests.Issues
{
    using System;
    using System.IO;

    using Lucene.Net.Support;

    using Raven.Abstractions.Data;
    using Raven.Abstractions.Smuggler;
    using Raven.Client;
    using Raven.Database.Extensions;
    using Raven.Json.Linq;
    using Raven.Smuggler;

    using Xunit;

    public class RDBQA_7 : RavenTest
    {
        private class Product
        {
            public int Id { get; set; }

            public string Value { get; set; }
        }

        [Fact]
        public void NegativeFiltersShouldNotFilterOutWhenThereAreNoMatches()
        {
            var options = new SmugglerOptions
            {
                BackupPath = Path.GetTempFileName(),
                Filters =
                    new EquatableList<FilterSetting>
                                  {
                                      new FilterSetting
                                      {
                                          Path = "Value",
                                          ShouldMatch = false,
                                          Values = new EquatableList<string> { "Value1" }
                                      }
                                  }
            };

            try
            {
                using (var store = NewRemoteDocumentStore())
                {
                    Initialize(store);

                    var smuggler = new SmugglerApi(options, new RavenConnectionStringOptions { Url = store.Url });

                    smuggler.ExportData(null, options, false).Wait(TimeSpan.FromSeconds(15));
                }

                using (var store = NewRemoteDocumentStore())
                {
                    var smuggler = new SmugglerApi(options, new RavenConnectionStringOptions { Url = store.Url });

                    smuggler.ImportData(options).Wait(TimeSpan.FromSeconds(15));

                    Assert.NotNull(store.DatabaseCommands.Get("key/1"));

                    using (var session = store.OpenSession())
                    {
                        var product1 = session.Load<Product>(1);
                        var product2 = session.Load<Product>(2);
                        var product3 = session.Load<Product>(3);

                        Assert.Null(product1);
                        Assert.Null(product2);
                        Assert.NotNull(product3);
                    }
                }
            }
            finally
            {
                IOExtensions.DeleteDirectory(options.BackupPath);
            }
        }

        [Fact]
        public void NegativeMetadataFiltersShouldNotFilterOutWhenThereAreNoMatches()
        {
            var options = new SmugglerOptions
                          {
                              BackupPath = Path.GetTempFileName(),
                              Filters =
                                  new EquatableList<FilterSetting>
                                  {
                                      new FilterSetting
                                      {
                                          Path = "@metadata.Raven-Entity-Name",
                                          ShouldMatch = false,
                                          Values = new EquatableList<string> { "Products" }
                                      }
                                  }
                          };

            try
            {
                using (var store = NewRemoteDocumentStore())
                {
                    Initialize(store);

                    var smuggler = new SmugglerApi(options, new RavenConnectionStringOptions { Url = store.Url });

                    smuggler.ExportData(null, options, false).Wait(TimeSpan.FromSeconds(15));
                }

                using (var store = NewRemoteDocumentStore())
                {
                    var smuggler = new SmugglerApi(options, new RavenConnectionStringOptions { Url = store.Url });

                    smuggler.ImportData(options).Wait(TimeSpan.FromSeconds(15));

                    Assert.NotNull(store.DatabaseCommands.Get("key/1"));

                    using (var session = store.OpenSession())
                    {
                        var product1 = session.Load<Product>(1);
                        var product2 = session.Load<Product>(2);
                        var product3 = session.Load<Product>(3);

                        Assert.Null(product1);
                        Assert.Null(product2);
                        Assert.Null(product3);
                    }
                }
            }
            finally
            {
                IOExtensions.DeleteDirectory(options.BackupPath);
            }
        }

        private static void Initialize(IDocumentStore store)
        {
            store.DatabaseCommands.Put("key/1", null, new RavenJObject(), new RavenJObject());

            var product1 = new Product { Value = "Value1" };
            var product2 = new Product { Value = "Value1" };
            var product3 = new Product { Value = "Value2" };

            using (var session = store.OpenSession())
            {
                session.Store(product1);
                session.Store(product2);
                session.Store(product3);

                session.SaveChanges();
            }
        }
    }
}