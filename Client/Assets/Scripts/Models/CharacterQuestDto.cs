using System;

[Serializable]
public class CharacterQuestDto
{
    public int id;

    public int questId;

    public CharacterQuestStatusEnum status;

    public int progress;
}