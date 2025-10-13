using System;

[Serializable]
public class AddCharacterExperienceDto
{
    public byte level;

    public byte skillPoints;

    public int experience;

    public bool leveledUp;
}