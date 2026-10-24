using LogiTrack.Models;
using Microsoft.EntityFrameworkCore;

namespace LogiTrack.Data.Seeding;

public class SeedService(LogiTrackContext context, ILogger<SeedService> logger)
{

    public async Task SeedDataAsync()
    {
        if (await context.Items.AnyAsync())
        {
            logger.LogInformation("Data already exists, skipping seeding");               
            return;
        }

        logger.LogInformation("Seeding initial data...");

        var inventoryItem = Item.Create("Pallet Jack", 12, "Warehouse A");
        context.Items.Add(inventoryItem);

        context.SaveChanges();

        foreach (var item in context.Items)
        {
            logger.LogInformation($"Seeded Inventory Item: {item.DisplayInfo()}");
        }
    }
}