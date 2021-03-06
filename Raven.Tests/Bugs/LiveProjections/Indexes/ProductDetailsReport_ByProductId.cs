﻿namespace Raven.Tests.Bugs.LiveProjections.Indexes
{
	using System.Linq;

	using Raven.Client.Indexes;
	using Raven.Tests.Bugs.LiveProjections.Entities;
	using Raven.Tests.Bugs.LiveProjections.Views;

	public class ProductDetailsReport_ByProductId : AbstractIndexCreationTask<Product, ProductDetailsReport>
	{
		public ProductDetailsReport_ByProductId()
		{
			Map = products => from product in products
			                  select new
			                  	{
			                  		ProductId = product.Id,
			                  	};
		}
	}

	public class ProductDetailsReport_Transformer : AbstractTransformerCreationTask<Product>
	{
		public ProductDetailsReport_Transformer()
		{
			TransformResults = results =>
							   from result in results
							   let product = LoadDocument<Product>(result.Id)
							   let variants = product.Variants
							   select new
							   {
								   result.Id,
								   Name = product.Name,
								   Variants = variants.Select(x => new
								   {
									   x.ArticleNumber,
									   x.Name,
									   x.Packing,
									   IsInStock = x.QuantityInWarehouse > 0
								   })
							   };
		}
	}
}