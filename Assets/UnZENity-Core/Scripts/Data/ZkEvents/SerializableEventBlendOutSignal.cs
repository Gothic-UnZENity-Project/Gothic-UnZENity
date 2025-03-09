namespace GUZ.Core.Data.ZkEvents
{
    /// <summary>
    /// UnityEngine.JsonUtility doesn't serialize CachedEventTag. We therefore use this class to JSON-ify the data.
    /// </summary>
    public class SerializableEventBlendOutSignal
    {
        public string CurrentAnimName;
        public float BlendOutTime;

        public SerializableEventBlendOutSignal(string currentAnimName, float blendOutTime)
        {
            CurrentAnimName = currentAnimName;
            BlendOutTime = blendOutTime;
        }
    }
}
