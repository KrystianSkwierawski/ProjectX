using System;

[Serializable]
public class QuestDto
{
    public int id;

    public int previousQuestId;

    public QuestTypeEnum type;

    public string title;

    public string description;

    public string completeDescription;

    public string statusText;

    public string gameObjectName;

    public int requirement;

    public int reward;
}