namespace ATLAS.Application;

/// <summary>
/// Marker class used for MediatR assembly scanning
/// This allows MediatR to discover handlers from the Application layer
/// </summary>
public class AssemblyMarker
{
    // This class serves as a marker for assembly scanning
    // MediatR will scan this assembly for request/notification handlers
}