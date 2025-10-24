﻿using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectX.Domain.Entities;

public class CharacterTransform
{
    public int Id { get; set; }

    public int CharacterId { get; set; }

    public float PositionX { get; set; }

    public float PositionY { get; set; }

    public float PositionZ { get; set; }

    public float RotationY { get; set; }

    public DateTime ModDate { get; set; }

    [ForeignKey(nameof(CharacterId))]
    [InverseProperty(nameof(Character.CharacterTransforms))]
    public virtual Character Character { get; set; }
}
