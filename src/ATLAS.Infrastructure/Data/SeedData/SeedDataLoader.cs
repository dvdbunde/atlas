using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ATLAS.Domain.Entities;
using ATLAS.Domain.ValueObjects;
using ATLAS.Domain.Enums;

namespace ATLAS.Infrastructure.Data.SeedData
{
    public class SeedDataLoader
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SeedDataLoader> _logger;

        public SeedDataLoader(ApplicationDbContext context, IConfiguration configuration, ILogger<SeedDataLoader> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task LoadSeedDataAsync()
        {
            // Check if permit types already exist
            if (await _context.PermitTypes.AnyAsync())
            {
                _logger.LogInformation("Permit types already seeded. Skipping.");
                return;
            }

            var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "SeedData", "PermitTypes.json");
            
            if (!File.Exists(jsonPath))
            {
                _logger.LogWarning("PermitTypes.json not found at {Path}. Skipping seed.", jsonPath);
                return;
            }

            var json = await File.ReadAllTextAsync(jsonPath);
            var permitTypes = JsonSerializer.Deserialize<List<PermitTypeSeedModel>>(json);

            foreach (var pt in permitTypes)
            {
                var permitType = new PermitType(pt.Name, pt.Description, pt.Fee);
                
                foreach (var field in pt.Fields)
                {
                    var fieldType = Enum.Parse<FieldType>(field.Type);
                    var defaultValue = field.DefaultValue ?? string.Empty;                    
                    permitType.AddField(field.Name, fieldType, field.IsRequired, defaultValue);
                }

                await _context.PermitTypes.AddAsync(permitType);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} permit types.", permitTypes.Count);
        }

        private class PermitTypeSeedModel
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public decimal Fee { get; set; }
            public List<FieldSeedModel> Fields { get; set; }
        }

        private class FieldSeedModel
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public bool IsRequired { get; set; }
            public string DefaultValue { get; set; }
        }
    }
}