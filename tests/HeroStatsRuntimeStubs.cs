// Only used by the isolated Unity HUD regression check.
using System.Collections.Generic;
using UnityEngine;
public class GUIHeroStats : MonoBehaviour { }
public class GUIArtifactIcon : MonoBehaviour { public TextMesh m_text; }
public enum ePurchasableType { None, Spell, Artifact, Potion, Weapon }
public class Powerup
{
    public string m_displayName, m_displayNameHud;
    public ePurchasableType Kind;
    public ePurchasableType GetPurchasableType() { return Kind; }
}
public class PlayerGameData
{
    public object Weapon = new object();
    public List<Powerup> Powerups = new List<Powerup>();
    public object GetWeaponData() { return Weapon; }
    public List<Powerup> GetPowerups() { return Powerups; }
}
