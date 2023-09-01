using MobX.Utilities;
using System.Collections.Generic;

namespace MobX.Serialization.Mediator
{
    public class StringListSaveData : SaveDataAsset<List<string>>
    {
    }

    public static class SaveDataListAssetExtensions
    {
        public static void Add<T>(this SaveDataAsset<List<T>> asset, T element)
        {
            asset.Value.Add(element);
        }

        public static void AddUnique<T>(this SaveDataAsset<List<T>> asset, T element)
        {
            asset.Value.AddUnique(element);
        }

        public static bool Remove<T>(this SaveDataAsset<List<T>> asset, T element)
        {
            return asset.Value.Remove(element);
        }

        public static bool Contains<T>(this SaveDataAsset<List<T>> asset, T element)
        {
            return asset.Value.Contains(element);
        }

        public static int Count<T>(this SaveDataAsset<List<T>> asset, T element)
        {
            return asset.Value.Count;
        }

        public static List<T>.Enumerator GetEnumerator<T>(this SaveDataAsset<List<T>> asset)
        {
            return asset.Value.GetEnumerator();
        }
    }
}
