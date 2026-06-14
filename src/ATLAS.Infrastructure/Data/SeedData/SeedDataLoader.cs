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

namespace ATLAS.Infrastructure.Data.SeedData
{
    public class SeedDataLoader
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SeedDataLoader> _logger;

        public SeedDataLoader(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<SeedDataLoader> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SeedPermitTypesAsync(CancellationToken cancellationToken = default)
        {
            // Check if permit types already exist
            if (await _context.PermitTypes.AnyAsync(cancellationToken))
            {
                _logger.LogInformation("Permit types already seeded. Skipping.");
                return;
            }

            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                "Data", "SeedData", "PermitTypes.json");

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Seed file not found at {FilePath}. Skipping.", filePath);
                return;
            }

            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var seedData = JsonSerializer.Deserialize<PermitTypeSeedDto[]>(json);

            if (seedData == null || seedData.Length == 0)
            {
                _logger.LogWarning("No permit types found in seed file. Skipping.");
                return;
            }

            foreach (var dto in seedData)
            {
                var permitType = new PermitType(dto.Name, dto.Description, dto.Fee);

                foreach (var fieldDto in dto.Fields)
                {
                    if (Enum.TryParse<FieldType>(fieldDto.Type, out var fieldType))
                    {
                        permitType.AddField(fieldDto.Name, fieldType, fieldDto.IsRequired, fieldDto.DefaultValue);
                    }
                }

                _context.PermitTypes.Add(permitType);
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully seeded {Count} permit types.", seedData.Length);
        }
    }

    public class PermitTypeSeedDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public bool IsActive { get; set; }
        public PermitFieldSeedDto[] Fields { get; set; } = Array.Empty<PermitFieldSeedDto>();
    }

    public class PermitFieldSeedDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public string? DefaultValue { get; set; }
    }
}