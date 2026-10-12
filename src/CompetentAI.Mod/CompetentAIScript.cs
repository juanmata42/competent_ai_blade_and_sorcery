using ThunderRoad;
namespace CompetentAI.Mod
{
    public class CompetentAIScript : ThunderScript
    {
        public override void ScriptLoaded(ModManager.ModData modData) { base.ScriptLoaded(modData); UnityEngine.Debug.Log("probe " + CompetentAI.Core.Probe.One); }
    }
}
