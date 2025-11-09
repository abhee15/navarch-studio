using Microsoft.EntityFrameworkCore;
using Shared.Models.Sizing;

namespace HullSizingService.Data.Seeds;

/// <summary>
/// Seeds ISO container standard dimensions
/// Reference: ISO 668:2020 - Series 1 freight containers
/// </summary>
public static class IsoContainerSeeder
{
    public static async Task SeedAsync(SizingDbContext context, ILogger logger, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[SEED] Loading ISO container standards...");

        // Load existing containers
        var existingContainers = await context.IsoContainers
            .ToDictionaryAsync(c => c.ContainerType, c => c, cancellationToken);

        var containers = GetIsoContainers();

        int added = 0;
        int updated = 0;

        foreach (var container in containers)
        {
            if (existingContainers.TryGetValue(container.ContainerType, out var existing))
            {
                // Update existing
                existing.LengthMm = container.LengthMm;
                existing.WidthMm = container.WidthMm;
                existing.HeightMm = container.HeightMm;
                existing.MaxGrossKg = container.MaxGrossKg;
                updated++;
            }
            else
            {
                // Add new
                context.IsoContainers.Add(container);
                added++;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("[SEED] ISO containers synced: {Added} added, {Updated} updated (Total: {Total})",
            added, updated, containers.Count);
    }

    private static List<IsoContainer> GetIsoContainers()
    {
        return new List<IsoContainer>
        {
            // 20ft General Purpose
            new IsoContainer
            {
                Id = Guid.NewGuid(),
                ContainerType = "20GP",
                LengthMm = 6058,   // 6.058m
                WidthMm = 2438,    // 2.438m (8ft)
                HeightMm = 2591,   // 2.591m (8ft 6in)
                MaxGrossKg = 30480 // 30.48 tonnes
            },

            // 40ft General Purpose
            new IsoContainer
            {
                Id = Guid.NewGuid(),
                ContainerType = "40GP",
                LengthMm = 12192,  // 12.192m (40ft)
                WidthMm = 2438,    // 2.438m (8ft)
                HeightMm = 2591,   // 2.591m (8ft 6in)
                MaxGrossKg = 30480 // 30.48 tonnes
            },

            // 40ft High Cube
            new IsoContainer
            {
                Id = Guid.NewGuid(),
                ContainerType = "40HC",
                LengthMm = 12192,  // 12.192m (40ft)
                WidthMm = 2438,    // 2.438m (8ft)
                HeightMm = 2896,   // 2.896m (9ft 6in)
                MaxGrossKg = 30480 // 30.48 tonnes
            },

            // 45ft High Cube
            new IsoContainer
            {
                Id = Guid.NewGuid(),
                ContainerType = "45HC",
                LengthMm = 13716,  // 13.716m (45ft)
                WidthMm = 2438,    // 2.438m (8ft)
                HeightMm = 2896,   // 2.896m (9ft 6in)
                MaxGrossKg = 30480 // 30.48 tonnes
            },

            // 20ft Open Top
            new IsoContainer
            {
                Id = Guid.NewGuid(),
                ContainerType = "20OT",
                LengthMm = 6058,   // 6.058m (20ft)
                WidthMm = 2438,    // 2.438m (8ft)
                HeightMm = 2591,   // 2.591m (8ft 6in)
                MaxGrossKg = 30480 // 30.48 tonnes
            },

            // 40ft Open Top
            new IsoContainer
            {
                Id = Guid.NewGuid(),
                ContainerType = "40OT",
                LengthMm = 12192,  // 12.192m (40ft)
                WidthMm = 2438,    // 2.438m (8ft)
                HeightMm = 2591,   // 2.591m (8ft 6in)
                MaxGrossKg = 30480 // 30.48 tonnes
            },

            // 20ft Flat Rack
            new IsoContainer
            {
                Id = Guid.NewGuid(),
                ContainerType = "20FR",
                LengthMm = 6058,   // 6.058m (20ft)
                WidthMm = 2438,    // 2.438m (8ft)
                HeightMm = 2591,   // 2.591m (8ft 6in)
                MaxGrossKg = 30480 // 30.48 tonnes
            },

            // 40ft Flat Rack
            new IsoContainer
            {
                Id = Guid.NewGuid(),
                ContainerType = "40FR",
                LengthMm = 12192,  // 12.192m (40ft)
                WidthMm = 2438,    // 2.438m (8ft)
                HeightMm = 2591,   // 2.591m (8ft 6in)
                MaxGrossKg = 30480 // 30.48 tonnes
            }
        };
    }
}


