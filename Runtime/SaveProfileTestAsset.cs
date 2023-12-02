using Cysharp.Threading.Tasks;
using MobX.Inspector;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MobX.Serialization
{
    public class SaveProfileTestAsset : ScriptableObject
    {
        [SerializeField] private SaveProfile profile;

        [Line]
        [Button(ButtonStyle.FoldoutButton)]
        private void SaveData(string key, string data)
        {
            profile.SaveFile(key, data);
        }

        [Line]
        [Button(ButtonStyle.FoldoutButton)]
        private void LoadData(string key)
        {
            var file = profile.LoadFile<string>(key);
            Debug.Log(file);
        }

        [Line]
        [Button(ButtonStyle.FoldoutButton)]
        private void LoadProfile()
        {
            profile.LoadAsync().Forget();
        }
    }
}