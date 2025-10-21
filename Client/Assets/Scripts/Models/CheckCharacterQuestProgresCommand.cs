using System;

[Serializable]
public class CheckCharacterQuestProgresCommand
{
    public int characterId;

    public string gameObjectName;

    public int progres;
    
    public string clientToken;
}