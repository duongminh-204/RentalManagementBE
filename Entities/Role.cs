using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Backend.Entities;

public class Role : IdentityRole<int>
{
    [NotMapped]
    public int RoleId
    {
        get => Id;
        set => Id = value;
    }

    [MaxLength(500)]
    public string? Description { get; set; }
}
