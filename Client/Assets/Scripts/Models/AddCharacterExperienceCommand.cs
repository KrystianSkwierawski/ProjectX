using System;

[Serializable]
public class AddCharacterExperienceCommand
{
    public int amount;

    public ExperienceTypeEnum type;

    public string clientToken;
}
