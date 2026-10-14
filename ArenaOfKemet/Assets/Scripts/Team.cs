using UnityEngine;

public enum Team { Kemet, Olympus }

public static class TeamUtil
{
    public static Team Opponent(Team t) => t == Team.Kemet ? Team.Olympus : Team.Kemet;

    public static Color ColorOf(Team t) =>
        t == Team.Kemet ? new Color(0.85f, 0.64f, 0.13f) : new Color(0.35f, 0.55f, 0.85f);

    public static Color AccentOf(Team t) =>
        t == Team.Kemet ? new Color(1f, 0.84f, 0.37f) : new Color(0.62f, 0.85f, 1f);

    public static Shader LitShader =>
        Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

    public static Material Mat(Team t)
    {
        var m = new Material(LitShader);
        m.color = ColorOf(t);
        return m;
    }
}
