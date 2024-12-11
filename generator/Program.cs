using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using Microsoft.EntityFrameworkCore;
using TechStore.Models;
using TechStore.Data;

public class Program
{
    public static void Main(string[] args)
    {
        using var context = new ApplicationDbContext();

        // Tworzenie generatora klientów
        var clientFaker = new Faker<Client>()
            .RuleFor(c => c.Id, f => Guid.NewGuid().ToString())
            .RuleFor(c => c.FirstName, f => f.Name.FirstName())
            .RuleFor(c => c.LastName, f => f.Name.LastName())
            .RuleFor(c => c.Email, f => f.Internet.Email())
            .RuleFor(c => c.PasswordHash, f => f.Internet.Password())
            .RuleFor(c => c.UserName, (f, c) => c.Email)
            .RuleFor(c => c.NormalizedEmail, (f, c) => c.Email.ToUpper())
            .RuleFor(c => c.NormalizedUserName, (f, c) => c.Email.ToUpper())
            .RuleFor(c => c.EmailConfirmed, c => true)
            .RuleFor(c => c.Address, f => new Address
            {
                Street = f.Address.StreetName(),
                BuildingNumber = f.Address.BuildingNumber(),
                ApartmentNumber = f.Address.SecondaryAddress(),
                PostalCode = f.Address.ZipCode(),
                Locality = f.Address.City()
            });

        // Kategorie
        var categoryFaker = new Faker<Category>()
            .RuleFor(c => c.Name, f => f.Commerce.Categories(1).First());

        // Tworzenie generatora produktów
        var productFaker = new Faker<Product>()
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Price, f => f.Random.Decimal(10, 1000))
            .RuleFor(p => p.Description, f => f.Lorem.Paragraph())
            .RuleFor(p => p.Quantity, f => f.Random.Int(1, 100))
            .RuleFor(p => p.Image, f => f.Image.PicsumUrl())
            .RuleFor(p => p.Company, f => f.Company.CompanyName())
            .RuleFor(p => p.IsOnSale, f => f.Random.Bool())
            .RuleFor(p => p.SalePrice, (f, p) => p.IsOnSale ? (double?)(p.Price * 0.9M) : null)
            .RuleFor(p => p.Url, f => f.Internet.Url())
            .RuleFor(p => p.CategoryId, f => f.PickRandom(context.Categories.Select(c => c.Id).ToList()));

        // Adres dostawy
         var orderFaker = new Faker<Order>()
             .RuleFor(o => o.OrderStatus, f => f.PickRandom("Pending", "Completed", "Shipped", "Cancelled"))
             .RuleFor(o => o.OrderValue, f => Convert.ToDouble(f.Commerce.Price(100, 100000)))
             .RuleFor(o => o.OrderDate, f => f.Date.Past(5))
             .RuleFor(o => o.OrderConfirmation, f => f.Random.Bool())
             .RuleFor(o => o.CompletionConfirmation, f => f.Random.Bool())
             .RuleFor(o => o.ClientId, f => f.PickRandom(context.Clients.Select(c => c.Id).ToList())) // Losowy klient
             .RuleFor(o => o.ShippingAddress, f => new ShippingAddress // Tworzenie nowego adresu dostawy
             {
                 Locality = f.Address.City(),
                 Street = f.Address.StreetName(),
                 BuildingNumber = f.Address.BuildingNumber(),
                 ApartmentNumber = f.Address.SecondaryAddress(),
                 PostalCode = f.Address.ZipCode()
             });
        var ProductOrderRelFaker = new Faker<ProductOrderRelation>()
        .RuleFor(o => o.OrderId, f => f.PickRandom(context.Orders.Select(c => c.Id).ToList()))
        .RuleFor(o => o.ProductId, f => f.PickRandom(context.Products.Select(c => c.Id).ToList()));
        var reportFaker = new Faker<Report>()
            .RuleFor(r => r.Title, f => f.PickRandom(context.Products.Select(c => c.Name).ToList())) // Tytuł raportu
            .RuleFor(r => r.Description, f => f.Lorem.Sentence()) // Opis raportu
            .RuleFor(r => r.Answered, f => f.Random.Bool()) // Odpowiedziano na raport
            .RuleFor(r => r.ClientId, f => f.PickRandom(context.Clients.Select(c => c.Id).ToList())); // Losowe przypisanie
        var reviewFaker = new Faker<Review>()
            .RuleFor(r => r.Comment, f => f.Lorem.Sentence()) // Komentarz do recenzji
            .RuleFor(r => r.Rating, f => f.Random.Int(1, 5)) // Ocena (od 1 do 5)
            .RuleFor(r => r.ProductId, f => f.PickRandom(context.Products.Select(c => c.Id).ToList())) // Losowy ProductId z wygenerowanych produktów
            .RuleFor(r => r.ClientId, f => f.PickRandom(context.Clients.Select(c => c.Id).ToList())); // Losowy ClientId z wygenerowanych klientów

        Console.WriteLine("Wybierz co chcesz generować:");
        Console.WriteLine("1. Klienci");
        Console.WriteLine("2. Kategorie");
        Console.WriteLine("3. Produkty");
        Console.WriteLine("4. Recenzje");
        Console.WriteLine("5. Zamówienia");
        Console.WriteLine("7. Roparty");
        Console.WriteLine("6. Wszystko");
        var choice = Console.ReadLine();

        Console.WriteLine("Ile rekordów chcesz wygenerować?");
        int recordCount = int.Parse(Console.ReadLine());

        context.ChangeTracker.AutoDetectChangesEnabled = false;

        using var transaction = context.Database.BeginTransaction();

        try
        {
            if (choice == "1" || choice == "6")
            {
                var clients = clientFaker.Generate(recordCount);
                context.Clients.AddRange(clients);
                context.SaveChanges();
                Console.WriteLine("Klienci wygenerowani.");
            }

            if (choice == "2" || choice == "6")
            {
                var categories = categoryFaker.Generate(recordCount);
                context.Categories.AddRange(categories);
                context.SaveChanges();
                Console.WriteLine("Kategorie wygenerowane.");
            }

            if (choice == "3" || choice == "6")
            {
                var products = productFaker.Generate(recordCount);
                context.Products.AddRange(products);
                context.SaveChanges();
                Console.WriteLine("Produkty wygenerowane.");
            }

            if (choice == "4" || choice == "6")
            {
                var review = reviewFaker.Generate(recordCount);
                context.SaveChanges();
                Console.WriteLine("wygenerowano review");
            }

			if (choice == "5" || choice == "6")
			{
				// Ensure there are clients and products in the database
				if (!context.Clients.Any())
				{
					Console.WriteLine("Brak klientów w bazie. Generowanie klientów...");
					var clients = clientFaker.Generate(recordCount);
					context.Clients.AddRange(clients);
					context.SaveChanges();
				}

				if (!context.Products.Any())
				{
					Console.WriteLine("Brak produktów w bazie. Generowanie produktów...");
					var products = productFaker.Generate(recordCount);
					context.Products.AddRange(products);
					context.SaveChanges();
				}

				Console.WriteLine("Generowanie zamówień z unikalnymi adresami dostawy...");
				var orders = orderFaker.Generate(recordCount);

				// Add Orders to the context
				context.Orders.AddRange(orders);
				context.SaveChanges();

				// Generate ProductOrderRelation for each Order
				var productOrderRelations = new List<ProductOrderRelation>();
				var productIds = context.Products.Select(p => p.Id).ToList();

				foreach (var order in orders)
				{
					// Ensure at least one product is associated with the order
					var selectedProductIds = productIds
						.OrderBy(x => Guid.NewGuid())
						.Take(new Random().Next(1, Math.Min(5, productIds.Count))) // Pick 1 to 5 products
						.ToList();

					// Create ProductOrderRelation entries
					foreach (var productId in selectedProductIds)
					{
						productOrderRelations.Add(new ProductOrderRelation
						{
							OrderId = order.Id,
							ProductId = productId
						});
					}
				}

				// Add ProductOrderRelations to the context
				context.ProductOrderRelations.AddRange(productOrderRelations);
				context.SaveChanges();
				Console.WriteLine("Zamówienia i relacje z produktami wygenerowane.");
			}
			if (choice == "7" || choice == "6")
            {
                var report = reportFaker.Generate(recordCount);
                context.SaveChanges();
                Console.WriteLine("wygenerowano raporty");
            }

            transaction.Commit();
            Console.WriteLine("Wszystkie dane zostały wygenerowane.");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Console.WriteLine($"Błąd: {ex.Message}");
            Console.WriteLine($"Szczegóły: {ex.InnerException?.Message}");
        }
        finally
        {
            context.ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }
}
