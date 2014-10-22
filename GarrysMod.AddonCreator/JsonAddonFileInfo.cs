using System.Text;
using Newtonsoft.Json;

namespace GarrysMod.AddonCreator
{
    public class JsonAddonFileInfo : AddonFileInfo
    {
        private readonly byte[] _serializedJson;

        public JsonAddonFileInfo(object obj)
        {
            _serializedJson = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));
        }

        public override byte[] GetContents()
        {
            return _serializedJson;
        }
    }
}