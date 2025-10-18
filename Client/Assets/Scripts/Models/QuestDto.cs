using System;

[Serializable]
public class QuestDto
{
    public int id;

    public QuestTypeEnum type;

    public string title;

    public string description;

    public string statusText;

    public int requirement;

    public int reward;
}