// Isolated display tests only: no game methods, input, save files or callbacks execute.
using UnityEngine;
public class MenuPrompt : MonoBehaviour { }
public class MenuEvolvePlayer : MonoBehaviour { }
public class MenuWin : MonoBehaviour { }
public class MenuLibrary : MonoBehaviour { }
public class MenuDeitySelectPlayer : MonoBehaviour { }
public class GUIStore : MonoBehaviour { }
public class TextWrapper { }
public class Deity
{
    public string Name="Teok",Text="Monsters are stronger";
    public string GetName() { return Name; }
    public string GetText() { return Text; }
}
public class ProgressionPlayerGameData { public Deity Deity=new Deity(); public Deity GetDeity() { return Deity; } }
public class PlayerData { public string Name; public float Level=10; public ProgressionPlayerGameData GameData=new ProgressionPlayerGameData(); }
public static class SystemPlayers
{
    public static PlayerData[] Players=new PlayerData[4];
    public static int GetMaxPlayers() { return Players.Length; }
    public static PlayerData GetPlayer(int id) { return Players[id]; }
}

public class SystemMenu : MonoBehaviour { }
