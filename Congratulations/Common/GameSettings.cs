using Congratulations.Game;

namespace Congratulations.Common;

public static class GameSettings
{
    public static float GetEffectiveSfxVolume()
    {
        if (GameConfig.System.GetBool("IsSndSe") ||
            GameConfig.System.GetBool("IsSndMaster"))
        {
            return 0;
        }
        return GameConfig.System.GetUInt("SoundSe")/100f * (GameConfig.System.GetUInt("SoundMaster")/100f);
    }
}
