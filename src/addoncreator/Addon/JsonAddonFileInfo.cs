using System.Text;
using Newtonsoft.Json;

namespace GarrysMod.AddonCreator.Addon
{
    /// <summary>
    /// Represents a JSON-serialized object, prepared for inclusion in <see cref="AddonFile"/> instances.
    /// </summary>
    public class JsonAddonFileInfo : AddonFileInfo
    {
        private readonly byte[] _serializedJson;

        /// <summary>
        /// JSON-serializes a given object using UTF-8 encoding.
        /// </summary>
        /// <param name="obj">The object to serialize</param>
        public JsonAddonFileInfo(object obj)
        {
            _serializedJson = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));
        }

        /// <summary>
        /// Returns the serialized object as a byte array.
        /// </summary>
        /// <returns></returns>
        public override byte[] GetContents()
        {
            return _serializedJson;
        }
    }
}