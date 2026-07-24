using System;
using ATLAS.Domain.Enums;

namespace ATLAS.Application.DTOs;

/// <summary>
/// Lightweight, read-only projection of an application for the Administrator
/// Application Explorer. Contains only the fields needed for the list view.
/// </summary>
public class AdminApplicationExplorerDto
{
    public Guid ApplicationId { get; init; }
    public string ApplicationNumber { get; init; } = string.Empty;
    public string PermitTypeName { get; init; } = string.Empty;
    public string CitizenName { get; init; } = string.Empty;
    public ApplicationStatus Status { get; init; }
    public string? AssignedOfficerName { get; init; }
    public DateTime? SubmittedDate { get; init; }
    public DateTime? LastUpdated { get; init; }
}