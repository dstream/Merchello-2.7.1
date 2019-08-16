﻿namespace Merchello.Tests.IntegrationTests.Services.ProductOptions
{
    using System;
    using System.Linq;

    using Merchello.Core;
    using Merchello.Core.Models;
    using Merchello.Core.Services;
    using Merchello.Tests.Base.TestHelpers;

    using NUnit.Framework;

    [TestFixture]
    public class ShareOptionTests : OptionTestsBase
    {
        [Test]
        public void Can_Create_A_Clone_Of_An_Option()
        {
            //// Arrange
            var option = this._productOptionService.GetByKey(_optionKey);

            //// Act
            var clone = option.Clone();
            option.Name = "New Name";
            option.Choices.First().Name = "New Name";

            //// Assert
            Assert.NotNull(clone);
            Assert.AreEqual(clone.Key, option.Key);
            Assert.AreEqual(clone.Choices.Count, option.Choices.Count);
            Assert.AreNotEqual(clone.Name, option.Name);
            Assert.IsFalse(clone.Choices.Any(x => x.Name == "New Name"), "There is a choice with this name");
        }


        [Test]
        public void Can_Add_A_Shared_Option()
        {
            //// Arrange
            
            //// Act
            var option = this._productOptionService.CreateProductOption("Shared", true);
            option.AddChoice("Shared", "shared");

            this._productOptionService.Save(option);

            //// Assert
            Assert.IsTrue(option.HasIdentity);
            Assert.AreEqual(option.Choices.Count, 1);
        }

        [Test]
        public void Can_Add_A_Shared_Option_With_Multiple_Choices()
        {
            //// Arrange

            //// Act
            var option = this._productOptionService.CreateProductOption("Shared", true);
            option.AddChoice("Shared", "shared");
            option.AddChoice("Shared", "shared");
            option.AddChoice("Shared", "shared");
            option.AddChoice("Shared", "shared");

            this._productOptionService.Save(option);

            //// Assert
            Assert.IsTrue(option.HasIdentity);
            Assert.AreEqual(option.Choices.Count, 4);
        }


        [Test]
        public void Can_Delete_An_Option()
        {
            //// Arrange
            var option = this._productOptionService.CreateProductOption("Shared", true);
            option.AddChoice("Shared", "shared");
            option.AddChoice("Shared", "shared");
            option.AddChoice("Shared", "shared");
            option.AddChoice("Shared", "shared");
            this._productOptionService.Save(option);

            Assert.IsTrue(option.HasIdentity);
            Assert.AreEqual(option.Choices.Count, 4);

            var key = option.Key;

            //// Act
            this._productOptionService.Delete(option);

            //// Assert
            var deleted = this._productOptionService.GetByKey(key);

            Assert.IsNull(deleted);
        }

        [Test]
        public void Can_Add_A_Shared_Option_To_A_Product()
        {
            //// Arrange
            var product = _productService.GetByKey(this._product1Key);
            var option = _productOptionService.GetByKey(_optionKey);

            //// Act
            product.ProductOptions.Add(option);
            _productService.Save(product);

            MerchelloContext.Current.Cache.RuntimeCache.ClearAllCache();

            var p = _productService.GetByKey(this._product1Key);

            //// Assert
            Assert.IsTrue(p.ProductOptions.Any());
            Assert.IsTrue(p.ProductOptions.Any(x => x.Key == option.Key));
        }

        [Test]
        public void Can_Add_A_Shared_Option_To_A_Product_With_A_Reduced_Set_Of_Choices()
        {
            //// Arrange
            var product = CreateSavedProduct();
            var option = _productOptionService.GetByKey(_optionKey);
            var clone = option.Clone();

            //// Act
            clone.Choices.Remove(clone.Choices.First(x => x.Sku == "choice1"));
            clone.Choices.Remove(clone.Choices.First(x => x.Sku == "choice2"));

            MerchelloContext.Current.Cache.RuntimeCache.ClearAllCache();

            product.ProductOptions.Add(clone);
            _productService.Save(product);

            //// Assert
            Assert.AreEqual(4, product.ProductVariants.Count, "Variant count did not match was: " + product.ProductVariants.Count);
            Assert.AreEqual(2, product.ProductOptions.Count, "Option count did not match");
            Console.Write(product.Key);
            Assert.AreEqual(2, product.ProductOptions.First(x => x.Key == option.Key).Choices.Count, "Shared option choices was not 2");
            Assert.AreEqual(4, option.Choices.Count, "Option choice count did not match");

        }

        [Test]
        public void Can_Share_An_Option_With_Multiple_Products_With_Different_Selected_Options()
        {
            //// Arrange
            var p1 = _productService.GetByKey(_product1Key);
            var p2 = _productService.GetByKey(_product2Key);

            var option = _productOptionService.GetByKey(_optionKey);

            //// Act
            var so1 = option.Clone();
            so1.Choices.Remove(so1.Choices.First(x => x.Sku == "choice1"));
            so1.Choices.Remove(so1.Choices.First(x => x.Sku == "choice2"));

            var so2 = option.Clone();
            so2.Choices.Remove(so2.Choices.First(x => x.Sku == "choice1"));
            so2.Choices.Remove(so2.Choices.First(x => x.Sku == "choice3"));

            p1.ProductOptions.Add(so1);
            p2.ProductOptions.Add(so2);

            _productService.Save(new[] { p1, p2 });

            var query = _merchello.Query.Product.GetProductsWithOption(option.Key, 1, long.MaxValue);

            MerchelloContext.Current.Cache.RuntimeCache.ClearAllCache();

            //// Assert
            var uses = _productOptionService.GetProductOptionUseCount(option);

            Assert.NotNull(uses);
            Assert.IsTrue(query.Items.Any());

        }

        [Test]
        public void Can_Remove_A_Shared_Option_From_A_Product_And_Option_Still_Exists()
        {
            //// Arrange
            var product = _productService.GetByKey(this._product1Key);
            var option = _productOptionService.GetByKey(_optionKey);
            product.ProductOptions.Add(option);
            _productService.Save(product);

            MerchelloContext.Current.Cache.RuntimeCache.ClearAllCache();
            var p = _productService.GetByKey(this._product1Key);
            Assert.IsTrue(p.ProductOptions.Any(x => x.Key == option.Key));

            //// Act
            p.ProductOptions.Remove(option);
            _productService.Save(p);

            MerchelloContext.Current.Cache.RuntimeCache.ClearAllCache();
            var o = _productOptionService.GetByKey(_optionKey);

            //// Assert
                        Assert.NotNull(o);
            Assert.IsFalse(p.ProductOptions.Contains(option.Key), "Option still exists");
        }

        [Test]
        public void Can_Get_An_Option_By_Its_Key()
        {
            //// Arrange

            //// Act
            var retrieved = this._productOptionService.GetByKey(_optionKey);

            //// Assert
            Assert.NotNull(retrieved, "Retrieved was null");
            Assert.NotNull(retrieved.Choices, "Choice collection was null");
            Assert.AreEqual(retrieved.Choices.Count, 4, "Did not have any choices");
        }


    }
}