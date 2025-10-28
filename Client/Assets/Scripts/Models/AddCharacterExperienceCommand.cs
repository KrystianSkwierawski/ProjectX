using System;

[Serializable]
public class AddCharacterExperienceCommand
{
    public int characterId;

    public int characterQuestId;

    public ExperienceTypeEnum type;
}
