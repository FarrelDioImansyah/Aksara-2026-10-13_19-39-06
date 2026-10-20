using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Supabase/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("Supabase Configuration")]
    public string supabaseRestUrl;

    [TextArea]
    public string supabaseApiKey;
}            